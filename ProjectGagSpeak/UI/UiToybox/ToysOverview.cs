using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox.Controllers;
using GagSpeak.Toybox.Data;
using GagSpeak.Toybox.Services;
using GagSpeak.UI.UiRemote;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.UiToybox;

public class ToyboxOverview
{
    private readonly ILogger<ToyboxOverview> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiShared;
    private readonly PlayerCharacterData _playerManager;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly VibratorService _vibeService;
    private readonly TutorialService _guides;

    public ToyboxOverview(ILogger<ToyboxOverview> logger, GagspeakMediator mediator,
        UiSharedService uiSharedService, PlayerCharacterData playerData,
        ClientConfigurationManager clientConfigs, ServerConfigurationManager serverConfigs,
        VibratorService vibeService, TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _uiShared = uiSharedService;
        _playerManager = playerData;
        _clientConfigs = clientConfigs;
        _serverConfigs = serverConfigs;
        _vibeService = vibeService;
        _guides = guides;

        // grab path to the intiface
        if (ToyboxHelper.AppPath == string.Empty)
            ToyboxHelper.GetApplicationPath();
    }

    public void DrawOverviewPanel()
    {
        // draw the top display field for Intiface connectivity, similar to our other servers.
        DrawIntifaceConnectionStatus();
        // special case for the intiface connection, where if it is empty, we reset it to the default address.
        if (string.IsNullOrEmpty(_clientConfigs.GagspeakConfig.IntifaceConnectionSocket))
        {
            _clientConfigs.GagspeakConfig.IntifaceConnectionSocket = "ws://localhost:12345";
            _clientConfigs.Save();
        }

        // display a dropdown for the type of vibrator to use
        ImGui.SetNextItemWidth(125f);
        if (ImGui.BeginCombo("Set Vibrator Type##VibratorMode", _clientConfigs.GagspeakConfig.VibratorMode.ToString()))
        {
            foreach (VibratorMode mode in Enum.GetValues(typeof(VibratorMode)))
            {
                if (ImGui.Selectable(mode.ToString(), mode == _clientConfigs.GagspeakConfig.VibratorMode))
                {
                    _clientConfigs.GagspeakConfig.VibratorMode = mode;
                    _clientConfigs.Save();
                }
            }
            ImGui.EndCombo();
        }
        _guides.OpenTutorial(TutorialType.Toybox, StepsToybox.SelectingVibratorType, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => _clientConfigs.GagspeakConfig.VibratorMode = VibratorMode.Simulated);

        // display the wide list of connected devices, along with if they are active or not, below some scanner options
        if (_uiShared.IconTextButton(FontAwesomeIcon.TabletAlt, "Personal Remote", 125f))
        {
            // open the personal remote window
            _mediator.Publish(new UiToggleMessage(typeof(RemotePersonal)));
        }
        ImUtf8.SameLineInner();
        ImGui.Text("Open Personal Remote");

        if (_playerManager.GlobalPerms is not null)
            ImGui.Text("Active Toys State: " + (_playerManager.GlobalPerms.ToyIsActive ? "Active" : "Inactive"));

        ImGui.Text("ConnectedToyActive: " + _vibeService.ConnectedToyActive);

        // draw out the list of devices
        ImGui.Separator();
        _uiShared.BigText("Connected Device(s)");
        if (_clientConfigs.GagspeakConfig.VibratorMode == VibratorMode.Simulated)
        {
            DrawSimulatedVibeInfo();
        }
        else
        {
            DrawDevicesTable();
        }
    }


