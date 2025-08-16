using System.Reflection;

namespace GagSpeak.PlayerControl;

/// <summary>
///     Represents a sequence of tasks to be performed. <para />
///     This can also be a single task. But this 'group' of tasks are confined to the associated configuration.
/// </summary>
public class HcTaskOperation
{
    public string Name { get; init; }
    public string Location { get; init; }
    public List<Func<bool?>> Tasks { get; init; }
    public HcTaskConfiguration Config { get; init; }

    private int _currentTaskIdx = 0;
    public int CurrentTaskIdx => _currentTaskIdx;
    public bool IsComplete => _currentTaskIdx >= Tasks.Count;

    public HcTaskOperation(Func<bool?> task, HcTaskConfiguration config)
    {
        Tasks = [task];
        Config = config;
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(Func<bool?> task, string name, HcTaskConfiguration config)
    {
        Tasks = [task];
        Config = config;
        Name = name;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(Func<bool> task, HcTaskConfiguration config)
    {
        Tasks = [() => task()];
        Config = config;
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }
    public HcTaskOperation(Func<bool> task, string name, HcTaskConfiguration config)
    {
        Tasks = [() => task()];
        Config = config;
        Name = name;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(Action action, HcTaskConfiguration config)
    {
        Tasks = [() => { action(); return true; }];
        Config = config;
        Name = action.GetMethodInfo().Name ?? string.Empty;
        Location = action.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(Action action, string name, HcTaskConfiguration config)
    {
        Tasks = [() => { action(); return true; }];
        Config = config;
        Name = name;
        Location = action.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    // constructors to support multiple tasks in a single operation.
    public HcTaskOperation(IEnumerable<Func<bool?>> tasks, HcTaskConfiguration config)
    {
        Tasks = tasks.ToList();
        Config = config;
        Name = string.Join(", ", Tasks.Select(t => t.GetMethodInfo().Name));
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(IEnumerable<Func<bool?>> tasks, string name, HcTaskConfiguration config)
    {
        Tasks = tasks.ToList();
        Config = config;
        Name = name;
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(IEnumerable<Func<bool>> tasks, HcTaskConfiguration config)
    {
        Tasks = tasks.Select(t => (Func<bool?>)(() => t())).ToList();
        Config = config;
        Name = string.Join(", ", Tasks.Select(t => t.GetMethodInfo().Name));
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(IEnumerable<Func<bool>> tasks, string name, HcTaskConfiguration config)
    {
        Tasks = tasks.Select(t => (Func<bool?>)(() => t())).ToList();
        Config = config;
        Name = name;
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(IEnumerable<Action> actions, HcTaskConfiguration config)
    {
        Tasks = actions.Select(a => (Func<bool?>)(() => { a(); return true; })).ToList();
        Config = config;
        Name = string.Join(", ", actions.Select(a => a.GetMethodInfo().Name));
        Location = actions.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HcTaskOperation(IEnumerable<Action> actions, string name, HcTaskConfiguration config)
    {
        Tasks = actions.Select(a => (Func<bool?>)(() => { a(); return true; })).ToList();
        Config = config;
        Name = name;
        Location = actions.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public void AdvanceTaskIndex() 
        => _currentTaskIdx++;
}
