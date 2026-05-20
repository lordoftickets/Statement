using Statement.Failures;
using Statement.Tests.TestStates;
using Statement.Tests.TestStates.Statement;
using Statement.Fluent.Api;

namespace Statement.Tests;

[TestFixture]
public class StateMachineBuilderTests
{
    [Test]
    public void Build_WithoutBaseType_ProducesUsableMachine()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());

        machine.SetCurrentState<AdvancedUnitTestState>();
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void For_ConstrainsAddState_AtCompileTime_AndAtRuntime()
    {
        var machine = StateMachineBuilder.For<IUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void StartIn_EntersInitialStateAndFiresOnEntry()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleStatement>()
            .StartIn<SimpleStatement>()
            .Build();

        var state = machine.GetCurrentState<SimpleStatement>();
        Assert.That(state.OnEntryCalled, Is.True);
    }

    [Test]
    public void AddState_WithInstance_UsesProvidedInstance()
    {
        var preBuilt = new SimpleUnitTestState();
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(preBuilt)
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.That(machine.GetCurrentState(), Is.SameAs(preBuilt));
    }

    [Test]
    public void AddState_WithInstance_AllowsNonParameterlessConstructor()
    {
        var preBuilt = new StateWithCtorArgs("hello");
        var machine = StateMachineBuilder.New()
            .AddState<StateWithCtorArgs>(preBuilt)
            .StartIn<StateWithCtorArgs>()
            .Build();

        machine.SetCurrentState<StateWithCtorArgs>();

        Assert.That(machine.GetCurrentState<StateWithCtorArgs>().Value, Is.EqualTo("hello"));
    }

    [Test]
    public void AddState_WithInstance_SupportsConfigure()
    {
        StateWithCtorArgs? captured = null;
        var preBuilt = new StateWithCtorArgs("x");

        var machine = StateMachineBuilder.New()
            .StartIn<StateWithCtorArgs>()
            .AddState<StateWithCtorArgs>(preBuilt, s => s.OnEntry((state, _) => captured = state))
            .Build();

        machine.SetCurrentState<StateWithCtorArgs>();

        Assert.That(captured, Is.SameAs(preBuilt));
    }

    [Test]
    public void AddState_WithInstance_MixedWithParameterlessStates_Works()
    {
        var preBuilt = new StateWithCtorArgs("mixed");
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<StateWithCtorArgs>(preBuilt)
            .StartIn<StateWithCtorArgs>()
            .Build();

        Assert.That(machine.GetCurrentState(), Is.SameAs(preBuilt));

        machine.SetCurrentState<SimpleUnitTestState>();
        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void AddState_WithNullInstance_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StateMachineBuilder.New().AddState<SimpleUnitTestState>((SimpleUnitTestState)null!));
    }

    [Test]
    public void For_AddStateWithInstance_RespectsBaseTypeConstraint()
    {
        var preBuilt = new SimpleUnitTestState();
        var machine = StateMachineBuilder.For<IUnitTestState>()
            .AddState<SimpleUnitTestState>(preBuilt)
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.SameAs(preBuilt));
    }
    
    [Test]
    public void CanTransition_LegalNextTargetIsSet()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(state => state.CanTransitionTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();
        
        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void CannotTransition_LegalNextTargetIsWrong()
    {
        var machine = StateMachineBuilder.New()
            .OnTransitionFailure(TransitionFailurePolicy.Throw)
            .AddState<InitialUnitTestState>(state => state.CanTransitionTo<SimpleUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<InitialUnitTestState>()
            .Build();
        
        Assert.Throws<TransitionFailedException>(() => machine.SetCurrentState<AdvancedUnitTestState>());
    }
    
    [Test]
    public void CanTransition_MultipleLegalNextTargets()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(state => state
                .CanTransitionTo<AdvancedUnitTestState>()
                .CanTransitionTo<SimpleUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();
        
        machine.SetCurrentState<AdvancedUnitTestState>();
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
        
        machine.SetCurrentState<SimpleUnitTestState>();
        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());       
    }
}
