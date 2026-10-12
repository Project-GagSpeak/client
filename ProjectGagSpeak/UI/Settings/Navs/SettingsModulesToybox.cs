using CkCommons.Audio;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using ImSharp;
using OtterGui.Text;

namespace GagSpeak.Gui.Settings;

public class SettingsModulesToybox
{
    private enum PluginUiTabs
    {
        MyToys,
        PiShock,
    }

    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly PiShockProvider _shockProvider;

    // Include the individual TabBar for this selection, even if only 1 tab.
    private readonly StylizedTabbar<PluginUiTabs> _tabs;

    public SettingsModulesToybox(GagspeakMediator mediator, MainHub hub,
        MainConfig config, PiShockProvider shockProvider)
    {
        _mediator = mediator;
        _hub = hub;
        _config = config;
        _shockProvider = shockProvider;

        _tabs = new StylizedTabbarBuilder<PluginUiTabs>()
            .AddTab(PluginUiTabs.MyToys, "My Toys")
            .AddTab(PluginUiTabs.PiShock, "PiShock")
            .Build();
    }

    public int NavbarIdx => (int)_tabs.TabSelection;

    public void SetNavbarIdx(int idx)
    {
        if (idx < 0 || idx >= PluginUiTabs.Values.Count)
            return;
        _tabs.TabSelection = (PluginUiTabs)idx;
    }

