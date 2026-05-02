using System;

namespace Statement.Fluent.Api;

public static class MachineBuilder
{
    public static StateMachine Create() => new();
    
    extension(StateMachine machine)
    {
        public StateDecorator AddState<TState>() where TState : class, new()
        {
            machine.RegisterInnerState<TState>();
            return new StateDecorator(typeof(TState), machine);
        }

        public void Compile() => machine.Compile();
    }


    extension(StateDecorator decorator)
    {
        public StateDecorator AddOnEntry(Action<StateMachine> callback)
        {
            var machine = decorator.Machine;
            machine.AddOnEntry(decorator.InnerStateType, callback);
            return decorator;
        }

        public StateDecorator AddOnExit(Action<StateMachine> callback)
        {
            var machine = decorator.Machine;
            machine.AddOnExit(decorator.InnerStateType, callback);
            return decorator;
        }

        public StateDecorator ForbidNextState<T>()
        {
            var machine = decorator.Machine;
            machine.AddExitRule(decorator.InnerStateType, typeof(T));
            return decorator;
        }
    }

}