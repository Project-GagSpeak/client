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

    public static float FramedIconDisplayHeight(int rows = 1) => IconSize.Y * rows + ImGui.GetStyle().ItemSpacing.Y * (rows - 1);
    public static float FramedIconDisplayHeight(float h, int rows = 1) => h * rows + ImGui.GetStyle().ItemSpacing.Y * (rows - 1);

    public string MoodleTypeTooltip(Moodle moodle)
        => $"Switch Moodle Types. (Hold Shift)--SEP--Current: Moodle {(moodle is MoodlePreset ? MoodleType.Preset.ToString() : MoodleType.Status.ToString())}";

    public void DrawMoodleCombo(string id, Moodle item, float width, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // draw the dropdown for the status/preset selection. This is based on the type of moodle.
        if (item is MoodlePreset preset)
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
        else if (item is Moodle status)
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
                DrawMoodleCombo(id, item.Moodle, ImGui.GetContentRegionAvail().X);
            }

            // Below this, we need to draw the display field of the moodles that the selected status has.
            FramedMoodleIconDisplay(id, item.Moodle, ImGui.GetContentRegionAvail().X, CkStyle.ChildRounding(), moodleSize);
        }
    }

    public void FramedMoodleIconDisplay(string id, Moodle moodle, float width, float rounding, Vector2? iconSize = null, int rows = 1)
        => FramedMoodleIconDisplay(id, [ moodle ], width, rounding, iconSize ?? IconSize, rows);

    public void FramedMoodleIconDisplay(string id, IEnumerable<Moodle> moodles, float width, float rounding, Vector2? iconSize = null, int rows = 1)
        => FramedMoodleIconDisplay(id, moodles, width, rounding, iconSize ?? IconSize, rows);

    public void FramedMoodleIconDisplay(string id, IEnumerable<Moodle> moodles, float width, float rounding, Vector2 iconSize, int rows = 1)
    {
        using (CkRaii.FramedChildPaddedW($"##{id}-MoodleRowDrawn", width, FramedIconDisplayHeight(iconSize.Y, rows), CkColor.FancyHeaderContrast.Uint(), rounding))
        {
            if (moodles == null || moodles.Count() <= 0)
            {
                CkGui.ColorText("No Moodles To Display...", ImGuiColors.ParsedGrey);
                return;
            }

            var moodleStatusMap = VisualApplierMoodles.LatestIpcData.MoodlesStatuses
                .ToDictionary(m => m.GUID, m => m);

            var padding = ImGui.GetStyle().ItemInnerSpacing.X;
            var moodlePerRow = MathF.Floor((width - padding) / (iconSize.X + padding));

            List<MoodlesStatusInfo> statuses = new List<MoodlesStatusInfo>();
            foreach (var moodle in moodles)
            {
                // if no moodle is selected, draw nothing and early return.
                if (moodle.Id.IsEmptyGuid())
                    continue;

                if (moodle is MoodlePreset preset)
                    statuses.AddRange(preset.StatusIds.Where(moodleStatusMap.ContainsKey).Select(id => moodleStatusMap[id]));
                else if (moodleStatusMap.TryGetValue(moodle.Id, out var singleStatus))
                    statuses.Add(singleStatus);
            }

            // display each moodle.
            var currentRow = 0;
            var moodlesInRow = 0;
            foreach (var status in statuses)
            {
                // Prevent invalid draws
                if (status.IconID is 0)
                    continue;

                _statusMonitor.DrawMoodleIcon(status.IconID, status.Stacks, iconSize);
                if (ImGui.IsItemHovered())
                    _statusMonitor.DrawMoodleStatusTooltip(status, VisualApplierMoodles.LatestIpcData.MoodlesStatuses);
                moodlesInRow++;
                if (moodlesInRow >= moodlePerRow)
                {
                    currentRow++;
                    moodlesInRow = 0;
                }
                else
                    ImUtf8.SameLineInner();
                if (currentRow >= rows)
                    break;
            }
        }
    }

    public void DrawMoodleIconRow(string id, Moodle moodle, Vector2 region)
    {
        var padding = ImGui.GetStyle().FramePadding;
        var pos = ImGui.GetCursorScreenPos();
        using (CkRaii.FramedChild($"##{id}-MoodleRowDrawn", region, CkColor.FancyHeaderContrast.Uint()))
        {
            // if no moodle is selected, draw nothing and early return.
            if (moodle.Id.IsEmptyGuid())
                return;

            ImGui.SetCursorScreenPos(pos + new Vector2(padding.X, padding.Y * 3));
            var moodleStatuses = moodle switch
            {
                MoodlePreset p => VisualApplierMoodles.LatestIpcData.MoodlesStatuses.Where(x => p.StatusIds.Contains(x.GUID)),
                _ => !moodle.Id.IsEmptyGuid()
                    ? new[] { VisualApplierMoodles.LatestIpcData.MoodlesStatuses.FirstOrDefault(x => x.GUID == moodle.Id) }
                    : Enumerable.Empty<MoodlesStatusInfo>(),
            };

            DrawMoodleStatuses(moodleStatuses, IconSize);
        }
    }

    public void DrawMoodles(Moodle moodleItem, Vector2 size)
    {
        // determine what moodle statuses we are drawing.
        var moodleStatuses = moodleItem switch
        {
            MoodlePreset p => VisualApplierMoodles.LatestIpcData.MoodlesStatuses.Where(x => p.StatusIds.Contains(x.GUID)),
            _              => !moodleItem.Id.IsEmptyGuid() 
                ? new[] { VisualApplierMoodles.LatestIpcData.MoodlesStatuses.FirstOrDefault(x => x.GUID == moodleItem.Id) } 
                : Enumerable.Empty<MoodlesStatusInfo>(),
        };

        DrawMoodleStatuses(moodleStatuses, size);
    }

    public void DrawMoodleStatuses(IEnumerable<MoodlesStatusInfo> statuses, Vector2 iconSize)
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
                _statusMonitor.DrawMoodleStatusTooltip(status, VisualApplierMoodles.LatestIpcData.MoodlesStatuses);
            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
