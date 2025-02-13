using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.UserPair;
using Microsoft.AspNetCore.SignalR.Client;
using GagspeakAPI.Enums;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.Patterns;
using GagspeakAPI.Data.Permissions;
using Microsoft.AspNetCore.SignalR;
using GagspeakAPI.Dto;

namespace GagSpeak.WebAPI;

#pragma warning disable MA0040
/// <summary>
/// Handles the User functions of the API controller on the Main GagSpeak Server.
/// </summary>
public partial class MainHub
{
    /// <summary> 
    /// Creates a new Kinkster Pair request for the other user we wish to pair with.
    /// Will generate a UserAddPairRequest on callback if valid.
    /// </summary>
    public async Task UserSendPairRequest(UserPairSendRequestDto request)
    {
        if (!IsConnected) return;
        Logger.LogDebug("Pushing an outgoing kinkster request to " + request.User.UID, LoggerType.ApiCore);
        await GagSpeakHubMain!.SendAsync(nameof(UserSendPairRequest), request).ConfigureAwait(false); // wait for request to send.
    }

    public async Task UserCancelPairRequest(UserDto user)
    {
        if (!IsConnected) return;
        Logger.LogDebug("Cancelling an outgoing kinkster request to " + user, LoggerType.ApiCore);
        await GagSpeakHubMain!.SendAsync(nameof(UserCancelPairRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task UserAcceptIncPairRequest(UserDto user)
    {
        if (!IsConnected) return;
        Logger.LogDebug("Accepting an incoming kinkster request from " + user, LoggerType.ApiCore);
        await GagSpeakHubMain!.SendAsync(nameof(UserAcceptIncPairRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task UserRejectIncPairRequest(UserDto user)
    {
        if (!IsConnected) return;
        Logger.LogDebug("Rejecting an incoming kinkster request from " + user, LoggerType.ApiCore);
        await GagSpeakHubMain!.SendAsync(nameof(UserRejectIncPairRequest), user).ConfigureAwait(false); // wait for request to send.
    }


    /// <summary> 
    /// Send a request to the server, asking it to remove the declared UserDto from the clients userPair list.
    /// </summary>
    public async Task UserRemovePair(UserDto userDto)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.SendAsync(nameof(UserRemovePair), userDto).ConfigureAwait(false);
    }

    /// <summary> 
    /// Sends a request to the server, asking for the connected clients account to be deleted. 
    /// </summary>
    public async Task UserDelete()
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.SendAsync(nameof(UserDelete)).ConfigureAwait(false);
    }

    /// <summary> 
    /// Send a request to the server, asking it to return a list of all currently online users that you are paired with.
    /// </summary>
    /// <returns>Returns a list of OnlineUserIdent Data Transfer Objects</returns>
    public async Task<List<OnlineUserIdentDto>> UserGetOnlinePairs()
    {
        return await GagSpeakHubMain!.InvokeAsync<List<OnlineUserIdentDto>>(nameof(UserGetOnlinePairs)).ConfigureAwait(false);
    }

    /// <summary> 
    /// Send a request to the server, asking it to return a list of your paired clients.
    /// </summary>
    /// <returns>Returns a list of UserPair data transfer objects</returns>
    public async Task<List<UserPairDto>> UserGetPairedClients()
    {
        return await GagSpeakHubMain!.InvokeAsync<List<UserPairDto>>(nameof(UserGetPairedClients)).ConfigureAwait(false);
    }

    public async Task<List<UserPairRequestDto>> UserGetPairRequests()
    {
        return await GagSpeakHubMain!.InvokeAsync<List<UserPairRequestDto>>(nameof(UserGetPairRequests)).ConfigureAwait(false);
    }

    /// <summary> Uploads your pattern to the server. </summary>
    public async Task<bool> UploadPattern(PatternUploadDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(UploadPattern), dto).ConfigureAwait(false);
    }

    /// <summary> Uploads your a new Moodle to the server. </summary>
    public async Task<bool> UploadMoodle(MoodleUploadDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(UploadMoodle), dto).ConfigureAwait(false);
    }

    /// <summary> Downloads a pattern from the server. </summary>
    public async Task<string> DownloadPattern(Guid patternId)
    {
        if (!IsConnected) return string.Empty;
        return await GagSpeakHubMain!.InvokeAsync<string>(nameof(DownloadPattern), patternId).ConfigureAwait(false);
    }

    /// <summary> Likes a pattern you see on the server. AddingLike==true means we liked it, false means we un-liked it. </summary>
    public async Task<bool> LikePattern(Guid patternId)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(LikePattern), patternId).ConfigureAwait(false);
    }

