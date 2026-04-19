using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Interop.Helpers;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using LociApi.Enums;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;
using System.Globalization;

namespace GagSpeak.Gui.Publications;

// Wow this manager is a mess holy moly. Really needs a rework lol.
public class PublicationsManager
{
    private readonly PatternHubService _patternHub;
    private readonly LociHubService _lociHub;
    public PublicationsManager(ILogger<PublicationsManager> logger, GagspeakMediator mediator,
        FavoritesConfig favorites, PatternManager patterns, PatternHubService patternhub, LociHubService lociHub)
    {
        _patternHub = patternhub;
        _lociHub = lociHub;

        _patternCombo = new PatternCombo(logger, mediator, favorites, () => [
            ..patterns.Storage.OrderByDescending(p => FavoritesConfig.Patterns.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);

        _statusCombo = new LociStatusCombo(logger, 1.5f);
    }

    private HashSet<string> _searchableTagList => _tagList
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim().ToLower())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .ToHashSet();

    private string _authorName = string.Empty;
    private string _tagList = string.Empty;
    private readonly PatternCombo _patternCombo;
    private readonly LociStatusCombo _statusCombo;
    private Pattern _selectedPattern = Pattern.AsEmpty();

    public void DrawPatternPublications()
    {
        CkGui.FontText("Publish A Pattern", Fonts.UidFont);

        _patternCombo.Draw("##pattern-selector", _selectedPattern.Identifier, 200f);

        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        CkGui.ColorText("Chosen Pattern", ImGuiColors.ParsedGold);
        
        ImGui.SetNextItemWidth(200f);
        ImGui.InputTextWithHint("##AuthorName", "Author DisplayName...", ref _authorName, 50);
        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        CkGui.ColorText("Author Name", ImGuiColors.ParsedGold);

        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##shareHubTags", "Enter tags split by , (optional)", ref _tagList, 250);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _tagList = _tagList.ToLower();
            var commaCount = _tagList.Count(c => c == ',');
            if (commaCount > 4)
            {
                var tags = _tagList.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim()).ToArray();
                _tagList = string.Join(", ", tags.Take(5));
            }
        }
        CkGui.AttachTooltip("You can have a maximum of 5 tags."); 
        
        ImUtf8.SameLineInner();
        DrawTagCombo("##patternTagsFilter", ImGui.GetContentRegionAvail().X, (tag) =>
        {
            var commaCount = _tagList.Count(c => c == ',');
            if (commaCount >= 4) return;

            // Handle new tab
            if (!_tagList.Contains(tag))
            {
                if (_tagList.Length > 0 && _tagList[^1] != ',')
                    _tagList += ",";
                _tagList += tag.ToLower();
            }
        });

        var blockUpload = _authorName.IsNullOrEmpty() || _selectedPattern.Identifier == Guid.Empty || PatternHubService.InUpdate;
        if (CkGui.IconTextButton(FAI.CloudUploadAlt, "Publish Pattern to ShareHub", ImGui.GetContentRegionAvail().X, false, blockUpload))
            _patternHub.Upload(_selectedPattern, _authorName, _searchableTagList);
        CkGui.AttachTooltip("Must have a selected pattern and author name to upload.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGuiUtil.Center("Your Currently Published Patterns");
        ImGui.Separator();

        // push the style for the more thin scrollbar.
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        // draw the displayList section.
        using var child = ImRaii.Child("##PublishedPatternsList", ImGui.GetContentRegionAvail(), false);
        DrawPublishedPatternList();
    }