    // All Content
    public void Draw(ImGuiWindowPtr winPtr)
    {
        // Draw out the tabs using the custom backgrounds and whatever else.
        _tabs.DrawTabs(GsTabBarFlags.MinimalGlow);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2f * ImGuiHelpers.GlobalScale);
        ImGui.Separator();
        ImGui.Spacing();
        using var _ = ImRaii.Child("main-notifs-inner");
        // Draw out the content based on the selected tab.
        switch (_tabs.TabSelection)
        {
            case PluginUiTabs.MyToys:
                DrawMyToys();
                break;
            case PluginUiTabs.PiShock:
                DrawPiShock();
                break;
        }
    }

    private void AssignGlobalPermChangeTask(GlobalPerms perms, string globalKey, object newValue)
        => UiService.SetUITask(async () => await PermHelper.ChangeOwnGlobal(_hub, perms, globalKey, newValue));


    private void DrawMyToys()
    {
        CkGui.FontText("Toybox", Fonts.SubtitleFont);
        if (ClientData.Globals is not { } globals)
            return;
        
        var toyboxEnabled = globals.ToyboxEnabled;
        var emitSpatialAudio = globals.SpatialAudio;
        var vibeLobbyNickname = _config.Data.NicknameInVibeRooms;
        var intifaceAutoConnect = _config.Data.IntifaceAutoConnect;
        var intifaceConnectionAddr = _config.Data.IntifaceConnectionSocket;

        if (ImGui.Checkbox(GSLoc.Settings.Options.ToyboxActive, ref toyboxEnabled))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.ToyboxEnabled), toyboxEnabled);
        CkGui.HelpText(GSLoc.Settings.Options.ToyboxActiveTT);

        if (ImGui.Checkbox(GSLoc.Settings.Options.SpatialAudioActive, ref emitSpatialAudio))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.SpatialAudio), emitSpatialAudio);
        CkGui.HelpText(GSLoc.Settings.Options.SpatialAudioActiveTT);

        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText(GSLoc.Settings.Options.VibeLobbyNickname, ref vibeLobbyNickname, 25, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _config.Data.NicknameInVibeRooms = vibeLobbyNickname;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Options.VibeLobbyNicknameTT);

        if (ImGui.Checkbox(GSLoc.Settings.Options.IntifaceAutoConnect, ref intifaceAutoConnect))
        {
            _config.Data.IntifaceAutoConnect = intifaceAutoConnect;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Options.IntifaceAutoConnectTT);

        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint($"Server Address##ConnectionWSaddr", "Leave blank for default...", ref intifaceConnectionAddr, 100))
        {
            if (!intifaceConnectionAddr.Contains("ws://"))
                intifaceConnectionAddr = "ws://localhost:12345";
            else
            {
                _config.Data.IntifaceConnectionSocket = intifaceConnectionAddr;
                _config.Save();
            }
        }
        CkGui.HelpText(GSLoc.Settings.Options.IntifaceAddressTT);

        // Call our new audio configuration block
        DrawSimulatedAudioSettings();
    }

    private void DrawPiShock()
    {
        CkGui.FontText("Pi-Shock Integration", Fonts.SubtitleFont);
        var apiKey = _config.Data.PiShockApiKey;

        var inputWidth = 250 * ImGuiHelpers.GlobalScale;
        var saveWidth = CkGui.IconTextButtonSize(FAI.PlugCircleCheck, "Save & Connect");

        ImGui.SetNextItemWidth(inputWidth - saveWidth - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.InputText("##PiShock API Key", ref apiKey, 100);
        CkGui.AttachTooltip(GSLoc.Settings.Options.PiShockKeyTT);

        CkGui.TextInline("API Key");
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.PlugCircleCheck, "Save & Connect", disabled: UiService.DisableUI || string.IsNullOrEmpty(apiKey)))
        {
            _config.Data.PiShockApiKey = apiKey;
            _config.Save();
            UiService.SetUITask(async () => await _shockProvider.ConnectAsync());
        }
        CkGui.AttachTooltip("Save your API key and fetch your connected PiShock devices.");

        ImGui.Spacing();

        switch (_shockProvider.LastConnectState)
        {
            case PiShockProvider.ConnectState.NotAttempted:
                if (!_shockProvider.IsConfigured)
                    CkGui.ColorText("Enter your API Key, then click Save & Connect.", ImGuiColors.DalamudGrey);
                else
                    CkGui.ColorText("Click Save & Connect to detect your PiShock devices.", ImGuiColors.DalamudYellow);
                break;

            case PiShockProvider.ConnectState.AuthFailed:
                CkGui.ColorText("Authentication failed - check your API Key.", ImGuiColors.DalamudRed);
                break;

            case PiShockProvider.ConnectState.NetworkError:
                CkGui.ColorText("Connection error - check your internet or PiShock status.", ImGuiColors.DalamudRed);
                break;

            case PiShockProvider.ConnectState.Success when _shockProvider.ShockerCount == 0:
                CkGui.ColorText("Connected - no devices found. Check your PiShock account.", ImGuiColors.DalamudYellow);
                break;

            case PiShockProvider.ConnectState.Success:
                {
                    var shockers = _shockProvider.CachedShockers;
                    CkGui.ColorText($"{shockers.Count} Shocker(s) found. Default device for triggers:", ImGuiColors.DalamudGrey);

                    var currentId = _config.Data.GlobalShockerId;
                    var currentName = shockers.FirstOrDefault(s => s.Id == currentId).Name ?? "Select a device...";
                    ImGui.SetNextItemWidth(inputWidth);
                    using (var combo = ImRaii.Combo("##GlobalShocker", currentName))
                    {
                        if (combo)
                        {
                            foreach (var (id, name) in shockers)
                            {
                                if (ImGui.Selectable(name, id == currentId))
                                {
                                    _config.Data.GlobalShockerId = id;
                                    _config.Save();
                                }
                            }
                        }
                    }
                    CkGui.AttachTooltip("The device used when a trigger fires a shock action.");
                    ImGui.Spacing();
                    CkGui.ColorText("Configure a share code in each pair's permissions.", ImGuiColors.DalamudGrey);
                    break;
                }
        }
    }

    private void DrawSimulatedAudioSettings()
    {
        CkGui.FontText("Simulated Audio Output", Fonts.HeaderFont);
        CkGui.ColorText("This is still in development and very buggy, please bear with me.", ImGuiColors.DalamudYellow);

        var currentOutType = _config.Data.AudioOutputType;
        var currentDeviceId = _config.Data.AudioOutputDevice;

        // 1. Output API Type Selection
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        using (var c = ImRaii.Combo("Audio API", currentOutType.ToString()))
        {
            if (c)
            {
                foreach (OutputType type in Enum.GetValues<OutputType>())
                {
                    if (ImGui.Selectable(type.ToString(), type == currentOutType))
                    {
                        _config.Data.AudioOutputType = type;
                        _config.Data.AudioOutputDevice = string.Empty;
                        _config.Save();
                        // Immediately apply the new API to the backend
                        AudioSystem.InitializeOutputDevice(type, string.Empty);
                    }
                }
            }
        }
        CkGui.AttachTooltip("The audio interface used to route simulated toy sounds. (DirectSound is safest)");

        var currentDeviceName = GetCurrentDeviceName(currentOutType, currentDeviceId);
        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        using (var devCombo = ImRaii.Combo("Output Device", currentDeviceName))
        {
            if (devCombo)
                DrawDeviceSelectables(currentOutType, currentDeviceId);
        }
        CkGui.AttachTooltip("The physical device (speakers/headphones) to play simulated toy sounds through.");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.ArrowsSpin, "Refresh List"))
        {
            AudioSystem.FetchLatestAudioDevices();
            AudioSystem.InitializeOutputDevice(_config.Data.AudioOutputType, _config.Data.AudioOutputDevice);
        }
    }

    private void DrawDeviceSelectables(OutputType type, string currentId)
    {
        switch (type)
        {
            case OutputType.DirectSound:
                foreach (var (guid, name) in AudioSystem.DirectSoundAudioDevices)
                    if (ImGui.Selectable(name, guid.ToString() == currentId))
                        ApplyNewDevice(guid.ToString());
                break;

            case OutputType.Wasapi:
                foreach (var (id, name) in AudioSystem.WasapiAudioDevices)
                    if (ImGui.Selectable(name, id == currentId))
                        ApplyNewDevice(id);
                break;

            case OutputType.Asio:
                foreach (var name in AudioSystem.AsioAudioDevices)
                    if (ImGui.Selectable(name, name == currentId))
                        ApplyNewDevice(name);
                break;
        }
    }

    private void ApplyNewDevice(string newDeviceId)
    {
        _config.Data.AudioOutputDevice = newDeviceId;
        _config.Save();
        AudioSystem.InitializeOutputDevice(_config.Data.AudioOutputType, newDeviceId);
    }

    private string GetCurrentDeviceName(OutputType type, string id)
    {
        if (string.IsNullOrEmpty(id) || id == Guid.Empty.ToString())
            return "Default";

        return type switch
        {
            OutputType.DirectSound => Guid.TryParse(id, out var guid) && AudioSystem.DirectSoundAudioDevices.TryGetValue(guid, out var name) ? name : "Unknown Device",
            OutputType.Wasapi => AudioSystem.WasapiAudioDevices.TryGetValue(id, out var name) ? name : "Unknown Device",
            OutputType.Asio => AudioSystem.AsioAudioDevices.Contains(id) ? id : "Unknown Device",
            _ => "Unknown Device"
        };
    }
}
