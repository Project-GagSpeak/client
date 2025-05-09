using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.UiRemote;
using GagSpeak.CkCommons;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Controllers;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox;
using GagSpeak.Toybox.Services;
using ImGuiNET;
using OtterGui.Text;
using GagSpeak.CkCommons.Intiface;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.CkCommons.Gui.Components;

namespace GagSpeak.CkCommons.Gui.Toybox;

public class ToysPanel
{
    private readonly ILogger<ToysPanel> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly GlobalData _globals;
    private readonly SexToyManager _manager;
    private readonly GagspeakConfigService _clientConfigs;
    private readonly ServerConfigService _serverConfigs;
    private readonly TutorialService _guides;

    public ToysPanel(
        ILogger<ToysPanel> logger,
        GagspeakMediator mediator,
        GlobalData playerData,
        SexToyManager toysManager,
        GagspeakConfigService clientConfigs,
        ServerConfigService serverConfigs,
        TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _globals = playerData;
        _manager = toysManager;
        _clientConfigs = clientConfigs;
        _serverConfigs = serverConfigs;
        _guides = guides;

        // grab path to the intiface
        if (IntifaceCentral.AppPath == string.Empty)
            IntifaceCentral.GetApplicationPath();
    }

    public void DrawContents(CkHeader.DrawRegions regions, float rightLength, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(regions.Top.Pos);
        using (ImRaii.Child("ToysAndLobbiesTop", regions.Top.Size))
            DrawHeader(regions.Top, rightLength, curveSize, tabMenu);

        ImGui.SetCursorScreenPos(regions.Bottom.Pos);
        using (CkRaii.ChildPadded("ToysAndLobbiesBot", regions.Bottom.Size))
            DrawPanel(regions.Bottom);
    }

