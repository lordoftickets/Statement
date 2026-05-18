using System;
using System.Collections.Generic;
using Statement.Rules;
using Statement.Triggers;

namespace Statement;

internal sealed class StateNode(Type type, object? preBuiltInstance = null)
{
    private object? _instance = preBuiltInstance;

    internal Type Type { get; } = type;
    internal Action<StateMachine, object?>? OnEntry { get; set; }
    internal Action<StateMachine>? OnExit { get; set; }
    internal TransitionRule? TransitionRule { get; set; }
    internal Dictionary<object, TriggerHandler> Triggers { get; } = new();

    internal object GetOrCreateInstance()
        => _instance ??= Activator.CreateInstance(Type)
           ?? throw new InvalidOperationException($"Cannot instantiate {Type}");

    internal void PreInstantiate() => GetOrCreateInstance();
}
