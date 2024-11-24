using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui.Text;
using System.Globalization;
using System.Numerics;
namespace GagSpeak.UI.UiToybox;

public class ToyboxAlarmManager
{
    private readonly ILogger<ToyboxAlarmManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiShared;
    private readonly AlarmHandler _handler;
    private readonly PatternHandler _patternHandler;
    private readonly TutorialService _guides;
    public ToyboxAlarmManager(ILogger<ToyboxAlarmManager> logger, GagspeakMediator mediator,
        UiSharedService uiSharedService, AlarmHandler handler, PatternHandler patternHandler,
        TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _uiShared = uiSharedService;
        _handler = handler;
        _patternHandler = patternHandler;
        _guides = guides;
    }

    public string AlarmSearchString = string.Empty;
    private Alarm CreatedAlarm = new Alarm();
    public bool CreatingAlarm = false;
    private int LastHoveredIndex = -1; // -1 indicates no item is currently hovered
    private List<Alarm> FilteredAlarmsList
    => _handler.Alarms
        .Where(alarm => alarm.Name.Contains(AlarmSearchString, StringComparison.OrdinalIgnoreCase))
        .ToList();

    public void DrawAlarmManagerPanel()
    {
        // if we are creating a pattern
        if (CreatingAlarm)
        {
            DrawAlarmCreatingHeader();
            ImGui.Separator();
            DrawAlarmEditor(CreatedAlarm);
            return; // perform early returns so we dont access other methods
        }

        // if we are simply viewing the main page       
        if (_handler.ClonedAlarmForEdit is null)
        {
            DrawCreateAlarmHeader();
            ImGui.Separator();
            if (_handler.AlarmCount > 0)
                DrawAlarmSelectableMenu();

            return; // perform early returns so we dont access other methods
        }

        // if we are editing an alarm
        if (_handler.ClonedAlarmForEdit is not null)
        {
            DrawAlarmEditorHeader();
            ImGui.Separator();
            if (_handler.AlarmCount > 0 && _handler.ClonedAlarmForEdit is not null)
                DrawAlarmEditor(_handler.ClonedAlarmForEdit);
        }
    }

