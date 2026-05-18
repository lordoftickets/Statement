using Statement.Rules;
using Statement.Tests.TestStates;

namespace Statement.Tests;

[TestFixture]
public class RuleMasterTests
{
    private RuleMaster _ruleMaster = null!;

    [SetUp]
    public void SetUp() => _ruleMaster = new RuleMaster();

    [Test]
    public void IsAllowed_CurrentIsNull_ReturnsTrue()
    {
        var target = new StateNode(typeof(SimpleUnitTestState));

        Assert.That(_ruleMaster.IsAllowed(null, target), Is.True);
    }

    [Test]
    public void IsAllowed_TargetIsNull_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState));

        Assert.That(_ruleMaster.IsAllowed(current, null), Is.True);
    }

    [Test]
    public void IsAllowed_BothNull_ReturnsTrue()
    {
        Assert.That(_ruleMaster.IsAllowed(null, null), Is.True);
    }

    [Test]
    public void IsAllowed_CurrentHasNoRule_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState));
        var target = new StateNode(typeof(AdvancedUnitTestState));

        Assert.That(_ruleMaster.IsAllowed(current, target), Is.True);
    }

    [Test]
    public void IsAllowed_TargetNotForbidden_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule()
        };
        current.TransitionRule.ForbiddenNextStates.Add(typeof(ExtraUnitTestState));
        var target = new StateNode(typeof(AdvancedUnitTestState));

        Assert.That(_ruleMaster.IsAllowed(current, target), Is.True);
    }

    [Test]
    public void IsAllowed_TargetIsForbidden_ReturnsFalse()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule()
        };
        current.TransitionRule.ForbiddenNextStates.Add(typeof(AdvancedUnitTestState));
        var target = new StateNode(typeof(AdvancedUnitTestState));

        Assert.That(_ruleMaster.IsAllowed(current, target), Is.False);
    }

    [Test]
    public void IsAllowed_EmptyForbiddenList_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule()
        };
        var target = new StateNode(typeof(AdvancedUnitTestState));

        Assert.That(_ruleMaster.IsAllowed(current, target), Is.True);
    }
}
