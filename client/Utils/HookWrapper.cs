using Dalamud.Hooking;

namespace GagSpeak.Utils;

// Interface for a hook wrapper with basic lifecycle methods
// this is almost barely ever used. perhaps find a way to remove it? There are better ways.

public interface IHookWrapper : IDisposable
{
    public void Enable(); // Method to enable the hook
    public void Disable(); // Method to disable the hook
    public bool IsEnabled { get; } // Property to check if the hook is enabled
    public bool IsDisposed { get; } // Property to check if the hook is disposed
}

/// <summary> This class is used to wrap a hook. </summary>
public class HookWrapper<T> : IHookWrapper where T : Delegate
{
    private Hook<T> wrappedHook;    // The hook that is being wrapped
    private bool disposed;       // Flag to track if the hook has been disposed

    // Constructor that takes a hook to wrap
    public HookWrapper(Hook<T> hook)
    {
        this.wrappedHook = hook;
    }

    /// <summary> This function is used to create a new hook wrapper. </summary>
    public void Enable()
    {
        if (disposed) return;
        wrappedHook?.Enable();
    }

    /// <summary> This function is used to disable the hook. </summary>
    public void Disable()
    {
        if (disposed) return;
        wrappedHook?.Disable();
    }

    /// <summary> This function is used to dispose of the hook. </summary>
    public void Dispose()
    {
        Disable();
        disposed = true;
        wrappedHook?.Dispose();
    }

    // Property to get the address of the hook
    public nint Address => wrappedHook.Address;

    // Property to get the original delegate of the hook
    public T Original => wrappedHook.Original;

    // Property to check if the hook is enabled
    public bool IsEnabled => wrappedHook.IsEnabled;

    // Property to check if the hook is disposed
    public bool IsDisposed => wrappedHook.IsDisposed;
}
