using System;

namespace Statement;

/// <summary>
/// Snapshot of a transition delivered to global state-changed callbacks.
/// <see cref="Trigger"/> is non-null when the transition was caused by <c>Fire(...)</c>,
/// and null when caused by <c>SetCurrentState&lt;T&gt;()</c>.
/// </summary>
public record struct TransitionInformation(
    object? From,
    object? To,
    Type? FromType,
    Type? ToType,
    object? Trigger);
