using System.Reflection;

namespace GagSpeak.PlayerControl;

// Maintainers Note: when you are finished debugging you can remove any log messages internally to
// help improve performance or comment them out.
// Leave in for now until we are confident the process works for all combinations of cases.

// Base for hardcore tasks.
public abstract class HardcoreTaskBase
{
    public string Name { get; init; }
    public HcTaskConfiguration Config { get; init; }
    public int CurrentTaskIdx { get; protected set; } = 0;

    // Timing the Task internally.
    public long StartTime { get; protected set; } = 0;

    public void ResetTimeout() => StartTime = Environment.TickCount64;

    // virtual to be overriden by parent methods that need to access the individual timers.
    public virtual bool IsRunning => StartTime is not 0;
    public virtual long ElapsedTime => Environment.TickCount64 - StartTime;
    public virtual bool HasTimedOut => Config.TimeoutAt > 0 && (Environment.TickCount64 - StartTime) > Config.TimeoutAt;

    // abstract vars that must be implemented.
    public abstract int TotalTasks { get; }
    public abstract bool Finished { get; }
    public abstract bool? PerformTask();
    public abstract void Begin();
    public abstract void End();
}

public class HardcoreTask : HardcoreTaskBase
{
    private readonly Func<bool?> _task;
    public override int TotalTasks => 1;
    public override bool Finished => CurrentTaskIdx >= 1;
    public HardcoreTask(Func<bool?> task, HcTaskConfiguration? config = null)
    {
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Config = config ?? HcTaskConfiguration.Default;
        _task = task;
    }
    public HardcoreTask(Func<bool?> task, string name, HcTaskConfiguration? config = null)
    {
        Name = name;
        Config = config ?? HcTaskConfiguration.Default;
        _task = task;
    }
    public HardcoreTask(Func<bool> task, HcTaskConfiguration? config = null)
    {
        Name = task.GetMethodInfo().Name ?? string.Empty;
        Config = config ?? HcTaskConfiguration.Default;
        _task = () => task();
    }
    public HardcoreTask(Func<bool> task, string name, HcTaskConfiguration? config = null)
    {
        Name = name;
        Config = config ?? HcTaskConfiguration.Default;
        _task = () => task();
    }
    public HardcoreTask(Action action, HcTaskConfiguration? config = null)
    {
        Name = action.GetMethodInfo().Name ?? string.Empty;
        Config = config ?? HcTaskConfiguration.Default;
        _task = () => { action(); return true; };
    }
    public HardcoreTask(Action action, string name, HcTaskConfiguration? config = null)
    {
        Name = name;
        Config = config ?? HcTaskConfiguration.Default;
        _task = () => { action(); return true; };
    }

    // move outside HardcoreTask to an extention method when possible.
    public override void Begin() => ResetTimeout();
    public override bool? PerformTask()
    {
        if (HasTimedOut)
        {
            Svc.Logger.Warning($"HcGroup: Task group timed out! (Scope -> {Name})");
            End();
            return false;
        }

        var res = _task();
        if (res is true)
        {
            Svc.Logger.Verbose($"HcTask: Success! (Scope -> {Name})");
            CurrentTaskIdx++;
        }
        else if (res is null)
        {
            Svc.Logger.Warning($"HcTask: Failed! (Scope -> {Name})");
            End();
        }
        return res;
    }
    public override void End()
    {
        StartTime = 0;
        CurrentTaskIdx = 1;
        Config.OnEnd?.Invoke();
    }
}

