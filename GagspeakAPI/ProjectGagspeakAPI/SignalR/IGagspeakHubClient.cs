using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Enums;

namespace GagspeakAPI.SignalR;

/// <summary>
/// 
/// The interface for the GagspeakHubClient
/// 
/// <para>
/// 
/// Because this interface inherits from the IGagspeakHub,
/// it will have all the methods from the IGagspeakHub
/// 
/// </para>
/// </summary>
public interface IGagspeakHubClient : IGagspeakHub
{
    // to view function purposes, be sure to inspect the IGagspeakHub interface class comments.
    void OnReceiveServerMessage(Action<MessageSeverity, string> act); // send message to client
    void OnReceiveHardReconnectMessage(Action<MessageSeverity, string, ServerState> act); // send message to client forcing a reconnection
    void OnUpdateSystemInfo(Action<SystemInfoDto> act); // update client with the system info
    void OnUserAddClientPair(Action<UserPairDto> act); // once a pair is bidirectional, send to the client the userpairDto as validation
    void OnUserRemoveClientPair(Action<UserDto> act); // if either end of a bidirectional pair removes one another, remove the pairing.
    void OnUpdateUserIndividualPairStatusDto(Action<UserIndividualPairStatusDto> act); // pair status update

    void OnUserApplyMoodlesByGuid(Action<ApplyMoodlesByGuidDto> act);
    void OnUserApplyMoodlesByStatus(Action<ApplyMoodlesByStatusDto> act);
    void OnUserRemoveMoodles(Action<RemoveMoodlesDto> act);
    void OnUserClearMoodles(Action<UserDto> act);

    void OnUserUpdateSelfAllGlobalPerms(Action<UserAllGlobalPermChangeDto> act);
    void OnUserUpdateSelfAllUniquePerms(Action<UserPairUpdateAllUniqueDto> act);
    void OnUserUpdateSelfPairPermsGlobal(Action<UserGlobalPermChangeDto> act); // pair global permission
    void OnUserUpdateSelfPairPerms(Action<UserPairPermChangeDto> act); // pair permission update
    void OnUserUpdateSelfPairPermAccess(Action<UserPairAccessChangeDto> act); // pair permission access update
    void OnUserUpdateOtherAllPairPerms(Action<UserPairUpdateAllPermsDto> act); // pair permission all update
    void OnUserUpdateOtherAllGlobalPerms(Action<UserAllGlobalPermChangeDto> act);
    void OnUserUpdateOtherAllUniquePerms(Action<UserPairUpdateAllUniqueDto> act);
    void OnUserUpdateOtherPairPermsGlobal(Action<UserGlobalPermChangeDto> act); // pair permission global update
    void OnUserUpdateOtherPairPerms(Action<UserPairPermChangeDto> act); // pair permission update
    void OnUserUpdateOtherPairPermAccess(Action<UserPairAccessChangeDto> act); // pair permission access update

    void OnUserReceiveCharacterDataComposite(Action<OnlineUserCompositeDataDto> act); // send to a client that one of their paired users has updated their character data (composite)
    void OnUserReceiveOwnDataIpc(Action<OnlineUserCharaIpcDataDto> act); // send to a client that one of their paired users has updated their character data (ipc)
    void OnUserReceiveOtherDataIpc(Action<OnlineUserCharaIpcDataDto> act);
    void OnUserReceiveOwnDataAppearance(Action<OnlineUserCharaAppearanceDataDto> act); // send to a client that one of their paired users has updated their character data (appearance)
    void OnUserReceiveOtherDataAppearance(Action<OnlineUserCharaAppearanceDataDto> act);
    void OnUserReceiveOwnDataWardrobe(Action<OnlineUserCharaWardrobeDataDto> act);
    void OnUserReceiveOtherDataWardrobe(Action<OnlineUserCharaWardrobeDataDto> act); // send to a client that one of their paired users has updated their character data (alias)
    void OnUserReceiveOwnDataAlias(Action<OnlineUserCharaAliasDataDto> act); // send to a client that one of their paired users has updated their character data (alias)
    void OnUserReceiveOtherDataAlias(Action<OnlineUserCharaAliasDataDto> act); // update client with another UserPair's latest aliasList data.
    void OnUserReceiveOwnDataToybox(Action<OnlineUserCharaToyboxDataDto> act); // send to a client that one of their paired users has updated their character data (pattern)
    void OnUserReceiveOtherDataToybox(Action<OnlineUserCharaToyboxDataDto> act);
    void OnUserReceiveOwnLightStorage(Action<OnlineUserStorageUpdateDto> act); // send to a client that one of their paired users has updated their character data (pattern)
    void OnUserReceiveOtherLightStorage(Action<OnlineUserStorageUpdateDto> act);

    void OnUserReceiveShockInstruction(Action<ShockCollarActionDto> act);
    void OnGlobalChatMessage(Action<GlobalChatMessageDto> act); // sends message to gagspeak global chat group.
    void OnUserSendOffline(Action<UserDto> act); // send to a client that one of their paired users is offline
    void OnUserSendOnline(Action<OnlineUserIdentDto> act); // send to a client that one of their paired users is online
    void OnUserUpdateProfile(Action<UserDto> act); // send to a client that one of their paired users has updated their profile
    void OnDisplayVerificationPopup(Action<VerificationDto> act); // send to a client to display a verification popup
}
