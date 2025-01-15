using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.UiRemote;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Text;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;

namespace GagSpeak.UI.UiToybox;

public class ToyboxPatterns
{
    private readonly ILogger<ToyboxPatterns> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly KinkPlateService _kinkPlates;
    private readonly UiSharedService _uiShared;
    private readonly PatternHandler _handler;
    private readonly ShareHubService _shareHub;
    private readonly TutorialService _guides;
    public ToyboxPatterns(ILogger<ToyboxPatterns> logger, GagspeakMediator mediator,
        KinkPlateService kinkPlates, UiSharedService uiSharedService, PatternHandler patternHandler, 
        ShareHubService shareHubService, TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _kinkPlates = kinkPlates;
        _uiShared = uiSharedService;
        _handler = patternHandler;
        _shareHub = shareHubService;
        _guides = guides;
    }

    // -1 indicates no item is currently hovered
    private int LastHoveredIndex = -1;
    private LowerString PatternSearchString = LowerString.Empty;
    private List<PatternData> FilteredPatternsList
        => _handler.Patterns
            .Where(pattern => pattern.Name.Contains(PatternSearchString, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public void DrawPatternManagerPanel()
    {
        var regionSize = ImGui.GetContentRegionAvail();

        // if we are simply viewing the main page, display list of patterns  
        if (_handler.ClonedPatternForEdit is null)
        {
            DrawCreatePatternHeader();
            ImGui.Separator();
            DrawSearchFilter(regionSize.X, ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.Separator();
            if (_handler.PatternCount > 0)
                DrawPatternSelectableMenu();
            return;
        }

        // if we are editing an pattern
        if (_handler.ClonedPatternForEdit is not null)
        {
            DrawPatternEditorHeader();
            ImGui.Separator();
            if (_handler.PatternCount > 0 && _handler.ClonedPatternForEdit is not null)
                DrawPatternEditor(_handler.ClonedPatternForEdit);
        }
    }

    private void DrawCreatePatternHeader()
    {
        // use button wrounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize("New Pattern");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("CreatePatternHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2)))
        {
            // now calculate it so that the cursors Y position centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            // draw out the icon button
            if (_uiShared.IconButton(FontAwesomeIcon.Plus))
            {
                // Open the lovense remote in PATTERN mode
                _mediator.Publish(new UiToggleMessage(typeof(RemotePatternMaker)));
            }
            UiSharedService.AttachToolTip("Click me begin creating a new Pattern!");
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.CreatingNewPatterns, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, 
                () => _mediator.Publish(new UiToggleMessage(typeof(RemotePatternMaker))));


            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            _uiShared.BigText("New Pattern");
        }
    }

    private void DrawPatternEditorHeader()
    {
        // use button rounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var iconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Plus);
        Vector2 textSize;
        using (_uiShared.UidFont.Push())
        {
            textSize = ImGui.CalcTextSize($"Editor");
        }
        var centerYpos = (textSize.Y - iconSize.Y);
        using (ImRaii.Child("EditPatternHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), iconSize.Y + (centerYpos - startYpos) * 2), false, ImGuiWindowFlags.NoScrollbar))
        {
            if(_handler.ClonedPatternForEdit is null) return;

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);

            // the "fuck go back" button.
            if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft))
            {
                _handler.CancelEditingPattern();
                return;
            }
            UiSharedService.AttachToolTip("Discard Pattern Changes & Return to Pattern List");
            // now next to it we need to draw the header text
            ImGui.SameLine(10 * ImGuiHelpers.GlobalScale + iconSize.X + ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText("Editor", ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - iconSize.X * 2 - ImGui.GetStyle().ItemSpacing.X * 2);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();

            if (_uiShared.IconButton(FontAwesomeIcon.Save))
                _handler.SaveEditedPattern();
            UiSharedService.AttachToolTip("Save changes to Pattern & Return to Pattern List");
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.ApplyChanges, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

            // right beside it to the right, we need to draw the delete button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconButton(FontAwesomeIcon.Trash, disabled: !KeyMonitor.ShiftPressed()))
                _handler.RemovePattern(_handler.ClonedPatternForEdit.UniqueIdentifier);
            UiSharedService.AttachToolTip("Delete this Pattern--SEP--Must hold SHIFT while clicking to delete");
        }
    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - spacingX);
        string filter = PatternSearchString;
        if (ImGui.InputTextWithHint("##PatternSearchStringFilter", "Search for a Pattern", ref filter, 255))
        {
            PatternSearchString = filter;
            LastHoveredIndex = -1;
        }
        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(PatternSearchString));
        if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
        {
            PatternSearchString = string.Empty;
            LastHoveredIndex = -1;
        }
    }

