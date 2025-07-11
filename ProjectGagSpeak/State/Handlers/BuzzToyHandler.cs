using GagSpeak.FileSystems;
using GagSpeak.Interop;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;

namespace GagSpeak.State.Handlers;
public sealed class RemoteHandler : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerIntiface _ipc;
    private readonly BuzzToyManager _manager;
    private readonly RemoteService _service;

    public RemoteHandler(ILogger<RemoteHandler> logger, GagspeakMediator mediator,
        IpcCallerIntiface ipc, BuzzToyManager manager, RemoteService service)
        : base(logger, mediator)
    {
        _ipc = ipc;
        _manager = manager;
        _service = service;

        /// Monitors for changes to the client players devices.
        Mediator.Subscribe<ConfigSexToyChanged>(this, (msg) => OnClientToyChange(msg.Type, msg.Item));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) =>
        {
            if (msg.Module is GagspeakModule.SexToys)
                UpdateClientToys();
        });
    }

    // What to do when the client's toys change state.
    private void OnClientToyChange(StorageChangeType changeType, BuzzToy item)
    {
        switch (changeType)
        {
            case StorageChangeType.Created:
                _service.TryAddDeviceForKinkster(MainHub.UID, item);
                break;

            case StorageChangeType.Deleted:
                _service.TryRemoveDeviceForKinkster(MainHub.UID, item);
                break;

            case StorageChangeType.Modified:
                UpdateClientToys();
                break;
        }
    }

    // Add any missing active toys, and remove any toys that are no longer interactable.
    private void UpdateClientToys()
    {
        var clientServiceDevices = _service.ClientDevices;
        foreach (var toy in _manager.InteractableToys)
        {
            // if the toy already exists currently and is no longer valid, remove it. Otherwise, try and add it.
            if (clientServiceDevices.Any(dps => dps.Equals(toy)) && !toy.ValidForRemotes)
                _service.TryRemoveDeviceForKinkster(MainHub.UID, toy);
            else
                _service.TryAddDeviceForKinkster(MainHub.UID, toy);
        }
    }

    public bool TryChangeRemoteMode(RemoteService.RemoteMode newMode)
    {
        // Lots of things here can reject the change of mode.
        Logger.LogDebug($"Attempting to change remote mode to {newMode}.");
        return false;
    }

    public void StartPattern(Guid patternId)
        => StartPattern(patternId, TimeSpan.Zero, TimeSpan.Zero);

    public void StartPattern(Guid patternId, TimeSpan customStart, TimeSpan customDuration)
    {
        // Handle Logic Later
        Logger.LogDebug($"Beginning pattern {patternId} on all toys.");
        // would need to find from storage or something.
    }

    public void StartPattern(Pattern pattern)
        => StartPattern(pattern, pattern.StartPoint, pattern.PlaybackDuration);

    public void StartPattern(Pattern pattern, TimeSpan customStart, TimeSpan customDuration)
    {
        // Handle Logic Later
        Logger.LogDebug($"Beginning pattern {pattern.Label} on all toys.");
    }

    public void StopActivePattern()
    {
        // Handle logic later
        Logger.LogDebug($"Stopping active pattern for all toys.");
    }
}





