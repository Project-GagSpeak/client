using CkCommons;
using CkCommons.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

// The most fundamentally important service in the entire application.
// helps revert any active states applied to the player when used.
// This class should not be connected to anything that links back to the MainHub.
public class SafewordService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly MainHub _hub;
    private readonly OwnGlobals _globals;
    private readonly OwnGlobalsListener _globalsListener;
    private readonly HardcoreHandler _hcHandler;
    private readonly CacheStateManager _visualCacheManager;
    private readonly KinksterManager _kinksters;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    private readonly BuzzToyManager _toys;
    private readonly AchievementEventHandler _achievementHandler;

    private CancellationTokenSource _ctrlAltBackspaceSafewordTaskCTS = new();
    private Task? _ctrlAltBackspaceSafewordTask = null;

    // The last times each of these safewords were used.
    private DateTime _lastSafewordTime = DateTime.MinValue;
    private DateTime _lastHcSafewordTime = DateTime.MinValue;

    public SafewordService(ILogger<SafewordService> logger, GagspeakMediator mediator,
        MainHub hub, OwnGlobals globals, OwnGlobalsListener globalsListener,
        HardcoreHandler hcHandler, CacheStateManager visualManager, KinksterManager kinksters, 
        PatternManager patterns, AlarmManager alarms, TriggerManager triggers, 
        BuzzToyManager toys, AchievementEventHandler achievements)
        : base(logger, mediator)
    {
        _hub = hub;
        _globals = globals;
        _globalsListener = globalsListener;
        _hcHandler = hcHandler;
        _visualCacheManager = visualManager;
        _kinksters = kinksters;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
        _toys = toys;
        _achievementHandler = achievements;
    }

    public bool SafewordOnCooldown => DateTime.Now < _lastSafewordTime.AddMinutes(5);
    public bool HcSafewordOnCooldown => DateTime.Now < _lastHcSafewordTime.AddMinutes(1);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ctrlAltBackspaceSafewordTaskCTS.SafeCancel();
        Generic.Safe(() => _ctrlAltBackspaceSafewordTask?.Wait());
        _ctrlAltBackspaceSafewordTaskCTS?.SafeDispose();
    }

    public async Task OnSafewordInvoked(string isolatedUID = "")
    {
        // if we used this while the safeword was on cooldown, return with a warning.
        if (SafewordOnCooldown)
        {
            Logger.LogWarning("Safeword was used too soon after the last use. Must wait 5 minutes.");
            return;
        }

        // update the last time triggered.
        _lastSafewordTime = DateTime.Now;
        Logger.LogInformation("[SAFEWORD PROGRESS]: Safeword was used.");


        _achievementHandler.SafewordUsed(isolatedUID);

        // MAINTAINERS NOTE:
        // We will be pushing a CharacterCompositeData with NULL storage to serve
        // as a bulk update for all of our active states being changed due to the
        // safeword.

        // Therefore, it is important that we ONLY call the CLIENT SIDE UPDATES.
        // (aka the calls invoked from the server callbacks) to update our states!

        // We don't need to include anything directly impacted by the safeword, as
        // that information will not be processed by those that recieve your data.
        var newActiveData = new CharaCompositeActiveData()
        {
            GlobalAliasData = null!,
            PairAliasData = null!,
            LightStorageData = null!,
            ActiveAlarms = _alarms.ActiveAlarms.Select(x => x.Identifier).ToList(),
            ActiveTriggers = _triggers.Storage.Select(x => x.Identifier).ToList(),
        };

        // Forcibly stop any active patterns.
        if (_patterns.ActivePatternId != Guid.Empty)
        {
            _patterns.DisablePattern(_patterns.ActivePatternId, MainHub.UID, true);
            Logger.LogInformation("[SAFEWORD PROGRESS]: Active pattern stopped due to safeword.");
        }

        // Disable all active toys (WIP)
        _toys.SafewordUsed(); // might need to do something to prevent this from calling a toyschanged update or whatever.

        // Now we need to disable all global permissions in relation to our safeword that help prevent further abuse by others.
        var previousGlobals = OwnGlobals.Perms;
        var newGlobals = OwnGlobals.CurrentPermsWith((current) =>
        {
            current.ChatGarblerActive = false;
            current.ChatGarblerLocked = false;
            current.GaggedNameplate = false;

            // prevent any generic wardrobe calls.
            current.WardrobeEnabled = false;
            // prevent gag visuals.
            current.GagVisuals = false;
            // prevent restriction visuals.
            current.RestrictionVisuals = false;
            // prevent restraint visuals.
            current.RestraintSetVisuals = false;
            // prevent puppeteer from all sources.
            current.PuppeteerEnabled = false;
            // prevent toybox from all sources.
            current.ToyboxEnabled = false;

            // Clear out any active hypnosis effects.
            current.HypnosisCustomEffect = string.Empty;

            // clear any hardcore states in their entirety.
            current.LockedFollowing = string.Empty;
            current.LockedEmoteState = string.Empty;
            current.IndoorConfinement = string.Empty;
            current.Imprisonment = string.Empty;
            current.ChatBoxesHidden = string.Empty;
            current.ChatInputHidden = string.Empty;
            current.ChatInputBlocked = string.Empty;
        });

        // will need to properly handle any achievements here.

        // await the push for updating the global permissions in bulk.
        Logger.LogInformation("[SAFEWORD PROGRESS]: Pushing Global updates to the server.");
        var res = await _hub.UserBulkChangeGlobal(new(MainHub.PlayerUserData, newGlobals));
        if (res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"[SAFEWORD PROGRESS]: Failed to push safeword global permissions: {res.ErrorCode}.");

        // IF and only if the uid passed in is not empty, do we restrict the kinksters we revert to only the kinkster with that UID.
        if (!string.IsNullOrEmpty(isolatedUID))
        {
            Logger.LogInformation($"[SAFEWORD PROGRESS]: Safeword Invoked spesifically for: {isolatedUID}. Reverting hardcore!");
            if (_kinksters.TryGetKinkster(new(isolatedUID), out var kinkster))
                await _hub.UserChangeOwnPairPerm(new(kinkster.UserData, new KeyValuePair<string, object>(nameof(PairPerms.InHardcore) , false), UpdateDir.Own, MainHub.PlayerUserData));
            else
                Logger.LogWarning($"[SAFEWORD PROGRESS]: Kinkster with UID {isolatedUID} not found for safeword revert.");
        }
        else
        {
            // Process it for everyone!
            foreach (var pair in _kinksters.DirectPairs)
            {
                // if the pair is in hardcore, revert it.
                if (pair.UserPair.OwnPerms.InHardcore)
                {
                    Logger.LogInformation($"[SAFEWORD PROGRESS]: Reverting hardcore for Kinkster {pair.UserData.UID}.");
                    // might want to not await this, idk.
                    await _hub.UserChangeOwnPairPerm(new(pair.UserData, new KeyValuePair<string, object>(nameof(PairPerms.InHardcore), false), UpdateDir.Own, MainHub.PlayerUserData));
                }
            }
        }

        // now update our composite data for safeword.
        var activeDataUpdate = await _hub.UserPushActiveData(new(_kinksters.GetOnlineUserDatas(), newActiveData, true));
        if (activeDataUpdate.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"[SAFEWORD PROGRESS]: Failed to push safeword active data: {activeDataUpdate.ErrorCode}.");
            return; // return early here so that we dont fuck up our achievements and all.
        }
        else
        {
            Logger.LogInformation("[SAFEWORD PROGRESS]: Safeword active data pushed successfully.");
        }

        // assuming operation was successful, we now need to sync our overlays with our metadata.
        Logger.LogInformation("[SAFEWORD PROGRESS]: Syncing Global Permissions!");
        _hcHandler.ProcessBulkGlobalUpdate(previousGlobals, OwnGlobals.Perms!);
        // this should automatically sync overlays and other things.

        // might want to remove all cursed loot and things before syncing up the visual cache manager?
        // TODO::::

        // Now sync up the visuals!
        Logger.LogInformation("[SAFEWORD PROGRESS]: Syncing Visual Cache With Display");
        await _visualCacheManager.ResetCachesDueToSafeword(); // need to make sure we handle achievement updates properly!

        Logger.LogInformation("[SAFEWORD PROGRESS]: ===={ COMPLETED }====");
    }

    public async Task OnHcSafewordUsed(string isolatedUID = "")
    {
        // if we used this while the safeword was on cooldown, return with a warning.
        if (HcSafewordOnCooldown)
        {
            Logger.LogWarning("Hardcore Safeword used too soon after the last use. Must wait 1m between uses!");
            return;
        }

        // update the last time triggered.
        _lastHcSafewordTime = DateTime.Now;
        Logger.LogInformation("[HC-SAFEWORD PROGRESS]: Hardcore Safeword used!");

        _achievementHandler.SafewordUsed(isolatedUID);

        var previousGlobals = OwnGlobals.Perms;
        var newGlobals = OwnGlobals.CurrentPermsWith((current) =>
        {
            current.LockedFollowing = string.Empty;
            current.LockedEmoteState = string.Empty;
            current.IndoorConfinement = string.Empty;
            current.Imprisonment = string.Empty;
            current.ChatBoxesHidden = string.Empty;
            current.ChatInputHidden = string.Empty;
            current.ChatInputBlocked = string.Empty;
        });

        Logger.LogInformation("[HC-SAFEWORD PROGRESS]: Pushing Global updates to the server.");
        var res = await _hub.UserBulkChangeGlobal(new(MainHub.PlayerUserData, newGlobals));
        if (res.ErrorCode is GagSpeakApiEc.Success)
            Logger.LogInformation("[HARDCORE SAFEWORD PROGRESS]: Global updates pushed to the server.");
        else
            Logger.LogError($"[HC-SAFEWORD PROGRESS]: Failed to push safeword global permissions: {res.ErrorCode}.");

        // the kinksters we reset hardcore for depend on the isolatedUID.
        var kinkstersToReset = (!string.IsNullOrEmpty(isolatedUID) && _kinksters.TryGetKinkster(new(isolatedUID), out var match))
            ? [match]
            : _kinksters.DirectPairs.Where(p => p.OwnPerms.InHardcore);

        foreach (var kinkster in kinkstersToReset)
        {
            Logger.LogInformation($"[HC-SAFEWORD PROGRESS]: Reverting hardcore allowances for ({isolatedUID})!");
            var newPairPerms = kinkster.OwnPerms with
            {
                InHardcore = false,
                AllowGarbleChannelEditing = false,
                AllowLockedFollowing = false,
                AllowLockedSitting = false,
                AllowLockedEmoting = false,
                AllowIndoorConfinement = false,
                AllowImprisonment = false,
                AllowHidingChatBoxes = false,
                AllowHidingChatInput = false,
                AllowChatInputBlocking = false,
                AllowHypnoImageSending = false
            };
            await _hub.UserBulkChangeUnique(new(kinkster.UserData, newPairPerms, kinkster.OwnPermAccess, UpdateDir.Own, MainHub.PlayerUserData));
            Logger.LogInformation($"[HC-SAFEWORD PROGRESS]: Hardcore allowances reverted for ({isolatedUID})!");
        }

        // update the internals for the hardcore permissions to syncronize. (helpful for caches and other hardcore interactions)
        Logger.LogInformation("[HC-SAFEWORD PROGRESS]: Syncing Global Permissions!");
        _hcHandler.ProcessBulkGlobalUpdate(previousGlobals, OwnGlobals.Perms!);

        Logger.LogInformation("[HC-SAFEWORD PROGRESS]: ===={ COMPLETED }====");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting Safeword Monitor.");
        // Start the safeword monitor when the service starts.
        _ctrlAltBackspaceSafewordTaskCTS.SafeCancelRecreate();
        _ctrlAltBackspaceSafewordTask = Task.Run(async () =>
        {
            try
            {
                while (!_ctrlAltBackspaceSafewordTaskCTS.IsCancellationRequested)
                {
                    if (KeyMonitor.CtrlPressed() && KeyMonitor.AltPressed() && KeyMonitor.BackPressed())
                        UiService.SetUITask(async () => await OnHcSafewordUsed());

                    // Adjust delay time if there is issues with recognition.
                    await Task.Delay(100, _ctrlAltBackspaceSafewordTaskCTS.Token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Bagagwa ex)
            {
                Logger.LogError($"Summoned Bagagwa during the Hardcore Safeword Keybind detection loop: {ex}");
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
