using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Penumbra.Api.Helpers;

/// <summary>
/// Specialized disposable Provider for Actions.
/// </summary>
public sealed class ActionProvider : IDisposable
{
    private ICallGateProvider<object?>? _provider;

    public ActionProvider(IDalamudPluginInterface pi, string label, Action action)
    {
        try
        {
            _provider = pi.GetIpcProvider<object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }

        _provider?.RegisterAction(action);
    }

    public void Dispose()
    {
        _provider?.UnregisterAction();
        _provider = null;
        GC.SuppressFinalize(this);
    }

    ~ActionProvider()
        => Dispose();
}

/// <summary>
/// Specialized disposable Provider for Actions.
/// </summary>
public sealed class ActionProvider<T1> : IDisposable
{
    private ICallGateProvider<T1, object?>? _provider;

    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1> action)
    {
        try
        {
            _provider = pi.GetIpcProvider<T1, object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }

        _provider?.RegisterAction(action);
    }

    public void Dispose()
    {
        _provider?.UnregisterAction();
        _provider = null;
        GC.SuppressFinalize(this);
    }

    ~ActionProvider()
        => Dispose();
}

/// <summary>
/// <inheritdoc cref="ActionProvider{T1}"/>
/// </summary>
public sealed class ActionProvider<T1, T2> : IDisposable
{
    private ICallGateProvider<T1, T2, object?>? _provider;

    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2> action)
    {
        try
        {
            _provider = pi.GetIpcProvider<T1, T2, object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }

        _provider?.RegisterAction(action);
    }

    public void Dispose()
    {
        _provider?.UnregisterAction();
        _provider = null;
        GC.SuppressFinalize(this);
    }

    ~ActionProvider()
        => Dispose();
}

/// <summary>
/// <inheritdoc cref="ActionProvider{T1}"/>
/// </summary>
public sealed class ActionProvider<T1, T2, T3> : IDisposable
{
    private ICallGateProvider<T1, T2, T3, object?>? _provider;

    public ActionProvider(IDalamudPluginInterface pi, string label, Action<T1, T2, T3> action)
    {
        try
        {
            _provider = pi.GetIpcProvider<T1, T2, T3, object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Provider for {label}\n{e}");
            _provider = null;
        }

        _provider?.RegisterAction(action);
    }

    public void Dispose()
    {
        _provider?.UnregisterAction();
        _provider = null;
        GC.SuppressFinalize(this);
    }

    ~ActionProvider()
        => Dispose();
}
