using System;
using System.Threading.Tasks;
using Statement.State;

namespace Statement;

internal class TransitionExecutor
{
    internal async Task ExecuteAsync(Transition transition, StateMachine machine, Action commit)
    {
        if (transition.From is not null)
        {
            //on interface callbacks
            switch (transition.FromInstance)
            {
                case IStatement statement:
                    statement.OnExit();
                    break;
                case IAsyncStatement asyncFrom:
                    await asyncFrom.OnExitAsync();
                    break;
            }

            //provided callbacks through api
            if (transition.From.OnExit is not null)
            {
                await transition.From.OnExit(machine, transition.Payload);
            }
        }

        commit();
        await machine.InvokeTransitionCallbacksAsync(new TransitionInformation(
            transition.FromInstance,
            transition.ToInstance,
            transition.From?.Type,
            transition.To.Type,
            transition.Trigger,
            transition.Payload));

        switch (transition.ToInstance)
        {
            case IStatement toStatement:
                toStatement.OnEntry();
                break;
            case IAsyncStatement asyncToStatement:
                await asyncToStatement.OnEntryAsync();
                break;
        }

        if (transition.To.OnEntry is not null)
        {
            await transition.To.OnEntry(machine, transition.Payload);
        }
    }
}
