using System.Reflection;

namespace GagSpeak.PlayerControl;

// Base for hardcore tasks.
public abstract class HardcoreTaskBase
{
    public string Name { get; init; }
    public string Location { get; init; }
    public HcTaskConfiguration Config { get; init; }
    public abstract List<Func<bool?>> Tasks { get; }
    public int CurrentTaskIdx { get; private set; } = 0;
    public bool IsExecuting { get; protected set; } = false;
    public virtual bool IsComplete => CurrentTaskIdx >= Tasks.Count;
    // internally execute the current task index.
    public virtual bool? PerformTask()
    {
        // perform the current task and retrieve the result.
        var res = Tasks[CurrentTaskIdx]();
        // if the result is true, advance the current task index.
        if (res is true)
            CurrentTaskIdx++;
        return res;
    }
    public virtual void Begin() => IsExecuting = true;
    public virtual void End() 
    {
        IsExecuting = false;
        CurrentTaskIdx = Tasks.Count;
    }
    
    public virtual void Abort()
    {
        IsExecuting = false;
        CurrentTaskIdx = Tasks.Count;
    }
}

// this might get a little trippy / recursive, but hey, if it works it works.
public class HardcoreTaskCollection : HardcoreTaskBase
{
    public readonly List<HardcoreTaskBase> StoredTasks;
    private int _currentSubTaskIdx = 0;
    private HardcoreTaskBase _currentTask => StoredTasks[_currentSubTaskIdx]; // the focused one.
    public override List<Func<bool?>> Tasks => _currentTask.Tasks; // reflects the focused task.
    public override bool IsComplete => _currentSubTaskIdx >= StoredTasks.Count;

    public HardcoreTaskCollection(IEnumerable<HardcoreTaskBase> tasks, HcTaskConfiguration config)
    {
        Name = tasks.FirstOrDefault()?.Name ?? string.Empty;
        Location = tasks.FirstOrDefault()?.Location ?? string.Empty;
        Config = config;
        StoredTasks = tasks.ToList();
    }
    public HardcoreTaskCollection(IEnumerable<HardcoreTaskBase> tasks, string name, HcTaskConfiguration config)
    {
        StoredTasks = tasks.ToList();
        Config = config;
        Name = name;
        Location = StoredTasks.FirstOrDefault()?.Location ?? string.Empty;
    }
    public override void Begin()
    {
        if (IsComplete)
        {
            Svc.Logger.Debug($"HcCollection: Cannot begin, already complete! {Name}");
            return;
        }

        if (_currentTask.Tasks.Count is 0)
        {
            Svc.Logger.Warning($"HcCollection: Cannot begin, current subtask has no tasks! {Name} ({Location})");
            Abort();
            return;
        }

        // initiate the subtask.
        StoredTasks[_currentSubTaskIdx].Begin();
        
        // if the external task has not yet begun, begin it.
        if (!IsExecuting)
            base.Begin();
    }

    public override bool? PerformTask()
    {
        // compute the result.
        Svc.Logger.Information($"HcCollection: Performing subtask IDX {_currentSubTaskIdx} of {StoredTasks.Count}." +
            $"\nPerforming task at idx {_currentTask.CurrentTaskIdx} for subtask ({_currentTask.Name})");
        var res = _currentTask.PerformTask();
        // if the result is null we should move onto the next task and abort the current one.
        if (res is null)
        {
            Svc.Logger.Warning($"HcCollection subtask IDX {_currentSubTaskIdx} of {StoredTasks.Count} failed." +
                $"\nAborting subtask: ({_currentTask.Name})");
            _currentTask.Abort();
            Svc.Logger.Warning($"HcCollection: Aborted subtask: ({_currentTask.Name})");
            Svc.Logger.Warning($"HcCollection: CurrentTask IsComplete: {_currentTask.IsComplete}");
            Svc.Logger.Warning("Increasing _currentSubTaskIdx++.");
            _currentSubTaskIdx++;
            return res;
        }
        
        if (res is true)
        {
            Svc.Logger.Information($"HcSubTask Success for Collection: {Name}.");
            // the internal _currentTask.PerformTask() increased its index on success, so check for completion.
            // if the subtask is complete, end and move to the next one.
            if (_currentTask.IsComplete)
            {
                Svc.Logger.Information($"HcCollection: {Name} completed subtask at IDX: {_currentSubTaskIdx} ({_currentTask.Name})");
                _currentTask.End();
                _currentSubTaskIdx++;
            }
        }
        // begin the next task immidiately if not complete.
        if (!IsComplete)
        {
            Svc.Logger.Information($"HcCollection: Beginning next subtask at IDX: {_currentSubTaskIdx} of {StoredTasks.Count}." +
                $"\nBeginning subtask: ({_currentTask.Name})");
            StoredTasks[_currentSubTaskIdx].Begin();
        }
        // return the result.
        return res;
    }

