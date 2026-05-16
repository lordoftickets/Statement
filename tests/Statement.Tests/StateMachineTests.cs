using Builder.Tests.TestStates;
using Builder.Tests.TestStates.Statement;
using NUnit.Framework;
using Statement;
using Statement.Fluent.Api;

namespace Builder.Tests;

public class StateMachineTests
{
    private StateMachine _machine = null!;

    [SetUp]
    public void Setup()
    {
        _machine = MachineBuilder.Create();
        _machine.AddState<SimpleUnitTestState>();
        _machine.AddState<AdvancedUnitTestState>();
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
    public void SetCurrentState_UnregisteredType_LeavesCurrentUnchanged()
    {
        _machine.SetCurrentState<SimpleUnitTestState>();
        _machine.SetCurrentState<UnregisteredState>();

        Assert.That(_machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void SetCurrentState_FirstTransition_DoesNotInvokeOnExit()
    {
        var machine = MachineBuilder.Create();
        machine.AddState<SimpleStatement>();

        machine.SetCurrentState<SimpleStatement>();

        var current = machine.GetCurrentState<SimpleStatement>();
        Assert.That(current.OnEntryCalled, Is.True);
        Assert.That(current.OnExitCalled, Is.False);
    }

    [Test]
    public void SetCurrentState_TransitionInvokesOnExitThenOnEntry()
    {
        var machine = MachineBuilder.Create();
        machine.AddState<SimpleStatement>();
        machine.AddState<AdvancedUnitTestState>();

        machine.SetCurrentState<SimpleStatement>();
        var simple = machine.GetCurrentState<SimpleStatement>();

        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(simple.OnExitCalled, Is.True);
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void SetCurrentState_DuringOnExit_CurrentStateIsStillExitingState()
    {
        var machine = MachineBuilder.Create();
        object? currentDuringExit = null;
        machine.AddState<SimpleUnitTestState>()
            .AddOnExit(m => currentDuringExit = m.GetCurrentState());
        machine.AddState<AdvancedUnitTestState>();

        machine.SetCurrentState<SimpleUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(currentDuringExit, Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void SetCurrentState_DuringOnEntry_CurrentStateIsNewState()
    {
        var machine = MachineBuilder.Create();
        object? currentDuringEntry = null;
        machine.AddState<SimpleUnitTestState>()
            .AddOnEntry(m => currentDuringEntry = m.GetCurrentState());
        machine.AddState<AdvancedUnitTestState>();

        machine.SetCurrentState<SimpleUnitTestState>();

        Assert.That(currentDuringEntry, Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void SetCurrentState_ForbiddenTransition_DoesNotChangeStateOrCallCallbacks()
    {
        var machine = MachineBuilder.Create();
        var onExitCalled = false;
        machine.AddState<SimpleUnitTestState>()
            .AddOnExit(_ => onExitCalled = true)
            .ForbidNextState<AdvancedUnitTestState>();
        machine.AddState<AdvancedUnitTestState>();

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

    private class UnregisteredState
    {
    }
}
