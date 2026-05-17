namespace Statement;

internal readonly struct Transition
{
    internal StateNode? From { get; }
    internal StateNode To { get; }
    internal object? Trigger { get; }

    internal Transition(StateNode? from, StateNode to, object? trigger = null)
    {
        From = from;
        To = to;
        Trigger = trigger;
    }

    internal object? FromInstance => From?.GetOrCreateInstance();
    internal object ToInstance => To.GetOrCreateInstance();
}
