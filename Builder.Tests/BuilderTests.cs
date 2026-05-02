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
        machine.AddState<SimpleUnitTestState>();
        machine.AddState<AdvancedUnitTestState>();
        
        machine.SetCurrentState<SimpleUnitTestState>();
        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.TypeOf<SimpleUnitTestState>());
        
        machine.SetCurrentState<AdvancedUnitTestState>();
        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void SimpleHappyPath_Compiled_SameResult()
    {
        var machine = MachineBuilder.Create();
        machine.AddState<SimpleUnitTestState>();
        machine.AddState<AdvancedUnitTestState>();
        
        machine.SetCurrentState<SimpleUnitTestState>();
        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.TypeOf<SimpleUnitTestState>());
        
        machine.SetCurrentState<AdvancedUnitTestState>();
        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.TypeOf<AdvancedUnitTestState>());
    }
    
    [Test]
    public void SimpleHappyPath_TransitionMethodsWereCalled()
    {
        var machine = MachineBuilder.Create();
        machine.AddState<SimpleUnitTestState>()
            .AddOnEntry(SimpleHappyPathCallback)
            .AddOnExit(SimpleHappyPathCallbackOnExit);
        
        machine.AddState<AdvancedUnitTestState>();
        
        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();
        
        Assert.That(_wasCalledOnEntry, Is.True);
        Assert.That(_wasCalledOnExit, Is.True);
    }

    [Test]
    public void TransitionRulePreventsStateTransition()
    {
        var machine = MachineBuilder.Create();
        machine.AddState<SimpleUnitTestState>()
            .AddOnEntry(SimpleHappyPathCallback)
            .AddOnExit(SimpleHappyPathCallbackOnExit)
            .ForbidNextState<AdvancedUnitTestState>();
        
        machine.AddState<AdvancedUnitTestState>();
        
        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.TypeOf<SimpleUnitTestState>());
    }

    private void SimpleHappyPathCallback(StateMachine machine)
    {
        var currentState = machine.GetCurrentState<IUnitTestState>();
        if (currentState is SimpleUnitTestState)
        {
            _wasCalledOnEntry = true;
        }
    }
    
    private void SimpleHappyPathCallbackOnExit(StateMachine machine)
    {
        var currentState = machine.GetCurrentState<IUnitTestState>();
        if (currentState is AdvancedUnitTestState)
        {
            _wasCalledOnExit = true;
        }
    }
}