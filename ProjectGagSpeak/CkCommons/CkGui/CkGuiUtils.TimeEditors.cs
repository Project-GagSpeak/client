using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Component.Text;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Raii;
using System.Linq;
using System.Text.RegularExpressions;

namespace GagSpeak.CkCommons.Gui.Utility;
public static partial class CkGuiUtils
{
    public static float GetDateTimeDisplayWidth(DateTimeOffset time)
    {
        using var font = UiFontService.UidFont.Push();
        // 5 spacings accounts for 4 spacings on sides of '00', for two '00' and 1 for the ':'
        return GetBigTimeUnit(4).X + ImGui.GetStyle().ItemSpacing.X * 5;
    }

    public static Vector2 GetBigTimeUnit(int digits, char? c = null)
    {
        using var font = UiFontService.UidFont.Push();
        return c is null
            ? ImGui.CalcTextSize(new string('0', digits))
            : ImGui.CalcTextSize(new string('0', digits) + c);
    }

    public static float GetTimeSpanDisplayWidth(TimeSpan maxTime, string format)
    {
        using var font = UiFontService.UidFont.Push();
        var extraPadding = ImGui.GetStyle().ItemSpacing.X;

        // Define the regex pattern to match any of the time units (hh, mm, ss, fff)
        var regex = new Regex(@"hh|mm|ss|fff");
        var matches = regex.Matches(format);

        // Concatenate all time parts into a single string
        string timeString = string.Concat(matches
            .Select(match => match.Value switch
            {
                "hh" => $"{maxTime.Hours:00}h",
                "mm" => $"{maxTime.Minutes:00}m",
                "ss" => $"{maxTime.Seconds:00}s",
                "fff" => $"{maxTime.Milliseconds:000}ms",
                _ => string.Empty
            }));

        // Calculate the total width of the concatenated string with padding
        return ImGui.CalcTextSize(timeString).X + (extraPadding * matches.Count) - extraPadding;
    }

    public static float GetTimeDisplayHeight()
    {
        var smallH = ImGui.GetTextLineHeightWithSpacing() * 2;
        using var font = UiFontService.UidFont.Push();
        var bigH = ImGui.GetTextLineHeight();
        return smallH + bigH;
    }

