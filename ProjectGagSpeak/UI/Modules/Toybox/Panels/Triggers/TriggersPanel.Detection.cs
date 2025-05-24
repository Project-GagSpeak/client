using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.UiToybox;
// WORRY ABOUT FUNCTIONALIZATION LATER.
public partial class TriggersPanel
{
    private bool CheckboxOrPreview(string id, bool curr, out bool newVal, bool isEditing)
    {
        using var dis = ImRaii.Disabled(!isEditing);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f, !isEditing);

        var refVal = curr;
        if (ImGui.Checkbox(id, ref refVal))
        {
            newVal = refVal;
            return true;
        }

        newVal = curr;
        return false;
    }

    private bool InputWithHintOrPreview(string id, float width, ref string dispText, bool isEditing, string hint,
        string errorStr = "", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        var changed = InputWithHintOrPreview(id, width, dispText, out var newVal, isEditing, hint, errorStr, flags);
        if (changed)
            dispText = newVal;
        return changed;
    }

    private bool InputWithHintOrPreview(string id, float width, string curStr, out string newVal, bool isEditing,
        string hint, string errorStr = "", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        var col = isEditing ? 0 : CkColor.FancyHeaderContrast.Uint();
        using (var c = CkRaii.Child(id, new Vector2(width, ImGui.GetFrameHeight()), col, CkRaii.GetChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            if (isEditing)
            {
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.InputTextWithHint($"{id}##InputHint", hint, ref curStr, 68, flags))
                {
                    newVal = curStr;
                    return true;
                }
            }
            else
            {
                CkGui.CenterTextAligned(curStr.IsNullOrWhitespace() ? errorStr : curStr);
            }
        }

        newVal = curStr;
        return false;
    }

    private bool EnumComboOrPreview<T>(string id, float width, ref T current, bool isEditing, Func<T, string>? toStr = null,
        string none = "Select Item..", int skip = 0, CFlags flags = CFlags.None) where T : struct, Enum
        => EnumComboOrPreview(id, width, ref current, Enum.GetValues<T>().Skip(skip), isEditing, toStr, none, flags);

    private bool EnumComboOrPreview<T>(string id, float width, ref T ItemValue, IEnumerable<T> items, bool isEditing,
        Func<T, string>? toStr = null, string none = "Select Item..", CFlags flags = CFlags.None) where T : struct, Enum
    {
        var changed = EnumComboOrPreview(id, width, ItemValue, out T newVal, items, isEditing, toStr, none, flags);
        if (changed)
            ItemValue = newVal;

        return changed;
    }

    private bool EnumComboOrPreview<T>(string id, float width, T cur, out T newVal, bool isEditing, Func<T, string>? toStr = null,
        string none = "Select Item..", int skip = 0, CFlags flags = CFlags.None) where T : struct, Enum
        => EnumComboOrPreview(id, width, cur, out newVal, Enum.GetValues<T>().Skip(skip), isEditing, toStr, none, flags);

    private bool EnumComboOrPreview<T>(string id, float width, T cur, out T newVal, IEnumerable<T> items, bool isEditing,
        Func<T, string>? toStr = null, string none = "Select Item..", CFlags flags = CFlags.None) where T : struct, Enum
    {
        var col = isEditing ? 0 : CkColor.FancyHeaderContrast.Uint();
        using (CkRaii.Child(id, new Vector2(width, ImGui.GetFrameHeight()), col, CkRaii.GetChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            if (isEditing)
            {
                if (CkGuiUtils.EnumCombo("Combo" + id, width, cur, out newVal, toStr, flags: flags))
                    return true;
            }
            else
            {
                var dispName = toStr?.Invoke(cur) ?? cur.ToString();
                CkGui.CenterTextAligned(dispName);
            }
        }

        newVal = cur;
        return false;
    }

    private void DrawThresholdPercent(float width, IThresholdContainer trigger, bool isEditing, string? tt = null, string format = "%d%%")
    {
        var col = isEditing ? 0 : CkColor.FancyHeaderContrast.Uint();
        using (var c = CkRaii.Child("Perc_Thres", new Vector2(width, ImGui.GetFrameHeight()), col, CkRaii.GetChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var healthPercentRef = trigger.ThresholdMinValue;
            if (isEditing)
            {
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.DragInt("##HealthPercentage", ref healthPercentRef, 0.1f, 0, 100, format))
                    trigger.ThresholdMinValue = healthPercentRef;
            }
            else
            {
                CkGui.CenterTextAligned($"{healthPercentRef}%");
            }
        }
        CkGui.AttachToolTip(tt ?? "Maximum Percent Damage/Heal number to trigger effect.");
    }

    private void DrawThresholds(float width, IThresholdContainer trigger, bool isEditing, string? lowerTT = null, 
        string? upperTT = null, string lowerFormat = "%d", string upperFormat = "%d")
    {
        var col = isEditing ? 0 : CkColor.FancyHeaderContrast.Uint();
        var length = (width - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
        using (var c = CkRaii.Child("MinThreshold", new Vector2(length, ImGui.GetFrameHeight()), col, CkRaii.GetChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var minThresRef = trigger.ThresholdMinValue;
            if (isEditing)
            {
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.DragInt("##MinThresSlider", ref minThresRef, 10.0f, -1, 1000000, lowerFormat))
                    trigger.ThresholdMinValue = minThresRef;
            }
            else
            {
                string displayStr = lowerFormat.Replace("%d", minThresRef.ToString());
                CkGui.CenterTextAligned(displayStr);
            }
        }
        CkGui.AttachToolTip(lowerTT ?? "Minimum Damage/Heal number to trigger effect.\nLeave -1 for any.");

        ImUtf8.SameLineInner();
        using (var c = CkRaii.Child("MaxThreshold", new Vector2(length, ImGui.GetFrameHeight()), col, CkRaii.GetChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var maxThresRef = trigger.ThresholdMaxValue;
            if (isEditing)
            {
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.DragInt("##MaxThresSlider", ref maxThresRef, 10.0f, -1, 1000000, upperFormat))
                    trigger.ThresholdMaxValue = maxThresRef;
            }
            else
            {
                string displayStr = upperFormat.Replace("%d", maxThresRef.ToString());
                CkGui.CenterTextAligned(displayStr);
            }
        }
        CkGui.AttachToolTip(upperTT ?? "Maximum Damage/Heal number to trigger effect.");
    }

    // Health-Percent
    // Maybe maxhealth idk

    private void DrawSpellActionTriggerEditor(SpellActionTrigger spellActionTrigger)
    {
/*        bool anyChecked = spellActionTrigger.ActionIDs == uint.MaxValue;
        if (ImGui.Checkbox("Any", ref anyChecked))
        {
            spellActionTrigger.ActionID = anyChecked ? uint.MaxValue : 0;
        } 
        CkGui.HelpText("If checked, will listen for any action from any class for this type.");

        using (var disabled = ImRaii.Disabled(anyChecked))
        {
            CkGui.DrawComboSearchable("##ActionJobSelectionCombo", 85f, ClientMonitor.BattleClassJobs,
            (job) => job.Abbreviation.ToString(), false, (i) =>
            {
                _logger.LogTrace($"Selected Job ID for Trigger: {i.RowId}");
                SelectedJobId = i.RowId;
                _clientMonitor.CacheJobActionList(i.RowId);
            }, flags: ImGuiComboFlags.NoArrowButton);

            ImUtf8.SameLineInner();
            var loadedActions = ClientMonitor.LoadedActions[(int)SelectedJobId];
            CkGui.DrawComboSearchable("##ActionToListenTo" + SelectedJobId, 150f, loadedActions, (action) => action.Name.ToString(),
            false, (i) => spellActionTrigger.ActionID = (uint)i.RowId, defaultPreviewText: "Select Job Action..");
        }*/
    }

    private void DrawRestraintTriggerEditor(RestraintTrigger restraintTrigger)
    {
/*        CkGui.ColorText("Restraint Set to Monitor", ImGuiColors.ParsedGold);
        CkGui.HelpText("The Restraint Set to listen to for this trigger.");

        ImGui.SetNextItemWidth(200f);
        var setList = _clientConfigs.StoredRestraintSets.Select(x => x.ToLightData()).ToList();
        var defaultSet = setList.FirstOrDefault(x => x.Identifier == restraintTrigger.RestraintSetId)
            ?? setList.FirstOrDefault() ?? new LightRestraintData();

        CkGui.DrawCombo("EditRestraintSetCombo" + restraintTrigger.Identifier, 200f, setList, (setItem) => setItem.Label,
            (i) => restraintTrigger.RestraintSetId = i?.Identifier ?? Guid.Empty, defaultSet, false, ImGuiComboFlags.None, "No Set Selected...");

        CkGui.ColorText("Restraint State that fires Trigger", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        CkGui.DrawCombo("RestraintStateToMonitor" + restraintTrigger.Identifier, 200f, GenericHelpers.RestrictedTriggerStates, (state) => state.ToString(),
            (i) => restraintTrigger.RestraintState = i, restraintTrigger.RestraintState, false, ImGuiComboFlags.None, "No State Selected");*/
    }

    private void DrawGagTriggerEditor(GagTrigger gagTrigger)
    {
/*        CkGui.ColorText("Gag to Monitor", ImGuiColors.ParsedGold);
        var gagTypes = Enum.GetValues<GagType>().Where(gag => gag != GagType.None).ToArray();
        ImGui.SetNextItemWidth(200f);
        CkGui.DrawComboSearchable("GagTriggerGagType" + gagTrigger.Identifier, 250, gagTypes, (gag) => gag.GagName(), false, (i) => gagTrigger.Gag = i, gagTrigger.Gag);
        CkGui.HelpText("The Gag to listen to for this trigger.");

        CkGui.ColorText("Gag State that fires Trigger", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        CkGui.DrawCombo("GagStateToMonitor" + gagTrigger.Identifier, 200f, GenericHelpers.RestrictedTriggerStates, (state) => state.ToString(),
            (i) => gagTrigger.GagState = i, gagTrigger.GagState, false, ImGuiComboFlags.None, "No Layer Selected");
        CkGui.HelpText("Trigger should be fired when the gag state changes to this.");*/
    }

    private void DrawSocialTriggerEditor(SocialTrigger socialTrigger)
    {
/*        CkGui.ColorText("Social Action to Monitor", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        CkGui.DrawCombo("SocialActionToMonitor", 200f, Enum.GetValues<SocialActionType>(), (action) => action.ToString(),
            (i) => socialTrigger.SocialType = i, socialTrigger.SocialType, false, ImGuiComboFlags.None, "Select a Social Type..");*/
    }

    private void DrawEmoteTriggerEditor(EmoteTrigger emoteTrigger)
    {
/*        CkGui.ColorText("Emote to Monitor", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        CkGui.DrawCombo("EmoteToMonitor", 200f, EmoteMonitor.ValidEmotes, (e) => e.Value.ComboEmoteName(),
            (i) => emoteTrigger.EmoteID = i.Key, default, false, ImGuiComboFlags.None, "Select an Emote..");

        CkGui.ColorText("Currently under construction.\nExpect trigger rework with UI soon?", ImGuiColors.ParsedGold);*/
    }

    private void DrawGagSettings(Guid id, GagAction gagAction)
    {
        CkGui.ColorText("Apply Gag Type", ImGuiColors.ParsedGold);

        var gagTypes = Enum.GetValues<GagType>().Where(gag => gag != GagType.None).ToArray();
        ImGui.SetNextItemWidth(200f);
        CkGui.DrawComboSearchable("GagActionGagType" + id, 250, gagTypes, (gag) => gag.GagName(), false, (i) =>
        {
            _logger.LogTrace($"Selected Gag Type for Trigger: {i}", LoggerType.GagHandling);
            gagAction.GagType = i;
        }, gagAction.GagType, "No Gag Type Selected");
        CkGui.HelpText("Apply this Gag to your character when the trigger is fired.");
    }

    public void DrawRestraintSettings(Guid id, RestraintAction restraintAction)
    {
/*        CkGui.ColorText("Apply Restraint Set", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        List<LightRestraintData> lightRestraintItems = _clientConfigs.StoredRestraintSets.Select(x => x.ToLightData()).ToList();
        var defaultItem = lightRestraintItems.FirstOrDefault(x => x.Identifier == restraintAction.OutputIdentifier)
                          ?? lightRestraintItems.FirstOrDefault() ?? new LightRestraintData();

        CkGui.DrawCombo("ApplyRestraintSetActionCombo" + id, 200f, lightRestraintItems, (item) => item.Label,
            (i) => restraintAction.OutputIdentifier = i?.Identifier ?? Guid.Empty, defaultItem, defaultPreviewText: "No Set Selected...");
        CkGui.HelpText("Apply restraint set to your character when the trigger is fired.");*/
    }

    public void DrawMoodlesSettings(Guid id, MoodleAction moodleAction)
    {
        /*if (!IpcCallerMoodles.APIAvailable || _clientData.LastIpcData is null)
        {
            CkGui.ColorText("Moodles is not currently active!", ImGuiColors.DalamudRed);
            return;
        }

        CkGui.ColorText("Moodle Application Type", ImGuiColors.ParsedGold);
        CkGui.DrawCombo("##CursedItemMoodleType" + id, 150f, Enum.GetValues<IpcToggleType>(), (clicked) => clicked.ToName(),
        (i) =>
        {
            moodleAction.MoodleType = i;
            if (i is IpcToggleType.MoodlesStatus && _clientData.LastIpcData.MoodlesStatuses.Any())
                moodleAction.Identifier = _clientData.LastIpcData.MoodlesStatuses.First().GUID;
            else if (i is IpcToggleType.MoodlesPreset && _clientData.LastIpcData.MoodlesPresets.Any())
                moodleAction.Identifier = _clientData.LastIpcData.MoodlesPresets.First().Item1;
            else moodleAction.Identifier = Guid.Empty;
        }, moodleAction.MoodleType);

        if (moodleAction.MoodleType is IpcToggleType.MoodlesStatus)
        {
            // Handle Moodle Statuses
            CkGui.ColorText("Moodle Status to Apply", ImGuiColors.ParsedGold);
            ImGui.SetNextItemWidth(200f);

            _moodlesService.DrawMoodleStatusCombo("##MoodleStatusTriggerAction" + id, ImGui.GetContentRegionAvail().X,
            statusList: _clientData.LastIpcData.MoodlesStatuses, onSelected: (i) =>
            {
                _logger.LogTrace($"Selected Moodle Status for Trigger: {i}", LoggerType.IpcMoodles);
                moodleAction.Identifier = i ?? Guid.Empty;
            }, initialSelectedItem: moodleAction.Identifier);
            CkGui.HelpText("This Moodle will be applied when the trigger is fired.");
        }
        else
        {
            // Handle Presets
            CkGui.ColorText("Moodle Preset to Apply", ImGuiColors.ParsedGold);
            ImGui.SetNextItemWidth(200f);

            _moodlesService.DrawMoodlesPresetCombo("##MoodlePresetTriggerAction" + id, ImGui.GetContentRegionAvail().X,
                _clientData.LastIpcData.MoodlesPresets, _clientData.LastIpcData.MoodlesStatuses,
                (i) => moodleAction.Identifier = i ?? Guid.Empty);
            CkGui.HelpText("This Moodle Preset will be applied when the trigger is fired.");
        }*/
    }

    public void DrawShockSettings(Guid id, PiShockAction shockAction)
    {
        CkGui.ColorText("Shock Collar Action", ImGuiColors.ParsedGold);
        CkGui.HelpText("What kind of action to inflict on the shock collar.");

        if(CkGuiUtils.EnumCombo("##OpCode" + id, 100f, shockAction.ShockInstruction.OpCode, out var newType, defaultText: "Select Action...", skip: 1))
            shockAction.ShockInstruction.OpCode = newType;

        if (shockAction.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            ImGui.Spacing();
            // draw the intensity slider
            CkGui.ColorText(shockAction.ShockInstruction.OpCode + " Intensity", ImGuiColors.ParsedGold);
            CkGui.HelpText("Adjust the intensity level that will be sent to the shock collar.");

            var intensity = shockAction.ShockInstruction.Intensity;
            if (ImGui.SliderInt("##ShockCollarIntensity" + id, ref intensity, 0, 100))
            {
                shockAction.ShockInstruction.Intensity = intensity;
            }
        }

        ImGui.Spacing();
        // draw the duration slider
        CkGui.ColorText(shockAction.ShockInstruction.OpCode + " Duration", ImGuiColors.ParsedGold);
        CkGui.HelpText("Adjust the Duration the action is played for on the shock collar.");

        var duration = shockAction.ShockInstruction.Duration;
        var timeSpanFormat = (duration > 15 && duration < 100)
            ? TimeSpan.Zero // invalid range.
            : (duration >= 100 && duration <= 15000)
                ? TimeSpan.FromMilliseconds(duration) // convert to milliseconds
                : TimeSpan.FromSeconds(duration); // convert to seconds
        var value = (float)timeSpanFormat.TotalSeconds + (float)timeSpanFormat.Milliseconds / 1000;
        if (ImGui.SliderFloat("##ShockCollarDuration" + id, ref value, 0.016f, 15f))
        {
            int newMaxDuration;
            if (value % 1 == 0 && value >= 1 && value <= 15) { newMaxDuration = (int)value; }
            else { newMaxDuration = (int)(value * 1000); }
            shockAction.ShockInstruction.Duration = newMaxDuration;
        }
    }

    public void DrawSexToyActions(Guid id, SexToyAction sexToyAction)
    {
        /*try
        {
            var startAfterRef = sexToyAction.StartAfter;
            CkGui.ColorText("Start After (seconds : Milliseconds)", ImGuiColors.ParsedGold);
            CkGui.DrawTimeSpanCombo("##Start Delay (seconds)", triggerSliderLimit, ref startAfterRef, CkGui.GetWindowContentRegionWidth() / 2, "ss\\:fff", false);
            sexToyAction.StartAfter = startAfterRef;

            var runFor = sexToyAction.EndAfter;
            CkGui.ColorText("Run For (seconds : Milliseconds)", ImGuiColors.ParsedGold);
            CkGui.DrawTimeSpanCombo("##Execute for (seconds)", triggerSliderLimit, ref runFor, CkGui.GetWindowContentRegionWidth() / 2, "ss\\:fff", false);
            sexToyAction.EndAfter = runFor;


            float width = ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Plus).X - ImGui.GetStyle().ItemInnerSpacing.X;

            // concatinate the currently stored device names with the list of connected devices so that we dont delete unconnected devices.
            HashSet<string> unionDevices = new HashSet<string>(_deviceController.ConnectedDevices?.Select(device => device.DeviceName) ?? new List<string>())
                .Union(sexToyAction.DeviceActions.Select(device => device.DeviceName)).ToHashSet();

            var deviceNames = new HashSet<string>(_deviceController.ConnectedDevices?.Select(device => device.DeviceName) ?? new List<string>());

            CkGui.ColorText("Select and Add a Device", ImGuiColors.ParsedGold);

            CkGui.DrawCombo("VibeDeviceTriggerSelector" + id, width, deviceNames, (device) => device, (i) =>
                _logger.LogTrace("Device Selected: " + i, LoggerType.ToyboxDevices), shouldShowLabel: false, defaultPreviewText: "No Devices Connected");
            ImUtf8.SameLineInner();
            // try and get the current device.
            CkGui._selectedComboItems.TryGetValue("VibeDeviceTriggerSelector", out var selectedDevice);
            ImGui.Text("Selected Device Name: " + selectedDevice as string);
            if (CkGui.IconButton(FAI.Plus, null, null, string.IsNullOrEmpty(selectedDevice as string)))
            {
                if (string.IsNullOrWhiteSpace(selectedDevice as string))
                {
                    GagSpeak.StaticLog.Warning("No device selected to add to the trigger.");
                    return;
                }
                // attempt to find the device by its name.
                var connectedDevice = _deviceController.GetDeviceByName(SelectedDeviceName);
                if (connectedDevice is not null)
                    sexToyAction.DeviceActions.Add(new(connectedDevice.DeviceName, connectedDevice.VibeMotors, connectedDevice.RotateMotors));
            }

            ImGui.Separator();

            if (sexToyAction.DeviceActions.Count <= 0)
                return;

            // draw a collapsible header for each of the selected devices.
            for (var i = 0; i < sexToyAction.DeviceActions.Count; i++)
            {
                if (ImGui.CollapsingHeader("Settings for Device: " + sexToyAction.DeviceActions[i].DeviceName))
                {
                    DrawDeviceActions(sexToyAction.DeviceActions[i]);
                }
            }
        }
        catch (Exception ex)
        {
            GagSpeak.StaticLog.Error(ex, "Error drawing VibeActionSettings");
        }*/
    }

    private void DrawFooter(Trigger trigger)
    {
        // get the remaining region.
        var regionLeftover = ImGui.GetContentRegionAvail().Y;

        // Determine how to space the footer.
        if (regionLeftover < (CkGui.GetSeparatorHeight() + ImGui.GetFrameHeight()))
            CkGui.Separator();
        else
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + regionLeftover - ImGui.GetFrameHeight());

        // Draw it.
        ImUtf8.TextFrameAligned("ID:");
        ImGui.SameLine();
        ImUtf8.TextFrameAligned(trigger.Identifier.ToString());
    }
}
