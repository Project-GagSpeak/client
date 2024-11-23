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

namespace GagSpeak.UI.UiToybox;

public class ToyboxPatterns
{
    private readonly ILogger<ToyboxPatterns> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly KinkPlateService _kinkPlates;
    private readonly UiSharedService _uiShared;
    private readonly PatternHandler _handler;
    private readonly PatternHubService _patternHubService;
    private readonly TutorialService _guides;
    public ToyboxPatterns(ILogger<ToyboxPatterns> logger, GagspeakMediator mediator,
        KinkPlateService kinkPlates, UiSharedService uiSharedService, PatternHandler patternHandler, 
        PatternHubService patternHubService, TutorialService guides)
    {
        _logger = logger;
        _mediator = mediator;
        _kinkPlates = kinkPlates;
        _uiShared = uiSharedService;
        _handler = patternHandler;
        _patternHubService = patternHubService;
        _guides = guides;
    }

    // Private accessor vars for list management.
    private int LastHoveredIndex = -1; // -1 indicates no item is currently hovered
    private string _nextTabToSelect = "Display Info";
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
        // get if social features are revoked to know if we should prevent uploads.
        var profile = _kinkPlates.GetKinkPlate(MainHub.PlayerUserData);
        var canPublish = !profile.KinkPlateInfo.Disabled;

        // use button rounding
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        FontAwesomeIcon publishOrTakedownIcon = _handler.ClonedPatternForEdit!.IsPublished ? FontAwesomeIcon.Ban : FontAwesomeIcon.Upload;
        string publishOrTakedownText = _handler.ClonedPatternForEdit!.IsPublished ? "Unpublish" : "Publish";
        var publishOrTakedownSize = _uiShared.GetIconTextButtonSize(publishOrTakedownIcon, publishOrTakedownText);
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
            var jumpWidth = _handler.ClonedPatternForEdit.CreatorUID == MainHub.UID
                ? ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - publishOrTakedownSize - iconSize.X * 2 - ImGui.GetStyle().ItemSpacing.X * 3
                : ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - iconSize.X * 2 - ImGui.GetStyle().ItemSpacing.X * 2;
            ImGui.SameLine(jumpWidth);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();

            // draw out the icon button
            if(_handler.ClonedPatternForEdit.CreatorUID == MainHub.UID)
            {
                var disabled = _handler.ClonedPatternForEdit.IsPublished is false && !canPublish;
                if (_uiShared.IconTextButton(publishOrTakedownIcon, publishOrTakedownText, null, false, disabled))
                {
                    ImGui.OpenPopup("UploadPopup");
                    var buttonPos = ImGui.GetItemRectMin();
                    var buttonSize = ImGui.GetItemRectSize();
                    ImGui.SetNextWindowPos(new Vector2(buttonPos.X, buttonPos.Y + buttonSize.Y));
                }
                UiSharedService.AttachToolTip(canPublish is false
                    ? "Cannot Publish Patterns, your social features have been revoked!" : _handler.ClonedPatternForEdit.IsPublished
                        ? "Remove Pattern from Server" : "Upload Pattern to Server");
                _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.PublishingToPatternHub, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

                // for saving contents
                ImGui.SameLine();
                ImGui.SetCursorPosY(currentYpos);
            }

