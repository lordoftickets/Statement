using System;

namespace Statement.Fluent.Api;

public class StateDecorator(Type innerStateType, StateMachine machine)
{
    public StateMachine Machine { get; set; } = machine;
    public Type InnerStateType { get; set; } = innerStateType;
}