using CkCommons.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

// The most fundamentally important service in the entire application.
// helps revert any active states applied to the player when used.
public class SafewordService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly MainHub _hub;
    private readonly OwnGlobals _globals;
    private readonly KinksterManager _pairManager;

    // Handle the monitoring of our safeword checks that will occur outside the framework thread.
    // This helps to prevent bloating framework drawtimes.
    private CancellationTokenSource? _safewordCTS;
    private Task? _emergencySafewordTask;

    // The last times each of these safewords were used.
    private static DateTime _lastSafewordTime = DateTime.MinValue;
    private static DateTime _lastHcSafewordTime = DateTime.MinValue;

    public SafewordService(ILogger<SafewordService> logger, GagspeakMediator mediator,
        MainHub hub, OwnGlobals globals, KinksterManager pairManager)
        : base(logger, mediator)
    {
        _hub = hub;
        _globals = globals;
        _pairManager = pairManager;

        // set the chat log up.
        Mediator.Subscribe<SafewordUsedMessage>(pairManager, (msg) => OnSafewordUsed(msg.UID));
        Mediator.Subscribe<SafewordHardcoreUsedMessage>(this, (msg) => OnHcSafewordUsed(msg.UID));
    }

    public static bool SafewordOnCooldown => DateTime.Now < _lastSafewordTime.AddMinutes(5);
    public static bool HcSafewordOnCooldown => DateTime.Now < _lastHcSafewordTime.AddMinutes(1);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _safewordCTS?.Cancel();
        try { _emergencySafewordTask?.Wait(); } catch { }
        _safewordCTS?.Dispose();
        _safewordCTS = null;
        _emergencySafewordTask = null;
    }

    private void OnSafewordUsed(string isolatedUID)
    {
        // Always check for this nomadder what.
        if (KeyMonitor.CtrlPressed() && KeyMonitor.AltPressed() && KeyMonitor.BackPressed())
            Mediator.Publish(new SafewordHardcoreUsedMessage());
        //try
        //{
        //    // return if it has not yet been 5 minutes since the last use.
        //    if (SafewordOnCD)
        //    {
        //        Logger.LogWarning("Hardcore Safeword was used too soon after the last use. Must wait 5 minutes.", LoggerType.Safeword);
        //        return;
        //    }

        //    // set the time of the last safeword used.
        //    TimeOfLastSafewordUsed = DateTime.Now;
        //    Logger.LogInformation("Safeword was used.", LoggerType.Safeword);

        //    // grab active pattern first if any.
        //    if (_clientConfigs.AnyPatternIsPlaying)
        //    {
        //        Logger.LogInformation("Stopping active pattern.", LoggerType.Safeword);
        //        _toyboxManager.DisablePattern(_clientConfigs.ActivePatternGuid());
        //        Logger.LogInformation("Active pattern stopped.", LoggerType.Safeword);
        //    }

        //    // disable all other active things.
        //    await _clientConfigs.DisableEverythingDueToSafeword();
        //    await _appearanceManager.DisableAllDueToSafeword();

        //    // do direct updates so they apply first client side, then push to the server. The callback can validate these changes.
        //    if (_playerManager.GlobalPerms is not null)
        //    {
        //        _playerManager.GlobalPerms.ChatGarblerActive = false;
        //        _playerManager.GlobalPerms.ChatGarblerLocked = false;
        //        _playerManager.GlobalPerms.WardrobeEnabled = false;
        //        _playerManager.GlobalPerms.GagVisuals = false;
        //        _playerManager.GlobalPerms.RestraintSetVisuals = false;
        //        _playerManager.GlobalPerms.PuppeteerEnabled = false;
        //        _playerManager.GlobalPerms.ToyboxEnabled = false;
        //        _playerManager.GlobalPerms.LockToyboxUI = false;
        //        _playerManager.GlobalPerms.ToyIntensity = 0;
        //        _playerManager.GlobalPerms.SpatialVibratorAudio = false;

        //        Logger.LogInformation("Pushing Global updates to the server.", LoggerType.Safeword);
        //        _ = _hub.UserPushAllGlobalPerms(new(MainHub.PlayerUserData, MainHub.PlayerUserData, _playerManager.GlobalPerms, UpdateDir.Own));
        //        Logger.LogInformation("Global updates pushed to the server.", LoggerType.Safeword);
        //    }
        //    Logger.LogInformation("Everything Disabled.", LoggerType.Safeword);

        //    // Revert any pairs we have the "InHardcoreMode" permission set.
        //    // Only do it for the isolated UID if it is not empty.
        //    if (!isolatedUID.IsNullOrWhitespace())
        //    {
        //        // do it for the single pair we want to.
        //        var isolatedPair = _pairManager.DirectPairs.FirstOrDefault(user => string.Equals(user.UserData.UID, isolatedUID, StringComparison.OrdinalIgnoreCase));
        //        if (isolatedPair != null && isolatedPair.UserPair.OwnPerms.InHardcore)
        //        {
        //            // put us out of hardcore, and disable any active
        //            isolatedPair.OwnPerms.InHardcore = false;
        //            // send the updates to the server.
        //            if (MainHub.ServerStatus is ServerState.Connected)
        //                _ = _hub.UserUpdateOwnPairPerm(new(isolatedPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("InHardcore", false), UpdateDir.Own));
        //        }
        //    }
        //    else
        //    {
        //        // do it for all the pairs.
        //        foreach (var pair in _pairManager.DirectPairs)
        //        {
        //            if (pair.UserPair.OwnPerms.InHardcore)
        //            {
        //                // put us out of hardcore, and disable any active hardcore stuff.
        //                pair.OwnPerms.InHardcore = false;
        //                // send the updates to the server.
        //                if (MainHub.ServerStatus is ServerState.Connected)
        //                    _ = _hub.UserUpdateOwnPairPerm(new(pair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("InHardcore", false), UpdateDir.Own));
        //            }
        //        }
        //    }

        //    // reverting character.
        //    Logger.LogInformation("Character reverted.", LoggerType.Safeword);
        //}
        //catch (Exception ex)
        //{
        //    Logger.LogError(ex, "An error occurred while trying to process the safeword command.");
        //}
    }

    private void OnHcSafewordUsed(string isolatedUID)
    {
        //if (HardcoreSafewordOnCD)
        //{
        //    Logger.LogWarning("Hardcore Safeword was used too soon after the last use Wait 1m before using again.");
        //    return;
        //}
        //// set the time of the last hardcore safeword used.
        //TimeOfLastHardcoreSafewordUsed = DateTime.Now;
        //Logger.LogInformation("Hardcore Safeword was used.", LoggerType.Safeword);

        //// do direct updates so they apply first client side, then push to the server. The callback can validate these changes.
        //if (_playerManager.GlobalPerms is not null)
        //{
        //    _playerManager.GlobalPerms.ForcedFollow = string.Empty;
        //    _playerManager.GlobalPerms.ForcedEmoteState = string.Empty;
        //    _playerManager.GlobalPerms.ForcedStay = string.Empty;
        //    _playerManager.GlobalPerms.ForcedBlindfold = string.Empty;
        //    _playerManager.GlobalPerms.ChatBoxesHidden = string.Empty;
        //    _playerManager.GlobalPerms.ChatInputHidden = string.Empty;
        //    _playerManager.GlobalPerms.ChatInputBlocked = string.Empty;

        //    // if we are connected, push update server side.
        //    if(MainHub.IsServerAlive)
        //    {
        //        Logger.LogInformation("Pushing Global updates to the server.", LoggerType.Safeword);
        //        _ = _hub.UserPushAllGlobalPerms(new(MainHub.PlayerUserData, MainHub.PlayerUserData, _playerManager.GlobalPerms, UpdateDir.Own));
        //        Logger.LogInformation("Global updates pushed to the server.", LoggerType.Safeword);
        //    }
        //}

        //// for each pair in our direct pairs, we should update any and all unique pair permissions to be set regarding Hardcore Status.
        //// Only do it for the isolated UID if it is not empty.
        //if (!isolatedUID.IsNullOrWhitespace())
        //{
        //    // do it for the single pair we want to.
        //    var isolatedPair = _pairManager.DirectPairs.FirstOrDefault(user => string.Equals(user.UserData.UID, isolatedUID, StringComparison.OrdinalIgnoreCase));
        //    if (isolatedPair != null && isolatedPair.UserPair.OwnPerms.InHardcore)
        //    {
        //        // put us out of hardcore, and disable any active
        //        isolatedPair.OwnPerms.InHardcore = false;
        //        isolatedPair.OwnPerms.AllowForcedFollow = false;
        //        isolatedPair.OwnPerms.AllowForcedSit = false;
        //        isolatedPair.OwnPerms.AllowForcedEmote = false;
        //        isolatedPair.OwnPerms.AllowForcedStay = false;
        //        isolatedPair.OwnPerms.AllowBlindfold = false;
        //        isolatedPair.OwnPerms.AllowHidingChatBoxes = false;
        //        isolatedPair.OwnPerms.AllowHidingChatInput = false;
        //        isolatedPair.OwnPerms.AllowChatInputBlocking = false;
        //        // send the updates to the server.
        //        if (MainHub.ServerStatus is ServerState.Connected)
        //            _ = _hub.UserPushAllUniquePerms(new(isolatedPair.UserData, MainHub.PlayerUserData, isolatedPair.UserPair.OwnPerms, isolatedPair.UserPair.OwnEditAccessPerms, UpdateDir.Own));
        //    }
        //}
        //else
        //{
        //    foreach (var pair in _pairManager.DirectPairs)
        //    {
        //        if (pair.UserPair.OwnPerms.InHardcore)
        //        {
        //            // put us out of hardcore, and disable any active hardcore stuff.
        //            pair.OwnPerms.InHardcore = false;
        //            pair.OwnPerms.AllowForcedFollow = false;
        //            pair.OwnPerms.AllowForcedSit = false;
        //            pair.OwnPerms.AllowForcedEmote = false;
        //            pair.OwnPerms.AllowForcedStay = false;
        //            pair.OwnPerms.AllowBlindfold = false;
        //            pair.OwnPerms.AllowHidingChatBoxes = false;
        //            pair.OwnPerms.AllowHidingChatInput = false;
        //            pair.OwnPerms.AllowChatInputBlocking = false;
        //            // send the updates to the server.
        //            if (MainHub.ServerStatus is ServerState.Connected)
        //                _ = _hub.UserPushAllUniquePerms(new(pair.UserData, MainHub.PlayerUserData, pair.UserPair.OwnPerms, pair.UserPair.OwnEditAccessPerms, UpdateDir.Own));
        //        }
        //    }
        //}
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting Safeword Monitor.");
        // Start the safeword monitor when the service starts.
        _safewordCTS = new();
        _emergencySafewordTask = Task.Run(async () =>
        {
            try
            {
                while (!_safewordCTS.IsCancellationRequested)
                {
                    if (KeyMonitor.CtrlPressed() && KeyMonitor.AltPressed() && KeyMonitor.BackPressed())
                        Mediator.Publish(new SafewordHardcoreUsedMessage());

                    // Adjust delay time if there is issues with recognition.
                    await Task.Delay(100, _safewordCTS.Token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Safeword monitor encountered an error.");
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping Safeword Monitor.");
        return Task.CompletedTask;
    }
}
