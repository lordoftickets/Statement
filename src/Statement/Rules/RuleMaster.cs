using System;

namespace Statement.Rules;

/// <summary>
/// implements rule checks to be used in the <see cref="StateMachine"/>
/// </summary>
internal class RuleMaster
{
    /// <summary>
    /// evaluate if the transition is allowed
    /// </summary>
    /// <param name="current">the current active state node</param>
    /// <param name="target">the requested target state node</param>
    /// <returns>return true if transition is legal</returns>
    internal bool IsAllowed(StateNode? current, StateNode? target)
    {
        if (current is null || target is null)
        {
            return true;
        }

        var rule = current.TransitionRule;
        if (rule is null)
        {
            return true;
        }
        
        if (!IsAllowedByCurrentState(rule, target.Type))
        {
            return false;
        }

        if (!IsLegalTarget(rule, target.Type))
        {
            return false;
        }

        /*
         * later we can implement more rules here
         * like an inbound check
         */

        return true;
    }

    private static bool IsAllowedByCurrentState(TransitionRule rule, Type targetState)
        => !rule.ForbiddenNextStates.Contains(targetState);

    private static bool IsLegalTarget(TransitionRule rule, Type targetState)
        => rule.AllowedNextStates.Count == 0 || rule.AllowedNextStates.Contains(targetState);
}
