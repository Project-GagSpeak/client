using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Interop.Helpers;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI;

// This section of the MainHub focuses on responses received by the Server.
// We use this to perform actions to our client's data.
public partial class MainHub
{
    #region Pairing & Messages
    /// <summary> Called when the server sends a message to the client. </summary>
    public Task Callback_ServerMessage(MessageSeverity messageSeverity, string message)
    {
        switch (messageSeverity)
        {
            case MessageSeverity.Error:
                Mediator.Publish(new NotificationMessage("Error from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Error, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Warning:
                Mediator.Publish(new NotificationMessage("Warning from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Warning, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Information:
                if (_suppressNextNotification)
                {
                    _suppressNextNotification = false;
                    break;
                }
                Mediator.Publish(new NotificationMessage("Info from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Info, TimeSpan.FromSeconds(7.5)));
                break;
        }
        // return it as a completed task.
        return Task.CompletedTask;
    }

    // Sometimes Corby just wants to do a little bullying.
    public Task Callback_HardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState newServerState)
    {
        switch (messageSeverity)
        {
            case MessageSeverity.Error:
                Mediator.Publish(new NotificationMessage("Error from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Error, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Warning:
                Mediator.Publish(new NotificationMessage("Warning from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Warning, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Information:
                if (_suppressNextNotification)
                {
                    _suppressNextNotification = false;
                    break;
                }
                Mediator.Publish(new NotificationMessage("Info from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Info, TimeSpan.FromSeconds(5)));
                break;
        }
        // we need to update the api server state to be stopped if connected
        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                // pause the server state
                _serverConfigs.ServerStorage.FullPause = true;
                _serverConfigs.Save();
                _suppressNextNotification = true;
                // create a new connection to force the disconnect.
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);

                // because this is a forced reconnection, clear our token cache between, incase we were banned.
                _tokenProvider.ResetTokenCache();

                // after it stops, switch the connection pause back to false and create a new connection.
                _serverConfigs.ServerStorage.FullPause = false;
                _serverConfigs.Save();
                _suppressNextNotification = true;
                await Connect().ConfigureAwait(false);
            });
        }
        // return completed
        return Task.CompletedTask;
    }

    public Task Callback_ServerInfo(ServerInfoResponse serverInfo)
    {
        _serverInfo = serverInfo;
        return Task.CompletedTask;
    }

    public Task Callback_AddClientPair(KinksterPair dto)
    {
        Logger.LogDebug($"Callback_AddClientPair: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.AddNewKinksterPair(dto));
        return Task.CompletedTask;
    }

    public Task Callback_RemoveClientPair(KinksterBase dto)
    {
        Logger.LogDebug($"Callback_AddClientPair: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.RemoveKinksterPair(dto));
        return Task.CompletedTask;
    }

    public Task Callback_AddPairRequest(KinksterPairRequest dto)
    {
        Logger.LogDebug($"Callback_AddPairRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _clientDatListener.AddRequest(dto));
        return Task.CompletedTask;
    }

    public Task Callback_RemovePairRequest(KinksterPairRequest dto)
    {
        Logger.LogDebug($"Callback_RemovePairRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _clientDatListener.RemoveRequest(dto));
        return Task.CompletedTask;
    }

    public Task Callback_AddCollarRequest(CollarRequest dto)
    {
        Logger.LogDebug($"Callback_AddCollarRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _collarManager.AddRequest(dto));
        return Task.CompletedTask;
    }

    public Task Callback_RemoveCollarRequest(CollarRequest dto)
    {
        Logger.LogDebug($"Callback_RemoveCollarRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _collarManager.RemoveRequest(dto));
        return Task.CompletedTask;
    }

    #endregion Pairing & Messages

    #region KinksterSync
    //public Task Callback_SetKinksterIpcData(KinksterIpcData dto)
    //{
    //    // TODO: remove
    //    return Task.CompletedTask;
    //}

    //public Task Callback_SetKinksterIpcSingle(KinksterIpcSingle dto)
    //{
    //    // TODO: remove
    //    return Task.CompletedTask;
    //}
    #endregion KinksterSync

    #region Moodles
    public Task Callback_SetKinksterMoodlesFull(KinksterMoodlesDataFull dto)
    {
        Logger.LogDebug($"Callback_SetKinksterMoodlesFull: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        _kinksterListener.NewMoodlesData(dto.User, dto.Enactor, dto.NewData);
        return Task.CompletedTask;
    }
    public Task Callback_SetKinksterMoodlesSM(KinksterMoodlesSM dto)
    {
        Logger.LogDebug($"Callback_SetKinksterMoodlesSM: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        _kinksterListener.NewStatusManager(dto.User, dto.Enactor, dto.DataString, dto.DataInfo);
        return Task.CompletedTask;
    }
    public Task Callback_SetKinksterMoodlesStatuses(KinksterMoodlesStatuses dto)
    {
        Logger.LogDebug($"Callback_SetKinksterMoodlesStatuses: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        _kinksterListener.NewStatuses(dto.User, dto.Enactor, dto.Statuses);
        return Task.CompletedTask;
    }
    public Task Callback_SetKinksterMoodlesPresets(KinksterMoodlesPresets dto)
    {
        Logger.LogDebug($"Callback_SetKinksterMoodlesPresets: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        _kinksterListener.NewPresets(dto.User, dto.Enactor, dto.Presets);
        return Task.CompletedTask;
    }

    public async Task Callback_ApplyMoodlesByGuid(MoodlesApplierById dto)
    {
        Logger.LogDebug("Callback_ApplyMoodlesByGuid: " + dto, LoggerType.Callbacks);
        await _visualListener.ApplyStatusesByGuid(dto);
    }

    public async Task Callback_ApplyMoodlesByStatus(MoodlesApplierByStatus dto)
    {
        Logger.LogDebug("Callback_ApplyMoodlesByStatus: " + dto, LoggerType.Callbacks);
        // obtain the local player name and world
        await _visualListener.ApplyStatusesToSelf(dto, PlayerData.NameWithWorldInstanced);
        Logger.LogDebug("Applied Moodles to Self: " + dto, LoggerType.Callbacks);
    }

    /// <summary> Intended to clear all moodles from OUR client player. </summary>
    /// <remarks> Should make a call to our moodles IPC to remove the statuses listed by their GUID's </remarks>
    public async Task Callback_RemoveMoodles(MoodlesRemoval dto)
    {
        Logger.LogDebug("Callback_RemoveMoodles: " + dto, LoggerType.Callbacks);
        await _visualListener.RemoveStatusesFromSelf(dto);
    }

    /// <summary> Intended to clear all moodles from OUR client player. </summary>
    /// <remarks> Should make a call to our moodles IPC to clear all statuses. </remarks>
    public async Task Callback_ClearMoodles(KinksterBase dto)
    {
        Logger.LogDebug("Callback_ClearMoodles: " + dto, LoggerType.Callbacks);
        await _visualListener.ClearStatusesFromSelf(dto);
    }
    #endregion Moodles

    #region Pair Permission Exchange
    public Task Callback_BulkChangeGlobal(BulkChangeGlobal dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _clientDatListener.ChangeAllClientGlobals(dto.User, dto.NewPerms, dto.NewState));
            // handle soon.
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.PermBulkChangeGlobal(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_BulkChangeUnique(BulkChangeUnique dto)
    {
        Generic.Safe(() =>
        {
            if (dto.User.UID == UID)
                throw new Exception("Should never be calling a permission update for yourself in bulk, use BulkChangeAll for these!");

            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            if (dto.Direction is UpdateDir.Own)
                _kinksterListener.PermBulkChangeUniqueOwn(dto.User, dto.NewPerms, dto.NewAccess);
            else
                _kinksterListener.PermBulkChangeUniqueOther(dto.User, dto.NewPerms, dto.NewAccess);
        });
        return Task.CompletedTask;
    }

    public Task Callback_SingleChangeGlobal(SingleChangeGlobal dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _clientDatListener.ChangeGlobalPerm(dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.PermChangeGlobal(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
        }
        return Task.CompletedTask;
    }

    public Task Callback_SingleChangeUnique(SingleChangeUnique dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.PermChangeUnique(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.PermChangeUniqueOther(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeAccess(SingleChangeAccess dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.PermChangeAccess(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.PermChangeAccessOther(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
    }

    public Task Callback_StateChangeHardcore(HardcoreStateChange dto)
    {
        if (dto.Target.UID == UID)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _clientDatListener.ChangeHardcoreState(dto.Enactor, dto.Changed, dto.NewData));
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.StateChangeHardcore(dto.Target, dto.Enactor, dto.Changed, dto.NewData));
        }
        return Task.CompletedTask;

    }
    #endregion Pair Permission Exchange

    /// <summary> Should only ever get the other pairs. If getting self, something is up. </summary>
    public Task Callback_KinksterUpdateComposite(KinksterUpdateComposite dto)
    {
        if (dto.User.UID != UID)
            Generic.Safe(() => _kinksterListener.NewActiveComposite(dto.User, dto.Data, dto.WasSafeword));

        return Task.CompletedTask;
    }

    // Invoked by other kinksters.
    public Task Callback_KinksterUpdateActiveGag(KinksterUpdateActiveGag dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-GAGS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    if (dataDto.PreviousGag is GagType.None)
                        _visualListener.ApplyGag(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    else
                        _visualListener.SwapGag(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _visualListener.LockGag(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor);
                    break;
                case DataUpdateType.Unlocked:
                    _visualListener.UnlockGag(dataDto.AffectedLayer, dataDto.Enactor);
                    break;
                case DataUpdateType.Removed:
                    _visualListener.RemoveGag(dataDto.AffectedLayer, dataDto.Enactor).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-GAGS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveGags(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveRestriction(KinksterUpdateActiveRestriction dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-RESTRICTIONS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    if (dataDto.PreviousRestriction == Guid.Empty)
                        _visualListener.ApplyRestriction(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    else
                        _visualListener.SwapRestriction(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _visualListener.LockRestriction(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor);
                    break;
                case DataUpdateType.Unlocked:
                    _visualListener.UnlockRestriction(dataDto.AffectedLayer, dataDto.Enactor);
                    break;
                case DataUpdateType.Removed:
                    _visualListener.RemoveRestriction(dataDto.AffectedLayer, dataDto.Enactor).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-RESTRICTIONS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveRestriction(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveRestraint(KinksterUpdateActiveRestraint dataDto)
    {
        // If the update is for us, handle it.
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-RESTRAINT-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    if (dataDto.PreviousRestraint == Guid.Empty)
                        _visualListener.ApplyRestraint(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    else
                        _visualListener.SwapRestraint(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.LayersChanged:
                    _visualListener.SwapRestraintLayers(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.LayersApplied:
                    _visualListener.ApplyRestraintLayers(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _visualListener.LockRestraint(dataDto.NewData, dataDto.Enactor);
                    break;
                case DataUpdateType.Unlocked:
                    _visualListener.UnlockRestraint(dataDto.Enactor);
                    break;
                case DataUpdateType.LayersRemoved:
                    _visualListener.RemoveRestraintLayers(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Removed:
                    _visualListener.RemoveRestraint(dataDto.Enactor).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-RESTRAINT-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveRestraint(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveCollar(KinksterUpdateActiveCollar dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-COLLAR-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);

            switch (dataDto.Type)
            {
                case DataUpdateType.RequestAccepted:
                    // handle an accepted request here.
                    break;
                case DataUpdateType.OwnersUpdated:
                    // update owners and things here.
                    break;
                case DataUpdateType.VisibilityChange:
                    // process a toggle to visibility. Change always will inflict a toggle.
                    break;
                case DataUpdateType.DyesChange:
                    // process a change to the active collar's dyes.
                    break;
                case DataUpdateType.CollarMoodleChange:
                    // process a change to the active collar's Moodles.
                    break;
                case DataUpdateType.CollarWritingChange:
                    // process a change to the collar's writing,
                    // and perhaps an enforced profile refresh?
                    break;
                case DataUpdateType.CollarRemoved:
                    // process collar removal.
                    break;

            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-COLLAR-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveCollar(dataDto));
            return Task.CompletedTask;
        }
    }


    /// <summary> The only condition that we receive this, is if it's for another pair. </summary>
    public Task Callback_KinksterUpdateActiveCursedLoot(KinksterUpdateActiveCursedLoot dataDto)
    {
        if (dataDto.User.UID != UID)
        {
            Logger.LogDebug($"[OTHER-CURSEDLOOT-ACTIVE]: {dataDto.User} ({dataDto.ChangedItem})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveCursedLoot(dataDto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogWarning("Received a Cursed Loot update for ourselves, this should never happen!", LoggerType.Callbacks);
        }
        return Task.CompletedTask;
    }

    public Task Callback_KinksterUpdateAliasGlobal(KinksterUpdateAliasGlobal dto)
    {
        Logger.LogDebug($"Received a Kinksters updated Global AliasTrigger {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksterListener.NewAliasGlobal(dto.User, dto.AliasId, dto.NewData));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterUpdateAliasUnique(KinksterUpdateAliasUnique dto)
    {
        Logger.LogDebug($"Received a Kinksters updated Global AliasTrigger {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksterListener.NewAliasUnique(dto.User, dto.AliasId, dto.NewData));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterUpdateValidToys(KinksterUpdateValidToys dto)
    {
        Logger.LogDebug($"Received a Kinkster's updated Valid Toys: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksterListener.NewValidToys(dto.User, dto.ValidToys));
        return Task.CompletedTask;
    }

    public async Task Callback_KinksterUpdateActivePattern(KinksterUpdateActivePattern dto)
    {
        if (dto.User.UID == UID)
        {
            // if the callback wants us to update our pattern state but we are recording or in a vibe room we should reject this?
            // patterns are fragile babies and must be handled with care.
            Logger.LogDebug($"OWN KinksterUpdateActivePattern: {dto.User.AliasOrUID}", LoggerType.Callbacks);
            await Generic.Safe(async () =>
            {
                var success = dto.Type switch
                {
                    DataUpdateType.PatternSwitched => _toyboxListener.PatternSwitched(dto.ActivePattern, dto.Enactor.UID),
                    DataUpdateType.PatternExecuted => _toyboxListener.PatternStarted(dto.ActivePattern, dto.Enactor.UID),
                    DataUpdateType.PatternStopped => _toyboxListener.PatternStopped(dto.ActivePattern, dto.Enactor.UID),
                    _ => false
                };
                if (!success)
                {
                    Logger.LogError($"Failed to handle KinksterUpdateActivePattern for {dto.User.AliasOrUID} with type {dto.Type}");
                    Logger.LogError($"Attempt to find out why this is even allowed to happen, and fix it, as it should never occur!");
                    var recallType = _toyboxListener.ActivePattern == Guid.Empty ? DataUpdateType.PatternStopped : DataUpdateType.PatternSwitched;
                    await UserPushActivePattern(new PushClientActivePattern(_kinksters.GetOnlineUserDatas(), _toyboxListener.ActivePattern, recallType));
                }
            });
        }
        else
        {
            Logger.LogDebug($"OTHER KinksterUpdateActivePattern: {dto.User.AliasOrUID}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActivePattern(dto));
        }
    }

    public Task Callback_KinksterUpdateActiveAlarms(KinksterUpdateActiveAlarms dto)
    {
        if (dto.Type is not DataUpdateType.AlarmToggled)
        {
            Logger.LogWarning("Received an Alarm Update that was not a toggle, this should never happen! " + dto.Type, LoggerType.Callbacks);
            return Task.CompletedTask;
        }

        // Valid type, so process the change.
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"OWN Callback_KinksterUpdateActiveAlarms: {dto.User.AliasOrUID}", LoggerType.Callbacks);
            _toyboxListener.AlarmToggled(dto.ChangedItem, dto.Enactor.UID);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"OTHER Callback_ReceiveDataToybox: {dto.User.AliasOrUID}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveAlarms(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveTriggers(KinksterUpdateActiveTriggers dto)
    {
        if (dto.Type is not DataUpdateType.TriggerToggled)
        {
            Logger.LogWarning("Received a Trigger Update that was not a toggle, this should never happen! " + dto.Type, LoggerType.Callbacks);
            return Task.CompletedTask;
        }

        // Valid type, so process the change.
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"OWN Callback_ReceiveDataToybox: {dto.User.AliasOrUID}", LoggerType.Callbacks);
            _toyboxListener.TriggerToggled(dto.ChangedItem, dto.Enactor.UID);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"OTHER Callback_ReceiveDataToybox: {dto.Enactor.AliasOrUID}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveTriggers(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_ListenerName(UserData user, string trueNameWithWorld)
    {
        Logger.LogDebug($"Received a Kinkster {user.UID}'s updated puppeteer name", LoggerType.Callbacks);
        Generic.Safe(() => _puppetListener.UpdateListener(user.UID, trueNameWithWorld));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Shock Instruction from another Pair. </summary>
    public Task Callback_ShockInstruction(ShockCollarAction dto)
    {
        Generic.Safe(() => _shockies.PerformShockCollarAct(dto));
        return Task.CompletedTask;
    }

    // Expected to update their global permission with this new state. If it fails, should reset.
    public Task Callback_HypnoticEffect(HypnoticAction dto)
    {
        Logger.LogDebug("Callback_HypnoticEffect: " + dto, LoggerType.Callbacks);
        Generic.Safe(() => _clientDatListener.Hypnotize(dto.User, dto.Effect, dto.ExpireTime, dto.base64Image));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewGagData(KinksterNewGagData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedGagDataChange(dto.User, dto.GagType, dto.Item));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewRestrictionData(KinksterNewRestrictionData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedRestrictionDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewRestraintData(KinksterNewRestraintData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedRestraintDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewCollarData(KinksterNewCollarData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedCollarDataChange(dto.User, dto.LightItem));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewLootData(KinksterNewLootData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedCursedLootDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Kinkster's updated PatternData change. </summary>
    public Task Callback_KinksterNewPatternData(KinksterNewPatternData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedPatternDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Kinkster's updated AlarmData change. </summary>
    public Task Callback_KinksterNewAlarmData(KinksterNewAlarmData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedAlarmDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Kinkster's updated TriggerData change. </summary>
    public Task Callback_KinksterNewTriggerData(KinksterNewTriggerData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedTriggerDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Kinkster's updated TriggerData change. </summary>
    public Task Callback_KinksterNewAllowances(KinksterNewAllowances dto)
    {
        Generic.Safe(() => _kinksterListener.CachedAllowancesChange(dto.User, dto.Module, [.. dto.AllowedUids]));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Global Chat Message. </summary>
    public Task Callback_ChatMessageGlobal(ChatMessageGlobal dto)
    {
        Mediator.Publish(new GlobalChatMessage(dto, dto.Sender.UID.Equals(UID)));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs disconnects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the KinksterBase in our pair manager so they are marked as offline. </remarks>
    public Task Callback_KinksterOffline(KinksterBase dto)
    {
        Logger.LogDebug("Callback_SendOffline: " + dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.MarkKinksterOffline(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs connects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the KinksterBase in our pair manager so they are marked as online. </remarks>
    public Task Callback_KinksterOnline(OnlineKinkster dto)
    {
        Logger.LogDebug("Callback_SendOnline: " + dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.MarkKinksterOnline(dto));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever we need to update the profile data of anyone, including ourselves. </summary>
    public Task Callback_ProfileUpdated(KinksterBase dto)
    {
        Logger.LogDebug("Callback_UpdateProfile: " + dto, LoggerType.Callbacks);
        Mediator.Publish(new ClearProfileDataMessage(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> The callback responsible for displaying verification codes to the clients monitor. </summary>
    /// <remarks> This is currently experiencing issues for some reason with the discord bot. Look into more? </remarks>
    public Task Callback_ShowVerification(VerificationCode dto)
    {
        Logger.LogDebug("Callback_ShowVerification: " + dto, LoggerType.Callbacks);
        Mediator.Publish(new VerificationPopupMessage(dto));
        return Task.CompletedTask;
    }

    public Task Callback_RoomJoin(RoomParticipant dto)
    {
        Logger.LogDebug("Callback_RoomJoin: " + dto, LoggerType.Callbacks);
        _toyboxListener.KinksterJoinedRoom(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomLeave(UserData dto)
    {
        Logger.LogDebug("Callback_RoomLeave: " + dto, LoggerType.Callbacks);
        _toyboxListener.KinksterLeftRoom(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomAddInvite(RoomInvite dto)
    {
        Logger.LogDebug("Callback_RoomAddInvite: " + dto, LoggerType.Callbacks);
        _toyboxListener.VibeRoomInviteReceived(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomHostChanged(UserData dto)
    {
        Logger.LogDebug("Callback_RoomHostChanged: " + dto, LoggerType.Callbacks);
        _toyboxListener.VibeRoomHostChanged(dto);
        return Task.CompletedTask;
    }



    /// <summary> Receive a Device Update from a Room. </summary>
    public Task Callback_RoomDeviceUpdate(UserData user, ToyInfo device)
    {
        Logger.LogDebug("Callback_RoomDeviceUpdate: " + user, LoggerType.Callbacks);
        _toyboxListener.KinksterUpdatedDevice(user, device);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Data Stream from a Room. </summary>
    public Task Callback_RoomIncDataStream(ToyDataStreamResponse dto)
    {
        Logger.LogDebug("Callback_RoomIncDataStream: " + dto, LoggerType.Callbacks);
        _toyboxListener.ReceivedBuzzToyDataStream(dto);
        return Task.CompletedTask;
    }

    /// <summary> A User granted us access to control their sex toys. </summary>
    public Task Callback_RoomAccessGranted(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessGranted: " + user, LoggerType.Callbacks);
        _toyboxListener.KinksterGrantedAccess(user);
        return Task.CompletedTask;
    }

    /// <summary> A User revoked access to their sextoys. </summary>
    public Task Callback_RoomAccessRevoked(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessRevoked: " + user, LoggerType.Callbacks);
        _toyboxListener.KinksterRevokedAccess(user);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Chat Message from a Room. </summary>
    public Task Callback_RoomChatMessage(UserData user, string message)
    {
        Logger.LogDebug("Callback_RoomChatMessage: " + user + " - " + message, LoggerType.Callbacks);
        Mediator.Publish(new VibeRoomChatMessage(user, message));
        return Task.CompletedTask;
    }

    /* --------------------------------- void methods from the API to call the hooks --------------------------------- */
    public void OnServerMessage(Action<MessageSeverity, string> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ServerMessage), act);
    }

    public void OnHardReconnectMessage(Action<MessageSeverity, string, ServerState> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_HardReconnectMessage), act);
    }

    public void OnServerInfo(Action<ServerInfoResponse> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ServerInfo), act);
    }

    public void OnAddClientPair(Action<KinksterPair> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_AddClientPair), act);
    }

    public void OnRemoveClientPair(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemoveClientPair), act);
    }

    public void OnAddPairRequest(Action<KinksterPairRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_AddPairRequest), act);
    }

    public void OnRemovePairRequest(Action<KinksterPairRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemovePairRequest), act);
    }

    public void OnAddCollarRequest(Action<CollarRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_AddCollarRequest), act);
    }

    public void OnRemoveCollarRequest(Action<CollarRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemoveCollarRequest), act);
    }

    //public void OnSetKinksterIpcData(Action<KinksterIpcData> act)
    //{
    //    if (_apiHooksInitialized) return;
    //    _hubConnection!.On(nameof(Callback_SetKinksterIpcData), act);
    //}

    //public void OnSetKinksterIpcSingle(Action<KinksterIpcSingle> act)
    //{
    //    if (_apiHooksInitialized) return;
    //    _hubConnection!.On(nameof(Callback_SetKinksterIpcSingle), act);
    //}

    public void OnSetKinksterMoodlesFull(Action<KinksterMoodlesDataFull> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SetKinksterMoodlesFull), act);
    }

    public void OnSetKinksterMoodlesSM(Action<KinksterMoodlesSM> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SetKinksterMoodlesSM), act);
    }

    public void OnSetKinksterMoodlesStatuses(Action<KinksterMoodlesStatuses> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SetKinksterMoodlesStatuses), act);
    }

    public void OnSetKinksterMoodlesPresets(Action<KinksterMoodlesPresets> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SetKinksterMoodlesPresets), act);
    }

    public void OnApplyMoodlesByGuid(Action<MoodlesApplierById> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ApplyMoodlesByGuid), act);
    }

    public void OnApplyMoodlesByStatus(Action<MoodlesApplierByStatus> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ApplyMoodlesByStatus), act);
    }

    public void OnRemoveMoodles(Action<MoodlesRemoval> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemoveMoodles), act);
    }

    public void OnClearMoodles(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ClearMoodles), act);
    }
    public void OnBulkChangeGlobal(Action<BulkChangeGlobal> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_BulkChangeGlobal), act);
    }

    public void OnBulkChangeUnique(Action<BulkChangeUnique> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_BulkChangeUnique), act);
    }

    public void OnSingleChangeGlobal(Action<SingleChangeGlobal> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SingleChangeGlobal), act);
    }

    public void OnSingleChangeUnique(Action<SingleChangeUnique> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SingleChangeUnique), act);
    }

    public void OnSingleChangeAccess(Action<SingleChangeAccess> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SingleChangeAccess), act);
    }

    public void OnStateChangeHardcore(Action<HardcoreStateChange> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_StateChangeHardcore), act);
    }

    public void OnKinksterUpdateComposite(Action<KinksterUpdateComposite> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateComposite), act);
    }

    public void OnKinksterUpdateActiveGag(Action<KinksterUpdateActiveGag> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveGag), act);
    }

    public void OnKinksterUpdateActiveRestriction(Action<KinksterUpdateActiveRestriction> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveRestriction), act);
    }

    public void OnKinksterUpdateActiveRestraint(Action<KinksterUpdateActiveRestraint> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveRestraint), act);
    }

    public void OnKinksterUpdateActiveCollar(Action<KinksterUpdateActiveCollar> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveRestraint), act);
    }

    public void OnKinksterUpdateActiveCursedLoot(Action<KinksterUpdateActiveCursedLoot> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveCursedLoot), act);
    }

    public void OnKinksterUpdateAliasGlobal(Action<KinksterUpdateAliasGlobal> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateAliasGlobal), act);
    }

    public void OnKinksterUpdateAliasUnique(Action<KinksterUpdateAliasUnique> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateAliasUnique), act);
    }

    public void OnKinksterUpdateValidToys(Action<KinksterUpdateValidToys> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateValidToys), act);
    }

    public void OnKinksterUpdateActivePattern(Action<KinksterUpdateActivePattern> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActivePattern), act);
    }

