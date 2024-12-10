using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;

namespace GagSpeak.UI.MainWindow;
public class MainUiPatternHub : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ShareHubService _shareHub;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;

    public MainUiPatternHub(ILogger<MainUiPatternHub> logger,
        GagspeakMediator mediator, ClientConfigurationManager clientConfigs,
        ShareHubService patternHubService, UiSharedService uiShared,
        TutorialService guides) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _shareHub = patternHubService;
        _uiShared = uiShared;
        _guides = guides;
    }
    public void DrawPatternHub()
    {
        // Handle grabbing new info from the server if none is present.
        if(!_shareHub.InitialPatternsCall && _shareHub.CanShareHubTask)
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
        using var patternResultChild = ImRaii.Child("##PatternResultChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
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
        float height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##PatternResult_{patternInfo.Identifier}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {

            using (ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                UiSharedService.ColorText(patternInfo.Name, ImGuiColors.DalamudWhite);
                UiSharedService.AttachToolTip(patternInfo.Description);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X
                    - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Heart, patternInfo.Likes.ToString())
                    - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Download, patternInfo.Downloads.ToString()));
                using (var color = ImRaii.PushColor(ImGuiCol.Text, patternInfo.HasLiked ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey))
                {
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Heart, patternInfo.Likes.ToString(), null, true))
                    {
                        _shareHub.PerformPatternLikeAction(patternInfo.Identifier);
                    }
                    UiSharedService.AttachToolTip(patternInfo.HasLiked ? "Remove Like from this pattern." : "Like this pattern!");
                }
                ImGui.SameLine();
                using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite2))
                {
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Download, patternInfo.Downloads.ToString(), null, true,
                        _clientConfigs.PatternExists(patternInfo.Identifier), "DownloadPattern" + patternInfo.Identifier))
                    {
                        _shareHub.DownloadPattern(patternInfo.Identifier);
                    }
                    UiSharedService.AttachToolTip("Download this pattern!");
                }
            }
            // next line:
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.UserCircle);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(patternInfo.Author +
                    " (" + patternInfo.UploadedDate.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) + ")", ImGuiColors.DalamudGrey);
                UiSharedService.AttachToolTip("Publisher of the Pattern");


                var formatDuration = patternInfo.Length.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
                string timerText = patternInfo.Length.ToString(formatDuration);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(timerText).X - _uiShared.GetIconData(FontAwesomeIcon.Stopwatch).X - ImGui.GetStyle().ItemSpacing.X);
                _uiShared.IconText(FontAwesomeIcon.Stopwatch);
                UiSharedService.AttachToolTip("Total Pattern Duration");
                ImUtf8.SameLineInner();
                ImGui.TextUnformatted(patternInfo.Length.ToString(formatDuration));
                UiSharedService.AttachToolTip("Total Pattern Duration");
            }

            // next line:
            using (ImRaii.Group())
            {
                var vibeSize = _uiShared.GetIconData(FontAwesomeIcon.Water);
                var rotationSize = _uiShared.GetIconData(FontAwesomeIcon.GroupArrowsRotate);
                float allowedLength = ImGui.GetContentRegionAvail().X - vibeSize.X - rotationSize.X - ImGui.GetStyle().ItemSpacing.X;

                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.Tags);

                UiSharedService.AttachToolTip("Tags for the Pattern");
                ImGui.SameLine();
                var tagsString = string.Join(", ", patternInfo.Tags);
                if (ImGui.CalcTextSize(tagsString).X > allowedLength)
                {
                    tagsString = tagsString.Substring(0, (int)(allowedLength / ImGui.CalcTextSize("A").X)) + "...";
                }
                ImGui.TextUnformatted(tagsString);
                float rightEnd = ImGui.GetContentRegionAvail().X - vibeSize.X - rotationSize.X - ImGui.GetStyle().ItemSpacing.X;
                ImGui.SameLine(rightEnd);
                _uiShared.BooleanToColoredIcon(patternInfo.UsesVibrations, false, FontAwesomeIcon.Water, FontAwesomeIcon.Water, ImGuiColors.ParsedPink, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(patternInfo.UsesVibrations ? "Uses Vibrations" : "Does not use Vibrations");
                _uiShared.BooleanToColoredIcon(patternInfo.UsesRotations, true, FontAwesomeIcon.Sync, FontAwesomeIcon.Sync, ImGuiColors.ParsedPink, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(patternInfo.UsesRotations ? "Uses Rotations" : "Does not use Rotations");
            }
        }

    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        float updateSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Search).X;
        float sortIconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.SortAmountUp).X;
        float filterTypeSize = 80f;
        float durationFilterSize = 60f;
        FontAwesomeIcon sortIcon = _shareHub.SearchSort == SearchSort.Ascending ? FontAwesomeIcon.SortAmountUp : FontAwesomeIcon.SortAmountDown;

        // Draw out the first row. This contains the search bar, the Update Search Button, and the Show 
        using (ImRaii.Group())
        {
            ImGui.SetNextItemWidth(UiSharedService.GetWindowContentRegionWidth() - sortIconSize - durationFilterSize - filterTypeSize - updateSize - 4 * spacing);
            var searchString = _shareHub.SearchString;
            if (ImGui.InputTextWithHint("##patternSearchFilter", "Search for Patterns...", ref searchString, 125))
                _shareHub.SearchString = searchString;
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubSearch, ImGui.GetWindowPos(), ImGui.GetWindowSize());

            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(FontAwesomeIcon.Search, disabled: !_shareHub.CanShareHubTask))
                _shareHub.PerformPatternSearch();
            UiSharedService.AttachToolTip("Update Search Results");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubUpdate, ImGui.GetWindowPos(), ImGui.GetWindowSize());

            // Show the filter combo.
            ImUtf8.SameLineInner();
            _uiShared.DrawCombo("##patternFilterType", filterTypeSize, Enum.GetValues<ResultFilter>(), (filter) => filter.ToString(),
                (filter) => _shareHub.SearchFilter = filter, _shareHub.SearchFilter, false, ImGuiComboFlags.NoArrowButton);
            UiSharedService.AttachToolTip("Sort Method" +
                "--SEP--Define how results are found.");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubFilterType, ImGui.GetWindowPos(), ImGui.GetWindowSize());

            // beside this, the duration filter.
            ImUtf8.SameLineInner();
            _uiShared.DrawCombo("##patternDurationFilter", durationFilterSize, Enum.GetValues<DurationLength>(), (dura) => dura.ToName(),
                (newDura) => _shareHub.SearchDuration = newDura, _shareHub.SearchDuration, false, ImGuiComboFlags.NoArrowButton);
            UiSharedService.AttachToolTip("Time Range");

            // the sort direction.
            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(sortIcon))
                _shareHub.ToggleSortDirection();
            UiSharedService.AttachToolTip("Sort Direction" +
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
            _uiShared.DrawComboSearchable("##patternTagsFilter", tagsComboWidth, _shareHub.FetchedTags.ToImmutableList(), (i) => i, false,
                (tag) =>
                {
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
            UiSharedService.AttachToolTip("Select from an existing list of tags." +
                "--SEP--This will help make your Pattern easier to find.");
        }
    }
}

