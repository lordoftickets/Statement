using Builder.Tests.TestStates;
using NUnit.Framework;
using Statement.Fluent.Api;

namespace Builder.Tests;

[TestFixture]
public class CompileStateMachineTests
{
    [Test]
    public void For_BaseType_HappyPath()
    {
        Assert.DoesNotThrow(() => StateMachineBuilder.For<IUnitTestState>()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .Build());
    }

    // Invalid-state case (e.g. AddState<SomethingThatIsntIUnitTestState>) is now
    // a compile-time error thanks to the `where TState : TBase` constraint on
    // StateMachineBuilder<TBase>.AddState — no runtime test needed.

    [Test]
    public void New_AcceptsAnyState()
    {
        Assert.DoesNotThrow(() => StateMachineBuilder.New()
            .AddState<SimpleUnitTestState>()
            .AddState<AdvancedUnitTestState>()
            .Build());
    }
}