/// <summary>
///     Represents a sequence of tasks to be performed. <para />
///     This can also be a single task. But this 'group' of tasks are confined to the associated configuration.
/// </summary>
public class HardcoreTaskGroup : HardcoreTaskBase
{
    private List<Func<bool?>> _tasks;
    public override int TotalTasks => _tasks.Count;
    public override bool Finished => CurrentTaskIdx >= _tasks.Count;
    public HardcoreTaskGroup(IEnumerable<Func<bool?>> tasks, HcTaskConfiguration config)
    {
        _tasks = tasks.ToList();
        Config = config;
        Name = string.Join(", ", _tasks.Select(t => t.GetMethodInfo().Name));
    }
    public HardcoreTaskGroup(IEnumerable<Func<bool?>> tasks, string name, HcTaskConfiguration config)
    {
        _tasks = tasks.ToList();
        Config = config;
        Name = name;
    }
    public HardcoreTaskGroup(IEnumerable<Func<bool>> tasks, HcTaskConfiguration config)
    {
        _tasks = tasks.Select(t => (Func<bool?>)(() => t())).ToList();
        Config = config;
        Name = string.Join(", ", _tasks.Select(t => t.GetMethodInfo().Name));
    }
    public HardcoreTaskGroup(IEnumerable<Func<bool>> tasks, string name, HcTaskConfiguration config)
    {
        _tasks = tasks.Select(t => (Func<bool?>)(() => t())).ToList();
        Config = config;
        Name = name;
    }
    public HardcoreTaskGroup(IEnumerable<Action> actions, HcTaskConfiguration config)
    {
        _tasks = actions.Select(a => (Func<bool?>)(() => { a(); return true; })).ToList();
        Config = config;
        Name = string.Join(", ", actions.Select(a => a.GetMethodInfo().Name));
    }
    public HardcoreTaskGroup(IEnumerable<Action> actions, string name, HcTaskConfiguration config)
    {
        _tasks = actions.Select(a => (Func<bool?>)(() => { a(); return true; })).ToList();
        Config = config;
        Name = name;
    }

    public override void Begin() => ResetTimeout();
    public override bool? PerformTask()
    {
        if (HasTimedOut)
        {
            Svc.Logger.Warning($"HcGroup: Task group timed out! (Scope -> {Name})");
            End();
            return false;
        }

        var res = _tasks[CurrentTaskIdx]();
        if (res is true)
        {
            Svc.Logger.Verbose($"HcGroup: Success! (Scope -> {Name}) (Idx completed -> {CurrentTaskIdx})");
            CurrentTaskIdx++;
        }
        else if (res is null)
        {
            Svc.Logger.Warning($"HcGroup: Failed! (Scope -> {Name}) (Idx failed -> {CurrentTaskIdx})");
            End();
        }
        return res;
    }
    public override void End()
    {
        StartTime = 0;
        CurrentTaskIdx = TotalTasks;
        Config.OnEnd?.Invoke();
    }
}

// Branching variant, which executes one of two sets of tasks based on a predicate.
public class BranchingHardcoreTask : HardcoreTaskBase
{
    // change to private when done debugging.
    public readonly Func<bool> Predicate;
    public readonly HardcoreTaskBase TrueTask;
    public readonly HardcoreTaskBase FalseTask;
    private HardcoreTaskBase _activeTask;

    public override int TotalTasks => 1;
    public override bool Finished => CurrentTaskIdx >= TotalTasks;
    public override bool IsRunning => Config.InnerTimouts ? _activeTask.IsRunning : base.IsRunning;
    public override long ElapsedTime => Config.InnerTimouts ? _activeTask.ElapsedTime : base.ElapsedTime;
    public override bool HasTimedOut => Config.InnerTimouts ? _activeTask.HasTimedOut : base.HasTimedOut;
    public BranchingHardcoreTask(Func<bool> branch, HardcoreTaskBase? trueTask, HardcoreTaskBase? falseTask, HcTaskConfiguration? config = null)
    {
        Name = $"Branch-{branch.GetMethodInfo().Name}";
        Config = config ?? HcTaskConfiguration.Default;
        Predicate = branch;
        TrueTask = trueTask ?? new HardcoreTask(() => { }, $"DummyAct-{Name}");
        FalseTask = falseTask ?? new HardcoreTask(() => { }, $"DummyAct-{Name}");
        // dummy set the task until it is assigned.
        _activeTask = new HardcoreTask(() => { }, $"UNK_ACTIVE-{Name}");
    }
    public BranchingHardcoreTask(Func<bool> branch, HardcoreTaskBase? trueTask, HardcoreTaskBase? falseTask, string name, HcTaskConfiguration? config = null)
    {
        Name = name;
        Config = config ?? HcTaskConfiguration.Default;
        Predicate = branch;
        TrueTask = trueTask ?? new HardcoreTask(() => { }, $"DummyAct-{Name}");
        FalseTask = falseTask ?? new HardcoreTask(() => { }, $"DummyAct-{Name}");
        // dummy set the task until it is assigned.
        _activeTask = new HardcoreTask(() => { }, $"UNK_ACTIVE-{Name}");
    }
    public override void Begin()
    {
        if (IsRunning || Finished)
        {
            Svc.Logger.Warning($"HcBranch: Already Began! (Scope -> {Name})");
            return;
        }

        Svc.Logger.Verbose($"HcBranch: Beginning! (Scope -> {Name})");
        ResetTimeout();

        var res = Predicate();
        Svc.Logger.Verbose($"HcBranch: Predicate result: {res}. Assigning {(res ? "trueTask" : "falseTask")} " +
            $"with {(res ? TrueTask.TotalTasks : FalseTask.TotalTasks)} Tasks.");
        _activeTask = (res ? TrueTask : FalseTask);

        // regardless of how valid it is, still begin it.
        _activeTask.Begin();
        if (_activeTask.TotalTasks is 0)
        {
            Svc.Logger.Warning($"HcBranch: Has no tasks, ending early! (Scope -> {Name})");
            End();
        }
    }

