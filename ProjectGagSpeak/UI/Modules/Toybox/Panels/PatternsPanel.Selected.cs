using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Toybox;

// REMOVE THIS AND MIGRATE IT OVER TO THE MAIN PANEL. (or just functionalize it here)
public partial class PatternsPanel
{
    private static IconCheckboxEx LoopCheckbox = new(FAI.Sync, CkColor.LushPinkButton.Uint(), CkColor.IconCheckOff.Uint());

    private void DrawLabel(Pattern pattern, bool isEditing)
    {
        CkGui.ColorTextFrameAligned("Name", ImGuiColors.ParsedGold);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var c = CkRaii.ChildPaddedW("PatternName", ImGui.GetContentRegionAvail().X * .6f, ImGui.GetFrameHeight(),
            CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll);

        if (isEditing)
        {
            var refName = pattern.Label;
            ImGui.SetNextItemWidth(c.InnerRegion.X);
            if(ImGui.InputTextWithHint("##PatternName", "Name Here...", ref refName, 50))
                pattern.Label = refName;
        }
        else
        {
            ImUtf8.TextFrameAligned(pattern.Label);
        }
    }

    private void DrawDescription(Pattern pattern, bool isEditing)
    {
        CkGui.ColorTextFrameAligned("Description", ImGuiColors.ParsedGold);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var _ = CkRaii.ChildPaddedW("Description", ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 3,
            CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll);
        
        // Display the correct text field based on the editing state.
        if(isEditing)
        {
            var description = pattern.Description;
            if (ImGui.InputTextMultiline("##DescriptionField", ref description, 200, ImGui.GetContentRegionAvail()))
                pattern.Description = description;
        }
        else
            ImGui.TextWrapped(pattern.Description);

        // Draw a hint if no text is present.
        if (pattern.Description.IsNullOrWhitespace())
            ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding,
                0xFFBBBBBB,"Input a description in the space provided...");
    }

    private void DrawDurationLength(Pattern pattern, bool isEditing)
    {
        CkGui.ColorTextFrameAligned("Duration", ImGuiColors.ParsedGold);
        using (CkRaii.ChildPaddedW("Duration", ImGui.GetContentRegionAvail().X * .25f, ImGui.GetTextLineHeight(),
            CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll))
        {
            ImUtf8.Text(pattern.Duration.ToString(pattern.Duration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss"));
        }

        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint(), ImGui.GetStyle().FrameRounding, ImDrawFlags.RoundCornersAll))
            {
                if(isEditing)
                {
                    if (LoopCheckbox.Draw("##", pattern.ShouldLoop, out var newVal))
                        pattern.ShouldLoop = newVal;
                }
                else
                {
                    CkGui.FramedIconText(FAI.Sync, pattern.ShouldLoop ? CkColor.LushPinkButton.Uint() : CkColor.IconCheckOff.Uint());
                }
            }
            CkGui.TextFrameAlignedInline("Loop");
        }
        CkGui.AttachToolTip($"This pattern {(pattern.ShouldLoop ? "will loop upon finishing." : "will stop after reaching the end.")}");


    }

    private void DrawPatternTimeSpans(Pattern pattern, bool isEditing)
    {
        var refStartPoint = pattern.StartPoint;
        var refPlaybackDuration = pattern.PlaybackDuration;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        // Split things up into 2 columns.
        var columnWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        var height = CkGuiUtils.GetTimeDisplayHeight() + ImGui.GetFrameHeightWithSpacing();
        using var group = ImRaii.Group();

        // First child. (left, startpoint)
        using (var c = CkRaii.ChildPaddedW("PatternStartPoint", columnWidth, height, CkColor.FancyHeaderContrast.Uint(),
            CkRaii.GetChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
        {
            CkGui.ColorTextCentered("Start Point", ImGuiColors.ParsedGold);
            var format = pattern.Duration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
            if (isEditing)
            {
                CkGuiUtils.TimeSpanEditor("StartPnt", pattern.Duration, ref refStartPoint, format, c.InnerRegion.X);
                pattern.StartPoint = refStartPoint;
            }
            else
            {
                CkGuiUtils.TimeSpanPreview("StartPnt", pattern.Duration, refStartPoint, format, c.InnerRegion.X);
            }
        }

        // get time difference and apply the changes.
        if (pattern.StartPoint > pattern.Duration)
            pattern.StartPoint = pattern.Duration;

        // set the maximum possible playback duration allowed.
        var maxPlaybackDuration = pattern.Duration - pattern.StartPoint;

        // If the playback duration is greater than the max, set it to the max.
        if (pattern.PlaybackDuration > maxPlaybackDuration)
            pattern.PlaybackDuration = maxPlaybackDuration;

        // Shift to next column and display the pattern playback child.
        ImGui.SameLine();
        using (var c = CkRaii.ChildPaddedW("PlaybackDur", columnWidth, height, CkColor.FancyHeaderContrast.Uint(),
            CkRaii.GetChildRoundingLarge(), ImDrawFlags.RoundCornersAll))
        {
            CkGui.ColorTextCentered("Playback Duration", ImGuiColors.ParsedGold);
            var format2 = refPlaybackDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
            if (isEditing)
            {
                CkGuiUtils.TimeSpanEditor("PlaybackDur", maxPlaybackDuration, ref refPlaybackDuration, format2, c.InnerRegion.X);
                pattern.PlaybackDuration = refPlaybackDuration;
            }
            else
            {
                CkGuiUtils.TimeSpanPreview("PlaybackDur", maxPlaybackDuration, refPlaybackDuration, format2, c.InnerRegion.X);
            }
        }
    }

    private void DrawFooter(Pattern pattern)
    {
        ImUtf8.TextFrameAligned("ID:");
        CkGui.ColorTextFrameAlignedInline(pattern.Identifier.ToString(), ImGuiColors.DalamudGrey3);
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
