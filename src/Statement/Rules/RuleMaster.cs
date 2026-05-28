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
    /// <param name="targetNode">the requested target state node</param>
    /// <returns>return true if transition is legal</returns>
    internal bool IsAllowedTransition(StateNode? current, StateNode? targetNode)
    {
        if (current is null || targetNode is null)
        {
            return true;
        }

        return CheckIfTypeIsValidNextState(current, targetNode);
    }

    /// <summary>
    /// Validates whether a transition to the target state node is allowed from the current state.
    /// </summary>
    /// <param name="current">the current state node whose transition rules are evaluated</param>
    /// <param name="target">the target state node being transitioned to</param>
    /// <returns>true if the transition to the target is permitted by the current state's rules and the target is active; otherwise false</returns>
    internal bool CheckIfTypeIsValidNextState(StateNode? current, StateNode target)
    {
        if (current is null)
        {
            throw new InvalidOperationException(
                "Current state is null. Do not call this method before building the state machine.");
        }

        if (!target.IsActive)
        {
            return false;
        }

        if (current.TransitionRule is null)
        {
            return true;
        }

        if (!IsAllowedByCurrentState(current.TransitionRule, target.Type))
        {
            return false;
        }

        if (!IsLegalTarget(current.TransitionRule, target.Type))
        {
            return false;
        }

        return true;
    }

    private static bool IsAllowedByCurrentState(TransitionRule rule, Type targetState)
        => !rule.ForbiddenNextStates.Contains(targetState);

    private static bool IsLegalTarget(TransitionRule rule, Type targetState)
        => rule.AllowedNextStates.Count == 0 || rule.AllowedNextStates.Contains(targetState);
}