    private void DrawPublishedPatternList()
    {
        if (_patternHub.Publications.Count is 0)
        {
            ImGui.TextUnformatted("No Patterns Published.");
            return;
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f);
        using var col = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.TankBlue)
            .Push(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.3f, 0.4f));

        foreach (var pattern in _patternHub.Publications.ToList())
            PublishedPatternItem(pattern);
    }

    private void PublishedPatternItem(PublishedPattern pattern)
    {
        var unpublishButton = CkGui.IconTextButtonSize(FAI.Globe, "Unpublish");
        var height = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##PatternResult_{pattern.Identifier}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, WFlags.ChildWindow))
        {

            using (ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                ImGui.AlignTextToFramePadding();
                CkGui.ColorText(pattern.Label, ImGuiColors.DalamudWhite);
                if (!pattern.Description.IsNullOrEmpty()) CkGui.HelpText(pattern.Description, true);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - unpublishButton);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink))
                    if (CkGui.IconTextButton(FAI.Globe, "Unpublish", isInPopup: true, disabled: !ImGui.GetIO().KeyShift || PatternHubService.InUpdate))
                        _patternHub.Unpublish(pattern.Identifier);
                CkGui.AttachTooltip("Removes this pattern publication from the pattern hub.--SEP--Must hold SHIFT");
            }
            // next line:
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FAI.UserCircle);
                ImUtf8.SameLineInner();
                CkGui.ColorText(pattern.Author, ImGuiColors.DalamudGrey);
                CkGui.AttachTooltip("The Author Name you gave yourself when publishing this pattern.");
                ImGui.SameLine();
                CkGui.ColorText($"({pattern.UploadedDate.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)})", ImGuiColors.DalamudGrey);
                CkGui.AttachTooltip("The date this pattern was published.");

                var formatDuration = pattern.Length.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
                var timerText = pattern.Length.ToString(formatDuration);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(timerText).X - CkGui.IconSize(FAI.Stopwatch).X - ImGui.GetStyle().ItemSpacing.X);

                using (ImRaii.Group())
                {
                    CkGui.IconText(FAI.Stopwatch);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted(pattern.Length.ToString(formatDuration));
                }
                CkGui.AttachTooltip("Total Pattern Duration");
            }
        }
    }


    public void DrawLociPublications()
    {
        if (LociCache.Data.Statuses.Count <= 0)
        {
            CkGui.ColorText("No Ipc Data Available.", ImGuiColors.DalamudRed);
        }
        else
        {
            // draw the create section.
            CkGui.FontText("Publish Loci Status", Fonts.UidFont);

            _statusCombo.Draw("##status-selector", 200f);
            CkGui.ColorTextFrameAlignedInline("Chosen Status", ImGuiColors.ParsedGold);

            ImGui.SetNextItemWidth(200f);
            ImGui.InputTextWithHint("##author-name", "Author DisplayName...", ref _authorName, 50);
            CkGui.AttachTooltip("The name displayed on the ShareHub as the publisher of this Status.");

            CkGui.ColorTextFrameAlignedInline("Author Name", ImGuiColors.ParsedGold);

            ImGui.SetNextItemWidth(200);
            ImGui.InputTextWithHint("##loci-sharehub-tags", "Enter tags split by `,` (optional)", ref _tagList, 250);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _tagList = _tagList.ToLower();
                var commaCount = _tagList.Count(c => c == ',');
                if (commaCount > 4)
                {
                    var tags = _tagList.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim()).ToArray();
                    _tagList = string.Join(", ", tags.Take(5));
                }
            }
            CkGui.AttachTooltip("You can have a maximum of 5 tags.");

            ImUtf8.SameLineInner();
            DrawTagCombo("##loci-tags-filter", ImGui.GetContentRegionAvail().X, (tag) =>
            {
                var commaCount = _tagList.Count(c => c == ',');
                if (commaCount >= 4)
                    return;

                // Handle new tab
                if (!_tagList.Contains(tag))
                {
                    if (_tagList.Length > 0 && _tagList[^1] != ',')
                        _tagList += ",";
                    _tagList += tag.ToLower();
                }
            });
            CkGui.AttachTooltip("Select an existing tag on the Server.--SEP--This makes it easier for people to find your Status!");

            var blockUpload = _authorName.IsNullOrEmpty() || _statusCombo.Current.GUID == Guid.Empty || LociHubService.InUpdate;

            if (CkGui.IconTextButton(FAI.CloudUploadAlt, "Publish Status to Sharehub", ImGui.GetContentRegionAvail().X, false, blockUpload))
                _lociHub.Upload(_statusCombo.Current, _authorName, _searchableTagList);
            CkGui.AttachTooltip("Must have a selected Status and valid name to upload.");

            ImGui.Spacing();
            ImGui.Separator();
            CkGui.FontTextCentered("Your Published Loci Statuses", Fonts.GagspeakLabelFont);
            ImGui.Separator();
        }

        // push the style for the more thin scrollbar.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var _ = ImRaii.Child("published-statuses", ImGui.GetContentRegionAvail(), false);
        DrawUploadedStatuses();
    }

    private void DrawUploadedStatuses()
    {
        if (_lociHub.Publications.Count is 0)
        {
            ImGui.TextUnformatted("Nothing Published.");
            return;
        }

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f);
        using var col = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.TankBlue)
            .Push(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.3f, 0.4f));

        foreach (var lociStatus in _lociHub.Publications.ToList())
            DrawUploadedStatus(lociStatus);
    }

    private void DrawUploadedStatus(PublishedLociData lociData)
    {
        var modifiers = (Modifiers)lociData.Status.Modifiers;
        var unpublishButton = CkGui.IconTextButtonSize(FAI.Globe, "Unpublish");
        var height = ImGui.GetFrameHeight() * 2.25f + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"loci-upload-{lociData.Status.GUID}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, WFlags.ChildWindow))
        {
            var min = ImGui.GetCursorScreenPos();
            using (ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                ImGuiHelpers.ScaledDummy(LociIcon.SizeFramed);
                CkGui.ColorTextFrameAlignedInline(lociData.Status.Title.StripColorTags(), ImGuiColors.DalamudWhite, false);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - unpublishButton);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink))
                    if (CkGui.IconTextButton(FAI.Globe, "Unpublish", isInPopup: true, disabled: !ImGui.GetIO().KeyShift || LociHubService.InUpdate))
                        _lociHub.Unpublish(lociData.Status.GUID);
                CkGui.AttachTooltip("Unpublish from the Loci ShareHub!--SEP--Must hold SHIFT");
            }

            ImGui.Spacing();
            using (ImRaii.Group())
            {
                var stacksSize = CkGui.IconSize(FAI.LayerGroup).X;
                var dispellableSize = CkGui.IconSize(FAI.Eraser).X;
                var permanentSize = CkGui.IconSize(FAI.Infinity).X;
                var customVfxPath = CkGui.IconSize(FAI.Magic).X;
                var stackOnReapply = CkGui.IconSize(FAI.PersonCirclePlus).X;

                using (ImRaii.Group())
                {
                    CkGui.IconTextAligned(FAI.UserCircle);
                    CkGui.ColorTextInline(lociData.AuthorName, ImGuiColors.DalamudGrey);
                }
                CkGui.AttachTooltip("The name you published this status under.");


                // jump to the right side to draw all the icon data.
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImUtf8.ItemInnerSpacing.X * 5 - stacksSize - dispellableSize - permanentSize - customVfxPath - stackOnReapply);
                CkGui.BoolIconFramed(lociData.Status.Stacks > 1, false, FAI.LayerGroup, FAI.LayerGroup, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachTooltip(lociData.Status.Stacks > 1 ? $"Has {lociData.Status.Stacks} initial stacks." : "Not a stackable Status.");

                CkGui.BoolIconFramed(modifiers.Has(Modifiers.CanDispel), true, FAI.Eraser, FAI.Eraser, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachTooltip(modifiers.Has(Modifiers.CanDispel) ? "Can be dispelled." : "Cannot be dispelled.");

                CkGui.BoolIconFramed(lociData.Status.ExpireTicks < 0, true, FAI.Infinity, FAI.Infinity, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachTooltip(lociData.Status.ExpireTicks < 0 ? "Permanent status." : "Timed status.");

                CkGui.BoolIconFramed(!string.IsNullOrEmpty(lociData.Status.CustomVFXPath), true, FAI.Magic, FAI.Magic, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachTooltip(!string.IsNullOrEmpty(lociData.Status.CustomVFXPath) ? "Has a custom VFX path." : "No custom VFX path.");

                CkGui.BoolIconFramed(modifiers.Has(Modifiers.StacksIncrease), true, FAI.PersonCirclePlus, FAI.PersonCirclePlus, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachTooltip(modifiers.Has(Modifiers.StacksIncrease) ? $"Stacks {lociData.Status.StackSteps} times." : "Doesn't stack on reapplication.");
            }

            if (lociData.Status.IconID != 0)
            {
                ImGui.SetCursorScreenPos(min);
                LociIcon.Draw(lociData.Status.IconID, lociData.Status.Stacks, LociIcon.Size);
                LociHelpers.AttachTooltip(lociData.Status, LociCache.Data);
            }
        }
    }

    private void DrawTagCombo(string label, float width, Action<string> onSelected)
    {
        ImGui.SetNextItemWidth(width);
        using var tagCombo = ImRaii.Combo(label, MainHub.SharehubTags.FirstOrDefault() ?? "Select Tag..");
        if (tagCombo)
        {
            foreach (var tag in MainHub.SharehubTags.ToList())
                if (ImGui.Selectable(tag, false))
                    onSelected(tag);
        }
    }
}
