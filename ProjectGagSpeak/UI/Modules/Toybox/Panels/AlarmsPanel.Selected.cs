using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Models;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using GagSpeak.Services;

namespace GagSpeak.Gui.Toybox;

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
        var timeLengthDisp = CkGuiUtils.GetDateTimeDisplayWidth(alarm.SetTimeUTC, UiFontService.UidFont);
        var width = Math.Max(textlength, timeLengthDisp);
        var height = CkGuiUtils.GetTimeDisplayHeight(UiFontService.UidFont) + ImGui.GetFrameHeightWithSpacing();
        // create a group with a background and some rounding.
        using (var c = CkRaii.ChildPadded("C_AlarmTime", new Vector2(width, height), CkColor.FancyHeaderContrast.Uint(), CkStyle.ChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
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
                CkGuiUtils.DateTimeEditorUtcAsLocal("AlarmTimeEdit", ref refTime, UiFontService.UidFont, c.InnerRegion.X);
                alarm.SetTimeUTC = refTime;
            }
            else
            {
                CkGuiUtils.DateTimePreviewUtcAsLocal("AlarmTime", alarm.SetTimeUTC, UiFontService.UidFont, c.InnerRegion.X);
            }
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmTime, Vector2.Zero, Vector2.Zero);
    }

    private void DrawPatternSelection(Alarm alarm, bool isEditing)
    {
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, 0);

        var comboW = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) * .5f;
        CkGui.ColorTextFrameAligned("Alarm Pattern to Play", ImGuiColors.ParsedGold);
        using (CkRaii.Child("AlarmPattern", new Vector2(comboW, ImGui.GetFrameHeight()), CkColor.FancyHeaderContrast.Uint(), CkStyle.ChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            // Draw the pattern selection combo box.
            if (isEditing)
            {
                var change = _patternCombo.Draw("##AlarmPattern", alarm.PatternRef.Identifier, comboW);

                // Updates upon change.
                if (change && _patterns.Storage.FirstOrDefault(x => x.Identifier == _patternCombo.Current?.Identifier) is { } match)
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
            else
            {
                CkGui.TextFrameAlignedInline(alarm.PatternRef.Label);
            }
        }
    }

    private void DrawPatternTimeSpans(Alarm alarm, bool isEditing)
    {
        var refDuration = alarm.PatternRef.Duration;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        // Split things up into 2 columns.
        var columnWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        var height = CkGuiUtils.GetTimeDisplayHeight(UiFontService.UidFont) + ImGui.GetFrameHeightWithSpacing();

        // Enter the first column.
        using (ImRaii.Group())
        {
            var refStartPoint = alarm.PatternStartPoint;
            using (var c = CkRaii.ChildPaddedW("AlarmStartPnt", columnWidth, height, CkColor.FancyHeaderContrast.Uint(),
                CkStyle.ChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
            {
                ImGuiUtil.Center("Start Point");
                var format = refDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
                if (isEditing)
                {
                    CkGuiUtils.TimeSpanEditor("AlarmStartPnt", refDuration, ref refStartPoint, format, UiFontService.UidFont, c.InnerRegion.X);
                    alarm.PatternStartPoint = refStartPoint;
                }
                else
                {
                    CkGuiUtils.TimeSpanPreview("AlarmStartPnt", refDuration, refStartPoint, format, UiFontService.UidFont, c.InnerRegion.X);
                }
            }
        }

        // Prevent Overflow.
        if (alarm.PatternStartPoint > refDuration)
            alarm.PatternStartPoint = refDuration;

        // Ensure duration + startpoint does not exceed threshold.
        if (alarm.PatternStartPoint + alarm.PatternDuration > refDuration)
            alarm.PatternDuration = refDuration - alarm.PatternStartPoint;

        // set the maximum possible playback duration allowed.
        var maxPlaybackDuration = refDuration - alarm.PatternStartPoint;

        // Shift to next column and display the pattern playback child.
        ImGui.SameLine();
        using (ImRaii.Group())
        {
            var refPlaybackDuration = alarm.PatternDuration;
            using (var c = CkRaii.ChildPaddedW("AlarmPlaybackDur", columnWidth, height, CkColor.FancyHeaderContrast.Uint(),
                CkStyle.ChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
            {
                ImGuiUtil.Center("Playback Duration");
                var format = refPlaybackDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
                if (isEditing)
                {
                    CkGuiUtils.TimeSpanEditor("PlaybackDur", maxPlaybackDuration, ref refPlaybackDuration, format, UiFontService.UidFont, c.InnerRegion.X);
                    alarm.PatternDuration = refPlaybackDuration;
                }
                else
                {
                    CkGuiUtils.TimeSpanPreview("PlaybackDur", maxPlaybackDuration, refPlaybackDuration, format, UiFontService.UidFont, c.InnerRegion.X);
                }
            }
        }
    }

    private void DrawAlarmFrequency(Alarm alarm, bool isEditing)
    {
        using var _ = ImRaii.Group();
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());
        
        // Frequency of occurrence
        CkGui.ColorText("Alarm Frequency Per Week", ImGuiColors.ParsedGold);

        using var dis = ImRaii.Disabled(!isEditing);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f, !isEditing);
        using var font = ImRaii.PushFont(UiBuilder.MonoFont);

        // calc the exact width of each checkbox.
        var checkboxWidth = ImGui.CalcTextSize("MMM").X + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.GetFrameHeight();
        var dayFilters = (uint)alarm.DaysToFire;
        var daysOfTheWeek = Enum.GetValues<DaysOfWeek>().Skip(1).ToArray();
        for (var i = 0; i < daysOfTheWeek.Length; i++)
        {
            ImGui.CheckboxFlags(daysOfTheWeek[i].ToShortName(), ref dayFilters, (uint)daysOfTheWeek[i]);
            ImGui.SameLine();

            if (ImGui.GetContentRegionAvail().X - checkboxWidth < 0)
                ImGui.NewLine();
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingFrequency, Vector2.Zero, Vector2.Zero);

        if (dayFilters != (uint)alarm.DaysToFire)
            alarm.DaysToFire = (DaysOfWeek)dayFilters;
    }

    private void DrawFooter(Alarm alarm)
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
        ImUtf8.TextFrameAligned(alarm.Identifier.ToString());
    }
}
