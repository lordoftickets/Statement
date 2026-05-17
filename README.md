# Statement

[![CI](https://github.com/Jhanmn/Statement/actions/workflows/ci.yml/badge.svg)](https://github.com/Jhanmn/Statement/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/Jhanmn/Statement)](License)
[![.NET Standard 2.0](https://img.shields.io/badge/target-.NET%20Standard%202.0-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

A lightweight, type-driven state machine library for .NET.

In Statement, **each state is its own class**. Transitions are expressed by switching the machine's current state type, and entry/exit behavior lives either on the state itself (via `IStatement`) or on the machine through a fluent builder API. Optional base-type constraints let you guarantee at compile time that every state in a machine implements a common interface or inherits from a common base class.

## Features

- States as first-class types — no string identifiers, no enums.
- Fluent `StateMachineBuilder` API with `OnEntry` / `OnExit` / `CannotTransitionTo` rules.
- Optional typed machines (`StateMachineBuilder.For<TBase>()`) for compile-time safety.
- Register states by type (auto-instantiated) or by pre-built instance (for states with constructor arguments).
- Built-in `IStatement` interface for states that prefer to own their own entry/exit logic.
- Global transition callbacks via `AddOnStateChangedCallback` for cross-cutting concerns like logging.
- Mandatory initial state — `StartIn<TState>()` must be configured before `Build()`, otherwise a `MachineSetupException` is thrown. This guarantees the machine is never observed in a null state.

## Quick start

Install the project as a reference (NuGet package coming later) and define your states:

```csharp
// States are just plain classes — implementing IStatement is optional.
public class Idle { }

// Implement IStatement only if the state wants to own its entry/exit logic.
public class Running : IStatement
{
    public void OnEntry() => Console.WriteLine("started");
    public void OnExit()  => Console.WriteLine("stopped");
}
```

Build a machine and drive it by type:

```csharp
using Statement.Fluent.Api;

var machine = StateMachineBuilder.New()
    .AddState<Idle>()
    .AddState<Running>(s => s.CannotTransitionTo<Idle>()) // example rule
    .StartIn<Idle>()
    .Build();

machine.SetCurrentState<Running>();          // fires Running.OnEntry
var current = machine.GetCurrentState();     // returns the Running instance
```

### Forbidding multiple transition targets

Chain `CannotTransitionTo<T>()` to forbid more than one next state from the same source:

```csharp
var machine = StateMachineBuilder.New()
    .AddState<Running>(s => s
        .CannotTransitionTo<Idle>()
        .CannotTransitionTo<Faulted>())
    .AddState<Idle>()
    .AddState<Faulted>()
    .StartIn<Running>()
    .Build();
```

Attempts to switch into any forbidden target while `Running` is active are silently ignored by default. See the next section to change that.

### Handling failed transitions

By default, transitions blocked by a `CannotTransitionTo` rule are silently ignored. Configure a different policy via `OnTransitionFailure`:

```csharp
using Statement.Failures;

// Throw on blocked transitions
var machine = StateMachineBuilder.New()
    .OnTransitionFailure(TransitionFailurePolicy.Throw)
    .AddState<Running>(s => s.CannotTransitionTo<Idle>())
    .AddState<Idle>()
    .StartIn<Running>()
    .Build();

// Or run a custom callback
var machine2 = StateMachineBuilder.New()
    .OnTransitionFailure(TransitionFailurePolicy.Invoke(info =>
        Console.WriteLine($"blocked: {info.From?.Name} -> {info.To.Name}")))
    .AddState<Running>(s => s.CannotTransitionTo<Idle>())
    .AddState<Idle>()
    .StartIn<Running>()
    .Build();
```

Attempting to switch to a state that was never registered always throws `InvalidOperationException`, regardless of the configured policy.

### Observing every transition

Register a global callback to be notified whenever the machine moves to a new state. The callback runs after the previous state's `OnExit` and the current-state commit, but before the new state's `OnEntry`. Exceptions thrown from callbacks are swallowed so they cannot crash the machine.

```csharp
var machine = StateMachineBuilder.New()
    .AddOnStateChangedCallback(info =>
        Console.WriteLine($"{info.FromType?.Name} -> {info.ToType?.Name}"))
    .AddState<Idle>()
    .AddState<Running>()
    .StartIn<Idle>()
    .Build();
```

`AddOnStateChangedCallback` can be called multiple times to register more than one observer.

### Typed machine with a shared base type

```csharp
var machine = StateMachineBuilder.For<IMyState>()
    .AddState<Connecting>()
    .AddState<Connected>()
    .StartIn<Connecting>()
    .BuildTyped();   // StateMachine<IMyState>

IMyState state = machine.GetCurrentState<IMyState>();
```

### Pre-built state instances

For states that need constructor arguments or dependencies:

```csharp
var configured = new WithConfig("hello");

var machine = StateMachineBuilder.New()
    .AddState<WithConfig>(configured)
    .StartIn<WithConfig>()
    .Build();
```

### Initial state is required

Every machine must declare its initial state at build time via `StartIn<TState>()`. Calling `Build()` or `BuildTyped()` without it throws `MachineSetupException`:

```csharp
using Statement.Failures;

try
{
    var machine = StateMachineBuilder.New()
        .AddState<Idle>()
        .Build();   // throws: missing StartIn<T>()
}
catch (MachineSetupException ex)
{
    Console.WriteLine(ex.Message);
}
```

This makes the "no current state" case impossible at runtime — `GetCurrentState()` will never return `null` on a freshly built machine, and the configured `OnEntry` of the initial state runs as part of `Build()`.

## Examples

For more usage patterns — entry/exit callbacks, transition rules, typed machines, pre-built instances, and `IStatement` states — see the unit tests under [tests/Statement.Tests](tests/Statement.Tests).

## License

See [License](License).
