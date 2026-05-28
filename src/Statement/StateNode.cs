using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Statement.Rules;
using Statement.Triggers;

namespace Statement;

internal sealed class StateNode(Type type, object? preBuiltInstance = null)
{
    private object? _instance = preBuiltInstance;
    
    internal Type Type { get; } = type;
    internal bool IsActive { get; set; } = true; // states are active by default. deactivation is opt in
    internal Func<StateMachine, object?, CancellationToken, Task>? OnEntry { get; set; }
    internal Func<StateMachine, object?, CancellationToken, Task>? OnExit { get; set; }
    internal TransitionRule? TransitionRule { get; set; }
    internal Dictionary<object, TriggerHandler> Triggers { get; } = new();

    internal object GetOrCreateInstance()
        => _instance ??= Activator.CreateInstance(Type)
           ?? throw new InvalidOperationException($"Cannot instantiate {Type}");

    internal void PreInstantiate() => GetOrCreateInstance();
}
