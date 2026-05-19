using System.Threading.Tasks;
using Statement.Fluent.Api;
using Statement.Tests.TestStates;
using Statement.Tests.TestStates.Statement;

namespace Statement.Tests;

file sealed class GoTrigger;

[TestFixture]
public class AsyncStateMachineTests
{
    [Test]
    public async Task SetCurrentStateAsync_TransitionsToTargetState()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_WithPayload_TransitionsAndDeliversPayload()
    {
        string? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWith<string>(p => captured = p))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>("hello");

        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
        Assert.That(captured, Is.EqualTo("hello"));
    }

    [Test]
    public void SetCurrentStateAsync_UnregisteredState_Throws()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.ThrowsAsync<InvalidOperationException>(() => machine.SetCurrentStateAsync<AdvancedUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_BlockedByRule_DoesNotTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.CannotTransitionTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public async Task FireAsync_TransitionsToTargetState()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public async Task FireAsync_WithPayload_DeliversPayload()
    {
        string? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWith<string>(p => captured = p))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger(), "payload");

        Assert.That(captured, Is.EqualTo("payload"));
    }

    [Test]
    public void FireAsync_NullTrigger_Throws()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.ThrowsAsync<ArgumentNullException>(() => machine.FireAsync(null!));
    }

    [Test]
    public async Task FireAsync_NoHandler_SilentPolicy_DoesNotTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public async Task FireAsync_GuardFails_DoesNotTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s
                .On<GoTrigger>().If(() => false).GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public async Task FireAsync_BlockedByRule_DoesNotTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s
                .CannotTransitionTo<AdvancedUnitTestState>()
                .On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_InvokesIAsyncStatementHooks()
    {
        var machine = StateMachineBuilder.New()
            .AddState<AsyncStatement>()
            .AddState<SimpleUnitTestState>()
            .StartIn<AsyncStatement>()
            .Build();

        var asyncState = machine.GetCurrentState<AsyncStatement>();
        await machine.SetCurrentStateAsync<SimpleUnitTestState>();

        Assert.That(asyncState.OnEntryAsyncCalled, Is.True);
        Assert.That(asyncState.OnExitAsyncCalled, Is.True);
    }

    [Test]
    public async Task SetCurrentStateAsync_InvokesIStatementHooks()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleStatement>()
            .AddState<SimpleUnitTestState>()
            .StartIn<SimpleStatement>()
            .Build();

        var statement = machine.GetCurrentState<SimpleStatement>();
        await machine.SetCurrentStateAsync<SimpleUnitTestState>();

        Assert.That(statement.OnEntryCalled, Is.True);
        Assert.That(statement.OnExitCalled, Is.True);
    }

    [Test]
    public async Task SetCurrentStateAsync_AwaitsAsyncOnEntryCallback()
    {
        var completed = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Delay(50);
                completed = true;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(completed, Is.True);
    }

    [Test]
    public async Task SetCurrentStateAsync_AwaitsAsyncOnExitCallback()
    {
        var completed = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitAsync(async () =>
            {
                await Task.Delay(50);
                completed = true;
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(completed, Is.True);
    }

    [Test]
    public async Task FireAsync_AwaitsAsyncCallbacks()
    {
        var entryCompleted = false;
        var exitCompleted = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s
                .OnExitAsync(async () =>
                {
                    await Task.Delay(50);
                    exitCompleted = true;
                })
                .On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Delay(50);
                entryCompleted = true;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger());

        Assert.That(exitCompleted, Is.True);
        Assert.That(entryCompleted, Is.True);
    }

    [Test]
    public async Task SetCurrentStateAsync_CallbackOrder_ExitThenCommitThenEntry()
    {
        var order = new List<string>();
        var machine = StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>(s => s.OnExitAsync(async () =>
            {
                await Task.Yield();
                order.Add("exit");
            }))
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Yield();
                order.Add("entry");
            }))
            .AddOnStateChangedCallback(_ => order.Add("global"))
            .StartIn<InitialUnitTestState>()
            .Build();

        order.Clear();
        await machine.SetCurrentStateAsync<SimpleUnitTestState>();
        order.Clear();
        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(order, Is.EqualTo(new[] { "exit", "global", "entry" }));
    }

    [Test]
    public void SyncFire_WithAsyncCallback_ThrowsInvalidOperationException()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s
                .On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Delay(100);
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.Throws<InvalidOperationException>(() => machine.Fire(new GoTrigger()));
    }

    [Test]
    public void SyncSetCurrentState_WithAsyncCallback_ThrowsInvalidOperationException()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Delay(100);
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.Throws<InvalidOperationException>(() => machine.SetCurrentState<AdvancedUnitTestState>());
    }

    [Test]
    public void SyncFire_WithSyncCallbacksOnly_WorksNormally()
    {
        var entered = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>(s => s.OnEntry(() => entered = true))
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.Fire(new GoTrigger());

        Assert.That(entered, Is.True);
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_AsyncGlobalCallback_IsAwaited()
    {
        var completed = false;
        var machine = await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .AddOnStateChangedCallbackAsync(async _ =>
            {
                await Task.Delay(50);
                completed = true;
            })
            .StartIn<InitialUnitTestState>()
            .BuildAsync();

        completed = false;
        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(completed, Is.True);
    }

    [Test]
    public async Task SyncSetCurrentState_WithAsyncGlobalCallback_ThrowsInvalidOperationException()
    {
        var machine = await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .AddOnStateChangedCallbackAsync(async _ =>
            {
                await Task.Delay(100);
            })
            .StartIn<InitialUnitTestState>()
            .BuildAsync();

        Assert.Throws<InvalidOperationException>(() => machine.SetCurrentState<AdvancedUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_AsyncOnEntryWithPayload_IsAwaited()
    {
        string? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWithAsync<string>(async p =>
            {
                await Task.Delay(50);
                captured = p;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>("data");

        Assert.That(captured, Is.EqualTo("data"));
    }

    [Test]
    public async Task SetCurrentStateAsync_AsyncOnExitWithPayload_IsAwaited()
    {
        string? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitWithAsync<string>(async p =>
            {
                await Task.Delay(50);
                captured = p;
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>("data");

        Assert.That(captured, Is.EqualTo("data"));
    }

    [Test]
    public async Task SetCurrentStateAsync_AsyncOnEntryWithPayload_WrongType_SkipsCallback()
    {
        var invoked = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWithAsync<string>(async _ =>
            {
                await Task.Yield();
                invoked = true;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>(42);

        Assert.That(invoked, Is.False);
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_AsyncOnExitWithPayload_WrongType_SkipsCallback()
    {
        var invoked = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitWithAsync<string>(async _ =>
            {
                await Task.Yield();
                invoked = true;
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>(42);

        Assert.That(invoked, Is.False);
    }

    [Test]
    public async Task SetCurrentStateAsync_MultipleTransitions_AllAwaited()
    {
        var order = new List<int>();
        var machine = StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Yield();
                order.Add(1);
            }))
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Yield();
                order.Add(2);
            }))
            .StartIn<InitialUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<SimpleUnitTestState>();
        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(order, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task FireAsync_IgnoreTrigger_DoesNotTransition()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<GoTrigger>().Ignore())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger());

        Assert.That(machine.GetCurrentState(), Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_DuringAsyncOnExit_CurrentStateIsStillExitingState()
    {
        object? currentDuringExit = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitAsync(async (_, m) =>
            {
                await Task.Yield();
                currentDuringExit = m.GetCurrentState();
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(currentDuringExit, Is.TypeOf<SimpleUnitTestState>());
    }

    [Test]
    public async Task SetCurrentStateAsync_DuringAsyncOnEntry_CurrentStateIsNewState()
    {
        object? currentDuringEntry = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async (_, m) =>
            {
                await Task.Yield();
                currentDuringEntry = m.GetCurrentState();
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(currentDuringEntry, Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public async Task FireAsync_AsyncOnEntryWith_TypedCallback_ReceivesStateAndPayload()
    {
        AdvancedUnitTestState? capturedState = null;
        string? capturedPayload = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWithAsync<string>(async (state, p) =>
            {
                await Task.Yield();
                capturedState = state;
                capturedPayload = p;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger(), "test-data");

        Assert.That(capturedState, Is.Not.Null);
        Assert.That(capturedPayload, Is.EqualTo("test-data"));
    }

    [Test]
    public async Task FireAsync_AsyncOnExitWith_TypedCallback_ReceivesStateAndPayload()
    {
        SimpleUnitTestState? capturedState = null;
        string? capturedPayload = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s
                .OnExitWithAsync<string>(async (state, p) =>
                {
                    await Task.Yield();
                    capturedState = state;
                    capturedPayload = p;
                })
                .On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.FireAsync(new GoTrigger(), "exit-data");

        Assert.That(capturedState, Is.Not.Null);
        Assert.That(capturedPayload, Is.EqualTo("exit-data"));
    }

    [Test]
    public async Task SetCurrentStateAsync_GlobalCallback_ReceivesCorrectTransitionInfo()
    {
        TransitionInformation? captured = null;
        var machine = await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .AddOnStateChangedCallbackAsync(async info =>
            {
                await Task.Yield();
                captured = info;
            })
            .StartIn<InitialUnitTestState>()
            .BuildAsync();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>("payload");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Value.From, Is.TypeOf<InitialUnitTestState>());
        Assert.That(captured.Value.To, Is.TypeOf<AdvancedUnitTestState>());
        Assert.That(captured.Value.Payload, Is.EqualTo("payload"));
    }

    [Test]
    public async Task SetCurrentStateAsync_GlobalCallbackException_DoesNotCrashMachine()
    {
        var machine = await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .AddOnStateChangedCallbackAsync(async _ =>
            {
                await Task.Yield();
                throw new Exception("callback error");
            })
            .StartIn<InitialUnitTestState>()
            .BuildAsync();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }
}
