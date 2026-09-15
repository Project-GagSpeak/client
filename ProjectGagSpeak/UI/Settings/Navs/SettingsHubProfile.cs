using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Profile;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using GagspeakAPI.User;

namespace GagSpeak.Gui.Settings;

public class SettingsHubProfile
{
    private enum HubProfileTabs
    {
        Profile,
        Vanity,
    }

    private readonly ILogger<SettingsHubProfile> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly KinkPlateService _kinkplates;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<HubProfileTabs> _tabs;

    public SettingsHubProfile(ILogger<SettingsHubProfile> logger, GagspeakMediator mediator,
        MainHub hub, MainConfig mainConfig, KinkPlateService profiles)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _config = mainConfig;
        _kinkplates = profiles;

        _tabs = new StylizedTabbarBuilder<HubProfileTabs>()
            .AddTab(HubProfileTabs.Profile, "Profile")
            .AddTab(HubProfileTabs.Vanity, "Vanity Benefits")
            .Build();
    }
    public int NavbarIdx => (int)_tabs.TabSelection;
    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= Enum.GetValues<HubProfileTabs>().Length)
            return;
        _tabs.TabSelection = (HubProfileTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(TabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("service-profile-inner");

        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case HubProfileTabs.Profile:
                DrawProfileOptions();
                break;
            case HubProfileTabs.Vanity:
                DrawVanityOptions();
                break;
            //case HubProfileTabs.About:
            //    DrawAboutInfo();
            //    break;
        }
    }

    private void DrawProfileOptions()
    {
        CkGui.FontText("Profile", Fonts.DefaultScaled);

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
                CkGui.HelpTextFramed(GSLoc.Settings.Options.ProfileDelayTT);
            }
        }

        ImGui.Spacing();
        if (CkGui.IconTextButton(FAI.Images, "Open KinkPlate Editor"))
            _mediator.Publish(new UiToggleMessage(typeof(KinkPlateEditorUI), ToggleType.Show));
        CkGui.AttachTooltip("Opens the KinkPlate editor for your user.");
    }

    private string? _tmpAlias;
    private string? _tmpDispName;
    private NativeUiColor? _tmpColors;

    private void DrawVanityOptions()
    {
        CkGui.FontText("Vanity Benefits", Fonts.DefaultScaled);
        if (MainHub.OwnUserData is not { } userData)
            return;

        // do a Lazy assignment
        _tmpAlias ??= userData.Alias ?? string.Empty;
        _tmpDispName ??= userData.VanityName ?? string.Empty;
        // locally store the saved colors 
        var prevSaved = new NativeUiColor(Foreground: userData.Color ?? default);
        _tmpColors ??= prevSaved;

        var isDonor = userData.Tier is not CkVanityTier.NoRole;

        // Alias
        CkGui.TextUnderlined("Alias");
        CkGui.HelpText("Used in place of your UID for the displayed name.", true);

        if (VanityAliasChanged() && userData.Alias is not null)
            CkGui.ColorTextInline($"(Current: {userData.Alias})", ImGuiColors.DalamudViolet);

        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##vanity-alias", "Alias..", ref _tmpAlias, 15);
        var validAlias = IsValidName(_tmpAlias);
        if (!validAlias)
            CkGui.ColorTextWrapped("Must be 4-15 characters with no spaces (underscores & dashes allowed)", ImGuiColors.DalamudYellow);

        ImGui.Spacing();
        CkGui.TextUnderlined("Vanity Name");
        CkGui.HelpText("Displayed in place of Anon-User names for Radar, RadarGroup, and RadarChat.", true);
        if (VanityNameChanged() && userData.VanityName is not null)
            CkGui.ColorTextInline($" (Current: {userData.VanityName})", ImGuiColors.DalamudViolet);

        using (ImRaii.Disabled(!isDonor))
        {
            ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
            var maxLen = userData.Tier is CkVanityTier.KinkporiumMistress ? 15 : 10;
            ImGui.InputTextWithHint("##vanity-name", "Vanity name..", ref _tmpDispName, maxLen);
        }
        CkGui.AttachTooltip("Only supporters can set a vanity name", isDonor);

        var validVanityName = IsValidVanityName(_tmpDispName) || userData.Tier is CkVanityTier.KinkporiumMistress;
        if (!validVanityName)
            CkGui.ColorTextWrapped("Must be 4-10 characters with no spaces", ImGuiColors.DalamudYellow);

        ImGui.Spacing();
        CkGui.TextUnderlined("Name Appearance");
        CkGui.HelpText("Colors all displays of your VanityName/Alias/UID", true);

        using (ImRaii.Disabled(!isDonor))
        {
            var colors = _tmpColors.Value;
            if (CkGuiUtils.ColorEditNativeForeground("DisplayName Color", ref colors, GsCol.VibrantPink.Uint(), new(Foreground: uint.MinValue)))
                _tmpColors = colors;
        }
        if (!isDonor)
            CkGui.ColorTextWrapped("Only supporters can edit their DisplayName Color!",  CkCol.TriStateCross.Vec4());

        var canSubmit = VanityAnythingChanged() && validAlias && validVanityName && !UiService.DisableUI;
        if (CkGui.IconTextButton(FAI.Sync, "Update UserData", disabled: !canSubmit || !MainHub.IsConnectionDataSynced))
        {
            UiService.SetUITask(async () =>
            {
                var aliasUpdate = GetAliasUpdate();
                var vanityUpdate = GetVanityNameUpdate();
                var colorUpdate = GetColorUpdate();
                var dto = new UserDataUpdate(aliasUpdate, vanityUpdate, colorUpdate, null);

                var ret = await _hub.UserUpdateData(dto).ConfigureAwait(false);
                if (ret.ErrorCode is not GagSpeakApiEc.Success)
                {
                    _logger.LogWarning($"Failed to set new VanityData: {ret.ErrorCode}");
                    _tmpDispName = null;
                    _tmpAlias = null;
                    _tmpColors = null;
                    return;
                }
                // Update local state conditionally.
                // If the update string was empty, set it to null locally. 
                // If it was null, retain the existing local value.
                var newUserData = MainHub.ConnectionResponse!.User with
                {
                    Alias = aliasUpdate != null ? (aliasUpdate == string.Empty ? null : aliasUpdate) : userData.Alias,
                    VanityName = vanityUpdate != null ? (vanityUpdate == string.Empty ? null : vanityUpdate) : userData.VanityName,
                    Color = colorUpdate ?? userData.Color,
                };
                MainHub.ConnectionResponse = MainHub.ConnectionResponse with
                {
                    User = newUserData
                };
                _tmpDispName = null;
                _tmpAlias = null;
                _tmpColors = null;
            });
        }

        string? GetAliasUpdate()
        {
            var current = userData.Alias ?? string.Empty;
            var tmp = _tmpAlias ?? string.Empty;
            return current == tmp ? null : tmp;
        }

        string? GetVanityNameUpdate()
        {
            var current = userData.VanityName ?? string.Empty;
            var tmp = _tmpDispName ?? string.Empty;
            return current == tmp ? null : tmp;
        }

        uint? GetColorUpdate()
        {
            return !_tmpColors.Value.Foreground.Equals(prevSaved.Foreground) ? _tmpColors.Value.Foreground : null;
        }

        bool VanityAliasChanged()
            => GetAliasUpdate() is not null;

        bool VanityNameChanged()
            => GetVanityNameUpdate() is not null;

        bool VanityColorChanged()
            => GetColorUpdate() is not null;

        bool VanityAnythingChanged()
            => VanityAliasChanged() || VanityNameChanged() || VanityColorChanged();

        bool IsValidName(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return value.Length is >= 5 and <= 15 &&
                   value.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');
        }

        bool IsValidVanityName(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;
            return value.Length is >= 4 and <= 10 && value.All(c => char.IsLetterOrDigit(c));
        }
    }
}