    // Draws a datetime editor that accepts a UTC time, and displays in local.
    public static void DateTimePreviewUtcAsLocal(string id, DateTimeOffset time, float? displayWidth = null)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero);

        var fullWidth = displayWidth ?? GetDateTimeDisplayWidth(time);
        var localTime = time.ToLocalTime();
        float hourSize;
        float splitSize;
        float minuteSize;
        using (UiFontService.UidFont.Push())
        {
            hourSize = ImGui.CalcTextSize($"{localTime.Hour:00}").X;
            splitSize = ImGui.CalcTextSize(":").X;
            minuteSize = ImGui.CalcTextSize($"{localTime.Minute:00}").X;
        }

        // Get the remaining width to be used for splitting.
        var spacingWidth = fullWidth - (hourSize + minuteSize + splitSize);
        var individualSpacing = spacingWidth / 2;

        using (ImRaii.Table($"DateTimePreview_{id}", 3, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX))
        {
            ImGui.TableSetupColumn("Hour", ImGuiTableColumnFlags.WidthFixed, hourSize + individualSpacing);
            ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, splitSize);
            ImGui.TableSetupColumn("Minute", ImGuiTableColumnFlags.WidthFixed, minuteSize + individualSpacing);

            // Hours
            float cursorY = 0;
            ImGui.TableNextColumn();
            CkGui.ColorTextCentered($"{(localTime.Hour - 1 + 24) % 24:00}", ImGuiColors.DalamudGrey);
            cursorY = ImGui.GetCursorPosY();
            CkGui.FontTextCentered($"{localTime.Hour:00}", UiFontService.UidFont);
            CkGui.ColorTextCentered($"{(localTime.Hour + 1) % 24:00}", ImGuiColors.DalamudGrey);

            // Divider
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(cursorY);
            CkGui.FontTextCentered(":", UiFontService.UidFont);

            // Minutes
            ImGui.TableNextColumn();
            CkGui.ColorTextCentered($"{(localTime.Minute - 1 + 60) % 60:00}", ImGuiColors.DalamudGrey);
            CkGui.FontTextCentered($"{localTime.Minute:00}", UiFontService.UidFont);
            CkGui.ColorTextCentered($"{(localTime.Minute + 1) % 60:00}", ImGuiColors.DalamudGrey);
        }
    }

    public static void DateTimeEditorUtcAsLocal(string id, ref DateTimeOffset time, float? displayWidth = null)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero);

        var fullWidth = displayWidth ?? GetDateTimeDisplayWidth(time);
        var localTime = time.ToLocalTime();
        float hourSize;
        float splitSize;
        float minuteSize;
        using (UiFontService.UidFont.Push())
        {
            hourSize = ImGui.CalcTextSize($"{localTime.Hour:00}").X;
            splitSize = ImGui.CalcTextSize(":").X;
            minuteSize = ImGui.CalcTextSize($"{localTime.Minute:00}").X;
        }

        // Get the remaining width to be used for splitting.
        var spacingWidth = fullWidth - (hourSize + minuteSize + splitSize);
        var individualSpacing = spacingWidth / 2;

        using (ImRaii.Table($"DateTimeEditor_{id}", 3, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX))
        {
            ImGui.TableSetupColumn("Hour", ImGuiTableColumnFlags.WidthFixed, hourSize + individualSpacing);
            ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, splitSize);
            ImGui.TableSetupColumn("Minute", ImGuiTableColumnFlags.WidthFixed, minuteSize + individualSpacing);

            // Hours
            float cursorY = 0;
            ImGui.TableNextColumn();
            CkGui.ColorTextCentered($"{(localTime.Hour - 1 + 24) % 24:00}", ImGuiColors.DalamudGrey);
            cursorY = ImGui.GetCursorPosY();
            CkGui.FontTextCentered($"{localTime.Hour:00}", UiFontService.UidFont);
            if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
            {
                var newHour = (time.Hour - (int)ImGui.GetIO().MouseWheel + 24) % 24;
                var newLocalTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, newHour, localTime.Minute, 0);
                time = new DateTimeOffset(newLocalTime, TimeZoneInfo.Local.GetUtcOffset(newLocalTime)).ToUniversalTime();
            }
            CkGui.ColorTextCentered($"{(localTime.Hour + 1) % 24:00}", ImGuiColors.DalamudGrey);

            // Divider
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(cursorY);
            CkGui.FontTextCentered(":", UiFontService.UidFont);

            // Minutes.
            ImGui.TableNextColumn();
            CkGui.ColorTextCentered($"{(localTime.Minute - 1 + 60) % 60:00}", ImGuiColors.DalamudGrey);
            CkGui.FontTextCentered($"{localTime.Minute:00}", UiFontService.UidFont);
            if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
            {
                var newMinute = (time.Minute - (int)ImGui.GetIO().MouseWheel + 60) % 60;
                var newLocalTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, newMinute, 0);
                time = new DateTimeOffset(newLocalTime, TimeZoneInfo.Local.GetUtcOffset(newLocalTime)).ToUniversalTime();
            }
            CkGui.ColorTextCentered($"{(localTime.Minute + 1) % 60:00}", ImGuiColors.DalamudGrey);
        }
    }

    // Draws a datetime editor that accepts a UTC time, and displays in local.
    public static void TimeSpanPreview(string id, TimeSpan maxTime, TimeSpan timeRef, string format, float? displayWidth = null)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));

        var fullWidth = displayWidth ?? GetTimeSpanDisplayWidth(maxTime, format);
        float h, m, s, ms = 0;

        // get the regex for the format so we know how many columns to make and sizes to calculate.
        var regex = new Regex(@"hh|mm|ss|fff");
        var matches = regex.Matches(format).Select(m => m.Value).ToArray();
        var totalColumns = matches.Length;

        // Calculate the sizes for each time unit based on the format.
        using (UiFontService.UidFont.Push())
        {
            h = matches.Contains("hh") ? ImGui.CalcTextSize("0h").X : 0;
            m = matches.Contains("mm") ? ImGui.CalcTextSize("00m").X : 0;
            s = matches.Contains("ss") ? ImGui.CalcTextSize("00s").X : 0;
            ms = matches.Contains("fff") ? ImGui.CalcTextSize("000ms").X : 0;
        }

        // Get the remaining width to be used for splitting.
        var spacingWidth = fullWidth - (h + m + s + ms);
        var individualSpacing = spacingWidth / 2;

        using (ImRaii.Table($"DateTimePreview_{id}", totalColumns + 2, ImGuiTableFlags.NoPadOuterX))
        {
            ImGui.TableSetupColumn("SpaceLeft", ImGuiTableColumnFlags.WidthFixed, individualSpacing);
            foreach (var match in matches)
                switch (match)
                {
                    case "hh":
                        ImGui.TableSetupColumn("Hour", ImGuiTableColumnFlags.WidthFixed, h);
                        break;
                    case "mm":
                        ImGui.TableSetupColumn("Minute", ImGuiTableColumnFlags.WidthFixed, m);
                        break;
                    case "ss":
                        ImGui.TableSetupColumn("Second", ImGuiTableColumnFlags.WidthFixed, s);
                        break;
                    case "fff":
                        ImGui.TableSetupColumn("Millisecond", ImGuiTableColumnFlags.WidthFixed, ms);
                        break;
                }
            ImGui.TableSetupColumn("SpaceRight", ImGuiTableColumnFlags.WidthFixed, individualSpacing);

            // Left Spacing
            ImGui.TableNextColumn();

            // Draw the components based on the matches
            foreach (var match in matches)
            {
                ImGui.TableNextColumn();
                var (prev, cur, next) = GetDisplayTriplet(timeRef, maxTime, match);
                CkGui.ColorTextCentered(prev, ImGuiColors.DalamudGrey);
                CkGui.FontTextCentered(cur, UiFontService.UidFont);
                CkGui.ColorTextCentered(next, ImGuiColors.DalamudGrey);
            }

            // Right Spacing
            ImGui.TableNextColumn();
        }
    }

    public static void TimeSpanEditor(string id, TimeSpan maxTime, ref TimeSpan timeRef, string format, float? displayWidth = null)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X, 0));

        var fullWidth = displayWidth ?? GetTimeSpanDisplayWidth(maxTime, format);
        float h, m, s, ms = 0;

        // get the regex for the format so we know how many columns to make and sizes to calculate.
        var regex = new Regex(@"hh|mm|ss|fff");
        var matches = regex.Matches(format).Select(m => m.Value).ToArray();
        var totalColumns = matches.Length;

        // Calculate the sizes for each time unit based on the format.
        using (UiFontService.UidFont.Push())
        {
            h = matches.Contains("hh") ? ImGui.CalcTextSize("0h").X : 0;
            m = matches.Contains("mm") ? ImGui.CalcTextSize("00m").X : 0;
            s = matches.Contains("ss") ? ImGui.CalcTextSize("00s").X : 0;
            ms = matches.Contains("fff") ? ImGui.CalcTextSize("000ms").X : 0;
        }

        // Get the remaining width to be used for splitting.
        var spacingWidth = fullWidth - (h + m + s + ms);
        var individualSpacing = spacingWidth / 2;

        using (ImRaii.Table($"DateTimePreview_{id}", totalColumns + 2, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX))
        {
            ImGui.TableSetupColumn("SpaceLeft", ImGuiTableColumnFlags.WidthFixed, individualSpacing);
            foreach (var match in matches)
                switch (match)
                {
                    case "hh":
                        ImGui.TableSetupColumn("Hour", ImGuiTableColumnFlags.WidthFixed, h);
                        break;
                    case "mm":
                        ImGui.TableSetupColumn("Minute", ImGuiTableColumnFlags.WidthFixed, m);
                        break;
                    case "ss":
                        ImGui.TableSetupColumn("Second", ImGuiTableColumnFlags.WidthFixed, s);
                        break;
                    case "fff":
                        ImGui.TableSetupColumn("Millisecond", ImGuiTableColumnFlags.WidthFixed, ms);
                        break;
                }
            ImGui.TableSetupColumn("SpaceRight", ImGuiTableColumnFlags.WidthFixed, individualSpacing);

            // Left Spacing
            ImGui.TableNextColumn();

            // Draw the components based on the matches
            foreach (var match in matches)
            {
                ImGui.TableNextColumn();

                var (prev, cur, next) = GetDisplayTriplet(timeRef, maxTime, match);
                
                // Display the triplet.
                CkGui.ColorTextCentered(prev, ImGuiColors.DalamudGrey);
                
                float cursorY = ImGui.GetCursorPosY();
                CkGui.FontTextCentered(cur, UiFontService.UidFont);
                // Handle scrollwheel
                AdjustTimeSpan(ref timeRef, maxTime, match);

                CkGui.ColorTextCentered(next, ImGuiColors.DalamudGrey);
            }

            // Right Spacing
            ImGui.TableNextColumn();
        }
    }

    public static void TimeSpanEditor(string id, TimeSpan maxTime, ref TimeSpan timeRef, float? displayWidth = null)
    {
        /*using var s = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero);

        var fullWidth = displayWidth ?? GetTimeSpanDisplayWidth(maxTime, timeRef);
        float hourSize;
        float minuteSize;
        float secondSize;
        using (UiFontService.UidFont.Push())
        {
            hourSize = ImGui.CalcTextSize($"{timeRef.Hours:00}h").X;
            minuteSize = ImGui.CalcTextSize($"{timeRef.Minutes:00}m").X;
            secondSize = ImGui.CalcTextSize($"{timeRef.Seconds:00}s").X;
        }

        // Get the remaining width to be used for splitting.
        var spacingWidth = fullWidth - (hourSize + minuteSize + secondSize);
        var individualSpacing = spacingWidth / 2;

        using (ImRaii.Table($"DateTimePreview_{id}", 5, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX))
        {
            ImGui.TableSetupColumn("SpaceLeft", ImGuiTableColumnFlags.WidthFixed, individualSpacing);
            ImGui.TableSetupColumn("Hour", ImGuiTableColumnFlags.WidthFixed, hourSize);
            ImGui.TableSetupColumn("Minute", ImGuiTableColumnFlags.WidthFixed, minuteSize);
            ImGui.TableSetupColumn("Second", ImGuiTableColumnFlags.WidthFixed, secondSize);
            ImGui.TableSetupColumn("SpaceRight", ImGuiTableColumnFlags.WidthFixed, individualSpacing);

            // Left Spacing
            ImGui.TableNextColumn();

            // Hours
            float cursorY = 0;
            ImGui.TableNextColumn();
            CkGui.ColorTextCentered($"{(timeRef.Hours - 1 + 24) % 9:0}", ImGuiColors.DalamudGrey);
            cursorY = ImGui.GetCursorPosY();
            CkGui.FontTextCentered($"{timeRef.Hours:0}h", UiFontService.UidFont);
            if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
            {
                var newHour = (time.Hour - (int)ImGui.GetIO().MouseWheel + 24) % 24;
                var newLocalTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, newHour, localTime.Minute, 0);
                time = new DateTimeOffset(newLocalTime, TimeZoneInfo.Local.GetUtcOffset(newLocalTime)).ToUniversalTime();
            }
            CkGui.ColorTextCentered($"{(timeRef.Hours + 1) % 9:0}", ImGuiColors.DalamudGrey);

            // Minutes.
            ImGui.TableNextColumn();
            CkGui.ColorTextCentered($"{(localTime.Minute - 1 + 60) % 60:00}", ImGuiColors.DalamudGrey);
            CkGui.FontTextCentered($"{localTime.Minute:00}", UiFontService.UidFont);
            if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel != 0)
            {
                var newMinute = (time.Minute - (int)ImGui.GetIO().MouseWheel + 60) % 60;
                var newLocalTime = new DateTime(localTime.Year, localTime.Month, localTime.Day, localTime.Hour, newMinute, 0);
                time = new DateTimeOffset(newLocalTime, TimeZoneInfo.Local.GetUtcOffset(newLocalTime)).ToUniversalTime();
            }
            CkGui.ColorTextCentered($"{(localTime.Minute + 1) % 60:00}", ImGuiColors.DalamudGrey);
        }*/
    }

    private static (string prev, string cur, string next) GetDisplayTriplet(TimeSpan duration, TimeSpan maxTime, string suffix)
    {
        var prevValue = suffix switch
        {
            "hh" => $"{Math.Max(0, (duration.Hours - 1)):00}",
            "mm" => $"{Math.Max(0, (duration.Minutes - 1)):00}",
            "ss" => $"{Math.Max(0, (duration.Seconds - 1)):00}",
            "fff" => $"{Math.Max(0, (duration.Milliseconds - 10)):000}",
            _ => $"UNK"
        };

        var currentValue = suffix switch
        {
            "hh" => $"{duration.Hours:00}h",
            "mm" => $"{duration.Minutes:00}m",
            "ss" => $"{duration.Seconds:00}s",
            "fff" => $"{duration.Milliseconds:000}ms",
            _ => $"UNK"
        };

        var nextValue = suffix switch
        {
            "hh" => $"{Math.Min(maxTime.Hours, (duration.Hours + 1)):00}",
            "mm" => duration.Hours == maxTime.Hours
                ? $"{Math.Min(maxTime.Minutes, duration.Minutes + 1):00}"
                : $"{duration.Minutes + 1:00}",
            "ss" => duration.Minutes == maxTime.Minutes
                ? $"{Math.Min(maxTime.Seconds, duration.Seconds + 1):00}"
                : $"{duration.Seconds + 1:00}",
            "fff" => duration.Seconds == maxTime.Seconds
                ? $"{Math.Min(maxTime.Milliseconds, duration.Milliseconds + 10):000}"
                : $"{duration.Milliseconds + 10:000}",
            _ => $"UNK"
        };
        return (prevValue, currentValue, nextValue);
    }

    private static void AdjustTimeSpan(ref TimeSpan timeRef, TimeSpan maxDuration, string suffix)
    {
        if (!ImGui.IsItemHovered() || ImGui.GetIO().MouseWheel == 0)
            return;

        int delta = -(int)ImGui.GetIO().MouseWheel;

        int h = timeRef.Hours;
        int m = timeRef.Minutes;
        int s = timeRef.Seconds;
        int ms = timeRef.Milliseconds;

        switch (suffix)
        {
            case "hh": h += delta; break;
            case "mm": m += delta; break;
            case "ss": s += delta; break;
            case "fff": ms += delta * 10; break;
        }

        // Normalize up
        if (ms > 999) { s += ms / 1000; ms %= 1000; }
        if (s > 59) { m += s / 60; s %= 60; }
        if (m > 59) { h += m / 60; m %= 60; }

        // Normalize down
        if (ms < 0) { s += (ms / 1000) - 1; ms = 1000 + (ms % 1000); }
        if (s < 0) { m += (s / 60) - 1; s = 60 + (s % 60); }
        if (m < 0) { h += (m / 60) - 1; m = 60 + (m % 60); }

        // Build & clamp
        var adjusted = new TimeSpan(0, h, m, s, ms);
        if (adjusted < TimeSpan.Zero) adjusted = TimeSpan.Zero;
        if (adjusted > maxDuration) adjusted = maxDuration;

        timeRef = adjusted;
    }
}
