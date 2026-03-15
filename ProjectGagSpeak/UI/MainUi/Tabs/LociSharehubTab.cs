using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Gui.Components;
using GagSpeak.Interop.Helpers;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Caches;
using GagspeakAPI.Data;
using LociApi.Enums;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class LociSharehubTab : DisposableMediatorSubscriberBase
{
    private readonly ShareHubService _shareHub;
    private HubTagsCombo _hubTags;
    private readonly TutorialService _guides;
    private readonly MainMenuTabs _tabMenu;
    public LociSharehubTab(ILogger<LociSharehubTab> logger, GagspeakMediator mediator, ShareHubService shareHub,
        TutorialService guides, MainMenuTabs tabMenu) 
        : base(logger, mediator)
    {
        _shareHub = shareHub;
        _guides = guides;
        _tabMenu = tabMenu;

        _hubTags = new HubTagsCombo(logger, () => [ ..shareHub.FetchedTags.OrderBy(x => x) ]);
    }

    public void DrawSharehub()
    {
        // Handle grabbing new info from the server if none is present. (not the most elegent but it works)
        if (!_shareHub.InitialLociCall && !UiService.DisableUI)
            UiService.SetUITask(_shareHub.SearchLociStatuses());
        
        DrawSearchFilter();
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.LociSearch, MainUI.LastPos, MainUI.LastSize);

        ImGui.Separator();
        // draw the results if there are any.
        if (_shareHub.LatestLociResults.Count <= 0)
        {
            ImGui.Spacing();
            ImGuiUtil.Center("Search something to find results!");
            return;
        }

        using (ImRaii.Child("search-results-gaurd", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
            DrawResultList();
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SearchResults, MainUI.LastPos, MainUI.LastSize, _ => _tabMenu.TabSelection = MainMenuTabs.SelectedTab.GlobalChat);
    }

    private void DrawResultList()
    {
        // set the scrollbar width to be shorter than normal
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var _ = ImRaii.Child("search-results", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysVerticalScrollbar);
        
        using var innerStyle = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.FramePadding, new Vector2(ImUtf8.FramePadding.X, 2f));
        
        using var color = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var size = new Vector2(ImGui.GetContentRegionAvail().X, CkStyle.GetFrameRowsHeight(3).AddWinPadY());
        foreach (var lociInfo in _shareHub.LatestLociResults)
            DrawLociStatusEntry(lociInfo, size);
    }

    private void DrawLociStatusEntry(ServerDataLociStatus info, Vector2 size)
    {
        var modifiers = (Modifiers)info.Status.Modifiers;

        using var _ = ImRaii.Child($"loci-status-{info.Status.GUID}", size, true, WFlags.ChildWindow);

        var tryOnButtonSize = CkGui.IconTextButtonSize(FAI.PersonCircleQuestion, "Try");
        var LikeButtonSize = CkGui.IconTextButtonSize(FAI.Heart, info.Likes.ToString());
        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var imagePos = ImGui.GetCursorPos();
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var style = ImGui.GetStyle();

        ImGui.Dummy(iconSize);
        LociHelpers.AttachTooltip(info.Status, LociCache.Data);
        CkGui.TextFrameAlignedInline(info.Status.Title.StripColorTags());

        var buttonW = tryOnButtonSize + LikeButtonSize + iconSize.X + style.ItemInnerSpacing.X * 2;
        ImGui.SameLine(windowEndX - buttonW);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            if (CkGui.IconTextButton(FAI.PersonCircleQuestion, "Try", isInPopup: true))
                _shareHub.TryLociStatus(info.Status.GUID);
        CkGui.AttachToolTip("Try this Status on yourself to preview it!");
        
        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, info.HasLiked ? ImGuiColors.ParsedPink : ImGuiColors.ParsedGrey))
            if (CkGui.IconTextButton(FAI.Heart, info.Likes.ToString(), null, true, UiService.DisableUI))
                UiService.SetUITask(_shareHub.HeartLociStatus(info.Status.GUID));
        CkGui.AttachToolTip(info.HasLiked ? "Remove Like from status." : "Like this status!");

        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            if (CkGui.IconButton(FAI.Copy, inPopup: true))
                _shareHub.CopyLociStatusToClipboard(info.Status.GUID);
        CkGui.AttachToolTip("Copy this Status to import into Loci");

        // Middle Row.
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.UserCircle);
            CkGui.ColorTextFrameAlignedInline(info.Author, ImGuiColors.DalamudGrey);
        }
        CkGui.AttachToolTip("The Status Author/Publisher");
        DrawStatusProperties();

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

        // if the status icon is valid, display it at the starting pos.
        if (info.Status.IconID != 0)
        {
            ImGui.SetCursorPos(imagePos);
            LociIcon.Draw(info.Status.IconID, info.Status.Stacks, LociIcon.Size);
        }

        void DrawStatusProperties()
        {
            var width = iconSize.X * 6 + style.ItemInnerSpacing.X * 5;
            ImGui.SameLine(windowEndX - width);
            // Draw out each color icon frame aligned.
            StatusProperty(FAI.LayerGroup, info.Status.Stacks > 1, "Stackable Status", "Status has a stack count attached");
            ImUtf8.SameLineInner();
            StatusProperty(FAI.Eraser, modifiers.Has(Modifiers.CanDispel), "Dispellable", "Cannot be dispelled");
            ImUtf8.SameLineInner(); 
            StatusProperty(FAI.Infinity, info.Status.ExpireTicks < 0, "Permanent Status", "Timed Status");
            ImUtf8.SameLineInner();
            StatusProperty(FAI.Magic, !string.IsNullOrEmpty(info.Status.CustomVFXPath), "Includes Custom VFX", "No custom VFX");
            ImUtf8.SameLineInner();
            StatusProperty(FAI.SortNumericUpAlt, modifiers.Has(Modifiers.StacksIncrease), 
                $"Adds {info.Status.StackSteps} stacks each application, starting from {info.Status.Stacks}", "Stacks remain as is.");
        }

        void StatusProperty(FAI icon, bool state, string tooltipTrue, string tooltipFalse)
        {
            CkGui.BoolIcon(state, false, icon, icon, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(state ? tooltipTrue : tooltipFalse);
        } 
    }

    public void DrawSearchFilter()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        using var _ = ImRaii.Group();
    
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var updateSize = CkGui.IconTextButtonSize(FAI.Search, "Search");
        var sortIconSize = CkGui.IconButtonSize(FAI.SortAmountUp).X;
        var filterTypeSize = 80f * ImGuiHelpers.GlobalScale;

        var sortIcon = _shareHub.SortOrder == HubDirection.Ascending ? FAI.SortAmountUp : FAI.SortAmountDown;
        // Draw out the first row. This contains the search bar, the Update Search Button, and the Show 
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (sortIconSize + filterTypeSize + updateSize + spacing * 3));
        var searchString = _shareHub.SearchString;
        if (ImGui.InputTextWithHint("##status-search-filter", "Search for Statuses...", ref searchString, 125))
            _shareHub.SearchString = searchString;
        
        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Search, "Search", disabled: UiService.DisableUI))
            UiService.SetUITask(_shareHub.SearchLociStatuses());
        CkGui.AttachToolTip("Update Results");

        // Show the filter combo.
        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##filter-type", filterTypeSize, _shareHub.SearchFilter, out var newFilterType, [HubFilter.Likes, HubFilter.DatePosted], flags: CFlags.NoArrowButton))
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
        CkGui.AttachToolTip("Select from an existing list of tags.--NL----COL--(Helps make statuses easier to find)", ImGuiColors.DalamudGrey2);

        if (pressed)
            ImGui.OpenPopup("##loci-sharehub-tags");

        // open the popup if we should.
        if (_hubTags.DrawPopup("##loci-sharehub-tags", 200f, popupDrawPos))
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

