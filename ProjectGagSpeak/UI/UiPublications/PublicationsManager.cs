using Dalamud.Interface.Colors;
using Dalamud.Interface;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;
using Dalamud.Interface.Utility;
using GagSpeak.Utils;
using System.Globalization;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.Services.ConfigurationServices;
using System.Collections.Immutable;
using System.Diagnostics;
using GagSpeak.PlayerData.Data;
using GagspeakAPI.Extensions;
using OtterGui;
using System.Linq;

namespace GagSpeak.UI.UiPublications;
public class PublicationsManager
{
    private readonly ILogger<PublicationsManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ClientData _clientData;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly MoodlesService _moodlesService;
    private readonly ShareHubService _shareHub;
    private readonly UiSharedService _uiShared;

    public PublicationsManager(ILogger<PublicationsManager> logger, GagspeakMediator mediator,
        ClientData clientData, ClientConfigurationManager clientConfigs, MoodlesService moodlesService,
        ShareHubService shareHub, UiSharedService uiSharedService)
    {
        _logger = logger;
        _mediator = mediator;
        _clientData = clientData;
        _clientConfigs = clientConfigs;
        _moodlesService = moodlesService;
        _shareHub = shareHub;
        _uiShared = uiSharedService;
    }

    private PatternData? _selectedPattern = null;
    private MoodlesStatusInfo _selectedMoodle = new MoodlesStatusInfo();
    private string _authorName = string.Empty;
    private string _tagList = string.Empty;

    public void DrawPatternManager()
    {
        // draw the create section.
        _uiShared.GagspeakBigText("Publish A Pattern");

        // start by selecting the pattern.
        _uiShared.DrawComboSearchable("##PatternSelector", 200f, _clientConfigs.PatternConfig.PatternStorage.Patterns,
            (item) => item.Name, true, (selected) => _selectedPattern = selected);
        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Chosen Pattern", ImGuiColors.ParsedGold);
        
        ImGui.SetNextItemWidth(200f);
        ImGui.InputTextWithHint("##AuthorName", "Author DisplayName...", ref _authorName, 50);
        ImUtf8.SameLineInner();
        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Author Name", ImGuiColors.ParsedGold);

        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##shareHubTags", "Enter tags split by , (optional)", ref _tagList, 250);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _tagList = _tagList.ToLower();
            int commaCount = _tagList.Count(c => c == ',');
            if (commaCount > 4)
            {
                var tags = _tagList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(tag => tag.Trim())
                                   .ToArray();
                _tagList = string.Join(", ", tags.Take(5));
            }
        }
        UiSharedService.AttachToolTip("You can have a maximum of 5 tags."); 
        
        ImUtf8.SameLineInner();
        _uiShared.DrawCombo("##patternTagsFilter", ImGui.GetContentRegionAvail().X, _shareHub.FetchedTags.ToImmutableList(), (i) => i,
            (tag) =>
            {
                if(tag.IsNullOrEmpty()) return;
                int commaCount = _tagList.Count(c => c == ',');
                if (commaCount >= 4) return;

                // Handle new tab
                if (!_tagList.Contains(tag))
                {
                    if (_tagList.Length > 0 && _tagList[^1] != ',')
                        _tagList += ",";
                    _tagList += tag.ToLower();
                }
            }, shouldShowLabel: false, defaultPreviewText: "Add Existing Tags..");
        if (_uiShared.IconTextButton(FontAwesomeIcon.CloudUploadAlt, "Publish Pattern to the Pattern ShareHub", ImGui.GetContentRegionAvail().X, false, _authorName.IsNullOrEmpty() || _selectedPattern is null))
        {
            if (_selectedPattern is null) return;
            // upload itttt
            _shareHub.UploadPattern(_selectedPattern, _authorName, _tagList.Split(',').Select(x => x.ToLower().Trim()).ToHashSet());
            _selectedPattern = null;
        }
        UiSharedService.AttachToolTip("Must have a selected pattern and author name to upload.");
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

