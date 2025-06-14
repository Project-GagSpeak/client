using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
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
                if (SuppressNextNotification)
                {
                    SuppressNextNotification = false;
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
                if (SuppressNextNotification)
                {
                    SuppressNextNotification = false;
                    break;
                }
                Mediator.Publish(new NotificationMessage("Info from " +
                    _serverConfigs.ServerStorage.ServerName, message, NotificationType.Info, TimeSpan.FromSeconds(5)));
                break;
        }
        // we need to update the api server state to be stopped if connected
        if (ServerStatus is ServerState.Connected)
        {
            _ = Task.Run(async () =>
            {
                // pause the server state
                _serverConfigs.ServerStorage.FullPause = true;
                _serverConfigs.Save();
                SuppressNextNotification = true;
                // create a new connection to force the disconnect.
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);

                // because this is a forced reconnection, clear our token cache between, incase we were banned.
                _tokenProvider.ResetTokenCache();

                // after it stops, switch the connection pause back to false and create a new connection.
                _serverConfigs.ServerStorage.FullPause = false;
                _serverConfigs.Save();
                SuppressNextNotification = true;
                await Connect().ConfigureAwait(false);
            });
        }
        // return completed
        return Task.CompletedTask;
    }
    
    public Task Callback_ServerInfo(ServerInfoResponse serverInfo)
    {
        ServerInfo = serverInfo;
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a KinksterPair from one of our connected client pairs.
    /// </summary>
    public Task Callback_AddClientPair(KinksterPair dto)
    {
        Logger.LogDebug("Callback_AddClientPair: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.AddNewUserPair(dto));
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a KinksterBase that is requesting to be removed from our client pairs.
    /// </summary>
    public Task Callback_RemoveClientPair(KinksterBase dto)
    {
        Logger.LogDebug("Callback_RemoveClientPair: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.RemoveUserPair(dto));
        return Task.CompletedTask;
    }

    public Task Callback_AddPairRequest(KinksterRequestEntry dto)
    {
        Logger.LogDebug("Callback_AddPairRequest: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _requests.AddPairRequest(dto));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Only recieved as a callback from the server when a request has been rejected. Timeouts should be handled on their own.
    /// </summary>
    public Task Callback_RemovePairRequest(KinksterRequestEntry dto)
    {
        Logger.LogDebug("Callback_RemovePairRequest: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _requests.RemovePairRequest(dto));

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
        var NameWithWorld = _player.ClientPlayer.NameWithWorld();
        _visualListener.ApplyStatusesToSelf(dto, NameWithWorld);
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
        if(dto.User.UID == MainHub.UID)
        {
            Logger.LogError("Should never be calling self for an update all perms.");
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_BulkChangeAll: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairAllPermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_BulkChangeGlobal(BulkChangeGlobal dto)
    {
        if (dto.User.UID == MainHub.UID)
        {
            Logger.LogWarning("Called Back BulkChangeGlobal that was intended for yourself!: " + dto);
            ExecuteSafely(() => _globalListener.BulkGlobalPermissionUpdate(dto.NewPerms, dto.User.UID));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_BulkChangeGlobal: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdatePairUpdateOtherAllGlobalPermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_BulkChangeUnique(BulkChangeUnique dto)
    {
        if (dto.User.UID == MainHub.UID)
        {
            Logger.LogWarning("Called Back BulkChangeUnique that was intended for yourself!: " + dto);
            ExecuteSafely(() => _pairs.UpdatePairUpdateOwnAllUniquePermissions(dto)); return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_BulkChangeUnique: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdatePairUpdateOtherAllUniquePermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeGlobal(SingleChangeGlobal dto)
    {
        // Our Client's Global Permissions should be updated.
        if (dto.Direction is UpdateDir.Own)
        {
            // If we were the person who performed this, update the perm. If a pair did it, grab the pair.
            if(dto.Enactor.UID == UID)
            {
                Logger.LogDebug("OWN Callback_SingleChangeGlobal (From Self): " + dto, LoggerType.Callbacks);
                ExecuteSafely(() => _globals.ChangeGlobalPermission(dto));
            }
            else
            {
                Logger.LogDebug("OWN Callback_SingleChangeGlobal (From Other): " + dto, LoggerType.Callbacks);
                if (_pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == dto.User.UID) is { } pair)
                    ExecuteSafely(() => _globals.ChangeGlobalPermission(dto, pair));
            }
            return Task.CompletedTask;
        }
        // One of our added Kinkster's Global Permissions should be updated.
        else
        {
            Logger.LogDebug("OTHER Callback_SingleChangeGlobal: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairGlobalPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeUnique(SingleChangeUnique dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Callback_SingleChangeUnique: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateSelfPairPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_SingleChangeUnique: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeAccess(SingleChangeAccess dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Callback_SingleChangeAccess: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateSelfPairAccessPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_SingleChangeAccess: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairAccessPermission(dto));
            return Task.CompletedTask;
        }
    }
    #endregion Pair Permission Exchange

    /// <summary> Should only ever get the other pairs. If getting self, something is up. </summary>
    public Task Callback_KinksterUpdateComposite(KinksterUpdateComposite dataDto)
    {
        if (dataDto.User.UID != MainHub.UID)
        {
            Logger.LogDebug("User "+ dataDto.User.UID+" has went online and updated you with their composite data!", LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCompositeData(dataDto, UID));
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    /// <summary> Update Other UserPair Ipc Data </summary>
    public Task Callback_KinksterUpdateIpc(KinksterUpdateIpc dataDto)
    {
        if (dataDto.User.UID == MainHub.UID)
        {
            Logger.LogDebug("Callback_ReceiveOwnDataIpc (not executing any functions):" + dataDto.User, LoggerType.Callbacks);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_ReceiveDataIpc:" + dataDto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveIpcData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateGagSlot(KinksterUpdateGagSlot dataDto)
    {
        if (dataDto.User.UID == MainHub.UID)
        {
            Logger.LogDebug("OWN Callback_ReceiveDataGags:" + dataDto.User, LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    _visualListener.SwapOrApplyGag(dataDto).ConfigureAwait(false);
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
            ExecuteSafely(() => _pairs.ReceiveGagData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateRestriction(KinksterUpdateRestriction dataDto)
    {
        if (dataDto.User.UID == MainHub.UID)
        {
            Logger.LogDebug("OWN Callback_ReceiveDataRestrictions:" + dataDto.User, LoggerType.Callbacks);
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
            ExecuteSafely(() => _pairs.ReceiveRestrictionData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateRestraint(KinksterUpdateRestraint dataDto)
    {
        // If the update is for us, handle it.
        if (dataDto.User.UID == MainHub.UID)
        {
            Logger.LogDebug("OWN Callback_ReceiveDataRestraint:" + dataDto.User, LoggerType.Callbacks);
            switch(dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    _visualListener.SwapOrApplyRestraint(dataDto).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _visualListener.LockRestraint(dataDto);
                    break;
                case DataUpdateType.Unlocked:
                    _visualListener.UnlockRestraint(dataDto);
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
            ExecuteSafely(() => _pairs.ReceiveCharaWardrobeData(dataDto));
            return Task.CompletedTask;
        }
    }

    /// <summary> The only condition that we receive this, is if it's for another pair. </summary>
    public Task Callback_KinksterUpdateCursedLoot(KinksterUpdateCursedLoot dataDto)
    {
        if(dataDto.User.UID != UID)
        {
            Logger.LogDebug("OTHER Callback_ReceiveDataCursedLoot:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaCursedLootData(dataDto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogWarning("Consuming Callback_ReceiveDataCursedLoot for self, this should never happen! " + dataDto.User, LoggerType.Callbacks);
        }
        // Consume any request for the Client.
        return Task.CompletedTask;
    }

    public Task Callback_KinksterUpdateToybox(KinksterUpdateToybox dataDto)
    {
        if (dataDto.User.UID == MainHub.UID)
        {
            Logger.LogDebug("OWN Callback_ReceiveDataToybox:" + dataDto.User, LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.PatternSwitched:
                    _kinkListener.PatternSwitched(dataDto.InteractedIdentifier, dataDto.Enactor.UID);
                    break;
                case DataUpdateType.PatternExecuted:
                    _kinkListener.PatternStarted(dataDto.InteractedIdentifier, dataDto.Enactor.UID);
                    break;
                case DataUpdateType.PatternStopped:
                    _kinkListener.PatternStopped(dataDto.InteractedIdentifier, dataDto.Enactor.UID);
                    break;
                case DataUpdateType.AlarmToggled:
                    _kinkListener.AlarmToggled(dataDto.InteractedIdentifier, dataDto.Enactor.UID);
                    break;
                case DataUpdateType.TriggerToggled:
                    _kinkListener.TriggerToggled(dataDto.InteractedIdentifier, dataDto.Enactor.UID);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Callback_ReceiveDataToybox:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaToyboxData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateAliasGlobal(KinksterUpdateAliasGlobal dataDto)
    {
        Logger.LogDebug("Received a Kinksters updated Global AliasTrigger" + dataDto.User, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.ReceiveCharaAliasGlobalUpdate(dataDto));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterUpdateAliasUnique(KinksterUpdateAliasUnique dataDto)
    {
        Logger.LogDebug("Received a Kinksters updated Global AliasTrigger" + dataDto.User, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.ReceiveCharaAliasPairUpdate(dataDto));
        return Task.CompletedTask;
    }

    /// <summary> Update The Light Storage data of another pair. </summary>
    public Task Callback_KinksterUpdateLightStorage(KinksterUpdateLightStorage dataDto)
    {
        if (dataDto.User.UID != MainHub.UID)
        {
            Logger.LogDebug("Callback_ReceiveOtherLightStorage:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaLightStorageData(dataDto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogWarning("Consuming Callback_ReceiveLightStorage for self, this should never happen! " + dataDto.User, LoggerType.Callbacks);
        }
        // Consume any request for the Client.
        return Task.CompletedTask;
    }

    public Task Callback_ListenerName(UserData user, string trueNameWithWorld)
    {
        Logger.LogDebug("Received a Kinksters updated Global AliasTrigger" + user, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.ReceiveListenerName(user, trueNameWithWorld));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Shock Instruction from another Pair. </summary>
    public Task Callback_ShockInstruction(ShockCollarAction dto)
    {
        ExecuteSafely(() =>
        {
            // figure out who sent the command, and see if we have a unique sharecode setup for them.
            var pairMatch = _pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == dto.User.UID);
            if (pairMatch != null) 
            {
                var interactionType = dto.OpCode switch
                {
                    0 => "shocked",
                    1 => "vibrated",
                    2 => "beeped",
                    _ => "unknown"
                };
                var eventLogMessage = $"Pishock {interactionType}, intensity: {dto.Intensity}, duration: {dto.Duration}";
                Logger.LogInformation($"Received Instruction for {eventLogMessage}", LoggerType.Callbacks);

                if (!pairMatch.OwnPerms.PiShockShareCode.IsNullOrEmpty())
                {
                    Logger.LogDebug("Executing Shock Instruction to UniquePair ShareCode", LoggerType.Callbacks);
                    Mediator.Publish(new EventMessage(new(pairMatch.GetNickAliasOrUid(), pairMatch.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
                    Mediator.Publish(new PiShockExecuteOperation(pairMatch.OwnPerms.PiShockShareCode, dto.OpCode, dto.Intensity, dto.Duration));
                    if(dto.OpCode is 0)
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
                }
                else if (_globals.GlobalPerms is not null && !_globals.GlobalPerms.GlobalShockShareCode.IsNullOrEmpty())
                {
                    Logger.LogDebug("Executing Shock Instruction to Global ShareCode", LoggerType.Callbacks);
                    Mediator.Publish(new EventMessage(new(pairMatch.GetNickAliasOrUid(), pairMatch.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
                    Mediator.Publish(new PiShockExecuteOperation(_globals.GlobalPerms.GlobalShockShareCode, dto.OpCode, dto.Intensity, dto.Duration));
                    if (dto.OpCode is 0)
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
                }
                else
                {
                    Logger.LogWarning("Someone Attempted to execute an instruction to you, but you don't have any share codes enabled!");
                }
            }
        });
        return Task.CompletedTask;
    }

    /// <summary> Receive a Global Chat Message. </summary>
    public Task Callback_ChatMessageGlobal(ChatMessageGlobal dto)
    {
        var fromSelf = dto.Sender.UID == MainHub.UID;
        ExecuteSafely(() => Mediator.Publish(new GlobalChatMessage(dto, fromSelf)));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs disconnects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the KinksterBase in our pair manager so they are marked as offline. </remarks>
    public Task Callback_KinksterOffline(KinksterBase dto)
    {
        Logger.LogDebug("Callback_SendOffline: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.MarkPairOffline(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs connects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the KinksterBase in our pair manager so they are marked as online. </remarks>
    public Task Callback_KinksterOnline(OnlineKinkster dto)
    {
        Logger.LogDebug("Callback_SendOnline: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.MarkPairOnline(dto));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever we need to update the profile data of anyone, including ourselves. </summary>
    public Task Callback_ProfileUpdated(KinksterBase dto)
    {
        Logger.LogDebug("Callback_UpdateProfile: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => Mediator.Publish(new ClearProfileDataMessage(dto.User)));
        return Task.CompletedTask;
    }

    /// <summary> The callback responsible for displaying verification codes to the clients monitor. </summary>
    /// <remarks> This is currently experiencing issues for some reason with the discord bot. Look into more? </remarks>
    public Task Callback_ShowVerification(VerificationCode dto)
    {
        Logger.LogDebug("Callback_DisplayVerificationPopup: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => Mediator.Publish(new VerificationPopupMessage(dto)));
        return Task.CompletedTask;
    }

    public Task Callback_RoomJoin(RoomParticipant dto)
    {
        Logger.LogDebug("Callback_RoomJoin: " + dto, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomLeave(RoomParticipant dto)
    {
        Logger.LogDebug("Callback_RoomLeave: " + dto, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Device Update from a Room. </summary>
    public Task Callback_RoomDeviceUpdate(UserData user, ToyInfo device)
    {
        Logger.LogDebug("Callback_RoomDeviceUpdate: " + user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Data Stream from a Room. </summary>
    public Task Callback_RoomIncDataStream(ToyDataStreamResponse dto)
    {
        Logger.LogDebug("Callback_RoomIncDataStream: " + dto, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> A User granted us access to control their sex toys. </summary>
    public Task Callback_RoomAccessGranted(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessGranted: " + user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> A User revoked access to their sextoys. </summary>
    public Task Callback_RoomAccessRevoked(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessRevoked: " + user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Chat Message from a Room. </summary>
    public Task Callback_RoomChatMessage(UserData user, string message)
    {
        Logger.LogDebug("Callback_RoomChatMessage: " + user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /* --------------------------------- void methods from the API to call the hooks --------------------------------- */
    public void OnServerMessage(Action<MessageSeverity, string> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ServerMessage), act);
    }

    public void OnHardReconnectMessage(Action<MessageSeverity, string, ServerState> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_HardReconnectMessage), act);
    }

    public void OnServerInfo(Action<ServerInfoResponse> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ServerInfo), act);
    }

    public void OnAddClientPair(Action<KinksterPair> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_AddClientPair), act);
    }

    public void OnRemoveClientPair(Action<KinksterBase> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RemoveClientPair), act);
    }

    public void OnAddPairRequest(Action<KinksterRequestEntry> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_AddPairRequest), act);
    }

    public void OnRemovePairRequest(Action<KinksterRequestEntry> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RemovePairRequest), act);
    }

    public void OnApplyMoodlesByGuid(Action<MoodlesApplierById> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ApplyMoodlesByGuid), act);
    }

    public void OnApplyMoodlesByStatus(Action<MoodlesApplierByStatus> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ApplyMoodlesByStatus), act);
    }

    public void OnRemoveMoodles(Action<MoodlesRemoval> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RemoveMoodles), act);
    }

    public void OnClearMoodles(Action<KinksterBase> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ClearMoodles), act);
    }

    public void OnBulkChangeAll(Action<BulkChangeAll> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_BulkChangeAll), act);
    }

    public void OnBulkChangeGlobal(Action<BulkChangeGlobal> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_BulkChangeGlobal), act);
    }

    public void OnBulkChangeUnique(Action<BulkChangeUnique> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_BulkChangeUnique), act);
    }

    public void OnSingleChangeGlobal(Action<SingleChangeGlobal> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_SingleChangeGlobal), act);
    }

    public void OnSingleChangeUnique(Action<SingleChangeUnique> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_SingleChangeUnique), act);
    }

    public void OnSingleChangeAccess(Action<SingleChangeAccess> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_SingleChangeAccess), act);
    }

    public void OnKinksterUpdateComposite(Action<KinksterUpdateComposite> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateComposite), act);
    }

    public void OnKinksterUpdateIpc(Action<KinksterUpdateIpc> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateIpc), act);
    }

    public void OnKinksterUpdateGagSlot(Action<KinksterUpdateGagSlot> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateGagSlot), act);
    }

    public void OnKinksterUpdateRestriction(Action<KinksterUpdateRestriction> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateRestriction), act);
    }

    public void OnKinksterUpdateRestraint(Action<KinksterUpdateRestraint> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateRestraint), act);
    }

    public void OnKinksterUpdateCursedLoot(Action<KinksterUpdateCursedLoot> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateCursedLoot), act);
    }

    public void OnKinksterUpdateAliasGlobal(Action<KinksterUpdateAliasGlobal> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateAliasGlobal), act);
    }

    public void OnKinksterUpdateAliasUnique(Action<KinksterUpdateAliasUnique> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateAliasUnique), act);
    }

    public void OnKinksterUpdateToybox(Action<KinksterUpdateToybox> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateToybox), act);
    }

    public void OnKinksterUpdateLightStorage(Action<KinksterUpdateLightStorage> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterUpdateLightStorage), act);
    }

    public void OnListenerName(Action<UserData, string> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ListenerName), act);
    }

    public void OnShockInstruction(Action<ShockCollarAction> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ShockInstruction), act);
    }

    public void OnChatMessageGlobal(Action<ChatMessageGlobal> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ChatMessageGlobal), act);
    }

    public void OnKinksterOffline(Action<KinksterBase> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterOffline), act);
    }

    public void OnKinksterOnline(Action<OnlineKinkster> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_KinksterOnline), act);
    }

    public void OnProfileUpdated(Action<KinksterBase> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ProfileUpdated), act);
    }

    public void OnShowVerification(Action<VerificationCode> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_ShowVerification), act);
    }


    public void OnRoomJoin(Action<RoomParticipant> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RoomJoin), act);
    }

    public void OnRoomLeave(Action<RoomParticipant> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RoomLeave), act);
    }

    public void OnRoomDeviceUpdate(Action<UserData, ToyInfo> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RoomDeviceUpdate), act);
    }

    public void OnRoomIncDataStream(Action<ToyDataStreamResponse> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RoomIncDataStream), act);
    }

    public void OnRoomAccessGranted(Action<UserData> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RoomAccessGranted), act);
    }

    public void OnRoomAccessRevoked(Action<UserData> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RoomAccessRevoked), act);
    }

    public void OnRoomChatMessage(Action<UserData, string> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Callback_RoomChatMessage), act);
    }
}
