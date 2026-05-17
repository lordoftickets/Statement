using System;

namespace Statement.Failures;

/// <summary>
/// Thrown when a <see cref="StateMachine"/> cannot be built because its configuration is incomplete or invalid.
/// The most common cause is calling <c>Build()</c> or <c>BuildTyped()</c> without first specifying an initial
/// state via <c>StartIn&lt;TState&gt;()</c> — every machine must start in a known state, so this is enforced
/// at build time rather than surfacing as a null current-state at runtime.
/// </summary>
public class MachineSetupException(string message) : Exception(message);