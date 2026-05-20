using System;
using System.Collections.Generic;

namespace Statement.Rules;

/// <summary>
/// Holds transition constraints for a state.
/// </summary>
public class TransitionRule
{
    /// <summary>
    /// State types that cannot be transitioned to.
    /// </summary>
    public List<Type> ForbiddenNextStates { get; private set; } = [];

    /// <summary>
    /// State types that may be transitioned to (empty = allow all).
    /// </summary>
    public List<Type> AllowedNextStates { get; private set; } = [];
}