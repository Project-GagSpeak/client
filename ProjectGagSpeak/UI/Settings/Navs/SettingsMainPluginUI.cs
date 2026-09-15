using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Gui.Settings;

public class SettingsMainPluginUI
{
    private enum PluginUiTabs
    {
        MainUI,
        Users,
        Stylizer,
    }

    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly WhitelistDrawSystem _whitelistDDS;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<PluginUiTabs> _tabs;

    public SettingsMainPluginUI(GagspeakMediator mediator, MainConfig config, WhitelistDrawSystem whitelistDDS)
    {
        _mediator = mediator;
        _config = config;
        _whitelistDDS = whitelistDDS;

        _tabs = new StylizedTabbarBuilder<PluginUiTabs>()
            .AddTab(PluginUiTabs.MainUI, "Main UI")
            .AddTab(PluginUiTabs.Users, "Users")
            .AddTab(PluginUiTabs.Stylizer, "UI Style", FAI.Palette)
            .Build();
    }

    public int NavbarIdx => (int)_tabs.TabSelection;

    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<PluginUiTabs>().Length)
            return;
        _tabs.TabSelection = (PluginUiTabs)idx;
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
            case PluginUiTabs.MainUI:
                DrawMainUIOptions();
                break;
            case PluginUiTabs.Users:
                DrawUserOptions();
                break;
            case PluginUiTabs.Stylizer:
                DrawStylizer();
                break;
        }
    }

    private void DrawMainUIOptions()
    {
        CkGui.FontText("Main UI", Fonts.SubtitleFont);

        var autoOpen = _config.Data.OpenUiOnStartup;
        if (ImGui.Checkbox(GSLoc.Settings.Options.ShowMainUiOnStartLabel, ref autoOpen))
        {
            _config.Data.OpenUiOnStartup = autoOpen;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.ShowMainUiOnStartTT, true);

        var showVisibleFolder = _config.Data.VisibleFolder;
        if (ImGui.Checkbox("Show Visible folder.", ref showVisibleFolder))
        {
            _config.Data.VisibleFolder = showVisibleFolder;
            _config.Save();
            _whitelistDDS.UpdateVisibleFolderState(showVisibleFolder);
        }
        CkGui.HelpTextFramed(GSLoc.Settings.DDSPrefs.ShowVisibleSeparateTT, true);

        var showOfflineFolder = _config.Data.OfflineFolder;
        if (ImGui.Checkbox("Show Offline folder.", ref showOfflineFolder))
        {
            _config.Data.OfflineFolder = showOfflineFolder;
            _config.Save();
            _whitelistDDS.UpdateOfflineFolderState(showOfflineFolder);
        }
        CkGui.HelpTextFramed(GSLoc.Settings.DDSPrefs.ShowOfflineSeparateTT, true);
    }

    private void DrawUserOptions()
    {
        CkGui.FontText("Users", Fonts.SubtitleFont);

        var useFocusTarget = _config.Data.UseFocusTargetOnUsers;
        if (ImGui.Checkbox(GSLoc.Settings.DDSPrefs.FocusTargetLabel, ref useFocusTarget))
        {
            _config.Data.UseFocusTargetOnUsers = useFocusTarget;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.DDSPrefs.FocusTargetTT, true);

        var nicksOverNames = _config.Data.UseNicksOverPlayerNames;
        if (ImGui.Checkbox(GSLoc.Settings.DDSPrefs.PreferNicknamesLabel, ref nicksOverNames))
        {
            _config.Data.UseNicksOverPlayerNames = nicksOverNames;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.DDSPrefs.PreferNicknamesTT, true);

        // Profiles ---
        var showProfiles = _config.Data.ShowProfiles;
        if (ImGui.Checkbox(GSLoc.Settings.Options.ShowProfilesLabel, ref showProfiles))
        {
            _config.Data.ShowProfiles = showProfiles;
            _config.Save();
        }
        CkGui.HelpTextFramed(GSLoc.Settings.Options.ShowProfilesTT, true);

        using (ImRaii.Disabled(!showProfiles))
        {
            using (ImRaii.PushIndent())
            {
                var popoutDelay = _config.Data.ProfileDelay;
                ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
                if (ImGui.SliderFloat("##Profile-Delay", ref popoutDelay, 0.3f, 5, $"%.1f {GSLoc.Settings.Options.ProfileDelayLabel}"))
                {
                    _config.Data.ProfileDelay = popoutDelay;
                    _config.Save();
                }
                CkGui.AttachTooltip(GSLoc.Settings.Options.ProfileDelayTT, true);
            }
        }
    }

    private void DrawStylizer()
    {
        CkGui.FontText("Use at your own risk.", Fonts.DefaultScaled, ImGuiColors.DalamudYellow);
        CkGui.TextWrapped("This is very WIP and adjusting any values in this editor will most likely" +
            "crash your game outright.");
        CkGui.TextFrameAligned("This tool is primarily used for development.");
        ImGui.Spacing();
        if (CkGui.IconTextButton(FAI.Palette, "Open Style Editor UI"))
            _mediator.Publish(new UiToggleMessage(typeof(StyleEditorUI)));
    }
}
