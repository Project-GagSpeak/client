using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

namespace Glamourer.Api.Helpers;

/// <summary>
/// Specialized disposable Provider for Events.<para />
/// Will execute the unsubscriber action on dispose if any is provided.<para />
/// Can only be invoked and disposed.
/// </summary>
public sealed class EventProvider : IDisposable
{
    private readonly IPluginLog                  _log;
    private          ICallGateProvider<object?>? _provider;
    private          Delegate?                   _unsubscriber;

    public EventProvider(IDalamudPluginInterface pi, string label, (Action<Action> Add, Action<Action> Del)? subscribe = null)
    {
        _unsubscriber = null;
        _log          = PluginLogHelper.GetLog(pi);
        try
        {
            _provider = pi.GetIpcProvider<object?>(label);
            subscribe?.Add(Invoke);
            _unsubscriber = subscribe?.Del;
        }
        catch (Exception e)
        {
            _log.Error($"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }
    }

    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider> add, Action<EventProvider> del)
    {
        _unsubscriber = null;
        _log          = PluginLogHelper.GetLog(pi);
        try
        {
            _provider = pi.GetIpcProvider<object?>(label);
            add(this);
            _unsubscriber = del;
        }
        catch (Exception e)
        {
            _log.Error($"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }
    }

    /// <summary> Invoke the event.</summary>
    public void Invoke()
    {
        try
        {
            _provider?.SendMessage();
        }
        catch (Exception e)
        {
            _log.Error($"Exception thrown on IPC event:\n{e}");
        }
    }

    public void Dispose()
    {
        switch (_unsubscriber)
        {
            case Action<Action> a:
                a(Invoke);
                break;
            case Action<EventProvider> b:
                b(this);
                break;
        }

        _unsubscriber = null;
        _provider     = null;
        GC.SuppressFinalize(this);
    }

    ~EventProvider()
        => Dispose();
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1> : IDisposable
{
    private readonly IPluginLog                      _log;
    private          ICallGateProvider<T1, object?>? _provider;
    private          Delegate?                       _unsubscriber;

    public EventProvider(IDalamudPluginInterface pi, string label, (Action<Action<T1>> Add, Action<Action<T1>> Del)? subscribe = null)
    {
        _unsubscriber = null;
        _log          = PluginLogHelper.GetLog(pi);
        try
        {
            _provider = pi.GetIpcProvider<T1, object?>(label);
            subscribe?.Add(Invoke);
            _unsubscriber = subscribe?.Del;
        }
        catch (Exception e)
        {
            _log.Error($"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }
    }

    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1>> add, Action<EventProvider<T1>> del)
    {
        _unsubscriber = null;
        _log          = PluginLogHelper.GetLog(pi);
        try
        {
            _provider = pi.GetIpcProvider<T1, object?>(label);
            add(this);
            _unsubscriber = del;
        }
        catch (Exception e)
        {
            _log.Error($"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }
    }

    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a)
    {
        try
        {
            _provider?.SendMessage(a);
        }
        catch (Exception e)
        {
            _log.Error($"Exception thrown on IPC event:\n{e}");
        }
    }

    public void Dispose()
    {
        switch (_unsubscriber)
        {
            case Action<Action<T1>> a:
                a(Invoke);
                break;
            case Action<EventProvider<T1>> b:
                b(this);
                break;
        }

        _unsubscriber = null;
        _provider     = null;
        GC.SuppressFinalize(this);
    }

    ~EventProvider()
        => Dispose();
}

/// <inheritdoc cref="EventProvider"/>
public sealed class EventProvider<T1, T2> : IDisposable
{
    private readonly IPluginLog                          _log;
    private          ICallGateProvider<T1, T2, object?>? _provider;
    private          Delegate?                           _unsubscriber;

    public EventProvider(IDalamudPluginInterface pi, string label, (Action<Action<T1, T2>> Add, Action<Action<T1, T2>> Del)? subscribe = null)
    {
        _unsubscriber = null;
        _log          = PluginLogHelper.GetLog(pi);
        try
        {
            _provider = pi.GetIpcProvider<T1, T2, object?>(label);
            subscribe?.Add(Invoke);
            _unsubscriber = subscribe?.Del;
        }
        catch (Exception e)
        {
            _log.Error($"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }
    }

    public EventProvider(IDalamudPluginInterface pi, string label, Action<EventProvider<T1, T2>> add, Action<EventProvider<T1, T2>> del)
    {
        _unsubscriber = null;
        _log          = PluginLogHelper.GetLog(pi);
        try
        {
            _provider = pi.GetIpcProvider<T1, T2, object?>(label);
            add(this);
            _unsubscriber = del;
        }
        catch (Exception e)
        {
            _log.Error($"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }
    }

    /// <inheritdoc cref="EventProvider.Invoke"/>
    public void Invoke(T1 a, T2 b)
    {
        try
        {
            _provider?.SendMessage(a, b);
        }
        catch (Exception e)
        {
            _log.Error($"Exception thrown on IPC event:\n{e}");
        }
    }

    public void Dispose()
    {
        switch (_unsubscriber)
        {
            case Action<Action<T1, T2>> a:
                a(Invoke);
                break;
            case Action<EventProvider<T1, T2>> b:
                b(this);
                break;
        }

        _unsubscriber = null;
        _provider     = null;
        GC.SuppressFinalize(this);
    }

    ~EventProvider()
        => Dispose();
}
