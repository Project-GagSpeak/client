using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;
using GagSpeak.Utils.Enums;
using GagspeakAPI.Attributes;
using Microsoft.IdentityModel.Tokens;

namespace GagSpeak.CkCommons.Gui.Components;
public class MoodleDrawer
{
    private readonly ILogger<MoodleDrawer> _logger;
    private readonly IconDisplayer _statusMonitor;
    private MoodleStatusCombo _statusCombo { get; init; }
    private MoodlePresetCombo _presetCombo { get; init; }
    public MoodleDrawer(ILogger<MoodleDrawer> logger, IconDisplayer statusMonitor)
    {
        _logger = logger;
        _statusMonitor = statusMonitor;
        _statusCombo = new MoodleStatusCombo(1.15f, statusMonitor, logger);
        _presetCombo = new MoodlePresetCombo(1.15f, statusMonitor, logger);
    }

    public static Vector2 IconSize
        => new Vector2(24, 32);

    public static Vector2 IconSizeFramed
        => new(ImGui.GetFrameHeight() * .75f, ImGui.GetFrameHeight());

    public static float FramedIconDisplayHeight(int rows = 1)
        => IconSize.Y * rows + ImGui.GetStyle().ItemSpacing.Y * (rows - 1);

    public static float FramedIconDisplayHeight(float h, int rows = 1)
        => h * rows + ImGui.GetStyle().ItemSpacing.Y * (rows - 1);
    public string MoodleTypeTooltip(Moodle moodle) 
        => $"Switch Types. (Hold Shift)--SEP--Current: Moodle {(moodle is MoodlePreset ? "Preset" : "Status")}";

    public void DrawMoodleCombo(Moodle moodle, float width, CFlags flags = CFlags.None)
    {
        // draw the dropdown for the status/preset selection. This is based on the type of moodle.
        if (moodle is MoodlePreset preset)
        {
            var change = _presetCombo.Draw("GagMoodlePreset", preset.Id, width, flags);
            if (change && !preset.Id.Equals(_presetCombo.Current.GUID))
            {
                _logger.LogTrace($"Item changed to {_presetCombo.Current.GUID} [{_presetCombo.Current.Title}] from {preset.Id}");
                preset.UpdatePreset(_presetCombo.Current.GUID, _presetCombo.Current.Statuses);
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace("Combo Was Right Clicked, and Cleared the Moodle Preset.");
                preset.UpdatePreset(Guid.Empty, Enumerable.Empty<Guid>());
            }
        }
        else if (moodle is Moodle status)
        {
            var change = _statusCombo.Draw("GagMoodleStatus", status.Id, width, flags);
            if (change && !status.Id.Equals(_statusCombo.Current.GUID))
            {
                _logger.LogTrace($"Item changed to {_statusCombo.Current.GUID} [{_statusCombo.Current.Title}] from {status.Id}");
                status.UpdateId(_statusCombo.Current.GUID);
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace("Combo Was Right Clicked, and Cleared the Moodle Status.");
                status.UpdateId(Guid.Empty);
            }
        }
    }

    /// <summary> Draw the items associated moodle. This can be either a Moodle Status or Moodle Preset. </summary>
    public void DrawAssociatedMoodle(string id, IRestriction item, float width, Vector2? iconSize = null)
    {
        // construct a child object here.
        var moodleSize = iconSize ?? IconSize;
        var style = ImGui.GetStyle();
        var moodleDisplayHeight = moodleSize.Y.AddWinPadY();
        var winSize = new Vector2(width, moodleDisplayHeight + style.ItemSpacing.Y + ImGui.GetFrameHeight());
        using (CkRaii.HeaderChild("Associated Moodle", winSize, HeaderFlags.AddPaddingToHeight))
        {
            using (ImRaii.Group())
            {
                if (CkGui.IconButton(FAI.ArrowsLeftRight, disabled: !KeyMonitor.ShiftPressed()))
                {
                    // convert the type.
                    item.Moodle = item.Moodle switch
                    {
                        MoodlePreset => new Moodle(),
                        Moodle => new MoodlePreset(),
                        _ => throw new ArgumentOutOfRangeException(nameof(item.Moodle), item.Moodle, "Unknown Moodle Type"),
                    };
                }
                CkGui.AttachToolTip(MoodleTypeTooltip(item.Moodle));

                ImUtf8.SameLineInner();
                DrawMoodleCombo(item.Moodle, ImGui.GetContentRegionAvail().X);
            }

            // Below this, we need to draw the display field of the moodles that the selected status has.
            ShowStatusIconsFramed(id, item.Moodle, ImGui.GetContentRegionAvail().X, CkStyle.ChildRounding(), moodleSize);
        }
    }

