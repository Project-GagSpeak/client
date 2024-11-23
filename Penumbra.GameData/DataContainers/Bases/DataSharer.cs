using System.Threading;
using Dalamud.Game;
using Dalamud.Plugin;
using OtterGui.Log;
using OtterGui.Services;

namespace Penumbra.GameData.DataContainers.Bases;

/// <summary> A service interacting with the Dalamud Data Share. </summary>
/// <typeparam name="T"> The type to be constructed in the Data Share. This can only contain types that Dalamud itself knows. </typeparam>
/// <param name="pluginInterface"> The plugin interface. </param>
/// <param name="log"> A logger. </param>
/// <param name="name"> The name and tag for the data share. </param>
/// <param name="language"> The client language used for the data share. </param>
/// <param name="version"> The version used for the data share. </param>
public abstract class DataSharer<T>(IDalamudPluginInterface pluginInterface, Logger log, string name, ClientLanguage language, int version)
    : IDisposable, IAsyncDataContainer
{
    /// <summary> All DataSharers store a task inside Dalamud. </summary>
    protected readonly Task<T> Task = null!;

    /// <summary> The timer measures how much time creating the shared data took for whichever plugin created it. </summary>
    private readonly Stopwatch _timer = null!;

    private T?   _value;
    private int  _totalCount = -1;
    private long _memory     = -1;

    /// <summary> Whether the service is ready and the data created. </summary>
    public bool Finished
        => Task.IsCompletedSuccessfully;

    protected DataSharer(IDalamudPluginInterface pluginInterface, Logger log, string name, ClientLanguage language, int version,
        Func<T> factory, Task? continuation = null)
        : this(pluginInterface, log, name, language, version)
        => (Task, _timer) = CreateTask(factory, continuation);

    /// <summary> The value created will be set if it is not by waiting for the task to finish. </summary>
    public T Value
        => _value ?? Task.Result;

    /// <summary> Obtain a generic awaiter that can be continued on or waited for. </summary>
    public Task Awaiter
        => Task;

    /// <summary> The tag is constructed from a prefix and name, language and version. </summary>
    private string Label
        => $"Penumbra.GameData.{Name}.{language}.V{version}";

    /// <summary> Obtain the time used to create the data in milliseconds. </summary>
    public long Time
        => _timer.ElapsedMilliseconds;

    /// <summary> Obtain the name of the data share. </summary>
    public string Name
        => name;

    /// <summary> Obtain the total count of items stored in the data share. </summary>
    public int TotalCount
        => _totalCount < 0 ? _totalCount = ComputeTotalCount() : _totalCount;

    /// <summary> Obtain an approximate amount of memory used by the data share. </summary>
    public long Memory
        => _memory < 0 ? _memory = ComputeMemory() : _memory;

    /// <summary> Compute the approximate memory used by this class. </summary>
    protected abstract long ComputeMemory();

    /// <summary> Compute the total count of items stored in this data share. </summary>
    protected abstract int ComputeTotalCount();

    /// <summary> Dispose this share by relinquishing its data in Dalamud. </summary>
    public void Dispose()
    {
        pluginInterface.RelinquishData(Label);
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary> Used if inheriting classes need to dispose resources themselves. </summary>
    protected virtual void Dispose(bool _)
    { }

    /// <summary> Create the task that constructs the data for this service.  </summary>
    /// <param name="factory"> The factory function to construct the data. </param>
    /// <param name="continuation"> Whether the function should only be executed after something else has finished. </param>
    /// <returns> The task running and the stopwatch that measures it. </returns>
    private Tuple<Task<T>, Stopwatch> CreateTask(Func<T> factory, Task? continuation = null)
    {
        return pluginInterface.GetOrCreateData(Label, Create);

        Tuple<Task<T>, Stopwatch> Create()
        {
            var stopwatch = new Stopwatch();
            var task = continuation == null
                ? System.Threading.Tasks.Task.Run(() =>
                {
                    log.Verbose($"[{Name}] Creating v{version} for {language} on thread {Thread.CurrentThread.ManagedThreadId}...");
                    stopwatch.Start();
                    var ret = factory();
                    stopwatch.Stop();
                    log.Verbose($"[{Name}] Created v{version} for {language} in {stopwatch.ElapsedMilliseconds} ms.");
                    _value = ret;
                    return ret;
                })
                : continuation.ContinueWith(_ =>
                {
                    log.Verbose($"[{Name}] Creating v{version} for {language} on thread {Thread.CurrentThread.ManagedThreadId}...");
                    stopwatch.Start();
                    var ret = factory();
                    stopwatch.Stop();
                    log.Verbose($"[{Name}] Created v{version} for {language} in {stopwatch.ElapsedMilliseconds} ms.");
                    _value = ret;
                    return ret;
                }, TaskScheduler.Default);
            return new Tuple<Task<T>, Stopwatch>(task, stopwatch);
        }
    }
}
