namespace GagSpeak.PlayerControl;

/// <summary>
///     Hardcore Task Manager task stacking.
/// </summary>
public partial class HcTaskManager
{
    public HcGroupTaskBuilder CreateGroup(string name, HcTaskConfiguration? config = null)
        => new HcGroupTaskBuilder(this, name, config);

    public HcBranchTaskBuilder CreateBranch(Func<bool> conditionTask, string name, HcTaskConfiguration? config = null)
        => new HcBranchTaskBuilder(this, conditionTask, name, config);

    public HcCollectionTaskBuilder CreateCollection(string name, HcTaskConfiguration? config = null)
        => new HcCollectionTaskBuilder(this, name, config);

    public sealed class HcGroupTaskBuilder
    {
        private readonly HcTaskManager _manager;
        private readonly string _name;
        private readonly HcTaskConfiguration _config;
        private readonly List<Func<bool?>> _tasks = [];

        internal HcGroupTaskBuilder(HcTaskManager manager, string name, HcTaskConfiguration? config = null)
        {
            _manager = manager;
            _name = name;
            _config = config ?? HcTaskConfiguration.Default;
        }

        public HcGroupTaskBuilder Add(Func<bool?> func)
        {
            _tasks.Add(func);
            return this;
        }

        public HcGroupTaskBuilder Add(Func<bool> func)
        {
            _tasks.Add(() => func());
            return this;
        }

        public HcGroupTaskBuilder Add(Action action)
        {
            _tasks.Add(() => { action(); return true; });
            return this;
        }

        public HardcoreTaskGroup AsGroup()
            => new(_tasks, _name, _config);

        public void Enqueue()
        {
            if (_tasks.Count is 0) return;
            _manager.EnqueueOperation(new HardcoreTaskGroup(_tasks, _name, _config));
        }

        public void Insert()
        {
            if (_tasks.Count is 0) return;
            _manager.InsertHcTask(new HardcoreTaskGroup(_tasks, _name, _config));
        }
    }

    public sealed class HcBranchTaskBuilder
    {
        private readonly HcTaskManager _manager;
        private readonly string _name;
        private readonly HcTaskConfiguration _config;
        private Func<bool> _conditionTask;
        private HardcoreTaskBase _trueTask = new HardcoreTask(() => true);
        private HardcoreTaskBase _falseTask = new HardcoreTask(() => true);

        internal HcBranchTaskBuilder(HcTaskManager manager, Func<bool> conditionTask, string name, HcTaskConfiguration? config = null)
        {
            _manager = manager;
            _conditionTask = conditionTask;
            _name = name;
            _config = config ?? HcTaskConfiguration.Default;
        }

        public HcBranchTaskBuilder SetTrueTask(HardcoreTaskBase task)
        {
            _trueTask = task;
            return this;
        }

        public HcBranchTaskBuilder SetFalseTask(HardcoreTaskBase task)
        {
            _falseTask = task;
            return this;
        }

        public BranchingHardcoreTask AsBranch()
            => new(_conditionTask, _trueTask, _falseTask, _name, _config);

        public void Enqueue()
            => _manager.EnqueueOperation(new BranchingHardcoreTask(_conditionTask, _trueTask, _falseTask, _name, _config));

        public void Insert()
            => _manager.InsertHcTask(new BranchingHardcoreTask(_conditionTask, _trueTask, _falseTask, _name, _config));
    }

    public sealed class HcCollectionTaskBuilder
    {
        private readonly HcTaskManager _manager;
        private readonly string _name;
        private readonly HcTaskConfiguration _config;
        private readonly List<HardcoreTaskBase> _tasks = [];

        internal HcCollectionTaskBuilder(HcTaskManager manager, string name, HcTaskConfiguration? config = null)
        {
            _manager = manager;
            _name = name;
            _config = config ?? HcTaskConfiguration.Default;
        }

        public HcCollectionTaskBuilder Add(HardcoreTaskBase task)
        {
            _tasks.Add(task);
            return this;
        }

        public HardcoreTaskCollection AsCollection()
            => new(_tasks, _name, _config);

        public void Enqueue()
        {
            if (_tasks.Count is 0) return;
            _manager.EnqueueOperation(new HardcoreTaskCollection(_tasks, _name, _config));
        }

        public void Insert()
        {
            if (_tasks.Count is 0) return;
            _manager.InsertHcTask(new HardcoreTaskCollection(_tasks, _name, _config));
        }
    }
}
