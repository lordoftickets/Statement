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

        Assert.That(_ruleMaster.IsAllowedTransition(null, target), Is.True);
    }

    [Test]
    public void IsAllowed_TargetIsNull_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState));

        Assert.That(_ruleMaster.IsAllowedTransition(current, targetNode: null), Is.True);
    }

    [Test]
    public void IsAllowed_BothNull_ReturnsTrue()
    {
        Assert.That(_ruleMaster.IsAllowedTransition(current: null, targetNode: null), Is.True);
    }

    [Test]
    public void IsAllowed_CurrentHasNoRule_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState));
        var target = new StateNode(typeof(AdvancedUnitTestState));

        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.True);
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

        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.True);
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

        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.False);
    }

    [Test]
    public void IsAllowed_EmptyForbiddenList_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule()
        };
        var target = new StateNode(typeof(AdvancedUnitTestState));

        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.True);
    }

    [Test]
    public void IsAllowed_NextStateIsLegalTarget_ReturnTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                AllowedNextStates = { typeof(AdvancedUnitTestState) }
            }
        };
        
        var target = new StateNode(typeof(AdvancedUnitTestState));
        
        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.True);   
    }
    
    [Test]
    public void IsNotAllowed_NextStateIsNoLegalTarget_ReturnFalse()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                AllowedNextStates = { typeof(InitialUnitTestState), typeof(ExtraUnitTestState) }
            }
        };
        
        var target = new StateNode(typeof(AdvancedUnitTestState));
        
        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.False);   
    }

    [Test]
    public void IsAllowed_NoLegalNextTarget_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule()
        };
        
        var target = new StateNode(typeof(AdvancedUnitTestState));
        
        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.True);  
    }
    
    [Test]
    public void IsNotAllowed_NextStateIsLegalTarget_ButForbidden_ReturnFalse()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                AllowedNextStates = { typeof(AdvancedUnitTestState) },
                ForbiddenNextStates = { typeof(AdvancedUnitTestState) }
            }
        };

        var target = new StateNode(typeof(AdvancedUnitTestState));

        Assert.That(_ruleMaster.IsAllowedTransition(current, target), Is.False);
    }

    #region CheckIfTypeIsValidNextState

    [Test]
    public void CheckType_CurrentIsNull_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => _ruleMaster.CheckIfTypeIsValidNextState(null, new StateNode(typeof(SimpleUnitTestState))));
    }

    [Test]
    public void CheckType_NoTransitionRule_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState));

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(AdvancedUnitTestState))), Is.True);
    }

    [Test]
    public void CheckType_EmptyRule_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule()
        };

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(AdvancedUnitTestState))), Is.True);
    }

    [Test]
    public void CheckType_TargetIsForbidden_ReturnsFalse()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                ForbiddenNextStates = { typeof(AdvancedUnitTestState) }
            }
        };

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(AdvancedUnitTestState))), Is.False);
    }

    [Test]
    public void CheckType_TargetNotForbidden_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                ForbiddenNextStates = { typeof(ExtraUnitTestState) }
            }
        };

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(AdvancedUnitTestState))), Is.True);
    }

    [Test]
    public void CheckType_TargetInAllowedList_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                AllowedNextStates = { typeof(AdvancedUnitTestState), typeof(ExtraUnitTestState) }
            }
        };

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(AdvancedUnitTestState))), Is.True);
    }

    [Test]
    public void CheckType_TargetNotInAllowedList_ReturnsFalse()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                AllowedNextStates = { typeof(InitialUnitTestState), typeof(ExtraUnitTestState) }
            }
        };

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(AdvancedUnitTestState))), Is.False);
    }

    [Test]
    public void CheckType_TargetAllowedButAlsoForbidden_ReturnsFalse()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule
            {
                AllowedNextStates = { typeof(AdvancedUnitTestState) },
                ForbiddenNextStates = { typeof(AdvancedUnitTestState) }
            }
        };

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(AdvancedUnitTestState))), Is.False);
    }

    [Test]
    public void CheckType_EmptyAllowedList_NoForbidden_ReturnsTrue()
    {
        var current = new StateNode(typeof(SimpleUnitTestState))
        {
            TransitionRule = new TransitionRule()
        };

        Assert.That(_ruleMaster.CheckIfTypeIsValidNextState(current, new StateNode(typeof(InitialUnitTestState))), Is.True);
    }

    #endregion
}

