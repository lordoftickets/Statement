using System;
using System.Threading.Tasks;
using Statement.Failures;

namespace Statement.Fluent.Api;

/// <summary>
/// Entry point for the fluent API used to configure and build <see cref="StateMachine"/> instances.
/// </summary>
public static class StateMachineBuilder
{
    /// <summary>
    /// Creates a new untyped builder. States added to it have no common base-type constraint
    /// and the resulting machine is a plain <see cref="StateMachine"/>.
    /// </summary>
    public static StateMachineBuilder<object> New() => new(requireBaseType: false);

    /// <summary>
    /// Creates a new builder that constrains every registered state to derive from or implement <typeparamref name="TBase"/>.
    /// The resulting machine can be produced as a strongly-typed <see cref="StateMachine{TBase}"/> via <see cref="StateMachineBuilder{TBase}.BuildTyped"/>.
    /// </summary>
    /// <typeparam name="TBase">The common base type or interface shared by every state.</typeparam>
    public static StateMachineBuilder<TBase> For<TBase>() where TBase : class
        => new(requireBaseType: true);
}

/// <summary>
/// Fluent builder that registers states, configures their behavior, and produces a configured <see cref="StateMachine"/>.
/// </summary>
/// <typeparam name="TBase">
/// The common base type for registered states. When created via <see cref="StateMachineBuilder.New"/> this is <see cref="object"/>
/// and the constraint is not enforced; when created via <see cref="StateMachineBuilder.For{TBase}"/> every state must derive from <typeparamref name="TBase"/>.
/// </typeparam>
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

    /// <summary>
    /// Registers <typeparamref name="TState"/> as a state. The instance is created lazily on first access using its public parameterless constructor.
    /// </summary>
    /// <typeparam name="TState">The state type to register. Must have a public parameterless constructor.</typeparam>
    public StateMachineBuilder<TBase> AddState<TState>() where TState : class, TBase, new()
        => AddState<TState>(_ => { });

    /// <summary>
    /// Registers <typeparamref name="TState"/> as a state and invokes <paramref name="configure"/> to attach callbacks and rules to it.
    /// The instance is created lazily on first access using its public parameterless constructor.
    /// </summary>
    /// <param name="configure">Callback that configures the state via a <see cref="StateBuilder{TState}"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public StateMachineBuilder<TBase> AddState<TState>(Action<StateBuilder<TState>> configure)
        where TState : class, TBase, new()
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        _machine.RegisterInnerState<TState>();
        configure(new StateBuilder<TState>(_machine));
        return this;
    }

    /// <summary>
    /// Registers a pre-instantiated state object. The machine will use this exact instance rather than constructing its own.
    /// </summary>
    /// <param name="instance">The state instance to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <c>null</c>.</exception>
    public StateMachineBuilder<TBase> AddState<TState>(TState instance)
        where TState : class, TBase
        => AddState(instance, _ => { });

    /// <summary>
    /// Registers a callback that will be invoked whenever the machine transitions to a new state.
    /// The callback gets invoked before the explicit state callbacks for the new state are invoked.
    /// </summary>
    /// <param name="callback">the method to call</param>
    public StateMachineBuilder<TBase> AddOnStateChangedCallback(Action<TransitionInformation> callback)
    {
        _machine.AddTransitionCallbacks(info => { callback(info); return Task.CompletedTask; });
        return this;
    }

    /// <summary>
    /// Registers an async callback that will be invoked whenever the machine transitions to a new state.
    /// </summary>
    /// <param name="callback">the async method to call</param>
    public StateMachineBuilder<TBase> AddOnStateChangedCallbackAsync(Func<TransitionInformation, Task> callback)
    {
        _machine.AddTransitionCallbacks(callback);
        return this;
    }

    /// <summary>
    /// Registers a pre-instantiated state object and invokes <paramref name="configure"/> to attach callbacks and rules to it.
    /// </summary>
    /// <param name="instance">The state instance to register.</param>
    /// <param name="configure">Callback that configures the state via a <see cref="StateBuilder{TState}"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="configure"/> is <c>null</c>.</exception>
    public StateMachineBuilder<TBase> AddState<TState>(TState instance, Action<StateBuilder<TState>> configure)
        where TState : class, TBase
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        _machine.RegisterInnerState(instance);
        configure(new StateBuilder<TState>(_machine));
        return this;
    }

    /// <summary>
    /// Sets the policy used when a transition is blocked by a configured rule.
    /// Defaults to <see cref="TransitionFailurePolicy.Silent"/>.
    /// Attempts to switch to an unregistered state always throw, regardless of this policy.
    /// </summary>
    /// <param name="policy">The policy to apply. Use <see cref="TransitionFailurePolicy.Silent"/>,
    /// <see cref="TransitionFailurePolicy.Throw"/>, or <see cref="TransitionFailurePolicy.Invoke"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> is <c>null</c>.</exception>
    public StateMachineBuilder<TBase> OnTransitionFailure(TransitionFailurePolicy policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _machine.FailurePolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the policy used when <c>Fire(trigger)</c> finds no handler for the trigger on the current state,
    /// or when the matching handler's guard returns <c>false</c>.
    /// Defaults to <see cref="TriggerFailurePolicy.Silent"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> is <c>null</c>.</exception>
    public StateMachineBuilder<TBase> OnTriggerFailure(TriggerFailurePolicy policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _machine.TriggerFailurePolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the state the machine will be in after <see cref="Build"/> completes.
    /// The entry callback for <typeparamref name="TState"/> runs as part of building.
    /// </summary>
    /// <typeparam name="TState">The state type to start in. Must also be registered via <c>AddState</c>.</typeparam>
    public StateMachineBuilder<TBase> StartIn<TState>() where TState : class, TBase
    {
        _initialState = typeof(TState);
        return this;
    }

    /// <summary>
    /// Finalizes the configuration, pre-instantiates registered states as needed, applies the initial state if one was configured,
    /// and returns the built <see cref="StateMachine"/>.
    /// </summary>
    public StateMachine Build()
    {
        var task = BuildAsync();
        if (task.IsFaulted)
        {
            task.GetAwaiter().GetResult();
        }

        if (task.Status != TaskStatus.RanToCompletion)
        {
            throw new InvalidOperationException(
                "This machine has async callbacks on the initial state. Use BuildAsync instead.");
        }

        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously finalizes the configuration, pre-instantiates registered states as needed, applies the initial state,
    /// and returns the built <see cref="StateMachine"/>. Use this when the initial state or global callbacks are async.
    /// </summary>
    public async Task<StateMachine> BuildAsync()
    {
        if (_requireBaseType && typeof(TBase) != typeof(object))
        {
            _machine.CompileAgainst<TBase>();
        }
        else
        {
            _machine.Compile();
        }

        foreach (var triggerTarget in _machine.GetTriggerTargetTypes())
        {
            if (!_machine.HasState(triggerTarget))
            {
                throw new MachineSetupException(
                    $"Trigger target state {triggerTarget} is not registered on this machine.");
            }
        }

        if (_initialState is not null)
        {
            await _machine.SetCurrentStateByTypeAsync(_initialState);
        }
        else
        {
            throw new MachineSetupException("Missing initial state. Use StartIn<TState>() to set the initial state.");
        }

        return _machine;
    }

    /// <summary>
    /// Finalizes the configuration and returns a strongly-typed <see cref="StateMachine{TBase}"/>.
    /// Only valid for builders created via <see cref="StateMachineBuilder.For{TBase}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called on a builder created via <see cref="StateMachineBuilder.New"/>.</exception>
    public StateMachine<TBase> BuildTyped()
    {
        if (!_requireBaseType || typeof(TBase) == typeof(object))
        {
            throw new InvalidOperationException(
                "BuildTyped requires a builder created via StateMachineBuilder.For<TBase>().");
        }

        return (StateMachine<TBase>)Build();
    }

    /// <summary>
    /// Asynchronously finalizes the configuration and returns a strongly-typed <see cref="StateMachine{TBase}"/>.
    /// Only valid for builders created via <see cref="StateMachineBuilder.For{TBase}"/>.
    /// </summary>
    public async Task<StateMachine<TBase>> BuildTypedAsync()
    {
        if (!_requireBaseType || typeof(TBase) == typeof(object))
        {
            throw new InvalidOperationException(
                "BuildTypedAsync requires a builder created via StateMachineBuilder.For<TBase>().");
        }

        return (StateMachine<TBase>)await BuildAsync();
    }
}
