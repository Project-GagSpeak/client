using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Ipc;

namespace GagSpeak.Interop;

/// <summary>
///   Optional pathfinding through vnavmesh. When it is loaded and has a mesh for the current zone,
///   forced movement can route around obstacles instead of walking a straight line into them. <para />
///   State queries are cached so callers can poll these every frame without an IPC call per frame.
/// </summary>
public sealed class IpcCallerVnavmesh : IIpcCaller
{
    private const int ReadyRefreshMs = 1000;
    private const int BusyRefreshMs = 250;

    // API Getters
    private readonly ICallGateSubscriber<bool> NavIsReady;
    private readonly ICallGateSubscriber<bool> PathfindInProgress;
    private readonly ICallGateSubscriber<bool> PathIsRunning;

    // API Enactors
    private readonly ICallGateSubscriber<Vector3, bool, float, bool> PathfindAndMoveCloseTo;
    private readonly ICallGateSubscriber<bool, object> SetMovementAllowed;
    private readonly ICallGateSubscriber<object> PathStop;

    private bool _ready;
    private long _nextReadyCheck;
    private bool _busy;
    private long _nextBusyCheck;

    public IpcCallerVnavmesh()
    {
        NavIsReady = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        PathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");
        PathIsRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");

        PathfindAndMoveCloseTo = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, float, bool>("vnavmesh.SimpleMove.PathfindAndMoveCloseTo");
        SetMovementAllowed = Svc.PluginInterface.GetIpcSubscriber<bool, object>("vnavmesh.Path.SetMovementAllowed");
        PathStop = Svc.PluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        var wasAvailable = APIAvailable;
        APIAvailable = Svc.PluginInterface.InstalledPlugins.Any(p => p.IsLoaded && string.Equals(p.InternalName, "vnavmesh", StringComparison.OrdinalIgnoreCase));
        // Don't let a stale 'ready' from a previous load leak into a fresh one.
        if (APIAvailable != wasAvailable)
        {
            _ready = false;
            _nextReadyCheck = 0;
            _busy = false;
            _nextBusyCheck = 0;
        }
    }

    public void Dispose()
    { }

    /// <summary> If vnavmesh is loaded and has a navmesh for the current zone. Refreshed at most once a second. </summary>
    public bool IsReady()
    {
        if (!APIAvailable)
            return false;

        var now = Environment.TickCount64;
        if (now < _nextReadyCheck)
            return _ready;

        _nextReadyCheck = now + ReadyRefreshMs;
        _ready = Invoke(NavIsReady);
        return _ready;
    }

    /// <summary> If vnavmesh is still computing or walking a path. Refreshed at most every 250ms. </summary>
    public bool IsBusy()
    {
        if (!APIAvailable)
            return false;

        var now = Environment.TickCount64;
        if (now < _nextBusyCheck)
            return _busy;

        _nextBusyCheck = now + BusyRefreshMs;
        _busy = Invoke(PathfindInProgress) || Invoke(PathIsRunning);
        return _busy;
    }

    /// <summary> Pathfinds to within <paramref name="range"/> of <paramref name="dest"/> and starts walking. </summary>
    /// <returns> false if the request was not accepted (unavailable, or vnavmesh is still busy with a previous one). </returns>
    public bool MoveCloseTo(Vector3 dest, float range)
    {
        if (!APIAvailable)
            return false;

        try
        {
            SetMovementAllowed.InvokeAction(true);
            var fly = Svc.Condition[ConditionFlag.InFlight] || Svc.Condition[ConditionFlag.Diving];
            var accepted = PathfindAndMoveCloseTo.InvokeFunc(dest, fly, range);
            if (accepted)
            {
                // It is busy the moment it accepts, so don't let a cached 'idle' read as 'gave up'.
                _busy = true;
                _nextBusyCheck = Environment.TickCount64 + BusyRefreshMs;
            }
            return accepted;
        }
        catch (Exception ex)
        {
            Svc.Logger.Warning($"vnavmesh MoveCloseTo failed: {ex.Message}");
            return false;
        }
    }

    /// <summary> Stops any path vnavmesh is walking. </summary>
    public void Stop()
    {
        if (!APIAvailable)
            return;

        try
        {
            PathStop.InvokeAction();
        }
        catch (Exception ex)
        {
            Svc.Logger.Warning($"vnavmesh Stop failed: {ex.Message}");
        }
        _busy = false;
        _nextBusyCheck = 0;
    }

    private static bool Invoke(ICallGateSubscriber<bool> gate)
    {
        try
        {
            return gate.InvokeFunc();
        }
        catch
        {
            // Provider went away between CheckAPI calls, treat as not ready.
            return false;
        }
    }
}
