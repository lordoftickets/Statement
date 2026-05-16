using Builder.Tests.TestStates;
using Builder.Tests.TestStates.Statement;
using Statement;
using Statement.Fluent.Api;

namespace Builder.Tests;

public class StateMachineTests
{
    private StateMachine _machine = null!;

    [SetUp]
    public void Setup()
    {
        _machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .Build();
    }

    [Test]
    public void SetCurrentState_FromInitial_SetsState()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();

        Assert.That(_machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void SetCurrentState_BetweenStates_SwitchesState()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();
        _machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(_machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void SetCurrentState_UnregisteredType_Throws()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();

        Assert.Throws<InvalidOperationException>(() => _machine.SetCurrentState<UnregisteredState>());
        Assert.That(_machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void SetCurrentState_FirstTransition_DoesNotInvokeOnExit()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleStatement>()
            .Build();

        machine.SetCurrentState<SimpleStatement>();

        var current = machine.GetCurrentState<SimpleStatement>();
        Assert.That(current.OnEntryCalled, Is.True);
        Assert.That(current.OnExitCalled, Is.False);
    }

    [Test]
    public void SetCurrentState_TransitionInvokesOnExitThenOnEntry()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleStatement>()
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleStatement>();
        var simple = machine.GetCurrentState<SimpleStatement>();

        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(simple.OnExitCalled, Is.True);
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void SetCurrentState_DuringOnExit_CurrentStateIsStillExitingState()
    {
        object? currentDuringExit = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExit((_, m) => currentDuringExit = m.GetCurrentState()))
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(currentDuringExit, Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void SetCurrentState_DuringOnEntry_CurrentStateIsNewState()
    {
        object? currentDuringEntry = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnEntry((_, m) => currentDuringEntry = m.GetCurrentState()))
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();

        Assert.That(currentDuringEntry, Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void SetCurrentState_ForbiddenTransition_DoesNotChangeStateOrCallCallbacks()
    {
        var onExitCalled = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s
                .OnExit(() => onExitCalled = true)
                .CannotTransitionTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
        Assert.That(onExitCalled, Is.False);
    }

    [Test]
    public void GetCurrentState_BeforeAnyTransition_ReturnsNull()
    {
        Assert.That(_machine.GetCurrentState(), Is.Null);
    }

    [Test]
    public void GetCurrentStateGeneric_WhenMatchingType_ReturnsInstance()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();

        var state = _machine.GetCurrentState<IUnitTestState>();

        Assert.That(state, Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void GetCurrentStateGeneric_WhenNoMatchingType_Throws()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();

        Assert.Throws<InvalidOperationException>(() => _machine.GetCurrentState<AdvancedUnitTestState>());
    }

    [Test]
    public void GetCurrentStateGeneric_WhenNoCurrentState_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _machine.GetCurrentState<IUnitTestState>());
    }

    [Test]
    public void TryGetCurrentState_WhenMatching_ReturnsTrueAndInstance()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();

        var success = _machine.TryGetCurrentState<IUnitTestState>(out var state);

        Assert.That(success, Is.True);
        Assert.That(state, Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void TryGetCurrentState_WhenNoMatch_ReturnsFalseAndNull()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();

        var success = _machine.TryGetCurrentState<AdvancedUnitTestState>(out var state);

        Assert.That(success, Is.False);
        Assert.That(state, Is.Null);
    }

    [Test]
    public void TryGetCurrentState_WhenNoCurrentState_ReturnsFalseAndNull()
    {
        var success = _machine.TryGetCurrentState<IUnitTestState>(out var state);

        Assert.That(success, Is.False);
        Assert.That(state, Is.Null);
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
    public void GlobalOnTransitionMethod_GetsCalledOnEntry()
    {
        var onEntryCalled = false;

        var machine = StateMachineBuilder.New()
            .AddOnStateChangedCallback(_ => onEntryCalled = true)
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();

        Assert.That(onEntryCalled, Is.True);
    }

    [Test]
    public void GlobalOnTransitionMethod_GetsCalledMultipleTimes()
    {
        var callCount = 0;

        var machine = StateMachineBuilder.New()
            .AddOnStateChangedCallback(_ => callCount++)
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public void GlobalTransitionMethod_CalledBefore_StateCallback()
    {
        DateTime globalTransitionTimeStamp = default;
        DateTime stateTransitionTimeStamp = default;

        var machine = StateMachineBuilder.New()
            .AddOnStateChangedCallback(_ => globalTransitionTimeStamp = DateTime.Now)
            .AddState<SimpleUnitTestState>(state => state.OnEntry(() => stateTransitionTimeStamp = DateTime.Now))
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();

        Assert.That(globalTransitionTimeStamp < stateTransitionTimeStamp, Is.True);
    }

    [Test]
    public void GlobalTransitionMethod_TransitionInfo_ContainsCorrectState()
    {
        var machine = StateMachineBuilder.New()
            .AddOnStateChangedCallback(info => Assert.That(info.To, Is.TypeOf<SimpleUnitTestState>()))
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .Build();

        machine.SetCurrentState<SimpleUnitTestState>();
    }

    private class UnregisteredState
    {
    }
}
