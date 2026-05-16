using System;

namespace Statement.Fluent.Api;

public static class StateMachineBuilder
{
    public static StateMachineBuilder<object> New() => new(requireBaseType: false);

    public static StateMachineBuilder<TBase> For<TBase>() where TBase : class
        => new(requireBaseType: true);
}

public sealed class StateMachineBuilder<TBase> where TBase : class
{
    private readonly StateMachine _machine;
    private readonly bool _requireBaseType;
    private Type? _initialState;

    internal StateMachineBuilder(bool requireBaseType)
    {
        _requireBaseType = requireBaseType;
        _machine = requireBaseType && typeof(TBase) != typeof(object)
            ? new StateMachine<TBase>()
            : new StateMachine();
    }

    public StateMachineBuilder<TBase> AddState<TState>() where TState : class, TBase, new()
        => AddState<TState>(_ => { });

    public StateMachineBuilder<TBase> AddState<TState>(Action<StateBuilder<TState>> configure)
        where TState : class, TBase, new()
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        _machine.RegisterInnerState<TState>();
        configure(new StateBuilder<TState>(_machine));
        return this;
    }

    public StateMachineBuilder<TBase> AddState<TState>(TState instance)
        where TState : class, TBase
        => AddState(instance, _ => { });

    public StateMachineBuilder<TBase> AddState<TState>(TState instance, Action<StateBuilder<TState>> configure)
        where TState : class, TBase
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        _machine.RegisterInnerState(instance);
        configure(new StateBuilder<TState>(_machine));
        return this;
    }

    public StateMachineBuilder<TBase> StartIn<TState>() where TState : class, TBase
    {
        _initialState = typeof(TState);
        return this;
    }

    public StateMachine Build()
    {
        if (_requireBaseType && typeof(TBase) != typeof(object))
        {
            _machine.CompileAgainst<TBase>();
        }
        else
        {
            _machine.Compile();
        }

        if (_initialState is not null)
        {
            _machine.SetCurrentStateByType(_initialState);
        }

        return _machine;
    }

    public StateMachine<TBase> BuildTyped()
    {
        if (!_requireBaseType || typeof(TBase) == typeof(object))
        {
            throw new InvalidOperationException(
                "BuildTyped requires a builder created via StateMachineBuilder.For<TBase>().");
        }

        return (StateMachine<TBase>)Build();
    }
}
