using Statement.Fluent.Api;
using Statement.Tests.TestStates;

namespace Statement.Tests;

file sealed class GoTrigger;

[TestFixture]
public class TransitionPayloadTests
{
    private sealed record FileData(string Path);

    [Test]
    public void OnEntryWith_ReceivesPayload_FromSetCurrentState()
    {
        FileData? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWith<FileData>((_, p) => captured = p))
            .StartIn<SimpleUnitTestState>()
            .Build();

        var payload = new FileData("foo.txt");
        machine.SetCurrentState<AdvancedUnitTestState>(payload);

        Assert.That(captured, Is.SameAs(payload));
    }

    [Test]
    public void OnEntryWith_ReceivesPayload_FromFire()
    {
        FileData? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>(s => s.On<GoTrigger>().GoTo<AdvancedUnitTestState>())
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWith<FileData>(p => captured = p))
            .StartIn<SimpleUnitTestState>()
            .Build();

        var payload = new FileData("bar.txt");
        machine.Fire(new GoTrigger(), payload);

        Assert.That(captured, Is.SameAs(payload));
    }

    [Test]
    public void OnEntryWith_WrongPayloadType_SkipsCallback_StillTransitions()
    {
        var invoked = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWith<FileData>((_, _) => invoked = true))
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.DoesNotThrow(() => machine.SetCurrentState<AdvancedUnitTestState>("not a FileData"));
        Assert.That(invoked, Is.False);
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void OnEntryWith_NullPayload_SkipsCallback_StillTransitions()
    {
        var invoked = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntryWith<FileData>((_, _) => invoked = true))
            .StartIn<SimpleUnitTestState>()
            .Build();

        Assert.DoesNotThrow(() => machine.SetCurrentState<AdvancedUnitTestState>());
        Assert.That(invoked, Is.False);
        Assert.That(machine.GetCurrentState(), Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void OnEntry_WithoutPayloadCallback_IgnoresPayload()
    {
        var entered = false;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>(s => s.OnEntry((_, _) => entered = true))
            .StartIn<SimpleUnitTestState>()
            .Build();

        machine.SetCurrentState<AdvancedUnitTestState>(new FileData("ignored"));

        Assert.That(entered, Is.True);
    }

    [Test]
    public void TransitionInformation_CarriesPayload_ToGlobalCallback()
    {
        TransitionInformation? captured = null;
        var machine = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .StartIn<SimpleUnitTestState>()
            .AddOnStateChangedCallback(info => captured = info)
            .Build();

        var payload = new FileData("baz.txt");
        machine.SetCurrentState<AdvancedUnitTestState>(payload);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Value.Payload, Is.SameAs(payload));
    }
}
