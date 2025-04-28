using GagSpeak.Services;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Utility;
public static partial class CkGuiUtils
{
    public static void DrawTimeSpanCombo(string label, TimeSpan patternMaxDuration, ref TimeSpan patternDuration, float width, string format = "hh\\:mm\\:ss", bool showLabel = true)
    {
        if (patternDuration > patternMaxDuration) patternDuration = patternMaxDuration;

        var maxDurationFormatted = patternMaxDuration.ToString(format);
        var patternDurationFormatted = patternDuration.ToString(format);

        // Button to open popup
        var pos = ImGui.GetCursorScreenPos();
        if (ImGui.Button($"{patternDurationFormatted} / {maxDurationFormatted}##TimeSpanCombo-{label}", new Vector2(width, ImGui.GetFrameHeight())))
        {
            ImGui.SetNextWindowPos(new Vector2(pos.X, pos.Y + ImGui.GetFrameHeight()));
            ImGui.OpenPopup($"TimeSpanPopup-{label}");
        }
        // just to the right of it, aligned with the button, display the label
        if (showLabel)
        {
            ImUtf8.SameLineInner();
            ImGui.TextUnformatted(label);
        }

        // Popup
        if (ImGui.BeginPopup($"TimeSpanPopup-{label}"))
        {
            DrawTimeSpanUI(ref patternDuration, patternMaxDuration, width, format);
            ImGui.EndPopup();
        }
    }

