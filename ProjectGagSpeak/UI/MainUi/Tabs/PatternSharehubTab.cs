using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;
using System.Globalization;
using GagSpeak.WebAPI;

namespace GagSpeak.Gui.MainWindow;
public class PatternSharehubTab : DisposableMediatorSubscriberBase
{
    private readonly PatternManager _patterns;
    private readonly PatternHubService _shareHub;
    private readonly TutorialService _guides;
    
    private HubTagsCombo _hubTagCombo;

    private string _searchStr = string.Empty;
    private string _searchTags = string.Empty;

    public PatternSharehubTab(ILogger<PatternSharehubTab> logger, GagspeakMediator mediator,
        PatternManager patterns, PatternHubService shareHub, TutorialService guides) 
        : base(logger, mediator)
    {
        _patterns = patterns;
        _shareHub = shareHub;
        _guides = guides;

        _hubTagCombo = new HubTagsCombo(logger, () => [ ..MainHub.SharehubTags.OrderBy(x => x) ]);
    }

    public void DrawSharehub()
    {
        DrawSearchFilter();
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternSearch, MainUI.LastPos, MainUI.LastSize);
        
        ImGui.Separator();
        // draw the results if there are any.
        if (_shareHub.SearchResults.Count <= 0)
        {
            ImGui.Spacing();
            ImGuiUtil.Center("Search something to find results!");
            return;
        }

