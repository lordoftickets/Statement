using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Statement.Failures;
using Statement.Rules;
using Statement.Triggers;

namespace Statement;

/// <summary>
/// A state machine that holds a set of registered state types and transitions between them.
/// Instances are created and configured through <see cref="Statement.Fluent.Api.StateMachineBuilder"/>.
/// Transitions are gated by configured rules and surrounded by registered entry/exit callbacks.
/// </summary>
public class StateMachine : IStateMachine
{
    private Type? _innerParentType;
    private readonly Dictionary<Type, StateNode> _nodes = new();
    private StateNode? _current;
    private readonly RuleMaster _ruleMaster = new();
    private readonly TransitionExecutor _transitionExecutor = new();
    private readonly List<Func<TransitionInformation, Task>> _transitionCallbacks = [];

    internal StateMachine() { }
    internal TransitionFailurePolicy FailurePolicy { get; set; } = TransitionFailurePolicy.Silent;
    internal TriggerFailurePolicy TriggerFailurePolicy { get; set; } = TriggerFailurePolicy.Silent;

    private bool IsCompiledWithType => _innerParentType is not null;

    /// <summary>
    /// Transitions the machine to the registered state of type <typeparamref name="T"/>.
    /// The transition is silently ignored if the target state is not registered or is blocked by a transition rule.
    /// Exit callbacks of the previous state and entry callbacks of the new state run as part of the transition.
    /// </summary>
    /// <typeparam name="T">The state type to switch to. Must have been registered on this machine.</typeparam>
    public void SetCurrentState<T>() => SetCurrentStateByType(typeof(T), null);

    /// <summary>
    /// Transitions to <typeparamref name="T"/> and carries a typed <paramref name="payload"/> through to
    /// the target state's <see cref="Statement.Fluent.Api.StateBuilder{TState}.OnEntryWith{TPayload}"/> callback
    /// and to any global transition callbacks via <see cref="TransitionInformation.Payload"/>.
    /// </summary>
    public void SetCurrentState<T>(object? payload) => SetCurrentStateByType(typeof(T), payload);

    /// <summary>
    /// Asynchronously transitions the machine to the registered state of type <typeparamref name="T"/>.
    /// </summary>
    public Task SetCurrentStateAsync<T>() => SetCurrentStateByTypeAsync(typeof(T), null);

    /// <summary>
    /// Asynchronously transitions to <typeparamref name="T"/> with a typed <paramref name="payload"/>.
    /// </summary>
    public Task SetCurrentStateAsync<T>(object? payload) => SetCurrentStateByTypeAsync(typeof(T), payload);

    internal void SetCurrentStateByType(Type stateType, object? payload = null)
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

    internal async Task SetCurrentStateByTypeAsync(Type stateType, object? payload = null)
    {
        if (!_nodes.TryGetValue(stateType, out var target))
        {
            throw new InvalidOperationException($"State {stateType} is not registered.");
        }

        if (!_ruleMaster.IsAllowed(_current, target))
        {
            FailurePolicy.Handle(new TransitionFailureInfo(_current?.Type, stateType));
            return;
        }

        var transition = new Transition(_current, target, payload: payload);
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
        var task = FireAsync(trigger, payload);
        if (task.IsFaulted)
        {
            task.GetAwaiter().GetResult();
        }

        if (task.Status != TaskStatus.RanToCompletion)
        {
            throw new InvalidOperationException(
                "This transition involves async callbacks. Use FireAsync instead.");
        }
    }

    /// <summary>
    /// Asynchronously fires a trigger on the machine.
    /// </summary>
    public Task FireAsync(object trigger) => FireAsync(trigger, null);

    /// <summary>
    /// Asynchronously fires a trigger with a typed <paramref name="payload"/>.
    /// </summary>
    public async Task FireAsync(object trigger, object? payload)
    {
        if (trigger is null) throw new ArgumentNullException(nameof(trigger));
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

        if (!_ruleMaster.IsAllowed(_current, target))
        {
            FailurePolicy.Handle(new TransitionFailureInfo(_current.Type, handler.Target));
            return;
        }

        var transition = new Transition(_current, target, trigger, payload);
        await _transitionExecutor.ExecuteAsync(transition, this, () => _current = target);
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

    internal void AddTransitionCallbacks(params Func<TransitionInformation, Task>[] callbacks)
    {
        foreach (var callback in callbacks)
        {
            _transitionCallbacks.Add(callback);
        }
    }

    internal async Task InvokeTransitionCallbacksAsync(TransitionInformation transitionInformation)
    {
        foreach (var transitionCallback in _transitionCallbacks)
        {
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

    internal void AddOnEntry(Type stateType, Func<StateMachine, object?, Task> callback)
    {
        if (_nodes.TryGetValue(stateType, out var node))
        {
            node.OnEntry = callback;
        }
    }

    internal void AddOnExit(Type stateType, Func<StateMachine, object?, Task> callback)
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
}