    private void DrawSimulatedVibeInfo()
    {
        ImGui.SetNextItemWidth(175 * ImGuiHelpers.GlobalScale);
        var vibeType = _clientConfigs.GagspeakConfig.VibeSimAudio;
        if (ImGui.BeginCombo("Vibe Sim Audio##SimVibeAudioType", _clientConfigs.GagspeakConfig.VibeSimAudio.ToString()))
        {
            foreach (VibeSimType mode in Enum.GetValues(typeof(VibeSimType)))
            {
                if (ImGui.Selectable(mode.ToString(), mode == _clientConfigs.GagspeakConfig.VibeSimAudio))
                {
                    _vibeService.UpdateVibeSimAudioType(mode);
                }
            }
            ImGui.EndCombo();
        }
        UiSharedService.AttachToolTip("Select the type of simulated vibrator sound to play when the intensity is adjusted.");
        _guides.OpenTutorial(TutorialType.Toybox, StepsToybox.AudioTypeSelection, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        // draw out the combo for the audio device selection to play to
        ImGui.SetNextItemWidth(175 * ImGuiHelpers.GlobalScale);
        int prevDeviceId = _vibeService.VibeSimAudio.ActivePlaybackDeviceId; // to only execute code to update data once it is changed
        // display the list        
        if (ImGui.BeginCombo("Playback Device##Playback Device", _vibeService.ActiveSimPlaybackDevice))
        {
            foreach (var device in _vibeService.PlaybackDevices)
            {
                bool isSelected = (_vibeService.ActiveSimPlaybackDevice == device);
                if (ImGui.Selectable(device, isSelected))
                {
                    _vibeService.SwitchPlaybackDevice(_vibeService.PlaybackDevices.IndexOf(device));
                }
            }
            ImGui.EndCombo();
        }
        UiSharedService.AttachToolTip("Select the audio device to play the simulated vibrator sound to.");
        _guides.OpenTutorial(TutorialType.Toybox, StepsToybox.PlaybackAudioDevice, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => _clientConfigs.GagspeakConfig.VibratorMode = VibratorMode.Actual);
    }

    public void DrawDevicesTable()
    {
        if (_uiShared.IconTextButton(FontAwesomeIcon.Search, "Device Scanner", null, false, !_vibeService.IntifaceConnected))
        {
            // search scanning if we are not scanning, otherwise stop scanning.
            if (_vibeService.ScanningForDevices)
            {
                _vibeService.DeviceHandler.StopDeviceScanAsync().ConfigureAwait(false);
            }
            else
            {
                _vibeService.DeviceHandler.StartDeviceScanAsync().ConfigureAwait(false);
            }
        }
        _guides.OpenTutorial(TutorialType.Toybox, StepsToybox.DeviceScanner, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        var color = _vibeService.ScanningForDevices ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudRed;
        var scanText = _vibeService.ScanningForDevices ? "Scanning..." : "Idle";
        ImGui.SameLine();
        ImGui.TextUnformatted("Scanner Status: ");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(scanText);
        }

        foreach (var device in _vibeService.DeviceHandler.ConnectedDevices)
        {
            DrawDeviceInfo(device);
        }
    }

    private void DrawDeviceInfo(ConnectedDevice Device)
    {
        if (Device == null) { ImGui.Text("Device is null for this index."); return; }

        ImGui.Text("Device Index: " + Device.DeviceIdx);

        ImGui.Text("Device Name: " + Device.DeviceName);

        ImGui.Text("Device Display Name: " + Device.DisplayName);

        // Draw Vibrate Attributes
        ImGui.Text("Vibrate Attributes:");
        ImGui.Indent();
        foreach (var attr in Device.VibeAttributes)
        {
            ImGui.Text("Feature: " + attr.FeatureDescriptor);
            ImGui.Text("Actuator Type: " + attr.ActuatorType);
            ImGui.Text("Step Count: " + attr.StepCount);
            ImGui.Text("Index: " + attr.Index);
        }
        ImGui.Unindent();

        // Draw Rotate Attributes
        ImGui.Text("Rotate Attributes:");
        ImGui.Indent();
        foreach (var attr in Device.RotateAttributes)
        {
            ImGui.Text("Feature: " + attr.FeatureDescriptor);
            ImGui.Text("Actuator Type: " + attr.ActuatorType);
            ImGui.Text("Step Count: " + attr.StepCount);
            ImGui.Text("Index: " + attr.Index);
        }
        ImGui.Unindent();

        // Check if the device has a battery
        ImGui.Text("Has Battery: " + Device.BatteryPresent);
        ImGui.Text("Battery Level: " + Device.BatteryLevel);
    }


    private void DrawIntifaceConnectionStatus()
    {
        var windowPadding = ImGui.GetStyle().WindowPadding;
        // push the style var to supress the Y window padding.
        var intifaceOpenIcon = FontAwesomeIcon.ArrowUpRightFromSquare;
        var intifaceIconSize = _uiShared.GetIconButtonSize(intifaceOpenIcon);
        var connectedIcon = !_vibeService.IntifaceConnected ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink;
        var buttonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Link);
        var buttplugServerAddr = DeviceService.IntifaceClientName;
        var addrSize = ImGui.CalcTextSize(buttplugServerAddr);

        string intifaceConnectionStr = "Intiface Central Connection";

        var addrTextSize = ImGui.CalcTextSize(intifaceConnectionStr);
        var totalHeight = ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

        // create a table
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Table("IntifaceStatusUI", 3))
        {
            // define the column lengths.
            ImGui.TableSetupColumn("##openIntiface", ImGuiTableColumnFlags.WidthFixed, intifaceIconSize.X);
            ImGui.TableSetupColumn("##serverState", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##connectionButton", ImGuiTableColumnFlags.WidthFixed, buttonSize.X);

            // draw the add user button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - intifaceIconSize.Y) / 2);
            if (_uiShared.IconButton(intifaceOpenIcon))
            {
                ToyboxHelper.OpenIntiface(_logger, true);
            }
            UiSharedService.AttachToolTip("Opens Intiface Central on your PC for connection.\nIf application is not detected, opens a link to installer.");
            _guides.OpenTutorial(TutorialType.Toybox, StepsToybox.OpeningIntiface, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

            // in the next column, draw the centered status.
            ImGui.TableNextColumn();

            if (_vibeService.IntifaceConnected)
            {
                // fancy math shit for clean display, adjust when moving things around
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2 - (addrSize.X) / 2);
                ImGui.TextColored(ImGuiColors.ParsedGreen, buttplugServerAddr);
            }
            else
            {
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2 - (ImGui.CalcTextSize("No Client Connection").X) / 2);
                ImGui.TextColored(ImGuiColors.DalamudRed, "No Client Connection");
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2 - addrTextSize.X / 2);
            ImGui.TextUnformatted(intifaceConnectionStr);

            // draw the connection link button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - intifaceIconSize.Y) / 2);
            // now we need to display the connection link button beside it.
            var color = UiSharedService.GetBoolColor(_vibeService.IntifaceConnected);

            // we need to turn the button from the connected link to the disconnected link.
            using (ImRaii.PushColor(ImGuiCol.Text, color))
            {
                if (_uiShared.IconButton(connectedIcon))
                {
                    // if we are connected to intiface, then we should disconnect.
                    if (_vibeService.IntifaceConnected)
                    {
                        _vibeService.DeviceHandler.DisconnectFromIntifaceAsync();
                    }
                    // otherwise, we should connect to intiface.
                    else
                    {
                        _vibeService.DeviceHandler.ConnectToIntifaceAsync();
                    }
                }
                UiSharedService.AttachToolTip(_vibeService.IntifaceConnected ? "Disconnect from Intiface Central" : "Connect to Intiface Central");
            }
            _guides.OpenTutorial(TutorialType.Toybox, StepsToybox.IntifaceConnection, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
        }
        // draw out the vertical slider.
        ImGui.Separator();
    }
}
