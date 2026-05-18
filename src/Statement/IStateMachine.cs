namespace Statement;

public interface IStateMachine
{
    void SetCurrentState<T>();
    void SetCurrentState<T>(object? payload);
    T GetCurrentState<T>() where T : class;
    object? GetCurrentState();
    bool TryGetCurrentState<T>(out T? result) where T : class;
    void Fire(object trigger);
    void Fire(object trigger, object? payload);
}
