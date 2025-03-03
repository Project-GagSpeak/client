using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.UI.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MoodleHubTab : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly CkGui _ckGui;
    private readonly ShareHubService _shareHub;
    public MoodleHubTab(ILogger<MoodleHubTab> logger, GagspeakMediator mediator, MainHub hub,
        CkGui ckGui, ShareHubService moodleHubService) : base(logger, mediator)
    {
        _hub = hub;
        _ckGui = ckGui;
        _shareHub = moodleHubService;
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

        var tryOnButtonSize = CkGui.IconTextButtonSize(FontAwesomeIcon.PersonCircleQuestion, "Try");
        var LikeButtonSize = CkGui.IconTextButtonSize(FontAwesomeIcon.Heart, moodleInfo.Likes.ToString());
        var copyButtonSize = CkGui.IconButtonSize(FontAwesomeIcon.Copy).X;
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##MoodleResult_{moodleInfo.MoodleStatus.GUID}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {
            var imagePos = Vector2.Zero;
            using (ImRaii.Group())
            {
                // Handle displaying the icon.
                imagePos = ImGui.GetCursorPos();
                ImGuiHelpers.ScaledDummy(ImGui.GetFrameHeight());
                if (ImGui.IsItemHovered())
                    if (!moodleInfo.MoodleStatus.Description.IsNullOrWhitespace())
                        CkGui.AttachToolTip(moodleInfo.MoodleStatus.Description.StripColorTags());

                // Title Display
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                CkGui.ColorText(moodleInfo.MoodleStatus.Title.StripColorTags(), ImGuiColors.DalamudWhite);

                // Handle the Try On button.
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - tryOnButtonSize - LikeButtonSize - copyButtonSize - ImGui.GetStyle().ItemInnerSpacing.X * 2);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
                    if (CkGui.IconTextButton(FontAwesomeIcon.PersonCircleQuestion, "Try", isInPopup: true))
                        _shareHub.TryOnMoodle(moodleInfo.MoodleStatus.GUID);
                CkGui.AttachToolTip("Try this Moodle on your character to see a preview of it.");

                // Handle the like button
                ImUtf8.SameLineInner();
                using (ImRaii.PushColor(ImGuiCol.Text, moodleInfo.HasLikedMoodle ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey))
                    if (CkGui.IconTextButton(FontAwesomeIcon.Heart, moodleInfo.Likes.ToString(), null, true))
                        _shareHub.PerformMoodleLikeAction(moodleInfo.MoodleStatus.GUID);
                CkGui.AttachToolTip(moodleInfo.HasLikedMoodle ? "Remove Like from this pattern." : "Like this pattern!");

                // Handle the copy button.
                ImUtf8.SameLineInner();
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
                    if (CkGui.IconButton(FontAwesomeIcon.Copy, inPopup: true))
                        _shareHub.CopyMoodleToClipboard(moodleInfo.MoodleStatus.GUID);
                CkGui.AttachToolTip("Copy this Status for simple Moodles Import!");
            }
            // next line:
            using (ImRaii.Group())
            {
                var stacksSize = CkGui.IconSize(FontAwesomeIcon.LayerGroup).X;
                var dispellableSize = CkGui.IconSize(FontAwesomeIcon.Eraser).X;
                var permanentSize = CkGui.IconSize(FontAwesomeIcon.Infinity).X;
                var stickySize = CkGui.IconSize(FontAwesomeIcon.MapPin).X;
                var customVfxPath = CkGui.IconSize(FontAwesomeIcon.Magic).X;
                var stackOnReapply = CkGui.IconSize(FontAwesomeIcon.SortNumericUpAlt).X;

                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.UserCircle);
                ImUtf8.SameLineInner();
                CkGui.ColorText(moodleInfo.Author, ImGuiColors.DalamudGrey);
                CkGui.AttachToolTip("Publisher of the Moodle");


                // jump to the right side to draw all the icon data.
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X * 5
                    - stacksSize - dispellableSize - permanentSize - stickySize - customVfxPath - stackOnReapply);
                ImGui.AlignTextToFramePadding();
                CkGui.BooleanToColoredIcon(moodleInfo.MoodleStatus.Stacks > 1, false, FontAwesomeIcon.LayerGroup, FontAwesomeIcon.LayerGroup, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodleInfo.MoodleStatus.Stacks > 1 ? "Has " + moodleInfo.MoodleStatus.Stacks + "Stacks." : "Not a stackable Moodle.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodleInfo.MoodleStatus.Dispelable, false, FontAwesomeIcon.Eraser, FontAwesomeIcon.Eraser, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodleInfo.MoodleStatus.Dispelable ? "Can be dispelled." : "Cannot be dispelled.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodleInfo.MoodleStatus.AsPermanent, false, FontAwesomeIcon.Infinity, FontAwesomeIcon.Infinity, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodleInfo.MoodleStatus.AsPermanent ? "Permanent Moodle." : "Temporary Moodle.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodleInfo.MoodleStatus.Persistent, false, FontAwesomeIcon.MapPin, FontAwesomeIcon.MapPin, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodleInfo.MoodleStatus.Persistent ? "Marked as a Sticky Moodle." : "Not Sticky.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(!string.IsNullOrEmpty(moodleInfo.MoodleStatus.CustomVFXPath), false, FontAwesomeIcon.Magic, FontAwesomeIcon.Magic, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(!string.IsNullOrEmpty(moodleInfo.MoodleStatus.CustomVFXPath) ? "Has a custom VFX path." : "No custom VFX path.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodleInfo.MoodleStatus.StackOnReapply, false, FontAwesomeIcon.SortNumericUpAlt, FontAwesomeIcon.SortNumericUpAlt, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodleInfo.MoodleStatus.StackOnReapply ? "Stacks " + moodleInfo.MoodleStatus.StacksIncOnReapply + " times on Reapplication." : "Doesn't stack on reapplication.");
            }

            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FontAwesomeIcon.Tags);
                ImUtf8.SameLineInner();
                var maxWidth = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X;
                var tagsString = string.Join(", ", moodleInfo.Tags);
                if (ImGui.CalcTextSize(tagsString).X > maxWidth)
                {
                    tagsString = tagsString.Substring(0, (int)(maxWidth / ImGui.CalcTextSize("A").X)) + "...";
                }
                CkGui.ColorText(tagsString, ImGuiColors.ParsedGrey);
            }
            CkGui.AttachToolTip("Tags for the Pattern");

            try
            {
                if (moodleInfo.MoodleStatus.IconID != 0 && imagePos != Vector2.Zero)
                {
                    var statusIcon = _ckGui.GetGameStatusIcon((uint)((uint)moodleInfo.MoodleStatus.IconID + moodleInfo.MoodleStatus.Stacks - 1));

                    if (statusIcon is { } wrap)
                    {
                        ImGui.SetCursorPos(imagePos);
                        ImGui.Image(statusIcon.ImGuiHandle, MoodleStatusMonitor.DefaultSize);
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
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var updateSize = CkGui.IconTextButtonSize(FontAwesomeIcon.Search, "Search");
        var sortIconSize = CkGui.IconButtonSize(FontAwesomeIcon.SortAmountUp).X;
        var filterTypeSize = 80f;
        var sortIcon = _shareHub.SearchSort == SearchSort.Ascending ? FontAwesomeIcon.SortAmountUp : FontAwesomeIcon.SortAmountDown;

        // Draw out the first row. This contains the search bar, the Update Search Button, and the Show 
        using (ImRaii.Group())
        {
            ImGui.SetNextItemWidth(CkGui.GetWindowContentRegionWidth() - sortIconSize - filterTypeSize - updateSize - 3 * spacing);
            var searchString = _shareHub.SearchString;
            if (ImGui.InputTextWithHint("##moodleSearchFilter", "Search for Moodles...", ref searchString, 125))
                _shareHub.SearchString = searchString;
            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FontAwesomeIcon.Search, "Search", disabled: !_shareHub.CanShareHubTask))
                _shareHub.PerformMoodleSearch();
            CkGui.AttachToolTip("Update Search Results");

            // Show the filter combo.
            ImUtf8.SameLineInner();
            if(CkGuiUtils.EnumCombo("##filterType", filterTypeSize, _shareHub.SearchFilter, out ResultFilter newFilterType, skip: 1))
                _shareHub.SearchFilter = newFilterType;
            CkGui.AttachToolTip("Sort Method--SEP--Define how results are found.");

            // the sort direction.
            ImUtf8.SameLineInner();
            if (CkGui.IconButton(sortIcon))
                _shareHub.ToggleSortDirection();
            CkGui.AttachToolTip("Sort Direction--SEP--Current: " + _shareHub.SearchSort + "");
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
            CkGui.DrawComboSearchable("##moodleTagsFilter", tagsComboWidth, _shareHub.FetchedTags.ToImmutableList(), (i) => i, false,
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
            CkGui.AttachToolTip("Select from an existing list of tags." +
                "--SEP--This will help make your Moodle easier to find.");
        }
    }
}

