using Statement.Tests.TestStates;
using Statement.Tests.TestStates.Statement;

namespace Statement.Tests;

[TestFixture]
public class TransitionExecutorTests
{
    private TransitionExecutor _executor = null!;
    private StateMachine _machine = null!;

    [SetUp]
    public void SetUp()
    {
        _executor = new TransitionExecutor();
        _machine = new StateMachine();
    }

    [Test]
    public void Execute_InvokesCommit()
    {
        var from = new StateNode(typeof(SimpleUnitTestState));
        var to = new StateNode(typeof(AdvancedUnitTestState));
        var committed = false;

        _executor.Execute(new Transition(from, to), _machine, () => committed = true);

        Assert.That(committed, Is.True);
    }

    [Test]
    public void Execute_FromNull_DoesNotInvokeExitCallbacks()
    {
        var to = new StateNode(typeof(SimpleStatement));
        var entryCalled = false;
        to.OnEntry = (_, _) => entryCalled = true;

        Assert.DoesNotThrow(() => _executor.Execute(new Transition(null, to), _machine, () => { }));
        Assert.That(entryCalled, Is.True);
        Assert.That(((SimpleStatement)to.GetOrCreateInstance()).OnEntryCalled, Is.True);
    }

    [Test]
    public void Execute_CallsOrder_ExitThenCommitThenEntry()
    {
        var order = new List<string>();
        var from = new StateNode(typeof(SimpleStatement)) { OnExit = _ => order.Add("from.OnExit") };
        var to = new StateNode(typeof(SimpleStatement)) { OnEntry = (_, _) => order.Add("to.OnEntry") };

        _executor.Execute(new Transition(from, to), _machine, () => order.Add("commit"));

        Assert.That(order, Is.EqualTo(new[]
        {
            "from.OnExit",
            "commit",
            "to.OnEntry",
        }));
    }

    [Test]
    public void Execute_CallsIStatementHooksOnInstances()
    {
        var from = new StateNode(typeof(SimpleStatement));
        var to = new StateNode(typeof(SimpleStatement));
        var fromInstance = (SimpleStatement)from.GetOrCreateInstance();
        var toInstance = (SimpleStatement)to.GetOrCreateInstance();

        _executor.Execute(new Transition(from, to), _machine, () => { });

        Assert.That(fromInstance.OnExitCalled, Is.True);
        Assert.That(toInstance.OnEntryCalled, Is.True);
        Assert.That(fromInstance.OnEntryCalled, Is.False);
        Assert.That(toInstance.OnExitCalled, Is.False);
    }

    [Test]
    public void Execute_NonStatementStates_DoesNotThrow()
    {
        var from = new StateNode(typeof(SimpleUnitTestState));
        var to = new StateNode(typeof(AdvancedUnitTestState));

        Assert.DoesNotThrow(() => _executor.Execute(new Transition(from, to), _machine, () => { }));
    }

    [Test]
    public void Execute_InvokesTransitionCallbackWithFullInformation()
    {
        var from = new StateNode(typeof(SimpleUnitTestState));
        var to = new StateNode(typeof(AdvancedUnitTestState));
        var trigger = "go";
        TransitionInformation? captured = null;
        _machine.AddTransitionCallbacks(info => captured = info);

        _executor.Execute(new Transition(from, to, trigger), _machine, () => { });

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Value.FromType, Is.EqualTo(typeof(SimpleUnitTestState)));
        Assert.That(captured.Value.ToType, Is.EqualTo(typeof(AdvancedUnitTestState)));
        Assert.That(captured.Value.Trigger, Is.EqualTo(trigger));
        Assert.That(captured.Value.From, Is.TypeOf<SimpleUnitTestState>());
        Assert.That(captured.Value.To, Is.TypeOf<AdvancedUnitTestState>());
    }

    [Test]
    public void Execute_FromNull_CallbackHasNullFrom()
    {
        var to = new StateNode(typeof(SimpleUnitTestState));
        TransitionInformation? captured = null;
        _machine.AddTransitionCallbacks(info => captured = info);

        _executor.Execute(new Transition(null, to), _machine, () => { });

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Value.From, Is.Null);
        Assert.That(captured.Value.FromType, Is.Null);
        Assert.That(captured.Value.ToType, Is.EqualTo(typeof(SimpleUnitTestState)));
    }
}
