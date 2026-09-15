using CkCommons.Gui;
using CkCommons.Helpers;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;

namespace GagSpeak.Gui.Settings;

public partial class SettingsHubService
{
    private enum HubSettingsTabs
    {
        Reputation,
        Account,
        GlobalPerms,
        GlobalLoci,
        HubSelector,
    }

    private readonly ILogger<SettingsHubService> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _mainConfig;
    private readonly ConnectionsConfig _connections;
    private readonly AccountManager _account;
    private readonly KinkPlateService _profiles;


    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<HubSettingsTabs> _tabs;
    private string _customServerName = string.Empty;
    private string _customServerUri = string.Empty;
    private string? _timespanStrCache = null;
    public SettingsHubService(ILogger<SettingsHubService> logger,
        GagspeakMediator mediator, MainHub hub, MainConfig mainConfig, 
        ConnectionsConfig connections, AccountManager account, KinkPlateService profiles)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _mainConfig = mainConfig;
        _connections = connections;
        _account = account;
        _profiles = profiles;

        _tabs = new StylizedTabbarBuilder<HubSettingsTabs>()
            .AddTab(HubSettingsTabs.Reputation, "Reputation")
            .AddTab(HubSettingsTabs.Account, "Account")
            .AddTab(HubSettingsTabs.GlobalPerms, "Global Perms")
            .AddTab(HubSettingsTabs.GlobalLoci, "Global Loci")
            .AddTab(HubSettingsTabs.HubSelector, "Hub Selector")
            .Build();
    }
    public int NavbarIdx => (int)_tabs.TabSelection;
    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<HubSettingsTabs>().Length)
            return;
        _tabs.TabSelection = (HubSettingsTabs)idx;
    }


    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("service-hub-inner");

        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case HubSettingsTabs.Reputation:
                DrawReputation();
                break;
            case HubSettingsTabs.Account:
                DrawAccount();
                break;
            case HubSettingsTabs.GlobalPerms:
                DrawGlobalPerms();
                break;
            case HubSettingsTabs.GlobalLoci:
                DrawGlobalLoci();
                break;
            case HubSettingsTabs.HubSelector:
                DrawHubSelector();
                break;
        }
    }

    private void DrawHubSelector()
    {
        CkGui.FontText("Hub Selector", Fonts.DefaultScaled);
        var hubs = ConnectionsConfig.ServerHubs;
        var current = ConnectionsConfig.CurrentHub;

        CkGui.FramedIconText(FAI.GlobeAsia);
        if (ConnectionsConfig.MAIN_SERVER_URI == current.HubURI)
        {
            CkGui.TextFrameAlignedInline("Connected to");
            CkGui.ColorTextFrameAlignedInline(current.HubName, GsCol.LushPinkButton.Uint());
        }
        else
        {
            CkGui.TextFrameAlignedInline($"{current.HubName}, URI:");
            ImGui.SameLine();
            CkGui.TagLabelTextFrameAligned(current.HubURI, ImGuiColors.ParsedGold.Darken(.5f), 3 * ImGuiHelpers.GlobalScale);
        }

        CkGui.FramedIconText(FAI.Hdd);
        CkGui.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.5f);
        using (var c = ImRaii.Combo("Select Service", current.HubName))
        {
            if (c)
            {
                for (int i = 0; i < hubs.Count; i++)
                {
                    var selected = hubs[i] == current;
                    var isOfficial = hubs[i].HubURI == ConnectionsConfig.MAIN_SERVER_URI;
                    var displayString = isOfficial ? hubs[i].HubName : $"{hubs[i].HubName} ({hubs[i].HubURI})";
                    if (ImGui.Selectable(displayString, selected) && !selected)
                    {
                        _connections.SetHubIndex(i);
                        UiService.SetUITask(async () => await _hub.Reconnect(DisconnectIntent.Reload).ConfigureAwait(false));
                    }
                }
            }
        }
        if (ConnectionsConfig.CurrentHubIndex >= 2)
        {
            // Change this later to let you remove the service while not connected to it and stuff.
            ImGui.SameLine();
            if (CkGui.IconTextButton(FAI.Trash, "Remove Service"))
            {
                if (!_connections.RemoveHub(ConnectionsConfig.CurrentHub))
                    return;
                // Successful removal, reconnect.
                UiService.SetUITask(async () => await _hub.Reconnect(DisconnectIntent.Reload).ConfigureAwait(false));
            }
        }

        ImGui.Separator();
        ImGui.SetNextItemWidth(250);
        ImGui.InputText("Custom Service Name", ref _customServerName, 255);

        ImGui.SetNextItemWidth(250);
        ImGui.InputText("Custom Service URI", ref _customServerUri, 255);
        if (CkGui.IconTextButton(FAI.Plus, "Add Service"))
        {
            if (string.IsNullOrWhiteSpace(_customServerUri) || !Uri.IsWellFormedUriString(_customServerUri, UriKind.Absolute))
                return;
            // It is valid, so add the service.
            var newService = new ServerHubInfo() { HubName = _customServerName, HubURI = _customServerUri };
            if (!_connections.AddServerHub(newService))
                return;

            // Reset the input fields, and save the config.
            _customServerName = string.Empty;
            _customServerUri = string.Empty;
        }        
    }

    private void DrawReputation()
    {
        CkGui.FontText("Account Reputation", Fonts.SubtitleFont);

        if (MainHub.ConnectionResponse?.Reputation is not { } rep)
            return;

        ImGui.Text("Verified:");
        CkGui.BoolIcon(rep.IsVerified, true);

        ImGui.Text("Banned:");
        CkGui.BoolIcon(rep.IsBanned, true);

        ImGui.Text("Strikes:");
        CkGui.ColorTextInline($"{rep.WarningStrikes}", ImGuiColors.DalamudYellow);

        ImGui.Separator();
        CkGui.FontText("Details", Fonts.DefaultScaled);

        DrawCategoryStatus("Profile Viewing:", rep.CanViewProfiles, !rep.ProfileViewing, rep.ProfileViewTimeout, rep.ProfileViewStrikes);
        DrawCategoryStatus("Profile Editing:", rep.CanEditProfiles, !rep.ProfileEditing, rep.ProfileEditTimeout, rep.ProfileEditStrikes);
        DrawCategoryStatus("Radar Usage:", rep.CanUseRadar, !rep.RadarUsage, rep.RadarTimeout, rep.RadarStrikes);
        DrawCategoryStatus("Chat Usage:", rep.CanUseChat, !rep.ChatUsage, rep.ChatTimeout, rep.ChatStrikes);

        void DrawCategoryStatus(string label, bool canUse, bool usageBanned, DateTime timeout, int strikes)
        {
            ImGui.Spacing();
            ImGui.Text(label);
            CkGui.BoolIcon(canUse, true);
            CkGui.ColorTextInline($"({strikes} Strikes)", ImGuiColors.DalamudYellow, false);

            if (!canUse)
            {
                if (usageBanned)
                    CkGui.ColorTextInline(" - Access Revoked", ImGuiColors.DalamudRed, false);
                else if (timeout > DateTime.UtcNow)
                    CkGui.ColorTextInline($"- In Timeout for {(timeout - DateTime.UtcNow).ToTimeSpanStr()}..", ImGuiColors.DalamudOrange, false);
            }
        }
    }

    private void DrawGlobalPerms()
    {
        CkGui.FontText("GlobalPerms", Fonts.DefaultScaled);
    }

    private void DrawGlobalLoci()
    {
        CkGui.FontText("Global Loci Permissions", Fonts.DefaultScaled);
    }
}
