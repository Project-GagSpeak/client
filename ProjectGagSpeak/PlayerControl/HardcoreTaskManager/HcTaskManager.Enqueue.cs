namespace GagSpeak.PlayerControl;

/// <summary>
///     Handle Task Enqueuing, placing tasks at the end of the TaskManager's Task List
/// </summary>
public partial class HcTaskManager
{
    public void EnqueueTask(Func<bool?> func, string name, HcTaskConfiguration? config = null)
        => EnqueueTask(new(func, name, config));

    public void EnqueueTask(Func<bool?> func, HcTaskConfiguration? config = null)
        => EnqueueTask(new(func, config));

    public void EnqueueTask(Func<bool> func, string name, HcTaskConfiguration? config = null)
        => EnqueueTask(new(func, name, config));

    public void EnqueueTask(Func<bool> func, HcTaskConfiguration? config = null)
        => EnqueueTask(new(func, config));

    public void EnqueueTask(Action action, string name, HcTaskConfiguration? config = null)
        => EnqueueTask(new(action, name, config));

    public void EnqueueTask(Action action, HcTaskConfiguration? config = null)
        => EnqueueTask(new(action, config));

    public void EnqueueTask(HardcoreTask task)
        => EnqueueMultipleTasks(task);

    public void EnqueueMultipleTasks(params HardcoreTask?[] tasks)
    {
        // Enqueue multiple tasks into the end of the list in sequence.
        foreach (var task in tasks)
        {
            if (task is null) 
                continue;

            // if a stack is currently active, add the tasks into the stack instead.
            if (IsStackActive)
            {
                _logger.LogDebug($"Enqueued Hardcore Task to Stack: {task.Name} ({task.Location})", LoggerType.HardcoreTasks);
                _tempStack.Add(task);
            }
            else
            {
                _logger.LogDebug($"Enqueued Hardcore Task: {task.Name} ({task.Location})", LoggerType.HardcoreTasks);
                _tasks.Add(task);
            }
        }
    }
}