        using (ImRaii.Child("search-result-wrapper", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
            DrawResultList();
    }

    private void DrawResultList()
    {
        // set the scrollbar width to be shorter than normal
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var _ = ImRaii.Child("search-results", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysVerticalScrollbar);
        // result styles.
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, 2f));
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var size = new Vector2(ImGui.GetContentRegionAvail().X, CkStyle.GetFrameRowsHeight(3).AddWinPadY());
        foreach (var pattern in _shareHub.SearchResults)
            DrawPatternResultBox(pattern, size);
    }

    private void DrawPatternResultBox(SharehubPattern info, Vector2 size)
    {
        using var _ = ImRaii.Child($"Pattern-{info.Identifier}", size, true, WFlags.ChildWindow);
        
        var style = ImGui.GetStyle();
        var iconSize = new Vector2(ImGui.GetFrameHeight());
        var windowEndX = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth();
        var wdl = ImGui.GetWindowDrawList();

        // draw devices used.
        if (info.PrimaryDeviceUsed is not ToyBrandName.Unknown)
        {
            DrawToyIcon(info.PrimaryDeviceUsed.FromBrandName(), $"Primary: ({info.PrimaryDeviceUsed.ToName()})");
            ImUtf8.SameLineInner();
        }
        if (info.SecondaryDeviceUsed is not ToyBrandName.Unknown)
        {
            DrawToyIcon(info.SecondaryDeviceUsed.FromBrandName(), $"Secondary: ({info.SecondaryDeviceUsed.ToName()})");
            ImUtf8.SameLineInner();
        }

        ImUtf8.TextFrameAligned(info.Label);
        CkGui.AttachToolTip(string.IsNullOrEmpty(info.Description) ? "No Description Set" : info.Description);
        // display name, then display the downloads and likes on the other side.

        var buttonW = CkGui.IconTextButtonSize(FAI.Heart, info.Likes.ToString()) + CkGui.IconTextButtonSize(FAI.Download, info.Downloads.ToString()) + style.ItemInnerSpacing.X;
        ImGui.SameLine(windowEndX - buttonW + style.FramePadding.X);

        using (ImRaii.PushColor(ImGuiCol.Text, info.HasLiked ? ImGuiColors.ParsedPink : ImGuiColors.DalamudGrey))
            if (CkGui.IconTextButton(FAI.Heart, info.Likes.ToString(), null, true, PatternHubService.InUpdate))
                _shareHub.ToggleLike(info.Identifier);
        CkGui.AttachToolTip(info.HasLiked ? "Remove Like from this pattern." : "Like this pattern!");
            
        ImUtf8.SameLineInner();
        var disable = _patterns.Storage.Contains(info.Identifier) || PatternHubService.InUpdate;
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite2))
            if (CkGui.IconTextButton(FAI.Download, info.Downloads.ToString(), null, true, disable, $"DownloadPattern-{info.Identifier}"))
                _shareHub.Download(info.Identifier);
        CkGui.AttachToolTip("Download this pattern!");

        // Middle Row.
        DrawAuthor();
        DrawLength();

        // Bottom Row.
        DrawTags();
        CkGui.AttachToolTip("Tags for the Pattern");
        DrawMotors();

        void DrawAuthor()
        {
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(FAI.UserCircle);
                CkGui.ColorTextFrameAlignedInline($"{info.Author} ({info.UploadedDate.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)})", ImGuiColors.DalamudGrey);
            }
            CkGui.AttachToolTip("Publisher of the Pattern");
        }

        void DrawLength()
        {
            var formatDuration = info.Length.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
            var timerText = info.Length.ToString(formatDuration);
            ImGui.SameLine(windowEndX - ImGui.CalcTextSize(timerText).X - ImGui.GetFrameHeight() - style.ItemInnerSpacing.X);
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(FAI.Stopwatch);
                CkGui.TextFrameAlignedInline(info.Length.ToString(formatDuration));
            }
            CkGui.AttachToolTip("Total Pattern Duration");
        }

        void DrawToyIcon(CoreIntifaceElement icon, string tooltip)
        {
            ImGui.Dummy(iconSize);
            wdl.AddDalamudImage(CosmeticService.IntifaceTextures.Cache[icon], ImGui.GetItemRectMin(), iconSize);
            CkGui.AttachToolTip(tooltip);
        }

        void DrawTags()
        {
            var allowedLength = ImGui.GetContentRegionAvail().X * .7f;
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(FAI.Tags);
                var tagsString = string.Join(", ", info.Tags);
                if (ImGui.CalcTextSize(tagsString).X > allowedLength)
                    tagsString = tagsString.Substring(0, (int)(allowedLength / ImGui.CalcTextSize("A").X)) + "...";
                CkGui.ColorTextFrameAlignedInline(tagsString, ImGuiColors.ParsedGrey);
            }
            CkGui.AttachToolTip("Tags for the Pattern");
        }

        void DrawMotors()
        {
            var spacingX = style.ItemInnerSpacing.X / 2;
            var currentRightSide = windowEndX - iconSize.X;
            foreach (var motor in Enum.GetValues<ToyMotor>().Skip(1))
            {
                if (!info.MotorsUsed.HasAny(motor))
                    continue;
                ImGui.SameLine(currentRightSide);
                // motor found, draw it.
                DrawToyIcon(motor.FromMotor(), $"{motor.ToString()} Motor(s) Used");
                // shift over
                currentRightSide -= (iconSize.X + spacingX);
            }
        }
    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(2, ImGui.GetStyle().ItemInnerSpacing.Y));
        using var _ = ImRaii.Group();

        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var search = CkGui.IconTextButtonSize(FAI.Search, "Search");
        var sort = CkGui.IconButtonSize(FAI.SortAmountUp).X;
        var tags = CkGui.IconButtonSize(FAI.Tags).X;
        var filter = 80f * ImGuiHelpers.GlobalScale;
        var duration = 60f * ImGuiHelpers.GlobalScale;
        
        var sortIcon = _shareHub.SortDirection == SortDirection.Ascending ? FAI.SortAmountUp : FAI.SortAmountDown;

        // Draw out the virst fow.
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (search + filter + sort + spacing * 3));
        ImGui.InputTextWithHint("##search-filter", "Search for Patterns...", ref _searchStr, 125);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Search, "Search", disabled: PatternHubService.InUpdate))
            _shareHub.Search(_searchStr, _searchTags);
        CkGui.AttachToolTip("Update Search Results");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##filterType", filter, _shareHub.SortBy, out var newType, Enum.GetValues<HubSortBy>().SkipLast(1), flags: CFlags.NoArrowButton))
            _shareHub.SortBy = newType;
        CkGui.AttachToolTip("Sort Method--SEP--Define how results are found.");

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(sortIcon))
            _shareHub.ToggleSortDirection();
        CkGui.AttachToolTip($"Sort Direction--SEP--Current: {_shareHub.SortDirection}");

        // NEXT ROW.
        var buttonWidth = duration + tags + spacing * 2;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - buttonWidth);
        ImGui.InputTextWithHint("##search-tags", "Enter tags split by , (optional)", ref _searchTags, 200);

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##DurationFilter", duration, _shareHub.DurationFilter, out var newLength, i => i.ToName(), flags: CFlags.NoArrowButton))
            _shareHub.DurationFilter = newLength;
        CkGui.AttachToolTip("Time Range");

        ImUtf8.SameLineInner();
        var pressed = CkGui.IconButton(FAI.Tags);
        var popupDrawPos = ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0);
        CkGui.AttachToolTip("Select from an existing list of tags.--NL----COL--(Helps make patterns easier to find)", ImGuiColors.DalamudGrey2);


        if (pressed)
            ImGui.OpenPopup("##pattern-sharehub-tags");

        // open the popup if we should.
        if (_hubTagCombo.DrawPopup("##pattern-sharehub-tags", 200f, popupDrawPos))
        {
            if (_hubTagCombo.Current is not { } selected)
                return;

            if (string.IsNullOrWhiteSpace(selected) || _searchTags.Contains(selected))
                return;

            // Append the tag.
            if (_searchTags.Length > 0 && _searchTags[^1] != ',')
                _searchTags += ", ";
            // append the tag to it.
            _searchTags += selected.ToLower();
        }
    }
}

