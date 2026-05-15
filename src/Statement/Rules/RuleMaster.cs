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
    /// <param name="currentStateBundle">the current active state</param>
    /// <param name="targetStateBundle">the requested target state</param>
    /// <returns>return true if transition is legal</returns>
    internal bool IsAllowed(RegisteredStateBundle? currentStateBundle, RegisteredStateBundle? targetStateBundle)
    {
        if (currentStateBundle is null || targetStateBundle is null)
        {
            return true;
        }
        
        var rule = currentStateBundle.TransitionRule;
        if (rule is null)
        {
            return true;
        }

        if (!IsAllowedByCurrentState(rule, targetStateBundle.RegisteredState))
        {
            return false;
        }

        /*
         * later we can implement more rules here
         * like an inbound check
         */
        
        return true;
    }

    private bool IsAllowedByCurrentState(TransitionRule rule, Type targetState)
        => !rule.ForbiddenNextStates.Contains(targetState);
}