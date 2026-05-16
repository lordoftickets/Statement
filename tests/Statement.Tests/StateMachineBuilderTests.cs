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
