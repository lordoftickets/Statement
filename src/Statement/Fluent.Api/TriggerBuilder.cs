using System;
using Statement.Triggers;

namespace Statement.Fluent.Api;

/// <summary>
/// Fluent fragment that configures a single (state, trigger) entry. Obtained from
/// <see cref="StateBuilder{TState}.On{TTrigger}"/> or <see cref="StateBuilder{TState}.On(object)"/>.
/// </summary>
public sealed class TriggerBuilder<TState, TTrigger> where TState : class
{
    private readonly StateMachine _machine;
    private readonly object _triggerKey;
    private Func<bool>? _guard;
    private Action<TTrigger>? _onFire;

    internal TriggerBuilder(StateMachine machine, object triggerKey)
    {
        _machine = machine;
        _triggerKey = triggerKey;
    }

    /// <summary>
    /// Attaches a guard predicate. The transition only happens when the predicate returns <c>true</c>.
    /// If the predicate returns <c>false</c>, the configured <see cref="Statement.Failures.TriggerFailurePolicy"/> fires
    /// with <see cref="Statement.Failures.TriggerFailureReason.GuardFailed"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="guard"/> is <c>null</c>.</exception>
    public TriggerBuilder<TState, TTrigger> If(Func<bool> guard)
    {
        if (guard is null) throw new ArgumentNullException(nameof(guard));
        _guard = guard;
        return this;
    }

    /// <summary>
    /// Attaches a side-effect that runs after the guard passes but before the actual transition.
    /// Receives the trigger value so the side-effect can read payload data.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="onFire"/> is <c>null</c>.</exception>
    public TriggerBuilder<TState, TTrigger> Do(Action<TTrigger> onFire)
    {
        if (onFire is null) throw new ArgumentNullException(nameof(onFire));
        _onFire = onFire;
        return this;
    }

    /// <summary>
    /// Registers the trigger as a transition to <typeparamref name="TTarget"/>.
    /// </summary>
    public void GoTo<TTarget>()
    {
        _machine.AddTriggerHandler(typeof(TState), _triggerKey,
            new TriggerHandler(typeof(TTarget), _guard, Wrap(_onFire)));
    }

    /// <summary>
    /// Registers the trigger as an internal transition (no state change, no entry/exit callbacks).
    /// The <see cref="Do"/> side-effect, if any, still runs when the trigger is fired and the guard passes.
    /// </summary>
    public void Ignore()
    {
        _machine.AddTriggerHandler(typeof(TState), _triggerKey,
            new TriggerHandler(null, _guard, Wrap(_onFire)));
    }

    private static Action<object>? Wrap(Action<TTrigger>? typed)
        => typed is null ? null : t => typed((TTrigger)t);
}
