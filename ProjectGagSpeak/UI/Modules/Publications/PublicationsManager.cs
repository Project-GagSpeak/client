using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.State.Toybox;
using GagSpeak.State.Listeners;
using GagSpeak.Services;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Text;
using System.Collections.Immutable;
using System.Globalization;
using GagSpeak.State;
using GagSpeak.Services.Textures;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.State.Caches;

namespace GagSpeak.CkCommons.Gui.Publications;
public class PublicationsManager
{
    private readonly TextureService _textures;
    private readonly ShareHubService _shareHub;
    public PublicationsManager(ILogger<PublicationsManager> logger, MoodleIcons monitor,
        FavoritesManager favorites, PatternManager patterns, TextureService textures, ShareHubService shareHub)
    {
        _textures = textures;
        _shareHub = shareHub;

        _patternCombo = new PatternCombo(logger, favorites, () => [
            ..patterns.Storage.OrderByDescending(p => favorites._favoritePatterns.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);

        _statusCombo = new MoodleStatusCombo(1.5f, monitor, logger);
    }

    private string _authorName = string.Empty;
    private string _tagList = string.Empty;
    private readonly PatternCombo _patternCombo;
    private readonly MoodleStatusCombo _statusCombo;

    private Pattern _selectedPattern = Pattern.AsEmpty();

    public void DrawPatternPublications()
    {
        CkGui.GagspeakBigText("Publish A Pattern");

        _patternCombo.Draw("PatternSelector", _selectedPattern.Identifier, 200f);

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
                var tags = _tagList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(tag => tag.Trim())
                                   .ToArray();
                _tagList = string.Join(", ", tags.Take(5));
            }
        }
        CkGui.AttachToolTip("You can have a maximum of 5 tags."); 
        
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

        if (CkGui.IconTextButton(FAI.CloudUploadAlt, "Publish Pattern to the Pattern ShareHub", ImGui.GetContentRegionAvail().X,
            false, _authorName.IsNullOrEmpty() || _selectedPattern.Identifier.IsEmptyGuid()))
        {
            // upload itttt
            _shareHub.UploadPattern(_selectedPattern, _authorName, _tagList.Split(',').Select(x => x.ToLower().Trim()).ToHashSet());
        }
        CkGui.AttachToolTip("Must have a selected pattern and author name to upload.");
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

    public void DrawMoodlesPublications()
    {
        // start by selecting the pattern.
        if (MoodleCache.IpcData is null)
        {
            CkGui.ColorText("No Ipc Data Available.", ImGuiColors.DalamudRed);
        }
        else
        {
            // draw the create section.
            CkGui.GagspeakBigText("Publish A Moodle");

            _statusCombo.Draw("PublicationStatuses", 200f);

            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            CkGui.ColorText("Chosen Moodle", ImGuiColors.ParsedGold);

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
                    var tags = _tagList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(tag => tag.Trim())
                                       .ToArray();
                    _tagList = string.Join(", ", tags.Take(5));
                }
            }
            CkGui.AttachToolTip("You can have a maximum of 5 tags.");

            ImUtf8.SameLineInner();
            DrawTagCombo("##moodleTagsFilter", ImGui.GetContentRegionAvail().X, (tag) =>
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
            CkGui.AttachToolTip("Select an existing tag on the Server.--SEP--This makes it easier for people to find your Moodles!");

            if (CkGui.IconTextButton(FAI.CloudUploadAlt, "Publish Moodle to the Moodle ShareHub", ImGui.GetContentRegionAvail().X, 
                false, _authorName.IsNullOrEmpty() || _statusCombo.Current.GUID.IsEmptyGuid()))
            {
                if (_statusCombo.Current.GUID.IsEmptyGuid())
                    return;

                _shareHub.UploadMoodle(_authorName, _tagList.Split(',').Select(x => x.ToLower().Trim()).ToHashSet(), _statusCombo.Current);
            }
            CkGui.AttachToolTip("Must have a selected Moodle and author name to upload.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGuiUtil.Center("Your Currently Published Moodles");
            ImGui.Separator();
        }

        // push the style for the more thin scrollbar.
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);

        // draw the existing publications list.
        using var child = ImRaii.Child("##PublishedMoodlesList", ImGui.GetContentRegionAvail(), false);
        DrawPublishedMoodlesList();
    }


