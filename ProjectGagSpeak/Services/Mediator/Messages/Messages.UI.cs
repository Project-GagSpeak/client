using CkCommons.RichChat;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.State.Models;
using GagspeakAPI.Connection;
using GagspeakAPI.Reporting;
using GagspeakAPI.User;

namespace GagSpeak.Services.Mediator;

/// <summary> How we want to modify the defined UI window. </summary>
public enum ToggleType
{
    Toggle,
    Show,
    Hide
}

/// <summary> Fires once we wish to open the popout permissions menu for a Kinkster pair. </summary>
public record OpenKinksterSidePanel(Kinkster Kinkster, bool ForceOpen = false) : MessageBase;

/// <summary> Fires whenever we need to toggle the UI. </summary>
public record UiToggleMessage(Type UiType, ToggleType ToggleType = ToggleType.Toggle) : MessageBase;

public record OpenSettingsUI(int NavbarIdx, int SubnavBarIdx) : MessageBase;

/// <summary> Close all windows and open the IntroUI </summary>
public record SwitchToIntroUiMessage : MessageBase;

/// <summary> Forcefully opens Main UI, and closes the Introduction UI if opened. </summary>
public record IntoFinishedMessage : MessageBase;

/// <summary> Requests to the popup handler to display a report profile prompt. </summary>
public record OpenReportUIMessage(ReportKind Kind, UserData User, RichChatLog<NewGsChatMessage>? ChatLog, string? MsgId) : MessageBase;

/// <summary> Sets the tab of the MainUI. </summary>
public record MainWindowTabChangeMessage(MainMenuTabs.SelectedTab NewTab) : MessageBase;
public record OpenMainUiTab(MainMenuTabs.SelectedTab ToOpen) : MessageBase;

// Profile UI
public record OpenUserProfileMessage(Kinkster Kinkster) : MessageBase;
public record OpenUserLightProfileMessage(UserData UserData) : MessageBase;
public record OpenProfilePopout(UserData UserData) : MessageBase;
public record CloseProfilePopout : MessageBase;

// Profile Updates
public record FetchLatestUserProfile(UserData UserData) : MessageBase;
public record ClearUserProfileMessage(UserData UserData) : MessageBase;
public record UserProfileThemeChanged(UserData User) : MessageBase;

// Removal
public record RemoveCreatedWindowMessage(WindowMediatorSubscriberBase Window) : MessageBase;

/// <summary> This is fired whenever the discord bot wishes to send out an account verification to our client. </summary>
public record VerificationPopupMessage(VerificationCode VerificationCode) : MessageBase;

/// <summary> Fires whenever we finished recording a new pattern, and need to finalize it's details. </summary>
/// <param name="StoredData"> The data we are saving. </param>
/// <param name="Duration"> The duration of the pattern. </param>
public record PatternSavePromptMessage(FullPatternData Data, TimeSpan Duration) : MessageBase;

public record ClosePatternSavePromptMessage : MessageBase;

public record ReScanThumbnailFolder : MessageBase;

/// <summary> Fired upon selecting a thumbnail image within the Thumbnail Browser. </summary>
public record ThumbnailImageSelected(Guid SourceId, Vector2 ImgSize, ImageDataType Folder, string FileName) : MessageBase;
