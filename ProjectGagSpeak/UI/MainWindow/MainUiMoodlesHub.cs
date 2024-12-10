using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Numerics;
using System.Collections.Immutable;
using Dalamud.Interface.Utility;
using GagSpeak.Utils;
using Dalamud.Utility;

namespace GagSpeak.UI.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MainUiMoodlesHub : DisposableMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ShareHubService _shareHubService;
    private readonly UiSharedService _uiShared;
    public MainUiMoodlesHub(ILogger<MainUiMoodlesHub> logger, GagspeakMediator mediator,
        MainHub apiHubMain, ClientConfigurationManager clientConfigs,
        ShareHubService moodleHubService, UiSharedService uiShared) : base(logger, mediator)
    {
        _apiHubMain = apiHubMain;
        _clientConfigs = clientConfigs;
        _shareHubService = moodleHubService;
        _uiShared = uiShared;
    }

    public void DrawMoodlesHub()
    {
        // get the width of the window content region we set earlier
        var _windowContentWidth = UiSharedService.GetWindowContentRegionWidth();


        // draw the search filter
        DrawSearchFilter(_windowContentWidth);
        ImGui.Separator();

        // draw the results if there are any.
        if (_shareHubService.LatestMoodleResults.Count > 0)
        {
            DisplayResults();
            return;
        }

        // if they failed to draw, display that we we currently have no results!
        ImGui.Spacing();
        ImGuiUtil.Center("Search something to find results!");
    }

    private void DisplayResults()
    {
        // create a child window here. It will allow us to scroll up and dont in our moodle results search.
        using var moodleResultChild = ImRaii.Child("##MoodleResultChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
        // display a custom box icon for each search result obtained.
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        foreach (var moodle in _shareHubService.LatestMoodleResults)
            DrawMoodleResultBox(moodle);
    }

    private void DrawMoodleResultBox(ServerMoodleInfo moodleInfo)
    {

        float tryOnButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.PersonCircleQuestion, "Try On");
        float LikeButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Heart, moodleInfo.Likes.ToString());
        float copyButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Copy).X;
        float height = ImGui.GetFrameHeight() * 2.25f + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##MoodleResult_{moodleInfo.MoodleStatus.GUID}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {
            Vector2 imagePos = Vector2.Zero;
            using (ImRaii.Group())
            {
                // Handle displaying the icon.
                imagePos = ImGui.GetCursorPos();
                ImGuiHelpers.ScaledDummy(MoodlesService.StatusSize.X);
                if (ImGui.IsItemHovered())
                    if (!moodleInfo.MoodleStatus.Description.IsNullOrWhitespace())
                        UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.Description);

                // Title Display
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText(moodleInfo.MoodleStatus.Title, ImGuiColors.DalamudWhite);

                // Handle the like button
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - tryOnButtonSize - LikeButtonSize - copyButtonSize - ImGui.GetStyle().ItemInnerSpacing.X * 2);
                using (var color = ImRaii.PushColor(ImGuiCol.Text, moodleInfo.HasLikedMoodle ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey))
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Heart, moodleInfo.Likes.ToString(), null, true))
                        _shareHubService.PerformPatternLikeAction(moodleInfo.MoodleStatus.GUID);
                UiSharedService.AttachToolTip(moodleInfo.HasLikedMoodle ? "Remove Like from this pattern." : "Like this pattern!");

                // Handle the copy button.
                ImGui.SameLine();
                if (_uiShared.IconButton(FontAwesomeIcon.Copy, inPopup: true))
                    _shareHubService.CopyMoodleToClipboard(moodleInfo.MoodleStatus.GUID);
                UiSharedService.AttachToolTip("Copy this Status to import into Moodles!");
            }
            ImGui.Spacing();
            // next line:
            using (ImRaii.Group())
            {
                var stacksSize = _uiShared.GetIconData(FontAwesomeIcon.LayerGroup).X;
                var dispellableSize = _uiShared.GetIconData(FontAwesomeIcon.Eraser).X;
                var permanentSize = _uiShared.GetIconData(FontAwesomeIcon.Infinity).X;
                var stickySize = _uiShared.GetIconData(FontAwesomeIcon.MapPin).X;
                var customVfxPath = _uiShared.GetIconData(FontAwesomeIcon.Magic).X;
                var stackOnReapply = _uiShared.GetIconData(FontAwesomeIcon.LayerGroup).X;

                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.UserCircle);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(moodleInfo.Author, ImGuiColors.DalamudGrey);
                UiSharedService.AttachToolTip("Publisher of the Moodle");


                // jump to the right side to draw all the icon data.
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X * 5
                    - stacksSize - dispellableSize - permanentSize - stickySize - customVfxPath - stackOnReapply);
                ImGui.AlignTextToFramePadding();
                _uiShared.BooleanToColoredIcon(moodleInfo.MoodleStatus.Stacks > 1, false, FontAwesomeIcon.LayerGroup, FontAwesomeIcon.LayerGroup, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.Stacks > 1 ? "Has " + moodleInfo.MoodleStatus.Stacks + "Stacks." : "Not a stackable Moodle.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodleInfo.MoodleStatus.Dispelable, false, FontAwesomeIcon.Eraser, FontAwesomeIcon.Eraser, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.Dispelable ? "Can be dispelled." : "Cannot be dispelled.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodleInfo.MoodleStatus.AsPermanent, false, FontAwesomeIcon.Infinity, FontAwesomeIcon.Infinity, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.AsPermanent ? "Permanent Moodle." : "Temporary Moodle.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodleInfo.MoodleStatus.Persistent, false, FontAwesomeIcon.MapPin, FontAwesomeIcon.MapPin, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.Persistent ? "Marked as a Sticky Moodle." : "Not Sticky.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(!string.IsNullOrEmpty(moodleInfo.MoodleStatus.CustomVFXPath), false, FontAwesomeIcon.Magic, FontAwesomeIcon.Magic, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(!string.IsNullOrEmpty(moodleInfo.MoodleStatus.CustomVFXPath) ? "Has a custom VFX path." : "No custom VFX path.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodleInfo.MoodleStatus.StackOnReapply, false, FontAwesomeIcon.LayerGroup, FontAwesomeIcon.LayerGroup, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.StackOnReapply ? "Stacks on Reapplication." : "Doesn't stack on reapplication.");
            }

            try
            {
                if (moodleInfo.MoodleStatus.IconID != 0 && imagePos != Vector2.Zero)
                {
                    var statusIcon = _uiShared.GetGameStatusIcon((uint)((uint)moodleInfo.MoodleStatus.IconID + moodleInfo.MoodleStatus.Stacks - 1));

                    if (statusIcon is { } wrap)
                    {
                        ImGui.SetCursorPos(imagePos);
                        ImGui.Image(statusIcon.ImGuiHandle, MoodlesService.StatusSize);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to draw the status icon for the moodle.");
            }
        }
    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter(float availableWidth)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        float updateSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Search, "Search");
        float sortIconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.SortAmountUp).X;
        float filterTypeSize = 80f;
        FontAwesomeIcon sortIcon = _shareHubService.SearchSort == SearchSort.Ascending ? FontAwesomeIcon.SortAmountUp : FontAwesomeIcon.SortAmountDown;

        // Draw out the first row. This contains the search bar, the Update Search Button, and the Show 
        using (ImRaii.Group())
        {
            ImGui.SetNextItemWidth(availableWidth - sortIconSize - filterTypeSize - updateSize - 3 * spacing);
            var searchString = _shareHubService.SearchString;
            if (ImGui.InputTextWithHint("##moodleSearchFilter", "Search for Moodles...", ref searchString, 125))
                _shareHubService.SearchString = searchString;
            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(FontAwesomeIcon.Search, disabled: !_shareHubService.CanShareHubTask))
                _shareHubService.PerformMoodleSearch();
            UiSharedService.AttachToolTip("Update Search Results");

            // Show the filter combo.
            ImUtf8.SameLineInner();
            _uiShared.DrawCombo("##moodleFilterType", filterTypeSize, new[] { ResultFilter.Likes, ResultFilter.DatePosted }, (filter) => filter.ToString(),
                (filter) => _shareHubService.SearchFilter = filter, _shareHubService.SearchFilter, false, ImGuiComboFlags.NoArrowButton);
            UiSharedService.AttachToolTip("Sort Method--SEP--Define how results are found.");

            // the sort direction.
            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(sortIcon))
                _shareHubService.ToggleSortDirection();
            UiSharedService.AttachToolTip("Sort Direction--SEP--Current: " + _shareHubService.SearchSort + "");
        }

        using (ImRaii.Group())
        {
            float tagsComboWidth = 125;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - tagsComboWidth - ImGui.GetStyle().ItemInnerSpacing.X);
            var searchTags = _shareHubService.SearchTags;
            if (ImGui.InputTextWithHint("##shareHubTags", "Enter tags split by , (optional)", ref searchTags, 200))
                _shareHubService.SearchTags = searchTags;

            ImUtf8.SameLineInner();
            ImGui.SetNextItemWidth(tagsComboWidth);
            _uiShared.DrawCombo("##moodleTagsFilter", tagsComboWidth, _shareHubService.FetchedTags.ToImmutableList(), (i) => i,
                (tag) =>
                {
                    // append the tag to the search tags if it does not exist.
                    if (!_shareHubService.SearchTags.Contains(tag))
                    {
                        // if there is not a comma at the end of the string, add one.
                        if (_shareHubService.SearchTags.Length > 0 && _shareHubService.SearchTags[^1] != ',')
                            _shareHubService.SearchTags += ",";
                        // append the tag to it.
                        _shareHubService.SearchTags += tag.ToLower();
                    }
                }, shouldShowLabel: false, defaultPreviewText: "Add Tag..");
            UiSharedService.AttachToolTip("Select from an existing list of tags." +
                "--SEP--This will help make your Moodle easier to find.");
        }
    }
}