    private void DrawHeader(CkHeader.DrawRegion drawRegion, float rightLen, float curveSize, ToyboxTabs tabMenu)
    {
        // Calculate the size of the left box, and the size of the right box, with the spacing in mind.
        var leftBoxSize = new Vector2(drawRegion.Size.X - rightLen - ImGui.GetFrameHeight(), drawRegion.Size.Y);
        var rightBoxSize = new Vector2(rightLen, drawRegion.Size.Y);

        // Create the CkRaii Child for the left side to draw the connection status inside.
        ImGui.SetCursorScreenPos(drawRegion.Pos + new Vector2(curveSize, 0));
        using (CkRaii.ChildPadded("##ToyStatus", leftBoxSize, CkColor.FancyHeaderContrast.Uint(), CkRaii.GetChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
        {
            DrawIntifaceConnectionStatus();
        }

        // Setup position for the right area.
        ImGui.SetCursorScreenPos(drawRegion.Pos + new Vector2(leftBoxSize.X + ImGui.GetFrameHeight(), 0));
        // draw out the tab menu display here.
        tabMenu.Draw(rightBoxSize);

    }

    public void DrawPanel(CkHeader.DrawRegion drawRegion)
    {
        // display a dropdown for the type of vibrator to use
        ImGui.SetNextItemWidth(125f);
        if (ImGui.BeginCombo("Set Vibrator Type##VibratorMode", _clientConfigs.Config.VibratorMode.ToString()))
        {
            foreach (VibratorEnums mode in Enum.GetValues(typeof(VibratorEnums)))
            {
                if (ImGui.Selectable(mode.ToString(), mode == _clientConfigs.Config.VibratorMode))
                {
                    _clientConfigs.Config.VibratorMode = mode;
                    _clientConfigs.Save();
                }
            }
            ImGui.EndCombo();
        }

        // display the wide list of connected devices, along with if they are active or not, below some scanner options
        if (CkGui.IconTextButton(FAI.TabletAlt, "Personal Remote", 125f))
        {
            // open the personal remote window
            _mediator.Publish(new UiToggleMessage(typeof(RemotePersonal)));
        }
        ImUtf8.SameLineInner();
        ImGui.Text("Open Personal Remote");

        if (_globals.GlobalPerms is not null)
            ImGui.Text("Active Toys State: " + (_globals.GlobalPerms.ToysAreConnected ? "Active" : "Inactive"));

        ImGui.Text("ConnectedToyActive: " + _manager.ConnectedToyActive);

        // draw out the list of devices
        ImGui.Separator();
        CkGui.BigText("Connected Device(s)");
        if (_clientConfigs.Config.VibratorMode == VibratorEnums.Simulated)
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
        var vibeType = _clientConfigs.Config.VibeSimAudio;
        if (ImGui.BeginCombo("Vibe Sim Audio##SimVibeAudioType", _clientConfigs.Config.VibeSimAudio.ToString()))
        {
            foreach (VibeSimType mode in Enum.GetValues(typeof(VibeSimType)))
            {
                if (ImGui.Selectable(mode.ToString(), mode == _clientConfigs.Config.VibeSimAudio))
                {
                    _manager.UpdateVibeSimAudioType(mode);
                }
            }
            ImGui.EndCombo();
        }
        CkGui.AttachToolTip("Select the type of simulated vibrator sound to play when the intensity is adjusted.");

        // draw out the combo for the audio device selection to play to
        ImGui.SetNextItemWidth(175 * ImGuiHelpers.GlobalScale);
        var prevDeviceId = _manager.VibeSimAudio.ActivePlaybackDeviceId; // to only execute code to update data once it is changed
        // display the list        
        if (ImGui.BeginCombo("Playback Device##Playback Device", _manager.ActiveSimPlaybackDevice))
        {
            foreach (var device in _manager.PlaybackDevices)
            {
                var isSelected = (_manager.ActiveSimPlaybackDevice == device);
                if (ImGui.Selectable(device, isSelected))
                {
                    _manager.SwitchPlaybackDevice(_manager.PlaybackDevices.IndexOf(device));
                }
            }
            ImGui.EndCombo();
        }
        CkGui.AttachToolTip("Select the audio device to play the simulated vibrator sound to.");
    }

    public void DrawDevicesTable()
    {
        if (CkGui.IconTextButton(FAI.Search, "Device Scanner", null, false, !_manager.IntifaceConnected))
        {
            // search scanning if we are not scanning, otherwise stop scanning.
            if (_manager.ScanningForDevices)
            {
                _manager.DeviceHandler.StopDeviceScanAsync().ConfigureAwait(false);
            }
            else
            {
                _manager.DeviceHandler.StartDeviceScanAsync().ConfigureAwait(false);
            }
        }

        var color = _manager.ScanningForDevices ? ImGuiColors.DalamudYellow : ImGuiColors.DalamudRed;
        var scanText = _manager.ScanningForDevices ? "Scanning..." : "Idle";
        ImGui.SameLine();
        ImGui.TextUnformatted("Scanner Status: ");
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(scanText);
        }

        foreach (var device in _manager.DeviceHandler.ConnectedDevices)
        {
            DrawDeviceInfo(device);
        }
    }

    private void DrawDeviceInfo(ButtPlugDevice Device)
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
        var intifaceOpenIcon = FAI.ArrowUpRightFromSquare;
        var intifaceIconSize = CkGui.IconButtonSize(intifaceOpenIcon);
        var connectedIcon = !_manager.IntifaceConnected ? FAI.Link : FAI.Unlink;
        var buttonSize = CkGui.IconButtonSize(FAI.Link);
        var buttplugServerAddr = IntifaceController.IntifaceClientName;
        var addrSize = ImGui.CalcTextSize(buttplugServerAddr);

        var intifaceConnectionStr = "Intiface Central Connection";

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
            if (CkGui.IconButton(intifaceOpenIcon, inPopup: true))
                IntifaceCentral.OpenIntiface(_logger, true);
            CkGui.AttachToolTip("Opens Intiface Central on your PC for connection.\nIf application is not detected, opens a link to installer.");

            // in the next column, draw the centered status.
            ImGui.TableNextColumn();

            if (_manager.IntifaceConnected)
            {
                // fancy math shit for clean display, adjust when moving things around
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth()) / 2 - (addrSize.X) / 2);
                ImGui.TextColored(ImGuiColors.ParsedGreen, buttplugServerAddr);
            }
            else
            {
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth()) / 2 - (ImGui.CalcTextSize("No Client Connection").X) / 2);
                ImGui.TextColored(ImGuiColors.DalamudRed, "No Client Connection");
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth()) / 2 - addrTextSize.X / 2);
            ImGui.TextUnformatted(intifaceConnectionStr);

            // draw the connection link button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - intifaceIconSize.Y) / 2);
            // now we need to display the connection link button beside it.
            var color = CkGui.GetBoolColor(_manager.IntifaceConnected);

            // we need to turn the button from the connected link to the disconnected link.
            using (ImRaii.PushColor(ImGuiCol.Text, color))
            {
                if (CkGui.IconButton(connectedIcon, inPopup: true))
                {
                    // if we are connected to intiface, then we should disconnect.
                    if (_manager.IntifaceConnected)
                    {
                        _manager.DeviceHandler.DisconnectFromIntifaceAsync();
                    }
                    // otherwise, we should connect to intiface.
                    else
                    {
                        _manager.DeviceHandler.ConnectToIntifaceAsync();
                    }
                }
                CkGui.AttachToolTip(_manager.IntifaceConnected ? "Disconnect from Intiface Central" : "Connect to Intiface Central");
            }
        }
    }
}
