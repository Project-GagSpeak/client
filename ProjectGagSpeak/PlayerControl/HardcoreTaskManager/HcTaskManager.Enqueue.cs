namespace GagSpeak.PlayerControl;

/// <summary>
///     Handle Task Enqueuing, placing tasks at the end of the TaskManager's Task List
/// </summary>
public partial class HcTaskManager
{
    public void EnqueueTask(Func<bool?> func, HcTaskConfiguration? config = null)
        => EnqueueOperation(new HardcoreTask(func, config ?? HcTaskConfiguration.Default));

    public void EnqueueTask(Func<bool> func, string name, HcTaskConfiguration? config = null)
        => EnqueueOperation(new HardcoreTask(func, name, config ?? HcTaskConfiguration.Default));

    public void EnqueueTask(Func<bool> func, HcTaskConfiguration? config = null)
        => EnqueueOperation(new HardcoreTask(func, config ?? HcTaskConfiguration.Default));

    public void EnqueueTask(Action action, string name, HcTaskConfiguration? config = null)
        => EnqueueOperation(new HardcoreTask(action, name, config ?? HcTaskConfiguration.Default));

    public void EnqueueTask(Action action, HcTaskConfiguration? config = null)
        => EnqueueOperation(new HardcoreTask(action, config ?? HcTaskConfiguration.Default));

    public void EnqueueOperation(HardcoreTaskBase task)
    {
        if (task is null)
            return;
        
        _logger.LogDebug($"Enqueued Hardcore Task: {task.Name}", LoggerType.HardcoreTasks);
        _taskOperations.Add(task);
    }
}
