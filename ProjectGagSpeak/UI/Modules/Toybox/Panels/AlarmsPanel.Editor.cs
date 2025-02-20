using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using ImGuiNET;

namespace GagSpeak.UI.Toybox;

public partial class AlarmsPanel
{
    private void DrawEditor(Vector2 remainingRegion)
    {
        ImGui.Text("Alarm Editor Placeholder");
    }

    /*private void DrawAlarmSelectable(Alarm alarm, int idx)
    {
        //  automatically handle whether to use a 12-hour or 24-hour clock.
        var localTime = alarm.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
        string patternName = _handler.PatternName(alarm.PatternToPlay);
        // define our sizes
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var toggleSize = _uiShared.GetIconButtonSize(alarm.Enabled ? FontAwesomeIcon.ToggleOn : FontAwesomeIcon.ToggleOff);
        var nameTextSize = ImGui.CalcTextSize(alarm.Label);
        Vector2 alarmTextSize;
        var frequencyTextSize = ImGui.CalcTextSize(_handler.GetAlarmFrequencyString(alarm.RepeatFrequency));
        var patternNameSize = ImGui.CalcTextSize(patternName);
        string patternToPlayName = _handler.PatternName(alarm.PatternToPlay);
        using (_uiShared.UidFont.Push())
        {
            alarmTextSize = ImGui.CalcTextSize($"{localTime}");
        }
        using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), LastHoveredIndex == idx);
        using (ImRaii.Child($"##EditAlarmHeader{alarm.Identifier}", new Vector2(UiSharedService.GetWindowContentRegionWidth(), 65f)))
        {
            // create a group for the bounding area
            using (var group = ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                _uiShared.BigText($"{localTime}");
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((alarmTextSize.Y - nameTextSize.Y) / 2) + 5f);
                UiSharedService.ColorText(alarm.Label, ImGuiColors.DalamudGrey2);
            }

            // now draw the lower section out.
            using (var group = ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                UiSharedService.ColorText(_handler.GetAlarmFrequencyString(alarm.RepeatFrequency), ImGuiColors.DalamudGrey3);
                ImGui.SameLine();
                UiSharedService.ColorText("| " + patternToPlayName, ImGuiColors.DalamudGrey3);
            }

            // now, head to the sameline of the full width minus the width of the button
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - toggleSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (65f - toggleSize.Y) / 2);
            // draw out the icon button
            if (_uiShared.IconButton(alarm.Enabled ? FontAwesomeIcon.ToggleOn : FontAwesomeIcon.ToggleOff))
            {
                // set the enabled state of the alarm based on its current state so that we toggle it
                if (alarm.Enabled)
                    _handler.DisableAlarm(alarm);
                else
                    _handler.EnableAlarm(alarm);
                // toggle the state & early return so we dont access the childclicked button
                return;
            }
            if(idx is 0) _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.TogglingAlarms, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
        }
        if (ImGui.IsItemClicked())
            _handler.StartEditingAlarm(alarm);
    }*/
/*
    private void DrawAlarmEditor(Alarm alarmToCreate)
    {
        // Display the local time zone
        var textlength = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Clock, TimeZoneInfo.Local.StandardName);
        var localTime = alarmToCreate.SetTimeUTC.ToLocalTime();
        var hour = localTime.Hour;
        var minute = localTime.Minute;
        // set the x position to center the icontext button
        ImGui.Spacing();
        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - textlength) / 2);
        _uiShared.IconTextButton(FontAwesomeIcon.Clock, TimeZoneInfo.Local.StandardName, null!, true, true);
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.AlarmLocalTimeZone, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        // Draw out using the big pushed font, a large, blank button canvas
        using (ImRaii.Child("TimezoneFancyUI", new Vector2(UiSharedService.GetWindowContentRegionWidth(), 90f)))
        {
            // define the scales 
            Vector2 hourTextSize;
            Vector2 minuteTextSize;
            using (_uiShared.UidFont.Push())
            {
                hourTextSize = ImGui.CalcTextSize($"{hour:00}");
                minuteTextSize = ImGui.CalcTextSize($"{minute:00}");
            }

            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2 - (minuteTextSize.X + ImGui.GetStyle().ItemSpacing.X * 2));
            using (ImRaii.Child("FancyHourDisplay", new Vector2(hourTextSize.X + ImGui.GetStyle().ItemSpacing.X * 2, ImGui.GetContentRegionAvail().Y)))
            {
                var prevHour = $"{(hour - 1 + 24) % 24:00}";
                var currentHour = $"{hour:00}";
                var nextHour = $"{(hour + 1) % 24:00}";

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (hourTextSize.X - ImGui.CalcTextSize(prevHour).X) / 2);
                ImGui.TextDisabled(prevHour); // Previous hour (centered)

                _uiShared.BigText(currentHour);
                // adjust the hour with the mouse wheel
                if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
                {
                    hour = (hour - (int)ImGui.GetIO().MouseWheel + 24) % 24;
                    var newLocalTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, hour, localTime.Minute, 0);
                    alarmToCreate.SetTimeUTC = new DateTimeOffset(newLocalTime, TimeZoneInfo.Local.GetUtcOffset(newLocalTime)).ToUniversalTime();
                }

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (hourTextSize.X - ImGui.CalcTextSize(prevHour).X) / 2);
                ImGui.TextDisabled(nextHour); // Next hour (centered)
            }

            ImGui.SameLine((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2 - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.Y);
            _uiShared.BigText(":");
            ImGui.SameLine();

            using (ImRaii.Child("FancyMinuteDisplay", new Vector2(minuteTextSize.X + ImGui.GetStyle().ItemSpacing.X * 2, ImGui.GetContentRegionAvail().Y)))
            {
                var prevMinute = $"{(minute - 1 + 60) % 60:00}";
                var currentMinute = $"{minute:00}";
                var nextMinute = $"{(minute + 1) % 60:00}";

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (minuteTextSize.X - ImGui.CalcTextSize(prevMinute).X) / 2);
                ImGui.TextDisabled(prevMinute); // Previous hour (centered)

                _uiShared.BigText(currentMinute);
                // adjust the hour with the mouse wheel
                if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
                {
                    minute = (minute - (int)ImGui.GetIO().MouseWheel + 60) % 60;
                    var newLocalTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, minute, 0);
                    alarmToCreate.SetTimeUTC = new DateTimeOffset(newLocalTime, TimeZoneInfo.Local.GetUtcOffset(newLocalTime)).ToUniversalTime();
                }

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (minuteTextSize.X - ImGui.CalcTextSize(nextMinute).X) / 2);
                ImGui.TextDisabled(nextMinute); // Next hour (centered)
            }
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmTime, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5f);
        ImGui.Separator();
        ImGui.Spacing();

        // Input field for the Alarm name
        var name = alarmToCreate.Label;
        ImGui.SetNextItemWidth(UiSharedService.GetWindowContentRegionWidth() / 2);
        ImGui.InputText("Alarm Name", ref name, 32);
        if (ImGui.IsItemDeactivatedAfterEdit())
            alarmToCreate.Label = name;
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmName, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => alarmToCreate.Label = "Tutorial Alarm");

        // Input field for the pattern the alarm will play
        var pattern = alarmToCreate.PatternToPlay;
        // draw the selector on the left
        _uiShared.DrawComboSearchable("Alarm Pattern", UiSharedService.GetWindowContentRegionWidth() / 2, _patternHandler.Patterns, (i) => i.Name, true, (i) => alarmToCreate.PatternToPlay = i?.UniqueIdentifier ?? Guid.Empty);
        // tutorial stuff for the above combo.
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmPattern, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        // if the pattern is not the alarmTocreate.PatternToPlay, it has changed, so update newPatternMaxDuration
        TimeSpan durationTotal = _handler.GetPatternLength(alarmToCreate.PatternToPlay);
        var StartPointTimeSpan = alarmToCreate.PatternStartPoint;
        var PlaybackDuration = alarmToCreate.PatternDuration;

        var formatStart = durationTotal.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        _uiShared.DrawTimeSpanCombo("Playback Start-Point", durationTotal, ref StartPointTimeSpan, UiSharedService.GetWindowContentRegionWidth() / 2, formatStart, true);
        alarmToCreate.PatternStartPoint = StartPointTimeSpan;
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmStartPoint, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);


        // time difference calculation.
        if (alarmToCreate.PatternStartPoint > durationTotal) alarmToCreate.PatternStartPoint = durationTotal;
        var maxPlaybackDuration = durationTotal - alarmToCreate.PatternStartPoint;

        // playback duration
        var formatDuration = PlaybackDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        _uiShared.DrawTimeSpanCombo("Playback Duration", maxPlaybackDuration, ref PlaybackDuration, UiSharedService.GetWindowContentRegionWidth() / 2, formatDuration, true);
        alarmToCreate.PatternDuration = PlaybackDuration;
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmDuration, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        ImGui.Separator();

        // Frequency of occurrence
        ImGui.Text("Alarm Frequency Per Week");
        var alarmRepeatValues = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToArray();
        var totalValues = alarmRepeatValues.Length;
        var splitIndex = 4; // Index to split the groups

        using (ImRaii.Group())
        {
            // Group 1: First four
            using (ImRaii.Group())
            {
                for (var i = 0; i < splitIndex && i < totalValues; i++)
                {
                    var day = alarmRepeatValues[i];
                    var isSelected = alarmToCreate.RepeatFrequency.Contains(day);
                    if (ImGui.Checkbox(day.ToString(), ref isSelected))
                    {
                        if (isSelected)
                            alarmToCreate.RepeatFrequency.Add(day);
                        else
                            alarmToCreate.RepeatFrequency.Remove(day);
                    }
                }
            }
            ImGui.SameLine();

            // Group 2: Last three
            using (ImRaii.Group())
            {
                for (var i = splitIndex; i < totalValues; i++)
                {
                    var day = alarmRepeatValues[i];
                    var isSelected = alarmToCreate.RepeatFrequency.Contains(day);
                    if (ImGui.Checkbox(day.ToString(), ref isSelected))
                    {
                        if (isSelected)
                            alarmToCreate.RepeatFrequency.Add(day);
                        else
                            alarmToCreate.RepeatFrequency.Remove(day);
                    }
                }
            }
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingFrequency, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

    }*/
}
