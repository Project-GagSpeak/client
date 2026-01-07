using GagSpeak.Gui.Components;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Kinksters;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Network;

namespace GagSpeak.Services.Mediator;

/// <summary> How we want to modify the defined UI window. </summary>
public enum ToggleType
{
    Toggle,
    Show,
    Hide
}
public record UserPairSelected(Kinkster? Pair) : MessageBase; // This likely can be removed.

/// <summary> Fires once we wish to open the popout permissions menu for a Kinkster pair. </summary>
public record OpenKinksterSidePanel(Kinkster Kinkster, bool ForceOpen = false) : MessageBase;

public record FolderUpdateKinkster : MessageBase;
public record FolderUpdateRequests : MessageBase;
public record FolderUpdateCollars : MessageBase;




/// <summary> Fires whenever we need to toggle the UI. </summary>
public record UiToggleMessage(Type UiType, ToggleType ToggleType = ToggleType.Toggle) : MessageBase;

/// <summary> Once fired, closes all other windows, and switches to the Introduction UI </summary>
public record SwitchToIntroUiMessage : MessageBase;

/// <summary> Forcefully opens the Main UI, and closes the Introduction UI if opened. </summary>
public record SwitchToMainUiMessage : MessageBase;

/// <summary> Forces a specific tab to be opened within the Main UI </summary>
public record MainWindowTabChangeMessage(MainMenuTabs.SelectedTab NewTab) : MessageBase;

/// <summary> Informs other components in GagSpeak that the Main UI was just closed. </summary>
public record ClosedMainUiMessage : MessageBase;

/// <summary> Fired when we want to remove a specific window from the UI service. </summary>
public record RemoveWindowMessage(WindowMediatorSubscriberBase Window) : MessageBase;

/// <summary> Creates and opens a standalone KinkPlate™ UI. </summary>
public record KinkPlateCreateOpenMessage(Kinkster Kinkster) : MessageBase;

/// <summary> Creates and opens a standalone light KinkPlate™ UI. </summary>
public record KinkPlateLightCreateOpenMessage(UserData UserData) : MessageBase;

/// <summary> When the whitelist has a User hovered long enough and displays a KinkPlate™, this is fired. </summary>
public record OpenKinkPlatePopout(UserData UserData) : MessageBase;

/// <summary> When the KinkPlate™ popout is closed or needs to be toggled. </summary>
public record CloseKinkPlatePopout : MessageBase;

/// <summary> Notifies us that the profile data for a specific Kinkster needs to be cleared from the KinkPlate service. </summary>
public record ClearKinkPlateDataMessage(UserData? UserData = null) : MessageBase;

/// <summary> When we wish to create a report on a defined Kinkster's profile. </summary>
public record ReportKinkPlateMessage(UserData KinksterToReport) : MessageBase;

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
