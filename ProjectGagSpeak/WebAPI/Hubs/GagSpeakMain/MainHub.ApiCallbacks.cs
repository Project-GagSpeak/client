using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data;
using GagspeakAPI.Dto;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Dto.VibeRoom;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI;

/// <summary> The Callbacks received from the server. </summary>
public partial class MainHub
{
    #region Pairing & Messages
    /// <summary> Called when the server sends a message to the client. </summary>
    public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message)
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

    public Task Client_ReceiveHardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState newServerState)
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
    
    public Task Client_UpdateSystemInfo(SystemInfoDto systemInfo)
    {
        ServerSystemInfo = systemInfo;
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a UserPairDto from one of our connected client pairs.
    /// </summary>
    public Task Client_UserAddClientPair(UserPairDto dto)
    {
        Logger.LogDebug("Client_UserAddClientPair: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.AddNewUserPair(dto));
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a UserDto that is requesting to be removed from our client pairs.
    /// </summary>
    public Task Client_UserRemoveClientPair(UserDto dto)
    {
        Logger.LogDebug("Client_UserRemoveClientPair: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.RemoveUserPair(dto));
        return Task.CompletedTask;
    }

    public Task Client_UserAddPairRequest(UserPairRequestDto dto)
    {
        Logger.LogDebug("Client_UserAddPairRequest: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _globals.AddPairRequest(dto));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Only recieved as a callback from the server when a request has been rejected. Timeouts should be handled on their own.
    /// </summary>
    public Task Client_UserRemovePairRequest(UserPairRequestDto dto)
    {
        Logger.LogDebug("Client_UserRemovePairRequest: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _globals.RemovePairRequest(dto));

        return Task.CompletedTask;
    }

    #endregion Pairing & Messages

    #region Moodles
    public Task Client_UserApplyMoodlesByGuid(ApplyMoodlesByGuidDto dto)
    {
        Logger.LogDebug("Client_UserApplyMoodlesByGuid: "+dto, LoggerType.Callbacks);
        _visualListener.ApplyStatusesByGuid(dto);
        return Task.CompletedTask;
    }

    public Task Client_UserApplyMoodlesByStatus(ApplyMoodlesByStatusDto dto)
    {
        Logger.LogDebug("Client_UserApplyMoodlesByStatus: "+dto, LoggerType.Callbacks);
        // obtain the local player name and world
        var NameWithWorld = _clientMonitor.ClientPlayer.NameWithWorld();
        _visualListener.ApplyStatusesToSelf(dto, NameWithWorld);
        return Task.CompletedTask;
    }

    /// <summary> Intended to clear all moodles from OUR client player. </summary>
    /// <remarks> Should make a call to our moodles IPC to remove the statuses listed by their GUID's </remarks>
    public Task Client_UserRemoveMoodles(RemoveMoodlesDto dto)
    {
        Logger.LogDebug("Client_UserRemoveMoodles: "+dto, LoggerType.Callbacks);
        _visualListener.RemoveStatusesFromSelf(dto);
        return Task.CompletedTask;
    }

    /// <summary> Intended to clear all moodles from OUR client player. </summary>
    /// <remarks> Should make a call to our moodles IPC to clear all statuses. </remarks>
    public Task Client_UserClearMoodles(UserDto dto)
    {
        Logger.LogDebug("Client_UserClearMoodles: "+dto, LoggerType.Callbacks);
        _visualListener.ClearStatusesFromSelf(dto);
        return Task.CompletedTask;
    }
    #endregion Moodles

    #region Pair Permission Exchange
    public Task Client_UserUpdateAllPerms(BulkUpdatePermsAllDto dto)
    {
        if(dto.Direction is UpdateDir.Own)
        {
            Logger.LogError("Should never be calling self for an update all perms.");
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdateAllPerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairAllPermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdateAllGlobalPerms(BulkUpdatePermsGlobalDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdateAllGlobalPerms: " + dto, LoggerType.Callbacks);
            _globals.GlobalPerms = dto.GlobalPermissions;
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdateAllGlobalPerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdatePairUpdateOtherAllGlobalPermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdateAllUniquePerms(BulkUpdatePermsUniqueDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdateAllUniquePerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdatePairUpdateOwnAllUniquePermissions(dto)); return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdateAllUniquePerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdatePairUpdateOtherAllUniquePermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdateGlobalPerm(UserGlobalPermChangeDto dto)
    {
        // Our Client's Global Permissions should be updated.
        if (dto.Direction is UpdateDir.Own)
        {
            // If we were the person who performed this, update the perm. If a pair did it, grab the pair.
            if(dto.Enactor.UID == UID)
            {
                Logger.LogDebug("OWN Client_UserUpdateGlobalPerm (From Self): " + dto, LoggerType.Callbacks);
                ExecuteSafely(() => _globals.ChangeGlobalPermission(dto));
            }
            else
            {
                Logger.LogDebug("OWN Client_UserUpdateGlobalPerm (From Other): " + dto, LoggerType.Callbacks);
                if (_pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == dto.User.UID) is { } pair)
                    ExecuteSafely(() => _globals.ChangeGlobalPermission(dto, pair));
            }
            return Task.CompletedTask;
        }
        // One of our added Kinkster's Global Permissions should be updated.
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdateGlobalPerm: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairGlobalPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdateUniquePerm(UserPairPermChangeDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdateUniquePerm: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateSelfPairPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdateUniquePerm: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdatePermAccess(UserPairAccessChangeDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdatePermAccess: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateSelfPairAccessPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdatePermAccess: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairAccessPermission(dto));
            return Task.CompletedTask;
        }
    }
    #endregion Pair Permission Exchange

    /// <summary> Should only ever get the other pairs. If getting self, something is up. </summary>
    public Task Client_UserReceiveDataComposite(OnlineUserCompositeDataDto dataDto)
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
    public Task Client_UserReceiveDataIpc(CallbackIpcDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("Client_UserReceiveOwnDataIpc (not executing any functions):" + dataDto.User, LoggerType.Callbacks);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataIpc:" + dataDto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveIpcData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataGags(CallbackGagDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataGags:" + dataDto.User, LoggerType.Callbacks);
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
            Logger.LogDebug("OTHER Client_UserReceiveDataGags");
            ExecuteSafely(() => _pairs.ReceiveGagData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataRestrictions(CallbackRestrictionDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataRestrictions:" + dataDto.User, LoggerType.Callbacks);
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
            Logger.LogDebug("OTHER Client_UserReceiveDataRestrictions:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveRestrictionData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataRestraint(CallbackRestraintDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataRestraint:" + dataDto.User, LoggerType.Callbacks);
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
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataRestraint:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaWardrobeData(dataDto));
            return Task.CompletedTask;
        }
    }

    /// <summary> The only condition that we receive this, is if it's for another pair. </summary>
    public Task Client_UserReceiveDataCursedLoot(CallbackCursedLootDto dataDto)
    {
        if(dataDto.User.UID != UID)
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataCursedLoot:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaCursedLootData(dataDto));
            return Task.CompletedTask;
        }
        // Consume any request for the Client.
        return Task.CompletedTask;
    }

    // Do nothing for now because it is not implemented.
    public Task Client_UserReceiveDataOrders(CallbackOrdersDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataOrders:" + dataDto.User, LoggerType.Callbacks);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataOrders:" + dataDto.User, LoggerType.Callbacks);
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataAlias(CallbackAliasDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataAlias:" + dataDto.User, LoggerType.Callbacks);
            _puppetListener.UpdateListener(dataDto.User.UID, dataDto.NewData.ListenerName);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataAlias:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaAliasData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataToybox(CallbackToyboxDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataToybox:" + dataDto.User, LoggerType.Callbacks);
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
            Logger.LogDebug("OTHER Client_UserReceiveDataToybox:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaToyboxData(dataDto));
            return Task.CompletedTask;
        }
    }

    /// <summary> Update The Light Storage data of another pair. </summary>
    public Task Client_UserReceiveLightStorage(CallbackLightStorageDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Other)
        {
            Logger.LogDebug("Client_UserReceiveOtherLightStorage:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaLightStorageData(dataDto));
            return Task.CompletedTask;
        }
        // Consume any request for the Client.
        return Task.CompletedTask;
    }

    /// <summary> Receive a Shock Instruction from another Pair. </summary>
    public Task Client_UserReceiveShockInstruction(PiShockAction dto)
    {
        Logger.LogInformation($"Received Instruction OpCode: {dto.OpCode}, Intensity: {dto.Intensity}, Duration Value: {dto.Duration}");
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


    public Task Client_RoomJoin(VibeRoomKinksterFullDto dto)
    {
        Logger.LogDebug("Client_RoomJoin: "+dto, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Client_RoomLeave(VibeRoomKinksterFullDto dto)
    {
        Logger.LogDebug("Client_RoomLeave: "+dto, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Device Update from a Room. </summary>
    public Task Client_RoomReceiveDeviceUpdate(UserData user, DeviceInfo device)
    {
        Logger.LogDebug("Client_RoomReceiveDeviceUpdate: "+user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Data Stream from a Room. </summary>
    public Task Client_RoomReceiveDataStream(SexToyDataStreamCallbackDto dto)
    {
        Logger.LogDebug("Client_RoomReceiveDataStream: "+dto, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> A User granted us access to control their sex toys. </summary>
    public Task Client_RoomUserAccessGranted(UserData user)
    {
        Logger.LogDebug("Client_RoomUserAccessGranted: "+user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> A User revoked access to their sextoys. </summary>
    public Task Client_RoomUserAccessRevoked(UserData user)
    {
        Logger.LogDebug("Client_RoomUserAccessRevoked: "+user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Chat Message from a Room. </summary>
    public Task Client_RoomReceiveChatMessage(UserData user, string message)
    {
        Logger.LogDebug("Client_RoomReceiveChatMessage: "+user, LoggerType.Callbacks);
        return Task.CompletedTask;
    }


    /// <summary> Receive a Global Chat Message. </summary>
    public Task Client_GlobalChatMessage(GlobalChatMessageDto dto)
    {
        ExecuteSafely(() => Mediator.Publish(new GlobalChatMessage(dto, (dto.MessageSender.UID == UID))));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs disconnects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the UserDto in our pair manager so they are marked as offline. </remarks>
    public Task Client_UserSendOffline(UserDto dto)
    {
        Logger.LogDebug("Client_UserSendOffline: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.MarkPairOffline(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs connects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the UserDto in our pair manager so they are marked as online. </remarks>
    public Task Client_UserSendOnline(OnlineUserIdentDto dto)
    {
        Logger.LogDebug("Client_UserSendOnline: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.MarkPairOnline(dto));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever we need to update the profile data of anyone, including ourselves. </summary>
    public Task Client_UserUpdateProfile(UserDto dto)
    {
        Logger.LogDebug("Client_UserUpdateProfile: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => Mediator.Publish(new ClearProfileDataMessage(dto.User)));
        return Task.CompletedTask;
    }

    /// <summary> The callback responsible for displaying verification codes to the clients monitor. </summary>
    /// <remarks> This is currently experiencing issues for some reason with the discord bot. Look into more? </remarks>
    public Task Client_DisplayVerificationPopup(VerificationDto dto)
    {
        Logger.LogDebug("Client_DisplayVerificationPopup: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => Mediator.Publish(new VerificationPopupMessage(dto)));
        return Task.CompletedTask;
    }

    /* --------------------------------- void methods from the API to call the hooks --------------------------------- */
    public void OnReceiveServerMessage(Action<MessageSeverity, string> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_ReceiveServerMessage), act);
    }

    public void OnReceiveHardReconnectMessage(Action<MessageSeverity, string, ServerState> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_ReceiveHardReconnectMessage), act);
    }

    public void OnUpdateSystemInfo(Action<SystemInfoDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UpdateSystemInfo), act);
    }

    public void OnUserAddClientPair(Action<UserPairDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserAddClientPair), act);
    }

    public void OnUserRemoveClientPair(Action<UserDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserRemoveClientPair), act);
    }

    public void OnUserAddPairRequest(Action<UserPairRequestDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserAddPairRequest), act);
    }

    public void OnUserRemovePairRequest(Action<UserPairRequestDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserRemovePairRequest), act);
    }

    public void OnUserApplyMoodlesByGuid(Action<ApplyMoodlesByGuidDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserApplyMoodlesByGuid), act);
    }

    public void OnUserApplyMoodlesByStatus(Action<ApplyMoodlesByStatusDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserApplyMoodlesByStatus), act);
    }

    public void OnUserRemoveMoodles(Action<RemoveMoodlesDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserRemoveMoodles), act);
    }

    public void OnUserClearMoodles(Action<UserDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserClearMoodles), act);
    }

    public void OnUserUpdateAllPerms(Action<BulkUpdatePermsAllDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateAllPerms), act);
    }

    public void OnUserUpdateAllGlobalPerms(Action<BulkUpdatePermsGlobalDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateAllGlobalPerms), act);
    }

    public void OnUserUpdateAllUniquePerms(Action<BulkUpdatePermsUniqueDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateAllUniquePerms), act);
    }

    public void OnUserUpdateGlobalPerm(Action<UserGlobalPermChangeDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateGlobalPerm), act);
    }

    public void OnUserUpdateUniquePerm(Action<UserPairPermChangeDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateUniquePerm), act);
    }

    public void OnUserUpdatePermAccess(Action<UserPairAccessChangeDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdatePermAccess), act);
    }

    public void OnUserReceiveDataComposite(Action<OnlineUserCompositeDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataComposite), act);
    }

    public void OnUserReceiveDataIpc(Action<CallbackIpcDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataIpc), act);
    }

    public void OnUserReceiveDataGags(Action<CallbackGagDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataGags), act);
    }

    public void OnUserReceiveDataRestrictions(Action<CallbackRestrictionDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataRestrictions), act);
    }

    public void OnUserReceiveDataRestraint(Action<CallbackRestraintDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataRestraint), act);
    }

    public void OnUserReceiveDataCursedLoot(Action<CallbackCursedLootDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataCursedLoot), act);
    }

    public void OnUserReceiveDataOrders(Action<CallbackOrdersDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataOrders), act);
    }


    public void OnUserReceiveDataAlias(Action<CallbackAliasDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataAlias), act);
    }

    public void OnUserReceiveDataToybox(Action<CallbackToyboxDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataToybox), act);
    }

    public void OnUserReceiveLightStorage(Action<CallbackLightStorageDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveLightStorage), act);
    }

    public void OnUserReceiveShockInstruction(Action<PiShockAction> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveShockInstruction), act);
    }

    public void OnRoomJoin(Action<VibeRoomKinksterFullDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_RoomJoin), act);
    }

    public void OnRoomLeave(Action<VibeRoomKinksterFullDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_RoomLeave), act);
    }

    public void OnRoomReceiveDeviceUpdate(Action<UserData, DeviceInfo> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_RoomReceiveDeviceUpdate), act);
    }

    public void OnRoomReceiveDataStream(Action<SexToyDataStreamCallbackDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_RoomReceiveDataStream), act);
    }

    public void OnRoomUserAccessGranted(Action<UserData> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_RoomUserAccessGranted), act);
    }

    public void OnRoomUserAccessRevoked(Action<UserData> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_RoomUserAccessRevoked), act);
    }

    public void OnRoomReceiveChatMessage(Action<UserData, string> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_RoomReceiveChatMessage), act);
    }

    public void OnGlobalChatMessage(Action<GlobalChatMessageDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_GlobalChatMessage), act);
    }

    public void OnUserSendOffline(Action<UserDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserSendOffline), act);
    }

    public void OnUserSendOnline(Action<OnlineUserIdentDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserSendOnline), act);
    }

    public void OnUserUpdateProfile(Action<UserDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateProfile), act);
    }

    public void OnDisplayVerificationPopup(Action<VerificationDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_DisplayVerificationPopup), act);
    }
}
