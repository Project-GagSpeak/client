using GagspeakAPI.Data;
using GagspeakAPI.Dto.Sharehub;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI;

#pragma warning disable MA0040

public partial class MainHub
{
    #region ShareHubs
    public async Task<HubResponse> UploadPattern(PatternUpload dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UploadPattern), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UploadMoodle(MoodleUpload dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UploadMoodle), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse<string>> DownloadPattern(Guid patternId)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, string.Empty);
        return await _hubConnection!.InvokeAsync<HubResponse<string>>(nameof(DownloadPattern), patternId).ConfigureAwait(false);
    }

    public async Task<HubResponse> LikePattern(Guid patternId)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(LikePattern), patternId).ConfigureAwait(false);
    }

    public async Task<HubResponse> LikeMoodle(Guid moodleId)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(LikeMoodle), moodleId).ConfigureAwait(false);
    }

    public async Task<HubResponse> RemovePattern(Guid patternId)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RemovePattern), patternId).ConfigureAwait(false);
    }

    public async Task<HubResponse> RemoveMoodle(Guid moodleId)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RemoveMoodle), moodleId).ConfigureAwait(false);
    }

    public async Task<HubResponse<List<ServerPatternInfo>>> SearchPatterns(PatternSearch patternSearchDto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new List<ServerPatternInfo>());
        return await _hubConnection!.InvokeAsync<HubResponse<List<ServerPatternInfo>>>(nameof(SearchPatterns), patternSearchDto).ConfigureAwait(false);
    }

    public async Task<HubResponse<List<ServerMoodleInfo>>> SearchMoodles(SearchBase moodleSearchDto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new List<ServerMoodleInfo>());
        return await _hubConnection!.InvokeAsync<HubResponse<List<ServerMoodleInfo>>>(nameof(SearchMoodles), moodleSearchDto).ConfigureAwait(false);
    }

    public async Task<HubResponse<HashSet<string>>> FetchSearchTags()
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new HashSet<string>());
        return await _hubConnection!.InvokeAsync<HubResponse<HashSet<string>>>(nameof(FetchSearchTags)).ConfigureAwait(false);
    }
    #endregion ShareHubs

    #region Client Vanity
    public async Task<HubResponse> UserSendGlobalChat(ChatMessageGlobal dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserSendGlobalChat), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserUpdateAchievementData(AchievementsUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);

        // Achievement Syncronization without resets is a pain in the ass :)
        try
        {
            return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserUpdateAchievementData), dto).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            // Handle the operation canceled exception
            Svc.Logger.Error($"Operation was canceled while updating achievement data: {ex}");
        }
        catch (HubException ex)
        {
            // Handle SignalR hub exceptions
            Svc.Logger.Error($"Operation was canceled while updating achievement data: {ex}");
        }
        catch (Exception ex)
        {
            // Handle any other exceptions
            Svc.Logger.Error($"An unexpected error occurred while updating achievement data: {ex}");
        }
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
    }

    public async Task<HubResponse> UserSetKinkPlateContent(KinkPlateInfo kinkPlateInfo)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserSetKinkPlateContent), kinkPlateInfo).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserSetKinkPlatePicture(KinkPlateImage kinkPlateImage)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserSetKinkPlatePicture), kinkPlateImage).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserReportKinkPlate(KinkPlateReport userProfileDto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserReportKinkPlate), userProfileDto).ConfigureAwait(false);
    }
    #endregion Client Vanity

    #region Personal Interactions
    public async Task<HubResponse> UserPushData(PushClientCompositeUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushData), dto).ConfigureAwait(false);

    }

    public async Task<HubResponse> UserPushDataIpc(PushClientIpcUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushDataIpc), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushDataGags(PushClientGagSlotUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushDataGags), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushDataRestrictions(PushClientRestrictionUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushDataRestrictions), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushDataRestraint(PushClientRestraintUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushDataRestraint), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushDataCursedLoot(PushClientCursedLootUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushDataCursedLoot), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushAliasGlobalUpdate(PushClientAliasGlobalUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushAliasGlobalUpdate), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushAliasUniqueUpdate(PushClientAliasUniqueUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushAliasUniqueUpdate), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushDataToybox(PushClientToyboxUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushDataToybox), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushDataLightStorage(PushClientLightStorageUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushDataLightStorage), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserBulkChangeGlobal(BulkChangeGlobal allGlobalPerms)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserBulkChangeGlobal), allGlobalPerms);
    }

    public async Task<HubResponse> UserBulkChangeUnique(BulkChangeUnique allUniquePermsForPair)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserBulkChangeUnique), allUniquePermsForPair);
    }

    public async Task<HubResponse> UserChangeOwnGlobalPerm(string propertyName, object newValue)
        => await UserChangeOwnGlobalPerm(new(PlayerUserData, new KeyValuePair<string, object>(propertyName, newValue), UpdateDir.Own, PlayerUserData));


    public async Task<HubResponse> UserChangeOwnGlobalPerm(SingleChangeGlobal userPermissions)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOwnGlobalPerm), userPermissions);
    }

    public async Task<HubResponse> UserChangeOwnPairPerm(SingleChangeUnique userPermissions)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOwnPairPerm), userPermissions);
    }

    public async Task<HubResponse> UserChangeOwnPairPermAccess(SingleChangeAccess userPermissions)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOwnPairPermAccess), userPermissions);
    }

    public async Task<HubResponse> UserDelete()
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserDelete)).ConfigureAwait(false);
    }

    #endregion Personal Interactions

    #region Kinkster Interactions
    public async Task<HubResponse> UserSendKinksterRequest(CreateKinksterRequest request)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserSendKinksterRequest), request).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserCancelKinksterRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserCancelKinksterRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserAcceptKinksterRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserAcceptKinksterRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserRejectKinksterRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserRejectKinksterRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserRemoveKinkster(KinksterBase KinksterBase)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserRemoveKinkster), KinksterBase).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterGagState(PushKinksterGagSlotUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterGagState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterRestrictionState(PushKinksterRestrictionUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterRestrictionState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterRestraintState(PushKinksterRestraintUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterRestraintState), dto);
    }

    public async Task<HubResponse> UserChangeKinksterToyboxState(PushKinksterToyboxUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterToyboxState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserSendNameToKinkster(KinksterBase recipient, string listenerName)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserSendNameToKinkster), recipient, listenerName).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeOtherGlobalPerm(SingleChangeGlobal userPermissions)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOtherGlobalPerm), userPermissions);
    }

    public async Task<HubResponse> UserChangeOtherPairPerm(SingleChangeUnique userPermissions)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOtherPairPerm), userPermissions);
    }
    #endregion Kinkster Interactions

    #region IPC Interactions
    public async Task<HubResponse> UserApplyMoodlesByGuid(MoodlesApplierById dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserApplyMoodlesByGuid), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserApplyMoodlesByStatus(MoodlesApplierByStatus dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserApplyMoodlesByStatus), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserRemoveMoodles(MoodlesRemoval dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserRemoveMoodles), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserClearMoodles(KinksterBase dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserClearMoodles), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserShockKinkster(ShockCollarAction dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserShockKinkster), dto).ConfigureAwait(false);
    }

    #endregion IPC Interactions

    #region Vibe Rooms
    public async Task<HubResponse<List<RoomListing>>> SearchForRooms(SearchBase dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new List<RoomListing>());
        return await _hubConnection!.InvokeAsync<HubResponse<List<RoomListing>>>(nameof(SearchForRooms), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> RoomCreate(RoomCreateRequest dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RoomCreate), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> SendRoomInvite(RoomInvite dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(SendRoomInvite), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> ChangeRoomHost(string roomName, KinksterBase newHost)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(ChangeRoomHost), roomName, newHost).ConfigureAwait(false);
    }

    public async Task<HubResponse> ChangeRoomPassword(string roomName, string newPassword)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(ChangeRoomPassword), roomName, newPassword).ConfigureAwait(false);
    }

    public async Task<HubResponse<List<RoomParticipant>>> RoomJoin(string roomName, string? password, RoomParticipant dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new List<RoomParticipant>());
        return await _hubConnection!.InvokeAsync<HubResponse<List<RoomParticipant>>>(nameof(RoomJoin), roomName, password, dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> RoomLeave()
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RoomLeave)).ConfigureAwait(false);
    }

    public async Task<HubResponse> RoomGrantAccess(KinksterBase allowedUser)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RoomGrantAccess), allowedUser).ConfigureAwait(false);
    }

    public async Task<HubResponse> RoomRevokeAccess(KinksterBase allowedUser)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RoomRevokeAccess), allowedUser).ConfigureAwait(false);
    }

    public async Task<HubResponse> RoomPushDeviceUpdate(ToyInfo info)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RoomPushDeviceUpdate), info).ConfigureAwait(false);
    }

    public async Task<HubResponse> RoomSendDataStream(ToyDataStream dataStream)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RoomSendDataStream), dataStream).ConfigureAwait(false);
    }

    public async Task<HubResponse> RoomSendChat(ChatMessageVibeRoom vibeRoomMessage)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(RoomSendChat), vibeRoomMessage).ConfigureAwait(false);
    }
    #endregion Vibe Rooms
}
#pragma warning restore MA0040
