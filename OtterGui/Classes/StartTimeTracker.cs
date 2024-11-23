using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Classes;

public class StartTimeTracker
{
    private record TimerTuple
    {
        public readonly Stopwatch Watch = new();
        public          TimeSpan  StartDelay;
        public          int       Thread;
    }

    private readonly DateTime                                 _constructionTime = DateTime.UtcNow;
    private readonly ConcurrentDictionary<string, TimerTuple> _timers           = [];

    public readonly struct TimingStopper : IDisposable
    {
        private readonly Stopwatch _watch;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TimingStopper(StartTimeTracker manager, string name)
        {
            var tuple = manager.Get(name);
            _watch = tuple.Watch;
            _watch.Start();
            tuple.StartDelay = DateTime.UtcNow - manager._constructionTime;
            tuple.Thread     = Thread.CurrentThread.ManagedThreadId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => _watch.Stop();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TimingStopper Measure(string name)
        => new(this, name);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Measure(string name, Action action)
    {
        using var t = Measure(name);
        action();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TRet Measure<TRet>(string name, Func<TRet> func)
    {
        using var t = Measure(name);
        return func();
    }

    private TimerTuple Get(string name)
    {
        if (!_timers.TryGetValue(name, out var tuple))
        {
            tuple = new TimerTuple();
            _timers.TryAdd(name, tuple);
        }

        return tuple;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Start(string name)
    {
        var tuple = Get(name);
        tuple.Watch.Start();
        tuple.StartDelay = DateTime.UtcNow - _constructionTime;
        tuple.Thread     = Thread.CurrentThread.ManagedThreadId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Stop(string name)
        => Get(name).Watch.Stop();

    private UserRegex _filter = UserRegex.Empty;

    public void Draw(string label)
    {
        using var id = ImRaii.PushId(label);
        UserRegex.DrawRegexInput("##filter", ref _filter, "Filter...", null, ImGui.GetContentRegionAvail().X, 0xFF4040F0);
        using var table = ImRaii.Table("##startTimeTable", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);

        ImGui.TableSetupColumn("Name",   ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Time",   ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Start",  ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("End",    ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Thread", ImGuiTableColumnFlags.None, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        foreach (var (name, tuple) in _timers.Where(t => _filter.Match(t.Key)).OrderBy(t => t.Value.StartDelay))
        {
            ImGuiUtil.DrawTableColumn(name);
            var time = tuple.Watch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign(time.ToString("F4", CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign(tuple.StartDelay.TotalMilliseconds.ToString("F4", CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign((time + tuple.StartDelay.TotalMilliseconds).ToString("F4", CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign(tuple.Thread.ToString());
        }
    }
}
