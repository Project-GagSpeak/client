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

    public override int TotalTasks => _activeTask.TotalTasks;
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

            if (_activeTask.Finished)
            {
                // See if active task scope completed, then the branch is also complete by proxy.
                Svc.Logger.Verbose($"HcBranch: Active SubTask Completed! (Scope -> {Name}) (ActiveTask Scope -> {_activeTask.Name})");
                CurrentTaskIdx++;
            }
            return true;
        }
        // if returning null, we should end the active scope (which completes this branch as well).
        else if (res is null)
        {
            Svc.Logger.Warning($"HcBranch: Active SubTask Failed! (Scope -> {Name}) (ActiveTask Scope -> {_activeTask.Name})");
            _activeTask.End();
            return null;
        }

        // otherwise it was false, so we can just return false.
        return false;
    }

    // maybe force this to be a full end regardless? idk.
    public override void End()
    {
        // check outer scope.
        if (Finished && IsRunning)
        {
            Svc.Logger.Debug($"HcBranch: Ending Outer Task! (Scope -> {Name})");
            StartTime = 0;
            CurrentTaskIdx = TotalTasks;
            return;
        }

        if (!_activeTask.Finished)
        {
            Svc.Logger.Debug($"HcBranch: Ending Active SubTask! (Scope -> {Name}) (ActiveTask Scope -> {_activeTask.Name})");
            _activeTask.End();
            CurrentTaskIdx++;
        }
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
    public HardcoreTaskCollection(IEnumerable<HardcoreTaskBase> tasks, HcTaskConfiguration config)
    {
        Name = tasks.FirstOrDefault()?.Name ?? string.Empty;
        Config = config;
        StoredTasks = tasks.ToList();
    }
    public HardcoreTaskCollection(IEnumerable<HardcoreTaskBase> tasks, string name, HcTaskConfiguration config)
    {
        Name = name;
        Config = config;
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
                Svc.Logger.Debug($"InnerScope Timeouts were used, so beginning next task in collection. (Scope -> {Name}) (Inner Scope -> {_currentTask.Name})");
                _currentTask.End();
                CurrentTaskIdx++;
                if (!Finished)
                    _currentTask.Begin();
                return false;
            }
            // end current scope.
            End();
            return false;
        }

        var res = _currentTask.PerformTask();
        // If the active task was true, and also reached it's end, then end the branching task.
        if (res is true)
        {
            Svc.Logger.Verbose($"HcCollection: Active SubTask Success! (Scope -> {Name}) (ActiveTask Scope -> {_currentTask.Name})");
            // if we are using inner timeouts, we should reset it.
            if (Config.InnerTimouts)
                _currentTask.ResetTimeout();

            if (_currentTask.Finished)
            {
                // See if active task scope completed, then the branch is also complete by proxy.
                Svc.Logger.Verbose($"HcCollection: Active SubTask Completed! (Scope -> {Name}) (ActiveTask Scope -> {_currentTask.Name})");
                CurrentTaskIdx++;
                ResetTimeout();
                // begin the next task if there is one.
                if (!Finished)
                    _currentTask.Begin();
            }
        }
        // if returning null, we should end the active scope (which completes this branch as well).
        else if (res is null)
        {
            Svc.Logger.Warning($"HcCollection: Active SubTask Failed! (Scope -> {Name}) (ActiveTask Scope -> {_currentTask.Name})");
            // calls the inner scope end.
            _currentTask.End();
            // if after this is called it is marked as finished, we should move the index forward, but only if it is.
            if (_currentTask.Finished)
            {
                CurrentTaskIdx++;
                ResetTimeout();
                // begin the next task if there is one.
                if (!Finished)
                    _currentTask.Begin();
            }
        }
        return res;
    }

    // maybe force this to be a full end regardless? idk.
    public override void End()
    {
        // if the current collection scope is finished and still running, end that.
        if (Finished && IsRunning)
        {
            Svc.Logger.Verbose($"HcCollection: Ending Outer Task! (Scope -> {Name})");
            StartTime = 0;
            CurrentTaskIdx = TotalTasks;
            return;
        }
        // if the current task is not yet finished, finish it instead of the whole collection.
        if (!_currentTask.Finished)
        {
            Svc.Logger.Verbose($"HcCollection: Ending Current SubTask! (Scope -> {Name}) (CurrentTask Scope -> {_currentTask.Name})");
            _currentTask.End();
            CurrentTaskIdx++;
            ResetTimeout();
            // begin the next task if there is one.
            if (!Finished)
                _currentTask.Begin();
        }
    }
}