    private static void DrawTimeSpanUI(ref TimeSpan patternDuration, TimeSpan maxDuration, float width, string format)
    {
        var totalColumns = GetColumnCountFromFormat(format);
        var extraPadding = ImGui.GetStyle().ItemSpacing.X;

        Vector2 patternHourTextSize;
        Vector2 patternMinuteTextSize;
        Vector2 patternSecondTextSize;
        Vector2 patternMillisecondTextSize;

        using (UiFontService.UidFont.Push())
        {
            patternHourTextSize = ImGui.CalcTextSize($"{patternDuration.Hours:00}h");
            patternMinuteTextSize = ImGui.CalcTextSize($"{patternDuration.Minutes:00}m");
            patternSecondTextSize = ImGui.CalcTextSize($"{patternDuration.Seconds:00}s");
            patternMillisecondTextSize = ImGui.CalcTextSize($"{patternDuration.Milliseconds:000}ms");
        }

        // Specify the number of columns. In this case, 2 for minutes and seconds.
        if (ImGui.BeginTable("TimeDurationTable", totalColumns)) // 3 columns for hours, minutes, seconds
        {
            // Setup columns based on the format
            if (format.Contains("hh")) ImGui.TableSetupColumn("##Hours", ImGuiTableColumnFlags.WidthFixed, patternHourTextSize.X + totalColumns + 1);
            if (format.Contains("mm")) ImGui.TableSetupColumn("##Minutes", ImGuiTableColumnFlags.WidthFixed, patternMinuteTextSize.X + totalColumns + 1);
            if (format.Contains("ss")) ImGui.TableSetupColumn("##Seconds", ImGuiTableColumnFlags.WidthFixed, patternSecondTextSize.X + totalColumns + 1);
            if (format.Contains("fff")) ImGui.TableSetupColumn("##Milliseconds", ImGuiTableColumnFlags.WidthFixed, patternMillisecondTextSize.X + totalColumns + 1);
            ImGui.TableNextRow();

            // Draw components based on the format
            if (format.Contains("hh"))
            {
                ImGui.TableNextColumn();
                DrawTimeComponentUI(ref patternDuration, maxDuration, "h");
            }
            if (format.Contains("mm"))
            {
                ImGui.TableNextColumn();
                DrawTimeComponentUI(ref patternDuration, maxDuration, "m");
            }
            if (format.Contains("ss"))
            {
                ImGui.TableNextColumn();
                DrawTimeComponentUI(ref patternDuration, maxDuration, "s");
            }
            if (format.Contains("fff"))
            {
                ImGui.TableNextColumn();
                DrawTimeComponentUI(ref patternDuration, maxDuration, "ms");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawTimeComponentUI(ref TimeSpan duration, TimeSpan maxDuration, string suffix)
    {
        var prevValue = suffix switch
        {
            "h" => $"{Math.Max(0, (duration.Hours - 1)):00}",
            "m" => $"{Math.Max(0, (duration.Minutes - 1)):00}",
            "s" => $"{Math.Max(0, (duration.Seconds - 1)):00}",
            "ms" => $"{Math.Max(0, (duration.Milliseconds - 10)):000}",
            _ => $"UNK"
        };

        var currentValue = suffix switch
        {
            "h" => $"{duration.Hours:00}h",
            "m" => $"{duration.Minutes:00}m",
            "s" => $"{duration.Seconds:00}s",
            "ms" => $"{duration.Milliseconds:000}ms",
            _ => $"UNK"
        };

        var nextValue = suffix switch
        {
            "h" => $"{Math.Min(maxDuration.Hours, (duration.Hours + 1)):00}",
            "m" => $"{Math.Min(maxDuration.Minutes, (duration.Minutes + 1)):00}",
            "s" => $"{Math.Min(maxDuration.Seconds, (duration.Seconds + 1)):00}",
            "ms" => $"{Math.Min(maxDuration.Milliseconds, (duration.Milliseconds + 10)):000}",
            _ => $"UNK"
        };

        float CurrentValBigSize;
        using (UiFontService.UidFont.Push())
        {
            CurrentValBigSize = ImGui.CalcTextSize(currentValue).X;
        }
        var offset = (CurrentValBigSize - ImGui.CalcTextSize(prevValue).X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextDisabled(prevValue); // Previous value (centered)
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5f);
        CkGui.BigText(currentValue);

        // adjust the value with the mouse wheel
        if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
        {
            var hours = duration.Hours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;
            var milliseconds = duration.Milliseconds;

            var delta = -(int)ImGui.GetIO().MouseWheel;
            if (suffix == "h") { hours += delta; }
            if (suffix == "m") { minutes += delta; }
            if (suffix == "s") { seconds += delta; }
            if (suffix == "ms") { milliseconds += delta * 10; }
            // Rollover and clamp logic
            if (milliseconds < 0) { milliseconds += 1000; seconds--; }
            if (milliseconds > 999) { milliseconds -= 1000; seconds++; }
            if (seconds < 0) { seconds += 60; minutes--; }
            if (seconds > 59) { seconds -= 60; minutes++; }
            if (minutes < 0) { minutes += 60; hours--; }
            if (minutes > 59) { minutes -= 60; hours++; }

            hours = Math.Clamp(hours, 0, maxDuration.Hours);
            minutes = Math.Clamp(minutes, 0, (hours == maxDuration.Hours ? maxDuration.Minutes : 59));
            seconds = Math.Clamp(seconds, 0, (minutes == (hours == maxDuration.Hours ? maxDuration.Minutes : 59) ? maxDuration.Seconds : 59));
            milliseconds = Math.Clamp(milliseconds, 0, (seconds == (minutes == (hours == maxDuration.Hours ? maxDuration.Minutes : 59) ? maxDuration.Seconds : 59) ? maxDuration.Milliseconds : 999));

            // update the duration
            duration = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        }
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5f);
        var offset2 = (CurrentValBigSize - ImGui.CalcTextSize(prevValue).X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset2);
        ImGui.TextDisabled(nextValue); // Previous value (centered)
    }
    private static int GetColumnCountFromFormat(string format)
    {
        var columnCount = 0;
        if (format.Contains("hh")) columnCount++;
        if (format.Contains("mm")) columnCount++;
        if (format.Contains("ss")) columnCount++;
        if (format.Contains("fff")) columnCount++;
        return columnCount;
    }
}
