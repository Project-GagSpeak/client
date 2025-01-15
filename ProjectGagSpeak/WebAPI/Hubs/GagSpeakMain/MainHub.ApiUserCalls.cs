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
        Logger.LogDebug("Pushing an outgoing kinkster request to "+ request.User.UID, LoggerType.ApiCore);
        await GagSpeakHubMain!.SendAsync(nameof(UserSendPairRequest), request).ConfigureAwait(false); // wait for request to send.
    }

    public async Task UserCancelPairRequest(UserDto user)
    {
        if (!IsConnected) return;
        Logger.LogDebug("Cancelling an outgoing kinkster request to "+user, LoggerType.ApiCore);
        await GagSpeakHubMain!.SendAsync(nameof(UserCancelPairRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task UserAcceptIncPairRequest(UserDto user)
    {
        if (!IsConnected) return;
        Logger.LogDebug("Accepting an incoming kinkster request from "+user, LoggerType.ApiCore);
        await GagSpeakHubMain!.SendAsync(nameof(UserAcceptIncPairRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task UserRejectIncPairRequest(UserDto user)
    {
        if (!IsConnected) return;
        Logger.LogDebug("Rejecting an incoming kinkster request from "+user, LoggerType.ApiCore);
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
        CheckConnection();
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

    /// <summary> Sends a message to a pair spesific group chat. </summary>
    public async Task SendPairChat(PairChatMessageDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubMain!.InvokeAsync(nameof(SendPairChat), dto).ConfigureAwait(false);
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
        // if we are not connected, return
        if (!IsConnected) return;
        // if we are connected, send the report to the server
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


    /// <summary>
    /// Pushes the composite user data of a a character to other recipients.
    /// (This is called upon by PushCharacterDataInternal, see bottom)
    /// </summary>
    public async Task UserPushData(UserCharaCompositeDataMessageDto dto)
    {
        // try and push the character data dto to the server
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushData), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }

    /// <summary>
    /// Pushes the IPC data from the client character to other recipients.
    /// </summary>
    public async Task UserPushDataIpc(UserCharaIpcDataMessageDto dto)
    {
        // try and push the character data dto to the server
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataIpc), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }

    /// <summary>
    /// Pushes the composite user data of a a character to other recipients.
    /// </summary>
    public async Task UserPushDataAppearance(UserCharaAppearanceDataMessageDto dto)
    {
        // try and push the character data dto to the server
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataAppearance), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }

    /// <summary>
    /// Pushes the wardrobe data of the client to other recipients.
    /// </summary>
    public async Task UserPushDataWardrobe(UserCharaWardrobeDataMessageDto dto)
    {
        // try and push the character data dto to the server
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataWardrobe), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }

    public async Task UserPushDataOrders(UserCharaOrdersDataMessageDto dto)
    {
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataOrders), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }

    /// <summary>
    /// Pushes the puppeteer alias lists of the client to other recipients.
    /// </summary>
    public async Task UserPushDataAlias(UserCharaAliasDataMessageDto dto)
    {
        // try and push the character data dto to the server
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataAlias), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }

    /// <summary>
    /// Pushes the toybox pattern & trigger information of the client to other recipients.
    /// </summary>
    public async Task UserPushDataToybox(UserCharaToyboxDataMessageDto dto)
    {
        // try and push the character data dto to the server
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataToybox), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }


    /// <summary>
    /// Pushes the toybox pattern & trigger information of the client to other recipients.
    /// </summary>
    public async Task UserPushDataLightStorage(UserCharaLightStorageMessageDto dto)
    {
        // try and push the character data dto to the server
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushDataLightStorage), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to Push character data");
        }
    }

    public async Task UserPushAllGlobalPerms(UserPairUpdateAllGlobalPermsDto allGlobalPerms)
    {
        CheckConnection();
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushAllGlobalPerms), allGlobalPerms).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to push all Global Permissions");
        }
    }

    public async Task UserPushAllUniquePerms(UserPairUpdateAllUniqueDto allUniquePermsForPair)
    {
        CheckConnection();
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushAllUniquePerms), allUniquePermsForPair).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to push all Unique Permissions for a Pair.");
        }
    }

    /// <summary> Pushes to server a request to modify a global permissions of the client. </summary>
    public async Task UserUpdateOwnGlobalPerm(UserGlobalPermChangeDto userPermissions)
    {
        CheckConnection();
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOwnGlobalPerm), userPermissions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to set own global permission");
        }
    }


    /// <summary> Pushes to server a request to modify a global permissions of the client. </summary>
    public async Task UserUpdateOtherGlobalPerm(UserGlobalPermChangeDto userPermissions)
    {
        CheckConnection();
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOtherGlobalPerm), userPermissions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to set other userpair global permission");
        }
    }

    /// <summary> Pushes to server a request to modify a unique userpair related permission of the client. </summary>
    public async Task UserUpdateOwnPairPerm(UserPairPermChangeDto userPermissions)
    {
        CheckConnection();
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOwnPairPerm), userPermissions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update clients permission for a userpair.");
        }
    }

    /// <summary> Pushes to server a request to modify a permission on one of the clients userPairs pair permissions  </summary>
    public async Task UserUpdateOtherPairPerm(UserPairPermChangeDto userPermissions)
    {
        CheckConnection();
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOtherPairPerm), userPermissions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update a pair permission belonging to one of the clients userpairs.");
        }
    }

    /// <summary>
    /// pushes a request to update your edit access permissions for one of your userPairs.
    /// </summary>
    public async Task UserUpdateOwnPairPermAccess(UserPairAccessChangeDto userPermissions)
    {
        CheckConnection();
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserUpdateOwnPairPermAccess), userPermissions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update your edit access permission for one of your userPairs");
        }
    }


    /* --------------------- Character Data Handling --------------------- */

    /// <summary> 
    /// This will call upon characterDataInternal, which will handle the assignments and the recipients, storing them into the Dto
    /// </summary>
    public async Task PushCharacterCompositeData(CharaCompositeData data, List<UserData> onlineCharacters)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            Logger.LogDebug("Pushing Character Composite data to "+string.Join(", ", onlineCharacters.Select(v => v.AliasOrUID)), LoggerType.OnlinePairs);
            await UserPushData(new(onlineCharacters, data)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogDebug("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of composite data"); }
    }

    /// <summary>
    /// Pushes Client's updated IPC data to the list of visible recipients.
    /// </summary>
    public async Task PushCharacterIpcData(CharaIPCData data, List<UserData> visibleCharacters, IpcUpdateType updateKind)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            Logger.LogDebug("Pushing Character IPC data to "+string.Join(", ", visibleCharacters.Select(v => v.AliasOrUID)) + "[" + updateKind + "]", LoggerType.VisiblePairs);
            await UserPushDataIpc(new(visibleCharacters, data, updateKind)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogWarning("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of IPC data"); }
    }


    /// <summary>
    /// Pushes Client's updated appearance data to the list of online recipients.
    /// </summary>
    public async Task PushCharacterAppearanceData(CharaAppearanceData data, List<UserData> onlineCharacters, GagLayer affectedType, GagUpdateType type, Padlocks prevPadlock)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            if (onlineCharacters.Any())
            {
                Logger.LogDebug("Pushing Character Appearance data to " + string.Join(", ", onlineCharacters.Select(v => v.AliasOrUID)) + "[" + type + "]", LoggerType.OnlinePairs);
            }
            else
            {
                Logger.LogDebug("Updating AppearanceData to stored ActiveStateData", LoggerType.OnlinePairs);
            }
            await UserPushDataAppearance(new(onlineCharacters, data, affectedType, type, prevPadlock)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogWarning("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of Appearance data"); }
    }

    /// <summary>
    /// Pushes Client's updated wardrobe data to the list of online recipients.
    /// </summary>
    public async Task PushCharacterWardrobeData(CharaWardrobeData data, List<UserData> onlineCharacters, WardrobeUpdateType updateKind, string affectedItem)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            if(onlineCharacters.Any()) Logger.LogDebug("Pushing Character Wardrobe data to " + string.Join(", ", onlineCharacters.Select(v => v.AliasOrUID)) + "["+updateKind+"]", LoggerType.OnlinePairs);
            else Logger.LogDebug("Updating WardrobeData to stored ActiveStateData", LoggerType.OnlinePairs);
            await UserPushDataWardrobe(new(onlineCharacters, data, updateKind, affectedItem)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogWarning("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of Wardrobe data"); }
    }

    /// <summary>
    /// Pushes Client's updated active timed Items data to the list of online recipients.
    /// </summary>
    public async Task PushCharacterOrdersData(CharaOrdersData data, List<UserData> onlineCharacters, OrdersUpdateType updateKind, string affectedId)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            if (onlineCharacters.Any()) Logger.LogDebug("Pushing Orderss state to " + string.Join(", ", onlineCharacters.Select(v => v.AliasOrUID)) + "[" + updateKind + "]", LoggerType.OnlinePairs);
            else Logger.LogDebug("Updating Orderss state to other pairs", LoggerType.OnlinePairs);
            await UserPushDataOrders(new(onlineCharacters, data, updateKind, affectedId)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogWarning("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of Wardrobe data"); }
    }

    /// <summary>
    /// Pushes Client's updated alias list data to the respective recipient.
    /// </summary>
    public async Task PushCharacterAliasListData(CharaAliasData data, UserData onlineCharacter, PuppeteerUpdateType updateKind)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            Logger.LogDebug("Pushing Character Alias data to "+string.Join(", ", onlineCharacter.AliasOrUID) + "[" + updateKind + "]", LoggerType.OnlinePairs);
            await UserPushDataAlias(new(onlineCharacter, data, updateKind)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogWarning("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of Alias List data"); }
    }

    /// <summary>
    /// Pushes Client's updated pattern information to the list of online recipients.
    /// </summary>
    public async Task PushCharacterToyboxData(CharaToyboxData data, List<UserData> onlineCharacters, ToyboxUpdateType updateKind)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            if(onlineCharacters.Any()) Logger.LogDebug("Pushing Character PatternInfo to "+string.Join(", ", onlineCharacters.Select(v => v.AliasOrUID)) + "[" + updateKind + "]", LoggerType.VisiblePairs);
            else Logger.LogDebug("Updating ToyboxData to stored ActiveStateData", LoggerType.OnlinePairs);
            await UserPushDataToybox(new(onlineCharacters, data, updateKind)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogWarning("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of Pattern Information"); }
    }

    /// <summary>
    /// Pushes Client's updated Light Storage Data to the list of online recipients.
    /// </summary>
    public async Task PushCharacterLightStorageData(CharaStorageData data, List<UserData> onlineCharacters)
    {
        if (!IsConnected) return;

        try // if connected, try to push the data to the server
        {
            if (onlineCharacters.Any()) 
                Logger.LogDebug("Pushing LightStorage Data to " + string.Join(", ", onlineCharacters.Select(v => v.AliasOrUID)) + "[Light Storage Update]", LoggerType.VisiblePairs);
            
            await UserPushDataLightStorage(new(onlineCharacters, data)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { Logger.LogWarning("Upload operation was cancelled"); }
        catch (Exception ex) { Logger.LogWarning(ex, "Error during upload of Pattern Information"); }
    }



    /// <summary>
    /// Updates another pairs appearance data with the new changes you've made to them.
    /// </summary>
    public async Task UserPushPairDataAppearanceUpdate(OnlineUserCharaAppearanceDataDto dto)
    {
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataAppearanceUpdate), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Failed to Push an update to {dto.User.UID}'s Appearance data");
        }
    }

    /// <summary>
    /// Updates another pairs wardrobe data with the new changes you've made to them.
    /// </summary>
    public async Task UserPushPairDataWardrobeUpdate(OnlineUserCharaWardrobeDataDto dto)
    {
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataWardrobeUpdate), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Failed to Push an update to {dto.User.UID}'s Wardrobe data");
        }
    }

    public async Task UserPushPairDataAliasStorageUpdate(OnlineUserCharaAliasDataDto dto)
    {
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataAliasStorageUpdate), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Failed to Push an update to {dto.User.UID}'s Alias data");
        }
    }

    /// <summary>
    /// Updates another pairs toybox info data with the new changes you've made to them.
    /// </summary>
    public async Task UserPushPairDataToyboxUpdate(OnlineUserCharaToyboxDataDto dto)
    {
        try
        {
            await GagSpeakHubMain!.InvokeAsync(nameof(UserPushPairDataToyboxUpdate), dto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Failed to Push an update to {dto.User.UID}'s Toybox data");
        }
    }
}
#pragma warning restore MA0040
