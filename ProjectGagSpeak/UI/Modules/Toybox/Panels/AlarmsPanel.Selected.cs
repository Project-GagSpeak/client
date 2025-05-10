using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Toybox;

public partial class AlarmsPanel
{
    private void DrawLabel(Alarm alarm, bool isEditing)
    {
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        CkGui.ColorTextFrameAligned("Name", ImGuiColors.ParsedGold);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using (var c = CkRaii.ChildPaddedW("PatternName", ImGui.GetContentRegionAvail().X * .6f, ImGui.GetFrameHeight(),
            CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll))
        {
            if (isEditing)
            {
                var refName = alarm.Label;
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.InputTextWithHint("##AlarmName", "Name Here...", ref refName, 50))
                    alarm.Label = refName;
            }
            else
            {
                ImUtf8.TextFrameAligned(alarm.Label);
            }
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmName, Vector2.Zero, Vector2.Zero, () => alarm.Label = "Tutorial Alarm");
    }

    private void DrawAlarmTime(Alarm alarm, bool isEditing)
    {
        // Display the local time zone
        var padding = ImGui.GetStyle().FramePadding;
        var innerSpacing = ImGui.GetStyle().ItemInnerSpacing;
        var textlength = CkGui.IconSize(FAI.Clock).X + padding.X * 4 + innerSpacing.X + ImGui.CalcTextSize(TimeZoneInfo.Local.StandardName).X;
        var timeLengthDisp = CkGuiUtils.GetDateTimeDisplayWidth(alarm.SetTimeUTC);
        var width = Math.Max(textlength, timeLengthDisp);
        var height = CkGuiUtils.GetTimeDisplayHeight() + ImGui.GetFrameHeightWithSpacing();
        // create a group with a background and some rounding.
        using (var c = CkRaii.ChildPadded("C_AlarmTime", new Vector2(width, height), CkColor.FancyHeaderContrast.Uint(), CkRaii.GetChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
        {
            // Draw out the local timezone this alarm is relative too.
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (c.InnerRegion.X - textlength) / 2);
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(FAI.Clock);
                CkGui.TextFrameAlignedInline(TimeZoneInfo.Local.StandardName);
            }
            CkGui.AttachToolTip("The time that this alarm will go off at." +
                "--SEP--This is relative to your local timezone.");
            _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.AlarmLocalTimeZone, Vector2.Zero, Vector2.Zero);

            // Draw the time in a child.
            if (isEditing)
            {
                var refTime = alarm.SetTimeUTC;
                CkGuiUtils.DateTimeEditorUtcAsLocal("AlarmTimeEdit", ref refTime, c.InnerRegion.X);
                alarm.SetTimeUTC = refTime;
            }
            else
            {
                CkGuiUtils.DateTimePreviewUtcAsLocal("AlarmTime", alarm.SetTimeUTC, c.InnerRegion.X);
            }
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmTime, Vector2.Zero, Vector2.Zero);
    }

    private void DrawPatternSelection(Alarm alarm, bool isEditing)
    {
        CkGui.ColorTextFrameAligned("Pattern to Play", ImGuiColors.ParsedGold);
        var comboW = ImGui.GetContentRegionAvail().X * .5f;
        var change = _patternCombo.Draw("##AlarmPattern", alarm.PatternRef.Identifier, comboW);
        
        // Updates upon change.
        if(change && _patterns.Storage.FirstOrDefault(x => x.Identifier == alarm.PatternRef.Identifier) is { } match)
        {
            alarm.PatternRef = match;
            alarm.PatternStartPoint = TimeSpan.Zero;
            alarm.PatternDuration = match.Duration;
        }
        // Resets upon right click.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            alarm.PatternRef = Pattern.AsEmpty();
            alarm.PatternStartPoint = TimeSpan.Zero;
            alarm.PatternDuration = TimeSpan.Zero;
        }

    }

    private void DrawPatternTimeSpans(Alarm alarm, bool isEditing)
    {
        var refDuration = alarm.PatternDuration;
        var refStartPoint = alarm.PatternStartPoint;
        var refPlaybackDuration = alarm.PatternDuration;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        // Split things up into 2 columns.
        var columnWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        var height = CkGuiUtils.GetTimeDisplayHeight() + ImGui.GetFrameHeightWithSpacing();

        // Enter the first column.
        using (var c = CkRaii.ChildPaddedW("AlarmStartPnt", columnWidth, height, CkColor.FancyHeaderContrast.Uint(),
            CkRaii.GetChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
        {
            CkGui.ColorTextCentered("Start Point", ImGuiColors.ParsedGold);
            var format = refDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
            if (isEditing)
            {
                CkGuiUtils.TimeSpanEditor("AlarmStartPnt", refDuration, ref refStartPoint, format, c.InnerRegion.X);
                alarm.PatternStartPoint = refStartPoint;
            }
            else
            {
                CkGuiUtils.TimeSpanPreview("AlarmStartPnt", refDuration, refStartPoint, format, c.InnerRegion.X);
            }
        }

        // get time difference and apply the changes.
        if (alarm.PatternStartPoint > refDuration)
            alarm.PatternStartPoint = refDuration;

        // set the maximum possible playback duration allowed.
        var maxPlaybackDuration = refDuration - alarm.PatternStartPoint;

        // Shift to next column and display the pattern playback child.
        ImGui.SameLine();
        using (var c = CkRaii.ChildPaddedW("AlarmStartPnt", columnWidth, height, CkColor.FancyHeaderContrast.Uint(),
            CkRaii.GetChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
        {
            CkGui.ColorTextCentered("Playback Duration", ImGuiColors.ParsedGold);
            var format = refPlaybackDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
            if (isEditing)
            {
                CkGuiUtils.TimeSpanEditor("PlaybackDur", maxPlaybackDuration, ref refPlaybackDuration, format, c.InnerRegion.X);
                alarm.PatternDuration = refPlaybackDuration;
            }
            else
            {
                CkGuiUtils.TimeSpanPreview("PlaybackDur", maxPlaybackDuration, refPlaybackDuration, format, c.InnerRegion.X);
            }
        }
    }

    private void DrawAlarmFrequency(Alarm alarm, bool isEditing)
    {
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
                    var isSelected = alarm.RepeatFrequency.Contains(day);
                    if (ImGui.Checkbox(day.ToString(), ref isSelected))
                    {
                        if (isSelected)
                            alarm.RepeatFrequency.Add(day);
                        else
                            alarm.RepeatFrequency.Remove(day);
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
                    var isSelected = alarm.RepeatFrequency.Contains(day);
                    if (ImGui.Checkbox(day.ToString(), ref isSelected))
                    {
                        if (isSelected)
                            alarm.RepeatFrequency.Add(day);
                        else
                            alarm.RepeatFrequency.Remove(day);
                    }
                }
            }
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingFrequency, Vector2.Zero, Vector2.Zero);
    }

    private void DrawFooter(Alarm pattern)
    {
        ImUtf8.TextFrameAligned("ID:");
        CkGui.ColorTextFrameAlignedInline(pattern.Identifier.ToString(), ImGuiColors.DalamudGrey3);
    }
}
