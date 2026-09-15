using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiFontChooserDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Chat;
using OtterGui.Text;
using System.Windows.Forms;

namespace GagSpeak.Gui.Settings;

public class SettingsMainChat
{
    private enum ChatTabs
    {
        Rules,
        Format,
        Font,
        Mentions,
        GlobalChat,
    }

    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly ChatConfig _chatConfig;
    private readonly ChatFontManager _chatFont;
    private readonly ChatService _chatService;
    private readonly UiFileDialogService _fileDialog;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<ChatTabs> _tabs;
    private bool _newFontSelected;
    private static string _rootPath;
    private static bool _isLinux;
    public SettingsMainChat(GagspeakMediator mediator, MainConfig config,
        ChatConfig chatConfig, ChatFontManager chatFont, ChatService chatService,
        UiFileDialogService fileDialog)
    {
        _mediator = mediator;
        _config = config;
        _chatConfig = chatConfig;
        _chatFont = chatFont;
        _chatService = chatService;
        _fileDialog = fileDialog;

        _tabs = new StylizedTabbarBuilder<ChatTabs>()
            .AddTab(ChatTabs.Rules, "Rules")
            .AddTab(ChatTabs.Format, "Formating")
            .AddTab(ChatTabs.Font, "Font")
            .AddTab(ChatTabs.Mentions, "Mentions")
            .AddTab(ChatTabs.GlobalChat, "Global Chat")
            .Build();

        _isLinux = Util.IsWine();
        _rootPath = _isLinux ? @"Z:\" : @"C:\";
    }