    public void OnKinksterUpdateActiveAlarms(Action<KinksterUpdateActiveAlarms> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveAlarms), act);
    }

    public void OnKinksterUpdateActiveTriggers(Action<KinksterUpdateActiveTriggers> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveTriggers), act);
    }

    public void OnListenerName(Action<UserData, string> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ListenerName), act);
    }

    public void OnShockInstruction(Action<ShockCollarAction> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ShockInstruction), act);
    }

    public void OnHypnoticEffect(Action<HypnoticAction> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_HypnoticEffect), act);
    }

    public void OnKinksterNewGagData(Action<KinksterNewGagData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewGagData), act);
    }

    public void OnKinksterNewRestrictionData(Action<KinksterNewRestrictionData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewRestrictionData), act);
    }

    public void OnKinksterNewRestraintData(Action<KinksterNewRestraintData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewRestraintData), act);
    }

    public void OnKinksterNewCollarData(Action<KinksterNewCollarData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewCollarData), act);
    }

    public void OnKinksterNewLootData(Action<KinksterNewLootData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewLootData), act);
    }

    public void OnKinksterNewPatternData(Action<KinksterNewPatternData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewPatternData), act);
    }

    public void OnKinksterNewAlarmData(Action<KinksterNewAlarmData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewAlarmData), act);
    }

    public void OnKinksterNewTriggerData(Action<KinksterNewTriggerData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewTriggerData), act);
    }

    public void OnKinksterNewAllowances(Action<KinksterNewAllowances> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewAllowances), act);
    }

    public void OnChatMessageGlobal(Action<ChatMessageGlobal> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ChatMessageGlobal), act);
    }

    public void OnKinksterOffline(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterOffline), act);
    }

    public void OnKinksterOnline(Action<OnlineKinkster> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterOnline), act);
    }

    public void OnProfileUpdated(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ProfileUpdated), act);
    }

    public void OnShowVerification(Action<VerificationCode> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ShowVerification), act);
    }


    public void OnRoomJoin(Action<RoomParticipant> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomJoin), act);
    }

    public void OnRoomLeave(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomLeave), act);
    }

    public void OnRoomAddInvite(Action<RoomInvite> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomAddInvite), act);
    }

    public void OnRoomHostChanged(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomHostChanged), act);
    }

    public void OnRoomDeviceUpdate(Action<UserData, ToyInfo> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomDeviceUpdate), act);
    }

    public void OnRoomIncDataStream(Action<ToyDataStreamResponse> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomIncDataStream), act);
    }

    public void OnRoomAccessGranted(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomAccessGranted), act);
    }

    public void OnRoomAccessRevoked(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomAccessRevoked), act);
    }

    public void OnRoomChatMessage(Action<UserData, string> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomChatMessage), act);
    }
}
