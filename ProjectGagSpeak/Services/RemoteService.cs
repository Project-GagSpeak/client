using CkCommons;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Remote;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;

namespace GagSpeak.Services;

/// <summary>
///     Service to maintain the active devices selected for recording, 
///     and cache their recorded states.
/// </summary>
public sealed class RemoteService : DisposableMediatorSubscriberBase
{
    private readonly BuzzToyManager _manager;

    private Task _updateTask;
    private CancellationTokenSource _updateCTS;

    private Dictionary<Guid, DevicePlotState> _interactableToys = new();
    public RemoteService(ILogger<RemoteService> logger, GagspeakMediator mediator, BuzzToyManager manager)
        : base(logger, mediator)
    {
        _manager = manager;

        _updateCTS = new CancellationTokenSource();
        _updateTask = RecordDataLoop();
        DurationTimer = new Stopwatch();

        // Monitor any changes to a toy.
        Mediator.Subscribe<ConfigSexToyChanged>(this, (msg) => OnBuzzToyChanged(msg.Type, msg.Item));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) =>
        {
            if (msg.Module is GagspeakModule.SexToys)
                UpdateInteractableToys();
        });
    }

    public IReadOnlyDictionary<Guid, DevicePlotState> ManagedDevices => _interactableToys;
    public IReadOnlyList<DevicePlotState> ActiveDevices => _interactableToys.Values.Where(toy => toy.IsPoweredOn).ToList();
    public bool ClientIsBeingBuzzed => ActiveDevices.Any() && DurationTimer.IsRunning;

    public readonly Stopwatch DurationTimer = new();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Logger.LogInformation("Disposing RemoteService and stopping all timers.");
        DurationTimer.Stop();
        _updateCTS.SafeCancel();
        Generic.Safe(() => _updateTask?.Wait(), true);
        _updateCTS.SafeDispose();
    }

    private void OnBuzzToyChanged(StorageChangeType changeType, BuzzToy item)
    {
        switch (changeType)
        {
            case StorageChangeType.Created:
                if (!_interactableToys.ContainsKey(item.Id))
                {
                    if (TryAddActiveDevice(item))
                        Logger.LogInformation($"Added new device {item.LabelName} to RemoteService.");
                    else
                        Logger.LogWarning($"Failed to add device {item.LabelName} to RemoteService.");
                }
                break;

            case StorageChangeType.Deleted:
                if (TryRemoveActiveDevice(item))
                    Logger.LogInformation($"Removed device {item.LabelName} from RemoteService.");
                else
                    Logger.LogWarning($"Failed to remove device {item.LabelName} from RemoteService.");
                break;

            case StorageChangeType.Modified:
                UpdateInteractableToys();
                break;
        }
    }

    private void UpdateInteractableToys()
    {
        foreach (var interactableToy in _manager.InteractableToys)
        {
            if (_interactableToys.ContainsKey(interactableToy.Id))
                continue;
            // add it in.
            if (TryAddActiveDevice(interactableToy))
                Logger.LogInformation($"Added {interactableToy.LabelName} from BuzzToyManager Reload.");
            else
                Logger.LogWarning($"Failed to add {interactableToy.LabelName} from BuzzToyManager Reload.");
        }
    }

    private async Task RecordDataLoop()
    {
        try
        {
            var recordDataOnCycle = true;
            while (!_updateCTS.IsCancellationRequested)
            {
                RecordPosition(recordDataOnCycle);
                recordDataOnCycle = !recordDataOnCycle; // Toggle recording state
                await Task.Delay(10, _updateCTS.Token);
            }
        }
        catch (TaskCanceledException) { /* Consume */ }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in RemoteService UpdateLoop.");
        }
    }

    public void StartRecording()
    {
        if (DurationTimer.IsRunning)
        {
            Logger.LogWarning("Timer is already running.");
            return;
        }

        Logger.LogInformation($"Starting recording SexToy data stream");
        DurationTimer.Start();
    }

    public void StopRecording()
    {
        if (!DurationTimer.IsRunning)
        {
            Logger.LogWarning("Timer is not running.");
            return;
        }

        Logger.LogInformation($"Stopping SexToy data stream recording");
        DurationTimer.Stop();

        foreach (var device in ActiveDevices)
            device.PowerDown();
    }

    public bool TryAddActiveDevice(BuzzToy device)
    {
        if (_interactableToys.ContainsKey(device.Id))
        {
            Logger.LogWarning($"Device {device.LabelName} is already managed in RemoteService.");
            return false;
        }

        if (_interactableToys.TryAdd(device.Id, new DevicePlotState(device)))
            return true;

        Logger.LogError($"Failed to add device {device.LabelName} to RemoteService.");
        return false;
    }

    public bool TryRemoveActiveDevice(BuzzToy device)
    {
        if (_interactableToys.Remove(device.Id))
            return true;

        Logger.LogWarning($"Device {device.LabelName} failed to remove from the InteractableDevices.");
        return false;
    }

    private void RecordPosition(bool sendToMotors)
    {
        if (!sendToMotors)
            return;

        foreach (var device in ActiveDevices)
            device.SendLatestToToys();
    }
}
