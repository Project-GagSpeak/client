using Dalamud.Interface.ImGuiNotification;
using GagspeakAPI.Enums;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.UserPair;
using GagSpeak.Services.Mediator;
using Microsoft.AspNetCore.SignalR.Client;
using GagspeakAPI.Dto.IPC;
using GagSpeak.Utils;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Lua;
using GagSpeak.UpdateMonitoring;

namespace GagSpeak.WebAPI;

/// <summary>
/// The Callbacks received from the server.
/// </summary>
public partial class MainHub
{
    /// <summary> 
    /// Called when the server sends a message to the client. 
    /// </summary>
    public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message)
    {
        switch (messageSeverity)
        {
            case MessageSeverity.Error:
                Mediator.Publish(new NotificationMessage("Error from " +
                    _serverConfigs.CurrentServer!.ServerName, message, NotificationType.Error, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Warning:
                Mediator.Publish(new NotificationMessage("Warning from " +
                    _serverConfigs.CurrentServer!.ServerName, message, NotificationType.Warning, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Information:
                if (SuppressNextNotification)
                {
                    SuppressNextNotification = false;
                    break;
                }
                Mediator.Publish(new NotificationMessage("Info from " +
                    _serverConfigs.CurrentServer!.ServerName, message, NotificationType.Info, TimeSpan.FromSeconds(7.5)));
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
                    _serverConfigs.CurrentServer!.ServerName, message, NotificationType.Error, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Warning:
                Mediator.Publish(new NotificationMessage("Warning from " +
                    _serverConfigs.CurrentServer!.ServerName, message, NotificationType.Warning, TimeSpan.FromSeconds(7.5)));
                break;

            case MessageSeverity.Information:
                if (SuppressNextNotification)
                {
                    SuppressNextNotification = false;
                    break;
                }
                Mediator.Publish(new NotificationMessage("Info from " +
                    _serverConfigs.CurrentServer!.ServerName, message, NotificationType.Info, TimeSpan.FromSeconds(5)));
                break;
        }
        // we need to update the api server state to be stopped if connected
        if (ServerStatus is ServerState.Connected)
        {
            _ = Task.Run(async () =>
            {
                // pause the server state
                _serverConfigs.CurrentServer.FullPause = true;
                _serverConfigs.Save();
                SuppressNextNotification = true;
                // create a new connection to force the disconnect.
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);

                // because this is a forced reconnection, clear our token cache between, incase we were banned.
                _tokenProvider.ResetTokenCache();

                // after it stops, switch the connection pause back to false and create a new connection.
                _serverConfigs.CurrentServer.FullPause = false;
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
        ExecuteSafely(() => _playerData.AddPairRequest(dto));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Only recieved as a callback from the server when a request has been rejected. Timeouts should be handled on their own.
    /// </summary>
    public Task Client_UserRemovePairRequest(UserPairRequestDto dto)
    {
        Logger.LogDebug("Client_UserRemovePairRequest: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _playerData.RemovePairRequest(dto));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Should only be triggered if another pair is toggling on one of your existing moodles.
    /// </summary>
    public Task Client_UserApplyMoodlesByGuid(ApplyMoodlesByGuidDto dto)
    {
        // Should make us call to the moodles IPC to apply the statuses recieved
        // (permissions verified by server)
        Logger.LogDebug("Client_UserApplyMoodlesByGuid: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _clientCallbacks.ApplyStatusesByGuid(dto));
        return Task.CompletedTask;
    }


    public Task Client_UserApplyMoodlesByStatus(ApplyMoodlesByStatusDto dto)
    {
        Logger.LogDebug("Client_UserApplyMoodlesByStatus: "+dto, LoggerType.Callbacks);
        // obtain the local player name and world
        string NameWithWorld = _clientService.ClientPlayer.NameWithWorld();
        ExecuteSafely(() => _clientCallbacks.ApplyStatusesToSelf(dto, NameWithWorld));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Intended to clear all moodles from OUR client player.
    /// Should make a call to our moodles IPC to remove the statuses listed by their GUID's
    /// </summary>
    public Task Client_UserRemoveMoodles(RemoveMoodlesDto dto)
    {
        Logger.LogDebug("Client_UserRemoveMoodles: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _clientCallbacks.RemoveStatusesFromSelf(dto));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Intended to clear all moodles from OUR client player.
    /// Should make a call to our moodles IPC to clear all statuses.
    /// </summary>
    public Task Client_UserClearMoodles(UserDto dto)
    {
        Logger.LogDebug("Client_UserClearMoodles: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _clientCallbacks.ClearStatusesFromSelf(dto));
        return Task.CompletedTask;
    }




    /// <summary>
    /// Sent to client from server informing them to update individual pair status of a client pair
    /// Status should be updated in the pair manager.
    /// </summary>
    public Task Client_UpdateUserIndividualPairStatusDto(UserIndividualPairStatusDto dto)
    {
        Logger.LogDebug("Client_UpdateUserIndividualPairStatusDto: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.UpdateIndividualPairStatus(dto));
        return Task.CompletedTask;
    }

    public Task Client_UserUpdateAllPerms(UserPairUpdateAllPermsDto dto)
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

    public Task Client_UserUpdateAllGlobalPerms(UserPairUpdateAllGlobalPermsDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdateAllGlobalPerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _clientCallbacks.SetGlobalPerms(dto.GlobalPermissions));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdateAllGlobalPerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdatePairUpdateOtherAllGlobalPermissions(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdateAllUniquePerms(UserPairUpdateAllUniqueDto dto)
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

    public Task Client_UserUpdatePairPermsGlobal(UserGlobalPermChangeDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdatePairPermsGlobal: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _clientCallbacks.ApplyGlobalPerm(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdatePairPermsGlobal: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairGlobalPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdatePairPerms(UserPairPermChangeDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdatePairPerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateSelfPairPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdatePairPerms: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairPermission(dto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserUpdatePairPermAccess(UserPairAccessChangeDto dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserUpdatePairPermAccess: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateSelfPairAccessPermission(dto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserUpdatePairPermAccess: " + dto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.UpdateOtherPairAccessPermission(dto));
            return Task.CompletedTask;
        }
    }


    /// <summary> 
    /// Should only ever get the other pairs. If getting self, something is up.
    /// </summary>
    public Task Client_UserReceiveDataComposite(OnlineUserCompositeDataDto dataDto)
    {
        if (dataDto.User.UID == ConnectionDto?.User.UID)
        {
            Logger.LogWarning("Why are you trying to receive your own composite data? There is no need for this???");
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("User "+ dataDto.User.UID+" has went online and updated you with their composite data!", LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaCompositeData(dataDto, UID));
            return Task.CompletedTask;
        }
    }

    /// <summary> 
    /// Update Other UserPair Ipc Data 
    /// </summary>
    public Task Client_UserReceiveDataIpc(OnlineUserCharaIpcDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("Client_UserReceiveOwnDataIpc (not executing any functions):" + dataDto.User, LoggerType.Callbacks);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataIpc:" + dataDto, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaIpcData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataAppearance(OnlineUserCharaAppearanceDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataAppearance:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _clientCallbacks.CallbackAppearanceUpdate(dataDto, dataDto.Enactor.UID == MainHub.UID));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataAppearance");
            ExecuteSafely(() => _pairs.ReceiveCharaAppearanceData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataWardrobe(OnlineUserCharaWardrobeDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataWardrobe:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _clientCallbacks.CallbackWardrobeUpdate(dataDto, dataDto.Enactor.UID == MainHub.UID));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataWardrobe:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaWardrobeData(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Client_UserReceiveDataAlias(OnlineUserCharaAliasDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataAlias:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _clientCallbacks.CallbackAliasStorageUpdate(dataDto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataAlias:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaAliasData(dataDto));
            return Task.CompletedTask;
        }
    }

    /// <summary> 
    /// Update Own UserPair Toybox Data 
    /// </summary>
    public Task Client_UserReceiveDataToybox(OnlineUserCharaToyboxDataDto dataDto)
    {
        if (dataDto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug("OWN Client_UserReceiveDataToybox:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _clientCallbacks.CallbackToyboxUpdate(dataDto, dataDto.Enactor.UID == MainHub.UID));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("OTHER Client_UserReceiveDataToybox:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaToyboxData(dataDto));
            return Task.CompletedTask;
        }
    }

    /// <summary> 
    /// Update Other UserPair Toybox Data 
    /// </summary>
    public Task Client_UserReceiveLightStorage(OnlineUserStorageUpdateDto dataDto)
    {
        Logger.LogDebug("Client_UserReceiveOwnLightStorage:" + dataDto.User, LoggerType.Callbacks);
        if (dataDto.Direction is UpdateDir.Own)
        {
            ExecuteSafely(() => _clientCallbacks.CallbackLightStorageUpdate(dataDto));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug("Client_UserReceiveOtherLightStorage:" + dataDto.User, LoggerType.Callbacks);
            ExecuteSafely(() => _pairs.ReceiveCharaLightStorageData(dataDto));
            return Task.CompletedTask;
        }
    }

    /// <summary> 
    /// Receive a Shock Instruction from another Pair. 
    /// </summary>
    public Task Client_UserReceiveShockInstruction(ShockCollarActionDto dto)
    {
        Logger.LogInformation("Received Instruction from: {dto}" + Environment.NewLine
            + "OpCode: {opcode}, Intensity: {intensity}, Duration Value: {duration}"
            , dto.User.AliasOrUID, dto.OpCode, dto.Intensity, dto.Duration);
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

                if (!pairMatch.OwnPerms.ShockCollarShareCode.IsNullOrEmpty())
                {
                    Logger.LogDebug("Executing Shock Instruction to UniquePair ShareCode", LoggerType.Callbacks);
                    Mediator.Publish(new EventMessage(new(pairMatch.GetNickAliasOrUid(), pairMatch.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
                    Mediator.Publish(new PiShockExecuteOperation(pairMatch.OwnPerms.ShockCollarShareCode, dto.OpCode, dto.Intensity, dto.Duration));
                    if(dto.OpCode is 0)
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
                }
                else if (_clientCallbacks.ShockCodePresent)
                {
                    Logger.LogDebug("Executing Shock Instruction to Global ShareCode", LoggerType.Callbacks);
                    Mediator.Publish(new EventMessage(new(pairMatch.GetNickAliasOrUid(), pairMatch.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
                    Mediator.Publish(new PiShockExecuteOperation(_clientCallbacks.GlobalPiShockShareCode, dto.OpCode, dto.Intensity, dto.Duration));
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


    /// <summary> 
    /// Receive a Global Chat Message. DO NOT LOG THIS. 
    /// </summary>
    public Task Client_GlobalChatMessage(GlobalChatMessageDto dto)
    {
        ExecuteSafely(() => Mediator.Publish(new GlobalChatMessage(dto, (dto.MessageSender.UID == UID))));
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a UserDto has just went offline, and is notifying all connected pairs about it.
    /// <para> 
    /// Use this info to update the UserDto in our pair manager so they are marked as offline.
    /// </para>
    /// </summary>
    public Task Client_UserSendOffline(UserDto dto)
    {
        Logger.LogDebug("Client_UserSendOffline: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.MarkPairOffline(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a OnlineUserIdentDto that is notifying all connected pairs about a user going online.
    /// <para> 
    /// Use this info to update the UserDto in our pair manager so they are marked as online.
    /// </para>
    /// </summary>
    public Task Client_UserSendOnline(OnlineUserIdentDto dto)
    {
        Logger.LogDebug("Client_UserSendOnline: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => _pairs.MarkPairOnline(dto));
        return Task.CompletedTask;
    }

    /// <summary> 
    /// Server has sent us a UserDto from one of our client pairs that has just updated their profile.
    /// <para> 
    /// Use this information to publish a new message to our mediator so update it.
    /// </para>
    /// </summary>
    public Task Client_UserUpdateProfile(UserDto dto)
    {
        Logger.LogDebug("Client_UserUpdateProfile: "+dto, LoggerType.Callbacks);
        ExecuteSafely(() => Mediator.Publish(new ClearProfileDataMessage(dto.User)));
        return Task.CompletedTask;
    }

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

    public void OnUpdateUserIndividualPairStatusDto(Action<UserIndividualPairStatusDto> action)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UpdateUserIndividualPairStatusDto), action);
    }

    public void OnUserUpdateAllPerms(Action<UserPairUpdateAllPermsDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateAllPerms), act);
    }

    public void OnUserUpdateAllGlobalPerms(Action<UserPairUpdateAllGlobalPermsDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateAllGlobalPerms), act);
    }

    public void OnUserUpdateAllUniquePerms(Action<UserPairUpdateAllUniqueDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdateAllUniquePerms), act);
    }

    public void OnUserUpdatePairPermsGlobal(Action<UserGlobalPermChangeDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdatePairPermsGlobal), act);
    }

    public void OnUserUpdatePairPerms(Action<UserPairPermChangeDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdatePairPerms), act);
    }

    public void OnUserUpdatePairPermAccess(Action<UserPairAccessChangeDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserUpdatePairPermAccess), act);
    }

    public void OnUserReceiveDataComposite(Action<OnlineUserCompositeDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataComposite), act);
    }

    public void OnUserReceiveDataIpc(Action<OnlineUserCharaIpcDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataIpc), act);
    }

    public void OnUserReceiveDataAppearance(Action<OnlineUserCharaAppearanceDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataAppearance), act);
    }
    public void OnUserReceiveDataWardrobe(Action<OnlineUserCharaWardrobeDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataWardrobe), act);
    }


    public void OnUserReceiveDataAlias(Action<OnlineUserCharaAliasDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataAlias), act);
    }

    public void OnUserReceiveDataToybox(Action<OnlineUserCharaToyboxDataDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveDataToybox), act);
    }

    public void OnUserReceiveLightStorage(Action<OnlineUserStorageUpdateDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveLightStorage), act);
    }

    public void OnUserReceiveShockInstruction(Action<ShockCollarActionDto> act)
    {
        if (Initialized) return;
        GagSpeakHubMain!.On(nameof(Client_UserReceiveShockInstruction), act);
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
