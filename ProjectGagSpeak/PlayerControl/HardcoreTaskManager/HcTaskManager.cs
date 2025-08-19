using Dalamud.Plugin.Services;
using GagSpeak.State;
using GagSpeak.State.Caches;
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
    private List<HcTaskOperation> _taskOperations = new List<HcTaskOperation>();

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
    public int QueuedTasks => _taskOperations.Count + (CurrentTask != null ? 1 : 0);

    /// <summary> Current progress made out of the total tasks enqueued. </summary>
    public float ManagerProgress => ObservedTasks == 0 ? 0 : (float)(ObservedTasks - QueuedTasks) / (float)ObservedTasks;

    // gets the current progress of the list of tasks performed in a operation group.
    public float OperationProgress => CurrentTask is null ? 0 : (float)CurrentTask.CurrentTaskIdx / (float)CurrentTask.Tasks.Count;

    /// <summary> If the Hardcore Task Manager is currently busy performing tasks. </summary>
    public bool IsBusy => _taskOperations.Count > 0 || CurrentTask != null; 

    /// <summary> The task currently being processed by the Hardcore Task Manager. </summary>
    public HcTaskOperation? CurrentTask { get; private set; } = null;

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
        Svc.Framework.Update -= OnFramework;
        _logger.LogInformation("Hardcore Task Manager Disposed.");
        GC.SuppressFinalize(this);
    }

    public void AbortTasks()
    {
        _taskOperations.Clear();
        AbortTime = 0;
        StartTime = 0;
        CurrentTask = null;
        // if there is an active stack, discard it.
        if (_tempStack.Count > 0)
        {
            _logger.LogDebug("Aborting Hardcore Task Stack.", LoggerType.HardcoreTasks);
            DiscardStack();
        }
    }

    public void AbortCurrentTask()
    {
        // if there is an active task, abort it.
        if (CurrentTask != null)
        {
            _logger.LogDebug($"Aborting Hardcore Task: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
            CurrentTask = null;
            AbortTime = 0;
            StartTime = 0;
        }
    }

    // Task update loop.
    private void OnFramework(IFramework framework)
    {
        // Handle the condition in where there are no tasks currently being processed.
        if (_taskOperations.Count > 0 || CurrentTask is not null)
        {
            // if no current operation is being performed, begin the next operation.
            if (CurrentTask is null)
            {
                CurrentTask = _taskOperations[0];
                _taskOperations.RemoveAt(0);

                // Apply the control state to the cache.
                _cache.SetActiveTaskControl(CurrentTask.Config.ControlFlags);
                StartTime = Environment.TickCount64;
                AbortTime = 0;
                ObservedTasks = QueuedTasks;
            }

            // set the config to the operations config.
            var config = CurrentTask.Config;

            // Get the current task delegate inside the operation
            var curTaskIdx = CurrentTask.CurrentTaskIdx;
            // if the task is complete, we should move onto the next one.
            if (CurrentTask.IsComplete)
            {
                _logger.LogInformation($"HcTaskOperation Complete: {CurrentTask.Name}, {CurrentTask.Location}");
                _cache.SetActiveTaskControl(HcTaskControl.None);
                CurrentTask = null;
                return;
            }

            // obtain the current task to perform.
            var curTask = CurrentTask.Tasks[curTaskIdx];
            try
            {
                // Define timeout if not set.
                if (AbortTime is 0)
                {
                    RemainingTime = config.MaxTaskTime;
                    _logger.LogTrace($"Hardcore Task Begin: [{CurrentTask.Name} ({CurrentTask.Location})], with timeout of {RemainingTime}", LoggerType.HardcoreTasks);
                }
                // handle timeout occurances
                if (RemainingTime < 0)
                {
                    _logger.LogDebug($"Hardcore Task Timeout: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
                    throw new BagagwaTimeout();
                }

                // Execute the current task.
                var result = curTask();

                // dont think this will ever occur, but add just incase.
                if (CurrentTask == null)
                {
                    _logger.LogWarning($"Hardcore Task was aborted from inside!");
                    return;
                }
                // if the result was SUCCESSFUL (Y I P E E!), then we can iterate onto the next task!
                if (result is true)
                {
                    // the task completed, so we should move onto the next index.
                    _logger.LogInformation($"Hardcore Task Success: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
                    CurrentTask.AdvanceTaskIndex();
                    AbortTime = 0; // reset abort time.

                    // if the entire operation is complete, then remove sources and reset current.
                    if (CurrentTask.IsComplete)
                    {
                        _logger.LogInformation($"Hardcore Task Operation Complete: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
                        _cache.SetActiveTaskControl(HcTaskControl.None);
                        CurrentTask = null;
                    }
                }
                // if the result was null then abort all tasks.
                else if (result is null)
                {
                    _logger.LogTrace($"Received abort request from task: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
                    AbortTasks();
                }
            }
            // handle cases where we summoned Bagagwa via timeouts or other standard Bagagwa summoning practices.
            catch (BagagwaTimeout e)
            {
                _logger.LogWarning($"Timeout in operation [{CurrentTask?.Name}] task index {CurrentTask?.CurrentTaskIdx}: {e}", LoggerType.HardcoreTasks);
                AbortTasks();
            }
            catch (Bagagwa ex)
            {
                _logger.LogError($"Hardcore Task Error: {CurrentTask?.Name} ({CurrentTask?.Location}), Exception: {ex}", LoggerType.HardcoreTasks);
                AbortTasks();
            }
            // return early to not update the observed tasks count.
            return;
        }

        // reset the observed tasks count if we are not currently processing any tasks.
        if (ObservedTasks != 0 && CurrentTask is null)
            ObservedTasks = 0;
    }
}