    public override bool? PerformTask()
    {
        if (HasTimedOut) // This references either the outer of inner scope based on what is defined.
        {
            Svc.Logger.Warning($"HcBranch: Timed out! (Scope -> {Name}) (Inner Scope -> {_activeTask.Name}) [InnerTimeout? {Config.InnerTimouts}]");
            if (Config.InnerTimouts)
                _activeTask.End();
            End();
            return false;
        }

        // run the perform task call on the active task.
        // Keep in mind this 'perform task' also will update its index upon success,
        // so we want to be checking for completion afterwards if true.
        var res = _activeTask.PerformTask();
        // If the active task was true, and also reached it's end, then end the branching task.
        if (res is true)
        {
            Svc.Logger.Verbose($"HcBranch: Active SubTask Success! (Scope -> {Name}) (ActiveTask Scope -> {_activeTask.Name})");
            // if we are using innerTimeouts, we should reset the startTime.
            if (Config.InnerTimouts)
                _activeTask.ResetTimeout();
        }
        // if returning null, we should end the active scope (which completes this branch as well).
        else if (res is null)
        {
            Svc.Logger.Warning($"HcBranch: Active SubTask Failed! (Scope -> {Name}) (ActiveTask Scope -> {_activeTask.Name})");
            _activeTask.End();
        }

        // Safety net any timeouts or failures that resulted in completion to assure movement..
        if (_activeTask.Finished)
        {
            // See if active task scope completed, then the branch is also complete by proxy.
            Svc.Logger.Verbose($"HcBranch: Active SubTask Completed! (Scope -> {Name}) (ActiveTask Scope -> {_activeTask.Name})");
            CurrentTaskIdx++;
        }

        return res;
    }
    public override void End()
    {
        Svc.Logger.Debug($"HcBranch: Ending called, ending subtasks and current scope: (Scope -> {Name}) (Inner Scope -> {_activeTask.Name})");
        if (!_activeTask.Finished)
            _activeTask.End();
        StartTime = 0;
        CurrentTaskIdx = TotalTasks;
        Config.OnEnd?.Invoke();
    }
}

// this might get a little trippy / recursive, but hey, if it works it works.
public class HardcoreTaskCollection : HardcoreTaskBase
{
    // make private after debugging.
    public readonly List<HardcoreTaskBase> StoredTasks;
    private HardcoreTaskBase _currentTask => StoredTasks[CurrentTaskIdx];
    public override int TotalTasks => StoredTasks.Count;
    public override bool Finished => CurrentTaskIdx >= StoredTasks.Count;
    public override bool IsRunning => Config.InnerTimouts ? _currentTask.IsRunning : base.IsRunning;
    public override long ElapsedTime => Config.InnerTimouts ? _currentTask.ElapsedTime : base.ElapsedTime;
    public override bool HasTimedOut => Config.InnerTimouts ? _currentTask.HasTimedOut : base.HasTimedOut;
    public HardcoreTaskCollection(IEnumerable<HardcoreTaskBase> tasks, HcTaskConfiguration? config = null)
    {
        Name = tasks.FirstOrDefault()?.Name ?? string.Empty;
        Config = config ?? HcTaskConfiguration.Default;
        StoredTasks = tasks.ToList();
    }
    public HardcoreTaskCollection(IEnumerable<HardcoreTaskBase> tasks, string name, HcTaskConfiguration? config = null)
    {
        Name = name;
        Config = config ?? HcTaskConfiguration.Default;
        StoredTasks = tasks.ToList();
    }

