using System;

namespace Statement.Failures;

/// <summary>
/// Describes a fired trigger that did not produce a transition.
/// </summary>
public sealed class TriggerFailureInfo
{
    /// <summary>The state the machine was in when the trigger was fired.</summary>
    public Type CurrentState { get; }

    /// <summary>The trigger value that was fired.</summary>
    public object Trigger { get; }

    /// <summary>Why the trigger did not produce a transition.</summary>
    public TriggerFailureReason Reason { get; }

    internal TriggerFailureInfo(Type currentState, object trigger, TriggerFailureReason reason)
    {
        CurrentState = currentState;
        Trigger = trigger;
        Reason = reason;
    }
}
