using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Penumbra.Api.Helpers;

/// <summary>
/// Specialized subscriber only allowing to invoke actions.
/// </summary>
public class ActionSubscriber
{
    private readonly ICallGateSubscriber<object?>? _subscriber;

    /// <summary> Whether the subscriber could successfully be created. </summary>
    public bool Valid
        => _subscriber != null;

    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            _subscriber = pi.GetIpcSubscriber<object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Subscriber for {label}\n{e}");
            _subscriber = null;
        }
    }

    /// <summary> Invoke the action. See the source of the subscriber for details.</summary>
    protected void Invoke()
        => _subscriber?.InvokeAction();
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1>
{
    private readonly ICallGateSubscriber<T1, object?>? _subscriber;

    /// <summary> Whether the subscriber could successfully be created. </summary>
    public bool Valid
        => _subscriber != null;

    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            _subscriber = pi.GetIpcSubscriber<T1, object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Subscriber for {label}\n{e}");
            _subscriber = null;
        }
    }

    /// <summary> Invoke the action. See the source of the subscriber for details.</summary>
    protected void Invoke(T1 a)
        => _subscriber?.InvokeAction(a);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2>
{
    private readonly ICallGateSubscriber<T1, T2, object?>? _subscriber;

    /// <inheritdoc cref="ActionSubscriber{T1}.Valid"/> 
    public bool Valid
        => _subscriber != null;

    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            _subscriber = pi.GetIpcSubscriber<T1, T2, object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Subscriber for {label}\n{e}");
            _subscriber = null;
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/> 
    protected void Invoke(T1 a, T2 b)
        => _subscriber?.InvokeAction(a, b);
}

/// <inheritdoc cref="ActionSubscriber"/> 
public class ActionSubscriber<T1, T2, T3>
{
    private readonly ICallGateSubscriber<T1, T2, T3, object?>? _subscriber;

    /// <inheritdoc cref="ActionSubscriber{T1}.Valid"/> 
    public bool Valid
        => _subscriber != null;

    protected ActionSubscriber(IDalamudPluginInterface pi, string label)
    {
        try
        {
            _subscriber = pi.GetIpcSubscriber<T1, T2, T3, object?>(label);
        }
        catch (Exception e)
        {
            PluginLogHelper.WriteError(pi, $"Error registering IPC Subscriber for {label}\n{e}");
            _subscriber = null;
        }
    }

    /// <inheritdoc cref="ActionSubscriber.Invoke"/> 
    protected void Invoke(T1 a, T2 b, T3 c)
        => _subscriber?.InvokeAction(a, b, c);
}
