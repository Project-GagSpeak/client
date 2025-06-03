using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Permissions;

public partial class PairStickyUI
{
    private void DrawToyboxActions()
    {
        var lastToyboxData = SPair.LastToyboxData;
        var lastLightStorage = SPair.LastLightStorage;
        if (lastToyboxData is null || lastLightStorage is null)
            return;

        var createText = "Create Vibe Remote with " + PermissionData.DispName;
        var createTT = "Open a Remote UI that let's you control " + PermissionData.DispName + "'s Toys.";
        var executePatternText = "Activate " + PermissionData.DispName + "'s Patterns";
        var executePatternTT = "Play one of " + PermissionData.DispName + "'s patterns to their active Toy.";
        var stopPatternText = "Stop " + PermissionData.DispName + "'s Active Pattern";
        var stopPatternTT = "Halt the active pattern on " + PermissionData.DispName + "'s Toy";
        var toggleAlarmText = "Toggle " + PermissionData.DispName + "'s Alarms";
        var toggleAlarmTT = "Switch the state of " + PermissionData.DispName + "'s Alarms.";
        var toggleTriggerText = "Toggle " + PermissionData.DispName + "'s Triggers";
        var toggleTriggerTT = "Toggle the state of a trigger in " + PermissionData.DispName + "'s triggerList.";

        // Button to open vibe remote for a select pair.
        var openVibeRemoteDisabled = !SPair.PairPerms.RemoteControlAccess;
        if (CkGui.IconTextButton(FAI.Mobile, createText, WindowMenuWidth, true, openVibeRemoteDisabled))
        {
            // open a new private hosted room between the two of you automatically.
            _logger.LogDebug("Vibe Remote instance button pressed for " + PermissionData.DispName);
        }
        CkGui.AttachToolTip(createTT);

        // Expander for executing a pattern on another pair.
        var disablePatternExpand = !SPair.PairPerms.ExecutePatterns || !SPair.PairGlobals.ToysAreConnected || !lastLightStorage.Patterns.Any();
        if (CkGui.IconTextButton(FAI.PlayCircle, executePatternText, WindowMenuWidth, true, disablePatternExpand))
            OpenOrClose(InteractionType.StartPattern);
        CkGui.AttachToolTip(executePatternTT);

        // Pattern Execution
        if (OpenedInteraction is InteractionType.StartPattern)
        {
            using (ImRaii.Child("PatternExecute", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairPatterns.DrawComboIconButton("##ExecutePattern" + PermissionData.DispName, WindowMenuWidth, "Execute a Pattern");
            ImGui.Separator();
        }

        // Stop a Pattern
        var disablePatternStop = !SPair.PairPerms.StopPatterns || !SPair.PairGlobals.ToysAreConnected || lastToyboxData.ActivePattern.IsEmptyGuid();
        if (CkGui.IconTextButton(FAI.StopCircle, stopPatternText, WindowMenuWidth, true, disablePatternStop))
        {
            var idToStop = SPair.LastToyboxData.ActivePattern;
            // Construct the dto, and then send it off.
            var dto = new PushPairToyboxDataUpdateDto(SPair.UserData, SPair.LastToyboxData, DataUpdateType.PatternStopped)
            {
                AffectedIdentifier = idToStop,
            };

            // Avoid blocking the UI by executing this off the UI thread.
            _ = Task.Run(async () =>
            {
                var res = await _hub.UserPushPairDataToybox(dto);
                if (res is not GagSpeakApiEc.Success)
                {
                    _logger.LogError($"Failed to stop pattern on {PermissionData.DispName} 's toy: {res}", LoggerType.Permissions);
                }
                else
                {
                    _logger.LogDebug($"Stopped active Pattern running on {PermissionData.DispName}'s toy: {res}", LoggerType.Permissions);
                    CloseInteraction();
                }
            });
        }
        CkGui.AttachToolTip(stopPatternTT);

        // Expander for toggling an alarm.
        var disableAlarmExpand = !SPair.PairPerms.ToggleAlarms || !lastLightStorage.Alarms.Any();
        if (CkGui.IconTextButton(FAI.Clock, toggleAlarmText, WindowMenuWidth, true, disableAlarmExpand))
            OpenOrClose(InteractionType.ToggleAlarm);
        CkGui.AttachToolTip(toggleAlarmTT);

        if (OpenedInteraction is InteractionType.ToggleAlarm)
        {
            using (ImRaii.Child("AlarmToggle", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairAlarmToggles.DrawComboIconButton("##ToggleAlarm" + PermissionData.DispName, WindowMenuWidth, "Toggle an Alarm");
            ImGui.Separator();
        }

        // Expander for toggling a trigger.
        var disableTriggerExpand = !SPair.PairPerms.ToggleTriggers || !lastLightStorage.Triggers.Any();
        if (CkGui.IconTextButton(FAI.LandMineOn, toggleTriggerText, WindowMenuWidth, true, disableTriggerExpand))
            OpenOrClose(InteractionType.ToggleTrigger);
        CkGui.AttachToolTip(toggleTriggerTT);

        if (OpenedInteraction is InteractionType.ToggleTrigger)
        {
            using (ImRaii.Child("TriggerToggle", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairTriggerToggles.DrawComboIconButton("##ToggleTrigger" + PermissionData.DispName, WindowMenuWidth, "Toggle a Trigger");
        }

        ImGui.Separator();
    }
}