    public void ShowStatusIcons(string id, Moodle moodle, float width, float rounding, Vector2? iconSize = null, int rows = 1)
        => ShowStatusIcons(id, (moodle is MoodlePreset p ? p.StatusIds : [moodle.Id]), width, rounding, false, iconSize, rows: rows);

    public void ShowStatusIcons(string id, IEnumerable<Moodle> moodles, float width, float rounding, Vector2? iconSize = null, int rows = 1)
    {
        var ids = new List<Guid>();
        foreach (var moodle in moodles)
        {
            if (moodle.Id == Guid.Empty) continue;
            if (moodle is MoodlePreset p) ids.AddRange(p.StatusIds);
            else ids.Add(moodle.Id);
        }
        ShowStatusIcons(id, ids, width, rounding, false, iconSize, rows);
    }

    public void ShowStatusIconsFramed(string id, Moodle moodle, float width, float rounding, Vector2? iconSize = null, int rows = 1)
        => ShowStatusIcons(id, (moodle is MoodlePreset p ? p.StatusIds : [moodle.Id]), width, rounding, true, iconSize, rows);

    public void ShowStatusIconsFramed(string id, IEnumerable<Moodle> moodles, float width, float rounding, Vector2? iconSize = null, int rows = 1)
    {
        var ids = new List<Guid>();
        foreach (var moodle in moodles)
        {
            if (moodle.Id == Guid.Empty) continue;
            if (moodle is MoodlePreset p) ids.AddRange(p.StatusIds);
            else ids.Add(moodle.Id);
        }
        ShowStatusIcons(id, ids, width, rounding, true, iconSize, rows);
    }

    public void ShowStatusIcons(string id, IEnumerable<Guid> statusIds, float width, float rounding, bool framed, Vector2? iconSize = null, int rows = 1)
    {
        var size = iconSize ?? IconSize;
        if (framed)
            using (CkRaii.FramedChildPaddedW($"##{id}-MoodleRowDrawn", width, FramedIconDisplayHeight(size.Y, rows), CkColor.FancyHeaderContrast.Uint(), rounding))
                DrawIconsOrEmpty(statusIds, width, size, rows);
        else
            DrawIconsOrEmpty(statusIds, width, size, rows);
    }

    public void DrawIconsOrEmpty(IEnumerable<Guid> statusIds, float width, Vector2? iconSize = null, int rows = 1)
    {
        if (statusIds == null || !statusIds.Any())
        {
            CkGui.ColorText("No Moodles To Display...", ImGuiColors.ParsedGrey);
            return;
        }
        ShowIcons(statusIds, width, iconSize ?? IconSize, rows);
    }

    public void ShowIcons(IEnumerable<Guid> statusIds, float width, Vector2 iconSize, int rows = 1)
    {
        var padding = ImGui.GetStyle().ItemInnerSpacing.X;
        var iconsPerRow = MathF.Floor((width - padding) / (iconSize.X + padding));

        var icons = new List<MoodlesStatusInfo>();

        int row = 0, col = 0;
        foreach (var id in statusIds)
        {
            if (id.IsEmptyGuid())
                continue;

            if (!MoodleHandler.IpcData.Statuses.TryGetValue(id, out var status))
                continue;

            if (status.IconID is 0)
                continue;

            _statusMonitor.DrawMoodleIcon(status.IconID, status.Stacks, iconSize);
            _statusMonitor.DrawMoodleStatusTooltip(status, MoodleHandler.IpcData.StatusList);

            if (++col >= iconsPerRow)
            {
                col = 0;
                if (++row >= rows)
                    break;
            }
            else
            {
                ImUtf8.SameLineInner();
            }
        }
    }

    public void DrawStatusInfos(IEnumerable<MoodlesStatusInfo> statuses, Vector2 iconSize)
    {
        using var _ = ImRaii.Group();
        // Calculate the remaining height in the region.
        foreach (var status in statuses)
        {
            // Prevent invalid draws
            if(status.IconID is 0)
                continue;

            _statusMonitor.DrawMoodleIcon(status.IconID, status.Stacks, iconSize);
            if (ImGui.IsItemHovered())
                _statusMonitor.DrawMoodleStatusTooltip(status, MoodleHandler.IpcData.StatusList);
            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
