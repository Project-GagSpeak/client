using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Textures;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MoodleHubTab : DisposableMediatorSubscriberBase
{
    private readonly ShareHubService _shareHub;
    private HubTagsCombo _hubTags;
    private readonly TutorialService _guides;
    private readonly MainMenuTabs _tabMenu;
    public MoodleHubTab(ILogger<MoodleHubTab> logger, GagspeakMediator mediator, ShareHubService shareHub,
        TutorialService guides, MainMenuTabs tabMenu) 
        : base(logger, mediator)
    {
        _shareHub = shareHub;
        _guides = guides;
        _tabMenu = tabMenu;

        _hubTags = new HubTagsCombo(logger, () => [ ..shareHub.FetchedTags.OrderBy(x => x) ]);
    }

    public void DrawMoodlesHub()
    {
        CkGui.FontTextCentered("Database Broken Due", Fonts.Default150Percent, CkCol.TriStateCross.Uint());
        CkGui.FontTextCentered("To Moodles Changes", Fonts.Default150Percent, CkCol.TriStateCross.Uint());
        using (ImRaii.Disabled())
        {

            // Handle grabbing new info from the server if none is present. (not the most elegent but it works)
            if (!_shareHub.InitialMoodlesCall && !UiService.DisableUI)
                UiService.SetUITask(_shareHub.SearchMoodles());

            using (ImRaii.Group())
            {
                DrawSearchFilter();
            }
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.MoodleSearch, MainUI.LastPos, MainUI.LastSize);

        using (ImRaii.Disabled())
        {
            ImGui.Separator();

            // draw the results if there are any.
            if (_shareHub.LatestMoodleResults.Count <= 0)
            {
                ImGui.Spacing();
                ImGuiUtil.Center("Search something to find results!");
                return;
            }

            using (ImRaii.Child("ResultListGuard", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
                DrawResultList();
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.MoodleResults, MainUI.LastPos, MainUI.LastSize,
            () => _tabMenu.TabSelection = MainMenuTabs.SelectedTab.GlobalChat);
    }

    private void DrawResultList()
    {
        // set the scrollbar width to be shorter than normal
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var patternResultChild = ImRaii.Child("##PatternResultChild", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysVerticalScrollbar);
        // result styles.
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, 2f));
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var size = new Vector2(ImGui.GetContentRegionAvail().X, CkStyle.GetFrameRowsHeight(3).AddWinPadY());
        foreach (var pattern in _shareHub.LatestMoodleResults)
            DrawMoodleResultBox(pattern, size);
    }

    private void DrawMoodleResultBox(ServerMoodleInfo info, Vector2 size)
    {
        using var _ = ImRaii.Child($"Moodle-{info.Status.GUID}", size, true, WFlags.ChildWindow);

        var tryOnButtonSize = CkGui.IconTextButtonSize(FAI.PersonCircleQuestion, "Try");
        var LikeButtonSize = CkGui.IconTextButtonSize(FAI.Heart, info.Likes.ToString());
        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var imagePos = ImGui.GetCursorPos();
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var style = ImGui.GetStyle();

        ImGui.Dummy(iconSize);
        GagspeakEx.DrawMoodleStatusTooltip(info.Status, Enumerable.Empty<MoodlesStatusInfo>());
        CkGui.TextFrameAlignedInline(info.Status.Title.StripColorTags());

        var buttonW = tryOnButtonSize + LikeButtonSize + iconSize.X + style.ItemInnerSpacing.X * 2;
        ImGui.SameLine(windowEndX - buttonW);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            if (CkGui.IconTextButton(FAI.PersonCircleQuestion, "Try", isInPopup: true))
                _shareHub.TryOnMoodle(info.Status.GUID);
        CkGui.AttachToolTip("Try this Moodle on your character to see a preview of it.");
        
        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, info.HasLikedMoodle ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey))
            if (CkGui.IconTextButton(FAI.Heart, info.Likes.ToString(), null, true, UiService.DisableUI))
                UiService.SetUITask(_shareHub.LikeMoodle(info.Status.GUID));
        CkGui.AttachToolTip(info.HasLikedMoodle ? "Remove Like from this pattern." : "Like this pattern!");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            if (CkGui.IconButton(FAI.Copy, inPopup: true))
                _shareHub.CopyMoodleToClipboard(info.Status.GUID);
        CkGui.AttachToolTip("Copy this Status for simple Moodles Import!");

        // Middle Row.
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.UserCircle);
            CkGui.ColorTextFrameAlignedInline(info.Author, ImGuiColors.DalamudGrey);
        }
        CkGui.AttachToolTip("Publisher of the Moodle");
        DrawMoodleEffects();

        // Final Row.
        using (ImRaii.Group())
        {
            var allowedLength = ImGui.GetContentRegionAvail().X - style.ItemSpacing.X;
            CkGui.FramedIconText(FAI.Tags);
            var tagsString = string.Join(", ", info.Tags);
            if (ImGui.CalcTextSize(tagsString).X > allowedLength)
                tagsString = tagsString.Substring(0, (int)(allowedLength / ImGui.CalcTextSize("A").X)) + "...";
            CkGui.ColorTextFrameAlignedInline(tagsString, ImGuiColors.ParsedGrey);
        }
        CkGui.AttachToolTip("Associated Tags");

        // if the moodle image is valid, display it at the starting pos.
        if (info.Status.IconID != 0)
        {
            ImGui.SetCursorPos(imagePos);
            MoodleIcon.DrawMoodleIcon(info.Status.IconID, info.Status.Stacks, MoodleDrawer.IconSize);
        }

        void DrawMoodleEffects()
        {
            var width = iconSize.X * 6 + style.ItemInnerSpacing.X * 5;
            ImGui.SameLine(windowEndX - width);
            // Draw out each color icon frame aligned.
            MoodleEffect(FAI.LayerGroup, info.Status.Stacks > 1, "Effect Stacks", "Not a stackable Moodle");
            ImUtf8.SameLineInner();
            MoodleEffect(FAI.Eraser, info.Status.Modifiers.Has(Modifiers.CanDispel), "Can be dispelled", "Cannot be dispelled");
            ImUtf8.SameLineInner(); 
            MoodleEffect(FAI.Infinity, info.Status.ExpireTicks < 0, "Permanent Moodle", "Temporary Moodle");
            ImUtf8.SameLineInner(); 
            MoodleEffect(FAI.MapPin, info.Status.Permanent, "Is Sticky", "Not Sticky");
            ImUtf8.SameLineInner();
            MoodleEffect(FAI.Magic, !string.IsNullOrEmpty(info.Status.CustomVFXPath), "Has custom VFX", "No custom VFX");
            ImUtf8.SameLineInner();
            MoodleEffect(FAI.SortNumericUpAlt, info.Status.Modifiers.Has(Modifiers.StacksIncrease), 
                $"Adds {info.Status.Modifiers.Has(Modifiers.StacksIncrease)} stacks each application", "Stacks remain as is.");
        }

        void MoodleEffect(FAI icon, bool state, string tooltipTrue, string tooltipFalse)
        {
            CkGui.BooleanToColoredIcon(state, false, icon, icon, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(state ? tooltipTrue : tooltipFalse);
        }
    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var updateSize = CkGui.IconTextButtonSize(FAI.Search, "Search");
        var sortIconSize = CkGui.IconButtonSize(FAI.SortAmountUp).X;
        var filterTypeSize = 80f * ImGuiHelpers.GlobalScale;

        var sortIcon = _shareHub.SortOrder == HubDirection.Ascending ? FAI.SortAmountUp : FAI.SortAmountDown;
        // Draw out the first row. This contains the search bar, the Update Search Button, and the Show 
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (sortIconSize + filterTypeSize + updateSize + spacing * 3));
        var searchString = _shareHub.SearchString;
        if (ImGui.InputTextWithHint("##moodleSearchFilter", "Search for Moodles...", ref searchString, 125))
            _shareHub.SearchString = searchString;
        
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Search, "Search", disabled: UiService.DisableUI))
            UiService.SetUITask(_shareHub.SearchMoodles());
        CkGui.AttachToolTip("Update Search Results");

        // Show the filter combo.
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##filterType", filterTypeSize, _shareHub.SearchFilter, out var newFilterType, [HubFilter.Likes, HubFilter.DatePosted], flags: CFlags.NoArrowButton))
            _shareHub.SearchFilter = newFilterType;
        CkGui.AttachToolTip("Sort Method--SEP--Define how results are found.");

        // the sort direction.
        ImUtf8.SameLineInner();
        if (CkGui.IconButton(sortIcon))
            _shareHub.ToggleSortDirection();
        CkGui.AttachToolTip($"Sort Direction--SEP--Current: {_shareHub.SortOrder}");
        
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.Tags).X - ImGui.GetStyle().ItemInnerSpacing.X);
        var searchTags = _shareHub.SearchTags;
        if (ImGui.InputTextWithHint("##shareHubTags", "Enter tags split by , (optional)", ref searchTags, 200))
            _shareHub.SearchTags = searchTags;
        // the combo.
        ImUtf8.SameLineInner();
        var pressed = CkGui.IconButton(FAI.Tags);
        var popupDrawPos = ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0);
        CkGui.AttachToolTip("Select from an existing list of tags." +
            "--SEP--This will help make your Moodle easier to find.");

        if (pressed)
            ImGui.OpenPopup("##MoodleHubTags");

        // open the popup if we should.
        if (_hubTags.DrawPopup("##MoodleHubTags", 200f, popupDrawPos))
        {
            if (_hubTags.Current is not { } selected)
                return;

            if (string.IsNullOrWhiteSpace(selected) || _shareHub.SearchTags.Contains(selected))
                return;

            // Append the tag.
            if (_shareHub.SearchTags.Length > 0 && _shareHub.SearchTags[^1] != ',')
                _shareHub.SearchTags += ", ";
            // append the tag to it.
            _shareHub.SearchTags += selected.ToLower();
        }
    }
}

