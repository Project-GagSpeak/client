using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using OtterGui.Text;
using TerraFX.Interop.Windows;

namespace GagSpeak.Gui.Settings;

public class SettingsMainNativeUI
{
    private enum NativeUiTabs
    {
        Nameplates,
        ContextMenus,
        DtrEntries,
        Chat,
    }

    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly ChatConfig _chatConfig;
    private readonly ClientDataListener _clientDatListener;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<NativeUiTabs> _tabs;

    public SettingsMainNativeUI(GagspeakMediator mediator, MainHub hub,
        MainConfig config, ChatConfig chatConfig, ClientDataListener clientListener)
    {
        _mediator = mediator;
        _hub = hub;
        _config = config;
        _chatConfig = chatConfig;
        _clientDatListener = clientListener;

        _tabs = new StylizedTabbarBuilder<NativeUiTabs>()
            .AddTab(NativeUiTabs.Nameplates, "Nameplates")
            .AddTab(NativeUiTabs.ContextMenus, "Context Menus")
            .AddTab(NativeUiTabs.DtrEntries, "DTR Entries")
            .AddTab(NativeUiTabs.Chat, "Native Chat")
            .Build();
    }
    public int NavbarIdx => (int)_tabs.TabSelection;

    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<NativeUiTabs>().Length)
            return;
        _tabs.TabSelection = (NativeUiTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("main-native-inner");
        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case NativeUiTabs.Nameplates:   DrawNameplatesOptions();    break;
            case NativeUiTabs.ContextMenus: DrawContextMenusOptions();  break;
            case NativeUiTabs.DtrEntries:   DrawDtrOptions();           break;
            case NativeUiTabs.Chat:         DrawChatIntegrations();     break;
        }
    }

    private void DrawNameplatesOptions()
    {
        CkGui.FontText("Nameplates", Fonts.SubtitleFont);

        // Gagplates
        DrawGagplateRow();

        // PlayerName Highlights
        var highlightPairedPlayers = _config.Data.PlateHighlightKinksters;
        if (ImGui.Checkbox("Highlight paired players", ref highlightPairedPlayers))
        {
            _config.Data.PlateHighlightKinksters = highlightPairedPlayers;
            _config.Save();
            Svc.NamePlate.RequestRedraw();
        }
        CkGui.AttachTooltip("Replaces the nameplate colors of paired players with the defined color.");
        using (ImRaii.Disabled(!highlightPairedPlayers))
        {
            using (ImRaii.PushIndent())
            {
                var pairNameColor = _config.Data.KinksterHighlight;
                if (CkGuiUtils.ColorEditNative("Name Color", ref pairNameColor, defaultCol: GsDefaults.NameplateColorKinkster))
                {
                    _config.Data.KinksterHighlight = pairNameColor;
                    _config.Save();
                }

                var includeFriends = _config.Data.PlateIncludeFriendHighlights;
                if (ImGui.Checkbox("Include Friends", ref includeFriends))
                {
                    _config.Data.PlateIncludeFriendHighlights = includeFriends;
                    _config.Save();
                    Svc.NamePlate.RequestRedraw();
                }
                CkGui.AttachTooltip("If the highlights are applied to users on your friend list.", hoverFlags: ImGuiHoveredFlags.AllowWhenDisabled);
            }
        }

        void DrawGagplateRow()
        {
            if (ClientData.Globals is not { } globals)
                return;

            var gagplates = globals.GaggedNameplate;

            if (CkGui.Checkbox(GSLoc.Settings.Options.GaggedNameplates, ref gagplates, globals.ChatGarblerLocked))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.GaggedNameplate), gagplates);
            CkGui.HelpTextFramed(GSLoc.Settings.Options.GaggedNameplatesTT, true);
        }
    }

    private void DrawContextMenusOptions()
    {
        CkGui.FontText("Context Menus", Fonts.SubtitleFont);

        var contextMenus = _config.Data.ShowContextMenus;
        if (ImGui.Checkbox(GSLoc.Settings.Options.ContextMenusLabel, ref contextMenus))
        {
            _config.Data.ShowContextMenus = contextMenus;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.ContextMenusTT);
    }

    private void DrawDtrOptions()
    {
        CkGui.FontText("DTR Entries", Fonts.SubtitleFont);

        var requestsDtr = _config.Data.AlertKind.HasAny(AlertKind.DtrBar);
        if (ImGui.Checkbox("Enable Requests DTR", ref requestsDtr))
        {
            _config.Data.AlertKind ^= AlertKind.DtrBar;
            _config.Save();
            _mediator.Publish(new DTRRefreshMessage());
        }
        CkGui.HelpTextFramed("Displays your incoming and outoing requests in the DTR bar.", true);

        var privacyDtr = _config.Data.DtrPrivacy;
        if (ImGui.Checkbox(GSLoc.Settings.Options.PrivacyRadarLabel, ref privacyDtr))
        {
            _config.Data.DtrPrivacy = privacyDtr;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.PrivacyRadarTT, true);

        var actionNotifsDtr = _config.Data.DtrActionNotifs;
        if (ImGui.Checkbox(GSLoc.Settings.Options.ActionsNotifLabel, ref actionNotifsDtr))
        {
            _config.Data.DtrActionNotifs = actionNotifsDtr;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.ActionsNotifTT, true);

        var vibeDtr = _config.Data.DtrVibeStatus;
        if (ImGui.Checkbox(GSLoc.Settings.Options.VibeStatusLabel, ref vibeDtr))
        {
            _config.Data.DtrVibeStatus = vibeDtr;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.VibeStatusTT, true);
    }

    private void DrawChatIntegrations()
    {
        CkGui.FontText("Native Chat", Fonts.SubtitleFont);

        var legacyUid = _config.Data.UseLegacyAnonName;
        if (ImGui.Checkbox(GSLoc.Settings.Options.PrefThreeCharaAnonName, ref legacyUid))
        {
            _config.Data.UseLegacyAnonName = legacyUid;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.PrefThreeCharaAnonNameTT, true);

        var useNative = _chatConfig.Data.UseNativeChat;
        if (ImGui.Checkbox("Show GlobalChat", ref useNative))
        {
            _chatConfig.Data.UseNativeChat = useNative;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Includes messages from GlobalChat.", true);

        using (ImRaii.Disabled(!useNative))
        {
            using (ImRaii.PushIndent())
            {
                if (CkGuiUtils.EnumCombo("Channel", 200f, _chatConfig.Data.ChatType, out var newType, skip: 1, flags: CFlags.None))
                {
                    _chatConfig.Data.ChatType = newType;
                    _chatConfig.Save();
                }
                CkGui.HelpTextFramed("Will attempt to get it looking good on more than just debug later." +
                    "--NL----COL--Unfortunitely due to limitations we cannot set custom log filter values on chat log panels yet.--COL--", ImGuiColors.DalamudGrey2);
                var chatPrefixCol = _chatConfig.Data.ChatColor;
                if (CkGuiUtils.ColorEditNativeForeground("Chat Color", ref chatPrefixCol, defaultCol: GsDefaults.GlobalChatColor))
                {
                    _chatConfig.Data.ChatColor = chatPrefixCol;
                    _chatConfig.Save();
                }
            }
        }

        ImGui.Spacing();
        var useDMsNative = _chatConfig.Data.ShowDMsInChatbox;
        if (ImGui.Checkbox("Show Direct Messages", ref useDMsNative))
        {
            _chatConfig.Data.ShowDMsInChatbox = useDMsNative;
            _chatConfig.Save();
        }
        CkGui.HelpTextFramed("Includes DirectMessages sent via Sundouleia." +
            "--NL----COL--This is not yet integrated due to feedback.--COL--", CkCol.TriStateCross.Uint(), true);

        using (ImRaii.Disabled(!useDMsNative))
        {
            using (ImRaii.PushIndent())
            {
                var chatPrefixCol = _chatConfig.Data.DMPrefixColor;
                if (CkGuiUtils.ColorEditNativeForeground("Prefix Color", ref chatPrefixCol, defaultCol: GsDefaults.DMColorPrefix))
                {
                    _chatConfig.Data.DMPrefixColor = chatPrefixCol;
                    _chatConfig.Save();
                }
                ImGui.SameLine();
                var chatTextCol = _chatConfig.Data.DMTextColor;
                if (CkGuiUtils.ColorEditNativeForeground("Text Color", ref chatTextCol, defaultCol: GsDefaults.DMColorText))
                {
                    _chatConfig.Data.DMTextColor = chatTextCol;
                    _chatConfig.Save();
                }
            }
        }
    }

    private void AssignGlobalPermChangeTask(GlobalPerms perms, string globalKey, object newValue)
        => UiService.SetUITask(async () => await PermHelper.ChangeOwnGlobal(_hub, perms, globalKey, newValue));

    private void AssignShockPermBulkTask(GlobalPerms perms, GlobalPerms updated)
        => UiService.SetUITask(async () =>
        {
            if (ClientData.IsNull) return;
            var res = await _hub.UserBulkChangeGlobal(new(MainHub.OwnUserData, updated, ClientData.HardcoreClone() ?? new HardcoreState()));
            if (res.ErrorCode is GagSpeakApiEc.Success)
                _clientDatListener.ChangeAllGlobalPerms(updated);
        });
}
