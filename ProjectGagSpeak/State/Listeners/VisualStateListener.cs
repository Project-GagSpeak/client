using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using Penumbra.GameData.Interop;
using System.Windows.Forms;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Handles all incoming events from the GagSpeakHub, and potentially other sources
///     that intend to update server-synced playerData, and updates their visual caches.
/// </summary>
public sealed class VisualStateListener : DisposableMediatorSubscriberBase
{
    private readonly IpcManager             _interop;
    private readonly KinksterManager        _pairs;
    private readonly RestraintManager       _restraints;
    private readonly RestrictionManager     _restrictions;
    private readonly GagRestrictionManager  _gags;
    private readonly CursedLootManager      _cursedLoot;
    private readonly CacheStateManager      _cacheManager;

    public VisualStateListener(
        ILogger<VisualStateListener> logger,
        GagspeakMediator mediator,
        MainConfig config,
        IpcManager interop,
        KinksterManager pairs,
        RestraintManager restraints,
        RestrictionManager restrictions,
        GagRestrictionManager gags,
        CursedLootManager cursedLoot,
        CacheStateManager cacheManager,
        OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _interop = interop;
        _pairs = pairs;
        _restraints = restraints;
        _restrictions = restrictions;
        _gags = gags;
        _cursedLoot = cursedLoot;
        _cacheManager = cacheManager;
    }

    private bool PostActionMsg(string enactor, InteractionType type, string message)
    {
        var isPair = _pairs.TryGetNickAliasOrUid(enactor, out var nick);
        if (isPair) Mediator.Publish(new EventMessage(new(nick!, enactor, type, message)));
        return isPair;
    }

    // Re-Route directly to the CacheStateManager.
    public async Task SyncServerData(ConnectionResponse connectionDto)
        => await _cacheManager.SyncWithServerData(connectionDto);


    #region Gag Manipulation
    public async Task SwapOrApplyGag(KinksterUpdateActiveGag gagData)
    {
        if(gagData.PreviousGag is GagType.None)
            await ApplyGag(gagData);
        else
            await SwapGag(gagData);
    }

    /// <summary> We have received the instruction to swap our gag as a callback from the server. </summary>
    /// <remarks> We can skip all validation checks for this, but don't update if not connected. </remarks>
    public async Task SwapGag(KinksterUpdateActiveGag gagData)
    {
        if (!MainHub.IsConnectionDataSynced) 
            return;
        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Gags);
        await RemoveGag(gagData, false);
        await ApplyGag(gagData);

