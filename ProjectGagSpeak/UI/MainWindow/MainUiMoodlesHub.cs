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
    private readonly ShareHubService _shareHub;
    private readonly UiSharedService _uiShared;
    public MainUiMoodlesHub(ILogger<MainUiMoodlesHub> logger, GagspeakMediator mediator,
        MainHub apiHubMain, ClientConfigurationManager clientConfigs,
        ShareHubService moodleHubService, UiSharedService uiShared) : base(logger, mediator)
    {
        _apiHubMain = apiHubMain;
        _clientConfigs = clientConfigs;
        _shareHub = moodleHubService;
        _uiShared = uiShared;
    }

    public void DrawMoodlesHub()
    {
        // Handle grabbing new info from the server if none is present.
        if (!_shareHub.InitialMoodlesCall && _shareHub.CanShareHubTask)
            _shareHub.PerformMoodleSearch();

        DrawSearchFilter();
        ImGui.Separator();

        // draw the results if there are any.
        if (_shareHub.LatestMoodleResults.Count <= 0)
        {
            ImGui.Spacing();
            ImGuiUtil.Center("Search something to find results!");
            return;
        }

        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var moodleResultChild = ImRaii.Child("##MoodleResultChild", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
        // inner child styles
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // draw results.
        foreach (var moodle in _shareHub.LatestMoodleResults)
            DrawMoodleResultBox(moodle);
    }

    private void DrawMoodleResultBox(ServerMoodleInfo moodleInfo)
    {

        float tryOnButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.PersonCircleQuestion, "Try");
        float LikeButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Heart, moodleInfo.Likes.ToString());
        float copyButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Copy).X;
        float height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##MoodleResult_{moodleInfo.MoodleStatus.GUID}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {
            Vector2 imagePos = Vector2.Zero;
            using (ImRaii.Group())
            {
                // Handle displaying the icon.
                imagePos = ImGui.GetCursorPos();
                ImGuiHelpers.ScaledDummy(ImGui.GetFrameHeight());
                if (ImGui.IsItemHovered())
                    if (!moodleInfo.MoodleStatus.Description.IsNullOrWhitespace())
                        UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.Description.StripColorTags());

                // Title Display
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText(moodleInfo.MoodleStatus.Title.StripColorTags(), ImGuiColors.DalamudWhite);

                // Handle the Try On button.
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - tryOnButtonSize - LikeButtonSize - copyButtonSize - ImGui.GetStyle().ItemInnerSpacing.X * 2);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
                    if (_uiShared.IconTextButton(FontAwesomeIcon.PersonCircleQuestion, "Try", isInPopup: true))
                        _shareHub.TryOnMoodle(moodleInfo.MoodleStatus.GUID);
                UiSharedService.AttachToolTip("Try this Moodle on your character to see a preview of it.");

                // Handle the like button
                ImUtf8.SameLineInner();
                using (ImRaii.PushColor(ImGuiCol.Text, moodleInfo.HasLikedMoodle ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey))
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Heart, moodleInfo.Likes.ToString(), null, true))
                        _shareHub.PerformMoodleLikeAction(moodleInfo.MoodleStatus.GUID);
                UiSharedService.AttachToolTip(moodleInfo.HasLikedMoodle ? "Remove Like from this pattern." : "Like this pattern!");

                // Handle the copy button.
                ImUtf8.SameLineInner();
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
                    if (_uiShared.IconButton(FontAwesomeIcon.Copy, inPopup: true))
                        _shareHub.CopyMoodleToClipboard(moodleInfo.MoodleStatus.GUID);
                UiSharedService.AttachToolTip("Copy this Status for simple Moodles Import!");
            }
            // next line:
            using (ImRaii.Group())
            {
                var stacksSize = _uiShared.GetIconData(FontAwesomeIcon.LayerGroup).X;
                var dispellableSize = _uiShared.GetIconData(FontAwesomeIcon.Eraser).X;
                var permanentSize = _uiShared.GetIconData(FontAwesomeIcon.Infinity).X;
                var stickySize = _uiShared.GetIconData(FontAwesomeIcon.MapPin).X;
                var customVfxPath = _uiShared.GetIconData(FontAwesomeIcon.Magic).X;
                var stackOnReapply = _uiShared.GetIconData(FontAwesomeIcon.SortNumericUpAlt).X;

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
                _uiShared.BooleanToColoredIcon(moodleInfo.MoodleStatus.StackOnReapply, false, FontAwesomeIcon.SortNumericUpAlt, FontAwesomeIcon.SortNumericUpAlt, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodleInfo.MoodleStatus.StackOnReapply ? "Stacks " + moodleInfo.MoodleStatus.StacksIncOnReapply + " times on Reapplication." : "Doesn't stack on reapplication.");
            }

            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.Tags);
                ImUtf8.SameLineInner();
                var maxWidth = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X;
                var tagsString = string.Join(", ", moodleInfo.Tags);
                if (ImGui.CalcTextSize(tagsString).X > maxWidth)
                {
                    tagsString = tagsString.Substring(0, (int)(maxWidth / ImGui.CalcTextSize("A").X)) + "...";
                }
                UiSharedService.ColorText(tagsString, ImGuiColors.ParsedGrey);
            }
            UiSharedService.AttachToolTip("Tags for the Pattern");

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
    public void DrawSearchFilter()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        float updateSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Search, "Search");
        float sortIconSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.SortAmountUp).X;
        float filterTypeSize = 80f;
        FontAwesomeIcon sortIcon = _shareHub.SearchSort == SearchSort.Ascending ? FontAwesomeIcon.SortAmountUp : FontAwesomeIcon.SortAmountDown;

        // Draw out the first row. This contains the search bar, the Update Search Button, and the Show 
        using (ImRaii.Group())
        {
            ImGui.SetNextItemWidth(UiSharedService.GetWindowContentRegionWidth() - sortIconSize - filterTypeSize - updateSize - 3 * spacing);
            var searchString = _shareHub.SearchString;
            if (ImGui.InputTextWithHint("##moodleSearchFilter", "Search for Moodles...", ref searchString, 125))
                _shareHub.SearchString = searchString;
            ImUtf8.SameLineInner();
            if (_uiShared.IconTextButton(FontAwesomeIcon.Search, "Search", disabled: !_shareHub.CanShareHubTask))
                _shareHub.PerformMoodleSearch();
            UiSharedService.AttachToolTip("Update Search Results");

            // Show the filter combo.
            ImUtf8.SameLineInner();
            _uiShared.DrawCombo("##moodleFilterType", filterTypeSize, new[] { ResultFilter.Likes, ResultFilter.DatePosted }, (filter) => filter.ToString(),
                (filter) => _shareHub.SearchFilter = filter, _shareHub.SearchFilter, false, ImGuiComboFlags.NoArrowButton);
            UiSharedService.AttachToolTip("Sort Method--SEP--Define how results are found.");

            // the sort direction.
            ImUtf8.SameLineInner();
            if (_uiShared.IconButton(sortIcon))
                _shareHub.ToggleSortDirection();
            UiSharedService.AttachToolTip("Sort Direction--SEP--Current: " + _shareHub.SearchSort + "");
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
            _uiShared.DrawComboSearchable("##moodleTagsFilter", tagsComboWidth, _shareHub.FetchedTags.ToImmutableList(), (i) => i, false,
                (tag) =>
                {
                    if (tag.IsNullOrWhitespace())
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
            UiSharedService.AttachToolTip("Select from an existing list of tags." +
                "--SEP--This will help make your Moodle easier to find.");
        }
    }
}