    /// <summary> Likes a Moodle you see on the server. AddingLike==true means we liked it, false means we un-liked it. </summary>
    public async Task<bool> LikeMoodle(Guid moodleId)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(LikeMoodle), moodleId).ConfigureAwait(false);
    }

    /// <summary> Deletes a pattern from the server. </summary>
    public async Task<bool> RemovePattern(Guid patternId)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(RemovePattern), patternId).ConfigureAwait(false);
    }

    public async Task<bool> RemoveMoodle(Guid moodleId)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(RemoveMoodle), moodleId).ConfigureAwait(false);
    }

    /// <summary> Grabs the search result of your specified query to the server. </summary>
    public async Task<List<ServerPatternInfo>> SearchPatterns(PatternSearchDto patternSearchDto)
    {
        if (!IsConnected) return new List<ServerPatternInfo>();
        return await GagSpeakHubMain!.InvokeAsync<List<ServerPatternInfo>>(nameof(SearchPatterns), patternSearchDto).ConfigureAwait(false);
    }

    /// <summary> Grabs the search result of your specified query to the server. </summary>
    public async Task<List<ServerMoodleInfo>> SearchMoodles(MoodleSearchDto moodleSearchDto)
    {
        if (!IsConnected) return new List<ServerMoodleInfo>();
        return await GagSpeakHubMain!.InvokeAsync<List<ServerMoodleInfo>>(nameof(SearchMoodles), moodleSearchDto).ConfigureAwait(false);
    }

    /// <summary> Grabs the search result of your specified query to the server. </summary>
    public async Task<HashSet<string>> FetchSearchTags()
    {
        if (!IsConnected) return new HashSet<string>();
        return await GagSpeakHubMain!.InvokeAsync<HashSet<string>>(nameof(FetchSearchTags)).ConfigureAwait(false);
    }

    /// <summary> Sends a message to the gagspeak Global chat. </summary>
    public async Task SendGlobalChat(GlobalChatMessageDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(SendGlobalChat), dto).ConfigureAwait(false);
    }

    public async Task UserShockActionOnPair(ShockCollarActionDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(UserShockActionOnPair), dto).ConfigureAwait(false);
    }


    public async Task UserUpdateAchievementData(UserAchievementsDto dto)
    {
        if (!IsConnected) return;
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateAchievementData), dto);
        }
        catch (OperationCanceledException ex)
        {
            // Handle the operation canceled exception
            Logger.LogError(ex, "Operation was canceled while updating achievement data.");
        }
        catch (HubException ex)
        {
            // Handle SignalR hub exceptions
            Logger.LogError(ex, "HubException occurred while updating achievement data.");
        }
        catch (Exception ex)
        {
            // Handle any other exceptions
            Logger.LogError(ex, "An unexpected error occurred while updating achievement data.");
        }
    }

    public async Task<UserKinkPlateDto> UserGetKinkPlate(UserDto dto)
    {
        // if we are not connected, return a new user profile dto with the user data and disabled set to false
        if (!IsConnected) return new UserKinkPlateDto(dto.User, Info: new KinkPlateContent(), ProfilePictureBase64: string.Empty);
        // otherwise, if we are connected, invoke the UserGetKinkPlate function on the server with the user data transfer object
        return await GagSpeakHubMain!.InvokeAsync<UserKinkPlateDto>(nameof(UserGetKinkPlate), dto).ConfigureAwait(false);
    }

    public async Task UserReportKinkPlate(UserKinkPlateReportDto userProfileDto)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(UserReportKinkPlate), userProfileDto).ConfigureAwait(false);
    }

    public async Task UserSetKinkPlateContent(UserKinkPlateContentDto kinkPlateInfo)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(UserSetKinkPlateContent), kinkPlateInfo).ConfigureAwait(false);
    }

    public async Task UserSetKinkPlatePicture(UserKinkPlatePictureDto kinkPlateImage)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(UserSetKinkPlatePicture), kinkPlateImage).ConfigureAwait(false);
    }

    /// <summary> Moodles IPC senders. </summary>
    public async Task<bool> UserApplyMoodlesByGuid(ApplyMoodlesByGuidDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(UserApplyMoodlesByGuid), dto).ConfigureAwait(false);
    }

    public async Task<bool> UserApplyMoodlesByStatus(ApplyMoodlesByStatusDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(UserApplyMoodlesByStatus), dto).ConfigureAwait(false);
    }

    public async Task<bool> UserRemoveMoodles(RemoveMoodlesDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(UserRemoveMoodles), dto).ConfigureAwait(false);
    }

    public async Task<bool> UserClearMoodles(UserDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(UserClearMoodles), dto).ConfigureAwait(false);
    }

    public async Task<bool> VibeRoomCreate(string roomName, string password)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(VibeRoomCreate), roomName, password).ConfigureAwait(false);
    }

    public async Task<bool> SendRoomInvite(VibeRoomInviteDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(SendRoomInvite), dto).ConfigureAwait(false);
    }

    public async Task<bool> ChangeRoomPassword(string roomName, string newPassword)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(ChangeRoomPassword), roomName, newPassword).ConfigureAwait(false);
    }

    public async Task<List<VibeRoomKinksterFullDto>> VibeRoomJoin(string roomName, string password, VibeRoomKinkster dto)
    {
        if (!IsConnected) return new List<VibeRoomKinksterFullDto>();
        return await GagSpeakHubMain!.InvokeAsync<List<VibeRoomKinksterFullDto>>(nameof(VibeRoomJoin), roomName, password, dto).ConfigureAwait(false);
    }

    public async Task<bool> VibeRoomLeave()
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(VibeRoomLeave)).ConfigureAwait(false);
    }

    public async Task<bool> VibeRoomGrantAccess(UserDto allowedUser)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(VibeRoomGrantAccess), allowedUser).ConfigureAwait(false);
    }

    public async Task<bool> VibeRoomRevokeAccess(UserDto allowedUser)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(VibeRoomRevokeAccess), allowedUser).ConfigureAwait(false);
    }

    public async Task VibeRoomPushDeviceUpdate(DeviceInfo info)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(VibeRoomPushDeviceUpdate), info).ConfigureAwait(false);
    }

    public async Task VibeRoomSendDataStream(SexToyDataStreamDto dataStream)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(VibeRoomSendDataStream), dataStream).ConfigureAwait(false);
    }

    public async Task VibeRoomSendChat(string roomName, string message)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(VibeRoomSendChat), message).ConfigureAwait(false);
    }

    /* --------------------- Push Updates of Client Character Data --------------------- */
    public async Task UserPushData(PushCompositeDataMessageDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushData), dto).ConfigureAwait(false); });

    public async Task UserPushDataIpc(PushIpcDataUpdateDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataIpc), dto).ConfigureAwait(false); });

    public async Task UserPushDataGags(PushGagDataUpdateDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataGags), dto).ConfigureAwait(false); });

    public async Task UserPushDataRestrictions(PushRestrictionDataUpdateDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataRestrictions), dto).ConfigureAwait(false); });

    public async Task UserPushDataRestraint(PushRestraintDataUpdateDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataRestraint), dto).ConfigureAwait(false); });

    public async Task<bool> UserPushDataCursedLoot(PushCursedLootDataUpdateDto dto)
    {
        if (!IsConnected) return false;
        return await GagSpeakHubMain!.InvokeAsync<bool>(nameof(UserPushDataCursedLoot), dto).ConfigureAwait(false);
    }

    public async Task UserPushDataOrders(PushOrdersDataUpdateDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataOrders), dto).ConfigureAwait(false); });

    public async Task UserPushDataAlias(PushAliasDataUpdateDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataAlias), dto).ConfigureAwait(false); });

    public async Task UserPushDataToybox(PushToyboxDataUpdateDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataToybox), dto).ConfigureAwait(false); });

    public async Task UserPushDataLightStorage(PushLightStorageMessageDto dto)
        => await ExecuteSafelyAsync(async () => { await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataLightStorage), dto).ConfigureAwait(false); });


    /* --------------------- Permission Updates --------------------- */
    public async Task UserPushAllGlobalPerms(BulkUpdatePermsGlobalDto allGlobalPerms)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserPushAllGlobalPerms), allGlobalPerms));

    public async Task UserPushAllUniquePerms(BulkUpdatePermsUniqueDto allUniquePermsForPair)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserPushAllUniquePerms), allUniquePermsForPair));

    public async Task UserUpdateOwnGlobalPerm(UserGlobalPermChangeDto userPermissions)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOwnGlobalPerm), userPermissions));

    public async Task UserUpdateOtherGlobalPerm(UserGlobalPermChangeDto userPermissions)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOtherGlobalPerm), userPermissions));

    public async Task UserUpdateOwnPairPerm(UserPairPermChangeDto userPermissions)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOwnPairPerm), userPermissions));

    public async Task UserUpdateOtherPairPerm(UserPairPermChangeDto userPermissions)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOtherPairPerm), userPermissions));

    public async Task UserUpdateOwnPairPermAccess(UserPairAccessChangeDto userPermissions)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOwnPairPermAccess), userPermissions));

    // handle later.
    public async Task PushClientIpcData(CharaIPCData data, List<UserData> visibleCharacters, DataUpdateType updateKind)
    {
        Logger.LogDebug("Pushing Character IPC data to " + string.Join(", ", visibleCharacters.Select(v => v.AliasOrUID)) + "[" + updateKind + "]", LoggerType.VisiblePairs);
        await UserPushDataIpc(new(visibleCharacters, data, updateKind)).ConfigureAwait(false);
    }

    // ----------------- Update Pair Data ---------------
    public async Task UserPushPairDataGags(PushPairGagDataUpdateDto dto)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataGags), dto));

    public async Task UserPushPairDataRestrictions(PushPairRestrictionDataUpdateDto dto)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataRestrictions), dto));

    public async Task UserPushPairDataRestraint(PushPairRestraintDataUpdateDto dto)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataRestraint), dto));

    public async Task UserPushPairDataAliasStorage(PushPairAliasDataUpdateDto dto)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataAliasStorage), dto));

    public async Task UserPushPairDataToybox(PushPairToyboxDataUpdateDto dto)
        => await ExecuteSafelyAsync(() => GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataToybox), dto));

    private async Task ExecuteSafelyAsync(Func<Task> pushAction)
    {
        if (!IsConnected) return;
        try
        {
            await pushAction().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Logger.LogCritical("Failed to execute async call. The Operation was cancelled!");
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Failed to safely execute async function! :" + ex);
        }
    }
}
#pragma warning restore MA0040
