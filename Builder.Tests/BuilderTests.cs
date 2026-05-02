using Builder.Tests.TestStates;
using Statement;
using Statement.Fluent.Api;

namespace Builder.Tests;

public class BuilderTests
{
    private bool _wasCalledOnEntry;
    private bool _wasCalledOnExit;
    
    [SetUp]
    public void Setup()
    {
        _wasCalledOnEntry = false;
        _wasCalledOnExit = false;
    }

    [Test]
    public void SimpleHappyPath()
    {
        var machine = MachineBuilder.Create();
        machine.AddState<SimpleState>()
            .AddOnEntry(SimpleHappyPathCallback)
            .AddOnExit(SimpleHappyPathCallbackOnExit);
        
        machine.AddState<AdvancedState>();
        
        machine.Compile();
        
        machine.SetCurrentState<SimpleState>();
        machine.SetCurrentState<AdvancedState>();
        
        Assert.That(_wasCalledOnEntry, Is.True);
        Assert.That(_wasCalledOnExit, Is.True);
    }

    [Test]
    public void TransitionRulePreventsStateTransition()
    {
        var machine = MachineBuilder.Create();
        machine.AddState<SimpleState>()
            .AddOnEntry(SimpleHappyPathCallback)
            .AddOnExit(SimpleHappyPathCallbackOnExit)
            .ForbidNextState<AdvancedState>();
        
        machine.AddState<AdvancedState>();
        
        machine.SetCurrentState<SimpleState>();
        machine.SetCurrentState<AdvancedState>();

        Assert.That(machine.GetCurrentState<IState>(), Is.TypeOf<SimpleState>());
    }

    private void SimpleHappyPathCallback(StateMachine machine)
    {
        var currentState = machine.GetCurrentState<IState>();
        if (currentState is SimpleState)
        {
            _wasCalledOnEntry = true;
        }
    }
    
    private void SimpleHappyPathCallbackOnExit(StateMachine machine)
    {
        var currentState = machine.GetCurrentState<IState>();
        if (currentState is AdvancedState)
        {
            _wasCalledOnExit = true;
        }
    }
}