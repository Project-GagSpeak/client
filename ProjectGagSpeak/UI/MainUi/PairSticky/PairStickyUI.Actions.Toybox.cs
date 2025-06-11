using Dalamud.Interface.Utility.Raii;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
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

        var createText = "Create Vibe Remote with " + DisplayName;
        var createTT = "Open a Remote UI that let's you control " + DisplayName + "'s Toys.";
        var executePatternText = "Activate " + DisplayName + "'s Patterns";
        var executePatternTT = "Play one of " + DisplayName + "'s patterns to their active Toy.";
        var stopPatternText = "Stop " + DisplayName + "'s Active Pattern";
        var stopPatternTT = "Halt the active pattern on " + DisplayName + "'s Toy";
        var toggleAlarmText = "Toggle " + DisplayName + "'s Alarms";
        var toggleAlarmTT = "Switch the state of " + DisplayName + "'s Alarms.";
        var toggleTriggerText = "Toggle " + DisplayName + "'s Triggers";
        var toggleTriggerTT = "Toggle the state of a trigger in " + DisplayName + "'s triggerList.";

        // Button to open vibe remote for a select pair.
        var openVibeRemoteDisabled = !SPair.PairPerms.RemoteControlAccess;
        if (CkGui.IconTextButton(FAI.Mobile, createText, WindowMenuWidth, true, openVibeRemoteDisabled))
        {
            // open a new private hosted room between the two of you automatically.
            _logger.LogDebug("Vibe Remote instance button pressed for " + DisplayName);
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
                _pairPatterns.DrawComboIconButton("##ExecutePattern" + DisplayName, WindowMenuWidth, "Execute a Pattern");
            ImGui.Separator();
        }

        // Stop a Pattern
        var disablePatternStop = !SPair.PairPerms.StopPatterns || !SPair.PairGlobals.ToysAreConnected || lastToyboxData.ActivePattern.IsEmptyGuid();
        if (CkGui.IconTextButton(FAI.StopCircle, stopPatternText, WindowMenuWidth, true, disablePatternStop))
        {
            var idToStop = SPair.LastToyboxData.ActivePattern;
            // Construct the dto, and then send it off.
            PushKinksterToyboxUpdate dto = new(SPair.UserData, SPair.LastToyboxData, idToStop, DataUpdateType.PatternStopped);
            // Avoid blocking the UI by executing this off the UI thread.
            _ = Task.Run(async () =>
            {
                var res = await _hub.UserChangeKinksterToyboxState(dto);
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                {
                    _logger.LogError($"Failed to stop pattern on {DisplayName}'s toy: {res}", LoggerType.StickyUI);
                    return;
                }

                _logger.LogDebug($"Stopped active Pattern running on {DisplayName}'s toy: {res.ErrorCode}", LoggerType.StickyUI);
                CloseInteraction();
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
                _pairAlarmToggles.DrawComboIconButton("##ToggleAlarm" + DisplayName, WindowMenuWidth, "Toggle an Alarm");
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
                _pairTriggerToggles.DrawComboIconButton("##ToggleTrigger" + DisplayName, WindowMenuWidth, "Toggle a Trigger");
        }

        ImGui.Separator();
    }
}
