using Statement.Fluent.Api;
using Statement.Tests.TestStates;

namespace Statement.Tests;

[TestFixture]
public class StateActivationTests
{
    public sealed record Go;

    [Test]
    public void DeactivateState_BlocksTransitionToThatState()
    {
        var machine = StateMachineBuilder.For<IUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .BuildTyped();

        machine.DeactivateState<AdvancedUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void ActivateState_RestoresTransitionToThatState()
    {
        var machine = StateMachineBuilder.For<IUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .BuildTyped();

        machine.DeactivateState<AdvancedUnitTestState>();
        machine.ActivateState<AdvancedUnitTestState>();
        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void StatesAreActiveByDefault()
    {
        var machine = StateMachineBuilder.For<IUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .BuildTyped();

        machine.SetCurrentState<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void CanTransitionTo_ReturnsFalse_WhenTargetIsDeactivated()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.DeactivateState<AdvancedUnitTestState>();

        Assert.That(machine.CanTransitionTo(typeof(AdvancedUnitTestState)), Is.False);
    }

    [Test]
    public void CanTransitionTo_ReturnsTrue_AfterReactivation()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.DeactivateState<AdvancedUnitTestState>();
        machine.ActivateState<AdvancedUnitTestState>();

        Assert.That(machine.CanTransitionTo(typeof(AdvancedUnitTestState)), Is.True);
    }

    [Test]
    public void CanTransitionTo_ReturnsFalse_ForUnregisteredType()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.That(machine.CanTransitionTo(typeof(AdvancedUnitTestState)), Is.False);
    }

    [Test]
    public void DeactivateState_UnregisteredType_DoesNotThrow()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.DoesNotThrow(() => machine.DeactivateState<AdvancedUnitTestState>());
    }

    [Test]
    public void ActivateState_UnregisteredType_DoesNotThrow()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.DoesNotThrow(() => machine.ActivateState<AdvancedUnitTestState>());
    }

    [Test]
    public void CanTrigger_ReturnsFalse_WhenHandlerTargetIsDeactivated()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<Go>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.DeactivateState<AdvancedUnitTestState>();

        Assert.That(machine.CanTrigger(new Go()), Is.False);
    }

    [Test]
    public void CanFire_ReturnsFalse_WhenHandlerTargetIsDeactivated()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<Go>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.DeactivateState<AdvancedUnitTestState>();

        Assert.That(machine.CanFire(new Go(), null), Is.False);
    }

    [Test]
    public void Fire_DoesNotTransition_WhenTargetIsDeactivated()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<Go>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.DeactivateState<AdvancedUnitTestState>();
        machine.Fire(new Go());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public void DeactivatingCurrentState_DoesNotAffectCurrentState()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.DeactivateState<SimpleUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }
}
