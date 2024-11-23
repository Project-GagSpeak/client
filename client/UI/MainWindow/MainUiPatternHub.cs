using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.MainWindow;
public class MainUiPatternHub : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PatternHubService _patternHubService;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;

    public MainUiPatternHub(ILogger<MainUiPatternHub> logger,
        GagspeakMediator mediator, ClientConfigurationManager clientConfigs,
        PatternHubService patternHubService, UiSharedService uiShared,
        TutorialService guides) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _patternHubService = patternHubService;
        _uiShared = uiShared;
        _guides = guides;
    }
    public void DrawPatternHub()
    {
        // get the width of the window content region we set earlier
        var _windowContentWidth = UiSharedService.GetWindowContentRegionWidth();

        // draw the search filter
        DrawSearchFilter(_windowContentWidth);
        ImGui.Separator();

        // draw the results if there are any.
        if (_patternHubService.SearchResults.Count > 0)
        {
            DisplayResults();
        }
        else
        {
            _patternHubService.DisplayPendingMessages();
            ImGui.Spacing();
            ImGuiUtil.Center("Search something to find results!");
        }
    }

    private void DisplayResults()
    {
        // draw the current status if we are still fetching from the server.
        _patternHubService.DisplayPendingMessages();

        // create a child window here. It will allow us to scroll up and dont in our pattern results search.
        using var patternResultChild = ImRaii.Child("##PatternResultChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
        // display a custom box icon for each search result obtained.
        foreach (var pattern in _patternHubService.SearchResults)
        {
            // draw a unique box for each pattern result. (SELF REMINDER CORDY, DONT CARE TOO MUCH ABOUT THE VISUALS HERE THEY WILL BE REWORKED.
            DrawPatternResultBox(pattern);
        }
    }

    private void DrawPatternResultBox(ServerPatternInfo patternInfo)
    {
        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.

        float height = ImGui.GetFrameHeight()*3 +ImGui.GetStyle().ItemSpacing.Y*2 + ImGui.GetStyle().WindowPadding.Y*2;
        using (var patternResChild = ImRaii.Child($"##PatternResult_{patternInfo.Identifier}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {
            if(!patternResChild) return;

            using (var group = ImRaii.Group())
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
                        _patternHubService.RatePattern(patternInfo.Identifier);
                    }
                    UiSharedService.AttachToolTip(patternInfo.HasLiked ? "Remove Like from this pattern." : "Like this pattern!");
                }
                ImGui.SameLine();
                using (var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite2))
                {
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Download, patternInfo.Downloads.ToString(), null, true, 
                        _clientConfigs.PatternExists(patternInfo.Identifier), "DownloadPattern"+patternInfo.Identifier))
                    {
                        _patternHubService.DownloadPatternFromServer(patternInfo.Identifier);
                    }
                    UiSharedService.AttachToolTip("Download this pattern!");
                }
            }
            // next line:
            using (var group2 = ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.UserCircle);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(patternInfo.Author, ImGuiColors.DalamudGrey);
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
            using (var group3 = ImRaii.Group())
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
                UiSharedService.AttachToolTip(patternInfo.UsesVibrations? "Uses Vibrations" : "Does not use Vibrations");
                _uiShared.BooleanToColoredIcon(patternInfo.UsesRotations, true, FontAwesomeIcon.Sync, FontAwesomeIcon.Sync, ImGuiColors.ParsedPink, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(patternInfo.UsesRotations ? "Uses Rotations" : "Does not use Rotations");
            }
        }

    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter(float availableWidth)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        float comboSize = ImGui.CalcTextSize("Upload Date    ").X;
        FontAwesomeIcon sortIcon = _patternHubService.CurrentSort == SearchSort.Ascending ? FontAwesomeIcon.SortAmountUp : FontAwesomeIcon.SortAmountDown;
        float sortSize = _uiShared.GetIconButtonSize(sortIcon).X;
        float updateSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Sync).X;
        string patternFilter = _patternHubService.SearchQuery;
        ImGui.SetNextItemWidth(availableWidth - comboSize - sortSize - updateSize - 3*ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGui.InputTextWithHint("##patternSearchFilter", "Search for Patterns...", ref patternFilter, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _patternHubService.SearchPatterns(patternFilter);
        }
        UiSharedService.AttachToolTip("Enter fetches results from your search!");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubSearch, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        ImUtf8.SameLineInner();
        if (_uiShared.IconButton(FontAwesomeIcon.Sync))
        {
            _patternHubService.UpdateResults();
        }
        UiSharedService.AttachToolTip("Update Search Results");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubUpdate, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        ImUtf8.SameLineInner();
        _uiShared.DrawCombo("##patternFilterType", comboSize, Enum.GetValues<SearchFilter>(), (filter) => _patternHubService.FilterToName(filter),
        (filter) => { _patternHubService.SetFilter(filter); }, _patternHubService.CurrentFilter, false, ImGuiComboFlags.NoArrowButton);
        UiSharedService.AttachToolTip("Filter Type");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubFilterType, ImGui.GetWindowPos(), ImGui.GetWindowSize());


        ImUtf8.SameLineInner();
        if (_uiShared.IconButton(sortIcon))
        {
            _patternHubService.ToggleSort();
        }
        UiSharedService.AttachToolTip("Toggle Sort\n(Current: "+_patternHubService.CurrentSort+")");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubResultOrder, ImGui.GetWindowPos(), ImGui.GetWindowSize());
    }
}

