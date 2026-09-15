using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;

namespace GagSpeak.Gui.Settings;

public class SettingsModulesPuppeteer
{
    private enum PluginUiTabs
    {
        Globals,
        Listeners,
        Channels,
    }

    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly WhitelistDrawSystem _whitelistDDS;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<PluginUiTabs> _tabs;

    public SettingsModulesPuppeteer(GagspeakMediator mediator, MainHub hub,
        MainConfig config, WhitelistDrawSystem whitelistDDS)
    {
        _mediator = mediator;
        _hub = hub;
        _config = config;
        _whitelistDDS = whitelistDDS;

        _tabs = new StylizedTabbarBuilder<PluginUiTabs>()
            .AddTab(PluginUiTabs.Globals, "Global Settings")
            .AddTab(PluginUiTabs.Channels, "Enabled Channels")
            .AddTab(PluginUiTabs.Listeners, "Listeners")
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
            case PluginUiTabs.Globals:
                DrawGlobals();
                break;
            case PluginUiTabs.Listeners:
                DrawListeners();
                break;
            case PluginUiTabs.Channels:
                DrawChannels();
                break;
        }
    }

    private void AssignGlobalPermChangeTask(GlobalPerms perms, string globalKey, object newValue)
        => UiService.SetUITask(async () => await PermHelper.ChangeOwnGlobal(_hub, perms, globalKey, newValue));


    private void DrawGlobals()
    {
        CkGui.FontText("Global Permissions", Fonts.SubtitleFont);
        if (ClientData.Globals is not { } globals)
        {
            ImGui.Text("Global Perms is null! Safely returning early");
            return;
        }

        var puppeteerEnabled = globals.PuppeteerEnabled;
        var globalTriggerPhrase = globals.TriggerPhrase;
        var globalPuppetPerms = globals.PuppetPerms;

        if (ImGui.Checkbox(GSLoc.Settings.Options.PuppeteerActive, ref puppeteerEnabled))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppeteerEnabled), puppeteerEnabled);
        CkGui.HelpText(GSLoc.Settings.Options.PuppeteerActiveTT);

        using (ImRaii.Disabled(!puppeteerEnabled))
        {
            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint(GSLoc.Settings.Options.GlobalTriggerPhrase, "Global Triggers...", ref globalTriggerPhrase, 150);
            if (ImGui.IsItemDeactivatedAfterEdit())
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.TriggerPhrase), globalTriggerPhrase);
            CkGui.HelpText(GSLoc.Settings.Options.GlobalTriggerPhraseTT);

            // Correct these!
            var refSits = (globalPuppetPerms & PuppetPerms.Sit) == PuppetPerms.Sit;
            if (ImGui.Checkbox(GSLoc.Settings.Options.GlobalSit, ref refSits))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Sit);
            CkGui.HelpText(GSLoc.Settings.Options.GlobalSitTT);

            var refEmotes = (globalPuppetPerms & PuppetPerms.Emotes) == PuppetPerms.Emotes;
            if (ImGui.Checkbox(GSLoc.Settings.Options.GlobalMotion, ref refEmotes))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Emotes);
            CkGui.HelpText(GSLoc.Settings.Options.GlobalMotionTT);

            var refAlias = (globalPuppetPerms & PuppetPerms.Alias) == PuppetPerms.Alias;
            if (ImGui.Checkbox(GSLoc.Settings.Options.GlobalAlias, ref refAlias))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Alias);
            CkGui.HelpText(GSLoc.Settings.Options.GlobalAliasTT);

            var refAllPerms = (globalPuppetPerms & PuppetPerms.All) == PuppetPerms.All;
            if (ImGui.Checkbox(GSLoc.Settings.Options.GlobalAll, ref refAllPerms))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.All);
            CkGui.HelpText(GSLoc.Settings.Options.GlobalAllTT);
        }
    }

    private void DrawListeners()
    {
        CkGui.FontText("Listeners", Fonts.SubtitleFont);
        CkGui.ColorText("to be determined...", ImGuiColors.DalamudYellow);
    }

    private void DrawChannels()
    {
        CkGui.FontText("Enabled Channels", Fonts.SubtitleFont);
        using var _ = ImRaii.Group();

        foreach (var (label, channels) in ChatLogAgent.SortedChannels)
        {
            ImGui.Text(label);
            for (var i = 0; i < channels.Length; i++)
            {
                var channel = channels[i];
                var enabled = _config.Data.PuppeteerChannelsBitfield.IsActiveChannel((int)channel);
                var checkboxLabel = channel.ToString() + " "; // space for unique name in ImGui to avoid conflict with garble channels

                if (ImGui.Checkbox(checkboxLabel, ref enabled))
                {
                    var newBitfield = _config.Data.PuppeteerChannelsBitfield.SetChannelState((int)channel, enabled);
                    _config.Data.PuppeteerChannelsBitfield = newBitfield;
                    _config.Save();
                }

                // Only SameLine if not the third column
                if ((i + 1) % 4 != 0 && (i + 1) != channels.Length)
                    ImGui.SameLine();
            }
        }
    }
}
