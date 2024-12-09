/*using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Toybox.Controllers;
using GagSpeak.UI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using ImGuiNET;
using MessagePack;
using OtterGui.Text;
using System.Text.Json.Serialization;

namespace GagSpeak.InterfaceConverters;

public interface IExecutableAction
{
    /// <summary>
    /// The Identifier of the item with an executable action.
    /// </summary>
    public Guid Identifier { get; set; }

    /// <summary>
    /// If the item with the executable action kind is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The Priority of the item with the executable action kind.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The Data for the preferrred executable action kind.
    /// </summary>
    public IActionGS ExecutableAction { get; set; }
}

public interface IActionGS
{
    public ActionExecutionType ExecutionType { get; }
}
public class TextAction : IActionGS
{
    public ActionExecutionType ExecutionType => ActionExecutionType.TextOutput;

    /// <summary>
    /// The text to output.
    /// </summary>
    public string OutputCommand { get; set; } = string.Empty;
}

public class GagAction : IActionGS
{
    public ActionExecutionType ExecutionType => ActionExecutionType.Gag;

    /// <summary>
    /// The Type of Gag.
    /// </summary>
    public GagType GagType { get; set; } = GagType.BallGag;

    /// <summary>
    /// The new state of the gag.
    /// </summary>
    public NewState NewState { get; set; } = NewState.Enabled;

    /// <summary>
    /// Draws out the settings for the Triggers UI.
    /// </summary>
    public void DrawSettingsTrigger(Guid id, UiSharedService uiShared, ILogger logger)
    {
        UiSharedService.ColorText("Apply Gag Type", ImGuiColors.ParsedGold);

        var gagTypes = Enum.GetValues<GagType>().Where(gag => gag != GagType.None).ToArray();
        ImGui.SetNextItemWidth(200f);
        uiShared.DrawComboSearchable("GagActionGagType" + id, 250, gagTypes, (gag) => gag.GagName(), false, (i) =>
        {
            logger.LogTrace($"Selected Gag Type for Trigger: {i}", LoggerType.GagHandling);
            GagType = i;
        }, GagType, "No Gag Type Selected");
        uiShared.DrawHelpText("Apply this Gag to your character when the trigger is fired.");
    }

}

public class RestraintAction : IActionGS
{
    public ActionExecutionType ExecutionType => ActionExecutionType.Restraint;
    /// <summary>
    /// The new state of the restraint
    /// </summary>
    public NewState NewState { get; set; } = NewState.Enabled;
    /// <summary>
    /// The Identifier of the restraint set.
    /// </summary>
    public Guid OutputIdentifier { get; set; } = Guid.Empty;

    public void DrawSettingsTrigger(Guid id, UiSharedService uiShared, ClientConfigurationManager configs)
    {
        UiSharedService.ColorText("Apply Restraint Set", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        List<LightRestraintData> lightRestraintItems = configs.StoredRestraintSets.Select(x => x.ToLightData()).ToList();
        var defaultItem = lightRestraintItems.FirstOrDefault(x => x.Identifier == OutputIdentifier)
                          ?? lightRestraintItems.FirstOrDefault() ?? new LightRestraintData();

        uiShared.DrawCombo("ApplyRestraintSetActionCombo" + id, 200f, lightRestraintItems, (item) => item.Name,
            (i) => OutputIdentifier = i?.Identifier ?? Guid.Empty, defaultItem, defaultPreviewText: "No Set Selected...");
        uiShared.DrawHelpText("Apply restraint set to your character when the trigger is fired.");
    }
}

public class MoodleAction : IActionGS
{
    public ActionExecutionType ExecutionType => ActionExecutionType.Moodle;

    /// <summary>
    /// If a status or preset.
    /// </summary>
    public IpcToggleType MoodleType { get; set; } = IpcToggleType.MoodlesStatus;

    /// <summary>
    /// The Identifier of the Moodle to have applied.
    /// </summary>
    public Guid OutputIdentifier { get; set; } = Guid.Empty;

    public void DrawSettingsTrigger(Guid id, UiSharedService uiShared, ClientData data, MoodlesService moodlesService, ILogger logger)
    {
        if (!IpcCallerMoodles.APIAvailable || data.LastIpcData is null)
        {
            UiSharedService.ColorText("Moodles is not currently active!", ImGuiColors.DalamudRed);
            return;
        }

        UiSharedService.ColorText("Moodle Application Type", ImGuiColors.ParsedGold);
        uiShared.DrawCombo("##CursedItemMoodleType" + id, 90f, Enum.GetValues<IpcToggleType>(), (clicked) => clicked.ToName(),
        (i) =>
        {
            MoodleType = i;
            if (i is IpcToggleType.MoodlesStatus && data.LastIpcData.MoodlesStatuses.Any()) OutputIdentifier = data.LastIpcData.MoodlesStatuses.First().GUID;
            else if (i is IpcToggleType.MoodlesPreset && data.LastIpcData.MoodlesPresets.Any()) OutputIdentifier = data.LastIpcData.MoodlesPresets.First().Item1;
            else OutputIdentifier = Guid.Empty;
        }, MoodleType);

        if (MoodleType is IpcToggleType.MoodlesStatus)
        {
            // Handle Moodle Statuses
            UiSharedService.ColorText("Moodle Status to Apply", ImGuiColors.ParsedGold);
            ImGui.SetNextItemWidth(200f);

            moodlesService.DrawMoodleStatusCombo("##MoodleStatusTriggerAction" + id, ImGui.GetContentRegionAvail().X,
            statusList: data.LastIpcData.MoodlesStatuses, onSelected: (i) =>
            {
                logger.LogTrace($"Selected Moodle Status for Trigger: {i}", LoggerType.IpcMoodles);
                OutputIdentifier = i ?? Guid.Empty;
            }, initialSelectedItem: OutputIdentifier);
            uiShared.DrawHelpText("This Moodle will be applied when the trigger is fired.");
        }
        else
        {
            // Handle Presets
            UiSharedService.ColorText("Moodle Preset to Apply", ImGuiColors.ParsedGold);
            ImGui.SetNextItemWidth(200f);

            moodlesService.DrawMoodlesPresetCombo("##MoodlePresetTriggerAction" + id, ImGui.GetContentRegionAvail().X,
                data.LastIpcData.MoodlesPresets, data.LastIpcData.MoodlesStatuses, (i) => OutputIdentifier = i ?? Guid.Empty);
            uiShared.DrawHelpText("This Moodle Preset will be applied when the trigger is fired.");
        }
    }
}

public class PiShockAction : IActionGS
{
    public ActionExecutionType ExecutionType => ActionExecutionType.ShockCollar;
    /// <summary>
    /// The Shock Instruction to execute.
    /// </summary>
    public ShockTriggerAction ShockInstruction { get; set; } = new ShockTriggerAction();

    public void DrawSettingsTrigger(Guid id, UiSharedService uiShared)
    {
        UiSharedService.ColorText("Shock Collar Action", ImGuiColors.ParsedGold);
        uiShared.DrawHelpText("What kind of action to inflict on the shock collar.");

        uiShared.DrawCombo("ShockCollarActionType" + id, 100f, Enum.GetValues<ShockMode>(), (shockMode) => shockMode.ToString(),
            (i) => ShockInstruction.OpCode = i, ShockInstruction.OpCode, defaultPreviewText: "Select Action...");

        if (ShockInstruction.OpCode is not ShockMode.Beep)
        {
            ImGui.Spacing();
            // draw the intensity slider
            UiSharedService.ColorText(ShockInstruction.OpCode + " Intensity", ImGuiColors.ParsedGold);
            uiShared.DrawHelpText("Adjust the intensity level that will be sent to the shock collar.");

            int intensity = ShockInstruction.Intensity;
            if (ImGui.SliderInt("##ShockCollarIntensity" + id, ref intensity, 0, 100))
            {
                ShockInstruction.Intensity = intensity;
            }
        }

        ImGui.Spacing();
        // draw the duration slider
        UiSharedService.ColorText(ShockInstruction.OpCode + " Duration", ImGuiColors.ParsedGold);
        uiShared.DrawHelpText("Adjust the Duration the action is played for on the shock collar.");

        var duration = ShockInstruction.Duration;
        TimeSpan timeSpanFormat = (duration > 15 && duration < 100)
            ? TimeSpan.Zero // invalid range.
            : (duration >= 100 && duration <= 15000)
                ? TimeSpan.FromMilliseconds(duration) // convert to milliseconds
                : TimeSpan.FromSeconds(duration); // convert to seconds
        float value = (float)timeSpanFormat.TotalSeconds + (float)timeSpanFormat.Milliseconds / 1000;
        if (ImGui.SliderFloat("##ShockCollarDuration" + id, ref value, 0.016f, 15f))
        {
            int newMaxDuration;
            if (value % 1 == 0 && value >= 1 && value <= 15) { newMaxDuration = (int)value; }
            else { newMaxDuration = (int)(value * 1000); }
            ShockInstruction.Duration = newMaxDuration;
        }
    }
}

public class SexToyAction : IActionGS
{
    public ActionExecutionType ExecutionType => ActionExecutionType.SexToy;

    public List<DeviceTriggerAction> TriggerAction { get; set; } = new List<DeviceTriggerAction>();

    public void DrawSettingsTrigger(Guid id, UiSharedService uiShared, DeviceService devices, PatternHandler patterns, ILogger logger)
    {
        try
        {
            float width = ImGui.GetContentRegionAvail().X - uiShared.GetIconButtonSize(FontAwesomeIcon.Plus).X - ImGui.GetStyle().ItemInnerSpacing.X;

            // concatinate the currently stored device names with the list of connected devices so that we dont delete unconnected devices.
            HashSet<string> unionDevices = new HashSet<string>(devices.ConnectedDevices?.Select(device => device.DeviceName) ?? new List<string>())
                .Union(TriggerAction.Select(device => device.DeviceName)).ToHashSet();

            var deviceNames = new HashSet<string>(devices.ConnectedDevices?.Select(device => device.DeviceName) ?? new List<string>());

            UiSharedService.ColorText("Select and Add a Device", ImGuiColors.ParsedGold);

            uiShared.DrawCombo("VibeDeviceTriggerSelector" + id, width, deviceNames, (device) => device,
            (i) => StaticLogger.Logger.LogTrace("Device Selected: " + i, LoggerType.ToyboxDevices), default, false, ImGuiComboFlags.None, "No Devices Connected");
            ImUtf8.SameLineInner();
            if (uiShared.IconButton(FontAwesomeIcon.Plus, null, null, uiShared._selectedComboItems.ContainsKey("VibeDeviceTriggerSelector")))
            {
                var SelectedDeviceName = (string)uiShared._selectedComboItems["VibeDeviceTriggerSelector"];
                if (string.IsNullOrWhiteSpace(SelectedDeviceName))
                {
                    StaticLogger.Logger.LogWarning("No device selected to add to the trigger.");
                    return;
                }
                // attempt to find the device by its name.
                var connectedDevice = devices.GetDeviceByName(SelectedDeviceName);
                if (connectedDevice is not null)
                    TriggerAction.Add(new(connectedDevice.DeviceName, connectedDevice.VibeMotors, connectedDevice.RotateMotors));
            }

            ImGui.Separator();

            if (TriggerAction.Count <= 0)
                return;

            // draw a collapsible header for each of the selected devices.
            for (var i = 0; i < TriggerAction.Count; i++)
            {
                if (ImGui.CollapsingHeader("Settings for Device: " + TriggerAction[i].DeviceName))
                {
                    DrawDeviceActions(i, uiShared, patterns);
                }
            }
        }
        catch (Exception ex)
        {
            StaticLogger.Logger.LogError(ex, "Error drawing VibeActionSettings");
        }
    }

    private void DrawDeviceActions(int deviceIdx, UiSharedService uiShared, PatternHandler patterns)
    {
        if (TriggerAction[deviceIdx].VibrateMotorCount == 0) return;

        bool vibrates = TriggerAction[deviceIdx].Vibrate;
        if (ImGui.Checkbox("##Vibrate Device" + TriggerAction[deviceIdx].DeviceName, ref vibrates))
        {
            TriggerAction[deviceIdx].Vibrate = vibrates;
        }
        ImUtf8.SameLineInner();
        UiSharedService.ColorText("Vibrate Device", ImGuiColors.ParsedGold);
        uiShared.DrawHelpText("Determines if this device will have its vibration motors activated.");

        using (ImRaii.Disabled(!vibrates))
            for (var i = 0; i < TriggerAction[deviceIdx].VibrateMotorCount; i++)
            {
                DrawMotorAction(deviceIdx, i, uiShared, patterns);
            }
    }

    private void DrawMotorAction(int deviceIdx, int motorIndex, UiSharedService uiShared, PatternHandler patterns)
    {
        var motor = TriggerAction[deviceIdx].VibrateActions.FirstOrDefault(x => x.MotorIndex == motorIndex);
        bool enabled = motor != null;

        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Motor " + (motorIndex + 1), ImGuiColors.ParsedGold);
        ImGui.SameLine();

        ImGui.AlignTextToFramePadding();
        if (ImGui.Checkbox("##Motor" + motorIndex + TriggerAction[deviceIdx].DeviceName, ref enabled))
        {
            if (enabled)
            {
                TriggerAction[deviceIdx].VibrateActions.Add(new MotorAction((uint)motorIndex));
            }
            else
            {
                TriggerAction[deviceIdx].VibrateActions.RemoveAll(x => x.MotorIndex == motorIndex);
            }
        }
        UiSharedService.AttachToolTip("Enable/Disable Motor Activation on trigger execution");

        if (motor == null)
        {
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Motor not Enabled");
            return;
        }

        ImUtf8.SameLineInner();
        uiShared.DrawCombo("##ActionType" + TriggerAction[deviceIdx].DeviceName + motorIndex, ImGui.CalcTextSize("Vibration").X + ImGui.GetStyle().FramePadding.X * 2,
            Enum.GetValues<TriggerActionType>(), type => type.ToName(), (i) => motor.ExecuteType = i, motor.ExecuteType, false, ImGuiComboFlags.NoArrowButton);
        UiSharedService.AttachToolTip("What should be played to this motor?");


        ImUtf8.SameLineInner();
        if (motor.ExecuteType == TriggerActionType.Vibration)
        {
            int intensity = motor.Intensity;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.SliderInt("##MotorSlider" + TriggerAction[deviceIdx].DeviceName + motorIndex, ref intensity, 0, 100))
            {
                motor.Intensity = (byte)intensity;
            }
        }
        else
        {
            uiShared.DrawComboSearchable("PatternSelector" + TriggerAction[deviceIdx].DeviceName + motorIndex, ImGui.GetContentRegionAvail().X, patterns.Patterns,
                pattern => pattern.Name, false, (i) =>
                {
                    motor.PatternIdentifier = i?.UniqueIdentifier ?? Guid.Empty;
                    motor.StartPoint = i?.StartPoint ?? TimeSpan.Zero;
                }, default, "No Pattern Selected");
        }
    }
}

public record DeviceTriggerAction
{
    public string DeviceName { get; init; } = "Wildcard Device";
    public bool Vibrate { get; set; } = false;
    public bool Rotate { get; set; } = false;
    public int VibrateMotorCount { get; init; }
    public int RotateMotorCount { get; init; }
    public List<MotorAction> VibrateActions { get; set; } = new List<MotorAction>();
    public List<MotorAction> RotateActions { get; set; } = new List<MotorAction>();
    // Can add linear and oscillation actions here later if anyone actually needs them. But I doubt it.
    public DeviceTriggerAction(string Name, int vibeCount, int MotorCount)
    {
        DeviceName = Name;
        VibrateMotorCount = vibeCount;
        RotateMotorCount = MotorCount;
    }
}

public record MotorAction
{
    public MotorAction(uint motorIndex)
    {
        MotorIndex = motorIndex;
    }

    public uint MotorIndex { get; init; } = 0;

    // the type of action being executed
    public TriggerActionType ExecuteType { get; set; } = TriggerActionType.Vibration;

    // ONLY USED WHEN TYPE IS VIBRATION
    public byte Intensity { get; set; } = 0;

    // ONLY USED WHEN TYPE IS PATTERN
    public Guid PatternIdentifier { get; set; } = Guid.Empty;
    // (if we want to start at a certain point in the pattern.)
    public TimeSpan StartPoint { get; set; } = TimeSpan.Zero;
}



*/