    public void DrawMoodlesManager()
    {
        // start by selecting the pattern.
        if (_clientData.LastIpcData is null)
        {
            UiSharedService.ColorText("No Ipc Data Available.", ImGuiColors.DalamudRed);
        }
        else
        {
            // draw the create section.
            _uiShared.GagspeakBigText("Publish A Moodle");

            _moodlesService.DrawMoodleStatusCombo("##MoodleSelector", 200f, _clientData.LastIpcData.MoodlesStatuses, (selected) =>
            {
                if (!_clientData.LastIpcData.MoodlesStatuses.Any(x => x.GUID == selected)) return;
                var statusInfo = _clientData.LastIpcData.MoodlesStatuses.First(x => x.GUID == selected);
                _selectedMoodle = statusInfo;
            });
            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Chosen Moodle", ImGuiColors.ParsedGold);

            ImGui.SetNextItemWidth(200f);
            ImGui.InputTextWithHint("##AuthorName", "Author DisplayName...", ref _authorName, 50);
            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            UiSharedService.ColorText("Author Name", ImGuiColors.ParsedGold);

            ImGui.SetNextItemWidth(200);
            ImGui.InputTextWithHint("##shareHubTags", "Enter tags split by , (optional)", ref _tagList, 250);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _tagList = _tagList.ToLower();
                int commaCount = _tagList.Count(c => c == ',');
                if (commaCount > 4)
                {
                    var tags = _tagList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(tag => tag.Trim())
                                       .ToArray();
                    _tagList = string.Join(", ", tags.Take(5));
                }
            }
            UiSharedService.AttachToolTip("You can have a maximum of 5 tags.");

            ImUtf8.SameLineInner();
            _uiShared.DrawCombo("##moodleTagsFilter", ImGui.GetContentRegionAvail().X, _shareHub.FetchedTags.ToImmutableList(), (i) => i,
                (tag) =>
                {
                    if (tag.IsNullOrEmpty()) return;
                    int commaCount = _tagList.Count(c => c == ',');
                    if (commaCount >= 4) return;

                    // Handle new tab
                    if (!_tagList.Contains(tag))
                    {
                        if (_tagList.Length > 0 && _tagList[^1] != ',')
                            _tagList += ",";
                        _tagList += tag.ToLower();
                    }
                }, shouldShowLabel: false, defaultPreviewText: "Add Existing Tags..");
            UiSharedService.AttachToolTip("Select an existing tag on the Server." +
                "--SEP--This makes it easier for people to find your Moodles!");

            if (_uiShared.IconTextButton(FontAwesomeIcon.CloudUploadAlt, "Publish Moodle to the Moodle ShareHub", ImGui.GetContentRegionAvail().X, false, _authorName.IsNullOrEmpty() || _selectedMoodle.GUID.IsEmptyGuid()))
            {
                if (_selectedMoodle.GUID.IsEmptyGuid()) return;
                _shareHub.UploadMoodle(_authorName, _tagList.Split(',').Select(x => x.ToLower().Trim()).ToHashSet(), _selectedMoodle);
                _selectedMoodle = new MoodlesStatusInfo();
            }
            UiSharedService.AttachToolTip("Must have a selected Moodle and author name to upload.");
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
        float unpublishButton = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Globe, "Unpublish");
        float height = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##PatternResult_{pattern.Identifier}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {

