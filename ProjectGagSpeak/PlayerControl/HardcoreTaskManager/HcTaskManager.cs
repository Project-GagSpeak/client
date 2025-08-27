using Dalamud.Plugin.Services;
using GagSpeak.State;
using GagSpeak.State.Caches;
using Penumbra.GameData.Files.ShaderStructs;
using System.Reflection;

namespace GagSpeak.PlayerControl;

/// <summary>
///     TaskManager for Hardcore operations, that is capable of automating multiple sub tasks 
///     in sequence, while completely provending various forms of player movement.
/// </summary>
/// <remarks> 
///     It is possible to retain multiple instances if this is assumed with 
///     static CkCommon integration, but the questionable hardcore methods might make it best 
///     to keep seperate.
/// </remarks>
public partial class HcTaskManager : IDisposable
{
    private readonly ILogger<HcTaskManager> _logger;
    private readonly PlayerControlCache _cache;

    /// <summary> 
    ///     The list of hardcore task operations managed by the HcTaskManager.
    /// </summary>
    private List<HardcoreTaskBase> _taskOperations = new List<HardcoreTaskBase>();

    public HcTaskManager(ILogger<HcTaskManager> logger, PlayerControlCache cache)
    {
        _logger = logger;
        _cache = cache;

        Svc.Framework.Update += OnFramework;
        _logger.LogInformation("Hardcore Task Manager Initialized.");
    }

    /// <summary> # of tasks observed by the HcTaskManager. </summary>
    public int ObservedTasks {get; private set; } = 0;

    /// <summary> The total number of currently queued Tasks. </summary>
    public int QueuedTasks => _taskOperations.Count;

    /// <summary> Current progress made out of the total tasks enqueued. </summary>
    public float ManagerProgress => ObservedTasks is 0 ? 0 : (float)(ObservedTasks - QueuedTasks) / (float)ObservedTasks;

    // gets the current progress of the list of tasks performed in a operation group.
    public float OperationProgress => QueuedTasks is 0 ? 0 : (float)_taskOperations[0].CurrentTaskIdx / (float)_taskOperations[0].Tasks.Count;

    /// <summary> If the Hardcore Task Manager is currently busy performing tasks. </summary>
    public bool IsBusy => QueuedTasks > 0; 

    /// <summary> The time the current task is starting. </summary>
    public static long StartTime = 0;

    /// <summary> How much longer the task has to execute. </summary>
    public long RemainingTime
    {
        get => AbortTime - Environment.TickCount64;
        set => AbortTime = Environment.TickCount64 + value;
    }

    public static long ElapsedTime => Environment.TickCount64 - StartTime;

    /// <summary> The time in milliseconds when the Task Manager should abort. </summary>
    public long AbortTime = 0;

    public void Dispose()
    {
        // dispose of this hardcore task manager singleton.
        _cache.SetActiveTaskControl(HcTaskControl.None);
        Svc.Framework.Update -= OnFramework;
        _logger.LogInformation("Hardcore Task Manager Disposed.");
        GC.SuppressFinalize(this);
    }

    public void AbortIfActive(string taskName)
    {
        if (_taskOperations.Count is 0)
            return;

        if (taskName.Equals(_taskOperations[0].Name, StringComparison.OrdinalIgnoreCase))
            AbortCurrentTask();
    }

    public void RemoveIfPresent(string taskName)
    {
        if (_taskOperations.Count is 0)
            return;

        // if the task with the same name exists in the queue, remove it.
        if (_taskOperations[0].Name.Equals(taskName, StringComparison.OrdinalIgnoreCase))
            AbortCurrentTask();
        else
            _taskOperations.RemoveAll(t => taskName.Equals(t.Name, StringComparison.OrdinalIgnoreCase));
    }

    public void AbortTasks()
    {
        _taskOperations.Clear();
        AbortTime = 0;
        StartTime = 0;
        // set regardless.
        _cache.SetActiveTaskControl(HcTaskControl.None);
    }