    public int NavbarIdx => (int)_tabs.TabSelection;

    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<ChatTabs>().Length)
            return;
        _tabs.TabSelection = (ChatTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("main-chat-inner");
        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case ChatTabs.Rules:
                DrawRules();
                break;
            case ChatTabs.Format:
                DrawFormattingOptions();
                break;
            case ChatTabs.Font:
                DrawFontOptions();
                break;
            case ChatTabs.Mentions:
                DrawMentionsOptions();
                break;
            case ChatTabs.GlobalChat:
                DrawGlobalChatOptions();
                break;
        }
    }

    private void DrawRules()
    {
        CkGui.FontText("Chat Rules", Fonts.SubtitleFont);

        var showInUiHide = _chatConfig.Data.ShowInUIHide;
        if (ImGui.Checkbox("Show when hiding UI", ref showInUiHide))
        {
            _chatConfig.Data.ShowInUIHide = showInUiHide;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Displays the chat UI while hiding your HUD.", true);

        var showInCutscene = _chatConfig.Data.ShowInCutscene;
        if (ImGui.Checkbox("Show in cutscenes", ref showInCutscene))
        {
            _chatConfig.Data.ShowInCutscene = showInCutscene;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Displays the chat UI in cutscenes.", true);


        var showInGpose = _chatConfig.Data.ShowInGroupPose;
        if (ImGui.Checkbox("Show in gpose", ref showInGpose))
        {
            _chatConfig.Data.ShowInGroupPose = showInGpose;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Displays the chat UI in GPOSE.", true);

        var openChatOnStartup = _chatConfig.Data.OpenUIOnStartup;
        if (ImGui.Checkbox("Open Chat Window on startup.", ref openChatOnStartup))
        {
            _chatConfig.Data.OpenUIOnStartup = openChatOnStartup;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Automatically opens the chat window for you on plugin startup.", true);
    }

    private void DrawFormattingOptions()
    {
        CkGui.FontText("Formatting", Fonts.SubtitleFont);

        var useLegacyAnonName = _config.Data.UseLegacyAnonName;
        if (ImGui.Checkbox(GSLoc.Settings.Options.PrefThreeCharaAnonName, ref useLegacyAnonName))
        {
            _config.Data.UseLegacyAnonName = useLegacyAnonName;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.PrefThreeCharaAnonNameTT, true);

        var timestamps = _chatConfig.Data.Timestamps;
        if (ImGui.Checkbox("Show Timestamps", ref timestamps))
        {
            _chatConfig.Data.Timestamps = timestamps;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Attach timestamps to messages, relative to your timezone.", true);

        var showEmotes = _chatConfig.Data.ShowEmotes;
        if (ImGui.Checkbox("Show Emotes", ref showEmotes))
        {
            _chatConfig.Data.ShowEmotes = showEmotes;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Parse any emote-text to their emote display", true);

        var unreadBubble = _chatConfig.Data.UnreadBubble;
        if (ImGui.Checkbox("Show unread bubble", ref unreadBubble))
        {
            _chatConfig.Data.UnreadBubble = unreadBubble;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Adds a bubble in the UI for all unread messages in chats.", true);

        var showInChatbox = _chatConfig.Data.ShowDMsInChatbox;
        if (CkGui.Checkbox("Print DMs in the Native Chatbox", ref showInChatbox, UiService.DisableUI))
        {
            _chatConfig.Data.ShowDMsInChatbox = showInChatbox;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("In addition to the ChatUI, DM's are also output to the Chatbox.");

        using (ImRaii.PushIndent())
        {
            var dmPrefixCol = _chatConfig.Data.DMPrefixColor;
            if (CkGuiUtils.ColorEditNativeForeground("Prefix Color", ref dmPrefixCol, defaultCol: GsDefaults.DMColorPrefix))
            {
                _chatConfig.Data.DMPrefixColor = dmPrefixCol;
                _chatConfig.Save();
            }

            ImGui.SameLine();
            var dmTextCol = _chatConfig.Data.DMTextColor;
            if (CkGuiUtils.ColorEditNativeForeground("Text Color", ref dmTextCol, defaultCol: GsDefaults.DMColorText))
            {
                _chatConfig.Data.DMTextColor = dmTextCol;
                _chatConfig.Save();
            }
        }

        ImGui.Spacing();
        CkGui.FontText("Chat UI", Fonts.HeaderFont);

        var opacity = _chatConfig.Data.WindowOpacity;
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("Window Opacity", ref opacity, 0.1f, 1f, $"{opacity * 100:F1}%% Opacity"))
        {
            _chatConfig.Data.WindowOpacity = opacity;
            _chatConfig.Save();
        }

        var unfocusedOpacity = _chatConfig.Data.UnfocusedWindowOpacity;
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("Unfocused Opacity", ref unfocusedOpacity, 0.1f, 1f, $"{unfocusedOpacity * 100:F1}%% Opacity"))
        {
            _chatConfig.Data.UnfocusedWindowOpacity = unfocusedOpacity;
            _chatConfig.Save();
        }

        var transDelta = _chatConfig.Data.OpacityShiftDelta;
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.DragFloat("Opacity Delta per frame", ref transDelta, 0.001f, 0f, 1f))
        {
            _chatConfig.Data.OpacityShiftDelta = transDelta;
            _chatConfig.Save();
        }
    }

    private void DrawFontOptions()
    {
        CkGui.FontText("Chat Font", Fonts.SubtitleFont);
        var useCustom = _chatConfig.Data.UseCustomChatFont;
        if (ImGui.Checkbox("Use Custom Font.", ref useCustom))
        {
            _chatConfig.Data.UseCustomChatFont = useCustom;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Uses a custom font and scale for the Chat Window UI", true);

        if (_chatConfig.Data.UseCustomChatFont)
        {
            if (CkGui.IconTextButton(FAI.Font, "Select Custom Font.."))
                ShowFontSelectorPopup();
        }
        ImGui.Separator();
        using (ImRaii.PushColor(ImGuiCol.Text, Gradient.Get(CkCol.TriStateCheck.Vec4(), CkCol.TriStateCross.Vec4()), _newFontSelected))
            if (CkGui.IconTextButton(FAI.Check, "Apply Selected Font", disabled: !_newFontSelected))
            {
                _chatFont.ReloadFont();
                _newFontSelected = false;
            }
        CkGui.AttachTooltip("Applies the selected font to the chat window UI's.");
    }

    private void ShowFontSelectorPopup()
    {
        var chooser = SingleFontChooserDialog.CreateAuto((UiBuilder)Svc.PluginInterface.UiBuilder);
        if (_chatConfig.Data.ChatFont is SingleFontSpec sfs)
            chooser.SelectedFont = sfs;
        // Listen for the font selected event.
        chooser.SelectedFontSpecChanged += (chosenFont) =>
        {
            _newFontSelected = true;
            _chatConfig.Data.ChatFont = chosenFont;
            _chatConfig.Save();
        };
    }

    private void DrawMentionsOptions()
    {
        CkGui.FontText("Mentions", Fonts.DefaultScaled);

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

        using (ImRaii.PushIndent())
        {
            var mentionColor = ImGui.ColorConvertU32ToFloat4(_chatConfig.Data.MentionColor);
            if (ImGui.ColorEdit4("Mention Color", ref mentionColor, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoInputs))
                _chatConfig.Data.MentionColor = ImGui.ColorConvertFloat4ToU32(mentionColor);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                _chatConfig.Data.MentionColor = GsDefaults.DefaultMentionColor;
            CkGui.AttachTooltip("The color of your name when mentioned.");
        }

        var hasAudioAlerts = _chatConfig.Data.AlertKind.HasAny(AlertKind.Audio);
        if (ImGui.Checkbox("Play Audio on chat mentions", ref hasAudioAlerts))
        {
            _chatConfig.Data.AlertKind ^= AlertKind.Audio;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Plays the audio defined below on chat mentions.", true);

        var isCustom = _chatConfig.Data.AlertIsCustom;

        using var dis = ImRaii.Disabled(!hasAudioAlerts);
        using var ident = ImRaii.PushIndent();

        using (ImRaii.Group())
        {
            // Audio Type selection
            ImGui.SetNextItemWidth(125 * ImGuiHelpers.GlobalScale);
            int soundType = _chatConfig.Data.AlertIsCustom ? 1 : 0;
            if (ImGui.Combo($"##audio-type", ref soundType, "Game Sound\0Custom Sound\0"))
            {
                _chatConfig.Data.AlertIsCustom = soundType == 1;
                _chatConfig.UpdateAudio();
            }
            CkGui.AttachTooltip("The type of audio to be played.");

            // Sampler
            CkGui.FrameSeparatorV();
            if (CkGui.IconTextButton(FAI.Play, "Test Notification Sound", disabled: !hasAudioAlerts))
                _chatConfig.PlaySound();
        }
        var soundRowWidth = ImGui.GetItemRectSize().X;

        if (!isCustom)
        {
            var curGamesound = _chatConfig.Data.AlertSoundbyte;
            if (CkGuiUtils.EnumCombo($"##gamesounds", soundRowWidth, curGamesound, out var newSound, _ => _.ToName(), flags: CFlags.None))
            {
                _chatConfig.Data.AlertSoundbyte = newSound;
                unsafe { UIGlobals.PlaySoundEffect((uint)newSound); }
                _chatConfig.UpdateAudio();
            }
            CkGui.AttachTooltip("The native soundbyte to play when recieving a request");
            return;
        }

        DrawFolderPickerButton((newPath) =>
        {
            _chatConfig.Data.AlertCustomPath = newPath;
            _chatConfig.Save();
            _chatConfig.UpdateAudio();
        });
        ImUtf8.SameLineInner();

        // Draw the custom path if custom.
        var soundInvalid = _chatConfig.Data.AlertIsCustom && !_chatConfig.IsAudioReady();
        using (ImRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, soundInvalid))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2, soundInvalid))
        {
            var path = _chatConfig.Data.AlertCustomPath;
            ImGui.SetNextItemWidth(soundRowWidth - ImUtf8.FrameHeight - ImUtf8.ItemInnerSpacing.X);
            if (ImGui.InputTextWithHint($"##custom-path", "Sound File Path..", ref path, 256))
            {
                _chatConfig.Data.AlertCustomPath = path;
                _chatConfig.Save();
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
                _chatConfig.UpdateAudio();
        }
        CkGui.AttachTooltip(soundInvalid ? "--COL--Sound Path Invalid!--COL--"
            : "The filepath to the custom audio file.", ImGuiColors.DalamudRed);

        var volume = _chatConfig.Data.AlertVolume;
        ImGui.SetNextItemWidth(soundRowWidth);
        if (ImGui.SliderFloat($"##volume", ref volume, 0, 1, $"Volume: {volume * 100:F1}%%"))
        {
            _chatConfig.Data.AlertVolume = volume;
            _chatConfig.Save();
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _chatConfig.UpdateAudio();
        }
        CkGui.AttachTooltip("How loud the custom sound is in playback");
    }

    private void DrawGlobalChatOptions()
    {
        CkGui.FontText("Global Chat", Fonts.SubtitleFont);

        CkGui.BulletText("This feature is");
        ImUtf8.SameLineInner();
        CkGui.TextUnderlined("Privacy First", CkCol.TriStateCheck.Uint());
        ImGui.SameLine(0, 0);
        ImGui.TextUnformatted(", protecting your identity while socializing.");

        CkGui.BulletText("Your name will display as \"Kinkster.XXXX\" to other chatters, unless paired.");
        CkGui.BulletText("Preferences modify what is enabled on your ChatUser right-click menu.");

        CkGui.BulletText("DM's use your Anon-Kinkster name, sharing no relation to your player.", ImGuiColors.DalamudYellow);
        ImGui.Spacing();

        var useNative = _chatConfig.Data.UseNativeChat;
        if (ImGui.Checkbox("Print messages to Chatbox.", ref useNative))
        {
            _chatConfig.Data.UseNativeChat = useNative;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Uses the Native Game-Chat to ouput messages.", true);

        using (ImRaii.Disabled(!useNative))
        {
            using (ImRaii.PushIndent())
            {
                if (CkGuiUtils.EnumCombo("Channel", 200f, _chatConfig.Data.ChatType, out var newType, skip: 1, flags: CFlags.None))
                {
                    _chatConfig.Data.ChatType = newType;
                    _chatConfig.Save();
                }
                CkGui.HelpTextFramed("Sets the channel messages are sent in for GlobalChat.", true);

                var chatPrefixCol = _chatConfig.Data.ChatColor;
                if (CkGuiUtils.ColorEditNativeForeground("Prefix Color", ref chatPrefixCol, defaultCol: GsDefaults.GlobalChatColor))
                {
                    _chatConfig.Data.ChatColor = chatPrefixCol;
                    _chatConfig.Save();
                }
            }
        }

        CkGui.FontText("Preferences:", Fonts.DefaultScaled);
        using var _ = ImRaii.PushIndent();
        var chatPerms = _chatConfig.Data.ChatPerms;

        var chatDispName = chatPerms.HasAny(ChatFlags.UseDisplayName);
        if (CkGui.Checkbox("Use Display Name##chat-dispname", ref chatDispName, UiService.DisableUI))
        {
            _chatConfig.Data.ChatPerms = chatPerms ^ ChatFlags.UseDisplayName;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("If a supporter, you can opt-in to use your DisplayName over your Anon-Kinkster name.", true);

        var curChatProfiles = chatPerms.HasAny(ChatFlags.AllowProfileViewing);
        if (CkGui.Checkbox("Allow Profile Viewing##chat-pfpview", ref curChatProfiles, UiService.DisableUI))
        {
            _chatConfig.Data.ChatPerms = chatPerms ^ ChatFlags.AllowProfileViewing;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("If others can view your profile from the chat.", true);

        var curChatDMs = chatPerms.HasAny(ChatFlags.AllowDirectMessages);
        if (CkGui.Checkbox("Allow Direct Messages##chat-dms", ref curChatDMs, true))
        {
            _chatConfig.Data.ChatPerms = chatPerms ^ ChatFlags.AllowDirectMessages;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("If others can send you private messages from the chat.", true);

        var curChatRequests = chatPerms.HasAny(ChatFlags.AllowRequests);
        if (CkGui.Checkbox("Allow Kinkster Requests##chat-requesting", ref curChatRequests, UiService.DisableUI))
        {
            _chatConfig.Data.ChatPerms = chatPerms ^ ChatFlags.AllowRequests;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Others can send you kinkster requests through the radar chat.", true);
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
