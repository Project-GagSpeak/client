using Dalamud.Plugin.Services;
using OtterGui.Log;
using OtterGui.Services;
using OtterGui.Tasks;

namespace OtterGui.Classes;

/// <summary> Manage certain actions to only occur on framework updates. </summary>
public class FrameworkManager : IDisposable, IService
{
    public readonly IFramework Framework;

    private readonly Logger                                 _log;
    private readonly Dictionary<string, Action>             _important = [];
    private readonly Dictionary<string, Action>             _onTick    = [];
    private readonly LinkedList<(DateTime, string, Action)> _delayed   = [];

    public FrameworkManager(IFramework framework, Logger log)
    {
        Framework        =  framework;
        _log             =  log;
        Framework.Update += OnUpdate;
    }

    public List<string> Important
    {
        get
        {
            lock (_important)
            {
                return _important.Keys.ToList();
            }
        }
    }

    public List<string> OnTick
    {
        get
        {
            lock (_onTick)
            {
                return _onTick.Keys.ToList();
            }
        }
    }

    public List<(DateTime, string)> Delayed
    {
        get
        {
            lock (_delayed)
            {
                return _delayed.Select(t => (t.Item1, t.Item2)).ToList();
            }
        }
    }

    /// <summary>
    /// Register an action that is not time critical.
    /// One action per frame will be executed.
    /// On dispose, any remaining actions will be executed.
    /// </summary>
    public void RegisterOnTick(string tag, Action action)
    {
        lock (_onTick)
        {
            _onTick[tag] = action;
        }
    }

    /// <summary>
    /// Register an action that should be executed on the next frame.
    /// All of those actions will be executed in the next frame.
    /// If there are more than one, they will be launched in separated tasks, but waited for.
    /// </summary>
    public void RegisterImportant(string tag, Action action)
    {
        lock (_important)
        {
            _important[tag] = action;
        }
    }


    /// <summary>
    /// Register an action that is expected to be delayed.
    /// One action per frame will be executed when the delay has been waited for.
    /// On dispose, any remaining actions will be executed.
    /// If the action is already registered and the desired time is earlier, it will be updated,
    /// if it is later, it will be ignored.
    /// </summary>
    public void RegisterDelayed(string tag, Action action, TimeSpan delay)
    {
        var desiredTime = DateTime.UtcNow + delay;
        lock (_delayed)
        {
            var node = _delayed.First;
            if (node == null)
            {
                _delayed.AddFirst((desiredTime, tag, action));
                return;
            }

            LinkedListNode<(DateTime, string, Action)>? delete    = null;
            LinkedListNode<(DateTime, string, Action)>? addBefore = null;
            while (node != null)
            {
                if (delete == null && node.Value.Item2 == tag)
                {
                    if (node.Value.Item1 < desiredTime)
                        return;

                    delete = node;
                }

                if (addBefore == null && node.Value.Item1 > desiredTime)
                    addBefore = node;
                node = node.Next;
            }

            if (addBefore != null)
                _delayed.AddBefore(addBefore, (desiredTime, tag, action));
            else
                _delayed.AddLast((desiredTime, tag, action));

            if (delete != null)
                _delayed.Remove(delete);
        }
    }

    public void Dispose()
    {
        Framework.Update -= OnUpdate;
        foreach (var (_, action) in _onTick)
            action();

        _onTick.Clear();

        foreach (var (_, _, action) in _delayed)
            action();
        _delayed.Clear();
    }

    private void OnUpdate(IFramework _)
    {
        try
        {
            HandleOnTick(_onTick);
            HandleAllTasks(_important);
            HandleDelayed(_delayed);
        }
        catch (Exception e)
        {
            _log.Error($"Problem saving data:\n{e}");
        }
    }

    private static void HandleDelayed(LinkedList<(DateTime, string, Action)> list)
    {
        if (list.Count == 0)
            return;

        var                                         now = DateTime.UtcNow;
        LinkedListNode<(DateTime, string, Action)>? node;
        lock (list)
        {
            node = list.First;
            if (node != null && node.Value.Item1 < now)
                list.RemoveFirst();
            else
                node = null;
        }

        node?.Value.Item3.Invoke();
    }

    private static void HandleOnTick(IDictionary<string, Action> dict)
    {
        if (dict.Count == 0)
            return;

        Action action;
        lock (dict)
        {
            (var key, action) = dict.First();
            dict.Remove(key);
        }

        action();
    }

    private static void HandleAllTasks(IDictionary<string, Action> dict)
    {
        if (dict.Count < 2)
        {
            HandleOnTick(dict);
        }
        else
        {
            Task[] tasks;
            lock (dict)
            {
                tasks = dict.Values.Select(AwaitedTask.Run).ToArray();
                dict.Clear();
            }

            Task.WaitAll(tasks);
        }
    }
}
