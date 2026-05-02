using System;
using System.Collections.Generic;

namespace Statement.Rules;

public class TransitionRule
{
    public List<Type> ForbiddenNextStates { get; private set; } = [];
}