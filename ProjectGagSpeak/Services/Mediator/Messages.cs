using Buttplug.Client;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.ChatMessages;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Events;
using GagSpeak.UI.Components;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.VibeRoom;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Services.Mediator;

#pragma warning disable MA0048, S2094

/* ------------------ MESSAGE RELATED RECORDS ------------------ */
public record NotificationMessage(string Title, string Message, NotificationType Type, TimeSpan? TimeShownOnScreen = null) : MessageBase;
public record NotifyChatMessage(SeString Message, NotificationType Type) : MessageBase;
public record EventMessage(InteractionEvent Event) : MessageBase;

public record MainHubDisconnectedMessage : SameThreadMessage;
public record MainHubReconnectingMessage(Exception? Exception) : SameThreadMessage;
public record MainHubReconnectedMessage(string? Arg) : SameThreadMessage;
public record MainHubClosedMessage(Exception? Exception) : SameThreadMessage;
public record MainHubConnectedMessage : MessageBase;
public record OnlinePairsLoadedMessage : MessageBase;

// Unsure if we use these yet
public record VibeRoomUserJoined(VibeRoomKinksterFullDto User) : MessageBase;
public record VibeRoomUserLeft(VibeRoomKinksterFullDto User) : MessageBase;
public record VibeRoomInvite(VibeRoomInviteDto Invite) : MessageBase;
public record VibeRoomUserUpdatedDevice(UserData User, DeviceInfo Device) : MessageBase;
public record VibeRoomDataStreamRecieved(SexToyDataStreamCallbackDto dto) : MessageBase;
public record VibeRoomUserAccessGranted(UserData User) : MessageBase;
public record VibeRoomUserAccessRevoked(UserData User) : MessageBase;
public record VibeRoomChatMessage(UserData User, string Message) : MessageBase;


/* ------------- DALAMUD FRAMEWORK UPDATE RECORDS ------------- */
public record DalamudLoginMessage : MessageBase; // record indicating the moment the client logs into the game instance.
public record DalamudLogoutMessage(int type, int code) : MessageBase; // record indicating the moment the client logs out of the game instance.
public record FrameworkUpdateMessage : SameThreadMessage; // a message indicating the need for a framework update.
public record DelayedFrameworkUpdateMessage : SameThreadMessage; // a message indicating the need for a delayed framework update.
public record GPoseStartMessage : MessageBase; // a message indicating the start of gpose.
public record GPoseEndMessage : MessageBase; // a message indicating the end of gpose.
public record CutsceneBeginMessage : MessageBase;
public record CutsceneSkippedMessage : MessageBase; // Whenever a cutscene is skipped.
public record ClientPlayerInCutscene : MessageBase; // Informs us when the player has been loaded in a cutscene.
public record CutsceneEndMessage : MessageBase; // helps us know when to reapply data like moodles.
public record ZoneSwitchStartMessage(ushort prevZone) : MessageBase; // know when we are beginning to switch zones
public record ZoneSwitchEndMessage : MessageBase; // know when we have finished switching zones
public record JobChangeMessage(uint jobId) : MessageBase; // know when we have changed jobs
public record CommendationsIncreasedMessage(int amount) : MessageBase;

/* ------------------ PLAYER DATA RELATED RECORDS------------------ */
public record UpdateAllOnlineWithCompositeMessage : MessageBase; // for updating all online pairs with composite data.
public record PairWentOnlineMessage(UserData UserData) : MessageBase; // a message indicating a pair has gone online.
public record PairHandlerVisibleMessage(PairHandler Player) : MessageBase; // a message indicating the visibility of a pair handler.
public record PairWasRemovedMessage(UserData UserData) : MessageBase; // a message indicating a pair has been removed.
public record TargetPairMessage(Pair Pair) : MessageBase; // called when publishing a targeted pair connection (see UI)
public record CreateCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase;
public record ClearCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase; // called when we should clear a GameObject from cache creation service.
public record MufflerLanguageChanged : MessageBase; // called whenever the client language changes to a new language.
public record AppearanceImpactingSettingChanged : MessageBase; // called whenever an appearance impacting setting is changed.

