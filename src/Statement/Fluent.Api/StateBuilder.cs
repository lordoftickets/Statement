using System;

namespace Statement.Fluent.Api;

public sealed class StateBuilder<TState> where TState : class, new()
{
    private readonly StateMachine _machine;

    internal StateBuilder(StateMachine machine)
    {
        _machine = machine;
    }

    public StateBuilder<TState> OnEntry(Action<TState, IStateMachine> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnEntry(typeof(TState), m => callback((TState)m.GetCurrentState()!, m));
        return this;
    }

    public StateBuilder<TState> OnEntry(Action callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnEntry(typeof(TState), _ => callback());
        return this;
    }

    public StateBuilder<TState> OnExit(Action<TState, IStateMachine> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnExit(typeof(TState), m => callback((TState)m.GetCurrentState()!, m));
        return this;
    }

    public StateBuilder<TState> OnExit(Action callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnExit(typeof(TState), _ => callback());
        return this;
    }

    public StateBuilder<TState> CannotTransitionTo<TForbidden>()
    {
        _machine.AddExitRule(typeof(TState), typeof(TForbidden));
        return this;
    }
}
