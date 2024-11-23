using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace OtterGui.Services;

/// <summary> A utility to asynchronously create hooks, and dispose of them. </summary>
public sealed class HookManager(IGameInteropProvider _provider) : IDisposable, IService
{
    private readonly ConcurrentDictionary<string, (IDalamudHook, long)> _hooks = [];
    private          Task?                                              _currentTask;
    private          bool                                               _disposed;

    public IEnumerable<(string Name, nint Address, long Time, Type Delegate)> Diagnostics
        => _disposed
            ? []
            : _hooks.Select(
                kvp => (kvp.Key, kvp.Value.Item1.Address, kvp.Value.Item2, kvp.Value.Item1.GetType().GenericTypeArguments[0]));

    /// <summary> Create a hook for a given address. </summary>
    public Task<Hook<T>> CreateHook<T>(string name, nint address, T detour, bool enable = false) where T : Delegate
    {
        CheckDisposed();
        return AppendTask(Func);

        Hook<T> Func()
        {
            var timer = Stopwatch.StartNew();
            var hook  = _provider.HookFromAddress(address, detour);
            if (enable)
                hook.Enable();
            AddHook(name, hook, timer);
            return hook;
        }
    }

    /// <summary> Create a hook from a given signature. </summary>
    public Task<Hook<T>> CreateHook<T>(string name, string signature, T detour, bool enable = false) where T : Delegate
    {
        CheckDisposed();
        return AppendTask(Func);

        Hook<T> Func()
        {
            var timer = Stopwatch.StartNew();
            var hook  = _provider.HookFromSignature(signature, detour);
            if (enable)
                hook.Enable();
            AddHook(name, hook, timer);
            return hook;
        }
    }

    /// <summary> Try to replace an existing hook with a different detour. </summary>
    public Task<Hook<T>?> TryReplaceHook<T>(string name, T detour) where T : Delegate
    {
        CheckDisposed();
        return AppendTask(Func);

        Hook<T>? Func()
        {
            var timer = Stopwatch.StartNew();
            if (!_hooks.TryRemove(name, out var oldHook))
                return null;

            var enabled = oldHook.Item1.IsEnabled;
            oldHook.Item1.Dispose();
            var newHook = _provider.HookFromAddress(oldHook.Item1.Address, detour);
            if (enabled)
                newHook.Enable();
            AddHook(name, newHook, timer);
            return newHook;
        }
    }

    /// <summary> Dispose an existing hook. </summary>
    public Task<bool> DisposeHook(string name)
    {
        CheckDisposed();
        return AppendTask(Func);

        bool Func()
        {
            if (!_hooks.TryRemove(name, out var hook))
                return false;

            hook.Item1.Dispose();
            return true;
        }
    }

    /// <summary> Append a new hooking task to the current task. </summary>
    private Task<T> AppendTask<T>(Func<T> func)
    {
        Task<T> task;
        lock (_hooks)
        {
            task = _currentTask == null || _currentTask.IsCompleted
                ? Task.Run(func)
                : _currentTask.ContinueWith(_ => func(), TaskScheduler.Default);
            _currentTask = task;
        }

        return task;
    }

    /// <summary> Check whether the hook manager was disposed already. </summary>
    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HookManager));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_hooks)
        {
            _currentTask?.Wait();
            _disposed = true;
            foreach (var (_, hook) in _hooks)
                hook.Item1.Dispose();
            _hooks.Clear();
            _currentTask = null;
        }
    }

    /// <summary> Add the hook and throw on failure. </summary>
    private void AddHook(string name, IDalamudHook hook, Stopwatch timer)
    {
        if (!_hooks.TryAdd(name, (hook, timer.ElapsedMilliseconds)))
            throw new Exception($"A hook with the name of {name} already exists.");
    }
}
