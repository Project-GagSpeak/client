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

namespace GagSpeak.Gui.MainWindow;
public class PatternHubTab : DisposableMediatorSubscriberBase
{
    private readonly PatternManager _patterns;
    private readonly ShareHubService _shareHub;
    private readonly TutorialService _guides;
    private HubTagsCombo _hubTags;
    public PatternHubTab(ILogger<PatternHubTab> logger, GagspeakMediator mediator,
        PatternManager patterns, ShareHubService shareHub, TutorialService guides) 
        : base(logger, mediator)
    {
        _patterns = patterns;
        _shareHub = shareHub;
        _guides = guides;

        _hubTags = new HubTagsCombo(logger, () => [ .._shareHub.FetchedTags.OrderBy(x => x) ]);
    }

    public void DrawPatternHub()
    {
        if (!_shareHub.InitialPatternsCall && !UiService.DisableUI)
            UiService.SetUITask(_shareHub.SearchPatterns());
        
        using(ImRaii.Group())
        {
            DrawSearchFilter();
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternSearch, MainUI.LastPos, MainUI.LastSize);
        ImGui.Separator();
        // draw the results if there are any.
        if (_shareHub.LatestPatternResults.Count <= 0)
        {
            ImGui.Spacing();
            ImGuiUtil.Center("Search something to find results!");
            return;
        }
        using (ImRaii.Child("ResultListGuard", ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar))
            DrawResultList();
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
        foreach (var pattern in _shareHub.LatestPatternResults)
            DrawPatternResultBox(pattern, size);
    }

    private void DrawPatternResultBox(ServerPatternInfo info, Vector2 size)
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
            if (CkGui.IconTextButton(FAI.Heart, info.Likes.ToString(), null, true, UiService.DisableUI))
                UiService.SetUITask(_shareHub.LikePattern(info.Identifier));
        CkGui.AttachToolTip(info.HasLiked ? "Remove Like from this pattern." : "Like this pattern!");
            
        ImUtf8.SameLineInner();
        var disable = _patterns.Storage.Contains(info.Identifier) || UiService.DisableUI;
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudWhite2))
            if (CkGui.IconTextButton(FAI.Download, info.Downloads.ToString(), null, true, disable, $"DownloadPattern-{info.Identifier}"))
                UiService.SetUITask(_shareHub.DownloadPattern(info.Identifier));
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
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var search = CkGui.IconTextButtonSize(FAI.Search, "Search");
        var sort = CkGui.IconButtonSize(FAI.SortAmountUp).X;
        var tags = CkGui.IconButtonSize(FAI.Tags).X;
        var filter = 80f * ImGuiHelpers.GlobalScale;
        var duration = 60f * ImGuiHelpers.GlobalScale;
        
        var sortIcon = _shareHub.SortOrder == HubDirection.Ascending ? FAI.SortAmountUp : FAI.SortAmountDown;

        // Draw out the virst fow.
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (search + filter + sort + spacing * 3));
        var searchString = _shareHub.SearchString;
        if (ImGui.InputTextWithHint("##patternSearchFilter", "Search for Patterns...", ref searchString, 125))
            _shareHub.SearchString = searchString;
        // _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHubSearch, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Search, "Search", disabled: UiService.DisableUI))
            UiService.SetUITask(_shareHub.SearchPatterns());
        CkGui.AttachToolTip("Update Search Results");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##filterType", filter, _shareHub.SearchFilter, out var newType, Enum.GetValues<HubFilter>().SkipLast(1), flags: CFlags.NoArrowButton))
            _shareHub.SearchFilter = newType;
        CkGui.AttachToolTip("Sort Method--SEP--Define how results are found.");

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(sortIcon))
            _shareHub.ToggleSortDirection();
        CkGui.AttachToolTip($"Sort Direction--SEP--Current: {_shareHub.SortOrder}");

        // NEXT ROW.
        var buttonWidth = duration + tags + spacing * 2;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - buttonWidth);
        var searchTags = _shareHub.SearchTags;
        if (ImGui.InputTextWithHint("##shareHubTags", "Enter tags split by , (optional)", ref searchTags, 200))
            _shareHub.SearchTags = searchTags;

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo("##DurationFilter", duration, _shareHub.SearchDuration, out var newLength, i => i.ToName(), flags: CFlags.NoArrowButton))
            _shareHub.SearchDuration = newLength;
        CkGui.AttachToolTip("Time Range");

        ImUtf8.SameLineInner();
        var pressed = CkGui.IconButton(FAI.Tags);
        var popupDrawPos = ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0);
        CkGui.AttachToolTip("Select from an existing list of tags." +
            "--SEP--This will help make your Pattern easier to find.");

        if (pressed)
            ImGui.OpenPopup("##PatternHubTags");

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