    public void AbortCurrentTask()
    {
        if (_taskOperations.Count > 0)
        {
            _logger.LogDebug($"Aborting Task: {_taskOperations[0].Name} ({_taskOperations[0].Location})", LoggerType.HardcoreTasks);
            _taskOperations[0].End();
            _taskOperations.RemoveAt(0);
            AbortTime = 0;
            StartTime = 0;
        }
        // set regardless.
        _cache.SetActiveTaskControl(HcTaskControl.None);
    }

    // Task update loop.
    private void OnFramework(IFramework framework)
    {
        // Handle the condition in where there are no tasks currently being processed.
        if (_taskOperations.Count == 0)
            return;

        // if the task is complete before executing, it was aborted, so end and remove the item from the list.
        if (_taskOperations[0].IsComplete)
        {
            _taskOperations[0].End();
            _cache.SetActiveTaskControl(HcTaskControl.None);
            AbortTime = 0;
            StartTime = 0;
            _taskOperations.RemoveAt(0);
            // return early if there are no more tasks to process.
            if (_taskOperations.Count is 0)
                return;
        }

        // Assuming we have tasks present, we should process the first in the list.
        var currentHcTask = _taskOperations[0];

        // if the current task has not yet begin, we should begin it.
        if (!currentHcTask.IsExecuting)
        {
            currentHcTask.Begin();
            _cache.SetActiveTaskControl(currentHcTask.Config.ControlFlags);
            StartTime = Environment.TickCount64;
            AbortTime = 0;
            ObservedTasks = QueuedTasks;
        }

        // now that the task has begun executing, or is currently executing, perform the task.
        var config = currentHcTask.Config;

        try
        {
            // if the abort time is 0, set the remaining time and log it's beginning.
            if (AbortTime is 0)
            {
                RemainingTime = config.MaxTaskTime;
                _logger.LogTrace($"HcTask Begin: [{currentHcTask.CurrentTaskIdx}] ({currentHcTask.Name}), with timeout of {RemainingTime}", LoggerType.HardcoreTasks);
            }
            // if it timed out, handle that.
            if (RemainingTime < 0)
            {
                _logger.LogDebug($"HcTask Timeout: [{currentHcTask.CurrentTaskIdx}] ({currentHcTask.Name})", LoggerType.HardcoreTasks);
                throw new BagagwaTimeout();
            }

            // process the task, and return the result.
            var taskRes = currentHcTask.PerformTask();
            // handle the task result.
            if (taskRes is true)
            {
                Svc.Logger.Information($"HcTask [{currentHcTask.CurrentTaskIdx}] Success: ({currentHcTask.Name})");
                AbortTime = 0;
            }
            else if (taskRes is null)
            {
                _logger.LogTrace($"Abort Request: HcTask [{currentHcTask.CurrentTaskIdx}] ({currentHcTask.Name})", LoggerType.HardcoreTasks);
                throw new Bagagwa("Task requested abort.");
            }

            // At this point, it is possible for the hardcore task to be complete, if it is, we should end.
            if (currentHcTask.IsComplete)
            {
                // end the task, log its completion, and remove the hcTaskControl.
                _logger.LogInformation($"HcTask Complete: [{currentHcTask.CurrentTaskIdx} ({currentHcTask.Name})", LoggerType.HardcoreTasks);
                currentHcTask.End();
                _cache.SetActiveTaskControl(HcTaskControl.None);
            }

        }
        // handle cases where we summoned Bagagwa via timeouts or other standard Bagagwa summoning practices.
        catch (BagagwaTimeout)
        {
            _logger.LogWarning($"Timeout: [{currentHcTask.CurrentTaskIdx}] from ({currentHcTask.Name})", LoggerType.HardcoreTasks);
            AbortCurrentTask();
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"HcTask Error: {currentHcTask.Name} ({currentHcTask.Location}), Exception: {ex}", LoggerType.HardcoreTasks);
            AbortCurrentTask();
        }
        // return early to not update the observed tasks count.
        return;
    }
}
