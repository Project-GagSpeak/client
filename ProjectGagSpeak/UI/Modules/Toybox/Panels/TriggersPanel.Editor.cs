using Dalamud.Interface.Colors;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.UI.UiToybox;
public partial class TriggersPanel
{
    private void DrawEditor(Vector2 region)
    {
        if(_manager.ActiveEditorItem is not { } activeTrigger)
            return;

        // If we converted our trigger, return early so the rest of the draw is not errored.
        if (DrawTriggerTypeSelector(activeTrigger))
            return;

        ImGui.Separator();


    }

    public bool DrawTriggerTypeSelector(Trigger trigger)
    {
        var cur = trigger.Type;
        if (ImGuiUtil.GenericEnumCombo("##TriggerKind", ImGui.GetContentRegionAvail().X, cur, out var newType, Enum.GetValues<TriggerKind>(), (t) => t.ToName()))
            if (newType != cur)
            {
                _manager.ChangeTriggerType(trigger, newType);
                return true;
            }

        return false;
    }

    private void DrawInfoSettings(Trigger triggerToCreate)
    {
        // draw out the details for the base of the abstract type.
        var name = triggerToCreate.Label;
        UiSharedService.ColorText("Name", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(225f);
        if (ImGui.InputTextWithHint("##NewTriggerName", "Enter Trigger Name", ref name, 40))
        {
            triggerToCreate.Label = name;
        }

        var desc = triggerToCreate.Description;
        UiSharedService.ColorText("Description", ImGuiColors.ParsedGold);
        if (UiSharedService.InputTextWrapMultiline("##NewTriggerDescription", ref desc, 100, 3, 225f))
        {
            triggerToCreate.Description = desc;
        }
    }

    private void DrawSpellActionTriggerEditor(SpellActionTrigger spellActionTrigger)
    {
/*
        if (!CanDrawSpellActionTriggerUI())
            return;

        UiSharedService.ColorText("Action Type", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("The type of action to monitor for.");

        _ui.DrawCombo("##ActionKindCombo", 150f, Enum.GetValues<LimitedActionEffectType>(), (ActionKind) => ActionKind.ToName(),
        (i) => spellActionTrigger.ActionKind = i, spellActionTrigger.ActionKind);

        // the name of the action to listen to.
        UiSharedService.ColorText("Action Name", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("Action To listen for." + Environment.NewLine + Environment.NewLine
            + "NOTE: Effects Divine Benison or regen, that cast no heal value, do not count as heals.");

        bool anyChecked = spellActionTrigger.ActionID == uint.MaxValue;
        if (ImGui.Checkbox("Any", ref anyChecked))
        {
            spellActionTrigger.ActionID = anyChecked ? uint.MaxValue : 0;
        }
        _ui.DrawHelpText("If checked, will listen for any action from any class for this type.");

        using (var disabled = ImRaii.Disabled(anyChecked))
        {
            _ui.DrawComboSearchable("##ActionJobSelectionCombo", 85f, ClientMonitor.BattleClassJobs,
            (job) => job.Abbreviation.ToString(), false, (i) =>
            {
                _logger.LogTrace($"Selected Job ID for Trigger: {i.RowId}");
                SelectedJobId = i.RowId;
                _clientMonitor.CacheJobActionList(i.RowId);
            }, flags: ImGuiComboFlags.NoArrowButton);

            ImUtf8.SameLineInner();
            var loadedActions = ClientMonitor.LoadedActions[(int)SelectedJobId];
            _ui.DrawComboSearchable("##ActionToListenTo" + SelectedJobId, 150f, loadedActions, (action) => action.Name.ToString(),
            false, (i) => spellActionTrigger.ActionID = (uint)i.RowId, defaultPreviewText: "Select Job Action..");
        }

        // Determine how we draw out the rest of this based on the action type:
        switch (spellActionTrigger.ActionKind)
        {
            case LimitedActionEffectType.Miss:
            case LimitedActionEffectType.Attract1:
            case LimitedActionEffectType.Knockback:
                DrawDirection(spellActionTrigger);
                return;
            case LimitedActionEffectType.BlockedDamage:
            case LimitedActionEffectType.ParriedDamage:
            case LimitedActionEffectType.Damage:
            case LimitedActionEffectType.Heal:
                DrawDirection(spellActionTrigger);
                DrawThresholds(spellActionTrigger);
                return;
        }*/
    }

    private void DrawDirection(SpellActionTrigger spellActionTrigger)
    {
        UiSharedService.ColorText("Direction", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("Determines how the trigger is fired. --SEP--" +
            "From Self ⇒ ActionType was performed BY YOU (Target can be anything)--SEP--" +
            "Self to Others ⇒ ActionType was performed by you, and the target was NOT you--SEP--" +
            "From Others ⇒ ActionType was performed by someone besides you. (Target can be anything)--SEP--" +
            "Others to You ⇒ ActionType was performed by someone else, and YOU were the target.--SEP--" +
            "Any ⇒ Skips over the Direction Filter. Source and Target can be anyone.");

        // create a dropdown storing the enum values of TriggerDirection
        _ui.DrawCombo("##DirectionSelector", 150f, Enum.GetValues<TriggerDirection>(),
        (direction) => direction.ToName(), (i) => spellActionTrigger.Direction = i, spellActionTrigger.Direction);
    }

    private void DrawThresholds(SpellActionTrigger spellActionTrigger)
    {
/*        UiSharedService.ColorText("Threshold Min Value: ", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("Minimum Damage/Heal number to trigger effect.\nLeave -1 for any.");
        var minVal = spellActionTrigger.ThresholdMinValue;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputInt("##ThresholdMinValue", ref minVal))
        {
            spellActionTrigger.ThresholdMinValue = minVal;
        }

        UiSharedService.ColorText("Threshold Max Value: ", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("Maximum Damage/Heal number to trigger effect.");
        var maxVal = spellActionTrigger.ThresholdMaxValue;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputInt("##ThresholdMaxValue", ref maxVal))
        {
            spellActionTrigger.ThresholdMaxValue = maxVal;
        }*/
    }

    private void DrawHealthPercentTriggerEditor(HealthPercentTrigger healthPercentTrigger)
    {
/*        string playerName = healthPercentTrigger.PlayerToMonitor;
        UiSharedService.ColorText("Track Health % of:", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint("##PlayerToTrackHealthOf", "Player Name@World", ref playerName, 72))
        {
            healthPercentTrigger.PlayerToMonitor = playerName;
        }
        _ui.DrawHelpText("Must follow the format Player Name@World." + Environment.NewLine + "Example: Y'shtola Rhul@Mateus");

        UiSharedService.ColorText("Use % Threshold: ", ImGuiColors.ParsedGold);
        var usePercentageHealth = healthPercentTrigger.UsePercentageHealth;
        if (ImGui.Checkbox("##Use Percentage Health", ref usePercentageHealth))
        {
            healthPercentTrigger.UsePercentageHealth = usePercentageHealth;
        }
        _ui.DrawHelpText("When Enabled, will watch for when health goes above or below a specific %" +
            Environment.NewLine + "Otherwise, listens for when it goes above or below a health range.");

        UiSharedService.ColorText("Pass Kind: ", ImGuiColors.ParsedGold);
        _ui.DrawCombo("##PassKindCombo", 150f, Enum.GetValues<ThresholdPassType>(), (passKind) => passKind.ToString(),
            (i) => healthPercentTrigger.PassKind = i, healthPercentTrigger.PassKind);
        _ui.DrawHelpText("If the trigger should fire when the health passes above or below the threshold.");

        if (healthPercentTrigger.UsePercentageHealth)
        {
            UiSharedService.ColorText("Health % Threshold: ", ImGuiColors.ParsedGold);
            int minHealth = healthPercentTrigger.MinHealthValue;
            if (ImGui.SliderInt("##HealthPercentage", ref minHealth, 0, 100, "%d%%"))
            {
                healthPercentTrigger.MinHealthValue = minHealth;
            }
            _ui.DrawHelpText("The Health % that must be crossed to activate the trigger.");
        }
        else
        {
            UiSharedService.ColorText("Min Health Range Threshold: ", ImGuiColors.ParsedGold);
            int minHealth = healthPercentTrigger.MinHealthValue;
            if (ImGui.InputInt("##MinHealthValue", ref minHealth))
            {
                healthPercentTrigger.MinHealthValue = minHealth;
            }
            _ui.DrawHelpText("Lowest HP Value the health should be if triggered upon going below");

            UiSharedService.ColorText("Max Health Range Threshold: ", ImGuiColors.ParsedGold);
            int maxHealth = healthPercentTrigger.MaxHealthValue;
            if (ImGui.InputInt("##MaxHealthValue", ref maxHealth))
            {
                healthPercentTrigger.MaxHealthValue = maxHealth;
            }
            _ui.DrawHelpText("Highest HP Value the health should be if triggered upon going above");
        }*/
    }

    private void DrawRestraintTriggerEditor(RestraintTrigger restraintTrigger)
    {
/*        UiSharedService.ColorText("Restraint Set to Monitor", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("The Restraint Set to listen to for this trigger.");

        ImGui.SetNextItemWidth(200f);
        var setList = _clientConfigs.StoredRestraintSets.Select(x => x.ToLightData()).ToList();
        var defaultSet = setList.FirstOrDefault(x => x.Identifier == restraintTrigger.RestraintSetId)
            ?? setList.FirstOrDefault() ?? new LightRestraintData();

        _ui.DrawCombo("EditRestraintSetCombo" + restraintTrigger.Identifier, 200f, setList, (setItem) => setItem.Label,
            (i) => restraintTrigger.RestraintSetId = i?.Identifier ?? Guid.Empty, defaultSet, false, ImGuiComboFlags.None, "No Set Selected...");

        UiSharedService.ColorText("Restraint State that fires Trigger", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        _ui.DrawCombo("RestraintStateToMonitor" + restraintTrigger.Identifier, 200f, GenericHelpers.RestrictedTriggerStates, (state) => state.ToString(),
            (i) => restraintTrigger.RestraintState = i, restraintTrigger.RestraintState, false, ImGuiComboFlags.None, "No State Selected");*/
    }

    private void DrawGagTriggerEditor(GagTrigger gagTrigger)
    {
/*        UiSharedService.ColorText("Gag to Monitor", ImGuiColors.ParsedGold);
        var gagTypes = Enum.GetValues<GagType>().Where(gag => gag != GagType.None).ToArray();
        ImGui.SetNextItemWidth(200f);
        _ui.DrawComboSearchable("GagTriggerGagType" + gagTrigger.Identifier, 250, gagTypes, (gag) => gag.GagName(), false, (i) => gagTrigger.Gag = i, gagTrigger.Gag);
        _ui.DrawHelpText("The Gag to listen to for this trigger.");

        UiSharedService.ColorText("Gag State that fires Trigger", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        _ui.DrawCombo("GagStateToMonitor" + gagTrigger.Identifier, 200f, GenericHelpers.RestrictedTriggerStates, (state) => state.ToString(),
            (i) => gagTrigger.GagState = i, gagTrigger.GagState, false, ImGuiComboFlags.None, "No Layer Selected");
        _ui.DrawHelpText("Trigger should be fired when the gag state changes to this.");*/
    }

    private void DrawSocialTriggerEditor(SocialTrigger socialTrigger)
    {
/*        UiSharedService.ColorText("Social Action to Monitor", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        _ui.DrawCombo("SocialActionToMonitor", 200f, Enum.GetValues<SocialActionType>(), (action) => action.ToString(),
            (i) => socialTrigger.SocialType = i, socialTrigger.SocialType, false, ImGuiComboFlags.None, "Select a Social Type..");*/
    }

    private void DrawEmoteTriggerEditor(EmoteTrigger emoteTrigger)
    {
/*        UiSharedService.ColorText("Emote to Monitor", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        _ui.DrawCombo("EmoteToMonitor", 200f, EmoteMonitor.ValidEmotes, (e) => e.Value.ComboEmoteName(),
            (i) => emoteTrigger.EmoteID = i.Key, default, false, ImGuiComboFlags.None, "Select an Emote..");

        UiSharedService.ColorText("Currently under construction.\nExpect trigger rework with UI soon?", ImGuiColors.ParsedGold);*/
    }

    private void DrawTriggerActions(Trigger trigger)
    {
        /*UiSharedService.ColorText("Trigger Action Kind", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("The kind of action to perform when the trigger is activated.");

        // Prevent Loopholes
        var allowedKinds = trigger is RestraintTrigger
            ? GenericHelpers.ActionTypesRestraint
            : trigger is GagTrigger
                ? GenericHelpers.ActionTypesOnGag
                : GenericHelpers.ActionTypesTrigger;

        _ui.DrawCombo("##TriggerActionTypeCombo" + trigger.Identifier, 175f, allowedKinds, (newType) => newType.ToName(),
            (i) =>
            {
                switch (i)
                {
                    case InvokableActionType.Gag: trigger.InvokableAction = new GagAction(); break;
                    case InvokableActionType.Restraint: trigger.InvokableAction = new RestraintAction(); break;
                    case InvokableActionType.Moodle: trigger.InvokableAction = new MoodleAction(); break;
                    case InvokableActionType.ShockCollar: trigger.InvokableAction = new PiShockAction(); break;
                    case InvokableActionType.SexToy: trigger.InvokableAction = new SexToyAction(); break;
                    default: throw new NotImplementedException("Action Type not implemented.");
                };
            }, trigger.ActionType.ToName(), false);
        _guides.OpenTutorial(TutorialType.Triggers, StepsTriggers.InvokableActionType, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        ImGui.Separator();
        if (trigger.InvokableAction is GagAction gagAction)
            DrawGagSettings(trigger.Identifier, gagAction);

        else if (trigger.InvokableAction is RestraintAction restraintAction)
            DrawRestraintSettings(trigger.Identifier, restraintAction);

        else if (trigger.InvokableAction is MoodleAction moodleAction)
            DrawMoodlesSettings(trigger.Identifier, moodleAction);

        else if (trigger.InvokableAction is PiShockAction shockAction)
            DrawShockSettings(trigger.Identifier, shockAction);

        else if (trigger.InvokableAction is SexToyAction sexToyAction)
            DrawSexToyActions(trigger.Identifier, sexToyAction);*/
    }

    private void DrawGagSettings(Guid id, GagAction gagAction)
    {
        UiSharedService.ColorText("Apply Gag Type", ImGuiColors.ParsedGold);

        var gagTypes = Enum.GetValues<GagType>().Where(gag => gag != GagType.None).ToArray();
        ImGui.SetNextItemWidth(200f);
        _ui.DrawComboSearchable("GagActionGagType" + id, 250, gagTypes, (gag) => gag.GagName(), false, (i) =>
        {
            _logger.LogTrace($"Selected Gag Type for Trigger: {i}", LoggerType.GagHandling);
            gagAction.GagType = i;
        }, gagAction.GagType, "No Gag Type Selected");
        _ui.DrawHelpText("Apply this Gag to your character when the trigger is fired.");
    }

    public void DrawRestraintSettings(Guid id, RestraintAction restraintAction)
    {
/*        UiSharedService.ColorText("Apply Restraint Set", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        List<LightRestraintData> lightRestraintItems = _clientConfigs.StoredRestraintSets.Select(x => x.ToLightData()).ToList();
        var defaultItem = lightRestraintItems.FirstOrDefault(x => x.Identifier == restraintAction.OutputIdentifier)
                          ?? lightRestraintItems.FirstOrDefault() ?? new LightRestraintData();

        _ui.DrawCombo("ApplyRestraintSetActionCombo" + id, 200f, lightRestraintItems, (item) => item.Label,
            (i) => restraintAction.OutputIdentifier = i?.Identifier ?? Guid.Empty, defaultItem, defaultPreviewText: "No Set Selected...");
        _ui.DrawHelpText("Apply restraint set to your character when the trigger is fired.");*/
    }

    public void DrawMoodlesSettings(Guid id, MoodleAction moodleAction)
    {
        /*if (!IpcCallerMoodles.APIAvailable || _clientData.LastIpcData is null)
        {
            UiSharedService.ColorText("Moodles is not currently active!", ImGuiColors.DalamudRed);
            return;
        }

        UiSharedService.ColorText("Moodle Application Type", ImGuiColors.ParsedGold);
        _ui.DrawCombo("##CursedItemMoodleType" + id, 150f, Enum.GetValues<IpcToggleType>(), (clicked) => clicked.ToName(),
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
            UiSharedService.ColorText("Moodle Status to Apply", ImGuiColors.ParsedGold);
            ImGui.SetNextItemWidth(200f);

            _moodlesService.DrawMoodleStatusCombo("##MoodleStatusTriggerAction" + id, ImGui.GetContentRegionAvail().X,
            statusList: _clientData.LastIpcData.MoodlesStatuses, onSelected: (i) =>
            {
                _logger.LogTrace($"Selected Moodle Status for Trigger: {i}", LoggerType.IpcMoodles);
                moodleAction.Identifier = i ?? Guid.Empty;
            }, initialSelectedItem: moodleAction.Identifier);
            _ui.DrawHelpText("This Moodle will be applied when the trigger is fired.");
        }
        else
        {
            // Handle Presets
            UiSharedService.ColorText("Moodle Preset to Apply", ImGuiColors.ParsedGold);
            ImGui.SetNextItemWidth(200f);

            _moodlesService.DrawMoodlesPresetCombo("##MoodlePresetTriggerAction" + id, ImGui.GetContentRegionAvail().X,
                _clientData.LastIpcData.MoodlesPresets, _clientData.LastIpcData.MoodlesStatuses,
                (i) => moodleAction.Identifier = i ?? Guid.Empty);
            _ui.DrawHelpText("This Moodle Preset will be applied when the trigger is fired.");
        }*/
    }

    public void DrawShockSettings(Guid id, PiShockAction shockAction)
    {
        UiSharedService.ColorText("Shock Collar Action", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("What kind of action to inflict on the shock collar.");

        _ui.DrawCombo("##ShockCollarActionType" + id, 100f, Enum.GetValues<ShockMode>(), (shockMode) => shockMode.ToString(),
            (i) => shockAction.ShockInstruction.OpCode = i, shockAction.ShockInstruction.OpCode, defaultPreviewText: "Select Action...");

        if (shockAction.ShockInstruction.OpCode is not ShockMode.Beep)
        {
            ImGui.Spacing();
            // draw the intensity slider
            UiSharedService.ColorText(shockAction.ShockInstruction.OpCode + " Intensity", ImGuiColors.ParsedGold);
            _ui.DrawHelpText("Adjust the intensity level that will be sent to the shock collar.");

            var intensity = shockAction.ShockInstruction.Intensity;
            if (ImGui.SliderInt("##ShockCollarIntensity" + id, ref intensity, 0, 100))
            {
                shockAction.ShockInstruction.Intensity = intensity;
            }
        }

        ImGui.Spacing();
        // draw the duration slider
        UiSharedService.ColorText(shockAction.ShockInstruction.OpCode + " Duration", ImGuiColors.ParsedGold);
        _ui.DrawHelpText("Adjust the Duration the action is played for on the shock collar.");

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
            UiSharedService.ColorText("Start After (seconds : Milliseconds)", ImGuiColors.ParsedGold);
            _ui.DrawTimeSpanCombo("##Start Delay (seconds)", triggerSliderLimit, ref startAfterRef, UiSharedService.GetWindowContentRegionWidth() / 2, "ss\\:fff", false);
            sexToyAction.StartAfter = startAfterRef;

            var runFor = sexToyAction.EndAfter;
            UiSharedService.ColorText("Run For (seconds : Milliseconds)", ImGuiColors.ParsedGold);
            _ui.DrawTimeSpanCombo("##Execute for (seconds)", triggerSliderLimit, ref runFor, UiSharedService.GetWindowContentRegionWidth() / 2, "ss\\:fff", false);
            sexToyAction.EndAfter = runFor;


            float width = ImGui.GetContentRegionAvail().X - _ui.GetIconButtonSize(FontAwesomeIcon.Plus).X - ImGui.GetStyle().ItemInnerSpacing.X;

            // concatinate the currently stored device names with the list of connected devices so that we dont delete unconnected devices.
            HashSet<string> unionDevices = new HashSet<string>(_deviceController.ConnectedDevices?.Select(device => device.DeviceName) ?? new List<string>())
                .Union(sexToyAction.DeviceActions.Select(device => device.DeviceName)).ToHashSet();

            var deviceNames = new HashSet<string>(_deviceController.ConnectedDevices?.Select(device => device.DeviceName) ?? new List<string>());

            UiSharedService.ColorText("Select and Add a Device", ImGuiColors.ParsedGold);

            _ui.DrawCombo("VibeDeviceTriggerSelector" + id, width, deviceNames, (device) => device, (i) =>
                _logger.LogTrace("Device Selected: " + i, LoggerType.ToyboxDevices), shouldShowLabel: false, defaultPreviewText: "No Devices Connected");
            ImUtf8.SameLineInner();
            // try and get the current device.
            _ui._selectedComboItems.TryGetValue("VibeDeviceTriggerSelector", out var selectedDevice);
            ImGui.Text("Selected Device Name: " + selectedDevice as string);
            if (_ui.IconButton(FontAwesomeIcon.Plus, null, null, string.IsNullOrEmpty(selectedDevice as string)))
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
}
