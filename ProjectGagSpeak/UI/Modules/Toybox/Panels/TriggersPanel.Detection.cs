using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.UiToybox;
public partial class TriggersPanel
{
    // Shared Stuff (I guess?)
    private void DrawPlayerNameWorldField(ref string playerNameWorld, bool isEditingItem)
    {

    }

    // Spell-Action
    private void DrawActionKind(SpellActionTrigger trigger, bool isEditingItem)
    {

    }

    private void DrawSpellDirection(SpellActionTrigger trigger, bool isEditingItem)
    {

    }

    private void DrawActionSelector(SpellActionTrigger trigger, bool isEditingItem)
    {

    }

    private void DrawActionThreshold(SpellActionTrigger trigger, bool isEditingItem)
    {

    }

    // Health-Percent
    // Maybe maxhealth idk




    private void DrawSpellActionTriggerEditor(SpellActionTrigger spellActionTrigger)
    {
/*
        if (!CanDrawSpellActionTriggerUI())
            return;

        CkGui.ColorText("Action Type", ImGuiColors.ParsedGold);
        CkGui.HelpText("The type of action to monitor for.");

        CkGui.DrawCombo("##ActionKindCombo", 150f, Enum.GetValues<LimitedActionEffectType>(), (ActionKind) => ActionKind.ToName(),
        (i) => spellActionTrigger.ActionKind = i, spellActionTrigger.ActionKind);

        // the name of the action to listen to.
        CkGui.ColorText("Action Name", ImGuiColors.ParsedGold);
        CkGui.HelpText("Action To listen for." + Environment.NewLine + Environment.NewLine
            + "NOTE: Effects Divine Benison or regen, that cast no heal value, do not count as heals.");

        bool anyChecked = spellActionTrigger.ActionID == uint.MaxValue;
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
        CkGui.ColorText("Direction", ImGuiColors.ParsedGold);
        CkGui.HelpText("Determines how the trigger is fired. --SEP--" +
            "From Self ⇒ ActionType was performed BY YOU (Target can be anything)--SEP--" +
            "Self to Others ⇒ ActionType was performed by you, and the target was NOT you--SEP--" +
            "From Others ⇒ ActionType was performed by someone besides you. (Target can be anything)--SEP--" +
            "Others to You ⇒ ActionType was performed by someone else, and YOU were the target.--SEP--" +
            "Any ⇒ Skips over the Direction Filter. Source and Target can be anyone.");

        // create a dropdown storing the enum values of TriggerDirection
        if (ImGuiUtil.GenericEnumCombo("##Direction", 150f, spellActionTrigger.Direction, out var newDir, dir => dir.ToName()))
            spellActionTrigger.Direction = newDir;
    }

    private void DrawThresholds(SpellActionTrigger spellActionTrigger)
    {
/*        CkGui.ColorText("Threshold Min Value: ", ImGuiColors.ParsedGold);
        CkGui.HelpText("Minimum Damage/Heal number to trigger effect.\nLeave -1 for any.");
        var minVal = spellActionTrigger.ThresholdMinValue;
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputInt("##ThresholdMinValue", ref minVal))
        {
            spellActionTrigger.ThresholdMinValue = minVal;
        }

        CkGui.ColorText("Threshold Max Value: ", ImGuiColors.ParsedGold);
        CkGui.HelpText("Maximum Damage/Heal number to trigger effect.");
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
        CkGui.ColorText("Track Health % of:", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint("##PlayerToTrackHealthOf", "Player Name@World", ref playerName, 72))
        {
            healthPercentTrigger.PlayerToMonitor = playerName;
        }
        CkGui.HelpText("Must follow the format Player Name@World." + Environment.NewLine + "Example: Y'shtola Rhul@Mateus");

        CkGui.ColorText("Use % Threshold: ", ImGuiColors.ParsedGold);
        var usePercentageHealth = healthPercentTrigger.UsePercentageHealth;
        if (ImGui.Checkbox("##Use Percentage Health", ref usePercentageHealth))
        {
            healthPercentTrigger.UsePercentageHealth = usePercentageHealth;
        }
        CkGui.HelpText("When Enabled, will watch for when health goes above or below a specific %" +
            Environment.NewLine + "Otherwise, listens for when it goes above or below a health range.");

        CkGui.ColorText("Pass Kind: ", ImGuiColors.ParsedGold);
        CkGui.DrawCombo("##PassKindCombo", 150f, Enum.GetValues<ThresholdPassType>(), (passKind) => passKind.ToString(),
            (i) => healthPercentTrigger.PassKind = i, healthPercentTrigger.PassKind);
        CkGui.HelpText("If the trigger should fire when the health passes above or below the threshold.");

        if (healthPercentTrigger.UsePercentageHealth)
        {
            CkGui.ColorText("Health % Threshold: ", ImGuiColors.ParsedGold);
            int minHealth = healthPercentTrigger.MinHealthValue;
            if (ImGui.SliderInt("##HealthPercentage", ref minHealth, 0, 100, "%d%%"))
            {
                healthPercentTrigger.MinHealthValue = minHealth;
            }
            CkGui.HelpText("The Health % that must be crossed to activate the trigger.");
        }
        else
        {
            CkGui.ColorText("Min Health Range Threshold: ", ImGuiColors.ParsedGold);
            int minHealth = healthPercentTrigger.MinHealthValue;
            if (ImGui.InputInt("##MinHealthValue", ref minHealth))
            {
                healthPercentTrigger.MinHealthValue = minHealth;
            }
            CkGui.HelpText("Lowest HP Value the health should be if triggered upon going below");

            CkGui.ColorText("Max Health Range Threshold: ", ImGuiColors.ParsedGold);
            int maxHealth = healthPercentTrigger.MaxHealthValue;
            if (ImGui.InputInt("##MaxHealthValue", ref maxHealth))
            {
                healthPercentTrigger.MaxHealthValue = maxHealth;
            }
            CkGui.HelpText("Highest HP Value the health should be if triggered upon going above");
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

    private void DrawTriggerActions(Trigger trigger)
    {
        /*CkGui.ColorText("Trigger Action Kind", ImGuiColors.ParsedGold);
        CkGui.HelpText("The kind of action to perform when the trigger is activated.");

        // Prevent Loopholes
        var allowedKinds = trigger is RestraintTrigger
            ? GenericHelpers.ActionTypesRestraint
            : trigger is GagTrigger
                ? GenericHelpers.ActionTypesOnGag
                : GenericHelpers.ActionTypesTrigger;

        CkGui.DrawCombo("##TriggerActionTypeCombo" + trigger.Identifier, 175f, allowedKinds, (newType) => newType.ToName(),
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
