using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI;

/// <summary> The Callbacks received from the server. </summary>
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
                if (_suppresssNextNotification)
                {
                    _suppresssNextNotification = false;
                    break;
                }
                Mediator.Publish(new NotificationMessage("Info from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Info, TimeSpan.FromSeconds(7.5)));
                break;
        }
        // return it as a completed task.
        return Task.CompletedTask;
    }

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
                if (_suppresssNextNotification)
                {
                    _suppresssNextNotification = false;
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
                _suppresssNextNotification = true;
                // create a new connection to force the disconnect.
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);

                // because this is a forced reconnection, clear our token cache between, incase we were banned.
                _tokenProvider.ResetTokenCache();

                // after it stops, switch the connection pause back to false and create a new connection.
                _serverConfigs.ServerStorage.FullPause = false;
                _serverConfigs.Save();
                _suppresssNextNotification = true;
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

    /// <summary> 
    /// Server has sent us a KinksterPair from one of our connected client pairs.
    /// </summary>
    public Task Callback_AddClientPair(KinksterPair dto)
    {
        Logger.LogDebug("Callback_AddClientPair: "+dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.AddNewKinksterPair(dto));
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a KinksterBase that is requesting to be removed from our client pairs.
    /// </summary>
    public Task Callback_RemoveClientPair(KinksterBase dto)
    {
        Logger.LogDebug("Callback_RemoveClientPair: "+dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.RemoveUserPair(dto));
        return Task.CompletedTask;
    }

    public Task Callback_AddPairRequest(KinksterRequestEntry dto)
    {
        Logger.LogDebug("Callback_AddPairRequest: "+dto, LoggerType.Callbacks);
        Generic.Safe(() => _requests.AddPairRequest(dto));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Only recieved as a callback from the server when a request has been rejected. Timeouts should be handled on their own.
    /// </summary>
    public Task Callback_RemovePairRequest(KinksterRequestEntry dto)
    {
        Logger.LogDebug("Callback_RemovePairRequest: "+dto, LoggerType.Callbacks);
        Generic.Safe(() => _requests.RemovePairRequest(dto));
        return Task.CompletedTask;
    }

    #endregion Pairing & Messages

    #region Moodles
    public Task Callback_ApplyMoodlesByGuid(MoodlesApplierById dto)
    {
        Logger.LogDebug("Callback_ApplyMoodlesByGuid: "+dto, LoggerType.Callbacks);
        _visualListener.ApplyStatusesByGuid(dto);
        return Task.CompletedTask;
    }

    public Task Callback_ApplyMoodlesByStatus(MoodlesApplierByStatus dto)
    {
        Logger.LogDebug("Callback_ApplyMoodlesByStatus: "+dto, LoggerType.Callbacks);
        // obtain the local player name and world
        _visualListener.ApplyStatusesToSelf(dto, PlayerData.NameWithWorldInstanced);
        Logger.LogDebug("Applied Moodles to Self: " + dto, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Intended to clear all moodles from OUR client player. </summary>
    /// <remarks> Should make a call to our moodles IPC to remove the statuses listed by their GUID's </remarks>
    public Task Callback_RemoveMoodles(MoodlesRemoval dto)
    {
        Logger.LogDebug("Callback_RemoveMoodles: "+dto, LoggerType.Callbacks);
        _visualListener.RemoveStatusesFromSelf(dto);
        return Task.CompletedTask;
    }

    /// <summary> Intended to clear all moodles from OUR client player. </summary>
    /// <remarks> Should make a call to our moodles IPC to clear all statuses. </remarks>
    public Task Callback_ClearMoodles(KinksterBase dto)
    {
        Logger.LogDebug("Callback_ClearMoodles: "+dto, LoggerType.Callbacks);
        _visualListener.ClearStatusesFromSelf(dto);
        return Task.CompletedTask;
    }
    #endregion Moodles

    #region Pair Permission Exchange
    public Task Callback_BulkChangeAll(BulkChangeAll dto)
    {
        if(dto.User.UID == UID)
        {
            Logger.LogError("Should never be calling self for an update all perms.");
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_BulkChangeAll: " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdateOtherPairAllPermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_BulkChangeGlobal(BulkChangeGlobal dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogWarning("Called Back BulkChangeGlobal that was intended for yourself!: " + dto);
            Generic.Safe(() => _globalPerms.ApplyBulkChange(dto.NewPerms, dto.User.UID));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_BulkChangeGlobal: " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdatePairUpdateOtherAllGlobalPermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_BulkChangeUnique(BulkChangeUnique dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogWarning("Called Back BulkChangeUnique that was intended for yourself!: " + dto);
            Generic.Safe(() => _kinksters.UpdatePairUpdateOwnAllUniquePermissions(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_BulkChangeUnique: " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdatePairUpdateOtherAllUniquePermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeGlobal(SingleChangeGlobal dto)
    {
        // Our Client's Global Permissions should be updated.
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN SingleChangeGlobal (From Self): " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _globalPerms.SingleGlobalPermissionChange(dto));
            return Task.CompletedTask;
        }
        // One of our added Kinkster's Global Permissions should be updated.
        else
        {
            Logger.LogDebug("OWN SingleChangeGlobal (From Other): " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdateOtherPairGlobalPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeUnique(SingleChangeUnique dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Callback_SingleChangeUnique: " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdateSelfPairPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_SingleChangeUnique: " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdateOtherPairPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeAccess(SingleChangeAccess dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Callback_SingleChangeAccess: " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdateSelfPairAccessPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_SingleChangeAccess: " + dto, LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.UpdateOtherPairAccessPermission(dto));
            return Task.CompletedTask;
        }
    }
    #endregion Pair Permission Exchange

    /// <summary> Should only ever get the other pairs. If getting self, something is up. </summary>
    public Task Callback_KinksterUpdateComposite(KinksterUpdateComposite dto)
    {
        if (dto.User.UID != UID)
        {
            Logger.LogDebug($"User {dto.User.AliasOrUID} went online and updated you with their composite data!", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveComposite(dto.User, dto.Data, dto.WasSafeword));
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    /// <summary> Update Other UserPair Ipc Data </summary>
    public Task Callback_KinksterUpdateIpc(KinksterUpdateIpc dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"Callback_ReceiveOwnDataIpc (not executing any functions): {dto.User.AliasOrUID}", LoggerType.Callbacks);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"OTHER Callback_ReceiveDataIpc: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveIpc(dto.User, dto.Enactor, dto.NewData, dto.Type));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveGag(KinksterUpdateActiveGag dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"OWN Callback_ReceiveDataGags: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                    _visualListener.SwapOrApplyGag(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.Applied:
                    _visualListener.ApplyGag(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _visualListener.LockGag(dataDto);
                    break;
                case DataUpdateType.Unlocked:
                    _visualListener.UnlockGag(dataDto);
                    break;
                case DataUpdateType.Removed:
                    _visualListener.RemoveGag(dataDto).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_ReceiveDataGags");
            Generic.Safe(() => _kinksterListener.NewActiveGags(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveRestriction(KinksterUpdateActiveRestriction dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug("OWN Callback_ReceiveDataRestrictions:" + dataDto.User, LoggerType.Callbacks);
            Logger.LogDebug("Internal New Restriction Data: " +
                $"Identifier: {dataDto.NewData.Identifier}, " +
                $"Padlock: {dataDto.NewData.Padlock}, " +
                $"Timer: {dataDto.NewData.Timer - DateTimeOffset.UtcNow}, " +
                $"Password: {dataDto.NewData.Password}, " +
                $"PadlockAssigner: {dataDto.NewData.PadlockAssigner}, ");

            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    _visualListener.SwapOrApplyRestriction(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _visualListener.LockRestriction(dataDto);
                    break;
                case DataUpdateType.Unlocked:
                    _visualListener.UnlockRestriction(dataDto);
                    break;
                case DataUpdateType.Removed:
                    _visualListener.RemoveRestriction(dataDto).ConfigureAwait(false);
                    break;
            }
           return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_ReceiveDataRestrictions:" + dataDto.User, LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveRestriction(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveRestraint(KinksterUpdateActiveRestraint dataDto)
    {
        // If the update is for us, handle it.
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug("OWN Callback_ReceiveDataRestraint:" + dataDto.User, LoggerType.Callbacks);
            switch(dataDto.Type)
            {
                case DataUpdateType.Swapped:
                    _visualListener.SwapOrApplyRestraint(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.Applied:
                    _visualListener.ApplyRestraint(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.LayersChanged:
                    _visualListener.SwapRestraintLayers(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.LayersApplied:
                    _visualListener.ApplyRestraintLayers(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _visualListener.LockRestraint(dataDto);
                    break;
                case DataUpdateType.Unlocked:
                    _visualListener.UnlockRestraint(dataDto);
                    break;
                case DataUpdateType.LayersRemoved:
                    _visualListener.RemoveRestraintLayers(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.Removed:
                    _visualListener.RemoveRestraint(dataDto).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        // Update was for pair, so handle that.
        else
        {
            Logger.LogDebug("OTHER Callback_ReceiveDataRestraint:" + dataDto.User, LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveRestraint(dataDto));
            return Task.CompletedTask;
        }
    }

    /// <summary> The only condition that we receive this, is if it's for another pair. </summary>
    public Task Callback_KinksterUpdateActiveCursedLoot(KinksterUpdateActiveCursedLoot dataDto)
    {
        if(dataDto.User.UID != UID)
        {
            Logger.LogDebug("OTHER Callback_ReceiveDataCursedLoot:" + dataDto.User, LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActiveCursedLoot(dataDto));
            return Task.CompletedTask;
        }
        // Consume any request for the Client.
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

    public Task Callback_KinksterUpdateActivePattern(KinksterUpdateActivePattern dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"OWN Callback_ReceiveDataToybox: {dto.User.AliasOrUID}", LoggerType.Callbacks);
            switch (dto.Type)
            {
                case DataUpdateType.PatternSwitched:
                    _kinkListener.PatternSwitched(dto.ActivePattern, dto.Enactor.UID);
                    break;
                case DataUpdateType.PatternExecuted:
                    _kinkListener.PatternStarted(dto.ActivePattern, dto.Enactor.UID);
                    break;
                case DataUpdateType.PatternStopped:
                    _kinkListener.PatternStopped(dto.ActivePattern, dto.Enactor.UID);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"OTHER Callback_ReceiveDataToybox: {dto.User.AliasOrUID}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksterListener.NewActivePattern(dto));
            return Task.CompletedTask;
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
            _kinkListener.AlarmToggled(dto.ChangedItem, dto.Enactor.UID);
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
            _kinkListener.TriggerToggled(dto.ChangedItem, dto.Enactor.UID);
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
        Logger.LogDebug("Received a Kinksters updated Global AliasTrigger" + user, LoggerType.Callbacks);
        Generic.Safe(() => _kinksterListener.NewListenerName(user, trueNameWithWorld));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Shock Instruction from another Pair. </summary>
    public Task Callback_ShockInstruction(ShockCollarAction dto)
    {
        Generic.Safe(() => _globalPerms.ExecutePiShockAction(dto));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated GagData change. </summary>
    public Task Callback_KinksterNewGagData(KinksterNewGagData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedGagDataChange(dto.User, dto.GagType, dto.Item));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated RestrictionData change. </summary>
    public Task Callback_KinksterNewRestrictionData(KinksterNewRestrictionData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedRestrictionDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated RestraintData change. </summary>
    public Task Callback_KinksterNewRestraintData(KinksterNewRestraintData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedRestraintDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated CursedLootData change. </summary>
    public Task Callback_KinksterNewLootData(KinksterNewLootData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedCursedLootDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated PatternData change. </summary>
    public Task Callback_KinksterNewPatternData(KinksterNewPatternData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedPatternDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated AlarmData change. </summary>
    public Task Callback_KinksterNewAlarmData(KinksterNewAlarmData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedAlarmDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated TriggerData change. </summary>
    public Task Callback_KinksterNewTriggerData(KinksterNewTriggerData dto)
    {
        Generic.Safe(() => _kinksterListener.CachedTriggerDataChange(dto.User, dto.ItemId, dto.LightItem));
        return Task.CompletedTask;
    }

    /// <summary> Recieve a Kinkster's updated TriggerData change. </summary>
    public Task Callback_KinksterNewAllowances(KinksterNewAllowances dto)
    {
        Generic.Safe(() => _kinksterListener.CachedAllowancesChange(dto.User, dto.Module, [ ..dto.AllowedUids ]));
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
        Logger.LogDebug("Callback_SendOffline: "+dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.MarkKinksterOffline(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs connects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the KinksterBase in our pair manager so they are marked as online. </remarks>
    public Task Callback_KinksterOnline(OnlineKinkster dto)
    {
        Logger.LogDebug("Callback_SendOnline: "+dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.MarkKinksterOnline(dto));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever we need to update the profile data of anyone, including ourselves. </summary>
    public Task Callback_ProfileUpdated(KinksterBase dto)
    {
        Logger.LogDebug("Callback_UpdateProfile: "+dto, LoggerType.Callbacks);
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
        _kinkListener.KinksterJoinedRoom(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomLeave(UserData dto)
    {
        Logger.LogDebug("Callback_RoomLeave: " + dto, LoggerType.Callbacks);
        _kinkListener.KinksterLeftRoom(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomAddInvite(RoomInvite dto)
    {
        Logger.LogDebug("Callback_RoomAddInvite: " + dto, LoggerType.Callbacks);
        _kinkListener.VibeRoomInviteRecieved(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomHostChanged(UserData dto)
    {
        Logger.LogDebug("Callback_RoomHostChanged: " + dto, LoggerType.Callbacks);
        _kinkListener.VibeRoomHostChanged(dto);
        return Task.CompletedTask;
    }



    /// <summary> Receive a Device Update from a Room. </summary>
    public Task Callback_RoomDeviceUpdate(UserData user, ToyInfo device)
    {
        Logger.LogDebug("Callback_RoomDeviceUpdate: " + user, LoggerType.Callbacks);
        _kinkListener.KinksterUpdatedDevice(user, device);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Data Stream from a Room. </summary>
    public Task Callback_RoomIncDataStream(ToyDataStreamResponse dto)
    {
        Logger.LogDebug("Callback_RoomIncDataStream: " + dto, LoggerType.Callbacks); 
        _kinkListener.RecievedBuzzToyDataStream(dto);
        return Task.CompletedTask;
    }

    /// <summary> A User granted us access to control their sex toys. </summary>
    public Task Callback_RoomAccessGranted(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessGranted: " + user, LoggerType.Callbacks);
        _kinkListener.KinksterGrantedAccess(user);
        return Task.CompletedTask;
    }

    /// <summary> A User revoked access to their sextoys. </summary>
    public Task Callback_RoomAccessRevoked(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessRevoked: " + user, LoggerType.Callbacks);
        _kinkListener.KinksterRevokedAccess(user);
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

    public void OnAddPairRequest(Action<KinksterRequestEntry> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_AddPairRequest), act);
    }

    public void OnRemovePairRequest(Action<KinksterRequestEntry> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemovePairRequest), act);
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

    public void OnBulkChangeAll(Action<BulkChangeAll> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_BulkChangeAll), act);
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

    public void OnKinksterUpdateComposite(Action<KinksterUpdateComposite> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateComposite), act);
    }

    public void OnKinksterUpdateIpc(Action<KinksterUpdateIpc> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateIpc), act);
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
