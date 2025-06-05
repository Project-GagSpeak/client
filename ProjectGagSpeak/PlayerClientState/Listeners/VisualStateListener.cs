using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Components;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
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
    private readonly GagspeakConfigService  _mainConfig;
    private readonly PairManager            _pairs;
    private readonly IpcManager             _interop;
    private readonly RestraintManager       _restraints;
    private readonly RestrictionManager     _restrictions;
    private readonly GagRestrictionManager  _gags;
    private readonly CursedLootManager      _cursedLoot;
    private readonly CacheStateManager      _cacheManager;
    private readonly VisualApplierGlamour   _glamourApplier;
    private readonly VisualApplierPenumbra  _modApplier;
    private readonly VisualApplierMoodles   _moodleApplier;
    private readonly VisualApplierCPlus     _customizeApplier;
    private readonly ClientMonitor          _clientMonitor;
    private readonly OnFrameworkService     _frameworkUtils;

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
        CacheStateManager cacheManager,
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
        _cacheManager = cacheManager;
        _clientMonitor = clientMonitor;
        _frameworkUtils = frameworkUtils;

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

    public async Task SyncServerData(ConnectionResponse connectionDto)
    {
        // Reload the applied data.
        _gags.LoadServerData(connectionDto.SyncedGagData);
        _restrictions.LoadServerData(connectionDto.SyncedRestrictionsData);
        _restraints.LoadServerData(connectionDto.SyncedRestraintSetData);

        // i dont know how the fuck to do this cancerous stuff rn.
        await _cacheManager.ReapplyFinalState();
    }


    #region Gag Manipulation
    public async Task SwapOrApplyGag(KinksterUpdateGagSlot gagData)
    {
        if(gagData.PreviousGag is GagType.None)
            await ApplyGag(gagData);
        else
            await SwapGag(gagData);
    }

    /// <summary> We have received the instruction to swap our gag as a callback from the server. </summary>
    /// <remarks> We can skip all validation checks for this, but don't update if not connected. </remarks>
    public async Task SwapGag(KinksterUpdateGagSlot gagData)
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
    public async Task ApplyGag(KinksterUpdateGagSlot gagData)
    {
        if (!MainHub.IsConnected)
            return;

        Logger.LogTrace("Received ApplyGag instruction from server!", LoggerType.Callbacks);
        if(_gags.ApplyGag(gagData.AffectedLayer, gagData.NewData.GagItem, gagData.Enactor.UID, out var gagItem))
            await _cacheManager.TryApply(gagItem, gagData.AffectedLayer, gagData.Enactor.UID);

        PostActionMsg(gagData.Enactor.UID, InteractionType.ApplyGag, gagData.NewData.GagItem + " was applied on layer " + gagData.AffectedLayer);
    }

    /// <summary> Locks the gag with a padlock on a specified layer. </summary>
    public void LockGag(KinksterUpdateGagSlot gagData)
    {
        if (!MainHub.IsConnected)
            return;

        Logger.LogTrace("Received LockGag instruction from server!", LoggerType.Callbacks);
        _gags.LockGag(gagData.AffectedLayer, gagData.NewData.Padlock, gagData.NewData.Password, gagData.NewData.Timer, gagData.Enactor.UID);
        PostActionMsg(gagData.Enactor.UID, InteractionType.LockGag, gagData.NewData.Padlock + " was applied on layer " + gagData.AffectedLayer + "'s Gag");
    }

    /// <summary> Unlocks the gag's padlock on a spesified layer. </summary>
    public void UnlockGag(KinksterUpdateGagSlot gagData)
    {
        if (!MainHub.IsConnected)
            return;

        Logger.LogTrace("Received UnlockGag instruction from server!", LoggerType.Callbacks);
        _gags.UnlockGag(gagData.AffectedLayer, gagData.Enactor.UID);
        PostActionMsg(gagData.Enactor.UID, InteractionType.UnlockGag, gagData.PreviousPadlock + " was removed from layer " + gagData.AffectedLayer + "'s Gag");
    }

    /// <summary> Removes the gag from a defined layer. </summary>
    public async Task RemoveGag(KinksterUpdateGagSlot gagData, bool updateVisuals = true, bool doActionNotif = true)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received RemoveGag instruction from server!", LoggerType.Callbacks);
        if(_gags.RemoveGag(gagData.AffectedLayer, gagData.Enactor.UID, out var removedGagItem))
            await _cacheManager.TryRemove(removedGagItem, gagData.AffectedLayer, gagData.Enactor.UID);

        if (doActionNotif)
            PostActionMsg(gagData.Enactor.UID, InteractionType.RemoveGag, gagData.PreviousGag.GagName() + " was removed on layer " + gagData.AffectedLayer);
    }

    #endregion Gag Manipulation

    #region Restrictions Manipulation

    public async Task SwapOrApplyRestriction(KinksterUpdateRestriction itemData)
    {
        if (itemData.PreviousRestriction.IsEmptyGuid())
            await ApplyRestriction(itemData);
        else
            await SwapRestriction(itemData);
    }

    public async Task SwapRestriction(KinksterUpdateRestriction itemData)
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
    public async Task ApplyRestriction(KinksterUpdateRestriction itemData)
    {
        if (!MainHub.IsConnected)
            return;

        Logger.LogTrace("Received ApplyRestriction instruction from server!", LoggerType.Callbacks);
        if(_restrictions.ApplyRestriction(itemData.AffectedLayer, itemData.NewData.Identifier, itemData.Enactor.UID, out var visualItem))
            await _cacheManager.TryApply(visualItem, itemData.AffectedLayer, itemData.Enactor.UID);

        PostActionMsg(itemData.Enactor.UID, InteractionType.ApplyRestriction, "A Restriction item was applied to you!");
    }

    /// <summary> Locks a padlock from a restriction at a defined index. </summary>
    public void LockRestriction(KinksterUpdateRestriction itemData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received LockRestriction instruction from server!", LoggerType.Callbacks);
        _restrictions.LockRestriction(itemData.AffectedLayer, itemData.NewData.Padlock, itemData.NewData.Password, itemData.NewData.Timer, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.LockRestriction, itemData.NewData.Padlock + " was applied to the restriction in layer " + itemData.AffectedLayer);
    }

    /// <summary> Unlocks a padlock from a restriction at a defined index. </summary>
    public void UnlockRestriction(KinksterUpdateRestriction itemData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received UnlockRestriction instruction from server!", LoggerType.Callbacks);
        _restrictions.UnlockRestriction(itemData.AffectedLayer, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.UnlockRestriction, itemData.PreviousPadlock + " was removed from layer " + itemData.AffectedLayer + "'s Restriction");
    }

    /// <summary> Removes a restraint item from a defined index. </summary>
    public async Task RemoveRestriction(KinksterUpdateRestriction itemData, bool updateVisuals = true)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received RemoveRestriction instruction from server!", LoggerType.Callbacks);
        if(_restrictions.RemoveRestriction(itemData.AffectedLayer, itemData.Enactor.UID, out var removedItemData))
            await _cacheManager.TryRemove(removedItemData, itemData.AffectedLayer, itemData.Enactor.UID);

        PostActionMsg(itemData.Enactor.UID, InteractionType.RemoveRestriction, "A Restriction item was removed from you!");
    }

    #endregion Restrictions Manipulation

    #region RestraintSet Manipulation
    public async Task SwapOrApplyRestraint(KinksterUpdateRestraint restraintData)
    {
        if (restraintData.PreviousRestraint.IsEmptyGuid())
            await ApplyRestraint(restraintData);
        else
            await SwapRestraint(restraintData);
    }

    /// <summary> Swapped a Restraint set to the client. </summary>
    public async Task SwapRestraint(KinksterUpdateRestraint restraintData)
    {
        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Callbacks);
        await RemoveRestraint(restraintData, false);
        await ApplyRestraint(restraintData);
        PostActionMsg(restraintData.Enactor.UID, InteractionType.SwappedRestraint, restraintData.PreviousRestraint + " swapped to " + restraintData.NewData.Identifier);
    }

    /// <summary> Applies a Restraint set to the client. </summary>
    public async Task ApplyRestraint(KinksterUpdateRestraint restraintData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received ApplyRestraint instruction from server!", LoggerType.Callbacks);
        if(_restraints.ApplyRestraint(restraintData.NewData.Identifier, restraintData.Enactor.UID, out var restraintSet))
            await _cacheManager.TryApply(restraintSet, restraintData.Enactor.UID);

        PostActionMsg(restraintData.Enactor.UID, InteractionType.ApplyRestraint, restraintData.NewData.Identifier + " was applied to you!");
    }

    /// <summary> Locks the active restraint set. </summary>
    public void LockRestraint(KinksterUpdateRestraint restraintData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received LockRestraint instruction from server!", LoggerType.Callbacks);
        _restraints.LockRestraint(restraintData.NewData.Identifier, restraintData.NewData.Padlock, restraintData.NewData.Password, restraintData.NewData.Timer, restraintData.Enactor.UID);
        PostActionMsg(restraintData.Enactor.UID, InteractionType.LockRestraint, restraintData.NewData.Padlock + " was applied to the restraint");
    }

    /// <summary> Unlocks the active restraint set </summary>
    public void UnlockRestraint(KinksterUpdateRestraint restraintData)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received UnlockRestraint instruction from server!", LoggerType.Callbacks);
        _restraints.UnlockRestraint(restraintData.NewData.Identifier, restraintData.Enactor.UID);
        PostActionMsg(restraintData.Enactor.UID, InteractionType.UnlockRestraint, restraintData.PreviousPadlock + " was removed from the restraint");
    }

    /// <summary> Removes the active restraint. </summary>
    public async Task RemoveRestraint(KinksterUpdateRestraint restraintData, bool updateVisuals = true)
    {
        if (!MainHub.IsConnected)
            return;
        Logger.LogTrace("Received RemoveRestraint instruction from server!", LoggerType.Callbacks);
        if(_restraints.RemoveRestraint(restraintData.Enactor.UID, out var restraintSet))
            await _cacheManager.TryRemove(restraintSet, restraintData.Enactor.UID);

        PostActionMsg(restraintData.Enactor.UID, InteractionType.RemoveRestraint, "A Restriction item was removed from you!");
    }

    #endregion RestraintSet Manipulation

    public void ApplyStatusesByGuid(MoodlesApplierById dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ApplyOwnMoodle, "Moodle Status(s) Applied"))
            _interop.Moodles.ApplyOwnStatusByGUID(dto.Ids);
    }

    public void ApplyStatusesToSelf(MoodlesApplierByStatus dto, string clientPlayerNameWithWorld)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ApplyPairMoodle, "Pair's Moodle Status(s) Applied"))
            _interop.Moodles.ApplyStatusesFromPairToSelf(dto.User.UID, clientPlayerNameWithWorld, dto.Statuses);
    }

    public void RemoveStatusesFromSelf(MoodlesRemoval dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.RemoveMoodle, "Moodle Status Removed"))
            _interop.Moodles.RemoveOwnStatusByGuid(dto.StatusIds);
    }

    public void ClearStatusesFromSelf(KinksterBase dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ClearMoodle, "Moodles Cleared"))
            _interop.Moodles.ClearStatus();
    }
}
