using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Components;
public class MoodleDrawer
{
    private readonly ILogger<MoodleDrawer> _logger;
    private readonly MoodlesDisplayer _statusMonitor;
    private MoodleStatusCombo _statusCombo { get; init; }
    private MoodlePresetCombo _presetCombo { get; init; }
    public MoodleDrawer(ILogger<MoodleDrawer> logger, MoodlesDisplayer statusMonitor)
    {
        _logger = logger;
        _statusMonitor = statusMonitor;
        _statusCombo = new MoodleStatusCombo(1.15f, VisualApplierMoodles.LatestIpcData, statusMonitor, logger);
        _presetCombo = new MoodlePresetCombo(1.15f, VisualApplierMoodles.LatestIpcData, statusMonitor, logger);
    }

    /// <summary> Draw the items associated moodle. This can be either a Moodle Status or Moodle Preset. </summary>
    public void DrawAssociatedMoodle(string id, IRestriction item, float width)
    {
        // construct a child object here.
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var moodleDisplayHeight = MoodlesDisplayer.DefaultSize.Y + style.FramePadding.Y * 2;
        var statusRowHeight = ImGui.GetFrameHeight() + style.ItemSpacing.Y;
        var winSize = new Vector2(width, moodleDisplayHeight + statusRowHeight);
        using (CkComponents.CenterHeaderChild(id, "Associated Moodle", winSize, WFlags.AlwaysUseWindowPadding))
        {
            // get the innder width after the padding is applied.
            var widthInner = ImGui.GetContentRegionAvail().X;

            // group together the first row.
            using (ImRaii.Group())
            {
                var buttonTooltip = item.Moodle switch
                {
                    MoodlePreset => "Switch Moodle Types. (Hold Shift)--SEP--Current: Moodle Preset",
                    Moodle       => "Switch Moodle Types. (Hold Shift)--SEP--Current: Moodle Status",
                    _            => "Switch Moodle Types.--SEP--Current: Unknown",
                };
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
                CkGui.AttachToolTip(buttonTooltip);

                ImUtf8.SameLineInner();
                // draw the dropdown for the status/preset selection. This is based on the type of moodle.
                if (item.Moodle is MoodlePreset preset)
                {
                    var change = _presetCombo.Draw("GagMoodlePreset", preset.Id, ImGui.GetContentRegionAvail().X);
                    if (change && !preset.Id.Equals(_presetCombo.CurrentSelection.GUID))
                    {
                        _logger.LogTrace($"Item changed to {_presetCombo.CurrentSelection.GUID} " +
                            $"[{_presetCombo.CurrentSelection.Title}] from {preset.Id}");
                        preset.Id = _presetCombo.CurrentSelection.GUID;
                        preset.StatusIds = _presetCombo.CurrentSelection.Statuses;
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        _logger.LogTrace("Combo Was Right Clicked, and Cleared the Moodle Preset.");
                        preset.Id = Guid.Empty;
                        preset.StatusIds = Enumerable.Empty<Guid>();
                    }
                }
                else if (item.Moodle is Moodle status)
                {
                    var change = _statusCombo.Draw("GagMoodleStatus", status.Id, ImGui.GetContentRegionAvail().X);
                    if (change && !status.Id.Equals(_statusCombo.CurrentSelection.GUID))
                    {
                        _logger.LogTrace($"Item changed to {_statusCombo.CurrentSelection.GUID} " +
                            $"[{_statusCombo.CurrentSelection.Title}] from {status.Id}");
                        status.Id = _statusCombo.CurrentSelection.GUID;
                    }
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        _logger.LogTrace("Combo Was Right Clicked, and Cleared the Moodle Status.");
                        status.Id = Guid.Empty;
                    }
                }
            }
            // Below this, we need to draw the display field of the moodles that the selected status has.
            DrawMoodleIconRow(item.Moodle, new Vector2(ImGui.GetContentRegionAvail().X, MoodlesDisplayer.DefaultSize.Y + ImGui.GetStyle().FramePadding.Y * 2));
        }
    }

    private void DrawMoodleIconRow(Moodle moodle, Vector2 region)
    {
        var padding = ImGui.GetStyle().FramePadding;
        var pos = ImGui.GetCursorScreenPos();
        using (CkComponents.FramedChild("MoodleRowDrawn", CkColor.FancyHeaderContrast.Uint(), region))
        {
            // if no moodle is selected, draw nothing and early return.
            if (moodle.Id.IsEmptyGuid())
                return;

            ImGui.SetCursorScreenPos(pos + new Vector2(padding.X, padding.Y * 3));
            DrawMoodles(moodle, MoodlesDisplayer.DefaultSize);
        }
    }

    public void DrawMoodles(Moodle moodleItem, Vector2 size)
    {
        using (ImRaii.Group())
        {
            // determine what moodle statuses we are drawing.
            var moodleStatuses = moodleItem switch
            {
                MoodlePreset p => VisualApplierMoodles.LatestIpcData.MoodlesStatuses.Where(x => p.StatusIds.Contains(x.GUID)),
                _              => !moodleItem.Id.IsEmptyGuid() 
                    ? new[] { VisualApplierMoodles.LatestIpcData.MoodlesStatuses.FirstOrDefault(x => x.GUID == moodleItem.Id) } 
                    : Enumerable.Empty<MoodlesStatusInfo>(),
            };

            // Calculate the remaining height in the region.
            foreach (var status in moodleStatuses)
            {
                _statusMonitor.DrawMoodleIcon(status.IconID, status.Stacks, size);
                if (ImGui.IsItemHovered())
                    _statusMonitor.DrawMoodleStatusTooltip(status, VisualApplierMoodles.LatestIpcData.MoodlesStatuses);
                ImGui.SameLine();
            }
            ImGui.NewLine();
        }
    }
}
