using System;
using System.Collections.Generic;
using Statement.Rules;

namespace Statement;

public class StateMachine : IStateMachine
{
    private Type? _innerParentType;
    private readonly Dictionary<Type, StateNode> _nodes = new();
    private StateNode? _current;

    private readonly RuleMaster _ruleMaster = new();
    private readonly TransitionExecutor _transitionExecutor = new();

    private bool IsCompiledWithType => _innerParentType is not null;

    public void SetCurrentState<T>()
    {
        if (!_nodes.TryGetValue(typeof(T), out var target))
        {
            return;
        }

        if (!_ruleMaster.IsAllowed(_current, target))
        {
            return;
        }

        var transition = new Transition(_current, target);
        _transitionExecutor.Execute(transition, this, () => _current = target);
    }

    public T GetCurrentState<T>() where T : class
    {
        if (_current?.GetOrCreateInstance() is T state)
        {
            return state;
        }
        throw new InvalidOperationException();
    }

    public T? TryGetCurrentState<T>(out bool result) where T : class
    {
        if (_current?.GetOrCreateInstance() is T state)
        {
            result = true;
            return state;
        }

        result = false;
        return null;
    }

    public object? GetCurrentState() => _current?.GetOrCreateInstance();

    internal void RegisterInnerState<TState>() where TState : class, new()
    {
        if (_nodes.ContainsKey(typeof(TState)))
        {
            throw new InvalidOperationException($"State {typeof(TState)} is already registered.");
        }
        _nodes.Add(typeof(TState), new StateNode(typeof(TState)));
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