            using (ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText(pattern.Name, ImGuiColors.DalamudWhite);
                if(!pattern.Description.IsNullOrEmpty()) _uiShared.DrawHelpText(pattern.Description, true);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - unpublishButton);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink))
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Globe, "Unpublish", isInPopup: true, disabled: !KeyMonitor.ShiftPressed() || !_shareHub.CanShareHubTask))
                        _shareHub.RemovePattern(pattern.Identifier);
                UiSharedService.AttachToolTip("Removes this pattern publication from the pattern hub." +
                    "--SEP--Must hold SHIFT");
            }
            // next line:
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.UserCircle);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(pattern.Author, ImGuiColors.DalamudGrey);
                UiSharedService.AttachToolTip("The Author Name you gave yourself when publishing this pattern.");
                ImGui.SameLine();
                UiSharedService.ColorText("(" + pattern.UploadedDate.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) + ")", ImGuiColors.DalamudGrey);
                UiSharedService.AttachToolTip("The date this pattern was published.");

                var formatDuration = pattern.Length.Hours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss";
                string timerText = pattern.Length.ToString(formatDuration);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(timerText).X - _uiShared.GetIconData(FontAwesomeIcon.Stopwatch).X - ImGui.GetStyle().ItemSpacing.X);
                
                using (ImRaii.Group())
                {
                    _uiShared.IconText(FontAwesomeIcon.Stopwatch);
                    ImUtf8.SameLineInner();
                    ImGui.TextUnformatted(pattern.Length.ToString(formatDuration));
                }
                UiSharedService.AttachToolTip("Total Pattern Duration");
            }
        }
    }

    private void PublishedMoodleItem(PublishedMoodle moodle)
    {
        float unpublishButton = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Globe, "Unpublish");
        float height = ImGui.GetFrameHeight() * 2.25f + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;
        using (ImRaii.Child($"##MoodleResult_{moodle.MoodleStatus.GUID}", new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow))
        {
            Vector2 imagePos = Vector2.Zero;
            using (ImRaii.Group())
            {
                // display name, then display the downloads and likes on the other side.
                imagePos = ImGui.GetCursorPos();
                ImGuiHelpers.ScaledDummy(MoodlesService.StatusSize.X);
                // if the scaled dummy is hovered, display the description, if any.
                if(ImGui.IsItemHovered())
                {
                    if(!moodle.MoodleStatus.Description.IsNullOrEmpty()) 
                        UiSharedService.AttachToolTip(moodle.MoodleStatus.Description.StripColorTags());
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                UiSharedService.ColorText(moodle.MoodleStatus.Title.StripColorTags(), ImGuiColors.DalamudWhite);

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - unpublishButton);
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink))
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Globe, "Unpublish", isInPopup: true, disabled: !KeyMonitor.ShiftPressed() || !_shareHub.CanShareHubTask))
                        _shareHub.RemoveMoodle(moodle.MoodleStatus.GUID);
                UiSharedService.AttachToolTip("Remove this publication from the Moodle ShareHub!" +
                    "--SEP--Must hold SHIFT");
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
                var stackOnReapply = _uiShared.GetIconData(FontAwesomeIcon.PersonCirclePlus).X;

                ImGui.AlignTextToFramePadding();
                _uiShared.IconText(FontAwesomeIcon.UserCircle);
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(moodle.AuthorName, ImGuiColors.DalamudGrey);
                UiSharedService.AttachToolTip("Publisher of the Moodle");


                // jump to the right side to draw all the icon data.
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X * 5
                    - stacksSize - dispellableSize - permanentSize - stickySize - customVfxPath - stackOnReapply);
                ImGui.AlignTextToFramePadding();
                _uiShared.BooleanToColoredIcon(moodle.MoodleStatus.Stacks > 1, false, FontAwesomeIcon.LayerGroup, FontAwesomeIcon.LayerGroup, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodle.MoodleStatus.Stacks > 1 ? "Has " + moodle.MoodleStatus.Stacks + "Stacks." : "Not a stackable Moodle.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodle.MoodleStatus.Dispelable, false, FontAwesomeIcon.Eraser, FontAwesomeIcon.Eraser, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodle.MoodleStatus.Dispelable ? "Can be dispelled." : "Cannot be dispelled.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodle.MoodleStatus.AsPermanent, false, FontAwesomeIcon.Infinity, FontAwesomeIcon.Infinity, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodle.MoodleStatus.AsPermanent ? "Permanent Moodle." : "Temporary Moodle.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodle.MoodleStatus.Persistent, false, FontAwesomeIcon.MapPin, FontAwesomeIcon.MapPin, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodle.MoodleStatus.Persistent ? "Marked as a Sticky Moodle." : "Not Sticky.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(!string.IsNullOrEmpty(moodle.MoodleStatus.CustomVFXPath), false, FontAwesomeIcon.Magic, FontAwesomeIcon.Magic, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(!string.IsNullOrEmpty(moodle.MoodleStatus.CustomVFXPath) ? "Has a custom VFX path." : "No custom VFX path.");

                ImUtf8.SameLineInner();
                _uiShared.BooleanToColoredIcon(moodle.MoodleStatus.StackOnReapply, false, FontAwesomeIcon.PersonCirclePlus, FontAwesomeIcon.PersonCirclePlus, ImGuiColors.HealerGreen, ImGuiColors.DalamudGrey3);
                UiSharedService.AttachToolTip(moodle.MoodleStatus.StackOnReapply ? "Stacks " + moodle.MoodleStatus.StacksIncOnReapply + " times on Reapplication." : "Doesn't stack on reapplication.");
            }

            try
            {
                if (moodle.MoodleStatus.IconID != 0 && imagePos != Vector2.Zero)
                {
                    var statusIcon = _uiShared.GetGameStatusIcon((uint)((uint)moodle.MoodleStatus.IconID + moodle.MoodleStatus.Stacks - 1));

                    if (statusIcon is { } wrap)
                    {
                        ImGui.SetCursorPos(imagePos);
                        ImGui.Image(statusIcon.ImGuiHandle, MoodlesService.StatusSize);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to draw the status icon for the moodle.");
            }
        }
    }
}
