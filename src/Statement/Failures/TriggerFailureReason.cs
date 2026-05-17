namespace Statement.Failures;

/// <summary>
/// Reason a fired trigger could not produce a transition.
/// </summary>
public enum TriggerFailureReason
{
    /// <summary>The current state has no handler for the fired trigger.</summary>
    NoHandler,

    /// <summary>A handler exists, but its guard predicate returned <c>false</c>.</summary>
    GuardFailed
}
