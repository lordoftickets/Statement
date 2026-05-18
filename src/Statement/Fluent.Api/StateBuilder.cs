using System;

namespace Statement.Fluent.Api;

/// <summary>
/// Fluent builder for configuring a single registered state: its entry/exit callbacks and its transition rules.
/// Obtained via the <c>configure</c> callback parameter on <see cref="StateMachineBuilder{TBase}.AddState{TState}(Action{StateBuilder{TState}})"/>.
/// </summary>
/// <typeparam name="TState">The state type being configured.</typeparam>
public sealed class StateBuilder<TState> where TState : class
{
    private readonly StateMachine _machine;

    internal StateBuilder(StateMachine machine)
    {
        _machine = machine;
    }

    /// <summary>
    /// Registers a callback to run when the machine transitions into <typeparamref name="TState"/>.
    /// The callback receives the state instance and the owning machine.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
    public StateBuilder<TState> OnEntry(Action<TState, IStateMachine> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnEntry(typeof(TState), (m, _) => callback((TState)m.GetCurrentState()!, m));
        return this;
    }

    /// <summary>
    /// Registers a parameterless callback to run when the machine transitions into <typeparamref name="TState"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
    public StateBuilder<TState> OnEntry(Action callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnEntry(typeof(TState), (_, _) => callback());
        return this;
    }

    /// <summary>
    /// Registers a callback to run when the machine transitions into <typeparamref name="TState"/> with a typed payload.
    /// The callback receives the state instance and the payload passed to
    /// <see cref="StateMachine.Fire(object, object?)"/> or <see cref="StateMachine.SetCurrentState{T}(object?)"/>.
    /// </summary>
    /// <typeparam name="TPayload">Expected payload type. If the supplied payload is not assignable to <typeparamref name="TPayload"/> the callback is silently skipped; the transition itself still proceeds.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
    public StateBuilder<TState> OnEntryWith<TPayload>(Action<TState, TPayload> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnEntry(typeof(TState), (m, payload) =>
        {
            if (payload is TPayload typed)
            {
                callback((TState)m.GetCurrentState()!, typed);
            }
        });
        return this;
    }

    /// <summary>
    /// Registers a payload-only callback to run when the machine transitions into <typeparamref name="TState"/>.
    /// </summary>
    /// <typeparam name="TPayload">Expected payload type. If the supplied payload is not assignable to <typeparamref name="TPayload"/> the callback is silently skipped; the transition itself still proceeds.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
    public StateBuilder<TState> OnEntryWith<TPayload>(Action<TPayload> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnEntry(typeof(TState), (_, payload) =>
        {
            if (payload is TPayload typed)
            {
                callback(typed);
            }
        });
        return this;
    }

    /// <summary>
    /// Registers a callback to run when the machine transitions out of <typeparamref name="TState"/>.
    /// The callback receives the state instance and the owning machine.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
    public StateBuilder<TState> OnExit(Action<TState, IStateMachine> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnExit(typeof(TState), m => callback((TState)m.GetCurrentState()!, m));
        return this;
    }

    /// <summary>
    /// Registers a parameterless callback to run when the machine transitions out of <typeparamref name="TState"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
    public StateBuilder<TState> OnExit(Action callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        _machine.AddOnExit(typeof(TState), _ => callback());
        return this;
    }

    /// <summary>
    /// Begins configuration of a trigger handler keyed by the type <typeparamref name="TTrigger"/>.
    /// Use <c>Fire(new TTrigger(...))</c> (or any instance of <typeparamref name="TTrigger"/>) to invoke it later.
    /// </summary>
    public TriggerBuilder<TState, TTrigger> On<TTrigger>()
        => new(_machine, typeof(TTrigger));

    /// <summary>
    /// Begins configuration of a trigger handler keyed by a specific value (typically an enum value or string).
    /// Use <c>Fire(triggerKey)</c> with the equal value to invoke it later.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="triggerKey"/> is <c>null</c>.</exception>
    public TriggerBuilder<TState, object> On(object triggerKey)
    {
        if (triggerKey is null) throw new ArgumentNullException(nameof(triggerKey));
        return new TriggerBuilder<TState, object>(_machine, triggerKey);
    }

    /// <summary>
    /// Adds a rule forbidding direct transitions from <typeparamref name="TState"/> to <typeparamref name="TForbidden"/>.
    /// Attempts to switch to the forbidden state while this state is active are silently ignored.
    /// </summary>
    /// <typeparam name="TForbidden">The state type that may not be transitioned to from this state.</typeparam>
    public StateBuilder<TState> CannotTransitionTo<TForbidden>()
    {
        _machine.AddExitRule(typeof(TState), typeof(TForbidden));
        return this;
    }
}
