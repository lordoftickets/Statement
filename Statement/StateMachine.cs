using System;
using System.Collections.Generic;
using System.Linq;
using Statement.Rules;

namespace Statement;

public class StateMachine
{
    private readonly HashSet<RegisteredStateBundle> _registeredStates = [];
    private object? _currenState;
    private readonly List<object> _states = [];

    public void SetCurrentState<T>()
    {
        if (!CheckTransitionRule<T>(_currenState))
        {
            //forbidden transition
            return;
        }
        
        var state = _states.FirstOrDefault(s => s is T);
        if (state is not null)
        {
            var previousState = _currenState;
            _currenState = state;
            InvokeOnEntry(state, previousState);
            return;
        }
        else
        {
            var bundle = _registeredStates.FirstOrDefault(s => s.RegisteredState == typeof(T));
            if (bundle is null)
            {
                return;
            }

            var instance = Activator.CreateInstance(bundle.RegisteredState);
            _states.Add(instance);
            var previousState = _currenState;
            _currenState = instance;
            InvokeOnEntry(instance, previousState);
        }
    }

    public T GetCurrentState<T>() where T : class
    {
        if (_currenState is T state)
        {
            return state;
        }
        throw new InvalidOperationException();
    }
    
    public T? TryGetCurrentState<T>(out bool result) where T : class
    {
        if (_currenState is T state)
        {
            result = true;
            return state;
        }

        result = false;
        return null;
    }

    public object? GetCurrentState() => _currenState;

    internal void RegisterInnerState<TState>() where TState : class, new()
        => _registeredStates.Add(new RegisteredStateBundle(typeof(TState)));

    internal void AddOnEntry(Type stateType, Action<StateMachine> callback)
    {
        var bundle = _registeredStates.FirstOrDefault(s => s.RegisteredState == stateType);
        bundle?.OnEntryCallback = callback;
    }
    
    internal void AddOnExit(Type stateType, Action<StateMachine> callback)
    {
        var bundle = _registeredStates.FirstOrDefault(s => s.RegisteredState == stateType);
        bundle?.OnExitCallback = callback;
    }
    
    internal void AddExitRule(Type stateType, Type type)
    {
        var bundle = _registeredStates.FirstOrDefault(s => s.RegisteredState == stateType);
        if (bundle is null)
        {
            return;
        }
        if (bundle.TransitionRule is null)
        {
            bundle.TransitionRule = new TransitionRule();
        }

        bundle.TransitionRule.ForbiddenNextStates.Add(type);
    }

    internal void Compile()
    {
        foreach (var registeredStateBundle in _registeredStates)
        {
            var instance = Activator.CreateInstance(registeredStateBundle.RegisteredState);
            _states.Add(instance);
        }
    }

    private void InvokeOnEntry(object state, object? previousState)
    {
        var callback = _registeredStates.FirstOrDefault(s => s.RegisteredState == state.GetType())?.OnEntryCallback;
        callback?.Invoke(this);

        if (previousState is not null)
        {
            InvokeOnExit(previousState);
        }
    }
    
    private void InvokeOnExit(object state)
    {
        var callback = _registeredStates.FirstOrDefault(s => s.RegisteredState == state.GetType())?.OnExitCallback;
        callback?.Invoke(this);
    }

    private bool CheckTransitionRule<T>(object? state)
    {
        var bundle = _registeredStates.FirstOrDefault(s => s.RegisteredState == state?.GetType());
        if (bundle?.TransitionRule is null || state is null)
        {
            return true;
        }

        return !bundle.TransitionRule.ForbiddenNextStates.Contains(typeof(T));
    }
}