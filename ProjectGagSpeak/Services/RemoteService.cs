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
                // if the device already exists dont add the device.
                if (!_interactableToys.ContainsKey(item.Id) && item.ValidForRemotes)
                {
                    if (TryAddActiveDevice(item))
                        Logger.LogInformation($"Added new device {item.LabelName} to RemoteService.");
                    else
                        Logger.LogWarning($"Failed to add device {item.LabelName} to RemoteService.");
                }
                break;

            case StorageChangeType.Deleted:
                // Try removing the device, if it fails, show why.
                if (TryRemoveActiveDevice(item))
                    Logger.LogInformation($"Removed device {item.LabelName} from RemoteService.");
                else
                    Logger.LogWarning($"Failed to remove device {item.LabelName} from RemoteService.");
                break;

            case StorageChangeType.Modified:
                // Update all toys, additions and removals.
                UpdateInteractableToys();
                break;
        }
    }

    private void UpdateInteractableToys()
    {
        foreach (var interactableToy in _manager.InteractableToys)
        {
            // if the toy already exists currently
            if (_interactableToys.ContainsKey(interactableToy.Id))
            {
                // and the toy is no longer valid for remotes, remove it.
                if (!interactableToy.ValidForRemotes)
                {
                    // Try and remove the device, if it fails, log the error.
                    if (TryRemoveActiveDevice(interactableToy))
                        Logger.LogInformation($"Removed {interactableToy.LabelName} from BuzzToyManager Reload.");
                    else
                        Logger.LogWarning($"Failed to remove {interactableToy.LabelName} from BuzzToyManager Reload.");
                }
            }
            else
            {
                // Otherwise, try to add it in. If it fails, log the error.
                if (TryAddActiveDevice(interactableToy))
                    Logger.LogInformation($"Added {interactableToy.LabelName} from BuzzToyManager Reload.");
                else
                    Logger.LogWarning($"Failed to add {interactableToy.LabelName} from BuzzToyManager Reload.");
            }
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
        // try and find the device to remove, as we have to shut it down first.
        if (_interactableToys.TryGetValue(device.Id, out var match))
        {
            if (match.IsPoweredOn)
                match.PowerDown();

            // now remove it.
            if(_interactableToys.Remove(device.Id));
            {
                Logger.LogInformation($"Device {device.LabelName} removed from InteractableDevices.");
                return true;
            }
        }

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
