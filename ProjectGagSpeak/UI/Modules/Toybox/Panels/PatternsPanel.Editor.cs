using Dalamud.Interface;
using Dalamud.Interface.Colors;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.PlayerState.Models;
using ImGuiNET;

namespace GagSpeak.UI.Toybox;

public partial class PatternsPanel
{
    // Placeholder data inside currently.
    private void DrawEditor(Vector2 region)
    {
        if(_manager.ActiveEditorItem is not { } pattern)
            return;

        CkGui.ColorText("ID:", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        CkGui.ColorText(pattern.Identifier.ToString(), ImGuiColors.DalamudGrey);

        CkGui.ColorText("Pattern Name", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        var refName = pattern.Label;
        if (ImGui.InputTextWithHint("##PatternName", "Name Here...", ref refName, 50))
            pattern.Label = refName;
        CkGui.DrawHelpText("Define the name for the Pattern.");

        // description
        var refDescription = pattern.Description;
        CkGui.ColorText("Description", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextMultiline("##PatternDescription", ref refDescription, 100, new Vector2(225f, ImGui.GetFrameHeightWithSpacing()*3)))
            pattern.Description = refDescription;
        CkGui.DrawHelpText("Define the description for the Pattern.\n(Shown on tooltip hover if uploaded)");

        // total duration
        ImGui.Spacing();
        CkGui.ColorText("Total Duration", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.TextUnformatted(pattern.Duration.Hours > 0 ? pattern.Duration.ToString("hh\\:mm\\:ss") : pattern.Duration.ToString("mm\\:ss"));

        // looping
        ImGui.Spacing();
        CkGui.ColorText("Pattern Loop State", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        if (CkGui.IconTextButton(FontAwesomeIcon.Repeat, pattern.ShouldLoop ? "Looping" : "Not Looping", null, true))
            pattern.ShouldLoop = !pattern.ShouldLoop;

        var patternDurationTimeSpan = pattern.Duration;
        var patternStartPointTimeSpan = pattern.StartPoint;
        var patternPlaybackDuration = pattern.PlaybackDuration;

        // playback start point
        CkGui.ColorText("Pattern Start-Point Timestamp", ImGuiColors.ParsedGold);
        var formatStart = patternDurationTimeSpan.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        CkGuiUtils.DrawTimeSpanCombo("PatternStartPointTimeCombo", patternDurationTimeSpan, ref patternStartPointTimeSpan, 150f, formatStart, false);
        pattern.StartPoint = patternStartPointTimeSpan;

        // time difference calculation.
        if (pattern.StartPoint > patternDurationTimeSpan) pattern.StartPoint = patternDurationTimeSpan;
        var maxPlaybackDuration = patternDurationTimeSpan - pattern.StartPoint;

        // playback duration
        ImGui.Spacing();
        CkGui.ColorText("Pattern Playback Duration", ImGuiColors.ParsedGold);
        var formatDuration = patternPlaybackDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        CkGuiUtils.DrawTimeSpanCombo("Pattern Playback Duration", maxPlaybackDuration, ref patternPlaybackDuration, 150f, formatDuration, false);
        pattern.PlaybackDuration = patternPlaybackDuration;
    }

    // Unused.
    private void SetFromClipboard()
    {
        try
        {
            // Get the JSON string from the clipboard
            var base64 = ImGui.GetClipboardText();
            // Deserialize the JSON string back to pattern data
            var bytes = Convert.FromBase64String(base64);
            // Decode the base64 string back to a regular string
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            // Deserialize the string back to pattern data
            var pattern = JsonConvert.DeserializeObject<Pattern>(decompressed) ?? new Pattern();
            // Set the active pattern
            _logger.LogInformation("Set pattern data from clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not set pattern data from clipboard.{ex.Message}");
        }
    }
}
