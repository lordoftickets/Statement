namespace Statement;

/// <summary>
/// A strongly-typed <see cref="StateMachine"/> whose registered states all derive from or implement <typeparamref name="T"/>.
/// Provides typed overloads of the state accessors so callers don't have to specify the common base type at every call site.
/// </summary>
/// <typeparam name="T">The common base type or interface shared by every state registered on this machine.</typeparam>
public class StateMachine<T> : StateMachine where T : class
{
    internal StateMachine() { }

    /// <summary>
    /// Returns the current state instance as <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if no state is currently set.</exception>
    public new T GetCurrentState() => GetCurrentState<T>();

    /// <summary>
    /// Attempts to get the current state instance as <typeparamref name="T"/>.
    /// </summary>
    /// <param name="result">When this method returns <c>true</c>, contains the current state instance; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a current state is set; otherwise <c>false</c>.</returns>
    public bool TryGetCurrentState(out T? result) => TryGetCurrentState<T>(out result);

    /// <summary>
    /// Transitions the machine to the registered state of type <typeparamref name="TState"/>.
    /// The transition is silently ignored if the target state is not registered or is blocked by a transition rule.
    /// </summary>
    /// <typeparam name="TState">The state type to switch to. Must derive from or implement <typeparamref name="T"/>.</typeparam>
    public new void SetCurrentState<TState>() where TState : T => SetCurrentStateByType(typeof(TState));
}
