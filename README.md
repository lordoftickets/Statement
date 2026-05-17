# Statement

[![CI](https://github.com/Jhanmn/Statement/actions/workflows/ci.yml/badge.svg)](https://github.com/Jhanmn/Statement/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Jhanmn.Statement?logo=nuget)](https://www.nuget.org/packages/Jhanmn.Statement)
[![NuGet Pre-release](https://img.shields.io/nuget/vpre/Jhanmn.Statement?logo=nuget&label=nuget%20%28pre%29)](https://www.nuget.org/packages/Jhanmn.Statement)
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
- Trigger-based transitions — declare `On<TTrigger>().GoTo<TTarget>()` per state and drive the machine via `machine.Fire(trigger)`. Supports marker types, enums, strings, or any value. Guards (`If`), payload-receiving side-effects (`Do`), and internal transitions (`Ignore`) are first-class.

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

### Trigger-driven transitions

Instead of (or alongside) calling `SetCurrentState<T>()`, you can declare named triggers per state and `Fire()` them. The current state owns the routing, so the same trigger can mean different things in different states.

```csharp
public sealed record Open;
public sealed record Close;
public sealed record Lock(string KeyId);

public class Closed { }
public class Opened { }
public class Locked { }

var machine = StateMachineBuilder.New()
    .AddState<Closed>(s => s
        .On<Open>().GoTo<Opened>()
        .On<Lock>().If(() => hasKey).Do(t => Audit(t.KeyId)).GoTo<Locked>())
    .AddState<Opened>(s => s
        .On<Close>().GoTo<Closed>())
    .AddState<Locked>(s => s
        .On<Open>().Ignore())          // valid here, but does nothing
    .StartIn<Closed>()
    .Build();

machine.Fire(new Open());              // Closed -> Opened
machine.Fire(new Close());             // Opened -> Closed
machine.Fire(new Lock("k1"));          // Closed -> Locked (guard checks `hasKey`)
machine.Fire(new Open());              // ignored — no callbacks fire
```

The `TriggerBuilder` fragment supports:

| Call | Effect |
|---|---|
| `.GoTo<TTarget>()` | Transition to the target state. |
| `.Ignore()` | Internal transition: consume the trigger without firing `OnExit` / `OnEntry`. |
| `.If(Func<bool>)` | Guard the transition. Failed guards route through `TriggerFailurePolicy`. |
| `.Do(Action<TTrigger>)` | Side-effect that runs after the guard passes, before `OnExit`. Receives the trigger value (payload). |

Triggers can be any non-null object: marker-type instances (`new Open()`), enum values (`On(DoorTrigger.Open)`), strings (`On("open")`), or even a `Type`. Use whatever fits your domain.

When `Fire(...)` finds no handler on the current state or a guard returns `false`, the configured `TriggerFailurePolicy` decides what happens — `Silent` (default), `Throw` (raises `TriggerFailedException`), or `Invoke(callback)` to receive a `TriggerFailureInfo`:

```csharp
var machine = StateMachineBuilder.New()
    .OnTriggerFailure(TriggerFailurePolicy.Throw)
    .AddState<Closed>(s => s.On<Open>().GoTo<Opened>())
    .AddState<Opened>()
    .StartIn<Closed>()
    .Build();

machine.Fire(new Close());   // throws TriggerFailedException (NoHandler)
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
