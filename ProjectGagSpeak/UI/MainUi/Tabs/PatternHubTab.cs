using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;
using System.Globalization;
using GagSpeak.Achievements;
using GagSpeak.State.Managers;

namespace GagSpeak.CkCommons.Gui.MainWindow;
public class PatternHubTab : DisposableMediatorSubscriberBase
{
    private readonly PatternManager _patterns;
    private readonly ShareHubService _shareHub;
    private readonly TutorialService _guides;

    public PatternHubTab(ILogger<PatternHubTab> logger, GagspeakMediator mediator,
        PatternManager patterns, ShareHubService shareHub, TutorialService guides) : base(logger, mediator)
    {
        _patterns = patterns;
        _shareHub = shareHub;
        _guides = guides;
    }

    public void DrawPatternHub()
    {
        // Handle grabbing new info from the server if none is present.
        if(!_shareHub.InitialPatternsCall && !_shareHub.DisableUI)
            _shareHub.PerformPatternSearch();

        DrawSearchFilter();
        ImGui.Separator();

        // draw the results if there are any.
        if (_shareHub.LatestPatternResults.Count <= 0)
        {
            ImGui.Spacing();
            ImGuiUtil.Center("Search something to find results!");
            return;
        }

        // set the scrollbar width to be shorter than normal
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var patternResultChild = ImRaii.Child("##PatternResultChild", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysVerticalScrollbar);
        // result styles.
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // display results.
        foreach (var pattern in _shareHub.LatestPatternResults)
            DrawPatternResultBox(pattern);
    }

