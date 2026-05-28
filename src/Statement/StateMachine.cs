using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Statement.Failures;
using Statement.Rules;
using Statement.Triggers;
using Statement.Utils;

namespace Statement;

/// <summary>
/// A state machine that holds a set of registered state types and transitions between them.
/// Instances are created and configured through <see cref="Statement.Fluent.Api.StateMachineBuilder"/>.
/// Transitions are gated by configured rules and surrounded by registered entry/exit callbacks.
/// </summary>
public class StateMachine
{
    private Type? _innerParentType;
    private readonly Dictionary<Type, StateNode> _nodes = new();
    private StateNode? _current;
    private readonly RuleMaster _ruleMaster = new();
    private readonly TransitionExecutor _transitionExecutor = new();
    private readonly List<Func<TransitionInformation, Task>> _transitionCallbacks = [];
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1,maxCount: 1);
    private readonly AsyncLocal<bool> _inTransition = new();
    private readonly Queue<(object trigger, object? payload)> _triggerQueue = [];
    private volatile StateMachineState _executionState = StateMachineState.Active;

    internal StateMachine() { }
    internal TransitionFailurePolicy FailurePolicy { get; set; } = TransitionFailurePolicy.Silent;
    internal TriggerFailurePolicy TriggerFailurePolicy { get; set; } = TriggerFailurePolicy.Silent;

    /// <summary>
    /// Current execution state of the machine. When <see cref="StateMachineState.Paused"/>, transition and trigger
    /// calls are short-circuited and reported through the configured failure policy.
    /// <remarks>
    ///changing the <see cref="ExecutionState"/> does not affect any currently active transition. They will not abort!
    /// </remarks>
    /// </summary>
    public StateMachineState ExecutionState
    {
        get => _executionState;
        private set => _executionState = value;
    }

    private bool IsCompiledWithType => _innerParentType is not null;

    /// <summary>
    /// Pauses the machine. Subsequent transition and trigger requests are skipped until <see cref="Resume"/> is called.
    /// </summary>
    public void Pause() => ExecutionState = StateMachineState.Paused;

    /// <summary>
    /// Resumes the machine after a previous <see cref="Pause"/> call.
    /// </summary>
    public void Resume() =>  ExecutionState = StateMachineState.Active;

    /// <summary>
    /// Marks the registered state <typeparamref name="T"/> as active, making it a valid transition target again.
    /// </summary>
    /// <remarks>
    /// States are active by default; this only has an observable effect after a prior <see cref="DeactivateState{T}"/> call.
    /// Has no effect if <typeparamref name="T"/> is not registered on this machine. Does not affect the current state or
    /// any in-flight transition — it only gates future transition checks performed by the rule evaluator.
    /// </remarks>
    /// <typeparam name="T">The registered state type to activate.</typeparam>
    public void ActivateState<T>()
    {
        if (_nodes.TryGetValue(typeof(T), out var target))
        {
            target.IsActive = true;
        }
    }

    /// <summary>
    /// Marks the registered state <typeparamref name="T"/> as inactive, preventing future transitions into it.
    /// </summary>
    /// <remarks>
    /// While inactive, the rule evaluator rejects transitions targeting <typeparamref name="T"/>, and triggers whose
    /// handler targets <typeparamref name="T"/> will be reported as not fireable by <see cref="CanTrigger"/> /
    /// <see cref="CanFire"/>. Has no effect if <typeparamref name="T"/> is not registered, and does not interrupt
    /// the current state or any in-flight transition — even if the deactivated state happens to be the current one.
    /// Re-enable with <see cref="ActivateState{T}"/>.
    /// </remarks>
    /// <typeparam name="T">The registered state type to deactivate.</typeparam>
    public void DeactivateState<T>()
    {
        if (_nodes.TryGetValue(typeof(T), out var target))
        {
            target.IsActive = false;
        }
    }

    /// <summary>
    /// Transitions the machine to the registered state of type <typeparamref name="T"/>.
    /// The transition is silently ignored if the target state is not registered or is blocked by a transition rule.
    /// Exit callbacks of the previous state and entry callbacks of the new state run as part of the transition.
    /// </summary>
    /// <typeparam name="T">The state type to switch to. Must have been registered on this machine.</typeparam>
    public void SetCurrentState<T>()
    {
        if (!ExecutionState.ReportIfPausedAndBlock(FailurePolicy, _current?.GetType(), typeof(T)))
        {
            return;
        }
        
        _inTransition.ThrowIfActive();
        _semaphore.RunAction(() => SetCurrentStateByType(typeof(T), null));
    }

    /// <summary>
    /// Transitions to <typeparamref name="T"/> and carries a typed <paramref name="payload"/> through to
    /// the target state's <see cref="Statement.Fluent.Api.StateBuilder{TState}.OnEntryWith{TPayload}"/> callback
    /// and to any global transition callbacks via <see cref="TransitionInformation.Payload"/>.
    /// </summary>
    public void SetCurrentState<T>(object? payload)
    {
        if (!ExecutionState.ReportIfPausedAndBlock(FailurePolicy, _current?.GetType(), typeof(T)))
        {
            return;
        }
        
        _inTransition.ThrowIfActive();
        _semaphore.RunAction(() => SetCurrentStateByType(typeof(T), payload));
    }

    /// <summary>
    /// Asynchronously transitions the machine to the registered state of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Cancellation semantics: the token is checked once before any callback runs and is forwarded to all
    /// async entry/exit callbacks. Cancellation is best-effort, not transactional —
    /// if a user callback observes the token (e.g. <c>Task.Delay(ct)</c>) and throws, the transition aborts
    /// at that point; if cancellation falls between callbacks where the token is not observed, the
    /// transition runs to completion. Callers should not assume machine state on cancellation —
    /// inspect <see cref="GetCurrentState()"/>.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the transition starts, or surfaced from a user callback that observes the token.</exception>
    public Task SetCurrentStateAsync<T>(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ExecutionState.ReportIfPausedAndBlock(FailurePolicy, _current?.GetType(), typeof(T)))
        {
            return Task.CompletedTask;
        }
        _inTransition.ThrowIfActive();
        return _semaphore.RunActionAsync(() => SetCurrentStateByTypeAsync(typeof(T), null, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Asynchronously transitions to <typeparamref name="T"/> with a typed <paramref name="payload"/>.
    /// </summary>
    /// <remarks>See <see cref="SetCurrentStateAsync{T}(CancellationToken)"/> for cancellation semantics.</remarks>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the transition starts, or surfaced from a user callback that observes the token.</exception>
    public Task SetCurrentStateAsync<T>(object? payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ExecutionState.ReportIfPausedAndBlock(FailurePolicy, _current?.GetType(), typeof(T)))
        {
            return Task.CompletedTask;
        }
        
        _inTransition.ThrowIfActive();
        return _semaphore.RunActionAsync(() => SetCurrentStateByTypeAsync(typeof(T), payload, cancellationToken), cancellationToken);
    }

    internal async Task SetCurrentStateByTypeAsync(
        Type stateType,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ExecutionState.ReportIfPausedAndBlock(FailurePolicy, _current?.GetType(), stateType))
        {
            return;
        }
        
        _inTransition.ThrowIfActive();
        _inTransition.Value = true;
        try
        {
            await PerformEnqueuedStateTransitions(cancellationToken);
            await HandleSetCurrentState(stateType, payload, cancellationToken);
            await PerformEnqueuedStateTransitions(cancellationToken);
        }
        finally
        {
            _inTransition.Value = false;
        }
    }

    private async Task HandleSetCurrentState(Type stateType, object? payload, CancellationToken cancellationToken)
    {
        if (!_nodes.TryGetValue(stateType, out var target))
        {
            throw new InvalidOperationException($"State {stateType} is not registered.");
        }

        if (!_ruleMaster.IsAllowedTransition(_current, target))
        {
            FailurePolicy.Handle(new TransitionFailureInfo(_current?.Type, stateType, TransitionFailureReason.BlockedByRule));
            return;
        }

        var transition = new Transition(_current, target, payload: payload, cancellationToken: cancellationToken);
        await _transitionExecutor.ExecuteAsync(transition, this, () => _current = target);
    }

    /// <summary>
    /// Fires a trigger on the machine. Looks up the trigger on the current state's trigger table and,
    /// if a handler is registered (and its guard passes), executes the configured transition.
    /// </summary>
    /// <param name="trigger">The trigger value. May be any non-null object — marker type instance, enum value, string, etc.</param>
    /// <exception cref="ArgumentNullException"><paramref name="trigger"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the machine has no current state (should not happen on a built machine).</exception>
    public void Fire(object trigger) => Fire(trigger, null);

    /// <summary>
    /// Fires a trigger and carries a typed <paramref name="payload"/> through to the target state's
    /// <see cref="Statement.Fluent.Api.StateBuilder{TState}.OnEntryWith{TPayload}"/> callback and to any
    /// global transition callbacks via <see cref="TransitionInformation.Payload"/>.
    /// </summary>
    public void Fire(object trigger, object? payload)
    {
        if (!ExecutionState.ReportIfPausedAndBlock(TriggerFailurePolicy, _current?.GetType(), trigger))
        {
            return;
        }
        
        _inTransition.ThrowIfActive();
        _semaphore.RunAction(() =>
        {
            var task = FireAsyncIntern(trigger, payload);
            if (task.IsFaulted)
            {
                task.GetAwaiter().GetResult();
            }

            if (task.Status != TaskStatus.RanToCompletion)
            {
                throw new InvalidOperationException(
                    "This transition involves async callbacks. Use FireAsync instead.");
            }
        });
    }

    /// <summary>
    /// Asynchronously fires a trigger with a typed <paramref name="payload"/>.
    /// </summary>
    /// <remarks>
    /// Cancellation semantics: the token is checked once before any callback runs and is forwarded to all
    /// async entry/exit callbacks. Cancellation is best-effort, not transactional —
    /// if a user callback observes the token (e.g. <c>Task.Delay(ct)</c>) and throws, the transition aborts
    /// at that point; if cancellation falls between callbacks where the token is not observed, the
    /// transition runs to completion. Callers should not assume machine state on cancellation —
    /// inspect <see cref="GetCurrentState()"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="trigger"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the transition starts, or surfaced from a user callback that observes the token.</exception>
    public Task FireAsync(object trigger, object? payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ExecutionState.ReportIfPausedAndBlock(TriggerFailurePolicy, _current?.GetType(), trigger))
        {
            return Task.CompletedTask;
        }
        
        _inTransition.ThrowIfActive();
        return _semaphore.RunActionAsync(() => FireAsyncIntern(trigger, payload, cancellationToken), cancellationToken);
    }
    
    /// <summary>
    /// Asynchronously fires a trigger on the machine.
    /// </summary>
    /// <remarks>See <see cref="FireAsync(object, object?, CancellationToken)"/> for cancellation semantics.</remarks>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the transition starts, or surfaced from a user callback that observes the token.</exception>
    public Task FireAsync(object trigger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ExecutionState.ReportIfPausedAndBlock(TriggerFailurePolicy, _current?.GetType(), trigger))
        {
            return Task.CompletedTask;
        }
        _inTransition.ThrowIfActive();
        return _semaphore.RunActionAsync(() => FireAsyncIntern(trigger, null, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Returns the current state instance cast to <typeparamref name="T"/>.
    /// The instance is created lazily on first access and cached for the lifetime of the machine.
    /// </summary>
    /// <typeparam name="T">The expected type of the current state.</typeparam>
    /// <returns>The current state instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no state is set, or the current state is not assignable to <typeparamref name="T"/>.</exception>
    public T GetCurrentState<T>() where T : class
    {
        if (_current?.GetOrCreateInstance() is T state)
        {
            return state;
        }
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Attempts to get the current state instance as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the current state.</typeparam>
    /// <param name="result">When this method returns <c>true</c>, contains the current state instance; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a current state is set and is assignable to <typeparamref name="T"/>; otherwise <c>false</c>.</returns>
    public bool TryGetCurrentState<T>(out T? result) where T : class
    {
        if (_current?.GetOrCreateInstance() is T state)
        {
            result = state;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Returns the current state instance as <see cref="object"/>, or <c>null</c> if no state is currently set.
    /// </summary>
    public object? GetCurrentState() => _current?.GetOrCreateInstance();

    /// <summary>
    /// Schedules a trigger to fire, either immediately (if called outside a transition) or after the current transition completes (if called from inside a callback).
    /// </summary>
    /// <remarks>
    /// This method is safe to call from <see cref="Statement.Fluent.Api.StateBuilder{TState}.OnEntry"/>,
    /// <see cref="Statement.Fluent.Api.StateBuilder{TState}.OnExit"/>, and transition callbacks.
    /// When called from inside a callback, the trigger is queued and will be processed after the current
    /// transition and all of its callbacks have completed, avoiding re-entrance and deadlock.
    ///
    /// Queued triggers are processed in FIFO order. If a queued trigger's callbacks enqueue additional triggers,
    /// those are appended to the queue and processed after the current trigger completes.
    ///
    /// When called outside a transition (e.g., from application code), this method behaves identically to
    /// <see cref="FireAsync(object, object?, CancellationToken)"/> and fires the trigger immediately.
    /// </remarks>
    /// <param name="trigger">The trigger to enqueue or fire. Must not be <c>null</c>.</param>
    /// <param name="payload">Optional payload to deliver to the target state's entry callback or global transition callbacks.</param>
    /// <returns>A <see cref="Task"/> that completes when the trigger (and any triggers it enqueues) have finished.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="trigger"/> is <c>null</c>.</exception>
    public async Task EnqueueAsync(object trigger, object? payload)
    {
        if (_inTransition.Value)
        {
            _triggerQueue.Enqueue((trigger, payload));
        }
        else
        {
            await _semaphore.RunActionAsync(() => FireAsyncIntern(trigger, payload));
        }
    }

    /// <summary>
    /// Schedules a trigger to fire synchronously, either immediately (if called outside a transition) or after the current transition completes (if called from inside a callback).
    /// </summary>
    /// <remarks>
    /// This method is the synchronous variant of <see cref="EnqueueAsync(object, object?)"/> and is safe to call from synchronous
    /// <see cref="Statement.Fluent.Api.StateBuilder{TState}.OnEntry"/>, <see cref="Statement.Fluent.Api.StateBuilder{TState}.OnExit"/>,
    /// and transition callbacks.
    ///
    /// When called from inside a callback, the trigger is queued and will be processed after the current transition and all
    /// of its callbacks have completed.
    ///
    /// When called outside a transition (e.g., from application code), this method behaves identically to
    /// <see cref="Fire(object, object?)"/> and fires the trigger immediately.
    ///
    /// If the enqueued or fired trigger involves async callbacks, an <see cref="InvalidOperationException"/> is thrown
    /// instructing the caller to use <see cref="EnqueueAsync(object, object?)"/> instead.
    /// </remarks>
    /// <param name="trigger">The trigger to enqueue or fire. Must not be <c>null</c>.</param>
    /// <param name="payload">Optional payload to deliver to the target state's entry callback or global transition callbacks.</param>
    /// <exception cref="ArgumentNullException"><paramref name="trigger"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The transition involves async callbacks. Use <see cref="EnqueueAsync(object, object?)"/> instead.</exception>
    public void Enqueue(object trigger, object? payload)
    {
        if (_inTransition.Value)
        {
            _triggerQueue.Enqueue((trigger, payload));
        }
        else
        {
            Fire(trigger, payload);
        }
    }

    /// <summary>
    /// Enumerates all registered state types that the machine is currently allowed to transition to,
    /// based on the active state's transition rules.
    /// </summary>
    /// <returns>The <see cref="Type"/> keys of every reachable state from the current state.</returns>
    public IReadOnlyList<Type> PossibleNextTransitions()
        => (from nodePair 
            in _nodes 
            where _ruleMaster.IsAllowedTransition(_current, nodePair.Value) 
            select nodePair.Key).ToList();

    /// <summary>
    /// Gets a list of all state types registered on this state machine.
    /// </summary>
    /// <returns>
    /// A list of all <see cref="Type"/> objects representing the registered state types.
    /// The list includes every state type added to the machine during configuration,
    /// regardless of whether transitions to them are currently allowed from the active state.
    /// </returns>
    public IReadOnlyList<Type> GetAllRegisteredStateTypes() => _nodes.Values.Select(stateNode => stateNode.Type).ToList();
    
    /// <summary>
    /// Gets a list of all state instances registered on this state machine.
    /// </summary>
    public IReadOnlyList<object> GetAllRegisteredStateInstances() 
        => _nodes.Values.Select(stateNode => stateNode.GetOrCreateInstance()).ToList();

    /// <summary>
    /// Checks whether a transition from the current state to <paramref name="possibleNextState"/> is permitted
    /// by the current state's transition rules.
    /// </summary>
    /// <param name="possibleNextState">The target state type to check.</param>
    /// <remarks>will throw <see cref="InvalidOperationException"/> if method was called before final build of <see cref="StateMachine"/></remarks>
    /// <returns><c>true</c> if the transition is allowed; otherwise <c>false</c>.</returns>
    public bool CanTransitionTo(Type possibleNextState)
        => _nodes.TryGetValue(possibleNextState, out var target)
           && _ruleMaster.CheckIfTypeIsValidNextState(_current, target);

    /// <summary>
    /// Returns the triggers that the current state has handlers registered for.
    /// </summary>
    /// <remarks>
    /// The result reflects only the trigger keys configured on the active state's trigger table.
    /// It does not evaluate per-handler guards or transition rules, so a trigger present in this list
    /// may still be rejected at fire time if its guard fails or the target transition is blocked.
    /// Returns an empty list when no state is currently set.
    /// </remarks>
    /// <returns>A list of trigger keys registered in the current state.</returns>
    public IReadOnlyCollection<object> GetAllPossibleTriggers() => (_current?.Triggers.Keys ?? Enumerable.Empty<object>()).ToList();

    /// <summary>
    /// Checks whether the current state has a handler registered for <paramref name="trigger"/>.
    /// </summary>
    /// <param name="trigger">The trigger value to check. Any non-null object — marker type instance, enum value, string, etc.</param>
    /// <remarks>
    /// This only checks for handler presence; it does not evaluate the handler's guard or the target
    /// state's transition rules. A return value of <c>true</c> does not guarantee that firing the trigger
    /// will result in a transition. Returns <c>false</c> when no state is currently set.
    /// </remarks>
    /// <returns><c>true</c> if a handler is registered for the trigger on the current state; otherwise <c>false</c>.</returns>
    public bool HasTrigger(object trigger) => _current?.Triggers.ContainsKey(TriggerKey.Of(trigger)) ?? false;

    /// <summary>
    /// Checks whether firing <paramref name="trigger"/> on the current state would lead to an allowed transition.
    /// </summary>
    /// <param name="trigger">The trigger value to check. Any non-null object — marker type instance, enum value, string, etc.</param>
    /// <remarks>
    /// Returns <c>true</c> only if the current state has a handler registered for the trigger, the handler
    /// has a target state, and the configured transition rules permit moving from the current state to that target.
    /// Per-handler guards are not evaluated here, so a <c>true</c> result does not guarantee the transition will run —
    /// a guard may still reject it at fire time. Use <see cref="HasTrigger(object)"/> to check only for handler presence.
    /// </remarks>
    /// <returns><c>true</c> if the trigger is handled and its target transition is currently allowed; otherwise <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the machine has no current state (i.e., the builder's <c>Build</c> step has not run).</exception>
    public bool CanTrigger(object trigger)
    {
        if (_current is null)
        {
            throw new InvalidOperationException("Machine has no current state. Call >>build<< before calling this method.");
        }
        
        if (!HasTrigger(trigger))
        {
            return false;
        }

        var triggerHandler = _current.Triggers[TriggerKey.Of(trigger)];
        if (triggerHandler?.Target != null
            && _nodes.TryGetValue(triggerHandler.Target, out var target)
            && _ruleMaster.CheckIfTypeIsValidNextState(_current, target))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether firing <paramref name="trigger"/> with <paramref name="payload"/> on the current state
    /// would result in an allowed transition, including evaluation of the handler's guard.
    /// </summary>
    /// <param name="trigger">The trigger value to check. Any non-null object — marker type instance, enum value, string, etc.</param>
    /// <param name="payload">The payload that would be forwarded to the guard. Passed through as-is; may be <c>null</c>.</param>
    /// <remarks>
    /// Returns <c>true</c> only if all the following hold: the current state has a handler registered for
    /// the trigger, the handler has a target state, the configured transition rules permit the transition,
    /// and the handler's guard (if any) returns <c>true</c> for <paramref name="payload"/>.
    /// <para>
    /// This method invokes user-supplied guard code. Guards are expected to be pure and side-effect-free;
    /// if a caller follows <c>CanFire</c> with <see cref="Fire(object)"/>/<see cref="Fire(object,object)"/>,
    /// the guard will run twice. Exceptions thrown from the guard propagate to the caller.
    /// </para>
    /// Use <see cref="CanTrigger(object)"/> if you want the same check without evaluating the guard,
    /// or <see cref="HasTrigger(object)"/> to check only for handler presence.
    /// </remarks>
    /// <returns><c>true</c> if the trigger would fire and transition under current rules and guard; otherwise <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the machine has no current state (i.e., the builder's <c>Build</c> step has not run).</exception>
    public bool CanFire(object trigger, object? payload)
    {
        if (ExecutionState == StateMachineState.Paused)
        {
            return false;
        }
        
        if (_current is null)
        {
            throw new InvalidOperationException("Machine has no current state. Call >>build<< before calling this method.");
        }

        if (!HasTrigger(trigger))
        {
            return false;
        }

        var triggerHandler = _current.Triggers[TriggerKey.Of(trigger)];
        return triggerHandler?.Target != null
               && _nodes.TryGetValue(triggerHandler.Target, out var target)
               && _ruleMaster.CheckIfTypeIsValidNextState(_current, target)
               && (triggerHandler.Guard is null || triggerHandler.Guard(payload));
    }
    
    /// <summary>
    /// Asynchronously fires a trigger with a typed <paramref name="payload"/>.
    /// </summary>
    /// <remarks>
    /// Cancellation semantics: the token is checked once before any callback runs and is forwarded to all
    /// async entry/exit callbacks. Cancellation is best-effort, not transactional —
    /// if a user callback observes the token (e.g. <c>Task.Delay(ct)</c>) and throws, the transition aborts
    /// at that point; if cancellation falls between callbacks where the token is not observed, the
    /// transition runs to completion. Callers should not assume machine state on cancellation —
    /// inspect <see cref="GetCurrentState()"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="trigger"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the transition starts, or surfaced from a user callback that observes the token.</exception>
    internal async Task FireAsyncIntern(
        object trigger,
        object? payload,
        CancellationToken cancellationToken = default)
    {
        if (trigger is null) throw new ArgumentNullException(nameof(trigger));
        _inTransition.ThrowIfActive();
        _inTransition.Value = true;
        try
        {
            await PerformEnqueuedStateTransitions(cancellationToken);
            await HandleStateTransition(trigger, payload, cancellationToken);
            await PerformEnqueuedStateTransitions(cancellationToken);
        }
        finally
        {
            _inTransition.Value = false;
        }
    }

    private async Task HandleStateTransition(object trigger, object? payload, CancellationToken cancellationToken)
    {
        if (_current is null) throw new InvalidOperationException("Machine has no current state.");

        var key = TriggerKey.Of(trigger);
        if (!_current.Triggers.TryGetValue(key, out var handler))
        {
            TriggerFailurePolicy.Handle(new TriggerFailureInfo(_current.Type, trigger, TriggerFailureReason.NoHandler));
            return;
        }
        
        if (handler.Guard is { } g && !g(payload))
        {
            TriggerFailurePolicy.Handle(new TriggerFailureInfo(_current.Type, trigger, TriggerFailureReason.GuardFailed));
            return;
        }

        handler.OnFire?.Invoke(trigger);

        if (handler.Target is null)
        {
            return;
        }

        if (!_nodes.TryGetValue(handler.Target, out var target))
        {
            throw new InvalidOperationException($"Trigger target state {handler.Target} is not registered.");
        }

        if (!_ruleMaster.IsAllowedTransition(_current, target))
        {
            FailurePolicy.Handle(new TransitionFailureInfo(_current.Type, handler.Target, TransitionFailureReason.BlockedByRule));
            return;
        }

        var transition = new Transition(_current, target, trigger, payload, cancellationToken);
        await _transitionExecutor.ExecuteAsync(transition, this, () => _current = target);
    }

    private async Task PerformEnqueuedStateTransitions(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_triggerQueue.Count > 0)
        {
            //we will drain the queued triggers first before handling new triggers
            await DrainQueueAsync(cancellationToken);
        }
    }

    private async Task DrainQueueAsync(CancellationToken cancellationToken = default)
    {
        while (_triggerQueue.Count > 0)
        {
            var (trigger, payload) = _triggerQueue.Dequeue();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await HandleStateTransition(trigger, payload, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                TriggerFailurePolicy.Handle(
                    new TriggerFailureInfo(_current?.Type, trigger, TriggerFailureReason.HandlerThrew, ex));
                // continue draining on transition errors
            }
        }
    }

    internal void AddTransitionCallbacks(params Func<TransitionInformation, Task>[] callbacks)
    {
        foreach (var callback in callbacks)
        {
            _transitionCallbacks.Add(callback);
        }
    }

    internal async Task InvokeTransitionCallbacksAsync(TransitionInformation transitionInformation, CancellationToken cancellationToken = default)
    {
        foreach (var transitionCallback in _transitionCallbacks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await transitionCallback(transitionInformation);
            }
            catch (Exception e)
            {
                //general catch-all for transition callbacks to prevent them from crashing the machine
                Debug.WriteLine($"Exception in transition callback: {e}");
            }
        }
    }

    internal void RegisterInnerState<TState>() where TState : class, new()
    {
        if (_nodes.ContainsKey(typeof(TState)))
        {
            throw new InvalidOperationException($"State {typeof(TState)} is already registered.");
        }
        _nodes.Add(typeof(TState), new StateNode(typeof(TState)));
    }

    internal void RegisterInnerState<TState>(TState instance) where TState : class
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        if (_nodes.ContainsKey(typeof(TState)))
        {
            throw new InvalidOperationException($"State {typeof(TState)} is already registered.");
        }
        _nodes.Add(typeof(TState), new StateNode(typeof(TState), instance));
    }

    internal void AddOnEntry(Type stateType, Func<StateMachine, object?, CancellationToken, Task> callback)
    {
        if (_nodes.TryGetValue(stateType, out var node))
        {
            node.OnEntry = callback;
        }
    }

    internal void AddOnExit(Type stateType, Func<StateMachine, object?, CancellationToken, Task> callback)
    {
        if (_nodes.TryGetValue(stateType, out var node))
        {
            node.OnExit = callback;
        }
    }

    internal void AddForbiddenNextState(Type stateType, Type forbiddenTarget)
    {
        if (!_nodes.TryGetValue(stateType, out var node))
        {
            return;
        }

        node.TransitionRule ??= new TransitionRule();
        node.TransitionRule.ForbiddenNextStates.Add(forbiddenTarget);
    }

    internal void AddAllowedNextState(Type stateType, Type allowedNextState)
    {
        if (!_nodes.TryGetValue(stateType, out var node))
        {
            return;
        }
        
        node.TransitionRule ??= new TransitionRule();
        node.TransitionRule.AllowedNextStates.Add(allowedNextState);
    }

    internal void AddTriggerHandler(Type stateType, object triggerKey, TriggerHandler handler)
    {
        if (!_nodes.TryGetValue(stateType, out var node))
        {
            throw new InvalidOperationException($"State {stateType} is not registered.");
        }

        if (node.Triggers.ContainsKey(triggerKey))
        {
            throw new InvalidOperationException(
                $"State {stateType} already has a handler for trigger '{triggerKey}'.");
        }

        node.Triggers.Add(triggerKey, handler);
    }

    internal IEnumerable<Type> GetTriggerTargetTypes()
    {
        foreach (var node in _nodes.Values)
        {
            foreach (var handler in node.Triggers.Values)
            {
                if (handler.Target is not null) yield return handler.Target;
            }
        }
    }

    internal bool HasState(Type stateType) => _nodes.ContainsKey(stateType);

    internal void Compile()
    {
        foreach (var node in _nodes.Values)
        {
            node.PreInstantiate();
        }
    }

    internal void CompileAgainst<T>()
    {
        if (IsCompiledWithType)
        {
            return;
        }

        foreach (var node in _nodes.Values)
        {
            if (!typeof(T).IsAssignableFrom(node.Type))
            {
                throw new InvalidOperationException();
            }

            node.PreInstantiate();
        }
        _innerParentType = typeof(T);
    }
    
    private void SetCurrentStateByType(Type stateType, object? payload = null)
    {
        var task = SetCurrentStateByTypeAsync(stateType, payload);
        if (task.IsFaulted)
        {
            task.GetAwaiter().GetResult();
        }

        if (task.Status != TaskStatus.RanToCompletion)
        {
            throw new InvalidOperationException(
                "This transition involves async callbacks. Use SetCurrentStateAsync instead.");
        }
    }
}
