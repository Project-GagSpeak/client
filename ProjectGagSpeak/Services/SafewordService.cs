using Dalamud.Utility;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Listener;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;
// TODO LATER:::::::

// The most fundamentally important service in the entire application.
// helps revert any active states applied to the player when used.
public sealed class SafewordService : MediatorSubscriberBase, IHostedService
{
    private readonly MainHub _hub;
    private readonly GlobalData _playerManager;
    private readonly PairManager _pairManager;
    private readonly VisualStateListener _visuaListener; // can directly call upon all reversion operations.
    private readonly ToyboxStateListener _toyboxListener;

    public SafewordService(ILogger<SafewordService> logger, GagspeakMediator mediator,
        MainHub hub, GlobalData playerManager, PairManager pairManager) : base(logger, mediator)
    {
        _hub = hub;
        _playerManager = playerManager;
        _pairManager = pairManager;

        // set the chat log up.
        Mediator.Subscribe<SafewordUsedMessage>(pairManager, (msg) => SafewordUsed(msg.UID));
        Mediator.Subscribe<SafewordHardcoreUsedMessage>(this, (msg) => HardcoreSafewordUsed(msg.UID));
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (msg) => CheckCooldown());
    }

    private DateTime TimeOfLastSafewordUsed = DateTime.MinValue;
    private DateTime TimeOfLastHardcoreSafewordUsed = DateTime.MinValue;
    public static bool SafewordOnCD = false;
    public static bool HardcoreSafewordOnCD = false;

    private async void SafewordUsed(string isolatedUID)
    {
        try
        {
/*            // return if it has not yet been 5 minutes since the last use.
            if (SafewordOnCD)
            {
                Logger.LogWarning("Hardcore Safeword was used too soon after the last use. Must wait 5 minutes.", LoggerType.Safeword);
                return;
            }

            // set the time of the last safeword used.
            TimeOfLastSafewordUsed = DateTime.Now;
            Logger.LogInformation("Safeword was used.", LoggerType.Safeword);

            // grab active pattern first if any.
            if (_clientConfigs.AnyPatternIsPlaying)
            {
                Logger.LogInformation("Stopping active pattern.", LoggerType.Safeword);
                _toyboxManager.DisablePattern(_clientConfigs.ActivePatternGuid());
                Logger.LogInformation("Active pattern stopped.", LoggerType.Safeword);
            }

            // disable all other active things.
            await _clientConfigs.DisableEverythingDueToSafeword();
            await _appearanceManager.DisableAllDueToSafeword();

            // do direct updates so they apply first client side, then push to the server. The callback can validate these changes.
            if (_playerManager.GlobalPerms is not null)
            {
                _playerManager.GlobalPerms.ChatGarblerActive = false;
                _playerManager.GlobalPerms.ChatGarblerLocked = false;
                _playerManager.GlobalPerms.WardrobeEnabled = false;
                _playerManager.GlobalPerms.GagVisuals = false;
                _playerManager.GlobalPerms.RestraintSetVisuals = false;
                _playerManager.GlobalPerms.PuppeteerEnabled = false;
                _playerManager.GlobalPerms.ToyboxEnabled = false;
                _playerManager.GlobalPerms.LockToyboxUI = false;
                _playerManager.GlobalPerms.ToyIntensity = 0;
                _playerManager.GlobalPerms.SpatialVibratorAudio = false;

                Logger.LogInformation("Pushing Global updates to the server.", LoggerType.Safeword);
                _ = _hub.UserPushAllGlobalPerms(new(MainHub.PlayerUserData, MainHub.PlayerUserData, _playerManager.GlobalPerms, UpdateDir.Own));
                Logger.LogInformation("Global updates pushed to the server.", LoggerType.Safeword);
            }
            Logger.LogInformation("Everything Disabled.", LoggerType.Safeword);

            // Revert any pairs we have the "InHardcoreMode" permission set.
            // Only do it for the isolated UID if it is not empty.
            if (!isolatedUID.IsNullOrWhitespace())
            {
                // do it for the single pair we want to.
                var isolatedPair = _pairManager.DirectPairs.FirstOrDefault(user => string.Equals(user.UserData.UID, isolatedUID, StringComparison.OrdinalIgnoreCase));
                if (isolatedPair != null && isolatedPair.UserPair.OwnPairPerms.InHardcore)
                {
                    // put us out of hardcore, and disable any active
                    isolatedPair.OwnPerms.InHardcore = false;
                    // send the updates to the server.
                    if (MainHub.ServerStatus is ServerState.Connected)
                        _ = _hub.UserUpdateOwnPairPerm(new(isolatedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("InHardcore", false), UpdateDir.Own));
                }
            }
            else
            {
                // do it for all the pairs.
                foreach (var pair in _pairManager.DirectPairs)
                {
                    if (pair.UserPair.OwnPairPerms.InHardcore)
                    {
                        // put us out of hardcore, and disable any active hardcore stuff.
                        pair.OwnPerms.InHardcore = false;
                        // send the updates to the server.
                        if (MainHub.ServerStatus is ServerState.Connected)
                            _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("InHardcore", false), UpdateDir.Own));
                    }
                }
            }

            // reverting character.
            Logger.LogInformation("Character reverted.", LoggerType.Safeword);*/
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while trying to process the safeword command.");
        }

    }

    private void HardcoreSafewordUsed(string isolatedUID)
    {/*
        if (HardcoreSafewordOnCD)
        {
            Logger.LogWarning("Hardcore Safeword was used too soon after the last use Wait 1m before using again.");
            return;
        }
        // set the time of the last hardcore safeword used.
        TimeOfLastHardcoreSafewordUsed = DateTime.Now;
        Logger.LogInformation("Hardcore Safeword was used.", LoggerType.Safeword);

        // do direct updates so they apply first client side, then push to the server. The callback can validate these changes.
        if (_playerManager.GlobalPerms is not null)
        {
            _playerManager.GlobalPerms.ForcedFollow = string.Empty;
            _playerManager.GlobalPerms.ForcedEmoteState = string.Empty;
            _playerManager.GlobalPerms.ForcedStay = string.Empty;
            _playerManager.GlobalPerms.ForcedBlindfold = string.Empty;
            _playerManager.GlobalPerms.ChatBoxesHidden = string.Empty;
            _playerManager.GlobalPerms.ChatInputHidden = string.Empty;
            _playerManager.GlobalPerms.ChatInputBlocked = string.Empty;

            // if we are connected, push update server side.
            if(MainHub.IsServerAlive)
            {
                Logger.LogInformation("Pushing Global updates to the server.", LoggerType.Safeword);
                _ = _hub.UserPushAllGlobalPerms(new(MainHub.PlayerUserData, MainHub.PlayerUserData, _playerManager.GlobalPerms, UpdateDir.Own));
                Logger.LogInformation("Global updates pushed to the server.", LoggerType.Safeword);
            }
        }

        // for each pair in our direct pairs, we should update any and all unique pair permissions to be set regarding Hardcore Status.
        // Only do it for the isolated UID if it is not empty.
        if (!isolatedUID.IsNullOrWhitespace())
        {
            // do it for the single pair we want to.
            var isolatedPair = _pairManager.DirectPairs.FirstOrDefault(user => string.Equals(user.UserData.UID, isolatedUID, StringComparison.OrdinalIgnoreCase));
            if (isolatedPair != null && isolatedPair.UserPair.OwnPairPerms.InHardcore)
            {
                // put us out of hardcore, and disable any active
                isolatedPair.OwnPerms.InHardcore = false;
                isolatedPair.OwnPerms.AllowForcedFollow = false;
                isolatedPair.OwnPerms.AllowForcedSit = false;
                isolatedPair.OwnPerms.AllowForcedEmote = false;
                isolatedPair.OwnPerms.AllowForcedToStay = false;
                isolatedPair.OwnPerms.AllowBlindfold = false;
                isolatedPair.OwnPerms.AllowHidingChatBoxes = false;
                isolatedPair.OwnPerms.AllowHidingChatInput = false;
                isolatedPair.OwnPerms.AllowChatInputBlocking = false;
                // send the updates to the server.
                if (MainHub.ServerStatus is ServerState.Connected)
                    _ = _hub.UserPushAllUniquePerms(new(isolatedPair.UserData, MainHub.PlayerUserData, isolatedPair.UserPair.OwnPairPerms, isolatedPair.UserPair.OwnEditAccessPerms, UpdateDir.Own));
            }
        }
        else
        {
            foreach (var pair in _pairManager.DirectPairs)
            {
                if (pair.UserPair.OwnPairPerms.InHardcore)
                {
                    // put us out of hardcore, and disable any active hardcore stuff.
                    pair.OwnPerms.InHardcore = false;
                    pair.OwnPerms.AllowForcedFollow = false;
                    pair.OwnPerms.AllowForcedSit = false;
                    pair.OwnPerms.AllowForcedEmote = false;
                    pair.OwnPerms.AllowForcedToStay = false;
                    pair.OwnPerms.AllowBlindfold = false;
                    pair.OwnPerms.AllowHidingChatBoxes = false;
                    pair.OwnPerms.AllowHidingChatInput = false;
                    pair.OwnPerms.AllowChatInputBlocking = false;
                    // send the updates to the server.
                    if (MainHub.ServerStatus is ServerState.Connected)
                        _ = _hub.UserPushAllUniquePerms(new(pair.UserData, MainHub.PlayerUserData, pair.UserPair.OwnPairPerms, pair.UserPair.OwnEditAccessPerms, UpdateDir.Own));
                }
            }
        }*/
    }

    private void CheckCooldown()
    {
        if (SafewordOnCD && TimeOfLastSafewordUsed.AddMinutes(5) < DateTime.Now)
            SafewordOnCD = false;
        if (HardcoreSafewordOnCD && TimeOfLastHardcoreSafewordUsed.AddMinutes(1) < DateTime.Now)
            HardcoreSafewordOnCD = false;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Started Safeword Service.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopped Safeword Service.");
        return Task.CompletedTask;
    }
}
