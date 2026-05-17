using System;

namespace Statement.Failures;

/// <summary>
/// Controls how the state machine reacts when a fired trigger does not produce a transition,
/// either because the current state declares no handler for it or because a guard returned <c>false</c>.
/// Pick one of <see cref="Silent"/>, <see cref="Throw"/>, or <see cref="Invoke"/> and pass it to
/// <c>StateMachineBuilder.OnTriggerFailure</c>.
/// </summary>
public abstract class TriggerFailurePolicy
{
    /// <summary>Default policy: failed triggers are silently ignored.</summary>
    public static TriggerFailurePolicy Silent { get; } = new SilentPolicy();

    /// <summary>Failed triggers raise a <see cref="TriggerFailedException"/>.</summary>
    public static TriggerFailurePolicy Throw { get; } = new ThrowPolicy();

    /// <summary>Failed triggers invoke the supplied callback with details of the attempt.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
    public static TriggerFailurePolicy Invoke(Action<TriggerFailureInfo> callback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        return new CallbackPolicy(callback);
    }

    internal abstract void Handle(TriggerFailureInfo info);

    private sealed class SilentPolicy : TriggerFailurePolicy
    {
        internal override void Handle(TriggerFailureInfo info) { }
    }

    private sealed class ThrowPolicy : TriggerFailurePolicy
    {
        internal override void Handle(TriggerFailureInfo info)
            => throw new TriggerFailedException(info);
    }

    private sealed class CallbackPolicy : TriggerFailurePolicy
    {
        private readonly Action<TriggerFailureInfo> _callback;
        internal CallbackPolicy(Action<TriggerFailureInfo> callback) => _callback = callback;
        internal override void Handle(TriggerFailureInfo info) => _callback(info);
    }
}
