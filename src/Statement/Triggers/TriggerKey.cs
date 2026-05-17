using System;

namespace Statement.Triggers;

/// <summary>
/// Resolves a trigger value to the key used for dictionary lookup in the per-state trigger table.
/// </summary>
internal static class TriggerKey
{
    /// <summary>
    /// Returns the lookup key for a trigger value.
    /// </summary>
    /// <remarks>
    /// Rules:
    /// <list type="bullet">
    /// <item><description>If the trigger is itself a <see cref="Type"/>, the <see cref="Type"/> is the key.</description></item>
    /// <item><description>If the trigger is an <see cref="Enum"/> or <see cref="string"/>, the value is the key.</description></item>
    /// <item><description>If the trigger is any other value type, the value is the key.</description></item>
    /// <item><description>Otherwise the trigger's runtime <see cref="Type"/> is the key (marker-type pattern).</description></item>
    /// </list>
    /// </remarks>
    internal static object Of(object trigger)
    {
        if (trigger is null) throw new ArgumentNullException(nameof(trigger));
        if (trigger is Type t) return t;
        if (trigger is Enum) return trigger;
        if (trigger is string) return trigger;
        return trigger.GetType().IsValueType ? trigger : trigger.GetType();
    }
}
