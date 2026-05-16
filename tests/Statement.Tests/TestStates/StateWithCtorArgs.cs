namespace Builder.Tests.TestStates;

public class StateWithCtorArgs : IUnitTestState
{
    public StateWithCtorArgs(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public void Test() { }
}
