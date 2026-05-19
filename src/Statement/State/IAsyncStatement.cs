using System.Threading.Tasks;

namespace Statement.State;

/// <summary>
/// Defines asynchronous lifecycle callbacks for a state.
/// Implement this interface on state classes to automatically run async code (I/O, delays, etc.)
/// when the machine transitions into or out of the state, without needing to register callbacks via the builder.
/// </summary>
/// <remarks>
/// <para>
/// When a state implements <see cref="IAsyncStatement"/>, its async lifecycle methods are invoked as follows:
/// <list type="bullet">
/// <item><see cref="OnExitAsync"/> is awaited immediately before the machine leaves the state (after all guards pass).</item>
/// <item><see cref="OnEntryAsync"/> is awaited immediately after the machine enters the state (after global transition callbacks).</item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="IAsyncStatement"/> when:
/// <list type="bullet">
/// <item>Your state needs to perform I/O, network calls, or other async work during entry or exit.</item>
/// <item>You prefer encapsulating async state behavior within the state class itself rather than registering callbacks externally.</item>
/// <item>You are using <see cref="Fluent.Api.StateMachineBuilder{TBase}.SetCurrentStateAsync{T}"/> or <see cref="Fluent.Api.StateMachineBuilder{TBase}.FireAsync(object)"/> to transition asynchronously.</item>
/// </list>
/// </para>
/// <para>
/// Important: When a state implements <see cref="IAsyncStatement"/> and is the initial state, you must use
/// <see cref="Fluent.Api.StateMachineBuilder{TBase}.BuildAsync"/> instead of <see cref="Fluent.Api.StateMachineBuilder{TBase}.Build"/>.
/// The sync <see cref="Fluent.Api.StateMachineBuilder{TBase}.Build"/> will throw <see cref="System.InvalidOperationException"/> if
/// async callbacks are present on the initial state.
/// </para>
/// <para>
/// A state can implement both <see cref="IStatement"/> and <see cref="IAsyncStatement"/>, in which case
/// both the sync and async methods are invoked: sync <see cref="IStatement.OnEntry"/> first,
/// then async <see cref="OnEntryAsync"/>, and likewise for exit.
/// </para>
/// </remarks>
public interface IAsyncStatement
{
    /// <summary>
    /// Asynchronously invoked when the machine transitions into this state, after global transition callbacks fire.
    /// </summary>
    /// <returns>A task representing the asynchronous entry operation.</returns>
    Task OnEntryAsync();

    /// <summary>
    /// Asynchronously invoked when the machine transitions out of this state, before the target state's OnEntry.
    /// </summary>
    /// <returns>A task representing the asynchronous exit operation.</returns>
    Task OnExitAsync();
}
