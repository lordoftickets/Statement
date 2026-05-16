namespace Statement;

internal readonly struct Transition
{
    internal StateNode? From { get; }
    internal StateNode To { get; }

    internal Transition(StateNode? from, StateNode to)
    {
        From = from;
        To = to;
    }

    internal object? FromInstance => From?.GetOrCreateInstance();
    internal object ToInstance => To.GetOrCreateInstance();
}
