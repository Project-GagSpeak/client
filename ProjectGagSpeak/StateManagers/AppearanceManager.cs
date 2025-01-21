using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.Localization;
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
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using System.Security.Cryptography.Pkcs;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Render.ModelRenderer;
using static Lumina.Data.Parsing.Layer.LayerCommon;

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
    private readonly GagGarbler _garbler;
    private readonly PairManager _pairManager;
    private readonly IpcManager _ipcManager;
    private readonly AppearanceService _appearanceService;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly PublishStateService _publishService;

    public AppearanceManager(ILogger<AppearanceManager> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientData playerData,
        GagGarbler garbler, PairManager pairManager, IpcManager ipcManager, 
        AppearanceService appearanceService, ClientMonitorService clientService,
        OnFrameworkService frameworkUtils, PublishStateService publishService) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _garbler = garbler;
        _pairManager = pairManager;
        _ipcManager = ipcManager;
        _appearanceService = appearanceService;
        _clientService = clientService;
        _frameworkUtils = frameworkUtils;
        _publishService = publishService;

        Mediator.Subscribe<ClientPlayerInCutscene>(this, (msg) => _ = _appearanceService.RefreshAppearance(GlamourUpdateType.ReapplyAll));
        Mediator.Subscribe<CutsceneEndMessage>(this, (msg) => _ = _appearanceService.RefreshAppearance(GlamourUpdateType.ReapplyAll));
        Mediator.Subscribe<AppearanceImpactingSettingChanged>(this, (msg) => _ = RecalcAndReload(true));

        Mediator.Subscribe<IpcDataCreatedMessage>(this, (msg) => LastIpcData = msg.CharaIPCData);


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

    public bool CanEnableSet(Guid restraintId) => _publishService.CanApplyRestraint(restraintId, out _);
    public bool CanDisableSet(Guid restraintId) => _publishService.CanRemoveRestraint(restraintId, out _);
    public bool CanGagApply(GagLayer layer) => _playerData.AppearanceData is not null && _publishService.CanApplyGag(layer);

    #region Updates
    public async Task SwapOrApplyRestraint(Guid newSetId, string assignerUid, bool publishOnApply)
    {
        // grab the active set for comparison.
        var activeSet = _clientConfigs.GetActiveSet();
        if (activeSet is not null && activeSet.RestraintId == newSetId)
        {
            Logger.LogWarning("SwapOrApplyRestraint was called with the same set ID as the active set. " +
                "This should technically never happen, but wont cause any bugs.", LoggerType.AppearanceState);
            return;
        }

        // if the active set is null, simply enable the new set.
        if (activeSet is null)
        {
            await EnableRestraintSet(newSetId, assignerUid, true, !publishOnApply);
        }
        // otherwise, we need to disable the active set, then enable the new set.
        else
        {
            Logger.LogTrace("SET-SWAPPED Executed. Triggering DISABLE-SET, then ENABLE-SET", LoggerType.AppearanceState);
            // First, disable the current set.
            await DisableRestraintSet(activeSet.RestraintId, assignerUid, true, true);
            // Then, enable the new set.
            await EnableRestraintSet(newSetId, assignerUid, true, !publishOnApply);
        }
    }

    public async Task EnableRestraintSet(Guid restraintId, string enactor, bool triggerAchievement, bool forced)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            if (!_publishService.CanApplyRestraint(restraintId, out var setRef) || _playerData.GlobalPerms is null)
                return;

            Logger.LogDebug("ENABLING SET [" + setRef.Name + "]", LoggerType.AppearanceState);
            // make changes.
            setRef.Enabled = true;
            setRef.EnabledBy = enactor;
            _clientConfigs.SaveWardrobe();

            // Enable the Hardcore Properties.
            if (setRef.HasPropertiesForUser(setRef.EnabledBy))
            {
                Logger.LogDebug("Set Contains HardcoreProperties for " + setRef.EnabledBy, LoggerType.AppearanceState);
                if (setRef.PropertiesEnabledForUser(setRef.EnabledBy))
                    IpcFastUpdates.InvokeHardcoreTraits(NewState.Enabled, setRef);
            }

            // Handle if we should perform glamour applications for these.(for now forced is included for safewords to work, and will be removed later during glamourer rework)
            if (_playerData.GlobalPerms.RestraintSetAutoEquip || forced)
            {
                // Set associated mods in penumbra.
                await PenumbraModsToggle(NewState.Enabled, setRef.AssociatedMods);
                // recalculate appearance and refresh.
                await RecalcAndReload(false);
            }

            // log completion.
            Logger.LogDebug("Set: " + setRef.Name + " has been applied by: " + enactor, LoggerType.AppearanceState);

            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintStateChange, restraintId, true, enactor);

            if (forced is false) 
                Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintApplied, restraintId.ToString()));
        });
    }

    public bool LockRestraintSet(Guid restraintId, Padlocks padlock, string pwd, string endTime, string enactor, bool triggerAchievement, bool forced)
    {
        if (!_publishService.CanLockRestraint(restraintId, out var setRef))
            return false;

        Logger.LogDebug("LOCKING SET [" + setRef.Name + "]", LoggerType.AppearanceState);
        // Store a copy of the values we need before we change them.
        var prevLock = setRef.Padlock;

        // perform the update for the lock state. If it fails to update, it will return false, thus we should return false.
        if(!UpdateStateForLocking(setRef, padlock, pwd, endTime, enactor, forced))
            return false;

        // Finally, we should fire to our achievement manager, if we have marked for us to.
        if (triggerAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, restraintId, padlock, true, enactor);

        // There will never be any case where we push publications if forceUpdate is true. Only every publish after validations.
        if (forced is false)
            Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintLocked, prevLock));

        return true;
    }

    public bool UnlockRestraintSet(Guid restraintId, string guessedPass, string enactorUid, bool triggerAchievement, bool forced)
    {
        if (!_publishService.CanUnlockRestraint(restraintId, out var setRef))
            return false;

        Logger.LogDebug("UNLOCKING SET [" + setRef.Name + "]", LoggerType.AppearanceState);
        // Store a copy of the values we need before we change them.
        var prevLock = setRef.Padlock;
        var prevAssigner = setRef.Assigner;

        // perform the update for the lock state. If it fails to update, it will return false, thus we should return false.
        if (!UpdateStateForUnlocking(setRef, guessedPass, enactorUid, forced))
            return false;

        // Handle achievements.
        bool soldSlaveSatisfied = (prevAssigner != MainHub.UID) && (enactorUid != MainHub.UID) && (enactorUid != prevAssigner);
        if (triggerAchievement && soldSlaveSatisfied)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.SoldSlave);

        if (triggerAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, restraintId, prevLock.ToPadlock(), false, enactorUid);

        // push to server if not a callback.
        if (forced is false)
            Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintUnlocked, prevLock));

        return true;
    }

    public async Task DisableRestraintSet(Guid restraintId, string enactor, bool triggerAchievement, bool forced)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            if (!_publishService.CanRemoveRestraint(restraintId, out var setRef))
                return;

            Logger.LogDebug("DISABLING SET [" + setRef.Name + "]", LoggerType.AppearanceState);

            // Remove temporarily applied mods.
            await PenumbraModsToggle(NewState.Disabled, setRef.AssociatedMods);

            // Remove any bound moodles to the set.
            var moodlesToRemove = RemoveMoodles(setRef);

            // Attach hardcore properties.
            if (setRef.HasPropertiesForUser(setRef.EnabledBy))
            {
                Logger.LogDebug("Set Contains HardcoreProperties for " + setRef.EnabledBy, LoggerType.AppearanceState);
                if (setRef.PropertiesEnabledForUser(setRef.EnabledBy))
                    IpcFastUpdates.InvokeHardcoreTraits(NewState.Disabled, setRef);
            }

            // fire changes now that the traits have been handled.
            setRef.Enabled = false;
            setRef.EnabledBy = string.Empty;
            _clientConfigs.SaveWardrobe();

            // recalculate appearance and refresh.
            await RecalcAndReload(true, moodlesToRemove);

            // Handle achievement triggers.
            bool auctionedOffSatisfied = setRef.EnabledBy != MainHub.UID && enactor != MainHub.UID && enactor != setRef.EnabledBy;
            if (triggerAchievement && auctionedOffSatisfied) // To Satisfy Auctioned off, set must be applied by one person, and removed by another.
                UnlocksEventManager.AchievementEvent(UnlocksEvent.AuctionedOff);

            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintStateChange, restraintId, false, enactor);

            if (forced is false)
                Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.RestraintDisabled, restraintId.ToString()));
        });
    }

    public async Task SwapOrApplyGag(GagLayer layer, GagType newGag, string enactor, bool forced)
    {
        if(_playerData.AppearanceData is null)
            return;

        // grab the current gagtype on the layer.
        var currentGag = _playerData.AppearanceData!.GagSlots[(int)layer].GagType.ToGagType();

        // if the new gag is the same as the current, do nothing.
        if (currentGag == newGag)
            return;

        // if the current gag is not GagType.None, perform a swap, otherwise, perform an apply.
        if (currentGag is GagType.None)
        {
            Logger.LogTrace("GAG-APPLIED Executed", LoggerType.AppearanceState);
            await GagApplied(layer, newGag, enactor, true, forced);
        }
        // otherwise, we need to disable the active set, then enable the new set.
        else
        {
            Logger.LogTrace("GAG-SWAPPED Executed. Triggering GAG-REMOVE, then GAG-APPLIED", LoggerType.AppearanceState);
            // First, remove the current gag.
            await GagRemoved(layer, enactor, true, true);
            // Then, apply the new gag.
            await GagApplied(layer, newGag, enactor, true, forced);
        }
    }

    public async Task GagApplied(GagLayer layer, GagType gagType, string enactor, bool triggerAchievement, bool forced)
    {
        await ExecuteWithApplierSlim(async () => { await GagApplyInternal(layer, gagType, enactor, triggerAchievement, forced); });
    }

    private async Task GagApplyInternal(GagLayer layer, GagType gagType, string enactor, bool triggerAchievement, bool forced)
    {
        if(!_publishService.CanApplyGag(layer) || _playerData.AppearanceData is null || _playerData.GlobalPerms is null)
            return;

        Logger.LogDebug("GAG-APPLIED triggered on slot [" + layer.ToString() + "] with a [" + gagType.GagName() + "]", LoggerType.AppearanceState);
        // update the gag slot information for this layer.
        _playerData.AppearanceData.GagSlots[(int)layer].GagType = gagType.GagName();
        // update garbler logic.
        _garbler.UpdateGarblerLogic();

        // Apply appearance changes if enabled.(for now forced is included for safewords to work, and will be removed later during glamourer rework)
        if (_playerData.GlobalPerms.ItemAutoEquip || forced)
        {
            await RecalcAndReload(false);
            // Update C+ Profile if applicable
            var drawData = _clientConfigs.GetDrawData(gagType);
            if (drawData.CustomizeGuid != Guid.Empty)
                _ipcManager.CustomizePlus.EnableProfile(drawData.CustomizeGuid);
        }

        // handle achievements.
        if (triggerAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.GagStateChange, true, layer, gagType, enactor);

        // publish the changes to the mediator.
        if (forced is false)
            Mediator.Publish(new PlayerCharAppearanceChanged(layer, GagUpdateType.GagApplied, Padlocks.None));
    }

    public bool GagLocked(GagLayer layer, Padlocks lockType, string pass, string endTime, string enactor, bool triggerAchievement, bool forced)
    {
        if (!_publishService.CanLockGag(layer) || _playerData.AppearanceData is null)
            return false;

        Logger.LogInformation(_playerData.AppearanceData.ToGagString());

        var previousPadlock = _playerData.AppearanceData.GagSlots[(int)layer].Padlock.ToPadlock();

        if(!UpdateStateForLocking(_playerData.AppearanceData.GagSlots[(int)layer], lockType, pass, endTime, enactor, forced))
            return false;

        // Send Achievement Event
        if (triggerAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, true, layer, lockType, enactor);

        // publish the changes to the mediator.
        if(forced is false)
            Mediator.Publish(new PlayerCharAppearanceChanged(layer, GagUpdateType.GagLocked, previousPadlock));

        return true;
    }

    public bool GagUnlocked(GagLayer layer, string guessedPass, string enactorUid, bool triggerAchievement, bool forced)
    {
        // return early if we are not allowed to apply.
        if (!_publishService.CanUnlockGag(layer) || _playerData.AppearanceData is null)
            return false;

        // store the previous information.
        var padlockRemoved = _playerData.AppearanceData.GagSlots[(int)layer].Padlock.ToPadlock();

        if (!UpdateStateForUnlocking(_playerData.AppearanceData.GagSlots[(int)layer], guessedPass, enactorUid, forced))
            return false;

        // Send Achievement Event
        if (triggerAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, false, layer, padlockRemoved, enactorUid);

        // publish the changes to the mediator.
        if (forced is false)
            Mediator.Publish(new PlayerCharAppearanceChanged(layer, GagUpdateType.GagUnlocked, padlockRemoved));

        return true;
    }

    public async Task GagRemoved(GagLayer layer, string enactorUid, bool triggerAchievement, bool forced)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            if(!_publishService.CanRemoveGag(layer) || _playerData.AppearanceData is null || _playerData.GlobalPerms is null)
                return;

            var gagRemoved = _playerData.AppearanceData.GagSlots[(int)layer].GagType.ToGagType();

            Logger.LogDebug("GAG-REMOVE triggered on slot [" + layer.ToString() + "], removing your [" + gagRemoved.GagName() + "]", LoggerType.AppearanceState);

            // update the data and the garbler logic.
            _playerData.AppearanceData.GagSlots[(int)layer].GagType = GagType.None.GagName();
            _garbler.UpdateGarblerLogic();


            // Apply appearance changes if enabled.(for now forced is included for safewords to work, and will be removed later during glamourer rework)
            if (_playerData.GlobalPerms.ItemAutoEquip || forced)
            {
                // Once it's been set to inactive, we should also remove our moodles.
                var gagSettings = _clientConfigs.GetDrawData(gagRemoved);
                var moodlesToRemove = RemoveMoodles(gagSettings);

                await RecalcAndReload(true, moodlesToRemove);

                // Remove the CustomizePlus Profile if applicable
                if (gagSettings.CustomizeGuid != Guid.Empty)
                    _ipcManager.CustomizePlus.DisableProfile(gagSettings.CustomizeGuid);
            }

            // handle achievement sending
            if (triggerAchievement)
                UnlocksEventManager.AchievementEvent(UnlocksEvent.GagStateChange, false, layer, gagRemoved, enactorUid);

            // publish the changes to the mediator.
            if (forced is false)
                Mediator.Publish(new PlayerCharAppearanceChanged(layer, GagUpdateType.GagRemoved, Padlocks.None));
        });
    }

    /// <summary>
    /// For applying cursed items.
    /// </summary>
    /// <param name="gagLayer"> Ignore this if the cursed item's IsGag is false. </param>
    public async Task CursedItemApplied(CursedItem cursedItem)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            Logger.LogTrace("CURSED-APPLIED Executed");
            // A cursed gag item shoudlnt be allowed to exist in this function. If it is, log error and return.
            if (cursedItem.IsGag)
            {
                Logger.LogError("CursedItemApplied was called with a Gag CursedItem, but this function does not support gag application.", LoggerType.AppearanceState);
                return;
            }

            // Cursed Item was Equip, so handle attached Mod Enable and recalculation here.
            await PenumbraModsToggle(NewState.Enabled, new List<AssociatedMod>() { cursedItem.AssociatedMod });
            await RecalcAndReload(false);

            // update achievements and push
            UnlocksEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
            Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.CursedItemApplied, cursedItem.LootId.ToString()));
        });
    }

    /// <summary>
    /// Variant of normal cursed item intended for gagtypes.
    /// </summary>
    public async Task CursedGagApplied(CursedItem cursedItem, GagLayer layer, DateTimeOffset releaseTimeUTC)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            // return if we cannot apply a gag to the slot.
            Logger.LogTrace("CURSED-APPLIED Executed", LoggerType.AppearanceState);

            // validate we can apply gags to this slot.
            if (!_publishService.CanApplyGag(layer) || _playerData.AppearanceData is null)
                return;

            // perform the apply operation without publishing, but fire achievements.
            await GagApplyInternal(layer, cursedItem.GagType, MainHub.UID, true, true);
            _playerData.AppearanceData.GagSlots[(int)layer].Padlock = Padlocks.MimicPadlock.ToName();
            _playerData.AppearanceData.GagSlots[(int)layer].Timer = releaseTimeUTC;
            _playerData.AppearanceData.GagSlots[(int)layer].Assigner = MainHub.UID;
            // fire lock related achievements.
            UnlocksEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, true, layer, Padlocks.MimicPadlock, MainHub.UID);

            // now we need to log that we have applied the cursed item.
            Logger.LogDebug("CURSED ITEM [" + cursedItem.Name + "] APPLIED TO GAG SLOT [" + layer.ToString() + "]", LoggerType.AppearanceState);

            // Update achievements and publish.
            UnlocksEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
            Mediator.Publish(new PlayerCharAppearanceChanged(layer, GagUpdateType.MimicGagApplied, Padlocks.None));
            Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.CursedItemApplied, cursedItem.LootId.ToString()));
        });
    }


    /// <summary>
    /// Easy incorrect assumption to make is that this only disables the associations for said items
    /// if NOT a gag. If it is a gag, it will need to be removed manually.
    /// </summary>
    public async Task CursedItemRemoved(CursedItem cursedItem, bool forced)
    {
        await ExecuteWithApplierSlim(async () =>
        {
            Logger.LogTrace("CURSED-REMOVED Executed", LoggerType.AppearanceState);
            // Only process the removal of things if not a gag cursed item.
            if (cursedItem.IsGag is false)
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

            if (forced is false) 
                Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.CursedItemRemoved, cursedItem.LootId.ToString()));
        });
    }
    #endregion Updates

    #region Helpers
    private bool UpdateStateForLocking<T>(T item, Padlocks lockType, string pass, string timer, string enactor, bool isForced) where T : IPadlockable
    {
        // if it is not forced, require the validation process.
        if (isForced is false)
        {
            var validationResult = GsPadlockEx.ValidateLockUpdate(item, lockType, pass, timer, enactor);
            if (validationResult is not PadlockReturnCode.Success)
            {
                Logger.LogDebug("Failed to lock padlock: " + item.Padlock + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
                return false;
            }
        }
        // otherwise, update the values directly. (no need for else statement, if validation is true, we set it anyways.
        GsPadlockEx.PerformLockUpdate(ref item, lockType, pass, timer, enactor);
        _clientConfigs.SaveWardrobe();
        Logger.LogDebug("Item was locked with Padlock: " + lockType +
            " for: " + (timer.GetEndTimeUTC() - DateTimeOffset.UtcNow) + " by: " + enactor, LoggerType.AppearanceState);
        return true;
    }

    private bool UpdateStateForUnlocking<T>(T item, string pass, string enactor, bool isForced) where T : IPadlockable
    {
        // if it is not forced, require the validation process.
        if (isForced is false)
        {
            var validationResult = GsPadlockEx.ValidateUnlockUpdate(item, MainHub.PlayerUserData, pass, enactor);
            if (validationResult is not PadlockReturnCode.Success)
            {
                Logger.LogDebug("Failed to unlock padlock: " + item.Padlock + " due to: " + validationResult.ToFlagString(), LoggerType.PadlockHandling);
                return false;
            }
        }
        // otherwise, update the values directly. (no need for else statement, if validation is true, we set it anyways.
        GsPadlockEx.PerformUnlockUpdate(ref item);
        _clientConfigs.SaveWardrobe();
        Logger.LogDebug("Item was unlocked by: " + enactor, LoggerType.AppearanceState);
        return true;
    }
    #endregion Helpers

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
                    GagUnlocked((GagLayer)i, gagSlot.Password, gagSlot.Assigner, true, true); // (doesn't fire any achievements so should be fine)
                    // then we should remove it, but not publish it to the mediator just yet.
                    await GagRemoved((GagLayer)i, MainHub.UID, true, true);
                }
            }

            Logger.LogInformation("Active gags disabled.", LoggerType.Safeword);
            Mediator.Publish(new PlayerCharAppearanceChanged(GagLayer.UnderLayer, GagUpdateType.Safeword, Padlocks.None));
        }

        // if an active set exists we need to unlock and disable it.
        if (_clientConfigs.TryGetActiveSet(out var set))
        {
            Logger.LogInformation("Unlocking and Disabling Active Set [" + set.Name + "] due to Safeword.", LoggerType.Safeword);

            // unlock the set, dont push changes yet.
            UnlockRestraintSet(set.RestraintId, set.Password, set.Assigner, true, true);
            // Disable the set, turning off any mods moodles ext and refreshing appearance.            
            await DisableRestraintSet(set.RestraintId, MainHub.UID, true, true);
        }

        // Disable all Cursed Items.
        Logger.LogInformation("Disabling all active Cursed Items due to Safeword.", LoggerType.Safeword);
        foreach (var cursedItem in CursedItems.Where(x => x.AppliedTime != DateTimeOffset.MinValue).ToList())
        {
            _clientConfigs.DeactivateCursedItem(cursedItem.LootId);
            await CursedItemRemoved(cursedItem, true);
        }
        // push appearance update.
        Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.Safeword, string.Empty));
    }

    /// <summary> Applies associated mods to the client when a Restraint or Cursed Item is toggled. </summary>
    public Task PenumbraModsToggle(NewState state, List<AssociatedMod> associatedMods)
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
                    _ipcManager.Penumbra.ModifyModState(associatedMod, modState: NewState.Disabled);

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
        if (_clientConfigs.TryGetActiveSet(out var activeSetRef))
        {
            foreach (var item in activeSetRef.DrawData)
            {
                if (!item.Value.IsEnabled && item.Value.GameItem.Equals(ItemIdVars.NothingItem(item.Value.Slot)))
                    continue;

                Logger.LogTrace("Adding item to apply: " + item.Key, LoggerType.AppearanceState);
                ItemsToApply[item.Key] = item.Value;
            }
            // Add the moodles from the active set.
            if (_playerData.LastIpcData is not null)
            {
                if (activeSetRef.AssociatedMoodles.Count > 0)
                    ExpectedMoodles.UnionWith(activeSetRef.AssociatedMoodles);
                if (activeSetRef.AssociatedMoodlePreset != Guid.Empty)
                {
                    var statuses = _playerData.LastIpcData.MoodlesPresets
                        .FirstOrDefault(p => p.Item1 == activeSetRef.AssociatedMoodlePreset).Item2;
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
                // only apply the glamour item if it is not an empty item.
                if (!data.GameItem.Equals(ItemIdVars.NothingItem(data.Slot)))
                    ItemsToApply[data.Slot] = data;

                // continue if moodles data is not present.
                if (_playerData.LastIpcData is not null)
                {
                    if (data.AssociatedMoodles.Count > 0) ExpectedMoodles.UnionWith(data.AssociatedMoodles);

                    if (data.AssociatedMoodlePreset != Guid.Empty)
                    {
                        var statuses = _playerData.LastIpcData.MoodlesPresets.FirstOrDefault(p => p.Item1 == data.AssociatedMoodlePreset).Item2;
                        if (statuses is not null) ExpectedMoodles.UnionWith(statuses);
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
        else
        {
            Logger.LogDebug("We are not Blindfolded.", LoggerType.AppearanceState);
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
            if (_playerData.LastIpcData is not null)
            {
                if (cursedItem.MoodleType is IpcToggleType.MoodlesStatus && cursedItem.MoodleIdentifier != Guid.Empty)
                    ExpectedMoodles.UnionWith(new List<Guid>() { cursedItem.MoodleIdentifier });

                else if (cursedItem.MoodleType is IpcToggleType.MoodlesPreset && cursedItem.MoodleIdentifier != Guid.Empty)
                    ExpectedMoodles
                        .UnionWith(_playerData.LastIpcData.MoodlesPresets
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

        HashSet<MoodlesStatusInfo> latestInfos = latest.ToHashSet();
        Logger.LogTrace("Latest Moodles  : " + string.Join(", ", latestInfos.Select(x => x.GUID)), LoggerType.IpcMoodles);
        Logger.LogTrace("Expected Moodles: " + string.Join(", ", ExpectedMoodles), LoggerType.IpcMoodles);
        // if any Guid in ExpectedMoodles are not present in latestGuids, request it to be reapplied, instead of pushing status manager update.
        var moodlesToReapply = ExpectedMoodles.Except(latestInfos.Select(x => x.GUID)).ToList();
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
                // determine if the two lists are the same or not.
                if (LastIpcData.MoodlesDataStatuses.Select(x => x.GUID).SequenceEqual(latestInfos.Select(x => x.GUID)) 
                 && LastIpcData.MoodlesDataStatuses.Select(x => x.Stacks).SequenceEqual(latestInfos.Select(x => x.Stacks)))
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
