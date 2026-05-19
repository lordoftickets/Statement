namespace Statement.State;

/// <summary>
/// Defines synchronous lifecycle callbacks for a state.
/// Implement this interface on state classes to automatically run code when the machine transitions
/// into or out of the state, without needing to register callbacks via the fluent builder API.
/// </summary>
/// <remarks>
/// <para>
/// When a state implements <see cref="IStatement"/>, its lifecycle methods are invoked as follows:
/// <list type="bullet">
/// <item><see cref="OnExit"/> is called immediately before the machine leaves the state (after all guards pass).</item>
/// <item><see cref="OnEntry"/> is called immediately after the machine enters the state (after global transition callbacks).</item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="IStatement"/> when:
/// <list type="bullet">
/// <item>Your state class needs synchronous setup/teardown logic that is independent of the builder configuration.</item>
/// <item>You prefer encapsulating state behavior within the state class itself rather than registering callbacks externally.</item>
/// <item>The initial state is called during <see cref="Fluent.Api.StateMachineBuilder{TBase}.Build"/>, which requires synchronous callbacks.</item>
/// </list>
/// </para>
/// <para>
/// For asynchronous state lifecycle, implement <see cref="IAsyncStatement"/> instead, and use <see cref="Fluent.Api.StateMachineBuilder{TBase}.BuildAsync"/>
/// to build the machine so async initialization completes before the machine is used.
/// </para>
/// </remarks>
public interface IStatement
{
    /// <summary>
    /// Called when the machine transitions into this state, after global transition callbacks fire.
    /// </summary>
    void OnEntry();

    /// <summary>
    /// Called when the machine transitions out of this state, before the target state's OnEntry.
    /// </summary>
    void OnExit();
}