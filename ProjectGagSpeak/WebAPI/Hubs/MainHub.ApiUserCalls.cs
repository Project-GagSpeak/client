using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
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
        catch (Bagagwa ex)
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

    public async Task<HubResponse> UserReportProfile(ProfileReport profileReport)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserReportProfile), profileReport).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserReportChat(ChatReport chatReport)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserReportChat), chatReport).ConfigureAwait(false);
    }
    #endregion Client Vanity

    #region Personal Interactions
    public async Task<HubResponse> UserPushActiveData(PushClientCompositeUpdate dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushActiveData), dto).ConfigureAwait(false);

    }

    public async Task<HubResponse<ActiveGagSlot>> UserPushActiveGags(PushClientActiveGagSlot dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new ActiveGagSlot());
        return await _hubConnection!.InvokeAsync<HubResponse<ActiveGagSlot>>(nameof(UserPushActiveGags), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse<ActiveRestriction>> UserPushActiveRestrictions(PushClientActiveRestriction dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new ActiveRestriction());
        return await _hubConnection!.InvokeAsync<HubResponse<ActiveRestriction>>(nameof(UserPushActiveRestrictions), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse<CharaActiveRestraint>> UserPushActiveRestraint(PushClientActiveRestraint dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new CharaActiveRestraint());
        return await _hubConnection!.InvokeAsync<HubResponse<CharaActiveRestraint>>(nameof(UserPushActiveRestraint), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse<CharaActiveCollar>> UserPushActiveCollar(PushClientActiveCollar dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new CharaActiveCollar());
        return await _hubConnection!.InvokeAsync<HubResponse<CharaActiveCollar>>(nameof(UserPushActiveCollar), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse<AppliedCursedItem>> UserPushActiveLoot(PushClientActiveLoot dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new AppliedCursedItem(Guid.Empty));
        return await _hubConnection!.InvokeAsync<HubResponse<AppliedCursedItem>>(nameof(UserPushActiveLoot), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushItemEnabledState(PushItemEnabledState dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushItemEnabledState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushGagEnabledState(PushGagEnabledState dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushGagEnabledState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushToyEnabledState(PushToyEnabledState dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushToyEnabledState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushItemEnabledStates(PushItemEnabledStates dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushItemEnabledStates), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushGagEnabledStates(PushGagEnabledStates dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushGagEnabledStates), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushToyEnabledStates(PushToyEnabledStates dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushToyEnabledStates), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewGagData(PushClientDataChangeGag dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewGagData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewRestrictionData(PushClientDataChangeRestriction dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewRestrictionData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewRestraintData(PushClientDataChangeRestraint dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewRestraintData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewCollarData(PushClientDataChangeCollar dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewCollarData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewLootData(PushClientDataChangeLoot dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewLootData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewAliasData(PushClientDataChangeAlias dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewAliasData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewPatternData(PushClientDataChangePattern dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewPatternData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewAlarmData(PushClientDataChangeAlarm dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewAlarmData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushNewTriggerData(PushClientDataChangeTrigger dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushNewTriggerData), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse<ClientGlobals>> UserBulkChangeGlobal(BulkChangeGlobal allGlobalPerms)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new ClientGlobals(new GlobalPerms(), new HardcoreStatus()));
        return await _hubConnection!.InvokeAsync<HubResponse<ClientGlobals>>(nameof(UserBulkChangeGlobal), allGlobalPerms);
    }

    public async Task<HubResponse> UserBulkChangeUnique(BulkChangeUnique allUniquePermsForPair)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserBulkChangeUnique), allUniquePermsForPair);
    }

    public async Task<HubResponse> ChangeOwnGlobalPerm(string propertyName, object newValue)
        => await UserChangeOwnGlobalPerm(new(OwnUserData, new KeyValuePair<string, object>(propertyName, newValue), OwnUserData));


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

    public async Task<HubResponse<HardcoreStatus>> UserHardcoreAttributeExpired(HardcoreAttributeExpired change)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError, new HardcoreStatus());
        return await _hubConnection!.InvokeAsync<HubResponse<HardcoreStatus>>(nameof(UserHardcoreAttributeExpired), change).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserDelete()
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserDelete)).ConfigureAwait(false);
    }

    #endregion Personal Interactions

    #region Kinkster Interactions
    public async Task<HubResponse<KinksterRequest>> UserSendKinksterRequest(CreateKinksterRequest request)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse<KinksterRequest>>(nameof(UserSendKinksterRequest), request).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserCancelKinksterRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserCancelKinksterRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse<AddedKinksterPair>> UserAcceptKinksterRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse<AddedKinksterPair>>(nameof(UserAcceptKinksterRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserRejectKinksterRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserRejectKinksterRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserSendCollarRequest(CreateCollarRequest request)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserSendCollarRequest), request).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserCancelCollarRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserCancelCollarRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserAcceptCollarRequest(AcceptCollarRequest acceptedRequest)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserAcceptCollarRequest), acceptedRequest).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserRejectCollarRequest(KinksterBase user)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserRejectCollarRequest), user).ConfigureAwait(false); // wait for request to send.
    }

    public async Task<HubResponse> UserRemoveKinkster(KinksterBase KinksterBase)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserRemoveKinkster), KinksterBase).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterActiveGag(PushKinksterActiveGagSlot dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterActiveGag), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterActiveRestriction(PushKinksterActiveRestriction dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterActiveRestriction), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterActiveRestraint(PushKinksterActiveRestraint dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterActiveRestraint), dto);
    }

    public async Task<HubResponse> UserChangeKinksterActiveCollar(PushKinksterActiveCollar dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterActiveCollar), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterPatternState(PushKinksterEnabledState dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterPatternState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterAlarmState(PushKinksterEnabledState dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterAlarmState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeKinksterTriggerState(PushKinksterEnabledState dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeKinksterTriggerState), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserChangeOtherGlobalPerm(SingleChangeGlobal userPermissions)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOtherGlobalPerm), userPermissions);
    }

    public async Task<HubResponse> ChangeOtherGlobalPerm(UserData target, string propertyName, object newValue)
        => await UserChangeOtherGlobalPerm(new(target, new KeyValuePair<string, object>(propertyName, newValue), target));

    public async Task<HubResponse> UserChangeOtherPairPerm(SingleChangeUnique userPermissions)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOtherPairPerm), userPermissions);
    }

    public async Task<HubResponse> UserChangeOtherHardcoreState(HardcoreStateChange newState)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserChangeOtherHardcoreState), newState);
    }
    #endregion Kinkster Interactions

    #region IPC Interactions
    public async Task<HubResponse> UserPushMoodlesFull(PushMoodlesFull dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushMoodlesFull), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushMoodlesSM(PushMoodlesSM dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushMoodlesSM), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushMoodlesStatuses(PushMoodlesStatuses dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushMoodlesStatuses), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushMoodlesPresets(PushMoodlesPresets dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushMoodlesPresets), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushStatusModified(PushStatusModified dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushStatusModified), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserPushPresetModified(PushPresetModified dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserPushPresetModified), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserApplyMoodlesByGuid(ApplyMoodleId dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserApplyMoodlesByGuid), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserApplyMoodlesByStatus(ApplyMoodleStatus dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserApplyMoodlesByStatus), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserRemoveMoodles(RemoveMoodleId dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserRemoveMoodles), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserClearMoodles(KinksterBase dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserClearMoodles), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserSendNameToKinkster(SendNameAction dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserSendNameToKinkster), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserShockKinkster(ShockCollarAction dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError); ;
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserShockKinkster), dto).ConfigureAwait(false);
    }

    public async Task<HubResponse> UserHypnotizeKinkster(HypnoticAction dto)
    {
        if (!IsConnected) return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NetworkError);
        return await _hubConnection!.InvokeAsync<HubResponse>(nameof(UserHypnotizeKinkster), dto).ConfigureAwait(false);
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
