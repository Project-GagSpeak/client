namespace GagSpeak.PlayerControl;

/// <summary>
///     Handles Task Insertion, adding tasks to the beginning of the _tasks list.
/// </summary>
public partial class HcTaskManager
{
    public void InsertTask(Func<bool?> func, string name, HcTaskConfiguration config)
        => InsertHcTask(new HardcoreTask(func, name, config));

    public void InsertTask(Func<bool?> func, HcTaskConfiguration config)
        => InsertHcTask(new HardcoreTask(func, config));

    public void InsertTask(Func<bool> func, string name, HcTaskConfiguration config)
        => InsertHcTask(new HardcoreTask(func, name, config));

    public void InsertTask(Func<bool> func, HcTaskConfiguration config)
        => InsertHcTask(new HardcoreTask(func, config));

    public void InsertTask(Action action, string name, HcTaskConfiguration config)
        => InsertHcTask(new HardcoreTask(action, name, config));

    public void InsertTask(Action action, HcTaskConfiguration config)
        => InsertHcTask(new HardcoreTask(action, config));

    public void InsertHcTask(HardcoreTaskBase task)
    {
        if (task is null)
            return;
        
        _logger.LogInformation($"Inserted Hardcore Task: {task.Name} ({task.Location})");
        _taskOperations.Insert(0, task);
    }
}
