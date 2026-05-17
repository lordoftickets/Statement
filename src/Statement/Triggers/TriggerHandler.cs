using System;

namespace Statement.Triggers;

internal sealed class TriggerHandler
{
    internal Type? Target { get; }
    internal Func<bool>? Guard { get; }
    internal Action<object>? OnFire { get; }

    internal TriggerHandler(Type? target, Func<bool>? guard, Action<object>? onFire)
    {
        Target = target;
        Guard = guard;
        OnFire = onFire;
    }
}
