using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
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

        bool openVibeRemoteDisabled = !StickyPair.OnlineToyboxUser || !PairPerms.CanUseVibeRemote;
        bool patternExecuteDisabled = !PairPerms.CanExecutePatterns || !StickyPair.PairGlobals.ToyIsActive || !lastLightStorage.Patterns.Any();
        bool patternStopDisabled = !PairPerms.CanStopPatterns || !StickyPair.PairGlobals.ToyIsActive || lastToyboxData.ActivePatternId.IsEmptyGuid();
        bool alarmToggleDisabled = !PairPerms.CanToggleAlarms || !lastLightStorage.Alarms.Any();
        bool alarmSendDisabled = !PairPerms.CanSendAlarms;
        bool triggerToggleDisabled = !PairPerms.CanToggleTriggers || !lastLightStorage.Triggers.Any();

        ////////// TOGGLE PAIRS ACTIVE TOYS //////////
        string toyToggleText = StickyPair.PairGlobals.ToyIsActive ? "Turn Off " + PairNickOrAliasOrUID + "'s Toys" : "Turn On " + PairNickOrAliasOrUID + "'s Toys";
        if (_uiShared.IconTextButton(FontAwesomeIcon.User, toyToggleText, WindowMenuWidth, true))
        {
            _ = _apiHubMain.UserUpdateOtherGlobalPerm(new UserGlobalPermChangeDto(StickyPair.UserData,
                new KeyValuePair<string, object>("ToyIsActive", !StickyPair.PairGlobals.ToyIsActive), MainHub.PlayerUserData));
            _logger.LogDebug("Toggled Toybox for " + PairNickOrAliasOrUID + "(New State: " + !StickyPair.PairGlobals.ToyIsActive + ")", LoggerType.Permissions);
        }
        UiSharedService.AttachToolTip("Toggles the state of " + PairNickOrAliasOrUID + "'s connected Toys.");

        ////////// OPEN VIBE REMOTE WITH PAIR //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.Mobile, "Create Vibe Remote with " + PairNickOrAliasOrUID, WindowMenuWidth, true, openVibeRemoteDisabled))
        {
            // open a new private hosted room between the two of you automatically.
            // figure out how to do this later.
            _logger.LogDebug("Vibe Remote instance button pressed for " + PairNickOrAliasOrUID);
        }
        UiSharedService.AttachToolTip("Open a Remote UI that let's you control " + PairNickOrAliasOrUID + "'s Toys.");

        ////////// EXECUTE PATTERN ON PAIR'S TOY //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.PlayCircle, ("Activate " + PairNickOrAliasOrUID + "'s Patterns"), WindowMenuWidth, true, patternExecuteDisabled))
        {
            Opened = (Opened == InteractionType.ActivatePattern) ? InteractionType.None : InteractionType.ActivatePattern;
        }
        UiSharedService.AttachToolTip("Play one of " + PairNickOrAliasOrUID + "'s patterns to their active Toy.");
        if (Opened is InteractionType.ActivatePattern)
        {
            using (var actionChild = ImRaii.Child("PatternExecuteChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!actionChild) return;

                // Grab the currently stored selected PatternDto.
                LightPattern selectedPattern = _permActions.GetSelectedItem<LightPattern>("ExecutePatternForPairPermCombo", PairUID) ?? new LightPattern();
                _permActions.DrawGenericComboButton(PairUID, "ExecutePatternForPairPermCombo", "Play", WindowMenuWidth,
                    comboItems: lastLightStorage.Patterns, 
                    itemToName: (Pattern) => Pattern.Name + "(" + Pattern.Duration.Minutes + "m + " + Pattern.Duration.Seconds + "s)" + (Pattern.ShouldLoop ? " Loop" : ""),
                    isSearchable: true, 
                    buttonDisabled: selectedPattern.Identifier.IsEmptyGuid(),
                    isIconButton: true,
                    initialSelectedItem: lastLightStorage.Patterns.FirstOrDefault(x => x.Identifier == selectedPattern.Identifier) ?? lastLightStorage.Patterns.First(),
                    icon: FontAwesomeIcon.Play,
                    onSelected: (selected) => { _logger.LogDebug("Selected Pattern Set: " + selected, LoggerType.Permissions); },
                    onButton: (onButtonPress) =>
                    {
                        try
                        {
                            var newToyboxData = lastToyboxData.DeepClone();
                            if (newToyboxData is null || onButtonPress is null) throw new Exception("Toybox data is null, not sending");
                            // set all other stored patterns active state to false, and the pattern with the onButtonPress matching GUID to true.
                            newToyboxData.InteractionId = onButtonPress.Identifier;
                            newToyboxData.ActivePatternId = onButtonPress.Identifier;

                            // Run the call to execute the pattern to the server.
                            _ = _apiHubMain.UserPushPairDataToyboxUpdate(new(StickyPair.UserData, newToyboxData, DataUpdateKind.ToyboxPatternExecuted));
                            _logger.LogDebug("Executing Pattern " + onButtonPress.Name + " on " + PairNickOrAliasOrUID + "'s Toy", LoggerType.Permissions);
                            Opened = InteractionType.None;
                        }
                        catch (Exception e) { _logger.LogError("Failed to push updated ToyboxPattern data: " + e.Message); }
                    });
            }
            ImGui.Separator();
        }

        ////////// STOP RUNNING PATTERN ON PAIR'S TOY //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.StopCircle, "Stop " + PairNickOrAliasOrUID + "'s Active Pattern", WindowMenuWidth, true, patternStopDisabled))
        {
            try
            {
                var newToyboxData = lastToyboxData.DeepClone();
                if (newToyboxData == null) throw new Exception("Toybox data is null, not sending");

                _ = _apiHubMain.UserPushPairDataToyboxUpdate(new(StickyPair.UserData, newToyboxData, DataUpdateKind.ToyboxPatternStopped));
                _logger.LogDebug("Stopped active Pattern running on "+PairNickOrAliasOrUID+"'s toy", LoggerType.Permissions);
            }
            catch (Exception e) { _logger.LogError("Failed to push updated ToyboxPattern data: " + e.Message); }
        }
        UiSharedService.AttachToolTip("Halt the active pattern on " + PairUID + "'s Toy");

        ////////// TOGGLE ALARM FOR PAIR //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.Clock, "Toggle " + PairNickOrAliasOrUID + "'s Alarms", WindowMenuWidth, true, alarmToggleDisabled))
        {
            Opened = Opened == InteractionType.ToggleAlarm ? InteractionType.None : InteractionType.ToggleAlarm;
        }
        UiSharedService.AttachToolTip("Toggle " + PairUID + "'s Alarms.");
        if (Opened is InteractionType.ToggleAlarm)
        {
            using (var actionChild = ImRaii.Child("AlarmToggleChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!actionChild) return;


                LightAlarm selectedAlarm = _permActions.GetSelectedItem<LightAlarm>("ToggleAlarmForPairPermCombo", PairUID) ?? new LightAlarm();
                bool isEnabled = StickyPair.LastToyboxData?.ActiveAlarms.Contains(selectedAlarm.Identifier) ?? false;

                _permActions.DrawGenericComboButton(PairUID, "ExecutePatternForPairPermCombo", (isEnabled ? "Disable" : "Enable"), WindowMenuWidth, 
                    comboItems: lastLightStorage.Alarms, 
                    itemToName: (Alarm) => Alarm.Name + " (" + (Alarm.SetTimeUTC.ToLocalTime().ToString("HH:mm")) + ") (Plays: " + Alarm.PatternThatPlays + ")",
                    isSearchable: true,
                    buttonDisabled: selectedAlarm.Name == string.Empty,
                    isIconButton: false,
                    initialSelectedItem: lastLightStorage.Alarms.FirstOrDefault(x => x.Identifier == selectedAlarm.Identifier) ?? lastLightStorage.Alarms.First(),
                    onSelected: (selected) => { _logger.LogDebug("Selected Alarm: " + selected?.Name); },
                    onButton: (onButtonPress) =>
                    {
                        try
                        {
                            if (onButtonPress is null) throw new Exception("Alarm is null, not sending");
                            var newToyboxData = lastToyboxData.DeepClone();
                            var alarmToToggle = lastLightStorage.Alarms.FirstOrDefault(x => x.Identifier == onButtonPress.Identifier);
                            if (alarmToToggle is null) throw new Exception("Alarm not found in list.");
                            // toggle the alarm state.
                            newToyboxData.InteractionId = onButtonPress.Identifier;
                            // if the id was in the active alarm list, remove it, otherwise, add it.
                            if (newToyboxData.ActiveAlarms.Contains(onButtonPress.Identifier)) 
                                newToyboxData.ActiveAlarms.Remove(onButtonPress.Identifier);
                            else 
                                newToyboxData.ActiveAlarms.Add(onButtonPress.Identifier);

                            // Send out the command.
                            _ = _apiHubMain.UserPushPairDataToyboxUpdate(new(StickyPair.UserData, newToyboxData, DataUpdateKind.ToyboxAlarmToggled));
                            _logger.LogDebug("Toggling Alarm " + onButtonPress.Name + " on " + PairNickOrAliasOrUID + "'s AlarmList", LoggerType.Permissions);
                            Opened = InteractionType.None;
                        }
                        catch (Exception e) { _logger.LogError("Failed to push updated ToyboxPattern data: " + e.Message); }
                    });
            }
            ImGui.Separator();
        }

        ////////// TOGGLE TRIGGER FOR PAIR //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.LandMineOn, "Toggle " + PairNickOrAliasOrUID + "'s Triggers", WindowMenuWidth, true, triggerToggleDisabled))
        {
            Opened = Opened == InteractionType.ToggleTrigger ? InteractionType.None : InteractionType.ToggleTrigger;
        }
        UiSharedService.AttachToolTip("Toggle the state of a trigger in " + PairNickOrAliasOrUID + "'s triggerList.");
        if (Opened is InteractionType.ToggleTrigger)
        {
            using (var actionChild = ImRaii.Child("TriggerToggleChild", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!actionChild) return;

                LightTrigger selected = _permActions.GetSelectedItem<LightTrigger>("ToggleTriggerForPairPermCombo", PairUID) ?? new LightTrigger();
                bool isEnabled = lastToyboxData.ActiveTriggers.Contains(selected.Identifier);

                _permActions.DrawGenericComboButton(PairUID, "ToggleTriggerForPairPermCombo", isEnabled ? "Disable" : "Enable", WindowMenuWidth,
                   comboItems: lastLightStorage.Triggers,
                   itemToName: (Trigger) => Trigger.Name + " (Type: " + Trigger.Type.TriggerKindToString()+ ") (" + Trigger.ActionOnTrigger.ToName() + ")",
                   isSearchable: true,
                   buttonDisabled: selected.Name == string.Empty,
                   onSelected: (onSelected) => { _logger.LogDebug("Selected Trigger: " + onSelected?.Name ?? "UNK NAME", LoggerType.Permissions); },
                   onButton: (onButtonPress) =>
                    {
                        try
                        {
                            if (onButtonPress is null) throw new Exception("Trigger is null, not sending");
                            var newToyboxData = lastToyboxData.DeepClone();
                            var triggerToToggle = lastLightStorage.Triggers.FirstOrDefault(x => x.Identifier == onButtonPress.Identifier);
                            if (triggerToToggle is null) throw new Exception("Trigger not found in list.");
                            // toggle the trigger state.
                            newToyboxData.InteractionId = onButtonPress.Identifier;
                            // if the id was in the active alarm list, remove it, otherwise, add it.
                            if (newToyboxData.ActiveTriggers.Contains(onButtonPress.Identifier)) 
                                newToyboxData.ActiveTriggers.Remove(onButtonPress.Identifier);
                            else 
                                newToyboxData.ActiveTriggers.Add(onButtonPress.Identifier);

                            _ = _apiHubMain.UserPushPairDataToyboxUpdate(new(StickyPair.UserData, newToyboxData, DataUpdateKind.ToyboxTriggerToggled));
                            _logger.LogDebug("Toggling Trigger "+onButtonPress.Name+" on "+PairNickOrAliasOrUID+"'s TriggerList", LoggerType.Permissions);
                            Opened = InteractionType.None;
                        }
                        catch (Exception e) { _logger.LogError("Failed to push updated ToyboxPattern data: " + e.Message); }
                    });
            }
        }
        ImGui.Separator();
    }
}
