using System;

namespace Statement.Failures;

/// <summary>
/// Thrown by <see cref="TriggerFailurePolicy.Throw"/> when a fired trigger does not produce a transition.
/// </summary>
public class TriggerFailedException : Exception
{
    /// <summary>Details about the failed trigger.</summary>
    public TriggerFailureInfo Info { get; }

    internal TriggerFailedException(TriggerFailureInfo info)
        : base($"Trigger '{info.Trigger}' fired in state {info.CurrentState.Name} failed: {info.Reason}.")
    {
        Info = info;
    }
}
