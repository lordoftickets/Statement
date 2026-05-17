using Statement.Tests.TestStates;
using Statement.Failures;
using Statement.Fluent.Api;

namespace Statement.Tests;

[TestFixture]
public class MachineSetupExceptionTests
{
    [Test]
    public void Build_WithoutStartIn_ThrowsMachineSetupException()
    {
        var builder = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>();

        Assert.Throws<MachineSetupException>(() => builder.Build());
    }

    [Test]
    public void BuildTyped_WithoutStartIn_ThrowsMachineSetupException()
    {
        var builder = StateMachineBuilder.For<IUnitTestState>()
            .AddState<SimpleUnitTestState>();

        Assert.Throws<MachineSetupException>(() => builder.BuildTyped());
    }

    [Test]
    public void Build_WithoutAnyStates_ThrowsMachineSetupException()
    {
        var builder = StateMachineBuilder.New();

        Assert.Throws<MachineSetupException>(() => builder.Build());
    }

    [Test]
    public void Build_WithoutStartIn_ExceptionMentionsStartIn()
    {
        var builder = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>();

        var ex = Assert.Throws<MachineSetupException>(() => builder.Build());
        Assert.That(ex!.Message, Does.Contain("StartIn"));
    }

    [Test]
    public void Build_WithStartIn_DoesNotThrow()
    {
        var builder = StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .StartIn<SimpleUnitTestState>();

        Assert.DoesNotThrow(() => builder.Build());
    }
}
