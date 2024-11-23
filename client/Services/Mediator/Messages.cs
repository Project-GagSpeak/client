using Buttplug.Client;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.Services.Events;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.Toybox;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Numerics;

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

public record ToyboxHubDisconnectedMessage : SameThreadMessage;
public record ToyboxHubReconnectingMessage(Exception? Exception) : SameThreadMessage;
public record ToyboxHubReconnectedMessage(string? Arg) : SameThreadMessage;
public record ToyboxHubClosedMessage(Exception? Exception) : SameThreadMessage;
public record ToyboxHubConnectedMessage : MessageBase;

public record ToyboxPrivateRoomJoined(string RoomName) : MessageBase;
public record ToyboxPrivateRoomLeft(string RoomName) : MessageBase;
public record OpenPrivateRoomRemote(PrivateRoom PrivateRoom) : MessageBase;


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
public record OpenUserPairPermissions(Pair? Pair, StickyWindowType PermsWindowType, bool ForceOpenMainUI) : MessageBase; // fired upon request to open the permissions window for a pair
public record TargetPairMessage(Pair Pair) : MessageBase; // called when publishing a targeted pair connection (see UI)
public record CreateCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase;
public record ClearCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase; // called when we should clear a gameobject from cache creation service.
public record MufflerLanguageChanged : MessageBase; // called whenever the client language changes to a new language.
public record AppearanceImpactingSettingChanged : MessageBase; // called whenever an appearance impacting setting is changed.

/* ------------- PLAYER DATA MODULE INTERACTIONS --------- */
public record GagTypeChanged(GagType NewGagType, GagLayer Layer, bool SelfApplied = false) : MessageBase; // called whenever the client changes their gag type.
public record GagLockToggle(PadlockData PadlockInfo, NewState newGagLockState, bool SelfApplied = false) : MessageBase; // called whenever the client changes their padlock.
public record TooltipSetItemToRestraintSetMessage(EquipSlot Slot, EquipItem Item) : MessageBase;
public record TooltipSetItemToCursedItemMessage(EquipSlot Slot, EquipItem Item) : MessageBase;

////////////// WARDROBE RELATED RECORDS //////////////
public record HardcoreActionMessage(HardcoreAction type, NewState State) : MessageBase;
public record HardcoreRemoveBlindfoldMessage : MessageBase;
public record MoodlesPermissionsUpdated(string NameWithWorld) : MessageBase;

////////////// PUPPETEER RELATED RECORDS //////////////
public record UpdateChatListeners : MessageBase; // for updating the chat listeners.
public record UpdateCharacterListenerForUid(string Uid, string CharName, string CharWorld) : MessageBase;


////////////// TOYBOX RELATED RECORDS //////////////
public record VfxActorRemoved(IntPtr data) : MessageBase;
public record ToyScanStarted : MessageBase; // for when the toybox scan is started.
public record ToyScanFinished : MessageBase; // for when the toybox scan is finished.
public record VibratorModeToggled(VibratorMode VibratorMode) : MessageBase; // for when the vibrator mode is toggled.
public record ToyDeviceAdded(ButtplugClientDevice Device) : MessageBase; // for when a device is added.
public record ToyDeviceRemoved(ButtplugClientDevice Device) : MessageBase; // for when a device is removed.
public record ButtplugClientDisconnected : MessageBase; // for when the buttplug client disconnects.
public record ToyboxActiveDeviceChangedMessage(int DeviceIndex) : MessageBase;
public record PlaybackStateToggled(Guid PatternId, NewState NewState) : MessageBase; // for when a pattern is activated.
public record PatternRemovedMessage(Guid PatternId) : MessageBase; // for when a pattern is removed.
public record TriggersModifiedMessage : MessageBase;
public record ExecuteHealthPercentTriggerMessage(HealthPercentTrigger Trigger) : MessageBase;


/* ------------------ PLAYERDATA CLIENTSIDE PERMISSION HANDLING ------------------- */
public record PlayerCharAppearanceChanged(CharaAppearanceData newGagData, DataUpdateKind UpdateKind) : MessageBase;
public record PlayerCharWardrobeChanged(DataUpdateKind UpdateKind) : MessageBase;
public record PlayerCharAliasChanged(string UpdatedPairUID, DataUpdateKind UpdateKind) : MessageBase;
public record PlayerCharToyboxChanged(DataUpdateKind UpdateKind) : MessageBase;
public record PlayerCharStorageUpdated : MessageBase;
public record PlayerLatestActiveItems(UserData User, List<string> ActiveGags, Guid ActiveRestraint) : MessageBase;


/* ------------------ IPC HANDLER RECORDS------------------ */
public record PenumbraInitializedMessage : MessageBase;
public record PenumbraDisposedMessage : MessageBase;
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
public record CharacterDataCreatedMessage(CharaIPCData CharacterData) : MessageBase; // TODO: See how to remove this?
public record CharacterIpcDataCreatedMessage(CharaIPCData CharaIPCData, DataUpdateKind UpdateKind) : SameThreadMessage;
public record CharacterAppearanceDataCreatedMessage(CharaAppearanceData CharaAppearanceData, DataUpdateKind UpdateKind) : SameThreadMessage;
public record CharacterWardrobeDataCreatedMessage(CharaWardrobeData CharaWardrobeData, DataUpdateKind UpdateKind) : SameThreadMessage;
public record CharacterAliasDataCreatedMessage(CharaAliasData CharaAliasData, UserData userData, DataUpdateKind UpdateKind) : SameThreadMessage;
public record CharacterToyboxDataCreatedMessage(CharaToyboxData CharaToyboxData, DataUpdateKind UpdateKind) : SameThreadMessage;
public record CharacterStorageDataCreatedMessage(CharaStorageData CharacterStorageData) : SameThreadMessage;
public record GameObjectHandlerCreatedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;
public record GameObjectHandlerDestroyedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;


/* ------------------ USER INTERFACE (UI) RECORDS------------------ */
public enum ToggleType { Toggle, Show, Hide }
public record RefreshUiMessage : MessageBase; // a message indicating the need to refresh the UI.
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
public record BlindfoldUiTypeChange(BlindfoldType NewType) : MessageBase; // for changing blindfold type.

/* -------------------- DISCOVER TAB RECORDS -------------------- */
public record GlobalChatMessage(GlobalChatMessageDto ChatMessage, bool FromSelf) : MessageBase;
public record SafewordUsedMessage : MessageBase; // for when the safeword is used.
public record SafewordHardcoreUsedMessage : MessageBase; // for when the hardcore safeword is used.

/* --------------------- COSMETICS & ACHIEVEMENTS RECORDS --------------------- */
public record AchievementDataUpdateMessage(string base64Data) : MessageBase; // Sent from Achievement Manager to APIController for data update.


#pragma warning restore S2094, MA0048
