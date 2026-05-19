using Statement.Fluent.Api;
using Statement.Tests.TestStates;
using Statement.Tests.TestStates.Statement;

namespace Statement.Tests;

file sealed class GoTrigger;

[TestFixture]
public class AsyncStateBuilderTests
{
    #region OnEntryAsync

    [Test]
    public async Task OnEntryAsync_ParameterlessOverload_IsInvoked()
    {
        var called = false;
        var machine = StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Yield();
                called = true;
            }))
            .StartIn<InitialUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<SimpleUnitTestState>();

        Assert.That(called, Is.True);
    }

    [Test]
    public async Task OnEntryAsync_TypedOverload_ReceivesStateAndMachine()
    {
        SimpleUnitTestState? capturedState = null;
        IStateMachine? capturedMachine = null;
        var machine = StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>(s => s.OnEntryAsync(async (state, m) =>
            {
                await Task.Yield();
                capturedState = state;
                capturedMachine = m;
            }))
            .StartIn<InitialUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<SimpleUnitTestState>();

        Assert.That(capturedState, Is.Not.Null);
        Assert.That(capturedState, Is.SameAs(machine.GetCurrentState()));
        Assert.That(capturedMachine, Is.SameAs(machine));
    }

    [Test]
    public void OnEntryAsync_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s => s.OnEntryAsync((Func<Task>)null!));
        });
    }

    [Test]
    public void OnEntryAsync_TypedOverload_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s =>
                    s.OnEntryAsync((Func<SimpleUnitTestState, IStateMachine, Task>)null!));
        });
    }

    #endregion

    #region OnExitAsync

    [Test]
    public async Task OnExitAsync_ParameterlessOverload_IsInvoked()
    {
        var called = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitAsync(async () =>
            {
                await Task.Yield();
                called = true;
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(called, Is.True);
    }

    [Test]
    public async Task OnExitAsync_TypedOverload_ReceivesStateAndMachine()
    {
        SimpleUnitTestState? capturedState = null;
        IStateMachine? capturedMachine = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitAsync(async (state, m) =>
            {
                await Task.Yield();
                capturedState = state;
                capturedMachine = m;
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(capturedState, Is.Not.Null);
        Assert.That(capturedMachine, Is.SameAs(machine));
    }

    [Test]
    public void OnExitAsync_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s => s.OnExitAsync((Func<Task>)null!));
        });
    }

    [Test]
    public void OnExitAsync_TypedOverload_NullCallback_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s =>
                    s.OnExitAsync((Func<SimpleUnitTestState, IStateMachine, Task>)null!));
        });
    }

    #endregion

    #region OnEntryWithAsync

    [Test]
    public async Task OnEntryWithAsync_PayloadOnly_ReceivesPayload()
    {
        const string payload = "hello";
        
        string? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWithAsync<string>(async p =>
            {
                await Task.Yield();
                captured = p;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>(payload);

        Assert.That(captured, Is.EqualTo(payload));
    }

    [Test]
    public async Task OnEntryWithAsync_StateAndPayload_ReceivesBoth()
    {
        const string payload = "world";
        
        AdvancedUnitTestState? capturedState = null;
        string? capturedPayload = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWithAsync<string>(async (state, p) =>
            {
                await Task.Yield();
                capturedState = state;
                capturedPayload = p;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>(payload);

        Assert.That(capturedState, Is.SameAs(machine.GetCurrentState()));
        Assert.That(capturedPayload, Is.EqualTo(payload));
    }

    [Test]
    public async Task OnEntryWithAsync_WrongPayloadType_SkipsCallback()
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
    public async Task OnEntryWithAsync_NullPayload_SkipsCallback()
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

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(invoked, Is.False);
    }

    [Test]
    public void OnEntryWithAsync_PayloadOnly_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s => s.OnEntryWithAsync<string>((Func<string, Task>)null!));
        });
    }

    [Test]
    public void OnEntryWithAsync_StateAndPayload_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s =>
                    s.OnEntryWithAsync<string>((Func<SimpleUnitTestState, string, Task>)null!));
        });
    }

    #endregion

    #region OnExitWithAsync

    [Test]
    public async Task OnExitWithAsync_PayloadOnly_ReceivesPayload()
    {
        string? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitWithAsync<string>(async p =>
            {
                await Task.Yield();
                captured = p;
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>("goodbye");

        Assert.That(captured, Is.EqualTo("goodbye"));
    }

    [Test]
    public async Task OnExitWithAsync_StateAndPayload_ReceivesBoth()
    {
        SimpleUnitTestState? capturedState = null;
        string? capturedPayload = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.OnExitWithAsync<string>(async (state, p) =>
            {
                await Task.Yield();
                capturedState = state;
                capturedPayload = p;
            }))
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>("leaving");

        Assert.That(capturedState, Is.Not.Null);
        Assert.That(capturedPayload, Is.EqualTo("leaving"));
    }

    [Test]
    public async Task OnExitWithAsync_WrongPayloadType_SkipsCallback()
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
    public async Task OnExitWithAsync_NullPayload_SkipsCallback()
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

        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(invoked, Is.False);
    }

    [Test]
    public void OnExitWithAsync_PayloadOnly_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s => s.OnExitWithAsync<string>((Func<string, Task>)null!));
        });
    }

    [Test]
    public void OnExitWithAsync_StateAndPayload_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            StateMachineBuilder.New()
                .AddState<SimpleUnitTestState>(s =>
                    s.OnExitWithAsync<string>((Func<SimpleUnitTestState, string, Task>)null!));
        });
    }

    #endregion

    #region AddOnStateChangedCallbackAsync

    [Test]
    public async Task AddOnStateChangedCallbackAsync_IsInvokedOnTransition()
    {
        var called = false;
        var machine = await BuildWithAsyncGlobalCallback(async _ =>
        {
            await Task.Yield();
            called = true;
        });

        called = false;
        await machine.SetCurrentStateAsync<SimpleUnitTestState>();

        Assert.That(called, Is.True);
    }

    [Test]
    public async Task AddOnStateChangedCallbackAsync_CalledMultipleTimes()
    {
        var count = 0;
        var machine = await BuildWithAsyncGlobalCallback(async _ =>
        {
            await Task.Yield();
            count++;
        });

        count = 0;
        await machine.SetCurrentStateAsync<SimpleUnitTestState>();
        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task AddOnStateChangedCallbackAsync_TransitionInfo_ContainsCorrectData()
    {
        TransitionInformation? captured = null;
        var machine = await BuildWithAsyncGlobalCallback(async info =>
        {
            await Task.Yield();
            captured = info;
        });

        await machine.SetCurrentStateAsync<SimpleUnitTestState>();

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Value.FromType, Is.EqualTo(typeof(InitialUnitTestState)));
        Assert.That(captured.Value.ToType, Is.EqualTo(typeof(SimpleUnitTestState)));
    }

    [Test]
    public async Task AddOnStateChangedCallbackAsync_CalledBeforeOnEntryAsync()
    {
        var order = new List<string>();
        var machine = await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryAsync(async () =>
            {
                await Task.Yield();
                order.Add("entry");
            }))
            .AddOnStateChangedCallbackAsync(async _ =>
            {
                await Task.Yield();
                order.Add("global");
            })
            .StartIn<InitialUnitTestState>()
            .BuildAsync();

        order.Clear();
        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(order, Is.EqualTo(new[] { "global", "entry" }));
    }

    [Test]
    public async Task SyncAndAsyncGlobalCallbacks_BothFire()
    {
        var syncCalled = false;
        var asyncCalled = false;
        var machine = await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .AddOnStateChangedCallback(_ => syncCalled = true)
            .AddOnStateChangedCallbackAsync(async _ =>
            {
                await Task.Yield();
                asyncCalled = true;
            })
            .StartIn<InitialUnitTestState>()
            .BuildAsync();

        syncCalled = false;
        asyncCalled = false;
        await machine.SetCurrentStateAsync<AdvancedUnitTestState>();

        Assert.That(syncCalled, Is.True);
        Assert.That(asyncCalled, Is.True);
    }

    private static async Task<StateMachine> BuildWithAsyncGlobalCallback(Func<TransitionInformation, Task> callback)
    {
        return await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .AddOnStateChangedCallbackAsync(callback)
            .StartIn<InitialUnitTestState>()
            .BuildAsync();
    }

    #endregion

    #region IAsyncStatement interface

    [Test]
    public async Task IAsyncStatement_OnEntryAsync_CalledOnTransitionIn()
    {
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AsyncStatement>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AsyncStatement>();

        var state = machine.GetCurrentState<AsyncStatement>();
        Assert.That(state.OnEntryAsyncCalled, Is.True);
        Assert.That(state.OnExitAsyncCalled, Is.False);
    }

    [Test]
    public async Task IAsyncStatement_OnExitAsync_CalledOnTransitionOut()
    {
        var machine = StateMachineBuilder.New()
            .AddState<AsyncStatement>()
            .AddState<SimpleUnitTestState>()
            .StartIn<AsyncStatement>()
            .Build();

        var asyncState = machine.GetCurrentState<AsyncStatement>();
        await machine.SetCurrentStateAsync<SimpleUnitTestState>();

        Assert.That(asyncState.OnExitAsyncCalled, Is.True);
    }

    [Test]
    public async Task IAsyncStatement_AndFluentCallbacks_BothFire()
    {
        var fluentCalled = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AsyncStatement>(s => s.OnEntryAsync(async () =>
            {
                await Task.Yield();
                fluentCalled = true;
            }))
            .StartIn<SimpleUnitTestState>()
            .Build();

        await machine.SetCurrentStateAsync<AsyncStatement>();

        var state = machine.GetCurrentState<AsyncStatement>();
        Assert.That(state.OnEntryAsyncCalled, Is.True);
        Assert.That(fluentCalled, Is.True);
    }

    #endregion

    #region Chaining / fluent return types

    [Test]
    public void OnEntryAsync_ReturnsSameBuilder_ForChaining()
    {
        StateBuilder<SimpleUnitTestState>? captured = null;
        StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s =>
            {
                var result = s.OnEntryAsync(() => Task.CompletedTask);
                captured = result;
            })
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.That(captured, Is.Not.Null);
    }

    [Test]
    public void OnExitAsync_ReturnsSameBuilder_ForChaining()
    {
        StateBuilder<SimpleUnitTestState>? captured = null;
        StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s =>
            {
                var result = s.OnExitAsync(() => Task.CompletedTask);
                captured = result;
            })
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.That(captured, Is.Not.Null);
    }

    [Test]
    public async Task FullChain_AsyncEntryExitPayloadAndGlobalCallback()
    {
        var order = new List<string>();
        var machine = await StateMachineBuilder.New()
            .AddState<InitialUnitTestState>()
            .AddState<SimpleUnitTestState>(s => s
                .OnExitWithAsync<string>(async p =>
                {
                    await Task.Yield();
                    order.Add($"exit:{p}");
                }))
            .AddState<AdvancedUnitTestState>(s => s
                .OnEntryWithAsync<string>(async p =>
                {
                    await Task.Yield();
                    order.Add($"entry:{p}");
                }))
            .AddOnStateChangedCallbackAsync(async _ =>
            {
                await Task.Yield();
                order.Add("global");
            })
            .StartIn<InitialUnitTestState>()
            .BuildAsync();

        order.Clear();
        await machine.SetCurrentStateAsync<SimpleUnitTestState>();
        order.Clear();
        await machine.SetCurrentStateAsync<AdvancedUnitTestState>("data");

        Assert.That(order, Is.EqualTo(new[] { "exit:data", "global", "entry:data" }));
    }

    #endregion
}
