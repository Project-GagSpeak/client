namespace GagSpeak.PlayerControl;

/// <summary>
///     Handle Task Enqueuing, placing tasks at the end of the TaskManager's Task List
/// </summary>
public partial class HcTaskManager
{
    public void EnqueueTask(Func<bool?> func, string name, HcTaskConfiguration config)
        => EnqueueTaskOperation(new(func, name, config));

    public void EnqueueTask(Func<bool?> func, HcTaskConfiguration config)
        => EnqueueTaskOperation(new(func, config));

    public void EnqueueTask(Func<bool> func, string name, HcTaskConfiguration config)
        => EnqueueTaskOperation(new(func, name, config));

    public void EnqueueTask(Func<bool> func, HcTaskConfiguration config)
        => EnqueueTaskOperation(new(func, config));

    public void EnqueueTask(Action action, string name, HcTaskConfiguration config)
        => EnqueueTaskOperation(new(action, name, config));

    public void EnqueueTask(Action action, HcTaskConfiguration config)
        => EnqueueTaskOperation(new(action, config));

    public void EnqueueTaskOperation(HcTaskOperation task)
    {
        if (task is null)
            return;
        
        _logger.LogDebug($"Enqueued Hardcore Task: {task.Name} ({task.Location})", LoggerType.HardcoreTasks);
        _taskOperations.Add(task);
    }
}