    public override void Abort()
    {
        // abort the current internal task and move onto the next. If completed, mark it.
        if (!IsComplete)
        {
            Svc.Logger.Warning($"HcCollection: Outer not complete. At Task IDX {_currentSubTaskIdx} of {StoredTasks.Count}." +
                $"\nAborting current subtask: ({_currentTask.Name})");
            _currentTask.Abort();
            Svc.Logger.Warning($"HcCollection: Aborted current subtask: ({_currentTask.Name})");
            Svc.Logger.Warning($"HcCollection: CurrentTask IsComplete: {_currentTask.IsComplete}");
            Svc.Logger.Warning("Increasing _currentSubTaskIdx++.");
            _currentSubTaskIdx++;
            // if completed now, abort base.
            if (IsComplete)
            {
                Svc.Logger.Warning($"HcCollection: Now complete after aborting subtask. Aborting outer task: {Name} ({Location})");
                base.Abort();
            }
            return;
        }
        // otherwise abort.
        base.Abort();
    }
}

public class HardcoreTask : HardcoreTaskBase
{
    private readonly Func<bool?> _task;
    public override List<Func<bool?>> Tasks => new() { _task };
    public HardcoreTask(Func<bool?> task, HcTaskConfiguration? config = null)
    {
        _task = task;
        Config = config ?? HcTaskConfiguration.Default;
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Func<bool?> task, string name, HcTaskConfiguration? config = null)
    {
        _task = task;
        Config = config ?? HcTaskConfiguration.Default;
        Name = name;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Func<bool> task, HcTaskConfiguration? config = null)
    {
        _task = () => task();
        Config = config ?? HcTaskConfiguration.Default;
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }
    public HardcoreTask(Func<bool> task, string name, HcTaskConfiguration? config = null)
    {
        _task = () => task();
        Config = config ?? HcTaskConfiguration.Default;
        Name = name;
        Location = task.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Action action, HcTaskConfiguration? config = null)
    {
        _task = () => { action(); return true; };
        Config = config ?? HcTaskConfiguration.Default;
        Name = action.GetMethodInfo().Name ?? string.Empty;
        Location = action.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTask(Action action, string name, HcTaskConfiguration? config = null)
    {
        _task = () => { action(); return true; };
        Config = config ?? HcTaskConfiguration.Default;
        Name = name;
        Location = action.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }
}

/// <summary>
///     Represents a sequence of tasks to be performed. <para />
///     This can also be a single task. But this 'group' of tasks are confined to the associated configuration.
/// </summary>
public class HardcoreTaskGroup : HardcoreTaskBase
{
    private List<Func<bool?>> _tasks;
    public override List<Func<bool?>> Tasks => _tasks;
    public HardcoreTaskGroup(IEnumerable<Func<bool?>> tasks, HcTaskConfiguration config)
    {
        _tasks = tasks.ToList();
        Config = config;
        Name = string.Join(", ", Tasks.Select(t => t.GetMethodInfo().Name));
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTaskGroup(IEnumerable<Func<bool?>> tasks, string name, HcTaskConfiguration config)
    {
        _tasks = tasks.ToList();
        Config = config;
        Name = name;
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTaskGroup(IEnumerable<Func<bool>> tasks, HcTaskConfiguration config)
    {
        _tasks = tasks.Select(t => (Func<bool?>)(() => t())).ToList();
        Config = config;
        Name = string.Join(", ", Tasks.Select(t => t.GetMethodInfo().Name));
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTaskGroup(IEnumerable<Func<bool>> tasks, string name, HcTaskConfiguration config)
    {
        _tasks = tasks.Select(t => (Func<bool?>)(() => t())).ToList();
        Config = config;
        Name = name;
        Location = Tasks.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTaskGroup(IEnumerable<Action> actions, HcTaskConfiguration config)
    {
        _tasks = actions.Select(a => (Func<bool?>)(() => { a(); return true; })).ToList();
        Config = config;
        Name = string.Join(", ", actions.Select(a => a.GetMethodInfo().Name));
        Location = actions.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }

    public HardcoreTaskGroup(IEnumerable<Action> actions, string name, HcTaskConfiguration config)
    {
        _tasks = actions.Select(a => (Func<bool?>)(() => { a(); return true; })).ToList();
        Config = config;
        Name = name;
        Location = actions.FirstOrDefault()?.GetMethodInfo().DeclaringType?.FullName ?? string.Empty;
    }
}

// Branching variant, which executes one of two sets of tasks based on a predicate.
public class BranchingHardcoreTask : HardcoreTaskBase
{
    public readonly Func<bool> Predicate;
    public readonly HardcoreTaskBase TrueTask = new HardcoreTask(() => true);
    public readonly HardcoreTaskBase FalseTask = new HardcoreTask(() => true);

    private HardcoreTaskBase _activeTask = new HardcoreTask(() => true);
    public override List<Func<bool?>> Tasks => _activeTask.Tasks;
     
    public override bool IsComplete => _activeTask.IsComplete;
    public BranchingHardcoreTask(Func<bool> branch, HardcoreTaskBase? trueTask, HardcoreTaskBase? falseTask, HcTaskConfiguration? config = null)
    {
        Predicate = branch;
        TrueTask = trueTask ?? new HardcoreTask(() => true);
        FalseTask = falseTask ?? new HardcoreTask(() => true);
        Config = config ?? HcTaskConfiguration.Default;
        Name = string.Join(", ", Tasks.Select(t => t.GetMethodInfo().Name));
        Location = string.Empty;
    }

    public BranchingHardcoreTask(Func<bool> branch, HardcoreTaskBase? trueTask, HardcoreTaskBase? falseTask, string name, HcTaskConfiguration? config = null)
    {
        Predicate = branch;
        TrueTask = trueTask ?? new HardcoreTask(() => true);
        FalseTask = falseTask ?? new HardcoreTask(() => true);
        Config = config ?? HcTaskConfiguration.Default;
        Name = name;
        Location = string.Empty;
    }
    public override void Begin()
    {
        if (IsComplete || IsExecuting)
        {
            Svc.Logger.Warning($"HcBranch: Beginning task {Name}.");
            return;
        }

        // initialize
        Svc.Logger.Information("HcBranch: Beginning task.");
        base.Begin();
        // set the active task.
        var res = Predicate();
        Svc.Logger.Information($"HcBranch: Predicate result: {res}. Choosing " +
            $"{(res ? "trueTask" : "falseTask")}.");
        _activeTask = (res ? TrueTask : FalseTask);

        if (_activeTask.Tasks.Count is 0)
        {
            Svc.Logger.Warning($"HcBranch: Cannot begin, active task has no tasks! {Name}");
            Abort();
        }

        // begin internal.
        Svc.Logger.Information($"HcBranch: Beginning active task: {_activeTask.Name}.");
        _activeTask.Begin();
    }

    public override bool? PerformTask()
    {
        // compute the result.
        Svc.Logger.Information($"HcBranch: Performing task at idx {_activeTask.CurrentTaskIdx} for active task ({_activeTask.Name})");
        var res = _activeTask.PerformTask();

        if (res is true)
        {
            Svc.Logger.Information($"HcBranch: Active task idx {_activeTask.CurrentTaskIdx} ({_activeTask.Name}) reported success.");
            if (_activeTask.IsComplete)
            {
                Svc.Logger.Information($"HcBranch: Active task ({_activeTask.Name}) is complete. Ending.");
                _activeTask.End();
                Svc.Logger.Information($"HcBranch: Ending Branching Task.");
                End();
            }
            return res;
        }

        if (res is null)
        {
            Svc.Logger.Warning($"HcBranch: Active task idx {_activeTask.CurrentTaskIdx} ({_activeTask.Name}) reported failure. Aborting.");
            _activeTask.Abort();
            Svc.Logger.Warning("HcBranch: Aborted inner task, checking completion.");
            Svc.Logger.Warning($"HcBranch: ActiveTask IsComplete: {_activeTask.IsComplete}");
        }
            Abort();
        // return the result.
        return res;
    }

    public override void Abort()
    {
        // abort the current internal task and move onto the next. If completed, mark it.
        if (!IsComplete)
            _activeTask.Abort();
        base.Abort();
    }
}

