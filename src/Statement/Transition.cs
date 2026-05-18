namespace Statement;

internal readonly struct Transition
{
    internal StateNode? From { get; }
    internal StateNode To { get; }
    internal object? Trigger { get; }
    internal object? Payload { get; }

    internal Transition(StateNode? from, StateNode to, object? trigger = null, object? payload = null)
    {
        From = from;
        To = to;
        Trigger = trigger;
        Payload = payload;
    }

    internal object? FromInstance => From?.GetOrCreateInstance();
    internal object ToInstance => To.GetOrCreateInstance();
}