/* ------------- PLAYER DATA MODULE INTERACTIONS --------- */
public record TooltipSetItemToEditorMessage(EquipSlot Slot, EquipItem Item) : MessageBase;
public record HardcoreActionMessage(InteractionType type, NewState State) : MessageBase;
public record HardcoreRemoveBlindfoldMessage : MessageBase;
public record MoodlesPermissionsUpdated(string NameWithWorld) : MessageBase;

////////////// TOYBOX RELATED RECORDS //////////////
public record VfxActorRemoved(IntPtr data) : MessageBase;
public record ToyScanStarted : MessageBase; // for when the toybox scan is started.
public record ToyScanFinished : MessageBase; // for when the toybox scan is finished.
public record VibratorModeToggled(VibratorEnums VibratorMode) : MessageBase; // for when the vibrator mode is toggled.
public record ToyDeviceAdded(ButtplugClientDevice Device) : MessageBase; // for when a device is added.
public record ToyDeviceRemoved(ButtplugClientDevice Device) : MessageBase; // for when a device is removed.
public record ButtplugClientDisconnected : MessageBase; // for when the buttplug client disconnects.
public record ToyboxActiveDeviceChangedMessage(int DeviceIndex) : MessageBase;
public record PlaybackStateToggled(Guid PatternId, NewState NewState) : MessageBase; // for when a pattern is activated.
public record PatternRemovedMessage(Guid PatternId) : MessageBase; // for when a pattern is removed.
public record TriggersModifiedMessage : MessageBase;
public record ExecuteHealthPercentTriggerMessage(HealthPercentTrigger Trigger) : MessageBase;
public record PlayerLatestActiveItems(UserData User, CharaActiveGags GagsInfo, CharaActiveRestrictions RestrictionsInfo, CharaActiveRestraint RestraintInfo) : MessageBase;


/* ------------------ IPC HANDLER RECORDS------------------ */
public record PenumbraInitializedMessage : MessageBase;
public record PenumbraDisposedMessage : MessageBase;
public record ModSettingPresetRemoved(string directory) : MessageBase;
public record MoodlesReady : MessageBase;
public record GlamourerReady : MessageBase;
public record CustomizeReady : MessageBase;
public record CustomizeDispose : MessageBase;
public record MoodlesStatusManagerUpdate : MessageBase;
public record MoodlesStatusModified(Guid Guid) : MessageBase; // when we change one of our moodles settings.
public record MoodlesPresetModified(Guid Guid) : MessageBase; // when we change one of our moodles presets.
public record MoodlesApplyStatusToPair(ApplyMoodlesByStatusDto StatusDto) : MessageBase;
public record MoodlesUpdateNotifyMessage : MessageBase; // for pinging the moodles.
public record PiShockExecuteOperation(string shareCode, int OpCode, int Intensity, int Duration) : MessageBase;


/* ----------------- Character Cache Creation Records ----------------- */
public record IpcDataChangedMessage(DataUpdateType UpdateType, CharaIPCData NewIpcData) : SameThreadMessage;
public record GagDataChangedMessage(DataUpdateType UpdateType, int AffectedIdx, ActiveGagSlot NewData) : SameThreadMessage;
public record RestrictionDataChangedMessage(DataUpdateType UpdateType, int AffectedIdx, ActiveRestriction NewData) : SameThreadMessage;
public record RestraintDataChangedMessage(DataUpdateType UpdateType, CharaActiveRestraint NewData) : SameThreadMessage;
public record OrdersDataChangedMessage(DataUpdateType UpdateType) : SameThreadMessage;
public record AliasDataChangedMessage(DataUpdateType UpdateType, UserData IntendedUser, CharaAliasData NewData) : SameThreadMessage;
public record ToyboxDataChangedMessage(DataUpdateType UpdateType, CharaToyboxData NewData, Guid InteractionId) : SameThreadMessage;
public record LightStorageDataChangedMessage(CharaLightStorageData CharacterStorageData) : SameThreadMessage;
public record GameObjectHandlerCreatedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;
public record GameObjectHandlerDestroyedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;