    public override void Begin()
    {
        if (IsRunning || Finished)
        {
            Svc.Logger.Warning($"HcCollection: Already Began! (Scope -> {Name})");
            return;
        }

        // If the collection has 0 tasks, end immediately.
        if (StoredTasks.Count is 0)
        {
            Svc.Logger.Debug($"HcCollection: No tasks to begin! Ending immediately. (Scope -> {Name})");
            End();
            return;
        }

        Svc.Logger.Verbose($"HcCollection: Beginning! (Scope -> {Name})");
        StartTime = Environment.TickCount64;
        // Begin inner at first index.
        _currentTask.Begin();
    }

    public override bool? PerformTask()
    {
        // if the task has timed out, then we should end the CURRENT SCOPED TASK.
        if (HasTimedOut)
        {
            Svc.Logger.Verbose($"HcCollection: Timed out! (Scope -> {Name}) (Inner Scope -> {_currentTask.Name}) [InnerTimeout? {Config.InnerTimouts}]");
            if (Config.InnerTimouts)
            {
                // end inner scope.
                Svc.Logger.Debug($"HcCollection: InnerTimeout used, ending innerscope: (Inner Scope -> {_currentTask.Name}) [IDX: {CurrentTaskIdx}]");
                // if we are on our last task, wrap things up and end.
                if (CurrentTaskIdx + 1 == TotalTasks)
                {
                    Svc.Logger.Debug($"HcCollection: Last task in collection timed out: (Scope -> {Name})");
                    End();
                }
                else
                {
                    _currentTask.End();
                    CurrentTaskIdx++;
                    Svc.Logger.Debug($"HcCollection: Moving to next inner scope: (Inner Scope -> {_currentTask.Name}) [IDX: {CurrentTaskIdx}]");
                    ResetTimeout();
                    _currentTask.Begin();
                }
                return false;
            }
            // end current scope.
            End();
            return false;
        }

        var res = _currentTask.PerformTask();
        if (res is true)
        {
            Svc.Logger.Verbose($"HcCollection: Active SubTask Success! (Scope -> {Name}) (Inner Scope -> {_currentTask.Name})");
        }
        else if (res is null)
        {
            Svc.Logger.Warning($"HcCollection: Active SubTask Failed! (Scope -> {Name}) (Inner Scope -> {_currentTask.Name})");
            _currentTask.End();
        }

        if (_currentTask.Finished)
        {
            Svc.Logger.Verbose($"HcCollection: Active SubTask Completed after performed task! (Scope -> {Name}) (Inner Scope -> {_currentTask.Name})");
            if (CurrentTaskIdx + 1 == TotalTasks)
                End();
            else
            {
                Svc.Logger.Debug($"HcCollection: Moving to next inner scope: (Inner Scope -> {_currentTask.Name}) [IDX: {CurrentTaskIdx}]");
                CurrentTaskIdx++;
                ResetTimeout();
                _currentTask.Begin();
            }
        }

        return res;
    }

    public override void End()
    {
        // if we are not yet finished, bump the idx so that we are.
        if (!Finished)
        {
            Svc.Logger.Verbose($"HcCollection: End() requested, but not finished, forcing finish! (Scope -> {Name}) [Idx: {CurrentTaskIdx}]");
            _currentTask.End();
            CurrentTaskIdx = TotalTasks;
        }

        Svc.Logger.Verbose($"HcCollection: Ending Task Scope! (Scope -> {Name})");
        StartTime = 0;
        Config.OnEnd?.Invoke();
    }
}

