using System;
using Statement.Rules;

namespace Statement;

internal class RegisteredStateBundle(Type registeredState)
{
    internal Type RegisteredState { get; set; } = registeredState;
    internal Action<StateMachine>? OnEntryCallback { get; set; }
    internal Action<StateMachine>? OnExitCallback { get; set; }
    internal TransitionRule? TransitionRule { get; set; }
}