            if (_uiShared.IconButton(FontAwesomeIcon.Save))
            {
                _handler.SaveEditedPattern();
            }
            UiSharedService.AttachToolTip("Save changes to Pattern & Return to Pattern List");
            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.ApplyChanges, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);

            // right beside it to the right, we need to draw the delete button
            ImGui.SameLine();
            ImGui.SetCursorPosY(currentYpos);
            if (_uiShared.IconButton(FontAwesomeIcon.Trash, disabled: !KeyMonitor.ShiftPressed()))
            {
                HandleDelete(_handler.ClonedPatternForEdit);
            }
            UiSharedService.AttachToolTip("Delete this Pattern--SEP--Must hold SHIFT while clicking to delete");

            try
            {
                if (ImGui.BeginPopup("UploadPopup"))
                {
                    ImGui.InvisibleButton("##DummyWidth", new Vector2(ImGui.CalcTextSize("Yes I'm Sure __ Fuck, Go Back.").X, 0));
                    string text = _handler.ClonedPatternForEdit.IsPublished ? "Remove Pattern from Server?" : "Upload Pattern to Server?";
                    ImGuiUtil.Center(text);
                    var width = (ImGui.GetContentRegionAvail().X / 2) - ImGui.GetStyle().ItemInnerSpacing.X;
                    if (ImGui.Button("Yes, I'm Sure", new Vector2(width, 25f)))
                    {
                        if (_handler.ClonedPatternForEdit.IsPublished)
                        {
                            _patternHubService.RemovePatternFromServer(_handler.ClonedPatternForEdit);
                        }
                        else
                        {
                            _patternHubService.UploadPatternToServer(_handler.ClonedPatternForEdit);
                        }
                        ImGui.CloseCurrentPopup();
                    }
                    ImUtf8.SameLineInner();
                    if (ImGui.Button("Fuck, go back", new Vector2(width, 25f)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
            }
            catch (Exception ex)
            {
                // prevent crashes.
                _logger.LogError(ex.ToString());
            }
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
            {
                // Begin the popup for the current item
                if (ImGui.BeginPopup($"PatternDataContext{LastHoveredIndex}"))
                {
                    if (ImGui.Selectable("Delete Pattern")) 
                        HandleDelete(FilteredPatternsList[LastHoveredIndex]);    
                    if (FilteredPatternsList[LastHoveredIndex].CreatorUID == MainHub.UID)
                    {
                        if (ImGui.Selectable("Unpublish & Delete Pattern"))
                            HandleDelete(FilteredPatternsList[LastHoveredIndex], true);
                    }
                    ImGui.EndPopup();
                }
            }

            // if no item is hovered, reset the last hovered index
            if (!anyItemHovered && !isPopupOpen) LastHoveredIndex = -1;
        }
        _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.ModifyingPatterns, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
    }

    private void HandleDelete(PatternData refPattern, bool unpublish = false)
    {
        if(refPattern.CreatorUID == MainHub.UID && unpublish)
        {
            _logger.LogInformation("Unpublishing the Pattern.");
            _patternHubService.RemovePatternFromServer(refPattern);
        }
        // remove it normally.
        _handler.RemovePattern(refPattern.UniqueIdentifier);
    }

    private void DrawPatternSelectable(PatternData pattern, int idx)
    {
        // fetch the name of the pattern, and its text size
        var name = pattern.Name;
        Vector2 tmpAlarmTextSize;
        using (_uiShared.UidFont.Push()) { tmpAlarmTextSize = ImGui.CalcTextSize(name); }

        var author = pattern.Author;
        var authorTextSize = ImGui.CalcTextSize(author);

        // fetch the duration of the pattern
        var duration = pattern.Duration.Hours > 0 ? pattern.Duration.ToString("hh\\:mm\\:ss") : pattern.Duration.ToString("mm\\:ss");
        var durationTextSize = ImGui.CalcTextSize(duration);

        // fetch the list of tags.
        var tags = pattern.Tags;

        // get loop icon size
        var loopIconSize = _uiShared.GetIconData(FontAwesomeIcon.Repeat);

        // Get Style sizes
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        var patternToggleButton = pattern.IsActive ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play;
        var patternToggleButtonSize = _uiShared.GetIconButtonSize(pattern.IsActive ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play);

        // create the selectable
        using var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), LastHoveredIndex == idx);
        using (ImRaii.Child($"##PatternSelectable{pattern.UniqueIdentifier}", new Vector2(UiSharedService.GetWindowContentRegionWidth(), 65f)))
        {
            // create a group for the bounding area
            using (var group = ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                // get Y pos
                var currentYpos = ImGui.GetCursorPosY();
                _uiShared.BigText(name);
                ImGui.SameLine();
                ImGui.SetCursorPosY(currentYpos + ((tmpAlarmTextSize.Y - loopIconSize.Y) / 1.5f));

                if (pattern.ShouldLoop)
                {
                    _uiShared.IconText(FontAwesomeIcon.Repeat, ImGuiColors.ParsedPink);
                }
                if (pattern.IsPublished)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((tmpAlarmTextSize.Y - loopIconSize.Y) / 1.5f));
                    _uiShared.IconText(FontAwesomeIcon.Globe, ImGuiColors.ParsedPink);
                }
            }

            using (var group = ImRaii.Group())
            {
                // scooch over a bit like 5f
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5f);
                UiSharedService.ColorText(author, ImGuiColors.DalamudGrey2);
                ImGui.SameLine();
                UiSharedService.ColorText("|  " + duration, ImGuiColors.DalamudGrey3);
                // Below this, we should draw out the tags in a row, with a max of 5 tags. If they extend the width of the window, stop drawing.
                var widthRef = ImGui.GetContentRegionAvail().X;
            }

            // now, head to the same line of the full width minus the width of the button
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - patternToggleButtonSize.X - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (65f - patternToggleButtonSize.Y) / 2);
            // draw out the icon button
            if (_uiShared.IconButton(patternToggleButton))
            {
                // set the enabled state of the pattern based on its current state so that we toggle it
                if (pattern.IsActive)
                    _handler.DisablePattern(pattern);
                else
                    _handler.EnablePattern(pattern);
                // toggle the state & early return so we dont access the childclicked button
                return;
            }
            UiSharedService.AttachToolTip("Play / Stop this Pattern");
        }
        // if the item is clicked, set the editing pattern to this pattern
        if (ImGui.IsItemClicked())
        {
            _handler.StartEditingPattern(pattern);
        }
    }

    private void DrawPatternEditor(PatternData patternToEdit)
    {
        if (ImGui.BeginTabBar("PatternEditorTabBar"))
        {
            var tabs = new Dictionary<string, Action>
            {
                { "Display Info", () => DrawDisplayInfo(patternToEdit) },
                { "Adjustments", () => DrawAdjustments(patternToEdit) }
            };

            foreach (var tab in tabs)
            {
                using (var open = ImRaii.TabItem(tab.Key, _nextTabToSelect == tab.Key ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    if (_nextTabToSelect == tab.Key) _nextTabToSelect = string.Empty;

                    switch (tab.Key)
                    {
                        case "Display Info":
                            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.EditDisplayInfo, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize);
                            break;
                        case "Adjustments":
                            _guides.OpenTutorial(TutorialType.Patterns, StepsPatterns.ToEditAdjustments, ToyboxUI.LastWinPos, ToyboxUI.LastWinSize, () => _nextTabToSelect = "Adjustments");
                            break;
                    }

                    if (open) tab.Value.Invoke();
                }
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawDisplayInfo(PatternData pattern)
    {
        // identifier
        UiSharedService.ColorText("Identifier", ImGuiColors.ParsedGold);
        UiSharedService.ColorText(pattern.UniqueIdentifier.ToString(), ImGuiColors.DalamudGrey);

        // name
        var refName = pattern.Name;
        UiSharedService.ColorText("Pattern Name", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint("##PatternName", "Name Here...", ref refName, 50))
        {
            pattern.Name = refName;
        }

        _uiShared.DrawHelpText("Define the name for the Pattern.");
        // author
        var refAuthor = pattern.Author;
        UiSharedService.ColorText("Author", ImGuiColors.ParsedGold);
        using (ImRaii.Disabled(pattern.CreatorUID != MainHub.UID))
        {
            ImGui.SetNextItemWidth(200f);
            if (ImGui.InputTextWithHint("##PatternAuthor", "Author Here...", ref refAuthor, 25))
            {
                pattern.Author = refAuthor;
            }
        }
        _uiShared.DrawHelpText("Define the author for the Pattern.\n(Shown as Publisher name if uploaded)");

        // description
        var refDescription = pattern.Description;
        UiSharedService.ColorText("Description", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(200f);
        if (UiSharedService.InputTextWrapMultiline("##PatternDescription", ref refDescription, 100, 3, 225f))
        {
            pattern.Description = refDescription;
        }
        _uiShared.DrawHelpText("Define the description for the Pattern.\n(Shown on tooltip hover if uploaded)");

        // tags.
        var tags = pattern.Tags;
        UiSharedService.ColorText("Tags:", ImGuiColors.ParsedGold);
        foreach (var tag in tags)
            ImGui.TextUnformatted("- "+tag);
    }

    private void DrawAdjustments(PatternData pattern)
    {
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
