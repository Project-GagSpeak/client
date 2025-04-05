using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Components;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using OtterGui.Classes;

namespace GagSpeak.PlayerState.Listener;

/// <summary>
/// This class handles all incoming events from IPC's or other manager sources and 
/// applies their respective update to the cached changes and their appliers.
/// </summary>
/// <remarks> The Listener is unique in that it can access both the active VisualState, and the Appliers for them.</remarks>
public sealed class VisualStateListener : DisposableMediatorSubscriberBase
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly PairManager             _pairs;
    private readonly IpcManager              _interop;
    private readonly RestraintManager        _restraints;
    private readonly RestrictionManager      _restrictions;
    private readonly GagRestrictionManager   _gags;
    private readonly CursedLootManager       _cursedLoot;
    private readonly ModSettingPresetManager _modSettingPresets;
    private readonly TraitsManager           _traits;
    private readonly VisualApplierGlamour    _glamourApplier;
    private readonly VisualApplierPenumbra   _modApplier;
    private readonly VisualApplierMoodles    _moodleApplier;
    private readonly VisualApplierCPlus      _customizeApplier;
    private readonly ClientMonitor           _clientMonitor;
    private readonly OnFrameworkService      _frameworkUtils;

    public VisualStateListener(
        ILogger<VisualStateListener> logger,
        GagspeakMediator mediator,
        GagspeakConfigService config,
        IpcManager interop,
        PairManager pairs,
        RestraintManager restraints,
        RestrictionManager restrictions,
        GagRestrictionManager gags,
        CursedLootManager cursedLoot,
        ModSettingPresetManager modSettingPresets,
        TraitsManager traits,
        VisualApplierGlamour glamourApplier,
        VisualApplierPenumbra modApplier,
        VisualApplierMoodles moodleApplier,
        VisualApplierCPlus customizeApplier,
        ClientMonitor clientMonitor,
        OnFrameworkService frameworkUtils,
        IDalamudPluginInterface pi) : base(logger, mediator)
    {
        _mainConfig = config;
        _interop = interop;
        _pairs = pairs;
        _restraints = restraints;
        _restrictions = restrictions;
        _gags = gags;
        _cursedLoot = cursedLoot;
        _modSettingPresets = modSettingPresets;
        _traits = traits;
        _glamourApplier = glamourApplier;
        _modApplier = modApplier;
        _moodleApplier = moodleApplier;
        _customizeApplier = customizeApplier;
        _clientMonitor = clientMonitor;
        _frameworkUtils = frameworkUtils;

        // Initialize _activeVisuals after instance variables are assigned
        ManagerCacheRef = new SortedDictionary<ManagerPriority, IVisualCache>
        {
            { ManagerPriority.Restraints, _restraints.LatestVisualCache },
            { ManagerPriority.Restrictions, _restrictions.LatestVisualCache },
            { ManagerPriority.Gags, _gags.LatestVisualCache }
        };

        // Subscribers for Glamourer
        _interop.Glamourer.StateWasChanged = StateChangedWithType.Subscriber(pi, OnStateChanged);
        _interop.Glamourer.StateWasFinalized = StateFinalized.Subscriber(pi, OnStateFinalized);
        _interop.Glamourer.StateWasChanged.Enable();
        _interop.Glamourer.StateWasFinalized.Enable();
        // Subscribers for Moodles
        _interop.Moodles.OnStatusManagerModified.Subscribe(OnStatusManagerModified);
        _interop.Moodles.OnStatusSettingsModified.Subscribe(OnStatusModified);
        _interop.Moodles.OnPresetModified.Subscribe(OnPresetModified);
        // Subscribers for Customize+
        _interop.CustomizePlus.OnProfileUpdate.Subscribe(OnProfileUpdate);

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => _gags.CheckForExpiredLocks());

    }

    private SortedDictionary<ManagerPriority, IVisualCache> ManagerCacheRef;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _interop.CustomizePlus.OnProfileUpdate.Unsubscribe(OnProfileUpdate);
        _interop.Moodles.OnStatusManagerModified.Unsubscribe(OnStatusManagerModified);
        _interop.Moodles.OnStatusSettingsModified.Unsubscribe(OnStatusModified);
        _interop.Moodles.OnPresetModified.Unsubscribe(OnPresetModified);
        _interop.Glamourer.StateWasChanged.Disable();
        _interop.Glamourer.StateWasChanged?.Dispose();
        _interop.Glamourer.StateWasFinalized.Disable();
        _interop.Glamourer.StateWasFinalized?.Dispose();
    }

    #region IPC Event Listeners.
    /// <summary> When ANY Glamourer state change occurs for ANY given actor, this is fired. </summary>
    /// <param name="address">The address of the actor that was changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <remarks> This is primarily used to cache the state of the Client. Discarded for other players. </remarks>
    private void OnStateChanged(nint address, StateChangeType changeType)
    {
        if (address != _clientMonitor.Address)
            return;

        if (changeType is not (StateChangeType.Equip or StateChangeType.Stains or StateChangeType.Other))
            return;

        if (_glamourApplier.StateChangeBlocked)
        {
            Logger.LogTrace($"OnStateChanged blocked: {(_glamourApplier.StateChangeBlocked 
                ? "BlockStateChangeEvent" : "AppearanceUpdateProcessing")}", LoggerType.IpcGlamourer);
            return;
        }

        // Handle MetaData change.
        if (changeType is StateChangeType.Other)
        {
            Logger.LogTrace($"Located Meta value change!", LoggerType.IpcGlamourer);
            // Do some invocation here.
            return;
        }

        // Handle Single Slot updates to ensure client remains restricted.
        Logger.LogTrace($"Accepted StateChange of type {changeType} occurred within allowance window!", LoggerType.IpcGlamourer);
        _glamourApplier.OnStateChanged(changeType).ConfigureAwait(false);
    }

    /// <summary> Any any primary Glamourer Operation has completed, StateFinalized will fire. (This IPC Call is a Godsend). </summary>
    /// <param name="address">The address of the actor that was finalized.</param>
    /// <param name="finalizationType">The type of finalization that occurred.</param>
    /// <remarks> This is primarily used to cache the state of the player after a glamour operation has completed. </remarks>
    private void OnStateFinalized(nint address, StateFinalizationType finalizationType)
    {
        if (address != _clientMonitor.Address)
            return;

        // Should not need any further filters here, should be simply base value.
        Logger.LogDebug($"OnStateFinalized! Finalization Type: {finalizationType}", LoggerType.IpcGlamourer);
        _glamourApplier.OnStateFinalized(finalizationType).ConfigureAwait(false);
    }

    /// <summary> This method is called when the moodles status list changes </summary>
    /// <param name="character">The character that had modified moodles.</param>
    private void OnStatusManagerModified(IPlayerCharacter character) => _moodleApplier.StatusManagerModified(character.Address);

    /// <summary> Called whenever our client changed the settings to any of their moodles in the moodles GUI </summary>
    /// <remarks> You will need to click off any text boxes to have this fire. Clicking directly from Moodles to another UI won't fire this. </remarks>
    private void OnStatusModified(Guid guid) => _moodleApplier.ClientStatusModified(guid);
    /// <summary> This method is called when the moodles change </summary>
    /// <remarks> You will need to click off any text boxes to have this fire. Clicking directly from Moodles to another UI won't fire this. </remarks>
    private void OnPresetModified(Guid guid) => _moodleApplier.ClientPresetModified(guid);

    /// <summary> This method is called when the Customize+ profile changes </summary>
    private void OnProfileUpdate(ushort characterObjectIndex, Guid g) => _customizeApplier.OnProfileUpdate(characterObjectIndex, g);

    #endregion IPC Event Listeners.

    private bool PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
        {
            Mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
            return true;
        }
        return false;
    }

    #region Gag Manipulation
    public async Task SwapOrApplyGag(CallbackGagDataDto gagData)
    {
        if(gagData.PreviousGag is GagType.None)
            await ApplyGag(gagData);
        else
            await SwapGag(gagData);
    }

    /// <summary> We have received the instruction to swap our gag as a callback from the server. </summary>
    /// <remarks> We can skip all validation checks for this, but don't update if not connected. </remarks>
    public async Task SwapGag(CallbackGagDataDto gagData)
    {
        if (!MainHub.IsConnected) 
            return;
        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Callbacks);
        await RemoveGag(gagData, false);
        await ApplyGag(gagData);

        PostActionMsg(gagData.Enactor.UID, InteractionType.SwappedGag, gagData.PreviousGag.GagName() + " swapped to " + gagData.NewData.GagItem + " on layer " + gagData.AffectedLayer);
    }

    /// <summary> Applies a Cursed Gag Item </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task ApplyCursedGag(/* Should be called directly from the Cursed Loot Handler but idk.*/) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    /// <summary> Applies a Gag to a spesified layer. </summary>
    public async Task ApplyGag(CallbackGagDataDto gagData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received ApplyGag instruction from server!", LoggerType.Callbacks);
        var changes = _gags.ApplyGag(gagData.AffectedLayer, gagData.NewData.GagItem, gagData.Enactor.UID, out var visualGagItem);

        PostActionMsg(gagData.Enactor.UID, InteractionType.ApplyGag, gagData.NewData.GagItem + " was applied on layer " + gagData.AffectedLayer);

        if (visualGagItem is null || changes == VisualUpdateFlags.None)
            return;

        if (changes.HasFlag(VisualUpdateFlags.Glamour))    await TryAddGlamour(visualGagItem.Glamour, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Mod))              TryAddOrUpdateMod(visualGagItem.Mod, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Helmet))     await TryUpdateMetaState(MetaIndex.HatState, visualGagItem.HeadgearState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Visor))      await TryUpdateMetaState(MetaIndex.VisorState, visualGagItem.VisorState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Moodle))           TryAddMoodle(visualGagItem.Moodle, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.CustomizeProfile)) TrySetProfile(visualGagItem.ProfileGuid, visualGagItem.ProfilePriority, ManagerPriority.Gags);

        // Check Traits.
        // UpdateAppliedTraits(visualGagItem.Traits, gagData.Enactor.UID, ManagerPriority.Gags);
    }

    /// <summary> Locks the gag with a padlock on a specified layer. </summary>
    public void LockGag(CallbackGagDataDto gagData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received LockGag instruction from server!", LoggerType.Callbacks);
        _gags.LockGag(gagData.AffectedLayer, gagData.NewData.Padlock, gagData.NewData.Password, gagData.NewData.Timer, gagData.Enactor.UID);

        PostActionMsg(gagData.Enactor.UID, InteractionType.LockGag, gagData.NewData.Padlock + " was applied on layer " + gagData.AffectedLayer + "'s Gag");
    }

    /// <summary> Unlocks the gag's padlock on a spesified layer. </summary>
    public void UnlockGag(CallbackGagDataDto gagData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received UnlockGag instruction from server!", LoggerType.Callbacks);
        _gags.UnlockGag(gagData.AffectedLayer, gagData.Enactor.UID);

        PostActionMsg(gagData.Enactor.UID, InteractionType.UnlockGag, gagData.PreviousPadlock + " was removed from layer " + gagData.AffectedLayer + "'s Gag");
    }

    /// <summary> Removes the gag from a defined layer. </summary>
    public async Task RemoveGag(CallbackGagDataDto gagData, bool updateVisuals = true, bool doActionNotif = true)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received RemoveGag instruction from server!", LoggerType.Callbacks);
        var changes = _gags.RemoveGag(gagData.AffectedLayer, gagData.Enactor.UID, out var removedItemData);

        if (doActionNotif)
            PostActionMsg(gagData.Enactor.UID, InteractionType.RemoveGag, gagData.PreviousGag.GagName() + " was removed on layer " + gagData.AffectedLayer);

        if (removedItemData is null)
            return;

        if (changes.HasFlag(VisualUpdateFlags.Glamour))    await TryUpdateorRemoveGlamour(removedItemData.Glamour);
        if (changes.HasFlag(VisualUpdateFlags.Mod))              TryUpdateOrRemoveMod(removedItemData.Mod);
        if (changes.HasFlag(VisualUpdateFlags.Helmet))     await ClearMetaState(MetaIndex.HatState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Visor))      await ClearMetaState(MetaIndex.VisorState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Weapon))     await ClearMetaState(MetaIndex.WeaponState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Moodle))           TryRemoveMoodle(removedItemData.Moodle);
        if (changes.HasFlag(VisualUpdateFlags.CustomizeProfile)) TryClearProfile(removedItemData.ProfileGuid);

        // Handle trait removal.
    }

    #endregion Gag Manipulation

    #region Restrictions Manipulation

    public async Task SwapOrApplyRestriction(CallbackRestrictionDataDto itemData)
    {
        if (itemData.PreviousRestriction.IsEmptyGuid())
            await ApplyRestriction(itemData);
        else
            await SwapRestriction(itemData);
    }

    public async Task SwapRestriction(CallbackRestrictionDataDto itemData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Callbacks);
        await RemoveRestriction(itemData, false);
        await ApplyRestriction(itemData);
        PostActionMsg(itemData.Enactor.UID, InteractionType.SwappedRestriction, itemData.PreviousRestriction + " swapped to " + 
            itemData.NewData.Identifier + " on layer " + itemData.AffectedLayer);
    }

    /// <summary> Applies a Restriction to the client at a defined index. </summary>
    public async Task ApplyRestriction(CallbackRestrictionDataDto itemData)
    {
        if (!MainHub.IsConnected)
            return;

        Logger.LogTrace("Received ApplyRestriction instruction from server!", LoggerType.Callbacks);
        var changes = _restrictions.ApplyRestriction(itemData.AffectedLayer, itemData.NewData.Identifier, itemData.Enactor.UID, out var visualItem);

        PostActionMsg(itemData.Enactor.UID, InteractionType.ApplyRestriction, "A Restriction item was applied to you!");

        if (visualItem is null || changes is VisualUpdateFlags.None)
            return;

        if (changes.HasFlag(VisualUpdateFlags.Glamour)) await TryAddGlamour(visualItem.Glamour, ManagerPriority.Restrictions);
        if (changes.HasFlag(VisualUpdateFlags.Mod))           TryAddOrUpdateMod(visualItem.Mod, ManagerPriority.Restrictions);
        if (changes.HasFlag(VisualUpdateFlags.Moodle))        TryAddMoodle(visualItem.Moodle, ManagerPriority.Restrictions);

        // Check Traits.
    }

    /// <summary> Locks a padlock from a restriction at a defined index. </summary>
    public void LockRestriction(CallbackRestrictionDataDto itemData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received LockRestriction instruction from server!", LoggerType.Callbacks);
        _restrictions.LockRestriction(itemData.AffectedLayer, itemData.NewData.Padlock, itemData.NewData.Password, itemData.NewData.Timer, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.LockRestriction, itemData.NewData.Padlock + " was applied to the restriction in layer " + itemData.AffectedLayer);
    }

    /// <summary> Unlocks a padlock from a restriction at a defined index. </summary>
    public void UnlockRestriction(CallbackRestrictionDataDto itemData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received UnlockRestriction instruction from server!", LoggerType.Callbacks);
        _restrictions.UnlockRestriction(itemData.AffectedLayer, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.UnlockRestriction, itemData.PreviousPadlock + " was removed from layer " + itemData.AffectedLayer + "'s Restriction");
    }

    /// <summary> Removes a restraint item from a defined index. </summary>
    public async Task RemoveRestriction(CallbackRestrictionDataDto itemData, bool updateVisuals = true)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received RemoveRestriction instruction from server!", LoggerType.Callbacks);
        var changes = _restrictions.RemoveRestriction(itemData.AffectedLayer, itemData.Enactor.UID, out var removedItemData);
        PostActionMsg(itemData.Enactor.UID, InteractionType.RemoveRestriction, "A Restriction item was removed from you!");

        if (removedItemData is null || changes == VisualUpdateFlags.None || !updateVisuals)
            return;

        if (changes.HasFlag(VisualUpdateFlags.Glamour)) await TryUpdateorRemoveGlamour(removedItemData.Glamour);
        if (changes.HasFlag(VisualUpdateFlags.Mod))           TryUpdateOrRemoveMod(removedItemData.Mod);
        if (changes.HasFlag(VisualUpdateFlags.Moodle))        TryRemoveMoodle(removedItemData.Moodle);
    }

    #endregion Restrictions Manipulation

    #region RestraintSet Manipulation
    public async Task SwapOrApplyRestraint(CallbackRestraintDataDto restraintData)
    {
        if (restraintData.PreviousRestraint.IsEmptyGuid())
            await ApplyRestraint(restraintData);
        else
            await SwapRestraint(restraintData);
    }

    /// <summary> Swapped a Restraint set to the client. </summary>
    public async Task SwapRestraint(CallbackRestraintDataDto restraintData)
    {
        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Callbacks);
        await RemoveRestraint(restraintData, false);
        await ApplyRestraint(restraintData);
        PostActionMsg(restraintData.Enactor.UID, InteractionType.SwappedRestraint, restraintData.PreviousRestraint + " swapped to " + restraintData.NewData.Identifier);
    }

