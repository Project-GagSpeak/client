using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using OtterGui.Text;
using System.Windows.Forms;

namespace GagSpeak.Gui.Settings;

public class SettingsMainNotifications
{
    private enum NotificationTabs
    {
        Plugin,
        Requests,
        OnlineUsers,
        ChatMentions,
    }

    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly ChatConfig _chatConfig;
    private readonly UiFileDialogService _fileDialog;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<NotificationTabs> _tabs;
    private static string _rootPath;
    private static bool _isLinux;
    public SettingsMainNotifications(GagspeakMediator mediator, MainConfig config,
        ChatConfig chatConfig, UiFileDialogService dialog)
    {
        _mediator = mediator;
        _config = config;
        _chatConfig = chatConfig;
        _fileDialog = dialog;

        _tabs = new StylizedTabbarBuilder<NotificationTabs>()
            .AddTab(NotificationTabs.Plugin, "Plugin")
            .AddTab(NotificationTabs.Requests, "Requests")
            .AddTab(NotificationTabs.OnlineUsers, "Online Users")
            .AddTab(NotificationTabs.ChatMentions, "Chat")
            .Build();

        _isLinux = Util.IsWine();
        _rootPath = _isLinux ? @"Z:\" : @"C:\";
    }
    public int NavbarIdx => (int)_tabs.TabSelection;
    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<NotificationTabs>().Length)
            return;
        _tabs.TabSelection = (NotificationTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("main-notifs-inner");


        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case NotificationTabs.Plugin:
                DrawPluginOptions();
                break;
            case NotificationTabs.Requests:
                DrawRequestOptions();
                break;
            case NotificationTabs.OnlineUsers:
                DrawOnlineUserOptions();
                break;
            case NotificationTabs.ChatMentions:
                DrawChatMentionOptions();
                break;
        }
    }

    private void DrawPluginOptions()
    {
        CkGui.FontText(GSLoc.Settings.Options.HeaderPluginNotifs, Fonts.SubtitleFont);

        if (AlertLocationCombo("LiveChatGarbler Zone Warnings##notifLiveChat", _config.Data.GarblerWarnLocation, out var newGarberWarn))
        {
            _config.Data.GarblerWarnLocation = newGarberWarn;
            _config.Save();
        }
        CkGui.HelpTextFramed("Will output a warning when you change zones while the live chat garbler is still active.", true);

        if (AlertLocationCombo("Incoming Requests##notifReq", _config.Data.RequestAlertLocation, out var newReq))
        {
            _config.Data.RequestAlertLocation = newReq;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where notifications for incoming requests appear.", true);

        if (AlertLocationCombo("Connection Notifications", _config.Data.ConnectionAlertLocation, out var newConnect))
        {
            _config.Data.ConnectionAlertLocation = newConnect;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where messages go once connected.", true);

        if (AlertLocationCombo("Online Users##notifOnline", _config.Data.OnlineAlertLocation, out var newOnline))
        {
            _config.Data.OnlineAlertLocation = newOnline;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where notifications for these online users display.", true);

        if (AlertLocationCombo("Info Messages##notifInfo", _config.Data.InfoNotification, out var newInfo))
        {
            _config.Data.InfoNotification = newInfo;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where Plugin \"Info\" notifications will display.", true);

        if (AlertLocationCombo("Warnings##notifWarn", _config.Data.WarningNotification, out var newWarn))
        {
            _config.Data.WarningNotification = newWarn;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where Plugin \"Warning\" notifications will display.", true);

        if (AlertLocationCombo("Errors##notifError", _config.Data.ErrorNotification, out var newError))
        {
            _config.Data.ErrorNotification = newError;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where Plugin \"Error\" notifications will display.", true);
    }

    private bool AlertLocationCombo(string label, AlertLocation cur, out AlertLocation newValue, float width = 150f)
    {
        newValue = cur;
        ImGui.SetNextItemWidth(width);
        using var combo = ImUtf8.Combo(label, cur.ToString(), CFlags.None);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            newValue = AlertLocation.Nowhere;
            return true;
        }

        if (!combo) return false;

        // Draw out the selectables indivdually.
        if (ImGui.Selectable(AlertLocation.Nowhere.ToString(), AlertLocation.Nowhere == cur))
            newValue = AlertLocation.Nowhere;
        CkGui.AttachTooltip("Notifications will not be shown.");

        if (ImGui.Selectable(AlertLocation.Chat.ToString(), AlertLocation.Chat == cur))
            newValue = AlertLocation.Chat;
        CkGui.AttachTooltip("Notifications will be printed in chat.");

        if (ImGui.Selectable(AlertLocation.Toast.ToString(), AlertLocation.Toast == cur))
            newValue = AlertLocation.Toast;
        CkGui.AttachTooltip("Notifications will be shown in the bottom right corner.");

        if (ImGui.Selectable(AlertLocation.Both.ToString(), AlertLocation.Both == cur))
            newValue = AlertLocation.Both;
        CkGui.AttachTooltip("Notifications will be printed in chat and shown in the bottom right corner.");

        return newValue != cur;
    }

    private void DrawRequestOptions()
    {
        CkGui.FontText("Requests", Fonts.SubtitleFont);

        if (AlertLocationCombo("Alert Location##notifReq", _config.Data.RequestAlertLocation, out var newReq))
        {
            _config.Data.RequestAlertLocation = newReq;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where notifications for incoming requests appear.", true);

        var showBubbles = _config.Data.AlertKind.HasAny(AlertKind.Bubble);
        if (ImGui.Checkbox("Show Incoming total", ref showBubbles))
        {
            _config.Data.AlertKind ^= AlertKind.Bubble;
            _config.Save();
        }
        CkGui.HelpTextFramed("Shows the total requests on the topright of the requests tab.", true);

        var showDtr = _config.Data.AlertKind.HasAny(AlertKind.DtrBar);
        if (ImGui.Checkbox("Show as DTR Entry", ref showDtr))
        {
            _config.Data.AlertKind ^= AlertKind.DtrBar;
            _config.Save();
            _mediator.Publish(new DTRRefreshMessage());
        }
        CkGui.HelpTextFramed("Displays the number of incoming and outoing requests to the DTR bar.", true);

        var sounds = _config.Data.AlertKind.HasAny(AlertKind.Audio);
        if (ImGui.Checkbox("Play Audio Alerts", ref sounds))
        {
            _config.Data.AlertKind ^= AlertKind.Audio;
            _config.Save();
        }
        CkGui.HelpTextFramed("Use a native or custom sound upon receiving a request", true);

        DrawAudioOptions("requests", _config);
    }

    private void DrawOnlineUserOptions()
    {
        CkGui.FontText("Online Users", Fonts.SubtitleFont);

        if (AlertLocationCombo("Alert Location##notifOnline", _config.Data.OnlineAlertLocation, out var newOnline))
        {
            _config.Data.OnlineAlertLocation = newOnline;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where notifications for these online users display.", true);

        ImGui.Spacing();
        ImGui.Text("Match Filters");
        CkGui.HelpText("Who to show online notifications for.", true);
        var pingFilter = _config.Data.OnlineNotifyFilter;
        var tempPairs = pingFilter.HasAny(OnlineFilter.Temporary);
        if (ImGui.Checkbox("Temporary", ref tempPairs))
        {
            _config.Data.OnlineNotifyFilter ^= OnlineFilter.Temporary;
            _config.Save();
        }
        var nicked = pingFilter.HasAny(OnlineFilter.Nicknamed);
        if (ImGui.Checkbox("Nicknamed", ref nicked))
        {
            _config.Data.OnlineNotifyFilter ^= OnlineFilter.Nicknamed;
            _config.Save();
        }
        var favorited = pingFilter.HasAny(OnlineFilter.Favorited);
        if (ImGui.Checkbox("Favorited", ref favorited))
        {
            _config.Data.OnlineNotifyFilter ^= OnlineFilter.Favorited;
            _config.Save();
        }

        ImGui.Spacing();
        ImGui.Text("Match Policy:");
        CkGui.HelpText("If we are notified when matching any condition, or all selected.", true);
        var pingPolicy = _config.Data.OnlineNotifyPolicy;
        if (ImGui.RadioButton("Any Filter", pingPolicy == FilterPolicy.MatchAny))
        {
            _config.Data.OnlineNotifyPolicy = FilterPolicy.MatchAny;
            _config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("All Selected Filters", pingPolicy == FilterPolicy.MatchAll))
        {
            _config.Data.OnlineNotifyPolicy = FilterPolicy.MatchAll;
            _config.Save();
        }
    }

    private void DrawChatMentionOptions()
    {
        CkGui.FontText("Chat Mentions", Fonts.SubtitleFont);

        var pingOnTell = _chatConfig.Data.PingOnDM;
        if (ImGui.Checkbox("Enable Pings for DMs", ref pingOnTell))
        {
            _chatConfig.Data.PingOnDM = pingOnTell;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("If any message that is a DM should alert you like mentions.", true);

        var chatMentions = _chatConfig.Data.MentionHighlights;
        if (ImGui.Checkbox("Highlight Mentions", ref chatMentions))
        {
            _chatConfig.Data.MentionHighlights = chatMentions;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Show your Alias, UID, Anon-Tag, or name in chat in a different color.", true);

        var alertKind = _chatConfig.Data.AlertKind;
        var bubbleAlert = alertKind.HasAny(AlertKind.Bubble);
        if (ImGui.Checkbox("Display unread mentions Bubble", ref bubbleAlert))
        {
            _chatConfig.Data.AlertKind ^= AlertKind.Bubble;
            _chatConfig.Save();
        }
        CkGui.AttachTooltip("Display a small counter of unread mentions.");

        var audioAlert = alertKind.HasAny(AlertKind.Audio);
        if (ImGui.Checkbox("Play Audio on chat mentions", ref audioAlert))
        {
            _chatConfig.Data.AlertKind ^= AlertKind.Audio;
            _chatConfig.Save();
        }
        CkGui.AttachTooltip("Plays the audio defined below on chat mentions.");

        DrawAudioOptions("chat", _chatConfig);
    }

    private void DrawAudioOptions<T>(string id, IAudioConfig<T> config) where T : IAudioConfigData
    {
        var hasAudioAlerts = config.Data.AlertKind.HasAny(AlertKind.Audio);
        var isCustom = config.Data.AlertIsCustom;

        using var dis = ImRaii.Disabled(!hasAudioAlerts);
        using var ident = ImRaii.PushIndent();

        using (ImRaii.Group())
        {
            // Audio Type selection
            ImGui.SetNextItemWidth(125 * ImGuiHelpers.GlobalScale);
            int soundType = config.Data.AlertIsCustom ? 1 : 0;
            if (ImGui.Combo($"##audio-type", ref soundType, "Game Sound\0Custom Sound\0"))
            {
                config.Data.AlertIsCustom = soundType == 1;
                config.UpdateAudio();
            }
            CkGui.AttachTooltip("The type of audio to be played.");

            // Sampler
            CkGui.FrameSeparatorV();
            if (CkGui.IconTextButton(FAI.Play, "Test Notification Sound", disabled: !hasAudioAlerts))
                config.PlaySound();
        }
        var soundRowWidth = ImGui.GetItemRectSize().X;

        if (!isCustom)
        {
            var curGamesound = config.Data.AlertSoundbyte;
            if (CkGuiUtils.EnumCombo($"##gamesounds", soundRowWidth, curGamesound, out var newSound, _ => _.ToName(), flags: CFlags.None))
            {
                config.Data.AlertSoundbyte = newSound;
                unsafe { UIGlobals.PlaySoundEffect((uint)newSound); }
                config.UpdateAudio();
            }
            CkGui.AttachTooltip("The native soundbyte to play when receiving a mention");
            return;
        }

        DrawFolderPickerButton((newPath) =>
        {
            config.Data.AlertCustomPath = newPath;
            config.Save();
            config.UpdateAudio();
        });
        ImUtf8.SameLineInner();

        // Draw the custom path if custom.
        var soundInvalid = config.Data.AlertIsCustom && !config.IsAudioReady();
        using (ImRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, soundInvalid))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2, soundInvalid))
        {
            var path = config.Data.AlertCustomPath;
            ImGui.SetNextItemWidth(soundRowWidth - ImUtf8.FrameHeight - ImUtf8.ItemInnerSpacing.X);
            if (ImGui.InputTextWithHint($"##custom-path", "Sound File Path..", ref path, 256))
            {
                config.Data.AlertCustomPath = path;
                config.Save();
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
                config.UpdateAudio();
        }
        CkGui.AttachTooltip(soundInvalid ? "--COL--Sound Path Invalid!--COL--"
            : "The filepath to the custom audio file.", ImGuiColors.DalamudRed);

        var volume = config.Data.AlertVolume;
        ImGui.SetNextItemWidth(soundRowWidth);
        if (ImGui.SliderFloat($"##volume", ref volume, 0, 1, $"Volume: {volume * 100:F1}%%"))
        {
            config.Data.AlertVolume = volume;
            config.UpdateAudio();
        }
        CkGui.AttachTooltip("How loud the custom sound is in playback");
    }

    private void DrawFolderPickerButton(Action<string> onSelected)
    {
        if (CkGui.IconButton(FAI.FolderOpen))
        {
            if (_isLinux)
                OpenDalamudAudioDialog(onSelected);
            else
                ImGui.OpenPopup("audio-import-options");
        }
        CkGui.AttachTooltip("Browse for an audio file (.mp3, .wav)");

        // Fancy dropdown options for Windows users
        var min = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        var popUpPos = min + new Vector2(0, size.Y);
        ImGui.SetNextWindowPos(popUpPos);
        ImGui.SetNextWindowSize(new Vector2(250 * ImGuiHelpers.GlobalScale, ImUtf8.FrameHeightSpacing + size.Y + 8 * ImGuiHelpers.GlobalScale));

        using var s = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f)
            .Push(ImGuiStyleVar.PopupRounding, 5f)
            .Push(ImGuiStyleVar.WindowPadding, ImGuiHelpers.ScaledVector2(4f));

        using var _ = ImRaii.Popup("audio-import-options", WFlags.NoMove | WFlags.NoResize | WFlags.NoCollapse | WFlags.NoScrollbar);
        if (!_) return;

        if (CkGui.IconTextButton(FAI.FolderOpen, "Import via FileDialog", 240 * ImGuiHelpers.GlobalScale, true))
        {
            OpenDalamudAudioDialog(onSelected);
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachTooltip("Opens Dalamuds FileDialog window to select a file from.");

        if (CkGui.IconTextButton(FAI.FolderOpen, "Import via File Explorer", 240 * ImGuiHelpers.GlobalScale, true, _isLinux))
        {
            OpenWindowsAudioExplorer(onSelected);
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachTooltip("Open Windows File Explorer to select an audio file.");
    }

    private void OpenDalamudAudioDialog(Action<string> onSuccess)
    {
        // Filter strictly to mp3 and wav
        _fileDialog.OpenSingleFilePicker("Select Custom Alert Sound", ".mp3,.wav",
            (success, file) => { if (success) onSuccess?.Invoke(file); });
    }

    private void OpenWindowsAudioExplorer(Action<string> onSuccess, string? directory = null)
    {
        var thread = new Thread(() =>
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*";
                    dialog.Title = "Select Custom Alert Sound";

                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        dialog.InitialDirectory = directory;

                    if (dialog.ShowDialog() is DialogResult.OK)
                    {
                        Svc.Logger.Information($"Selected audio file {dialog.FileName}");
                        onSuccess?.Invoke(dialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Svc.Logger.Error($"There was an error while opening the File Browser: {ex.Message}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }
}
