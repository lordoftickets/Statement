using Builder.Tests.TestStates;
using Builder.Tests.TestStates.Statement;
using Statement.Fluent.Api;

namespace Builder.Tests;

[TestFixture]
public class StateMachineBuilderTests
{
    [Test]
    public void Build_WithoutBaseType_ProducesUsableMachine()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
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
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
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
    public void OnEntry_TypedCallback_ReceivesStateInstance()
    {
        SimpleUnitTestState? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnEntry((state, _) => captured = state))
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured, Is.SameAs(machine.GetCurrentState()));
    }

    [Test]
    public void OnExit_TypedCallback_FiresOnTransition()
    {
        var exited = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExit((_, _) => exited = true))
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(exited, Is.True);
    }

    [Test]
    public void AddState_WithInstance_UsesProvidedInstance()
    {
        var preBuilt = new SimpleUnitTestState();
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(preBuilt)
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.SameAs(preBuilt));
    }

    [Test]
    public void AddState_WithInstance_AllowsNonParameterlessConstructor()
    {
        var preBuilt = new StateWithCtorArgs("hello");
        var machine = StateMachineBuilder.New()
            .AddState<StateWithCtorArgs>(preBuilt)
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
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
        Assert.That(machine.GetCurrentState<IUnitTestState>(), Is.SameAs(preBuilt));
    }

    [Test]
    public void CannotTransitionTo_BlocksTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.CannotTransitionTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }
}