/// <summary> Applies a Restraint set to the client. </summary>
    public async Task ApplyRestraint(CallbackRestraintDataDto restraintData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received ApplyRestraint instruction from server!", LoggerType.Callbacks);
        var changes = _restraints.ApplyRestraint(restraintData.NewData.Identifier, restraintData.Enactor.UID, out var visualItem);
            PostActionMsg(restraintData.Enactor.UID, InteractionType.ApplyRestraint, restraintData.NewData.Identifier + " was applied to you!");

        if (visualItem is null || changes == VisualUpdateFlags.None)
            return;

        // Handle Visual Changes through a series of tasks to run in parallel. (Things calling the same appliers must be run together)
        if (changes.HasFlag(VisualUpdateFlags.Glamour)) await TryAddGlamour(visualItem.GetGlamour(), ManagerPriority.Restraints);
        if (changes.HasFlag(VisualUpdateFlags.Mod))           TryAddOrUpdateMod(visualItem.GetMods(), ManagerPriority.Restraints);
        if (changes.HasFlag(VisualUpdateFlags.Helmet))  await TryUpdateMetaState(MetaIndex.HatState, visualItem.HeadgearState, ManagerPriority.Restraints);
        if (changes.HasFlag(VisualUpdateFlags.Visor))   await TryUpdateMetaState(MetaIndex.VisorState, visualItem.VisorState, ManagerPriority.Restraints);
        if (changes.HasFlag(VisualUpdateFlags.Weapon))  await TryUpdateMetaState(MetaIndex.WeaponState, visualItem.WeaponState, ManagerPriority.Restraints);
        if (changes.HasFlag(VisualUpdateFlags.Moodle))        TryAddMoodle(visualItem.GetMoodles(), ManagerPriority.Restraints);

        // Check Traits.
    }

    /// <summary> Locks the active restraint set. </summary>
    public void LockRestraint(CallbackRestraintDataDto restraintData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received LockRestraint instruction from server!", LoggerType.Callbacks);
        _restraints.LockRestraint(restraintData.NewData.Identifier, restraintData.NewData.Padlock, restraintData.NewData.Password, restraintData.NewData.Timer, restraintData.Enactor.UID);
        PostActionMsg(restraintData.Enactor.UID, InteractionType.LockRestraint, restraintData.NewData.Padlock + " was applied to the restraint");
    }

    /// <summary> Unlocks the active restraint set </summary>
    public void UnlockRestraint(CallbackRestraintDataDto restraintData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received UnlockRestraint instruction from server!", LoggerType.Callbacks);
        _restraints.UnlockRestraint(restraintData.NewData.Identifier, restraintData.Enactor.UID);
        PostActionMsg(restraintData.Enactor.UID, InteractionType.UnlockRestraint, restraintData.PreviousPadlock + " was removed from the restraint");
    }

    /// <summary> Removes the active restraint. </summary>
    public async Task RemoveRestraint(CallbackRestraintDataDto restraintData, bool updateVisuals = true)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received RemoveRestraint instruction from server!", LoggerType.Callbacks);
        var changes = _restraints.RemoveRestraint(restraintData.Enactor.UID, out var removedItemData);
        PostActionMsg(restraintData.Enactor.UID, InteractionType.RemoveRestraint, "A Restriction item was removed from you!");

        if (removedItemData is null || changes == VisualUpdateFlags.None)
            return;

        if (changes.HasFlag(VisualUpdateFlags.Glamour)) await TryUpdateorRemoveGlamour(removedItemData.GetGlamour());
        if (changes.HasFlag(VisualUpdateFlags.Mod))           TryUpdateOrRemoveMod(removedItemData.GetMods());
        if (changes.HasFlag(VisualUpdateFlags.Helmet))  await ClearMetaState(MetaIndex.HatState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Visor))   await ClearMetaState(MetaIndex.VisorState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Weapon))  await ClearMetaState(MetaIndex.WeaponState, ManagerPriority.Gags);
        if (changes.HasFlag(VisualUpdateFlags.Moodle))        TryRemoveMoodle(removedItemData.GetMoodles());
    }

    #endregion RestraintSet Manipulation

    #region Direct Moodle Calls
    public void ApplyStatusesByGuid(ApplyMoodlesByGuidDto dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ApplyOwnMoodle, "Moodle Status(s) Applied"))
            _interop.Moodles.ApplyOwnStatusByGUID(dto.Statuses);
    }

    public void ApplyStatusesToSelf(ApplyMoodlesByStatusDto dto, string clientPlayerNameWithWorld)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ApplyPairMoodle, "Pair's Moodle Status(s) Applied"))
            _interop.Moodles.ApplyStatusesFromPairToSelf(dto.User.UID, clientPlayerNameWithWorld, dto.Statuses);
    }

    public void RemoveStatusesFromSelf(RemoveMoodlesDto dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.RemoveMoodle, "Moodle Status Removed"))
            _interop.Moodles.RemoveOwnStatusByGuid(dto.Statuses);
    }

    public void ClearStatusesFromSelf(UserDto dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ClearMoodle, "Moodles Cleared"))
            _interop.Moodles.ClearStatus();
    }
    #endregion Direct Moodle Calls


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task ApplyFullUpdate()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        // Apply a full update of application for all current caches as they are in their current state.

        // not yet known how atm.
        Logger.LogInformation("Full Update Applied!");
    }


    #region Applier Callers
    private async Task TryAddGlamour(GlamourSlot item, ManagerPriority source)
    {
        if(ManagerCacheRef.Where(entry => entry.Key > source).Any(x => x.Value.Glamour.Any(y => y.Value.Slot == item.Slot)))
        {
            Logger.LogTrace("Glamour was not applied due to higher priority glamour already in use.", LoggerType.ClientPlayerData);
            return;
        }

        await _glamourApplier.UpdateActiveSlot(item);
    }
    
    private async Task TryAddGlamour(IEnumerable<GlamourSlot> items,  ManagerPriority source)
    {
        var newSlots = items // Limit the application of new slots
            .Where(kvp => !ManagerCacheRef // To any managers,
                .Where(kv => kv.Key > source) // With a higher Priority than the source,
                .Any(entry => entry.Value.Glamour.ContainsKey(kvp.Slot)));

        if(newSlots.Any())
        {
            Logger.LogTrace("Glamour was not applied due to higher priority glamour already in use.", LoggerType.ClientPlayerData);
            return;
        }

        await _glamourApplier.UpdateActiveSlot(newSlots);
    }

    private async Task TryUpdateorRemoveGlamour(GlamourSlot item) => await TryUpdateorRemoveGlamour(new[] { item });

    /// <summary> A Glamour Slot was removed and we need to perform a recalculation to account for any minor changes. </summary>
    /// <remarks> For now, this will recalculate all slots and reapply all slots. </remarks>
    private async Task TryUpdateorRemoveGlamour(IEnumerable<GlamourSlot> items)
    {
        // Build a dictionary of the latest active GlamourSlot items from the caches. Take the last for values. (highest priority)
        var newRestrictionSlots = ManagerCacheRef
            .SelectMany(kv => kv.Value.Glamour)
            .GroupBy(entry => entry.Key)
            .ToDictionary(group => group.Key, group => group.Last().Value);

        // for each item that we request to removed, see if the slot still exists in the newRestrictionSlots. If it does, update it. Otherwise, remove it.
/*        foreach (var item in items)
        {
            if (newRestrictionSlots.ContainsKey(item.Slot))
                await _glamourApplier.UpdateActiveSlot(newRestrictionSlots[item.Slot]);
            else
                await _glamourApplier.RemoveActiveSlot(item.Slot);
        }*/

        // This may be a better solution for now to avoid glamour state change updates messing up anywhere.
        // Try the above instead once everything works to see if it handles things better.
        await _glamourApplier.UpdateAllActiveSlots(newRestrictionSlots);
        Logger.LogDebug("Glamour Restrictions Updated!", LoggerType.ClientPlayerData);
    }

    /// <summary> Will either add a new mod, or update it to the newly preferred settings. </summary>
    /// <remarks> Update will be blocked if another ModAssociation with the same ModInfo on a higher manager is found. </remarks>
    private void TryAddOrUpdateMod(ModAssociation mod, ManagerPriority priority) => TryAddOrUpdateMod(new[] { mod }, priority);

    /// <summary> Will either add a new mod, or update it to the newly preferred settings. </summary>
    /// <remarks> Update will be blocked if another ModAssociation with the same ModInfo on a higher manager is found. </remarks>
    private void TryAddOrUpdateMod(IEnumerable<ModAssociation> mods, ManagerPriority priority)
    {
        var higherPriorityMods = new HashSet<ModAssociation>(ManagerCacheRef.Where(entry => entry.Key > priority).SelectMany(entry => entry.Value.Mods));
        if(mods.Except(higherPriorityMods) is { } newMods)
        {
            foreach (var mod in newMods)
            {
                // grab their respective mod settings and apply them.
                var settings = _modSettingPresets.GetSettingPreset(mod.ModInfo.DirectoryName, mod.CustomSettings);
                _modApplier.SetOrUpdateTempMod(mod.ModInfo, settings);
            }
            return;
        }
        Logger.LogTrace("Mod was not applied due to higher priority mod already in use.", LoggerType.ClientPlayerData);
    }

    /// <summary> Will either remove a mod, or update it to the newly preferred settings. </summary>
    /// <remarks> A mod will be have its newly updated settings applied if the same mod with different settings is found. </remarks>
    private void TryUpdateOrRemoveMod(ModAssociation mod) => TryUpdateOrRemoveMod(new[] { mod });

    /// <summary> Will either remove a mod, or update it to the newly preferred settings. </summary>
    /// <remarks> A mod will be have its newly updated settings applied if the same mod with different settings is found. </remarks>
    private void TryUpdateOrRemoveMod(IEnumerable<ModAssociation> mods)
    {
        // Build a dictionary of the last active mod for each ModInfo
        var activeModsByInfo = ManagerCacheRef.SelectMany(kv => kv.Value.Mods);
        var activeLowerPrioMods = activeModsByInfo.Intersect(mods);

        // Update still existing.
        foreach (var lowerPrioMod in activeLowerPrioMods)
        {
            var settings = _modSettingPresets.GetSettingPreset(lowerPrioMod.ModInfo.DirectoryName, lowerPrioMod.CustomSettings);
            _modApplier.SetOrUpdateTempMod(lowerPrioMod.ModInfo, settings);
        }

        // Remove remaining.
        _modApplier.RemoveTempMod(mods.Except(activeLowerPrioMods));
    }


    /// <summary> Passes in the MetaDataStruct after a change in application. Any Non-Null values are new changed made by this item. </summary>
    /// <remarks> Additional States from the same manager would already be set and do not need to be considered. </remarks>
    private async Task TryUpdateMetaState(MetaIndex id, OptionalBool state, ManagerPriority source)
    {
        if (source is ManagerPriority.Restraints && ManagerCacheRef[ManagerPriority.Gags] is VisualAdvancedRestrictionsCache cache)
        {
            var canSet = id switch
            {
                MetaIndex.HatState => !cache.Headgear.HasValue,
                MetaIndex.VisorState => !cache.Visor.HasValue,
                MetaIndex.WeaponState => !cache.Weapon.HasValue,
                _ => false
            };
            if (!canSet)
            {
                Logger.LogTrace("MetaState was not applied due to higher priority meta already in use.", LoggerType.ClientPlayerData);
                return;
            }
        }
        // try and update the state.
        await _glamourApplier.UpdateMetaState(id, state);
    }

    // Might be wise to check if anything else has another state as well.
    private async Task ClearMetaState(MetaIndex id, ManagerPriority source)
    {
        if (source is ManagerPriority.Restraints && ManagerCacheRef[ManagerPriority.Gags] is VisualAdvancedRestrictionsCache cache)
        {
            var blockClear = id switch
            {
                MetaIndex.HatState => cache.Headgear.HasValue,
                MetaIndex.VisorState => cache.Visor.HasValue,
                MetaIndex.WeaponState => cache.Weapon.HasValue,
                _ => false
            };
            if (blockClear)
            {
                Logger.LogTrace("MetaState was not removed due to higher priority meta already in use.", LoggerType.ClientPlayerData);
                return;
            }
        }
        // try and clear the state.
        await _glamourApplier.UpdateMetaState(id, OptionalBool.Null);
    }

    private void TryAddMoodle(Moodle moodle, ManagerPriority source) => TryAddMoodle(new[] { moodle }, source);
    private void TryAddMoodle(IEnumerable<Moodle> moodles, ManagerPriority source)
    {
        // MAINTAINERS NOTE: (TODO)
        // For now just directly instruct the removal if the Caches dont contain it.
        // This is going to cause issues with statuses -vs- statuses of presets, but we can cross that bridge when it becomes an issue.
        var higherPriorityMoodles = new HashSet<Moodle>(ManagerCacheRef.Where(entry => entry.Key > source).SelectMany(entry => entry.Value.Moodles));
        if(moodles.Where(moodle => !higherPriorityMoodles.Contains(moodle)) is { } newMoodles)
        {
            _moodleApplier.AddRestrictedMoodle(newMoodles);
            return;
        }
        Logger.LogTrace("Moodle was not applied due to higher priority moodle already in use.", LoggerType.ClientPlayerData);
    }

    /* This is how Moodles Preset -vs- Statuses were handled in the past by updates. Maybe it can be fine tuned into integration here later.
        if (_playerData.LastIpcData is not null)
        {
            if (data.AssociatedMoodles.Count > 0) ExpectedMoodles.UnionWith(data.AssociatedMoodles);

            if (data.AssociatedMoodlePreset != Guid.Empty)
            {
                var statuses = _playerData.LastIpcData.MoodlesPresets.FirstOrDefault(p => p.Item1 == data.AssociatedMoodlePreset).Item2;
                if (statuses is not null) ExpectedMoodles.UnionWith(statuses);
            }
        }
     */

    private void TryRemoveMoodle(Moodle moodle) => TryRemoveMoodle(new[] { moodle });

    private void TryRemoveMoodle(IEnumerable<Moodle> moodles)
    {
        // Build a dictionary of the last active mod for each ModInfo
        var activeMoodles = ManagerCacheRef.SelectMany(kv => kv.Value.Moodles).ToHashSet();
        var moodlesToRemove = moodles.Except(activeMoodles);
        _moodleApplier.RemoveRestrictedMoodle(moodlesToRemove);
    }

    private void TrySetProfile(Guid profileGuid, uint profilePriority, ManagerPriority source)
    {
        if(source is ManagerPriority.Restraints && ManagerCacheRef[ManagerPriority.Gags] is VisualAdvancedRestrictionsCache cache)
            if(cache.CustomizeProfile.Priority > profilePriority)
            {
                Logger.LogTrace("Customize Profile was not applied due to higher priority profile already in use.", LoggerType.ClientPlayerData);
                return;
            }
        // Otherwise, allow it.
        _customizeApplier.SetOrUpdateProfile(profileGuid, (int)profilePriority);
    }

    private void TryClearProfile(Guid profileGuid)
    {
        // we are attempting to remove this current profile GUID. Before we do, we should see if any others should replace it.
        if (ManagerCacheRef[ManagerPriority.Gags] is VisualAdvancedRestrictionsCache cacheGags && !cacheGags.CustomizeProfile.Profile.IsEmptyGuid())
        {
            Logger.LogTrace("Upon removing Customize+ Profile, we found another profile that should be active. Switching!", LoggerType.ClientPlayerData);
            _customizeApplier.SetOrUpdateProfile(cacheGags.CustomizeProfile.Profile, (int)cacheGags.CustomizeProfile.Priority);
        }
        else if (ManagerCacheRef[ManagerPriority.Restraints] is VisualAdvancedRestrictionsCache cacheRestraints && !cacheRestraints.CustomizeProfile.Profile.IsEmptyGuid())
        {
            Logger.LogTrace("Upon removing Customize+ Profile, we found another profile that should be active. Switching!", LoggerType.ClientPlayerData);
            _customizeApplier.SetOrUpdateProfile(cacheRestraints.CustomizeProfile.Profile, (int)cacheRestraints.CustomizeProfile.Priority);
        }
        // otherwise, clear it.
        _customizeApplier.ClearRestrictedProfile();
    }
    #endregion Applier Callers
}
