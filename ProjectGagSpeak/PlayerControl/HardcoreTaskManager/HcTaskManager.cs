using Dalamud.Plugin.Services;
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

    /// <summary> Configuration for the task manager, such as max task time and if we should abort on timeout. </summary>
    /// <remarks> This could possible function per-task, instead of globally. </remarks>
    private HcTaskConfiguration _taskConfig = new HcTaskConfiguration();

    /// <summary> The list of tasks currently being managed by the Hardcore Task Manager. </summary>
    private List<HardcoreTask> _tasks = new List<HardcoreTask>();
    public HcTaskManager(ILogger<HcTaskManager> logger)
    {
        _logger = logger;
        // make sure our tasks run on the framework update loop so that game functions are properly handled.
        Svc.Framework.Update += OnFramework;
        _logger.LogInformation("Hardcore Task Manager Initialized.");
    }

    /// <summary> # of tasks observed by the HcTaskManager. </summary>
    public int ObservedTasks {get; private set; } = 0;

    /// <summary> The total number of currently queued Tasks. </summary>
    public int QueuedTasks => _tasks.Count + (CurrentTask != null ? 1 : 0);

    /// <summary> Current progress made out of the total tasks enqueued. </summary>
    public float ManagerProgress => ObservedTasks == 0 ? 0 : (float)(ObservedTasks - QueuedTasks) / (float)ObservedTasks;

    /// <summary> If the Hardcore Task Manager is currently busy performing tasks. </summary>
    public bool IsBusy => _tasks.Count > 0 || CurrentTask != null; 

    /// <summary> The task currently being processed by the Hardcore Task Manager. </summary>
    public HardcoreTask? CurrentTask { get; private set; } = null;

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
        _tasks.Clear();
        AbortTime = 0;
        StartTime = 0;
        CurrentTask = null;
        // if there is an active stack, discard it.
        if (IsStackActive)
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
        if (_tasks.Count > 0 || CurrentTask is not null)
        {
            // if the current task is null, begin the next task.
            if (CurrentTask is null)
            {
                CurrentTask = _tasks[0];
                _tasks.RemoveAt(0);
                AbortTime = 0;
                StartTime = Environment.TickCount64;
            }

            // set the config to the individual config, or fallback to the default task config.
            var config = CurrentTask.Config ?? _taskConfig;

            // if the number of queued tasks exceeds the observed tasks, update the observed tasks count.
            if (QueuedTasks > ObservedTasks)
            {
                ObservedTasks = QueuedTasks;
                _logger.LogDebug($"Observed Tasks Updated: {ObservedTasks}", LoggerType.HardcoreTasks);
            }

            // Attempt to update the task execution.
            try
            {
                // if the abort time is not set, mark the remaining time to the tasks max task time.
                if (AbortTime is 0)
                {
                    RemainingTime = config.MaxTaskTime;
                    _logger.LogTrace($"Hardcore Task Begin: [{CurrentTask.Name} ({CurrentTask.Location})], with timeout of {RemainingTime}", LoggerType.HardcoreTasks);
                }
                // if the remaining time if below 0, we should update the tim and handle timeouts.
                if (RemainingTime < 0)
                {
                    // handle timeout occurances.
                    _logger.LogDebug($"Hardcore Task Timeout: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
                    throw new BagagwaTimeoutException();
                }
                // Perform the current task, and yield its result.
                var taskResult = CurrentTask.Task();
                // if the current task is null, warn abortion.
                if (CurrentTask == null)
                {
                    _logger.LogWarning($"Hardcore Task was aborted from inside!");
                    return;
                }
                // if the result was SUCCESSFUL (Y I P E E!), then we can iterate onto the next task!
                if (taskResult is true)
                {
                    _logger.LogTrace($"Hardcore Task Success: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
                    CurrentTask = null;
                }
                else if (taskResult is null)
                {
                    _logger.LogTrace($"Recieved abort request from task: {CurrentTask.Name} ({CurrentTask.Location})", LoggerType.HardcoreTasks);
                    AbortTasks();
                }
            }
            catch (BagagwaTimeoutException e)
            {
                // log the timeout exception.
                _logger.LogWarning($"Hardcore Task Timeout: {CurrentTask?.Name} ({CurrentTask?.Location}), Exception: {e}", LoggerType.HardcoreTasks);
                // if we should abort on timeout, abort the current task.
                if (config.AbortOnTimeout)
                    AbortTasks();
                else
                    CurrentTask = null;
            }
            catch (Bagagwa ex)
            {
                _logger.LogError($"Hardcore Task Error: {CurrentTask?.Name} ({CurrentTask?.Location}), Exception: {ex}", LoggerType.HardcoreTasks);
                // abort the task and set the current task to null.
                if (config.AbortOnTimeout)
                    AbortTasks();
                else
                    CurrentTask = null;
            }
            // return early to not update the observed tasks count.
            return;
        }

        // reset the observed tasks count if we are not currently processing any tasks.
        if (ObservedTasks != 0 && CurrentTask is null)
            ObservedTasks = 0;
    }
}
