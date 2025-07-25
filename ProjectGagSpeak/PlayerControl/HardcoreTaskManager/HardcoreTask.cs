using System.Reflection;

namespace GagSpeak.PlayerControl;

/// <summary>
///     Indicates a singular Hardcore Task operation. <para />
///     Tasks can have names, and provide locations they were called from. <para />
///     The HcTaskManager will only move onto the next task if this task returns true.
/// </summary>
/// <remarks> The provided <see cref="Config"/> can provide customized Abort and Task Timers. </remarks>
public class HardcoreTask
{
    public string Name { get; init; }
    public string Location { get; init; }
    public Func<bool?> Task { get; init; }
    public HcTaskConfiguration? Config { get; init; }

    public HardcoreTask(Func<bool?> task, HcTaskConfiguration? config = null)
    {
        Task = task;
        Config = config;
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Func<bool?> task, string name, HcTaskConfiguration? config = null)
    {
        Task = task;
        Config = config;
        Name = name;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Func<bool> task, HcTaskConfiguration? config = null)
    {
        Task = () => task();
        Config = config;
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }
    public HardcoreTask(Func<bool> task, string name, HcTaskConfiguration? config = null)
    {
        Task = () => task();
        Config = config;
        Name = name;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Action action, HcTaskConfiguration? config = null)
    {
        Task = () => { action(); return true; };
        Config = config;
        Name = action.GetMethodInfo().Name ?? string.Empty;
        Location = action.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Action action, string name, HcTaskConfiguration? config = null)
    {
        Task = () => { action(); return true; };
        Config = config;
        Name = name;
        Location = action.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }
}
