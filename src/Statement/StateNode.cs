using System;
using Statement.Rules;

namespace Statement;

internal sealed class StateNode(Type type)
{
    private object? _instance;

    internal Type Type { get; } = type;
    internal Action<StateMachine>? OnEntry { get; set; }
    internal Action<StateMachine>? OnExit { get; set; }
    internal TransitionRule? TransitionRule { get; set; }

    internal object GetOrCreateInstance()
        => _instance ??= Activator.CreateInstance(Type)
           ?? throw new InvalidOperationException($"Cannot instantiate {Type}");

    internal void PreInstantiate() => GetOrCreateInstance();
}