/* ------------------ USER INTERFACE (UI) RECORDS------------------ */
public enum ToggleType { Toggle, Show, Hide }
public record UserPairSelected(Pair? Pair) : MessageBase; // Fires whenever a new pair is selected from the userPairListHandler.
public record OpenUserPairPermissions(Pair? Pair, StickyWindowType PermsWindowType, bool ForceOpenMainUI) : MessageBase; // fired upon request to open the permissions window for a pair
public record StickyPairWindowCreated(Pair newPair) : MessageBase;
public record RefreshUiMessage : MessageBase;
public record UiToggleMessage(Type UiType, ToggleType ToggleType = ToggleType.Toggle) : MessageBase; // For toggling the UI.
public record SwitchToIntroUiMessage : MessageBase; // indicates that we are in the introduction UI.
public record SwitchToMainUiMessage : MessageBase; // indicates we are in the main UI.
public record OpenSettingsUiMessage : MessageBase; // indicates we are in the settings UI.
public record MainWindowTabChangeMessage(MainMenuTabs.SelectedTab NewTab) : MessageBase;
public record ClosedMainUiMessage : MessageBase; // indicates the main UI has been closed.
public record RemoveWindowMessage(WindowMediatorSubscriberBase Window) : MessageBase; // fired upon request to remove a window from the UI service.
public record CompactUiChange(Vector2 Size, Vector2 Position) : MessageBase; // fired whenever we change the window size or position
public record KinkPlateOpenStandaloneMessage(Pair Pair) : MessageBase; // for opening the profile standalone window.
public record KinkPlateOpenStandaloneLightMessage(UserData UserData) : MessageBase; // for opening the profile standalone window.

public record ProfilePopoutToggle(UserData? PairUserData) : MessageBase; // toggles the profile popout window for a paired client.
public record ClearProfileDataMessage(UserData? UserData = null) : MessageBase; // a message indicating the need to clear profile data.
public record ReportKinkPlateMessage(UserData KinksterToReport) : MessageBase; // for reporting a GagSpeak profile.
public record VerificationPopupMessage(VerificationDto VerificationCode) : MessageBase; // indicating that we have received a verification code popup.
public record PatternSavePromptMessage(List<byte> StoredData, TimeSpan Duration) : MessageBase; // prompts the popup and passes in savedata
public record ClosePatternSavePromptMessage : MessageBase; // closes the pattern save prompt.

/* -------------------- CHAT RELATED RECORDS -------------------- */
public record GlobalChatMessage(GlobalChatMessageDto ChatMessage, bool FromSelf) : MessageBase;
public record ClientSentChat(ChatChannel.Channels Channel, string Message) : MessageBase; // Client Player sent a chat message.
public record SafewordUsedMessage(string UID = "") : MessageBase; // for when the safeword is used.
public record SafewordHardcoreUsedMessage(string UID = "") : MessageBase; // for when the hardcore safeword is used.

/* -------------------- FILE MANAGER RECORDS -------------------- */
// May remove this down the line if events turn out to be better, but we will see to be honest. For now stick with these.
public record ConfigGagRestrictionChanged(StorageItemChangeType Type, GarblerRestriction Item, string? OldString) : MessageBase;
public record ConfigRestrictionChanged(StorageItemChangeType Type, RestrictionItem Item, string? OldString) : MessageBase;
public record ConfigRestraintSetChanged(StorageItemChangeType Type, RestraintSet Item, string? OldString) : MessageBase;
public record ConfigCursedItemChanged(StorageItemChangeType Type, CursedItem Item, string? OldString) : MessageBase;
public record ConfigPatternChanged(StorageItemChangeType Type, Pattern Item, string? OldString) : MessageBase;
public record ConfigAlarmChanged(StorageItemChangeType Type, Alarm Item, string? OldString) : MessageBase;
public record ConfigTriggerChanged(StorageItemChangeType Type, Trigger Item, string? OldString) : MessageBase;
public record ReloadFileSystem(ModuleSection Module) : MessageBase; // for reloading the file system.
#pragma warning restore S2094, MA0048