    private void DrawPublishedPatternList()
    {
        var items = _shareHub.ClientPublishedPatterns.ToHashSet();
        if (items.Count == 0)
        {
            ImGui.TextUnformatted("No Patterns Published.");
            return;
        }

        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.TankBlue);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.3f, 0.4f));

        foreach (var pattern in items) PublishedPatternItem(pattern);
    }

    private void DrawPublishedMoodlesList()
    {
        var items = _shareHub.ClientPublishedMoodles.ToHashSet();
        if (items.Count == 0)
        {
            ImGui.TextUnformatted("No Moodles Published.");
            return;
        }

        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.TankBlue);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.3f, 0.4f));

        foreach (var moodle in items) PublishedMoodleItem(moodle);
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
                if(!pattern.Description.IsNullOrEmpty()) CkGui.HelpText(pattern.Description, true);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - unpublishButton);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink))
                    if (CkGui.IconTextButton(FAI.Globe, "Unpublish", isInPopup: true, disabled: !KeyMonitor.ShiftPressed() || !_shareHub.DisableUI))
                        _shareHub.RemovePattern(pattern.Identifier);
                CkGui.AttachToolTip("Removes this pattern publication from the pattern hub." +
                    "--SEP--Must hold SHIFT");
            }
            // next line:
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FAI.UserCircle);
                ImUtf8.SameLineInner();
                CkGui.ColorText(pattern.Author, ImGuiColors.DalamudGrey);
                CkGui.AttachToolTip("The Author Name you gave yourself when publishing this pattern.");
                ImGui.SameLine();
                CkGui.ColorText("(" + pattern.UploadedDate.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) + ")", ImGuiColors.DalamudGrey);
                CkGui.AttachToolTip("The date this pattern was published.");

                var formatDuration = pattern.Length.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
                var timerText = pattern.Length.ToString(formatDuration);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(timerText).X - CkGui.IconSize(FAI.Stopwatch).X - ImGui.GetStyle().ItemSpacing.X);
                
                using (ImRaii.Group())
                {
                    CkGui.IconText(FAI.Stopwatch);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted(pattern.Length.ToString(formatDuration));
                }
                CkGui.AttachToolTip("Total Pattern Duration");
            }
        }
    }

    private void PublishedMoodleItem(PublishedMoodle moodle)
    {
        var unpublishButton = CkGui.IconTextButtonSize(FAI.Globe, "Unpublish");
        var height = ImGui.GetFrameHeight() * 2.25f + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##MoodleResult_{moodle.MoodleStatus.GUID}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, WFlags.ChildWindow))
        {
            var imagePos = Vector2.Zero;
            using (ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                imagePos = ImGui.GetCursorPos();
                ImGuiHelpers.ScaledDummy(MoodleDrawer.IconSize);
                // if the scaled dummy is hovered, display the description, if any.
                if(ImGui.IsItemHovered())
                {
                    if(!moodle.MoodleStatus.Description.IsNullOrEmpty()) 
                        CkGui.AttachToolTip(moodle.MoodleStatus.Description.StripColorTags());
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                CkGui.ColorText(moodle.MoodleStatus.Title.StripColorTags(), ImGuiColors.DalamudWhite);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - unpublishButton);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink))
                    if (CkGui.IconTextButton(FAI.Globe, "Unpublish", isInPopup: true, disabled: !KeyMonitor.ShiftPressed() || !_shareHub.DisableUI))
                        _shareHub.RemoveMoodle(moodle.MoodleStatus.GUID);
                CkGui.AttachToolTip("Remove this publication from the Moodle ShareHub!" +
                    "--SEP--Must hold SHIFT");
            }
            ImGui.Spacing();
            // next line:
            using (ImRaii.Group())
            {
                var stacksSize = CkGui.IconSize(FAI.LayerGroup).X;
                var dispellableSize = CkGui.IconSize(FAI.Eraser).X;
                var permanentSize = CkGui.IconSize(FAI.Infinity).X;
                var stickySize = CkGui.IconSize(FAI.MapPin).X;
                var customVfxPath = CkGui.IconSize(FAI.Magic).X;
                var stackOnReapply = CkGui.IconSize(FAI.PersonCirclePlus).X;

                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FAI.UserCircle);
                ImUtf8.SameLineInner();
                CkGui.ColorText(moodle.AuthorName, ImGuiColors.DalamudGrey);
                CkGui.AttachToolTip("Publisher of the Moodle");


                // jump to the right side to draw all the icon data.
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X * 5
                    - stacksSize - dispellableSize - permanentSize - stickySize - customVfxPath - stackOnReapply);
                ImGui.AlignTextToFramePadding();
                CkGui.BooleanToColoredIcon(moodle.MoodleStatus.Stacks > 1, false, FAI.LayerGroup, FAI.LayerGroup, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodle.MoodleStatus.Stacks > 1 ? "Has " + moodle.MoodleStatus.Stacks + "Stacks." : "Not a stackable Moodle.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodle.MoodleStatus.Dispelable, false, FAI.Eraser, FAI.Eraser, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodle.MoodleStatus.Dispelable ? "Can be dispelled." : "Cannot be dispelled.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodle.MoodleStatus.AsPermanent, false, FAI.Infinity, FAI.Infinity, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodle.MoodleStatus.AsPermanent ? "Permanent Moodle." : "Temporary Moodle.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodle.MoodleStatus.Persistent, false, FAI.MapPin, FAI.MapPin, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodle.MoodleStatus.Persistent ? "Marked as a Sticky Moodle." : "Not Sticky.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(!string.IsNullOrEmpty(moodle.MoodleStatus.CustomVFXPath), false, FAI.Magic, FAI.Magic, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(!string.IsNullOrEmpty(moodle.MoodleStatus.CustomVFXPath) ? "Has a custom VFX path." : "No custom VFX path.");

                ImUtf8.SameLineInner();
                CkGui.BooleanToColoredIcon(moodle.MoodleStatus.StackOnReapply, false, FAI.PersonCirclePlus, FAI.PersonCirclePlus, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                CkGui.AttachToolTip(moodle.MoodleStatus.StackOnReapply ? "Stacks " + moodle.MoodleStatus.StacksIncOnReapply + " times on Reapplication." : "Doesn't stack on reapplication.");
            }

            if (moodle.MoodleStatus.IconID != 0 && imagePos != Vector2.Zero)
                _monitor.DrawMoodleIcon(moodle.MoodleStatus.IconID, moodle.MoodleStatus.Stacks, MoodleDrawer.IconSize);
        }
    }

    private void DrawTagCombo(string label, float width, Action<string> onSelected)
    {
        var tagList = _shareHub.FetchedTags.ToImmutableList();
        ImGui.SetNextItemWidth(width);
        using (var tagCombo = ImRaii.Combo(label, tagList.FirstOrDefault() ?? "Select Tag.."))
        {
            if (tagCombo)
                foreach (var tag in tagList)
                    if (ImGui.Selectable(tag, false))
                        onSelected(tag);
        }
    }
}
