namespace GagSpeak.PlayerControl;

/// <summary>
///     Handles Task Insertion, adding tasks to the beginning of the _tasks list.
/// </summary>
public partial class HcTaskManager
{
    public void InsertTask(Func<bool?> func, string name, HcTaskConfiguration? config = null)
        => InsertTask(new(func, name, config));

    public void InsertTask(Func<bool?> func, HcTaskConfiguration? config = null)
        => InsertTask(new(func, config));

    public void InsertTask(Func<bool> func, string name, HcTaskConfiguration? config = null)
        => InsertTask(new(func, name, config));

    public void InsertTask(Func<bool> func, HcTaskConfiguration? config = null)
        => InsertTask(new(func, config));

    public void InsertTask(Action action, string name, HcTaskConfiguration? config = null)
        => InsertTask(new(action, name, config));

    public void InsertTask(Action action, HcTaskConfiguration? config = null)
        => InsertTask(new(action, config));

    public void InsertTask(HardcoreTask task)
        => InsertMultipleTasks(task);

    public void InsertMultipleTasks(params HardcoreTask?[] tasks)
    {
        // Insert multiple tasks into the beginning of the list in sequence.
        foreach (var task in Enumerable.Reverse(tasks))
        {
            if (task is null)
                continue;

            // if a stack is currently active, add the tasks into the stack instead.
            if (IsStackActive)
            {
                _logger.LogDebug($"Inserted Hardcore Task to Stack: {task.Name} ({task.Location})", LoggerType.HardcoreTasks);
                _tempStack.Insert(0, task);
            }
            else
            {
                _logger.LogDebug($"Inserted Hardcore Task: {task.Name} ({task.Location})", LoggerType.HardcoreTasks);
                _tasks.Insert(0, task);
            }
        }
    }
}