    private void DrawPatternResultBox(ServerPatternInfo patternInfo)
    {
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##PatternResult_{patternInfo.Identifier}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, WFlags.ChildWindow))
        {

            using (ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                CkGui.ColorText(patternInfo.Label, ImGuiColors.DalamudWhite);
                CkGui.AttachToolTip(patternInfo.Description);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X
                    - CkGui.IconTextButtonSize(FAI.Heart, patternInfo.Likes.ToString())
                    - CkGui.IconTextButtonSize(FAI.Download, patternInfo.Downloads.ToString()));
                using (var color = ImRaii.PushColor(ImGuiCol.Text, patternInfo.HasLiked ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey))
                {
                    if (CkGui.IconTextButton(FAI.Heart, patternInfo.Likes.ToString(), null, true))
                    {
                        _shareHub.PerformPatternLikeAction(patternInfo.Identifier);
                    }
                    CkGui.AttachToolTip(patternInfo.HasLiked ? "Remove Like from this pattern." : "Like this pattern!");
                }
                ImGui.SameLine();
                using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite2))
                {
                    if (CkGui.IconTextButton(FAI.Download, patternInfo.Downloads.ToString(), null, true,
                        _patterns.Storage.Contains(patternInfo.Identifier), "DownloadPattern" + patternInfo.Identifier))
                    {
                        _shareHub.DownloadPattern(patternInfo.Identifier);
                    }
                    CkGui.AttachToolTip("Download this pattern!");
                }
            }
            // next line:
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FAI.UserCircle);
                ImUtf8.SameLineInner();
                CkGui.ColorText(patternInfo.Author +
                    " (" + patternInfo.UploadedDate.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) + ")", ImGuiColors.DalamudGrey);
                CkGui.AttachToolTip("Publisher of the Pattern");


                var formatDuration = patternInfo.Length.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
                var timerText = patternInfo.Length.ToString(formatDuration);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(timerText).X - CkGui.IconSize(FAI.Stopwatch).X - ImGui.GetStyle().ItemSpacing.X);
                CkGui.IconText(FAI.Stopwatch);
                CkGui.AttachToolTip("Total Pattern Duration");
                ImUtf8.SameLineInner();
                ImGui.TextUnformatted(patternInfo.Length.ToString(formatDuration));
                CkGui.AttachToolTip("Total Pattern Duration");
            }

            // next line:
            using (ImRaii.Group())
            {
                var vibeSize = CkGui.IconSize(FAI.Water);
                var rotationSize = CkGui.IconSize(FAI.GroupArrowsRotate);
                var allowedLength = ImGui.GetContentRegionAvail().X - vibeSize.X - rotationSize.X - ImGui.GetStyle().ItemSpacing.X;

                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FAI.Tags);

                CkGui.AttachToolTip("Tags for the Pattern");
                ImGui.SameLine();
                var tagsString = string.Join(", ", patternInfo.Tags);
                if (ImGui.CalcTextSize(tagsString).X > allowedLength)
                {
                    tagsString = tagsString.Substring(0, (int)(allowedLength / ImGui.CalcTextSize("A").X)) + "...";
                }
                ImGui.TextUnformatted(tagsString);
                var rightEnd = ImGui.GetContentRegionAvail().X - vibeSize.X - rotationSize.X - ImGui.GetStyle().ItemSpacing.X;
                ImGui.SameLine(rightEnd);
                CkGui.BooleanToColoredIcon(patternInfo.UsesVibrations, false, FAI.Water, FAI.Water, ImGuiColors.ParsedPink, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(patternInfo.UsesVibrations ? "Uses Vibrations" : "Does not use Vibrations");
                CkGui.BooleanToColoredIcon(patternInfo.UsesRotations, true, FAI.Sync, FAI.Sync, ImGuiColors.ParsedPink, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(patternInfo.UsesRotations ? "Uses Rotations" : "Does not use Rotations");
            }
        }

    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var updateSize = CkGui.IconButtonSize(FAI.Search).X;
        var sortIconSize = CkGui.IconButtonSize(FAI.SortAmountUp).X;
        var filterTypeSize = 80f;
        var durationFilterSize = 60f;
        var sortIcon = _shareHub.SearchSort == SearchSort.Ascending ? FAI.SortAmountUp : FAI.SortAmountDown;

        // Draw out the first row. This contains the search bar, the Update Search Button, and the Show 
        using (ImRaii.Group())
        {
            ImGui.SetNextItemWidth(CkGui.GetWindowContentRegionWidth() - sortIconSize - durationFilterSize - filterTypeSize - updateSize - 4 * spacing);
            var searchString = _shareHub.SearchString;
            if (ImGui.InputTextWithHint("##patternSearchFilter", "Search for Patterns...", ref searchString, 125))
                _shareHub.SearchString = searchString;
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubSearch, ImGui.GetWindowPos(), ImGui.GetWindowSize());

            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.Search, disabled: !_shareHub.DisableUI))
                _shareHub.PerformPatternSearch();
            CkGui.AttachToolTip("Update Search Results");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubUpdate, ImGui.GetWindowPos(), ImGui.GetWindowSize());

            // Show the filter combo.
            ImUtf8.SameLineInner();
            if(ImGuiUtil.GenericEnumCombo("##PatternFilterType", filterTypeSize, _shareHub.SearchFilter, out ResultFilter newFilter, i => i.ToString()))
                _shareHub.SearchFilter = newFilter;
            CkGui.AttachToolTip("Sort Method.--SEP--Defines how results are found.");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubFilterType, ImGui.GetWindowPos(), ImGui.GetWindowSize());

            // beside this, the duration filter.
            ImUtf8.SameLineInner();
            if(ImGuiUtil.GenericEnumCombo("##DurationFilter", durationFilterSize, _shareHub.SearchDuration, out DurationLength newLength, i => i.ToName()))
                _shareHub.SearchDuration = newLength;
            CkGui.AttachToolTip("Time Range");

            // the sort direction.
            ImUtf8.SameLineInner();
            if (CkGui.IconButton(sortIcon))
                _shareHub.ToggleSortDirection();
            CkGui.AttachToolTip("Sort Direction" +
                "--SEP--Current: " + _shareHub.SearchSort + "");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubResultOrder, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => UnlocksEventManager.AchievementEvent(UnlocksEvent.TutorialCompleted));
        }

        using (ImRaii.Group())
        {
            float tagsComboWidth = 125;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - tagsComboWidth - ImGui.GetStyle().ItemInnerSpacing.X);
            var searchTags = _shareHub.SearchTags;
            if (ImGui.InputTextWithHint("##shareHubTags", "Enter tags split by , (optional)", ref searchTags, 200))
                _shareHub.SearchTags = searchTags;

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(tagsComboWidth);
            CkGui.DrawComboSearchable("##patternTagsFilter", tagsComboWidth, _shareHub.FetchedTags.ToImmutableList(), (i) => i, false,
                (tag) =>
                {
                    if(string.IsNullOrWhiteSpace(tag))
                        return;
                    // append the tag to the search tags if it does not exist.
                    if (!_shareHub.SearchTags.Contains(tag))
                    {
                        // if there is not a comma at the end of the string, add one.
                        if (_shareHub.SearchTags.Length > 0 && _shareHub.SearchTags[^1] != ',')
                            _shareHub.SearchTags += ", ";
                        // append the tag to it.
                        _shareHub.SearchTags += tag.ToLower();
                    }
                }, defaultPreviewText: "Add Tag..");
            CkGui.AttachToolTip("Select from an existing list of tags." +
                "--SEP--This will help make your Pattern easier to find.");
        }
    }
}