        PostActionMsg(gagData.Enactor.UID, InteractionType.SwappedGag, gagData.PreviousGag.GagName() + " swapped to " + gagData.NewData.GagItem + " on layer " + gagData.AffectedLayer);
    }

    /// <summary> Applies a Gag to a spesified layer. </summary>
    public async Task ApplyGag(KinksterUpdateActiveGag gagData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received ApplyGag instruction from server!", LoggerType.Gags);
        if (_gags.ApplyGag(gagData.AffectedLayer, gagData.NewData.GagItem, gagData.Enactor.UID, out var gagItem))
            await _cacheManager.AddGagItem(gagItem, gagData.AffectedLayer, gagData.Enactor.UID);

        PostActionMsg(gagData.Enactor.UID, InteractionType.ApplyGag, gagData.NewData.GagItem + " was applied on layer " + gagData.AffectedLayer);
    }

    /// <summary> Locks the gag with a padlock on a specified layer. </summary>
    public void LockGag(KinksterUpdateActiveGag gagData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received LockGag instruction from server!", LoggerType.Gags);
        _gags.LockGag(gagData.AffectedLayer, gagData.NewData.Padlock, gagData.NewData.Password, gagData.NewData.Timer, gagData.Enactor.UID);
        PostActionMsg(gagData.Enactor.UID, InteractionType.LockGag, $"{gagData.NewData.Padlock.ToName()} was set on layer {gagData.AffectedLayer}'s Gag");
    }

    /// <summary> Unlocks the gag's padlock on a spesified layer. </summary>
    public void UnlockGag(KinksterUpdateActiveGag gagData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received UnlockGag instruction from server!", LoggerType.Gags);
        _gags.UnlockGag(gagData.AffectedLayer, gagData.Enactor.UID);
        PostActionMsg(gagData.Enactor.UID, InteractionType.UnlockGag, $"{gagData.PreviousPadlock.ToName()} was removed from layer {gagData.AffectedLayer}'s Gag");
    }

    /// <summary> Removes the gag from a defined layer. </summary>
    public async Task RemoveGag(KinksterUpdateActiveGag gagData, bool updateVisuals = true)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received RemoveGag instruction from server!", LoggerType.Gags);
        if(_gags.RemoveGag(gagData.AffectedLayer, gagData.Enactor.UID, out var visualItem))
            await _cacheManager.RemoveGagItem(visualItem, gagData.AffectedLayer);
        
        PostActionMsg(gagData.Enactor.UID, InteractionType.RemoveGag, $"{gagData.PreviousGag.GagName()} was removed from layer {gagData.AffectedLayer}");
    }

    #endregion Gag Manipulation

    #region Restrictions Manipulation

    public async Task SwapOrApplyRestriction(KinksterUpdateActiveRestriction itemData)
    {
        if (itemData.PreviousRestriction== Guid.Empty)
            await ApplyRestriction(itemData);
        else
            await SwapRestriction(itemData);
    }

    public async Task SwapRestriction(KinksterUpdateActiveRestriction itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Gags);
        await RemoveRestriction(itemData, false);
        await ApplyRestriction(itemData);

        PostActionMsg(itemData.Enactor.UID, InteractionType.SwappedRestriction, itemData.PreviousRestriction + " swapped to " + 
            itemData.NewData.Identifier + " on layer " + itemData.AffectedLayer);
    }

    /// <summary> Applies a Restriction to the client at a defined index. </summary>
    public async Task ApplyRestriction(KinksterUpdateActiveRestriction itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received ApplyRestriction instruction from server!", LoggerType.Restrictions);
        if(_restrictions.ApplyRestriction(itemData.AffectedLayer, itemData.NewData.Identifier, itemData.Enactor.UID, out var visualItem))
            await _cacheManager.AddRestrictionItem(visualItem, itemData.AffectedLayer, itemData.Enactor.UID);

        PostActionMsg(itemData.Enactor.UID, InteractionType.ApplyRestriction, "A Restriction item was applied to you!");
    }

    /// <summary> Locks a padlock from a restriction at a defined index. </summary>
    public void LockRestriction(KinksterUpdateActiveRestriction itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received LockRestriction instruction from server!", LoggerType.Gags);
        _restrictions.LockRestriction(itemData.AffectedLayer, itemData.NewData.Padlock, itemData.NewData.Password, itemData.NewData.Timer, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.LockRestriction, itemData.NewData.Padlock + " was applied to the restriction in layer " + itemData.AffectedLayer);
    }

    /// <summary> Unlocks a padlock from a restriction at a defined index. </summary>
    public void UnlockRestriction(KinksterUpdateActiveRestriction itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received UnlockRestriction instruction from server!", LoggerType.Gags);
        _restrictions.UnlockRestriction(itemData.AffectedLayer, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.UnlockRestriction, itemData.PreviousPadlock + " was removed from layer " + itemData.AffectedLayer + "'s Restriction");
    }

    /// <summary> Removes a restraint item from a defined index. </summary>
    public async Task RemoveRestriction(KinksterUpdateActiveRestriction itemData, bool updateVisuals = true)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received RemoveRestriction instruction from server!", LoggerType.Gags);
        if(_restrictions.RemoveRestriction(itemData.AffectedLayer, itemData.Enactor.UID, out var visualItem))
            await _cacheManager.RemoveRestrictionItem(visualItem, itemData.AffectedLayer);
        
        PostActionMsg(itemData.Enactor.UID, InteractionType.RemoveRestriction, "A Restriction item was removed from you!");
    }

    #endregion Restrictions Manipulation

    #region RestraintSet Manipulation
    public async Task SwapOrApplyRestraint(KinksterUpdateActiveRestraint itemData)
    {
        if (itemData.PreviousRestraint == Guid.Empty)
            await ApplyRestraint(itemData);
        else
            await SwapRestraint(itemData);
    }

    /// <summary> Swapped a Restraint set to the client. </summary>
    public async Task SwapRestraint(KinksterUpdateActiveRestraint itemData)
    {
        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Gags);
        await RemoveRestraint(itemData, false);
        await ApplyRestraint(itemData);
        PostActionMsg(itemData.Enactor.UID, InteractionType.SwappedRestraint, itemData.PreviousRestraint + " swapped to " + itemData.NewData.Identifier);
    }

    /// <summary> Applies a Restraint set to the client. </summary>
    /// <remarks> It is assumed, since this is a callback, that this change is valid and allowed. </remarks>
    public async Task ApplyRestraint(KinksterUpdateActiveRestraint itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received ApplyRestraint instruction from server!", LoggerType.Gags);
        if(_restraints.Apply(itemData, out var restraintSet))
            await _cacheManager.AddRestraintSet(restraintSet, itemData.Enactor.UID);

        PostActionMsg(itemData.Enactor.UID, InteractionType.ApplyRestraint, itemData.NewData.Identifier + " was applied to you!");
    }

    /// <summary> Updates the active Restraint Set's layers with a new configuration. Triggering removal and application of certain layers. </summary>
    /// <remarks> It is assumed, since this is a callback, that this change is valid and allowed. </remarks>
    public async Task SwapRestraintLayers(KinksterUpdateActiveRestraint itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received SwapRestraintLayer instruction from server!", LoggerType.Gags);
        if (_restraints.SwapLayers(itemData, out var restraintSet, out var removedLayers, out var addedLayers))
            await _cacheManager.SwapRestraintSetLayers(restraintSet, removedLayers, addedLayers, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.SwappedRestraintLayers, "Restraint Layers were swapped to a new configuration!");

    }

    /// <summary> Applies a Restraint set layer(s) to the client. </summary>
    /// <remarks> It is assumed, since this is a callback, that this change is valid and allowed. </remarks>
    public async Task ApplyRestraintLayers(KinksterUpdateActiveRestraint itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received ApplyRestraintLayer instruction from server!", LoggerType.Gags);
        if (_restraints.ApplyLayers(itemData, out var restraintSet, out var addedLayers))
            await _cacheManager.AddRestraintSetLayers(restraintSet, addedLayers, itemData.Enactor.UID);

        PostActionMsg(itemData.Enactor.UID, InteractionType.ApplyRestraintLayers, itemData.NewData.Identifier + " was applied to you!");
    }

    /// <summary> Locks the active restraint set. </summary>
    /// <remarks> It is assumed, since this is a callback, that this change is valid and allowed. </remarks>
    public void LockRestraint(KinksterUpdateActiveRestraint itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received LockRestraint instruction from server!", LoggerType.Gags);
        _restraints.Lock(itemData.NewData.Identifier, itemData.NewData.Padlock, itemData.NewData.Password, itemData.NewData.Timer, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.LockRestraint, itemData.NewData.Padlock + " was applied to the restraint");
    }

    /// <summary> Unlocks the active restraint set </summary>
    /// <remarks> It is assumed, since this is a callback, that this change is valid and allowed. </remarks>
    public void UnlockRestraint(KinksterUpdateActiveRestraint itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received UnlockRestraint instruction from server!", LoggerType.Gags);
        _restraints.Unlock(itemData.NewData.Identifier, itemData.Enactor.UID);
        PostActionMsg(itemData.Enactor.UID, InteractionType.UnlockRestraint, itemData.PreviousPadlock + " was removed from the restraint");
    }

    /// <summary> Removes restraint layer(s) from the active restraint set. </summary>
    /// <remarks> It is assumed, since this is a callback, that this change is valid and allowed. </remarks>
    public async Task RemoveRestraintLayers(KinksterUpdateActiveRestraint itemData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received RemoveRestraintLayer instruction from server!", LoggerType.Gags);
        if (_restraints.RemoveLayers(itemData, out var restraintSet, out var removedLayers))
            await _cacheManager.RemoveRestraintSetLayers(restraintSet, removedLayers);

        PostActionMsg(itemData.Enactor.UID, InteractionType.RemoveRestraintLayers, "A Restraint Layer was removed from you!");
    }

    /// <summary> Removes the active restraint. </summary>
    /// <remarks> It is assumed, since this is a callback, that this change is valid and allowed. </remarks>
    public async Task RemoveRestraint(KinksterUpdateActiveRestraint itemData, bool updateVisuals = true)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received RemoveRestraint instruction from server!", LoggerType.Gags);
        if (_restraints.Remove(itemData.Enactor.UID, out var restraintSet, out var removedLayers))
            await _cacheManager.RemoveRestraintSet(restraintSet, removedLayers);

        PostActionMsg(itemData.Enactor.UID, InteractionType.RemoveRestraint, "A Restriction item was removed from you!");
    }

    #endregion RestraintSet Manipulation

    public async void ApplyStatusesByGuid(MoodlesApplierById dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ApplyOwnMoodle, "Moodle Status(s) Applied"))
            await _interop.Moodles.ApplyOwnStatusByGUID(dto.Ids);
    }

    public async void ApplyStatusesToSelf(MoodlesApplierByStatus dto, string clientPlayerNameWithWorld)
    {
        if (_pairs.DirectPairs.FirstOrDefault(p => p.UserData.UID == dto.User.UID) is not { } pair)
        {
            Logger.LogWarning($"Recieved ApplyStatusesToSelf for an unpaired user: {dto.User.AliasOrUID}");
            return;
        }

        // Pair is valid, make sure are visible.
        if (!pair.IsVisible)
        {
            Logger.LogWarning($"Refusing to apply moodles. The sender is not visible: {dto.User.AliasOrUID}");
            return;
        }

        Mediator.Publish(new EventMessage(new(pair.GetNickAliasOrUid(), pair.UserData.UID, InteractionType.ApplyPairMoodle, "Pair's Moodle Status(s) Applied")));
        await _interop.Moodles.ApplyStatusesFromPairToSelf(pair.PlayerNameWithWorld, clientPlayerNameWithWorld, dto.Statuses);
    }

    public async void RemoveStatusesFromSelf(MoodlesRemoval dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.RemoveMoodle, "Moodle Status Removed"))
            await _interop.Moodles.RemoveOwnStatusByGuid(dto.StatusIds);
    }

    public async void ClearStatusesFromSelf(KinksterBase dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ClearMoodle, "Moodles Cleared"))
            await _interop.Moodles.ClearStatus();
    }
}
