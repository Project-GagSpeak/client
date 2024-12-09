using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Controllers;
using GagSpeak.UI.Components;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Extensions;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;

namespace GagSpeak.StateManagers;

/// <summary>
/// AppearanceHandler is responsible for handling any changes to the client player's appearance.
/// These changes can be made by self or other players.
/// <para>
/// Appearance Handler's Primary responsibility is to ensure that the data in the Appearance Service 
/// class remains synchronized with the most recent information.
/// </para>
/// </summary>
public sealed class AppearanceManager : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientData _playerData;
    private readonly GagManager _gagManager;
    private readonly PairManager _pairManager;
    private readonly IpcManager _ipcManager;
    private readonly AppearanceService _appearanceService;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;

    public AppearanceManager(ILogger<AppearanceManager> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientData playerData,
        GagManager gagManager, PairManager pairManager, IpcManager ipcManager, 
        AppearanceService appearanceService, ClientMonitorService clientService,
        OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _gagManager = gagManager;
        _pairManager = pairManager;
        _ipcManager = ipcManager;
        _appearanceService = appearanceService;
        _clientService = clientService;
        _frameworkUtils = frameworkUtils;

        Mediator.Subscribe<ClientPlayerInCutscene>(this, (msg) => _ = _appearanceService.RefreshAppearance(GlamourUpdateType.ReapplyAll));
        Mediator.Subscribe<CutsceneEndMessage>(this, (msg) => _ = _appearanceService.RefreshAppearance(GlamourUpdateType.ReapplyAll));
        Mediator.Subscribe<AppearanceImpactingSettingChanged>(this, (msg) => _ = RecalcAndReload(true));

        Mediator.Subscribe<CharacterIpcDataCreatedMessage>(this, (msg) => LastIpcData = msg.CharaIPCData);


        IpcFastUpdates.StatusManagerChangedEventFired += (addr) => MoodlesUpdated(addr).ConfigureAwait(false);
    }

    private CharaIPCData LastIpcData = null!;

    private List<RestraintSet> RestraintSets => _clientConfigs.WardrobeConfig.WardrobeStorage.RestraintSets;
    private List<CursedItem> CursedItems => _clientConfigs.CursedLootConfig.CursedLootStorage.CursedItems;

    /// <summary> 
    /// Finalized Glamourer Appearance that should be visible on the player. 
    /// </summary>
    private Dictionary<EquipSlot, IGlamourItem> ItemsToApply => _appearanceService.ItemsToApply;
    private IpcCallerGlamourer.MetaData MetaToApply => _appearanceService.MetaToApply;
    private HashSet<Guid> ExpectedMoodles => _appearanceService.ExpectedMoodles;
    private (JToken? Customize, JToken? Parameters) ExpectedCustomizations => _appearanceService.ExpectedCustomizations;

    /// <summary>
    /// The Latest Client Moodles Status List since the last update.
    /// This usually updates whenever the IPC updates, however if we need an immidate fast refresh, 
    /// the fast updater here updates it directly.
    /// </summary>
    public static List<MoodlesStatusInfo> LatestClientMoodleStatusList = new();

    /// <summary> Static accessor to know if we're processing a redraw from a mod toggle </summary>
    public static bool ManualRedrawProcessing = false;

    private CancellationTokenSource RedrawTokenSource = new();
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        IpcFastUpdates.StatusManagerChangedEventFired -= (addr) => MoodlesUpdated(addr).ConfigureAwait(false);
        RedrawTokenSource?.Cancel();
        RedrawTokenSource?.Dispose();
        _applierSlimCTS?.Cancel();
        _applierSlimCTS?.Dispose();
    }

    public static bool IsApplierProcessing => _applierSlim.CurrentCount == 0;
    private CancellationTokenSource _applierSlimCTS = new CancellationTokenSource();
    private static SemaphoreSlim _applierSlim = new SemaphoreSlim(1, 1);

    private async Task ExecuteWithApplierSlim(Func<Task> action)
    {
        _applierSlimCTS.Cancel();
        await _applierSlim.WaitAsync();
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during semaphore execution: {ex}");
        }
        finally
        {
            _applierSlim.Release();
        }
    }

    private async Task UpdateLatestMoodleData()
    {
        LatestClientMoodleStatusList = await _ipcManager.Moodles.GetStatusInfoAsync();
    }

    public async Task RecalcAndReload(bool refreshing, HashSet<Guid>? removeMoodles = null)
    {
        // perform a recalculation to appearance data. 
        var updateType = refreshing ? GlamourUpdateType.RefreshAll : GlamourUpdateType.ReapplyAll;

        await RecalculateAppearance();
        await WaitForRedrawCompletion();
        await _appearanceService.RefreshAppearance(updateType, removeMoodles);
    }


    /// <summary>
    /// This logic will occur after a Restraint Set has been enabled via the WardrobeHandler.
    /// </summary>
    public async Task EnableRestraintSet(Guid restraintID, string assignerUID, bool pushToServer = true, bool triggerAchievement = true)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            Logger.LogTrace("ENABLE-SET Executed", LoggerType.AppearanceState);
            if (_clientConfigs.GetActiveSet() is not null)
            {
                Logger.LogError("You must Disable the active Set before calling this!");
                return;
            }

            // Enable the set. For starters we should apply the mods.
            var setIdx = RestraintSets.FindIndex(x => x.RestraintId == restraintID);
            if (setIdx == -1)
            {
                Logger.LogWarning("Attempted to enable a restraint set that does not exist.");
                return;
            }

            var setRef = RestraintSets[setIdx];
            Logger.LogInformation("ENABLE SET [" + RestraintSets[setIdx].Name + "] START", LoggerType.AppearanceState);
            Logger.LogDebug("Assigner was: " + assignerUID, LoggerType.AppearanceState);
            setRef.Enabled = true;
            setRef.EnabledBy = assignerUID;
            _clientConfigs.SaveWardrobe();

            // Raise the priority of, and enable the mods bound to the active set.
            await PenumbraModsToggle(NewState.Enabled, setRef.AssociatedMods);

            // Enable the Hardcore Properties by invoking the ipc call.
            if (setRef.HasPropertiesForUser(setRef.EnabledBy))
            {
                Logger.LogDebug("Set Contains HardcoreProperties for " + setRef.EnabledBy, LoggerType.AppearanceState);
                if (setRef.PropertiesEnabledForUser(setRef.EnabledBy))
                {
                    Logger.LogDebug("Hardcore properties are enabled for this set!", LoggerType.AppearanceState);
                    IpcFastUpdates.InvokeHardcoreTraits(NewState.Enabled, setRef);
                }
            }

            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintApplicationChanged, setRef, true, assignerUID);
            Logger.LogInformation("ENABLE SET [" + setRef.Name + "] END", LoggerType.AppearanceState);

            await RecalcAndReload(false);

            if (pushToServer) Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintApplied, setRef.LockType.ToPadlock()));

            // Finally, we should let our trigger controller know that we just enabled a restraint set.
            //_triggerController.CheckActiveRestraintTriggers(restraintID, NewState.Enabled);
        });
    }

    public async Task DisableRestraintSet(Guid restraintID, string disablerUID, bool pushToServer = true, bool triggerAchievement = true)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            Logger.LogTrace("DISABLE-SET Executed", LoggerType.AppearanceState);
            var setIdx = RestraintSets.FindIndex(x => x.RestraintId == restraintID);
            if (setIdx == -1) { Logger.LogWarning("Set Does not Exist, Skipping."); return; }

            var setRef = RestraintSets[setIdx];
            if (!setRef.Enabled || setRef.Locked) { Logger.LogWarning(setRef.Name + " is already disabled or locked. Skipping!"); return; }

            Logger.LogInformation("DISABLE SET [" + setRef.Name + "] START", LoggerType.AppearanceState);

            // Lower the priority of, and if desired, disable, the mods bound to the active set.
            await PenumbraModsToggle(NewState.Disabled, setRef.AssociatedMods);

            // This simply removes it from the list of expected, not the actual moodles call. This occurs in the service.
            var moodlesToRemove = RemoveMoodles(setRef);

            // Disable the Hardcore Properties by invoking the ipc call.
            if (setRef.HasPropertiesForUser(setRef.EnabledBy))
            {
                Logger.LogDebug("Set Contains HardcoreProperties for " + setRef.EnabledBy, LoggerType.AppearanceState);
                if (setRef.PropertiesEnabledForUser(setRef.EnabledBy))
                {
                    Logger.LogDebug("Hardcore properties are enabled for this set, so disabling them!", LoggerType.AppearanceState);
                    IpcFastUpdates.InvokeHardcoreTraits(NewState.Disabled, setRef);
                }
            }

            // see if we qualify for the achievement Auctioned off, and if so, fire it.
            bool auctionedOffSatisfied = setRef.EnabledBy != MainHub.UID && disablerUID != MainHub.UID;
            if (triggerAchievement && (setRef.EnabledBy != disablerUID) && auctionedOffSatisfied)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.AuctionedOff);

            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintApplicationChanged, setRef, false, disablerUID);
            setRef.Enabled = false;
            setRef.EnabledBy = string.Empty;
            _clientConfigs.SaveWardrobe();
            // Update our active Set monitor.

            Logger.LogInformation("DISABLE SET [" + setRef.Name + "] END", LoggerType.AppearanceState);
            await RecalcAndReload(true, moodlesToRemove);

            if (pushToServer) Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintDisabled, setRef.LockType.ToPadlock()));

            // Finally, we should let our trigger controller know that we just enabled a restraint set.
            //_triggerController.CheckActiveRestraintTriggers(restraintID, NewState.Disabled);
        });
    }

    public async Task LockRestraintSet(Guid id, Padlocks padlock, string pwd, DateTimeOffset endTime, string assigner, bool pushToServer = true, bool triggerAchievement = true)
    {
        await ExecuteWithApplierSlim(() =>
        {
            Logger.LogTrace("LOCKING SET START", LoggerType.AppearanceState);
            var setIdx = RestraintSets.FindIndex(x => x.RestraintId == id);
            if (setIdx == -1)
            {
                Logger.LogWarning("Set Does not Exist, Skipping.");
                return Task.CompletedTask;
            }
            // if the set is not the active set, log that this is invalid, as we should only be locking / unlocking the active set.
            if (setIdx != _clientConfigs.GetActiveSetIdx())
            {
                Logger.LogWarning("Attempted to lock a set that is not the active set. Skipping.");
                return Task.CompletedTask;
            }

            // Grab the set reference.
            var setRef = RestraintSets[setIdx];
            if (setRef.Locked)
            {
                Logger.LogDebug(setRef.Name + " is already locked. Skipping!", LoggerType.AppearanceState);
                return Task.CompletedTask;
            }

            // Assign the lock information to the set.
            var prevLock = setRef.LockType.ToPadlock();
            setRef.LockType = padlock.ToName();
            setRef.LockPassword = pwd;
            setRef.LockedUntil = endTime;
            setRef.LockedBy = assigner;
            _clientConfigs.SaveWardrobe();
            // Set this so that when we go to unlock we can ref for auto removal.
            _gagManager.UpdateRestraintLockSelections(false);


            Logger.LogDebug("Set: " + setRef.Name + " Locked by: " + assigner + " with a Padlock of Type: " + padlock.ToName()
                + " with: " + (endTime - DateTimeOffset.UtcNow) + " by: " + assigner, LoggerType.AppearanceState);

            // After this, we should push our changes to the server, if we have marked for us to.
            if (pushToServer) Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintLocked, prevLock));

            // Finally, we should fire to our achievement manager, if we have marked for us to.
            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, setRef, padlock, true, assigner);

            Logger.LogInformation("LOCKING SET END", LoggerType.AppearanceState);

            // Finally, we should let our trigger controller know that we just enabled a restraint set.
            //_triggerController.CheckActiveRestraintTriggers(id, NewState.Locked);
            return Task.CompletedTask;
        });
    }

    public async Task UnlockRestraintSet(Guid id, string lockRemover, bool pushToServer = true, bool triggerAchievement = true, bool fromTimer = false)
    {
        await ExecuteWithApplierSlim(() =>
        {
            Logger.LogTrace("UNLOCKING SET START", LoggerType.AppearanceState);
            var setIdx = RestraintSets.FindIndex(x => x.RestraintId == id);
            if (setIdx == -1)
            {
                Logger.LogWarning("Set Does not Exist, Skipping.");
                return Task.CompletedTask;
            }
            // if the set is not the active set, log that this is invalid, as we should only be locking / unlocking the active set.
            if (setIdx != _clientConfigs.GetActiveSetIdx())
            {
                Logger.LogWarning("Attempted to unlock a set that is not the active set. Skipping.");
                return Task.CompletedTask;
            }

            // Grab the set reference.
            var setRef = RestraintSets[setIdx];
            if (!setRef.Locked)
            {
                Logger.LogDebug(setRef.Name + " is not even locked. Skipping!", LoggerType.AppearanceState);
                return Task.CompletedTask;
            }

            // Store a copy of the values we need before we change them.
            var previousLock = setRef.LockType;
            var previousAssigner = setRef.LockedBy;

            // Assign the lock information to the set.
            var prevLock = setRef.LockType.ToPadlock();
            setRef.LockType = Padlocks.None.ToName();
            setRef.LockPassword = string.Empty;
            setRef.LockedUntil = DateTimeOffset.MinValue;
            setRef.LockedBy = string.Empty;
            _clientConfigs.SaveWardrobe();

            // reset this ONLY if wasTimer was false, otherwise keep it as is so we have a valid reference after unlock callback.
            if (!fromTimer)
            {
                Logger.LogTrace("Not Setting GagManager.ActiveSlotPadlocks to None, as this was not from a timer.", LoggerType.AppearanceState);
                GagManager.ActiveSlotPadlocks[3] = Padlocks.None;
            }

            Logger.LogDebug("Set: " + setRef.Name + " Unlocked by: " + lockRemover, LoggerType.AppearanceState);

            // After this, we should push our changes to the server, if we have marked for us to.
            if (pushToServer)
                Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintUnlocked, prevLock));

            // If we should fire the sold slave achievement, fire it.
            bool soldSlaveSatisfied = (previousAssigner != MainHub.UID && previousAssigner != MainHub.UID) && (lockRemover != MainHub.UID && lockRemover != MainHub.UID);
            if (triggerAchievement && (previousAssigner != lockRemover) && soldSlaveSatisfied)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.SoldSlave);

            // Finally, we should fire to our achievement manager, if we have marked for us to.
            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, setRef, previousLock.ToPadlock(), false, lockRemover);

            Logger.LogInformation("UNLOCKING SET END", LoggerType.AppearanceState);
            // Finally, we should let our trigger controller know that we just enabled a restraint set.
            //_triggerController.CheckActiveRestraintTriggers(id, NewState.Unlocked);
            return Task.CompletedTask;
        });
    }

    public async Task RestraintSwapped(Guid newSetId, bool isSelfApplied = true, bool publish = true)
    {
        Logger.LogTrace("SET-SWAPPED Executed. Triggering DISABLE-SET, then ENABLE-SET", LoggerType.AppearanceState);

        // We just do this for extra security overhead even though we could just pass it in.
        var activeSet = _clientConfigs.GetActiveSet();
        if (activeSet is null)
        {
            Logger.LogWarning("No Active Set to swap from. Skipping.");
            return;
        }

        // First, disable the current set.
        await DisableRestraintSet(activeSet.RestraintId, disablerUID: MainHub.UID, pushToServer: false);
        // Then, enable the new set.
        await EnableRestraintSet(newSetId, assignerUID: MainHub.UID, pushToServer: publish);
    }

    /// <summary>
    /// When a gag is applied, because its data has already been adjusted, we will simply
    /// need to do a refresh for appearance. The only thing that needs to be adjusted here is C+ Profile.
    /// </summary>
    public async Task GagApplied(GagLayer layer, GagType gagType, bool publishApply = true, bool isSelfApplied = true, bool triggerAchievement = true)
    {
        await ExecuteWithApplierSlim(async () => { await GagApplyInternal(layer, gagType, isSelfApplied, publishApply, triggerAchievement); });
    }

    private async Task GagApplyInternal(GagLayer layer, GagType gagType, bool isSelfApplied = true, bool publishApply = true, bool triggerAchievement = true)
    {
        Logger.LogDebug("GAG-APPLIED triggered on slot [" + layer.ToString() + "] with a [" + gagType.GagName() + "]", LoggerType.AppearanceState);

        _gagManager.ApplyGag(layer, gagType);

        // if publishing, publish it.
        if (publishApply)
        {
            Logger.LogInformation("WE ARE PUBLISHING");
            _gagManager.PublishGagApplied(layer);
        }

        // Send Achievement Event
        if (triggerAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.GagAction, layer, gagType, isSelfApplied);

        // If the Gag is not Enabled, or our auto equip is disabled, dont do anything else and return.
        if (!_clientConfigs.IsGagEnabled(gagType) || !_playerData.GlobalPerms!.ItemAutoEquip)
        {
            Logger.LogDebug("Either this gag was not set to be enabled, or item auto equip is off, skipping appearnace altaring changes!", LoggerType.GagHandling);
            return;
        }

        await RecalcAndReload(false);

        // Update C+ Profile if applicable
        var drawData = _clientConfigs.GetDrawData(gagType);
        if (drawData.CustomizeGuid != Guid.Empty)
            _ipcManager.CustomizePlus.EnableProfile(drawData.CustomizeGuid);
    }

    /// <summary>
    /// When a gag is removed, because its data has already been adjusted, we will simply
    /// need to do a refresh for appearance. The only thing that needs to be adjusted here is C+ Profile.
    /// </summary>
    public async Task GagRemoved(GagLayer layer, GagType gagType, bool publishRemoval = true, bool isSelfApplied = true, bool triggerAchievement = true)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            Logger.LogDebug("GAG-REMOVE triggered on slot [" + layer.ToString() + "] with a [" + gagType.GagName() + "]", LoggerType.AppearanceState);

            // if we aren't even making a change dont push anything.
            if (_playerData.AppearanceData?.GagSlots[(int)layer].GagType.ToGagType() is GagType.None)
                return;

            bool changeOccured = _gagManager.RemoveGag(layer);

            // Once it's been set to inactive, we should also remove our moodles.
            var gagSettings = _clientConfigs.GetDrawData(gagType);
            var moodlesToRemove = RemoveMoodles(gagSettings);

            await RecalcAndReload(true, moodlesToRemove);

            // Remove the CustomizePlus Profile if applicable
            if (gagSettings.CustomizeGuid != Guid.Empty)
                _ipcManager.CustomizePlus.DisableProfile(gagSettings.CustomizeGuid);

            if (publishRemoval && changeOccured)
                _gagManager.PublishGagRemoved(layer);

            // Send Achievement Event
            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.GagRemoval, layer, gagType, isSelfApplied);
        });
    }

    public async Task GagSwapped(GagLayer layer, GagType curGag, GagType newGag, bool isSelfApplied = true, bool publish = true)
    {
        Logger.LogTrace("GAG-SWAPPED Executed. Triggering GAG-REMOVE, then GAG-APPLIED", LoggerType.AppearanceState);

        // First, remove the current gag.
        await GagRemoved(layer, curGag, publishRemoval: false, isSelfApplied: isSelfApplied);

        // Then, apply the new gag.
        await GagApplied(layer, newGag, publishApply: publish, isSelfApplied: isSelfApplied);
    }

    /// <summary>
    /// For applying cursed items.
    /// </summary>
    /// <param name="gagLayer"> Ignore this if the cursed item's IsGag is false. </param>
    public async Task CursedItemApplied(CursedItem cursedItem, GagLayer gagLayer = GagLayer.UnderLayer, bool publish = true)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            Logger.LogTrace("CURSED-APPLIED Executed");
            // If the cursed item is a gag item, handle it via the gag manager, otherwise, handle through mod toggle
            if (cursedItem.IsGag)
            {
                // Cursed Item was Gag, so handle it via GagApplied.
                await GagApplyInternal(gagLayer, cursedItem.GagType);
            }
            else
            {
                // Cursed Item was Equip, so handle attached Mod Enable and recalculation here.
                await PenumbraModsToggle(NewState.Enabled, new List<AssociatedMod>() { cursedItem.AssociatedMod });

                await RecalcAndReload(false);
            }
            if (publish)
                Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.CursedItemApplied, Padlocks.None));
        });
    }

    public async Task CursedItemRemoved(CursedItem cursedItem, bool publish = true)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            Logger.LogTrace("CURSED-REMOVED Executed", LoggerType.AppearanceState);
            // If the Cursed Item is a GagItem, it will be handled automatically by lock expiration. 
            // However, it also means none of the below will process, so we should return if it is.
            if (!cursedItem.IsGag)
            {
                // We are removing a Equip-based CursedItem
                await PenumbraModsToggle(NewState.Disabled, new List<AssociatedMod>() { cursedItem.AssociatedMod });

                // The attached Moodle will need to be removed as well. (need to handle seperately since it stores moodles differently)
                var moodlesToRemove = new HashSet<Guid>();
                if (!_playerData.IpcDataNull && cursedItem.MoodleIdentifier != Guid.Empty)
                {
                    if (cursedItem.MoodleType is IpcToggleType.MoodlesStatus)
                        moodlesToRemove.UnionWith(new HashSet<Guid>() { cursedItem.MoodleIdentifier });
                    else if (cursedItem.MoodleType is IpcToggleType.MoodlesPreset)
                    {
                        var statuses = _playerData.LastIpcData!.MoodlesPresets
                            .FirstOrDefault(p => p.GUID == cursedItem.MoodleIdentifier).Statuses;
                        moodlesToRemove.UnionWith(statuses);
                    }
                }
                await RecalcAndReload(true, moodlesToRemove);
            }

            if (publish)
                Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.CursedItemRemoved, Padlocks.MimicPadlock));
        });
    }

    private HashSet<Guid> RemoveMoodles(IMoodlesAssociable data)
    {
        Logger.LogTrace("Removing Moodles", LoggerType.AppearanceState);
        if (_playerData.IpcDataNull)
            return new HashSet<Guid>();

        // if our preset is not null, store the list of guids respective of them.
        var statuses = new HashSet<Guid>();
        if (data.AssociatedMoodlePreset != Guid.Empty)
        {
            statuses = _playerData.LastIpcData!.MoodlesPresets
                .FirstOrDefault(p => p.GUID == data.AssociatedMoodlePreset).Statuses.ToHashSet();
        }
        // concat this list with the associated moodles.
        statuses.UnionWith(data.AssociatedMoodles);

        // log the moodles we are removing.
        Logger.LogTrace("Removing Moodles from Expected: " + string.Join(", ", statuses), LoggerType.AppearanceState);

        // remove the moodles.
        ExpectedMoodles.ExceptWith(statuses);
        // return the list of moodles we removed.
        return statuses;
    }

    public async Task DisableAllDueToSafeword()
    {
        // disable all gags,
        if (_playerData.AppearanceData is not null)
        {
            Logger.LogInformation("Disabling all active Gags due to Safeword.", LoggerType.Safeword);
            for (var i = 0; i < 3; i++)
            {
                var gagSlot = _playerData.AppearanceData.GagSlots[i];
                // check to see if the gag is currently active.
                if (gagSlot.GagType.ToGagType() is not GagType.None)
                {
                    _gagManager.UnlockGag((GagLayer)i); // (doesn't fire any achievements so should be fine)
                    // then we should remove it, but not publish it to the mediator just yet.
                    await GagRemoved((GagLayer)i, gagSlot.GagType.ToGagType(), publishRemoval: false, isSelfApplied: gagSlot.Assigner == MainHub.UID);
                }
            }
            Logger.LogInformation("Active gags disabled.", LoggerType.Safeword);
            // finally, push the gag change for the safeword.
            Mediator.Publish(new PlayerCharAppearanceChanged(new CharaAppearanceData(), GagLayer.UnderLayer, GagUpdateType.Safeword, Padlocks.None));
        }

        // if an active set exists we need to unlock and disable it.
        if (_clientConfigs.GetActiveSet() is not null)
        {
            var activeIdx = _clientConfigs.GetActiveSetIdx();
            var set = RestraintSets[activeIdx];
            Logger.LogInformation("Unlocking and Disabling Active Set [" + set.Name + "] due to Safeword.", LoggerType.Safeword);

            // unlock the set, dont push changes yet.
            await UnlockRestraintSet(set.RestraintId, set.LockedBy);

            // Disable the set, turning off any mods moodles ext and refreshing appearance.            
            await DisableRestraintSet(set.RestraintId, MainHub.UID);
        }

        // Disable all Cursed Items.
        var activeCursedItems = CursedItems.Where(x => x.AppliedTime != DateTimeOffset.MinValue).ToList();
        Logger.LogInformation("Disabling all active Cursed Items due to Safeword.", LoggerType.Safeword);
        foreach (var cursedItem in activeCursedItems)
        {
            _clientConfigs.DeactivateCursedItem(cursedItem.LootId);
            await CursedItemRemoved(cursedItem);
        }
    }

    /// <summary> Applies associated mods to the client when a Restraint or Cursed Item is toggled. </summary>
    private Task PenumbraModsToggle(NewState state, List<AssociatedMod> associatedMods)
    {
        try
        {
            // if we are trying to enabling the Restraint/Cursed Item, then we should just enable.
            if (state is NewState.Enabled)
            {
                foreach (var associatedMod in associatedMods)
                    _ipcManager.Penumbra.ModifyModState(associatedMod);

                // if any of those mods wanted us to perform a redraw, then do so now. (PlayerObjectIndex is always 0)
                if (associatedMods.Any(x => x.RedrawAfterToggle))
                    _ipcManager.Penumbra.RedrawObject(0, RedrawType.Redraw);
            }

            // If we are trying to disable the Restraint/Cursed Item, we should disable only if we ask it to.
            if (state is NewState.Disabled)
            {
                // For each of the associated mods, if we marked it to disable when inactive, disable it.
                foreach (var associatedMod in associatedMods)
                    _ipcManager.Penumbra.ModifyModState(associatedMod,
                        modState: NewState.Disabled,
                        adjustPriorityOnly: associatedMod.DisableWhenInactive ? false : true);

                // if any of those mods wanted us to perform a redraw, then do so now. (PlayerObjectIndex is always 0)
                if (associatedMods.Any(x => x.RedrawAfterToggle))
                    _ipcManager.Penumbra.RedrawObject(0, RedrawType.Redraw);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Error while toggling mods: " + e.Message);
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Syncronizes the data to be updated with most recent information.
    /// </summary>
    /// <param name="fetchMoodlesManually"> If true, will fetch moodles manually via IPC call for true latest data. </param>
    public async Task RecalculateAppearance()
    {
        // Return if the core data is null.
        if (_playerData.CoreDataNull)
        {
            Logger.LogWarning("Core Data is Null, Skipping Recalculation.");
            return;
        }

        Logger.LogInformation("Recalculating Appearance Data.", LoggerType.ClientPlayerData);
        // Temp Storage for Data Collection during reapply
        Dictionary<EquipSlot, IGlamourItem> ItemsToApply = new Dictionary<EquipSlot, IGlamourItem>();
        IpcCallerGlamourer.MetaData MetaToApply = IpcCallerGlamourer.MetaData.None;
        HashSet<Guid> ExpectedMoodles = new HashSet<Guid>();
        (JToken? Customize, JToken? Parameters) ExpectedCustomizations = (null, null);

        // store the data to apply from the active set.
        Logger.LogInformation("Wardrobe is Enabled, Collecting Data from Active Set.", LoggerType.AppearanceState);
        // we need to store a reference to the active sets draw data.
        var activeSetRef = _clientConfigs.GetActiveSet();
        if (activeSetRef is not null)
        {
            foreach (var item in activeSetRef.DrawData)
            {
                if (!item.Value.IsEnabled && item.Value.GameItem.Equals(ItemIdVars.NothingItem(item.Value.Slot)))
                    continue;

                Logger.LogTrace("Adding item to apply: " + item.Key, LoggerType.AppearanceState);
                ItemsToApply[item.Key] = item.Value;
            }
            // Add the moodles from the active set.
            if (!_playerData.IpcDataNull)
            {
                if (activeSetRef.AssociatedMoodles.Count > 0)
                    ExpectedMoodles.UnionWith(activeSetRef.AssociatedMoodles);
                if (activeSetRef.AssociatedMoodlePreset != Guid.Empty)
                {
                    var statuses = _playerData.LastIpcData!.MoodlesPresets.FirstOrDefault(p => p.Item1 == activeSetRef.AssociatedMoodlePreset).Item2;
                    if (statuses is not null)
                        ExpectedMoodles.UnionWith(statuses);
                }
            }

            // add the meta data
            MetaToApply = (activeSetRef.ForceHeadgear && activeSetRef.ForceVisor)
                ? IpcCallerGlamourer.MetaData.Both : (activeSetRef.ForceHeadgear)
                    ? IpcCallerGlamourer.MetaData.Hat : (activeSetRef.ForceVisor)
                        ? IpcCallerGlamourer.MetaData.Visor : IpcCallerGlamourer.MetaData.None;
            // add the customizations if we desire it.
            if (activeSetRef.ApplyCustomizations)
                ExpectedCustomizations = (activeSetRef.CustomizeObject, activeSetRef.ParametersObject);
        }

        // Collect gag info if used.
        Logger.LogInformation("Collecting Data from Active Gags.", LoggerType.AppearanceState);
        // grab the active gags, should grab in order (underlayer, middle, uppermost)
        var gagSlots = _playerData.AppearanceData!.GagSlots.Where(slot => slot.GagType.ToGagType() != GagType.None).ToList();

        // update the stored data.
        foreach (var slot in gagSlots)
        {
            var data = _clientConfigs.GetDrawData(slot.GagType.ToGagType());
            if (data is not null && data.IsEnabled)
            {
                ItemsToApply[data.Slot] = data;

                // continue if moodles data is not present.
                if (!_playerData.IpcDataNull)
                {
                    if (data.AssociatedMoodles.Count > 0)
                        ExpectedMoodles.UnionWith(data.AssociatedMoodles);

                    if (data.AssociatedMoodlePreset != Guid.Empty)
                    {
                        var statuses = _playerData.LastIpcData!.MoodlesPresets.FirstOrDefault(p => p.Item1 == data.AssociatedMoodlePreset).Item2;
                        if (statuses is not null)
                            ExpectedMoodles.UnionWith(statuses);
                    }
                }

                // Apply the metadata stored in this gag item. Any gags after it will overwrite previous meta set.
                MetaToApply = (data.ForceHeadgear && data.ForceVisor)
                    ? IpcCallerGlamourer.MetaData.Both : (data.ForceHeadgear)
                        ? IpcCallerGlamourer.MetaData.Hat : (data.ForceVisor)
                            ? IpcCallerGlamourer.MetaData.Visor : IpcCallerGlamourer.MetaData.None;
            }
        }

        // Collect the data from the blindfold.
        if (_playerData.GlobalPerms.IsBlindfolded())
        {
            Logger.LogDebug("We are Blindfolded!", LoggerType.AppearanceState);
            var blindfoldData = _clientConfigs.GetBlindfoldItem();
            ItemsToApply[blindfoldData.Slot] = blindfoldData;
        }

        // collect the data from the cursed sets.
        Logger.LogInformation("Collecting Data from Cursed Items.", LoggerType.AppearanceState);
        // track the items that will be applied.
        var cursedItems = _clientConfigs.CursedLootConfig.CursedLootStorage.CursedItems
            .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
            .OrderBy(x => x.AppliedTime)
            .ToList();
        Logger.LogDebug("Found " + cursedItems.Count + " Cursed Items to Apply.", LoggerType.AppearanceState);
        var appliedItems = new Dictionary<EquipSlot, CursedItem>();

        foreach (var cursedItem in cursedItems)
        {
            if (appliedItems.TryGetValue(cursedItem.AppliedItem.Slot, out var existingItem))
            {
                // if an item was already applied to that slot, only apply if it satisfied conditions.
                if (existingItem.CanOverride && cursedItem.OverridePrecedence >= existingItem.OverridePrecedence)
                {
                    Logger.LogTrace($"Slot: " + cursedItem.AppliedItem.Slot + " already had an item [" + existingItem.Name + "]. "
                        + "but [" + cursedItem.Name + "] had higher precedence", LoggerType.AppearanceState);
                    appliedItems[cursedItem.AppliedItem.Slot] = cursedItem;
                }
            }
            else
            {
                Logger.LogTrace($"Storing Cursed Item [" + cursedItem.Name + "] to Slot: " + cursedItem.AppliedItem.Slot, LoggerType.AppearanceState);
                if (cursedItem.IsGag)
                {
                    // store the item set in the gag storage
                    var drawData = _clientConfigs.GetDrawData(cursedItem.GagType);
                    ItemsToApply[drawData.Slot] = drawData;
                }
                else
                {
                    // Store the equip item.
                    appliedItems[cursedItem.AppliedItem.Slot] = cursedItem;
                }
            }

            // add in the moodle if it exists.
            if (!_playerData.IpcDataNull)
            {
                if (cursedItem.MoodleType is IpcToggleType.MoodlesStatus && cursedItem.MoodleIdentifier != Guid.Empty)
                    ExpectedMoodles.UnionWith(new List<Guid>() { cursedItem.MoodleIdentifier });

                else if (cursedItem.MoodleType is IpcToggleType.MoodlesPreset && cursedItem.MoodleIdentifier != Guid.Empty)
                    ExpectedMoodles
                        .UnionWith(_playerData.LastIpcData!.MoodlesPresets
                            .Where(p => p.Item1 == cursedItem.MoodleIdentifier)
                            .SelectMany(p => p.Item2));
            }
        }

        // take the dictionary of applied items and replace any existing items in the ItemsToApply dictionary.
        foreach (var item in appliedItems)
        {
            Logger.LogTrace($"Applying Cursed Item to Slot: {item.Key}", LoggerType.AppearanceState);
            if (item.Value.IsGag)
            {
                var drawData = _clientConfigs.GetDrawData(item.Value.GagType);
                ItemsToApply[drawData.Slot] = drawData;
            }
            else
            {
                ItemsToApply[item.Key] = item.Value.AppliedItem;
            }
        }

        // if we are fetching moodles manually, we should do so now.
        await UpdateLatestMoodleData();

        // Update the stored data.
        _appearanceService.ItemsToApply = ItemsToApply;
        _appearanceService.MetaToApply = MetaToApply;
        _appearanceService.ExpectedMoodles = ExpectedMoodles;
        _appearanceService.ExpectedCustomizations = ExpectedCustomizations;

        Logger.LogInformation("Appearance Data Recalculated.", LoggerType.AppearanceState);
        return;
    }

    private async Task MoodlesUpdated(IntPtr address)
    {
        if (address != _clientService.Address)
            return;

        List<MoodlesStatusInfo> latest = new List<MoodlesStatusInfo>();
        await _frameworkUtils.RunOnFrameworkTickDelayed(async () =>
        {
            Logger.LogDebug("Grabbing Latest Status", LoggerType.IpcGlamourer);
            latest = await _ipcManager.Moodles.GetStatusInfoAsync().ConfigureAwait(false);
        }, 2);

        HashSet<Guid> latestGuids = new HashSet<Guid>(latest.Select(x => x.GUID));
        Logger.LogTrace("Latest Moodles  : " + string.Join(", ", latestGuids), LoggerType.IpcMoodles);
        Logger.LogTrace("Expected Moodles: " + string.Join(", ", ExpectedMoodles), LoggerType.IpcMoodles);
        // if any Guid in ExpectedMoodles are not present in latestGuids, request it to be reapplied, instead of pushing status manager update.
        var moodlesToReapply = ExpectedMoodles.Except(latestGuids).ToList();
        Logger.LogDebug("Missing Moodles from Required: " + string.Join(", ", moodlesToReapply), LoggerType.IpcMoodles);
        if (moodlesToReapply.Any())
        {
            Logger.LogTrace("You do not currently have all active moodles that should be active from your restraints. Reapplying.", LoggerType.IpcMoodles);
            // obtain the moodles that we need to reapply to the player from the expected moodles.            
            await _ipcManager.Moodles.ApplyOwnStatusByGUID(moodlesToReapply);
            return;
        }
        else
        {
            if (LastIpcData is not null)
            {
                var list = LastIpcData.MoodlesDataStatuses.Select(x => x.GUID);
                // determine if the two lists are the same or not.
                if (list.SequenceEqual(latestGuids))
                    return;
            }
            Logger.LogDebug("Pushing IPC update to CacheCreation for processing", LoggerType.IpcMoodles);
            Mediator.Publish(new MoodlesStatusManagerUpdate());
        }
    }

    /// <summary>
    /// Cycle a while loop to wait for when we are finished redrawing, if we are currently redrawing.
    /// </summary>
    private async Task WaitForRedrawCompletion()
    {
        // Return if we are not redrawing.
        if (!ManualRedrawProcessing)
            return;

        RedrawTokenSource?.Cancel();
        RedrawTokenSource = new CancellationTokenSource();

        var token = RedrawTokenSource.Token;
        int delay = 20; // Initial delay of 20 ms
        const int maxDelay = 1280; // Max allowed delay

        try
        {
            while (ManualRedrawProcessing)
            {
                // Check if cancellation is requested
                if (token.IsCancellationRequested)
                {
                    Logger.LogWarning("Manual redraw processing wait was cancelled due to timeout.");
                    return;
                }

                // Wait for the current delay period
                await Task.Delay(delay, token);

                // Double the delay for the next iteration
                delay *= 2;

                // If the delay exceeds the maximum limit, log a warning and exit the loop
                if (delay > maxDelay)
                {
                    Logger.LogWarning("Player redraw is taking too long. Exiting wait.");
                    return;
                }
            }

            Logger.LogDebug("Manual redraw processing completed. Proceeding with refresh.");
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogWarning("WaitForRedrawCompletion was canceled: " + ex.Message);
            // Handle the cancellation gracefully
        }
        catch (Exception ex)
        {
            Logger.LogError("An error occurred in WaitForRedrawCompletion: " + ex.Message);
            throw; // Re-throw if it's not a TaskCanceledException
        }
    }
}
