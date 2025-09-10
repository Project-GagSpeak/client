using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Handles all incoming events from the GagSpeakHub, and potentially other sources
///     that intend to update server-synced playerData, and updates their visual caches.
/// </summary>
public sealed class VisualStateListener : DisposableMediatorSubscriberBase
{
    private readonly IpcProvider            _provider;
    private readonly IpcManager             _interop;
    private readonly KinksterManager        _pairs;
    private readonly RestraintManager       _restraints;
    private readonly RestrictionManager     _restrictions;
    private readonly GagRestrictionManager  _gags;
    private readonly CollarManager          _collar;
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
        CollarManager collar,
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
        _collar = collar;
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
    public async Task SwapGag(int layer, ActiveGagSlot newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced || _gags.ServerGagData is not { } gagData)
            return;

        Logger.LogTrace("Received SwapGag instruction from server!", LoggerType.Gags);
        var prevGag = gagData.GagSlots[layer].GagItem.GagName();
        PostActionMsg(enactor.UID, InteractionType.SwappedGag, $"Swapped Gag: [{prevGag} >> {newData.GagItem.GagName()}] on layer <{layer}>");

        // Remove it.
        if (_gags.RemoveGag(layer, enactor.UID, out var visualItem))
            await _cacheManager.RemoveGagItem(visualItem, layer);
        // Now apply it.
        if (_gags.ApplyGag(layer, newData.GagItem, enactor.UID, out var gagItem))
            await _cacheManager.AddGagItem(gagItem, layer, enactor.UID);
    }

    public async Task ApplyGag(int layer, ActiveGagSlot newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received ApplyGag instruction from server!", LoggerType.Gags);
        PostActionMsg(enactor.UID, InteractionType.ApplyGag, $"A {newData.GagItem.GagName()} was applied on layer <{layer}>");

        if (_gags.ApplyGag(layer, newData.GagItem, enactor.UID, out var gagItem))
            await _cacheManager.AddGagItem(gagItem, layer, enactor.UID);
    }

    public Task LockGag(int layer, ActiveGagSlot newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return Task.CompletedTask;

        Logger.LogTrace("Received LockGag instruction from server!", LoggerType.Gags);
        PostActionMsg(enactor.UID, InteractionType.LockGag, $"A {newData.Padlock.ToName()} was locked onto layer <{layer}>'s Gag");
        _gags.LockGag(layer, newData, enactor.UID);
        return Task.CompletedTask;
    }

    public Task UnlockGag(int layer, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced || _gags.ServerGagData is not { } gagData)
            return Task.CompletedTask;

        Logger.LogTrace("Received UnlockGag instruction from server!", LoggerType.Gags);
        PostActionMsg(enactor.UID, InteractionType.UnlockGag, $"A {gagData.GagSlots[layer].Padlock.ToName()} was removed from layer <{layer}>'s Gag");
        _gags.UnlockGag(layer, enactor.UID);
        return Task.CompletedTask;
    }

    public async Task RemoveGag(int layer, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced || _gags.ServerGagData is not { } curData)
            return;

        PostActionMsg(enactor.UID, InteractionType.RemoveGag, $"The {curData.GagSlots[layer].GagItem.GagName()} on layer <{layer}> was removed!");
        Logger.LogTrace("Received RemoveGag instruction from server!", LoggerType.Gags);
        if(_gags.RemoveGag(layer, enactor.UID, out var visualItem))
            await _cacheManager.RemoveGagItem(visualItem, layer);
    }

    #endregion Gag Manipulation

    #region Restrictions Manipulation
    public async Task SwapRestriction(int layer, ActiveRestriction newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced || _restrictions.ServerRestrictionData is not { } curData)
            return;

        Logger.LogTrace("Received SwapRestriction instruction from server!", LoggerType.Gags);
        var prevRestriction = curData.Restrictions[layer].Identifier;
        PostActionMsg(enactor.UID, InteractionType.SwappedRestriction, $"Swapped Layer <{layer}> Restriction: [{prevRestriction} >> {newData.Identifier}]");
        // Remove it.
        if (_restrictions.RemoveRestriction(layer, enactor.UID, out var visualRemItem))
            await _cacheManager.RemoveRestrictionItem(visualRemItem, layer);
        // Now apply it.
        if (_restrictions.ApplyRestriction(layer, newData, enactor.UID, out var visualAddItem))
            await _cacheManager.AddRestrictionItem(visualAddItem, layer, enactor.UID);
    }

    public async Task ApplyRestriction(int layer, ActiveRestriction newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received ApplyRestriction instruction from server!", LoggerType.Restrictions);
        PostActionMsg(enactor.UID, InteractionType.ApplyRestriction, "A Restriction item was applied to you!");

        if (_restrictions.ApplyRestriction(layer, newData, enactor.UID, out var visualItem))
            await _cacheManager.AddRestrictionItem(visualItem, layer, enactor.UID);
    }

    public Task LockRestriction(int layer, ActiveRestriction newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return Task.CompletedTask;
        Logger.LogTrace("Received LockRestriction instruction from server!", LoggerType.Restrictions);
        PostActionMsg(enactor.UID, InteractionType.LockRestriction, $"Locked a {newData.Padlock.ToName()} to layer <{layer}>'s Restriction");
        _restrictions.LockRestriction(layer, newData, enactor.UID);
        return Task.CompletedTask;
    }

    public Task UnlockRestriction(int layer, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced || _restrictions.ServerRestrictionData is not { } curData)
            return Task.CompletedTask;
        Logger.LogTrace("Received UnlockRestriction instruction from server!", LoggerType.Restrictions);
        PostActionMsg(enactor.UID, InteractionType.UnlockRestriction, $"Removed {curData.Restrictions[layer].Padlock.ToName()} from layer <{layer}>'s Restriction");
        _restrictions.UnlockRestriction(layer, enactor.UID);
        return Task.CompletedTask;
    }

    public async Task RemoveRestriction(int layer, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received RemoveRestriction instruction from server!", LoggerType.Restrictions);
        PostActionMsg(enactor.UID, InteractionType.RemoveRestriction, "A Restriction item was removed from you!");

        if (_restrictions.RemoveRestriction(layer, enactor.UID, out var visualItem))
            await _cacheManager.RemoveRestrictionItem(visualItem, layer);
    }

    #endregion Restrictions Manipulation

    #region RestraintSet Manipulation
    public async Task SwapRestraint(CharaActiveRestraint newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced || _restraints.ServerData is not { } itemData)
            return;

        Logger.LogTrace("Received SwapRestraintSet instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.SwappedRestraint, $"Swapped RestraintSet: [{itemData.Identifier} >> {newData.Identifier}]");
        // Remove it.
        if (_restraints.Remove(enactor.UID, out var visualRemItem, out var remLayers))
            await _cacheManager.RemoveRestraintSet(visualRemItem, remLayers);
        // Now apply it.
        if (_restraints.Apply(newData, enactor.UID, out var visualAddItem))
            await _cacheManager.AddRestraintSet(visualAddItem, enactor.UID);
    }

    public async Task ApplyRestraint(CharaActiveRestraint newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received ApplyRestraint instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.ApplyRestraint, $"A RestraintSet was applied to you! ({newData.Identifier})");

        if (_restraints.Apply(newData, enactor.UID, out var restraintSet))
            await _cacheManager.AddRestraintSet(restraintSet, enactor.UID);
    }

    public async Task SwapRestraintLayers(CharaActiveRestraint newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received SwapRestraintLayer instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.SwappedRestraintLayers, $"RestraintSet Layers were swapped! ({newData.ActiveLayers})");

        if (_restraints.SwapLayers(newData, enactor.UID, out var restraintSet, out var removedLayers, out var addedLayers))
            await _cacheManager.SwapRestraintSetLayers(restraintSet, removedLayers, addedLayers, enactor.UID);

    }

    public async Task ApplyRestraintLayers(CharaActiveRestraint newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received ApplyRestraintLayer instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.ApplyRestraintLayers, $"New RestraintSet Layers were applied! ({newData.ActiveLayers})");

        if (_restraints.ApplyLayers(newData.ActiveLayers, enactor.UID, out var restraintSet, out var addedLayers))
            await _cacheManager.AddRestraintSetLayers(restraintSet, addedLayers, enactor.UID);
    }

    public Task LockRestraint(CharaActiveRestraint newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return Task.CompletedTask;
        Logger.LogTrace("Received LockRestraint instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.LockRestraint, $"A {newData.Padlock.ToName()} was locked onto your Restraint Set");
        _restraints.Lock(newData, enactor.UID);
        return Task.CompletedTask;
    }

    public Task UnlockRestraint(UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced || _restraints.ServerData is not { } itemData)
            return Task.CompletedTask;

        Logger.LogTrace("Received UnlockRestraint instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.UnlockRestraint, $"The {itemData.Padlock.ToName()} was removed from your Restraint Set");
        _restraints.Unlock(enactor.UID);
        return Task.CompletedTask;
    }

    public async Task RemoveRestraintLayers(CharaActiveRestraint newData, UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received RemoveRestraintLayer instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.RemoveRestraintLayers, "RestraintSet Layers were removed from you!");

        if (_restraints.RemoveLayers(newData.ActiveLayers, enactor.UID, out var restraintSet, out var removedLayers))
            await _cacheManager.RemoveRestraintSetLayers(restraintSet, removedLayers);
    }

    public async Task RemoveRestraint(UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received RemoveRestraint instruction from server!", LoggerType.Restraints);
        PostActionMsg(enactor.UID, InteractionType.RemoveRestraint, "Your RestraintSet was removed!");

        if (_restraints.Remove(enactor.UID, out var restraintSet, out var removedLayers))
            await _cacheManager.RemoveRestraintSet(restraintSet, removedLayers);
    }

    #endregion RestraintSet Manipulation

    #region Collar Manipulation
    // Should occur upon a collar request being accepted.
    public async Task ApplyCollar(KinksterUpdateActiveCollar collarData)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        Logger.LogTrace("Received ApplyCollar instruction from server!", LoggerType.Collars);
        // Apply the collar.
        _collar.Apply(collarData);
        await _cacheManager.AddCollar(collarData.Enactor);

        PostActionMsg(collarData.Enactor.UID, InteractionType.ApplyCollar, "Your collar was applied!");
    }

    public async Task UpdateActiveCollar(CharaActiveCollar newData, UserData enactor, DataUpdateType type)
    {
        if (!MainHub.IsConnectionDataSynced || _collar.SyncedData is not { } synced)
            return;

        Logger.LogTrace($"Received {type} instruction from server!", LoggerType.Collars);
        var prevVisuals = synced.Visuals;
        _collar.UpdateActive(newData, enactor, type);

        // if the visuals were turned on, add the collar item
        if (!prevVisuals && synced.Visuals)
            await _cacheManager.AddCollar(enactor);
        // if the visuals were turned off, remove the collar item
        else if (prevVisuals && !synced.Visuals)
            await _cacheManager.RemoveCollar(enactor);
        // if they were on after the update, update the visuals
        else if (synced.Visuals)
            await _cacheManager.UpdateCollar(type, enactor);

        PostActionMsg(enactor.UID, InteractionType.UpdateCollar, $"Your Collar's SyncedData was updated by {enactor.AliasOrUID}");
    }

    public async Task RemoveCollar(UserData enactor)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        Logger.LogTrace("Received RemoveCollar instruction from server!", LoggerType.Collars);
        
        _collar.Remove(enactor);
        await _cacheManager.RemoveCollar(enactor);

        PostActionMsg(enactor.UID, InteractionType.RemoveRestriction, "Your Collar was removed from you!");
    }
    #endregion Collar Manipulation

    #region CursedLoot Manipulation
    public async Task CursedGagApplied(CursedGagItem item, int layer, DateTimeOffset endTime)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        // Update the release time inside of the cursed loot manager. 
        item.AppliedTime = DateTimeOffset.UtcNow;
        item.ReleaseTime = endTime;
        _cursedLoot.ForceSave();

        // new data for cursed item. (this is the same as what was sent over the server, so it syncs)
        var newData = new ActiveGagSlot
        {
            GagItem = item.RefItem.GagType,
            Enabler = "Mimic",
            Padlock = Padlocks.Mimic,
            Password = string.Empty,
            Timer = endTime,
            PadlockAssigner = "Mimic"
        };

        // apply the gag, and it's visual updates.
        if (_gags.ApplyGag(layer, newData.GagItem, "Mimic", out var gagItem))
            await _cacheManager.AddGagItem(gagItem, layer, "Mimic");

        // Lock it immediately.
        _gags.LockGag(layer, newData, "Mimic");
        // the apply gag function already handled all of the visual cache application for us, so we can return here.

        Logger.LogInformation($"Cursed Loot Applied & Locked!", LoggerType.CursedItems);
        Svc.Chat.PrintError(new SeStringBuilder()
            .AddItalics("As the coffer opens, cursed loot spills forth, silencing your mouth with a Gag now strapped on tight!")
            .BuiltString);

        // if we should warn the user, do so!.
        if (ClientData.Globals?.ChatGarblerActive ?? false)
            Mediator.Publish(new NotificationMessage("Chat Garbler", "Your garbler is still active! Be careful chatting around strangers!", NotificationType.Warning));

        // Signal achievement event.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
    }

    public async Task CursedItemApplied(CursedRestrictionItem item, DateTimeOffset endTime)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        _cursedLoot.ActivateItem(item, endTime);
        // now we need to update the equivalent visual data.
        if (_restrictions.ApplyCursedItem(item, out var layer))
            await _cacheManager.AddCursedItem(item, layer);

        Logger.LogInformation($"Cursed Loot Applied!", LoggerType.CursedItems);
        Svc.Chat.PrintError(new SeStringBuilder().AddItalics("As the coffer opens, cursed loot spills forth, binding you in an inescapable restraint!").BuiltString);
        // Signal achievement event.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
    }

    public async Task CursedItemRemoved(Guid removedId)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        // if the item doesnt exist, return.
        if (!_cursedLoot.Storage.TryGetLoot(removedId, out var item))
            return;

        // take the loot item out of the active item pool.
        _cursedLoot.SetInactive(removedId);
        if (item is CursedRestrictionItem restriction)
        {
            // remove it, and then remove it from the visuals if valid.
            if (_restrictions.RemoveCursedItem(restriction, out var layer))
                await _cacheManager.RemoveCursedItem(restriction, layer);
        }

        Logger.LogInformation($"Cursed Loot Removed!", LoggerType.CursedItems);
        Svc.Chat.PrintError(new SeStringBuilder().AddItalics("The curse lifts, and the item vanishes in a puff of smoke!").BuiltString);
    }
    #endregion CursedLoot Manipulation


    public async void ApplyStatusesByGuid(MoodlesApplierById dto)
    {
        if(PostActionMsg(dto.User.UID, InteractionType.ApplyOwnMoodle, "Moodle Status(s) Applied"))
            await _interop.Moodles.ApplyOwnStatusByGUID(dto.Ids);
    }

    public void ApplyStatusesToSelf(MoodlesApplierByStatus dto)
    {
        if (_pairs.DirectPairs.FirstOrDefault(p => p.UserData.UID == dto.User.UID) is not { } pair)
        {
            Logger.LogWarning($"Received ApplyStatusesToSelf for an unpaired user: {dto.User.AliasOrUID}");
            return;
        }

        // Pair is valid, make sure are visible.
        if (!pair.IsVisible)
        {
            Logger.LogWarning($"Refusing to apply moodles. The sender is not visible: {dto.User.AliasOrUID}");
            return;
        }

        Mediator.Publish(new EventMessage(new(pair.GetNickAliasOrUid(), pair.UserData.UID, InteractionType.ApplyPairMoodle, "Pair's Moodle Status(s) Applied to self!")));
        _provider.ApplyMoodlesSentByKinkster(pair.PlayerNameWithWorld, dto.Statuses.ToList());
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
