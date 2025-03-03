using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components;
using GagSpeak.UI.Components.Combos;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.UI.Permissions;

public partial class PairStickyUI
{
    private void DrawToyboxActions()
    {
        var lastToyboxData = SPair.LastToyboxData;
        var lastLightStorage = SPair.LastLightStorage;
        if (lastToyboxData is null || lastLightStorage is null)
            return;

        var createRemoteText = "Create Vibe Remote with " + PermissionData.DispName;
        var createRemoteTT = "Open a Remote UI that let's you control " + PermissionData.DispName + "'s Toys.";

        var executePatternText = "Activate " + PermissionData.DispName + "'s Patterns";
        var executePatternTT = "Play one of " + PermissionData.DispName + "'s patterns to their active Toy.";

        var stopPatternText = "Stop " + PermissionData.DispName + "'s Active Pattern";
        var stopPatternTT = "Halt the active pattern on " + PermissionData.DispName + "'s Toy";

        var toggleAlarmText = "Toggle " + PermissionData.DispName + "'s Alarms";
        var toggleAlarmTT = "Switch the state of " + PermissionData.DispName + "'s Alarms.";

        var toggleTriggerText = "Toggle " + PermissionData.DispName + "'s Triggers";
        var toggleTriggerTT = "Toggle the state of a trigger in " + PermissionData.DispName + "'s triggerList.";


        var openVibeRemoteDisabled = !SPair.PairPerms.RemoteControlAccess;
        var patternExecuteDisabled = !SPair.PairPerms.ExecutePatterns || !SPair.PairGlobals.ToysAreConnected || !lastLightStorage.Patterns.Any();
        var patternStopDisabled = !SPair.PairPerms.StopPatterns || !SPair.PairGlobals.ToysAreConnected || lastToyboxData.ActivePattern.IsEmptyGuid();
        var alarmToggleDisabled = !SPair.PairPerms.ToggleAlarms || !lastLightStorage.Alarms.Any();
        var triggerToggleDisabled = !SPair.PairPerms.ToggleTriggers || !lastLightStorage.Triggers.Any();


        // Button to open vibe remote for a select pair.
        if (CkGui.IconTextButton(FontAwesomeIcon.Mobile, createRemoteText, WindowMenuWidth, true, openVibeRemoteDisabled))
        {
            // open a new private hosted room between the two of you automatically.
            _logger.LogDebug("Vibe Remote instance button pressed for " + PermissionData.DispName);
        }
        CkGui.AttachToolTip(createRemoteTT);


        // Expander for executing a pattern on another pair.
        var disablePatternExpand = !SPair.PairPerms.ExecutePatterns || !SPair.PairGlobals.ToysAreConnected;
        if (CkGui.IconTextButton(FontAwesomeIcon.PlayCircle, executePatternText, WindowMenuWidth, true, patternExecuteDisabled))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.StartPattern) ? InteractionType.None : InteractionType.StartPattern;
        CkGui.AttachToolTip(executePatternTT);

        // Pattern Execution
        if (PairCombos.Opened is InteractionType.StartPattern)
        {
            using (ImRaii.Child("PatternExecute", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.PatternCombo.DrawComboButton("##ExecutePattern" + PermissionData.DispName, "Execute a Pattern", WindowMenuWidth, ImGui.GetTextLineHeightWithSpacing());
            ImGui.Separator();
        }

        // Stop a Pattern
        if (CkGui.IconTextButton(FontAwesomeIcon.StopCircle, stopPatternText, WindowMenuWidth, true, patternStopDisabled))
        {
            var idToStop = SPair.LastToyboxData.ActivePattern;
            // Construct the dto, and then send it off.
            var dto = new PushPairToyboxDataUpdateDto(SPair.UserData, SPair.LastToyboxData, DataUpdateType.PatternStopped)
            {
                AffectedIdentifier = idToStop,
            };
            _hub.UserPushPairDataToybox(dto).ConfigureAwait(false);
            PairCombos.Opened = InteractionType.None;
            _logger.LogDebug("Stopped active Pattern running on " + PermissionData.DispName + "'s toy", LoggerType.Permissions);
        }
        CkGui.AttachToolTip(stopPatternTT);

        // Expander for toggling an alarm.
        var disableAlarmExpand = !SPair.PairPerms.ToggleAlarms || !lastLightStorage.Alarms.Any();
        if (CkGui.IconTextButton(FontAwesomeIcon.Clock, toggleAlarmText, WindowMenuWidth, true, alarmToggleDisabled))
            PairCombos.Opened = PairCombos.Opened == InteractionType.ToggleAlarm ? InteractionType.None : InteractionType.ToggleAlarm;
        CkGui.AttachToolTip(toggleAlarmTT);

        if (PairCombos.Opened is InteractionType.ToggleAlarm)
        {
            using (ImRaii.Child("AlarmToggle", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.AlarmToggleCombo.DrawComboButton("##ToggleAlarm" + PermissionData.DispName, "Toggle an Alarm", WindowMenuWidth, ImGui.GetTextLineHeightWithSpacing());
            ImGui.Separator();
        }

        // Expander for toggling a trigger.
        var disableTriggerExpand = !SPair.PairPerms.ToggleTriggers || !lastLightStorage.Triggers.Any();
        if (CkGui.IconTextButton(FontAwesomeIcon.LandMineOn, toggleTriggerText, WindowMenuWidth, true, triggerToggleDisabled))
            PairCombos.Opened = PairCombos.Opened == InteractionType.ToggleTrigger ? InteractionType.None : InteractionType.ToggleTrigger;
        CkGui.AttachToolTip(toggleTriggerTT);

        if (PairCombos.Opened is InteractionType.ToggleTrigger)
        {
            using (ImRaii.Child("TriggerToggle", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.TriggerToggleCombo.DrawComboButton("##ToggleTrigger" + PermissionData.DispName, "Toggle a Trigger", WindowMenuWidth, ImGui.GetTextLineHeightWithSpacing());
        }

        ImGui.Separator();
    }
}
