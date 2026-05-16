namespace Statement;

public interface IStateMachine
{
    void SetCurrentState<T>();
    T GetCurrentState<T>() where T : class;
    object? GetCurrentState();
    bool TryGetCurrentState<T>(out T? result) where T : class;
}