    private void DrawPatternSelectableMenu()
    {
        var region = ImGui.GetContentRegionAvail();
        var topLeftSideHeight = region.Y;
        bool anyItemHovered = false;

        using (var rightChild = ImRaii.Child($"###PatternListPreview", region with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
        {
            try
            {
                for (int i = 0; i < FilteredPatternsList.Count; i++)
                {
                    var set = FilteredPatternsList[i];
                    DrawPatternSelectable(set, i);

                    if (ImGui.IsItemHovered())
                    {
                        anyItemHovered = true;
                        LastHoveredIndex = i;
                    }

                    // if the item is right clicked, open the popup
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        if (LastHoveredIndex == i && !FilteredPatternsList[i].IsActive)
                            ImGui.OpenPopup($"PatternDataContext{i}");
                    }
                }

                bool isPopupOpen = LastHoveredIndex != -1 && ImGui.IsPopupOpen($"PatternDataContext{LastHoveredIndex}");

                if (LastHoveredIndex != -1 && LastHoveredIndex < FilteredPatternsList.Count)
                    HandlePopupMenu();

                // if no item is hovered, reset the last hovered index
                if (!anyItemHovered && !isPopupOpen) LastHoveredIndex = -1;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error: " + ex);
                _logger.LogError("Values at time of Error: " + LastHoveredIndex + " " + FilteredPatternsList.Count);
            }
        }
        _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.ModifyingPatterns, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
    }

    private void HandlePopupMenu()
    {
        if (ImGui.BeginPopup("PatternDataContext"+LastHoveredIndex))
        {
            // perform early returns to avoid crashes.
            if (ImGui.Selectable("Delete Pattern"))
            {
                _handler.RemovePattern(FilteredPatternsList[LastHoveredIndex].UniqueIdentifier);
            }
            ImGui.EndPopup();
        }
    }

    private void DrawPatternSelectable(PatternData pattern, int idx)
    {
        // fetch the name of the pattern, and its text size
        var name = pattern.Name;
        Vector2 tmpAlarmTextSize;
        using (_uiShared.UidFont.Push()) { tmpAlarmTextSize = ImGui.CalcTextSize(name); }

        // fetch the duration of the pattern
        var durationTxt = pattern.Duration.Hours > 0 ? pattern.Duration.ToString("hh\\:mm\\:ss") : pattern.Duration.ToString("mm\\:ss");
        var startpointTxt = pattern.StartPoint.Hours > 0 ? pattern.StartPoint.ToString("hh\\:mm\\:ss") : pattern.StartPoint.ToString("mm\\:ss");
        var loopIconSize = _uiShared.GetIconData(FontAwesomeIcon.Repeat);

        // Get Style sizes
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var patternToggleButton = pattern.IsActive ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play;
        var patternToggleButtonSize = _uiShared.GetIconButtonSize(pattern.IsActive ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play);

        // create the selectable
        float height = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
        using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), LastHoveredIndex == idx);
        using (ImRaii.Child($"##PatternSelectable{pattern.UniqueIdentifier}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {
            using (var group = ImRaii.Group())
            {
                // get sizes for the likes and downloads
                var playbackSize = _uiShared.GetIconTextButtonSize(pattern.IsActive ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play, pattern.IsActive ? "Stop" : "Play");
                var trashbinSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Trash).X;

                // display name, then display the downloads and likes on the other side.
                _uiShared.GagspeakText(pattern.Name);
                _uiShared.DrawHelpText("Description:--SEP--"+pattern.Description);

                // playback button
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X - playbackSize - trashbinSize);
                using (ImRaii.PushColor(ImGuiCol.Text, pattern.IsActive ? ImGuiColors.DalamudRed : ImGuiColors.HealerGreen))
                {
                    if (_uiShared.IconTextButton(pattern.IsActive ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play, pattern.IsActive ? "Stop" : "Play", isInPopup: true))
                    {
                        if (pattern.IsActive) _handler.DisablePattern(pattern);
                        else _handler.EnablePattern(pattern);
                    }
                }
                UiSharedService.AttachToolTip(pattern.IsActive ? "Stop the current pattern." : "Play this pattern.");

                // Draw the delete button
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite2))
                {
                    if (_uiShared.IconButton(FontAwesomeIcon.TrashAlt, disabled: !KeyMonitor.ShiftPressed(), inPopup: true))
                        _handler.RemovePattern(pattern.UniqueIdentifier);
                }
                UiSharedService.AttachToolTip("Remove this pattern!");
            }
            // next line:
            using (var group2 = ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.Clock);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(durationTxt, ImGuiColors.DalamudGrey);
                UiSharedService.AttachToolTip("Total Length of the Pattern.");

                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.Stopwatch20);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(startpointTxt, ImGuiColors.DalamudGrey);
                UiSharedService.AttachToolTip("Start Point of the Pattern.");

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - _uiShared.GetIconData(FontAwesomeIcon.Sync).X - ImGui.GetStyle().ItemInnerSpacing.X);
                _uiShared.IconText(FontAwesomeIcon.Sync, pattern.ShouldLoop ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey2);
                UiSharedService.AttachToolTip(pattern.ShouldLoop ? "Pattern is set to loop." : "Pattern does not loop.");
            }
        }
        // if the item is clicked, set the editing pattern to this pattern
        if (ImGui.IsItemClicked()) _handler.StartEditingPattern(pattern);
    }

    private void DrawPatternEditor(PatternData pattern)
    {
        UiSharedService.ColorText("ID:", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        UiSharedService.ColorText(pattern.UniqueIdentifier.ToString(), ImGuiColors.DalamudGrey);

        UiSharedService.ColorText("Pattern Name", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        var refName = pattern.Name;
        if (ImGui.InputTextWithHint("##PatternName", "Name Here...", ref refName, 50))
            pattern.Name = refName;
        _uiShared.DrawHelpText("Define the name for the Pattern.");
        
        // description
        var refDescription = pattern.Description;
        UiSharedService.ColorText("Description", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        if (UiSharedService.InputTextWrapMultiline("##PatternDescription", ref refDescription, 100, 3, 225f))
            pattern.Description = refDescription;
        _uiShared.DrawHelpText("Define the description for the Pattern.\n(Shown on tooltip hover if uploaded)");

        // total duration
        ImGui.Spacing();
        UiSharedService.ColorText("Total Duration", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.TextUnformatted(pattern.Duration.Hours > 0 ? pattern.Duration.ToString("hh\\:mm\\:ss") : pattern.Duration.ToString("mm\\:ss"));

        // looping
        ImGui.Spacing();
        UiSharedService.ColorText("Pattern Loop State", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Repeat, pattern.ShouldLoop ? "Looping" : "Not Looping", null, true)) 
            pattern.ShouldLoop = !pattern.ShouldLoop;
        _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.EditLoopToggle, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        TimeSpan patternDurationTimeSpan = pattern.Duration;
        TimeSpan patternStartPointTimeSpan = pattern.StartPoint;
        TimeSpan patternPlaybackDuration = pattern.PlaybackDuration;

        // playback start point
        UiSharedService.ColorText("Pattern Start-Point Timestamp", ImGuiColors.ParsedGold);
        string formatStart = patternDurationTimeSpan.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        _uiShared.DrawTimeSpanCombo("PatternStartPointTimeCombo", patternDurationTimeSpan, ref patternStartPointTimeSpan, 150f, formatStart, false);
        pattern.StartPoint = patternStartPointTimeSpan;
        _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.EditStartPoint, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

        // time difference calculation.
        if (pattern.StartPoint > patternDurationTimeSpan) pattern.StartPoint = patternDurationTimeSpan;
        TimeSpan maxPlaybackDuration = patternDurationTimeSpan - pattern.StartPoint;

        // playback duration
        ImGui.Spacing();
        UiSharedService.ColorText("Pattern Playback Duration", ImGuiColors.ParsedGold);
        string formatDuration = patternPlaybackDuration.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
        _uiShared.DrawTimeSpanCombo("Pattern Playback Duration", maxPlaybackDuration, ref patternPlaybackDuration, 150f, formatDuration, false);
        pattern.PlaybackDuration = patternPlaybackDuration;
        _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.EditPlaybackDuration, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
    }

    private void SetFromClipboard()
    {
        try
        {
            // Get the JSON string from the clipboard
            string base64 = ImGui.GetClipboardText();
            // Deserialize the JSON string back to pattern data
            var bytes = Convert.FromBase64String(base64);
            // Decode the base64 string back to a regular string
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            // Deserialize the string back to pattern data
            PatternData pattern = JsonConvert.DeserializeObject<PatternData>(decompressed) ?? new PatternData();
            // Set the active pattern
            _logger.LogInformation("Set pattern data from clipboard");
            _handler.AddNewPattern(pattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not set pattern data from clipboard.{ex.Message}");
        }
    }


}
