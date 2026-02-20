using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using CkCommons;
using CkCommons.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.Textures;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.Services.Tutorial;
using TerraFX.Interop.Windows;
using Tutorial = OtterGui.Widgets.Tutorial;

namespace GagSpeak.Gui.Components;
public class MoodleDrawer
{
    private readonly ILogger<MoodleDrawer> _logger;
    private readonly TutorialService _guides;
    private MoodleStatusCombo _statusCombo { get; init; }
    private MoodlePresetCombo _presetCombo { get; init; }
    public MoodleDrawer(ILogger<MoodleDrawer> logger, TutorialService guides)
    {
        _logger = logger;
        _guides = guides;
        _statusCombo = new MoodleStatusCombo(logger, 1.15f);
        _presetCombo = new MoodlePresetCombo(logger, 1.15f);
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
                _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SwitchingMoodleType, WardrobeUI.LastPos, WardrobeUI.LastSize,
                                     () => item.Moodle = new MoodleTuple(MoodleCache.IpcData.StatusList.FirstOrDefault()));

                ImUtf8.SameLineInner();
                DrawMoodleCombo(item.Moodle, ImGui.GetContentRegionAvail().X);
            }

            // Below this, we need to draw the display field of the moodles that the selected status has.
            ShowStatusIconsFramed(id, item.Moodle, ImGui.GetContentRegionAvail().X, CkStyle.ChildRounding(), moodleSize);
            _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SelectedMoodlePreview, WardrobeUI.LastPos, WardrobeUI.LastSize);
        }
    }

    public void ShowStatusIcons(Moodle moodle, float width, Vector2? iconSize = null, int rows = 1)
        => DrawIconsOrEmpty(moodle is MoodlePreset p ? p.StatusIds : [moodle.Id], width, iconSize, rows);

    public void ShowStatusIcons(IEnumerable<Moodle> moodles, float width, Vector2? iconSize = null, int rows = 1)
    {
        var ids = new List<Guid>();
        foreach (var moodle in moodles)
        {
            if (moodle.Id == Guid.Empty) continue;
            if (moodle is MoodlePreset p) ids.AddRange(p.StatusIds);
            else ids.Add(moodle.Id);
        }
        DrawIconsOrEmpty(ids, width, iconSize, rows);
    }

    public void ShowStatusIconsFramed(string id, Moodle moodle, float width, float rounding, Vector2? iconSize = null, int rows = 1)
        => ShowStatusIconsFramed(id, [moodle], width, rounding, iconSize, rows);

    public void ShowStatusIconsFramed(string id, IEnumerable<Moodle> moodles, float width, float rounding, Vector2? iconSize = null, int rows = 1)
    {
        var ids = new List<Guid>();
        foreach (var moodle in moodles)
        {
            if (moodle.Id == Guid.Empty) continue;
            if (moodle is MoodlePreset p) ids.AddRange(p.StatusIds);
            else ids.Add(moodle.Id);
        }

        var size = iconSize ?? IconSize;
        using (CkRaii.FramedChildPaddedW($"##{id}-MoodleRowDrawn", width, FramedIconDisplayHeight(size.Y, rows), CkCol.CurvedHeaderFade.Uint(), 0, rounding))
            DrawIconsOrEmpty(ids, width, size, rows);
    }

    public void ShowStatusInfosFramed(string id, IEnumerable<MoodlesStatusInfo> statuses, float width, float rounding, Vector2? iconSize = null, int rows = 1)
    {
        var size = iconSize ?? IconSize;
        using (CkRaii.FramedChildPaddedW($"##{id}-MoodleRowDrawn", width, FramedIconDisplayHeight(size.Y, rows), ImGui.GetColorU32(ImGuiCol.FrameBgHovered), GsCol.VibrantPink.Uint(), rounding))
            ShowStatusInfos(statuses, width, size, rows);
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
            if (id== Guid.Empty)
                continue;

            if (!MoodleCache.IpcData.Statuses.TryGetValue(id, out var status))
                continue;

            if (status.IconID is 0)
                continue;

            MoodleIcon.DrawMoodleIcon(status.IconID, status.Stacks, iconSize);
            GagspeakEx.DrawMoodleStatusTooltip(status, MoodleCache.IpcData.StatusList);

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

    public void ShowStatusInfos(IEnumerable<MoodlesStatusInfo> statuses, float width, Vector2? iconSize = null, int rows = 1)
    {
        var size = iconSize ?? IconSize;
        var padding = ImGui.GetStyle().ItemInnerSpacing.X;
        var iconsPerRow = MathF.Floor((width - padding) / (size.X + padding));

        int row = 0, col = 0;
        foreach (var status in statuses)
        {
            if (status.IconID is 0)
                continue;

            MoodleIcon.DrawMoodleIcon(status.IconID, status.Stacks, size);
            GagspeakEx.DrawMoodleStatusTooltip(status, MoodleCache.IpcData.StatusList);

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

    public void DrawStatusInfos(List<MoodlesStatusInfo> statuses, Vector2 iconSize)
    {
        using var _ = ImRaii.Group();
        // Calculate the remaining height in the region.
        for (var i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].IconID is 0)
                continue;

            MoodleIcon.DrawMoodleIcon(statuses[i].IconID, statuses[i].Stacks, iconSize);
            GagspeakEx.DrawMoodleStatusTooltip(statuses[i], MoodleCache.IpcData.StatusList);

            if (i < statuses.Count - 1)
                ImUtf8.SameLineInner();
        }
    }
}
