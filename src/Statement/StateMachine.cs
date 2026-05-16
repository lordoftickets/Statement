using System;
using System.Collections.Generic;
using System.Diagnostics;
using Statement.Failures;
using Statement.Rules;

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
    private readonly List<Action<TransitionInformation>> _transitionCallbacks = [];
    
    internal StateMachine() { }
    internal TransitionFailurePolicy FailurePolicy { get; set; } = TransitionFailurePolicy.Silent;

    private bool IsCompiledWithType => _innerParentType is not null;

    /// <summary>
    /// Transitions the machine to the registered state of type <typeparamref name="T"/>.
    /// The transition is silently ignored if the target state is not registered or is blocked by a transition rule.
    /// Exit callbacks of the previous state and entry callbacks of the new state run as part of the transition.
    /// </summary>
    /// <typeparam name="T">The state type to switch to. Must have been registered on this machine.</typeparam>
    public void SetCurrentState<T>() => SetCurrentStateByType(typeof(T));

    internal void SetCurrentStateByType(Type stateType)
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

        var transition = new Transition(_current, target);
        _transitionExecutor.Execute(transition, this, () => _current = target);
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

    internal void AddTransitionCallbacks(params Action<TransitionInformation>[] callbacks)
    {
        foreach (var callback in callbacks)
        {
            _transitionCallbacks.Add(callback);
        }
    }

    internal void InvokeTransitionCallbacks(TransitionInformation transitionInformation)
    {
        foreach (var transitionCallback in _transitionCallbacks)
        {
            try
            {
                transitionCallback(transitionInformation);
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

    internal void AddOnEntry(Type stateType, Action<StateMachine> callback)
    {
        if (_nodes.TryGetValue(stateType, out var node))
        {
            node.OnEntry = callback;
        }
    }

    internal void AddOnExit(Type stateType, Action<StateMachine> callback)
    {
        if (_nodes.TryGetValue(stateType, out var node))
        {
            node.OnExit = callback;
        }
    }

    internal void AddExitRule(Type stateType, Type forbiddenTarget)
    {
        if (!_nodes.TryGetValue(stateType, out var node))
        {
            return;
        }

        node.TransitionRule ??= new TransitionRule();
        node.TransitionRule.ForbiddenNextStates.Add(forbiddenTarget);
    }

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
