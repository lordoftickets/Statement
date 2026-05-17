using System;
using Statement.Failures;
using Statement.Fluent.Api;
using Statement.Tests.TestStates;

namespace Statement.Tests;

[TestFixture]
public class TriggerTests
{
    // ---- helper trigger / state types ----

    public sealed record OpenDoor;
    public sealed record CloseDoor;
    public sealed record LockDoor(string KeyId);

    public enum DoorTrigger { Open, Close, Lock }

    public class Closed : IUnitTestState { }
    public class Opened : IUnitTestState { }
    public class Locked : IUnitTestState { }

    // ---- 1. basic dispatch ----

    [Test]
    public void Fire_WithMarkerTypeTrigger_TransitionsToTarget()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    [Test]
    public void Fire_WithEnumTrigger_TransitionsToTarget()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On(DoorTrigger.Open).GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(DoorTrigger.Open);

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    [Test]
    public void Fire_WithStringTrigger_TransitionsToTarget()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On("open").GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire("open");

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    [Test]
    public void Fire_WithTypeAsTrigger_TransitionsToTarget()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(typeof(OpenDoor));

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    [Test]
    public void Fire_DifferentInstancesOfSameMarkerType_BothMatch()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            .AddState<Opened>(s => s.On<CloseDoor>().GoTo<Closed>())
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());

        machine.Fire(new CloseDoor());
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Closed>());
    }

    // ---- 2. state-dependent routing ----

    [Test]
    public void Fire_SameTriggerInDifferentStates_RoutesIndependently()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            .AddState<Locked>(s => s.On<OpenDoor>().GoTo<Closed>())
            .AddState<Opened>()
            .StartIn<Locked>()
            .Build();

        machine.Fire(new OpenDoor());
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Closed>());

        machine.Fire(new OpenDoor());
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    // ---- 3. Ignore() — internal transition ----

    [Test]
    public void Ignore_DoesNotChangeState()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Locked>(s => s.On<OpenDoor>().Ignore())
            .StartIn<Locked>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Locked>());
    }

    [Test]
    public void Ignore_DoesNotInvokeOnExitOrOnEntry()
    {
        var entryCount = 0;
        var exitCount = 0;
        var machine = StateMachineBuilder.New()
            .AddState<Locked>(s => s
                .OnEntry(() => entryCount++)
                .OnExit(() => exitCount++)
                .On<OpenDoor>().Ignore())
            .StartIn<Locked>()
            .Build();

        // entry fired once during StartIn
        Assert.That(entryCount, Is.EqualTo(1));

        machine.Fire(new OpenDoor());

        Assert.That(entryCount, Is.EqualTo(1), "OnEntry must not fire for Ignore()");
        Assert.That(exitCount, Is.EqualTo(0), "OnExit must not fire for Ignore()");
    }

    [Test]
    public void Ignore_DoesNotInvokeGlobalStateChangedCallback()
    {
        var callCount = 0;
        var machine = StateMachineBuilder.New()
            .AddOnStateChangedCallback(_ => callCount++)
            .AddState<Locked>(s => s.On<OpenDoor>().Ignore())
            .StartIn<Locked>()
            .Build();

        callCount = 0;
        machine.Fire(new OpenDoor());

        Assert.That(callCount, Is.Zero);
    }

    [Test]
    public void Ignore_StillRunsDoSideEffect()
    {
        var sideEffectFired = false;
        var machine = StateMachineBuilder.New()
            .AddState<Locked>(s => s.On<OpenDoor>().Do(_ => sideEffectFired = true).Ignore())
            .StartIn<Locked>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(sideEffectFired, Is.True);
    }

    // ---- 4. self-transition (GoTo<TSelf>) vs Ignore ----

    [Test]
    public void GoToSelf_InvokesOnExitAndOnEntry()
    {
        var entryCount = 0;
        var exitCount = 0;
        var machine = StateMachineBuilder.New()
            .AddState<Locked>(s => s
                .OnEntry(() => entryCount++)
                .OnExit(() => exitCount++)
                .On<OpenDoor>().GoTo<Locked>())
            .StartIn<Locked>()
            .Build();

        entryCount = 0;
        machine.Fire(new OpenDoor());

        Assert.That(exitCount, Is.EqualTo(1));
        Assert.That(entryCount, Is.EqualTo(1));
    }

    // ---- 5. Guards ----

    [Test]
    public void If_GuardTrue_AllowsTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().If(() => true).GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    [Test]
    public void If_GuardFalse_BlocksTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().If(() => false).GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Closed>());
    }

    [Test]
    public void If_GuardEvaluatedAtFireTime()
    {
        var allow = false;
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().If(() => allow).GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Closed>());

        allow = true;
        machine.Fire(new OpenDoor());
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    [Test]
    public void If_NullGuard_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StateMachineBuilder.New()
                .AddState<Closed>(s => s.On<OpenDoor>().If(null!).GoTo<Opened>())
                .AddState<Opened>()
                .StartIn<Closed>()
                .Build());
    }

    // ---- 6. Do() side-effects with payload ----

    [Test]
    public void Do_ReceivesTriggerPayload()
    {
        LockDoor? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<LockDoor>().Do(t => captured = t).GoTo<Locked>())
            .AddState<Locked>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new LockDoor("key-1"));

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.KeyId, Is.EqualTo("key-1"));
    }

    [Test]
    public void Do_FiresBeforeOnExitAndOnEntry()
    {
        var sequence = new List<string>();
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s
                .OnExit(() => sequence.Add("exit"))
                .On<OpenDoor>().Do(_ => sequence.Add("do")).GoTo<Opened>())
            .AddState<Opened>(s => s.OnEntry(() => sequence.Add("entry")))
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(sequence, Is.EqualTo(new[] { "do", "exit", "entry" }));
    }

    [Test]
    public void Do_NotInvokedWhenGuardFails()
    {
        var sideEffectFired = false;
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().If(() => false).Do(_ => sideEffectFired = true).GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(sideEffectFired, Is.False);
    }

    [Test]
    public void Do_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StateMachineBuilder.New()
                .AddState<Closed>(s => s.On<OpenDoor>().Do(null!).GoTo<Opened>())
                .AddState<Opened>()
                .StartIn<Closed>()
                .Build());
    }

    // ---- 7. Coexistence with CannotTransitionTo ----

    [Test]
    public void Fire_WhenTargetForbiddenByCannotTransitionTo_BlockedByTransitionPolicy()
    {
        TransitionFailureInfo? captured = null;
        var machine = StateMachineBuilder.New()
            .OnTransitionFailure(TransitionFailurePolicy.Invoke(info => captured = info))
            .AddState<Closed>(s => s
                .CannotTransitionTo<Opened>()
                .On<OpenDoor>().GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Closed>());
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.From, Is.EqualTo(typeof(Closed)));
        Assert.That(captured.To, Is.EqualTo(typeof(Opened)));
    }

    // ---- 8. TriggerFailurePolicy ----

    [Test]
    public void Fire_NoHandler_SilentPolicy_DoesNothing()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>()
            .StartIn<Closed>()
            .Build();

        Assert.DoesNotThrow(() => machine.Fire(new OpenDoor()));
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Closed>());
    }

    [Test]
    public void Fire_NoHandler_ThrowPolicy_RaisesTriggerFailedException()
    {
        var machine = StateMachineBuilder.New()
            .OnTriggerFailure(TriggerFailurePolicy.Throw)
            .AddState<Closed>()
            .StartIn<Closed>()
            .Build();

        var ex = Assert.Throws<TriggerFailedException>(() => machine.Fire(new OpenDoor()));
        Assert.That(ex!.Info.Reason, Is.EqualTo(TriggerFailureReason.NoHandler));
        Assert.That(ex.Info.CurrentState, Is.EqualTo(typeof(Closed)));
        Assert.That(ex.Info.Trigger, Is.TypeOf<OpenDoor>());
    }

    [Test]
    public void Fire_GuardFailed_ThrowPolicy_RaisesWithGuardFailedReason()
    {
        var machine = StateMachineBuilder.New()
            .OnTriggerFailure(TriggerFailurePolicy.Throw)
            .AddState<Closed>(s => s.On<OpenDoor>().If(() => false).GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        var ex = Assert.Throws<TriggerFailedException>(() => machine.Fire(new OpenDoor()));
        Assert.That(ex!.Info.Reason, Is.EqualTo(TriggerFailureReason.GuardFailed));
    }

    [Test]
    public void Fire_NoHandler_InvokePolicy_FiresCallback()
    {
        TriggerFailureInfo? captured = null;
        var machine = StateMachineBuilder.New()
            .OnTriggerFailure(TriggerFailurePolicy.Invoke(info => captured = info))
            .AddState<Closed>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Reason, Is.EqualTo(TriggerFailureReason.NoHandler));
        Assert.That(captured.CurrentState, Is.EqualTo(typeof(Closed)));
    }

    [Test]
    public void OnTriggerFailure_NullPolicy_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StateMachineBuilder.New().OnTriggerFailure(null!));
    }

    [Test]
    public void TriggerFailurePolicy_Invoke_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TriggerFailurePolicy.Invoke(null!));
    }

    // ---- 9. TransitionInformation carries trigger ----

    [Test]
    public void GlobalCallback_ReceivesTriggerInTransitionInformation()
    {
        TransitionInformation? captured = null;
        var machine = StateMachineBuilder.New()
            .AddOnStateChangedCallback(info => captured = info)
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        var trigger = new OpenDoor();
        machine.Fire(trigger);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Value.Trigger, Is.SameAs(trigger));
        Assert.That(captured.Value.ToType, Is.EqualTo(typeof(Opened)));
    }

    [Test]
    public void GlobalCallback_TriggerIsNullForSetCurrentStateCalls()
    {
        TransitionInformation? captured = null;
        var machine = StateMachineBuilder.New()
            .AddOnStateChangedCallback(info => captured = info)
            .AddState<Closed>()
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.SetCurrentState<Opened>();

        Assert.That(captured!.Value.Trigger, Is.Null);
    }

    // ---- 10. Coexistence: Fire and SetCurrentState both work ----

    [Test]
    public void FireAndSetCurrentState_CoexistOnSameMachine()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            .AddState<Opened>()
            .AddState<Locked>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());

        machine.SetCurrentState<Locked>();
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Locked>());
    }

    // ---- 11. Build-time validation ----

    [Test]
    public void Build_TriggerTargetNotRegistered_ThrowsMachineSetupException()
    {
        var builder = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            // Opened is NOT registered
            .StartIn<Closed>();

        Assert.Throws<MachineSetupException>(() => builder.Build());
    }

    [Test]
    public void AddTriggerHandler_DuplicateTriggerOnSameState_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StateMachineBuilder.New()
                .AddState<Closed>(s =>
                {
                    s.On<OpenDoor>().GoTo<Opened>();
                    s.On<OpenDoor>().GoTo<Locked>();
                })
                .AddState<Opened>()
                .AddState<Locked>()
                .StartIn<Closed>()
                .Build());
    }

    // ---- 12. Argument validation on Fire ----

    [Test]
    public void Fire_NullTrigger_Throws()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>()
            .StartIn<Closed>()
            .Build();

        Assert.Throws<ArgumentNullException>(() => machine.Fire(null!));
    }

    [Test]
    public void StateBuilder_On_NullKey_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StateMachineBuilder.New()
                .AddState<Closed>(s => s.On((object)null!).GoTo<Opened>())
                .AddState<Opened>()
                .StartIn<Closed>()
                .Build());
    }

    // ---- 13. Chained .On / .Do / .If / .GoTo composition ----

    [Test]
    public void TriggerBuilder_AllowsChainingIfAndDoAndGoTo()
    {
        var fired = false;
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s => s.On<OpenDoor>()
                .If(() => true)
                .Do(_ => fired = true)
                .GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new OpenDoor());

        Assert.That(fired, Is.True);
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    // ---- 14. Typed machine still exposes Fire ----

    [Test]
    public void TypedMachine_FireWorks()
    {
        var machine = StateMachineBuilder.For<IUnitTestState>()
            .AddState<Closed>(s => s.On<OpenDoor>().GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .BuildTyped();

        machine.Fire(new OpenDoor());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<Opened>());
    }

    // ---- 15. Multiple distinct triggers on the same state ----

    [Test]
    public void State_WithMultipleTriggers_RoutesEachCorrectly()
    {
        var machine = StateMachineBuilder.New()
            .AddState<Closed>(s =>
            {
                s.On<OpenDoor>().GoTo<Opened>();
                s.On<LockDoor>().GoTo<Locked>();
            })
            .AddState<Opened>()
            .AddState<Locked>()
            .StartIn<Closed>()
            .Build();

        machine.Fire(new LockDoor("k"));
        Assert.That(machine.GetCurrentState(), Is.TypeOf<Locked>());
    }

    // ---- 16. Enum value identity ----

    [Test]
    public void EnumTrigger_DifferentValueOfSameEnum_DoesNotMatch()
    {
        var machine = StateMachineBuilder.New()
            .OnTriggerFailure(TriggerFailurePolicy.Throw)
            .AddState<Closed>(s => s.On(DoorTrigger.Open).GoTo<Opened>())
            .AddState<Opened>()
            .StartIn<Closed>()
            .Build();

        var ex = Assert.Throws<TriggerFailedException>(() => machine.Fire(DoorTrigger.Close));
        Assert.That(ex!.Info.Reason, Is.EqualTo(TriggerFailureReason.NoHandler));
    }
}
