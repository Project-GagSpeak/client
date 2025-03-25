using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Profile;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using ImGuiNET;

namespace GagSpeak.UI.MainWindow;

public class AccountTab : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly GagspeakConfigService _config;
    private readonly KinkPlateService _profileManager;
    private readonly TutorialService _guides;
    private readonly IDalamudPluginInterface _pi;

    public AccountTab(ILogger<AccountTab> logger, GagspeakMediator mediator,
        MainHub hub, CkGui uiShared, OnFrameworkService frameworkUtils,
        GagspeakConfigService config, KinkPlateService profileManager, TutorialService guides,
        IDalamudPluginInterface pi) : base(logger, mediator)
    {
        _hub = hub;

        _frameworkUtils = frameworkUtils;
        _config = config;
        _profileManager = profileManager;
        _guides = guides;
        _pi = pi;
    }

    private static Vector2 LastWinPos = Vector2.Zero;
    private static Vector2 LastWinSize = Vector2.Zero;

    /// <summary> Main Draw function for this tab </summary>
    public void DrawAccountSection()
    {
        // get the width of the window content region we set earlier
        var _windowContentWidth = CkGui.GetWindowContentRegionWidth();
        LastWinPos = ImGui.GetWindowPos();
        LastWinSize = ImGui.GetWindowSize();
        var _spacingX = ImGui.GetStyle().ItemSpacing.X;

        // make this whole thing a scrollable child window.
        // (keep the border because we will style it later and helps with alignment visual)
        using (ImRaii.Child("AccountPageChild", new Vector2(CkGui.GetWindowContentRegionWidth(), 0), false, WFlags.NoScrollbar))
        {
            try
            {
                var profileData = _profileManager.GetKinkPlate(new UserData(MainHub.UID));

                var pfpWrap = profileData.GetCurrentProfileOrDefault();
                if (pfpWrap is { } wrap)
                {
                    var region = ImGui.GetContentRegionAvail();
                    ImGui.Spacing();
                    var imgSize = new Vector2(180f, 180f);
                    // move the x position so that it centeres the image to the center of the window.
                    CkGui.SetCursorXtoCenter(imgSize.X);
                    var currentPosition = ImGui.GetCursorPos();

                    var pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddImageRounded(wrap.ImGuiHandle, pos, pos + imgSize, Vector2.Zero, Vector2.One,
                        ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 90f);
                    ImGuiHelpers.ScaledDummy(imgSize);
                    _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.UserProfilePicture, LastWinPos, LastWinSize);
                    ImGui.SetCursorPos(new Vector2(currentPosition.X, currentPosition.Y + imgSize.Y));

                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error: {ex}");
            }

            // draw the UID header below this.
            DrawUIDHeader();
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.UserIdentification, LastWinPos, LastWinSize);

            // below this, draw a separator. (temp)
            ImGui.Spacing();
            ImGui.Separator();

            DrawSafewordChild();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.PenSquare, "My Profile", "Open and Customize your Profile!", () => Mediator.Publish(new UiToggleMessage(typeof(KinkPlateEditorUI))));
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfileEditing, LastWinPos, LastWinSize);


            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.Cog, "My Settings", "Opens the Settings UI", () => Mediator.Publish(new UiToggleMessage(typeof(SettingsUi))));
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.AccessingSettings, LastWinPos, LastWinSize);

            // Actions Notifier thing.
            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.Bell, "Actions Notifier", "See who did what actions on you!", () => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI))));

            // now do one for ko-fi
            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.Coffee, "Support via Ko-fi", "This plugin took a massive toll on my life as a solo dev." +
                Environment.NewLine + "As happy as I am to make this free for all of you to enjoy, " +
                Environment.NewLine + "any support or tips are much appreciated ♥", () =>
                {
                    try { Process.Start(new ProcessStartInfo { FileName = "https://www.ko-fi.com/cordeliamist", UseShellExecute = true }); }
                    catch (Exception e) { Logger.LogError($"Failed to open the Ko-Fi link. {e.Message}"); }
                });
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SelfPlug, LastWinPos, LastWinSize);


            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.Pray, "Support via Patreon", "This plugin took a massive toll on my life as a solo dev." +
                Environment.NewLine + "As happy as I am to make this free for all of you to enjoy, " +
                Environment.NewLine + "any support / tips are much appreciated ♥", () =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = "https://www.patreon.com/CordeliaMist", UseShellExecute = true }); }
                catch (Exception e) { Logger.LogError($"Failed to open the Patreon link. {e.Message}"); }
            });

            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.ThumbsUp, "Send Positive Feedback!", "Opens a short 1 question positive feedback form ♥", () =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = "https://forms.gle/4AL43XUeWna2DtYK7", UseShellExecute = true }); }
                catch (Exception e) { Logger.LogError($"Failed to open the google form. {e.Message}"); }
            });

            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.ThumbsUp, "Frequently Asked Questions", "Opens a google doc of all the most commonly asked questions!", () =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = "https://docs.google.com/document/d/1nluBM2tsLzTrRdZEZN8FnpvOSPzdPrcH9R-SL78mJdA/edit?tab=t.0", UseShellExecute = true }); }
                catch (Exception e) { Logger.LogError($"Failed to open the google doc. {e.Message}"); }
            });

            ImGui.AlignTextToFramePadding();
            DrawAccountSettingChild(FAI.Wrench, "Open Configs", "Opens the Plugin Config Folder", () =>
            {
                try
                {
                    var ConfigDirectory = _pi.ConfigDirectory.FullName;
                    Process.Start(new ProcessStartInfo { FileName = ConfigDirectory, UseShellExecute = true });
                }
                catch (Exception e)
                {
                    Logger.LogError($"[ConfigFileOpen] Failed to open the config directory. {e.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Draws the UID header for the currently connected client (you)
    /// </summary>
    private void DrawUIDHeader()
    {
        // fetch the Uid Text of yourself
        var uidText = CkGui.GetUidText();

        // push the big boi font for the UID
        using (UiFontService.UidFont.Push())
        {
            var uidTextSize = ImGui.CalcTextSize(uidText);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - uidTextSize.X / 2);
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(CkGui.UidColor(), uidText);
        }

        // if we are connected
        if (MainHub.ServerStatus is ServerState.Connected)
        {
            CkGui.CopyableDisplayText(MainHub.DisplayName);

            // if the UID does not equal the display name
            if (!string.Equals(MainHub.DisplayName, MainHub.UID, StringComparison.Ordinal))
            {
                // grab the original text size for the UID in the api controller
                var origTextSize = ImGui.CalcTextSize(MainHub.UID);
                // adjust the cursor and redraw the UID (really not sure why this is here but we can trial and error later.
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - origTextSize.X / 2);
                ImGui.TextColored(CkGui.UidColor(), MainHub.UID);
                // give it the same functionality.
                CkGui.CopyableDisplayText(MainHub.UID);
            }
        }
    }

    private bool EditingSafeword = false;
    private void DrawSafewordChild()
    {
        var height = 35f; // static height
        Vector2 textSize;
        using (UiFontService.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize($"No Safeword Set");
        }
        var iconSize = CkGui.IconSize(FAI.ExclamationTriangle);
        var editButtonSize = CkGui.IconSize(FAI.Edit);
        var bigTextPaddingDistance = ((height - textSize.Y) / 2);
        var iconFontCenterY = (height - iconSize.Y) / 2;
        var editButtonCenterY = (height - editButtonSize.Y) / 2;

        using (ImRaii.Child($"##DrawSafewordChild", new Vector2(CkGui.GetWindowContentRegionWidth(), height), false))
        {
            // We love ImGui....
            var childStartYpos = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(childStartYpos + iconFontCenterY);
            CkGui.IconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);

            ImGui.SameLine(iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX());
            var safewordText = _config.Config.Safeword == "" ? "No Safeword Set" : _config.Config.Safeword;
            if (EditingSafeword)
            {
                ImGui.SetCursorPosY(childStartYpos + ((height - 23) / 2) + 0.5f); // 23 is the input text box height
                ImGui.SetNextItemWidth(225 * ImGuiHelpers.GlobalScale);
                var safeword = _config.Config.Safeword;
                if (ImGui.InputTextWithHint("##Your Safeword", "Enter Safeword", ref safeword, 30))
                {
                    _config.Config.Safeword = safeword;
                    _config.Save();
                    EditingSafeword = false;
                }
                // now, head to the same line of the full width minus the width of the button
                ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - editButtonSize.X - ImGui.GetStyle().ItemSpacing.X);
                ImGui.SetCursorPosY(childStartYpos + ((height - editButtonSize.Y) / 2) - 2f);
            }
            else
            {
                ImGui.SetCursorPosY(bigTextPaddingDistance - 2f); // -2f accounts for font visual offset
                using (UiFontService.UidFont.Push())
                {
                    CkGui.ColorText(safewordText, ImGuiColors.DalamudYellow);
                }
                // now, head to the same line of the full width minus the width of the button
                ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - editButtonSize.X - ImGui.GetStyle().ItemSpacing.X);
                ImGui.SetCursorPosY(childStartYpos + ((height - editButtonSize.Y) / 2) + 1f);
            }
            // draw out the icon button
            CkGui.IconText(FAI.Edit);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                EditingSafeword = !EditingSafeword;
            }
        }
        CkGui.AttachToolTip("Set a safeword to quickly revert any changes made by the plugin.");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SafewordPartOne, LastWinPos, LastWinSize);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SafewordPartTwo, LastWinPos, LastWinSize);
    }

    private void DrawAccountSettingChild(FontAwesomeIcon leftIcon, string displayText, string hoverTT, Action buttonAction)
    {
        var height = 20f; // static height
        var startYpos = ImGui.GetCursorPosY();

        var textSize = ImGui.CalcTextSize(displayText);
        var iconSize = CkGui.IconSize(leftIcon);
        var arrowRightSize = CkGui.IconSize(FAI.ChevronRight);
        var textCenterY = ((height - textSize.Y) / 2);
        var iconFontCenterY = (height - iconSize.Y) / 2;
        var arrowRightCenterY = (height - arrowRightSize.Y) / 2;
        // text height == 17, padding on top and bottom == 2f, so 21f
        using (ImRaii.Child($"##DrawSetting{displayText + hoverTT}", new Vector2(CkGui.GetWindowContentRegionWidth(), height)))
        {
            // We love ImGui....
            var childStartYpos = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(childStartYpos + iconFontCenterY);
            CkGui.IconText(leftIcon);

            ImGui.SameLine(iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(childStartYpos + textCenterY);
            ImGui.TextUnformatted(displayText);

            // Position the button on the same line, aligned to the right
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - arrowRightSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(childStartYpos + arrowRightCenterY);
            // Draw the icon button and perform the action when pressed
            CkGui.IconText(FAI.ChevronRight);
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            buttonAction.Invoke();
        }
        CkGui.AttachToolTip(hoverTT);
    }
}
