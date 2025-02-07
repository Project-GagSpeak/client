using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components.Combos;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using System.Numerics;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// 
/// Yes its messy, yet it's long, but i functionalized it best i could for the insane 
/// amount of logic being performed without adding too much overhead.
/// </summary>
public partial class PairStickyUI
{
    private void DrawToyboxActions()
    {
        var lastToyboxData = StickyPair.LastToyboxData;
        var lastLightStorage = StickyPair.LastLightStorage;
        if (lastToyboxData is null || lastLightStorage is null)
            return;

        var toggleToyText = StickyPair.PairGlobals.ToyIsActive ? "Turn Off " + PairNickOrAliasOrUID + "'s Toys" : "Turn On " + PairNickOrAliasOrUID + "'s Toys";
        var toggleToyTT = "Toggles the state of " + PairNickOrAliasOrUID + "'s connected Toys.";

        var createRemoteText = "Create Vibe Remote with " + PairNickOrAliasOrUID;
        var createRemoteTT = "Open a Remote UI that let's you control " + PairNickOrAliasOrUID + "'s Toys.";

        var executePatternText = "Activate " + PairNickOrAliasOrUID + "'s Patterns";
        var executePatternTT = "Play one of " + PairNickOrAliasOrUID + "'s patterns to their active Toy.";

        var stopPatternText = "Stop " + PairNickOrAliasOrUID + "'s Active Pattern";
        var stopPatternTT = "Halt the active pattern on " + PairUID + "'s Toy";

        var toggleAlarmText = "Toggle " + PairNickOrAliasOrUID + "'s Alarms";
        var toggleAlarmTT = "Switch the state of " + PairUID + "'s Alarms.";

        var toggleTriggerText = "Toggle " + PairNickOrAliasOrUID + "'s Triggers";
        var toggleTriggerTT = "Toggle the state of a trigger in " + PairNickOrAliasOrUID + "'s triggerList.";


        bool openVibeRemoteDisabled = !StickyPair.OnlineToyboxUser || !PairPerms.CanUseVibeRemote;
        bool patternExecuteDisabled = !PairPerms.CanExecutePatterns || !StickyPair.PairGlobals.ToyIsActive || !lastLightStorage.Patterns.Any();
        bool patternStopDisabled = !PairPerms.CanStopPatterns || !StickyPair.PairGlobals.ToyIsActive || lastToyboxData.ActivePatternId.IsEmptyGuid();
        bool alarmToggleDisabled = !PairPerms.CanToggleAlarms || !lastLightStorage.Alarms.Any();
        bool alarmSendDisabled = !PairPerms.CanSendAlarms;
        bool triggerToggleDisabled = !PairPerms.CanToggleTriggers || !lastLightStorage.Triggers.Any();

        ////////// TOGGLE PAIRS ACTIVE TOYS //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.User, toggleToyText, WindowMenuWidth, true))
        {
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new(StickyPair.UserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ToyIsActive", !StickyPair.PairGlobals.ToyIsActive), UpdateDir.Other));
            _logger.LogDebug("Toggled Toybox for " + PairNickOrAliasOrUID + "(New State: " + !StickyPair.PairGlobals.ToyIsActive + ")", LoggerType.Permissions);
        }
        UiSharedService.AttachToolTip(toggleToyTT);

        // Button to open vibe remote for a select pair.
        if (_uiShared.IconTextButton(FontAwesomeIcon.Mobile, createRemoteText, WindowMenuWidth, true, openVibeRemoteDisabled))
        {
            // open a new private hosted room between the two of you automatically.
            _logger.LogDebug("Vibe Remote instance button pressed for " + PairNickOrAliasOrUID);
        }
        UiSharedService.AttachToolTip(createRemoteTT);

        // Expander for executing a pattern on another pair.
        var disablePatternExpand = !PairPerms.CanExecutePatterns || !StickyPair.PairGlobals.ToyIsActive;
        if (_uiShared.IconTextButton(FontAwesomeIcon.PlayCircle, executePatternText, WindowMenuWidth, true, patternExecuteDisabled))
            PairCombos.Opened = (PairCombos.Opened == InteractionType.ActivatePattern) ? InteractionType.None : InteractionType.ActivatePattern;
        UiSharedService.AttachToolTip(executePatternTT);

        // Pattern Execution
        if (PairCombos.Opened is InteractionType.ActivatePattern)
        {
            using (ImRaii.Child("PatternExecute", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.PatternCombo.DrawComboButton("##ExecutePattern" + PairUID, "Execute a Pattern", WindowMenuWidth, 1.3f, ImGui.GetTextLineHeightWithSpacing());
            ImGui.Separator();
        }

        // Stop a Pattern
        if (_uiShared.IconTextButton(FontAwesomeIcon.StopCircle, stopPatternText, WindowMenuWidth, true, patternStopDisabled))
        {
            var newToyboxData = lastToyboxData.DeepClone();
            if (newToyboxData == null) return;

            newToyboxData.InteractionId = lastToyboxData.ActivePatternId;
            newToyboxData.ActivePatternId = Guid.Empty;
            _ = _apiHubMain.UserPushPairDataToyboxUpdate(new(StickyPair.UserData, MainHub.PlayerUserData, newToyboxData, ToyboxUpdateType.PatternStopped, UpdateDir.Other));
            _logger.LogDebug("Stopped active Pattern running on " + PairNickOrAliasOrUID + "'s toy", LoggerType.Permissions);
        }
        UiSharedService.AttachToolTip(stopPatternTT);

        // Expander for toggling an alarm.
        var disableAlarmExpand = !PairPerms.CanToggleAlarms || !lastLightStorage.Alarms.Any();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Clock, toggleAlarmText, WindowMenuWidth, true, alarmToggleDisabled))
            PairCombos.Opened = PairCombos.Opened == InteractionType.ToggleAlarm ? InteractionType.None : InteractionType.ToggleAlarm;
        UiSharedService.AttachToolTip(toggleAlarmTT);

        if (PairCombos.Opened is InteractionType.ToggleAlarm)
        {
            using (ImRaii.Child("AlarmToggle", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.AlarmToggleCombo.DrawComboButton("##ToggleAlarm" + PairUID, "Toggle an Alarm", WindowMenuWidth, 1.15f, ImGui.GetTextLineHeightWithSpacing());
            ImGui.Separator();
        }

        // Expander for toggling a trigger.
        var disableTriggerExpand = !PairPerms.CanToggleTriggers || !lastLightStorage.Triggers.Any();
        if (_uiShared.IconTextButton(FontAwesomeIcon.LandMineOn, toggleTriggerText, WindowMenuWidth, true, triggerToggleDisabled))
            PairCombos.Opened = PairCombos.Opened == InteractionType.ToggleTrigger ? InteractionType.None : InteractionType.ToggleTrigger;
        UiSharedService.AttachToolTip(toggleTriggerTT);

        if (PairCombos.Opened is InteractionType.ToggleTrigger)
        {
            using (ImRaii.Child("TriggerToggle", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairCombos.TriggerToggleCombo.DrawComboButton("##ToggleTrigger" + PairUID, "Toggle a Trigger", WindowMenuWidth, 1.15f, ImGui.GetTextLineHeightWithSpacing());
        }

        ImGui.Separator();
    }
}
