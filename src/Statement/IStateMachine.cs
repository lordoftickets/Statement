using System.Threading.Tasks;

namespace Statement;

public interface IStateMachine
{
    void SetCurrentState<T>();
    void SetCurrentState<T>(object? payload);
    Task SetCurrentStateAsync<T>();
    Task SetCurrentStateAsync<T>(object? payload);
    T GetCurrentState<T>() where T : class;
    object? GetCurrentState();
    bool TryGetCurrentState<T>(out T? result) where T : class;
    void Fire(object trigger);
    void Fire(object trigger, object? payload);
    Task FireAsync(object trigger);
    Task FireAsync(object trigger, object? payload);
}