    private void DrawCreateAlarmHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize("New Alarm");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("CreateAlarmHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // draw out the icon button
            if (_uiShared.IconButton(FontAwesomeIcon.Plus))
            {
                // reset the createdAlarm to a new alarm, and set editing alarm to true
                CreatedAlarm = new Alarm();
                CreatingAlarm = true;
            }
            _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.CreatingAlarms, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => CreatingAlarm = true);

            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            _uiShared.BigText("New Alarm");
        }
    }

    private void DrawAlarmCreatingHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize($"Creating Alarm: {CreatedAlarm.Name}");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("EditAlarmHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                // reset the createdAlarm to a new alarm, and set editing alarm to true
                CreatedAlarm = new Alarm();
                CreatingAlarm = false;
            }
            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText(CreatedAlarm.Name, ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - iconSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.Save))
            {
                // add the newly created alarm to the list of alarms
                _handler.AddNewAlarm(CreatedAlarm);
                // reset to default and turn off creating status.
                CreatedAlarm = new Alarm();
                CreatingAlarm = false;
            }
            _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SavingAlarms, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => 
            {
                _handler.AddNewAlarm(CreatedAlarm); 
                CreatedAlarm = new Alarm(); 
                CreatingAlarm = false; 
            });
        }
    }

    private void DrawAlarmEditorHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize($"{_handler.ClonedAlarmForEdit!.Name}");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("EditAlarmHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                _handler.CancelEditingAlarm();
                return;
            }
            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText(_handler.ClonedAlarmForEdit.Name, ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - iconSize.X * 2 - ImGui.GetStyle().ItemSpacing.X * 2);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            // for saving contents
            if (_uiShared.IconButton(FontAwesomeIcon.Save))
            {
                // reset the createdAlarm to a new alarm, and set editing alarm to true
                _handler.SaveEditedAlarm();
            }
            UiSharedService.AttachToolTip("Save changes to Pattern & Return to Pattern List");

            // right beside it to the right, we need to draw the delete button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconButton(FontAwesomeIcon.Trash, disabled: !KeyMonitor.ShiftPressed()))
                _handler.RemoveAlarm(_handler.ClonedAlarmForEdit);
        }
    }

    private void DrawAlarmSelectableMenu()
    {
        var region = ImGui.GetContentRegionAvail();
        var topLeftSideHeight = region.Y;
        bool anyItemHovered = false;
        using (var rightChild = ImRaii.Child($"###AlarmListPreview", region with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
        {
            for (int i = 0; i < FilteredAlarmsList.Count; i++)
            {
                var set = FilteredAlarmsList[i];
                DrawAlarmSelectable(set, i);

                if (ImGui.IsItemHovered())
                {
                    anyItemHovered = true;
                    LastHoveredIndex = i;
                }

                // if the item is right clicked, open the popup
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && LastHoveredIndex == i && !FilteredAlarmsList[i].Enabled)
                {
                    ImGui.OpenPopup($"AlarmItemContext{i}");
                }
            }

            // if no item is hovered, reset the last hovered index
            if (!anyItemHovered) LastHoveredIndex = -1;

            if (LastHoveredIndex != -1 && LastHoveredIndex < FilteredAlarmsList.Count)
            {
                if (ImGui.BeginPopup($"AlarmItemContext{LastHoveredIndex}"))
                {
                    if (ImGui.Selectable("Delete Alarm") && FilteredAlarmsList[LastHoveredIndex] is not null)
                    {
                        _handler.RemoveAlarm(FilteredAlarmsList[LastHoveredIndex]);
                    }
                    ImGui.EndPopup();
                }
            }
        }
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.AlarmList, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
    }

    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - spacingX);
        string filter = AlarmSearchString;
        if (ImGui.InputTextWithHint("##AlarmSearchStringFilter", "Search for a Alarm", ref filter, 255))
        {
            AlarmSearchString = filter;
            LastHoveredIndex = -1;
        }
        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(AlarmSearchString));
        if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
        {
            AlarmSearchString = string.Empty;
            LastHoveredIndex = -1;
        }
    }

    private void DrawAlarmSelectable(Alarm alarm, int idx)
    {
        //  automatically handle whether to use a 12-hour or 24-hour clock.
        var localTime = alarm.SetTimeUTC.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
        string patternName = _handler.PatternName(alarm.PatternToPlay);
        // define our sizes
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var toggleSize = _uiShared.GetIconButtonSize(alarm.Enabled ? FontAwesomeIcon.ToggleOn : FontAwesomeIcon.ToggleOff);
        var nameTextSize = ImGui.CalcTextSize(alarm.Name);
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
                UiSharedService.ColorText(alarm.Name, ImGuiColors.DalamudGrey2);
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
    }

    private void DrawAlarmEditor(Alarm alarmToCreate)
    {
        // Display the local time zone
        var textlength = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Clock, TimeZoneInfo.Local.StandardName);
        var localTime = alarmToCreate.SetTimeUTC.ToLocalTime();
        int hour = localTime.Hour;
        int minute = localTime.Minute;
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
                string prevHour = $"{(hour - 1 + 24) % 24:00}";
                string currentHour = $"{hour:00}";
                string nextHour = $"{(hour + 1) % 24:00}";

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
                string prevMinute = $"{(minute - 1 + 60) % 60:00}";
                string currentMinute = $"{minute:00}";
                string nextMinute = $"{(minute + 1) % 60:00}";

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
        var name = alarmToCreate.Name;
        ImGui.SetNextItemWidth(UiSharedService.GetWindowContentRegionWidth() / 2);
        ImGui.InputText("Alarm Name", ref name, 32);
        if (ImGui.IsItemDeactivatedAfterEdit())
            alarmToCreate.Name = name;
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmName, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => alarmToCreate.Name = "Tutorial Alarm");

        // Input field for the pattern the alarm will play
        var pattern = alarmToCreate.PatternToPlay;
        // draw the selector on the left
        _uiShared.DrawComboSearchable("Alarm Pattern", UiSharedService.GetWindowContentRegionWidth() / 2, _patternHandler.Patterns, (i) => i.Name, true, (i) => alarmToCreate.PatternToPlay = i?.UniqueIdentifier ?? Guid.Empty);
        // tutorial stuff for the above combo.
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmPattern, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        // if the pattern is not the alarmTocreate.PatternToPlay, it has changed, so update newPatternMaxDuration
        TimeSpan durationTotal = _handler.GetPatternLength(alarmToCreate.PatternToPlay);
        TimeSpan StartPointTimeSpan = alarmToCreate.PatternStartPoint;
        TimeSpan PlaybackDuration = alarmToCreate.PatternDuration;

        string formatStart = durationTotal.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        _uiShared.DrawTimeSpanCombo("Playback Start-Point", durationTotal, ref StartPointTimeSpan, UiSharedService.GetWindowContentRegionWidth() / 2, formatStart, true);
        alarmToCreate.PatternStartPoint = StartPointTimeSpan;
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmStartPoint, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);


        // time difference calculation.
        if (alarmToCreate.PatternStartPoint > durationTotal) alarmToCreate.PatternStartPoint = durationTotal;
        TimeSpan maxPlaybackDuration = durationTotal - alarmToCreate.PatternStartPoint;

        // playback duration
        string formatDuration = PlaybackDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        _uiShared.DrawTimeSpanCombo("Playback Duration", maxPlaybackDuration, ref PlaybackDuration, UiSharedService.GetWindowContentRegionWidth() / 2, formatDuration, true);
        alarmToCreate.PatternDuration = PlaybackDuration;
        _guides.OpenTutorial(TutorialType.Alarms, StepsAlarms.SettingAlarmDuration, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        ImGui.Separator();

        // Frequency of occurrence
        ImGui.Text("Alarm Frequency Per Week");
        var alarmRepeatValues = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToArray();
        int totalValues = alarmRepeatValues.Length;
        int splitIndex = 4; // Index to split the groups

        using (ImRaii.Group())
        {
            // Group 1: First four
            using (ImRaii.Group())
            {
                for (int i = 0; i < splitIndex && i < totalValues; i++)
                {
                    DayOfWeek day = alarmRepeatValues[i];
                    bool isSelected = alarmToCreate.RepeatFrequency.Contains(day);
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
                for (int i = splitIndex; i < totalValues; i++)
                {
                    DayOfWeek day = alarmRepeatValues[i];
                    bool isSelected = alarmToCreate.RepeatFrequency.Contains(day);
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

    }
}
