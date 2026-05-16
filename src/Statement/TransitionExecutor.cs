using System;
using Statement.State;

namespace Statement;

internal class TransitionExecutor
{
    internal void Execute(Transition transition, StateMachine machine, Action commit)
    {
        if (transition.From is not null)
        {
            (transition.FromInstance as IStatement)?.OnExit();
            transition.From.OnExit?.Invoke(machine);
        }

        commit();

        (transition.ToInstance as IStatement)?.OnEntry();
        transition.To.OnEntry?.Invoke(machine);
    }
}
