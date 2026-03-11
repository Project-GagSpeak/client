using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.State.Caches;
using GagspeakAPI.Data;
using Microsoft.IdentityModel.Tokens;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;
public static class LociDrawer
{
    public static string DataTypeTooltip(this LociItem lociItem) 
        => $"Switch Types. (Hold Shift)--SEP--Current: Loci {(lociItem is LociPreset ? "Preset" : "Status")}";

    public static void DrawIcons(LociItem item, float width, Vector2? iconSize = null, int rows = 1)
        => DrawIconsOrEmpty(item is LociPreset p ? p.StatusIds : [item.Id], width, iconSize, rows);

    public static void DrawIcons(IEnumerable<LociItem> items, float width, Vector2? iconSize = null, int rows = 1)
    {
        var ids = new List<Guid>();
        foreach (var item in items)
        {
            if (item.Id == Guid.Empty)
                continue;
            if (item is LociPreset p)
                ids.AddRange(p.StatusIds);
            else
                ids.Add(item.Id);
        }
        DrawIconsOrEmpty(ids, width, iconSize, rows);
    }

    public static void DrawIconsFramed(string id, LociItem item, float width, float rounding, Vector2? iconSize = null, int rows = 1)
        => DrawIconsFramed(id, [item], width, rounding, iconSize, rows);

    public static void DrawIconsFramed(string id, IEnumerable<LociItem> items, float width, float rounding, Vector2? iconSize = null, int rows = 1)
    {
        var ids = new List<Guid>();
        foreach (var item in items)
        {
            if (item.Id == Guid.Empty)
                continue;
            if (item is LociPreset p)
                ids.AddRange(p.StatusIds);
            else
                ids.Add(item.Id);
        }

        var size = iconSize ?? LociIcon.Size;
        using (CkRaii.FramedChildPaddedW($"##{id}-MoodleRowDrawn", width, LociIcon.GetRowHeight(size.Y, rows), CkCol.CurvedHeaderFade.Uint(), 0, rounding))
            DrawIconsOrEmpty(ids, width, size, rows);
    }

    public static void DrawTuplesFramed(string id, IEnumerable<LociStatusInfo> statuses, float width, float rounding, Vector2? iconSize = null, int rows = 1)
    {
        var size = iconSize ?? LociIcon.Size;
        using (CkRaii.FramedChildPaddedW($"##{id}-MoodleRowDrawn", width, LociIcon.GetRowHeight(size.Y, rows), ImGui.GetColorU32(ImGuiCol.FrameBgHovered), GsCol.VibrantPink.Uint(), rounding))
            DrawTuples(statuses, width, size, rows);
    }

    public static void DrawIconsOrEmpty(IEnumerable<Guid> ids, float width, Vector2? iconSize = null, int rows = 1)
    {
        if (ids.IsNullOrEmpty())
        {
            CkGui.ColorText("No Icons To Display...", ImGuiColors.ParsedGrey);
            return;
        }
        DrawIcons(ids, width, iconSize ?? LociIcon.Size, rows);
    }

    public static void DrawIcons(IEnumerable<Guid> statusIds, float width, Vector2 iconSize, int rows = 1)
    {
        var padding = ImGui.GetStyle().ItemInnerSpacing.X;
        var iconsPerRow = MathF.Floor((width - padding) / (iconSize.X + padding));

        var icons = new List<LociStatusInfo>();

        int row = 0, col = 0;
        foreach (var id in statusIds)
        {
            if (id== Guid.Empty)
                continue;

            if (!LociCache.Data.Statuses.TryGetValue(id, out var status))
                continue;

            if (status.IconID is 0)
                continue;

            LociIcon.Draw((uint)status.IconID, status.Stacks, iconSize);
            LociEx.AttachTooltip(status, LociCache.Data);

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

    public static void DrawTuples(List<LociStatusInfo> statuses, Vector2 iconSize)
    {
        using var _ = ImRaii.Group();
        // Calculate the remaining height in the region.
        for (var i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].IconID is 0)
                continue;

            LociIcon.Draw((uint)statuses[i].IconID, statuses[i].Stacks, iconSize);
            LociEx.AttachTooltip(statuses[i], LociCache.Data);

            if (i < statuses.Count - 1)
                ImUtf8.SameLineInner();
        }
    }

    public static void DrawTuples(IEnumerable<LociStatusInfo> statuses, float width, Vector2? iconSize = null, int rows = 1)
    {
        var size = iconSize ?? LociIcon.Size;
        var padding = ImGui.GetStyle().ItemInnerSpacing.X;
        var iconsPerRow = MathF.Floor((width - padding) / (size.X + padding));

        int row = 0, col = 0;
        foreach (var status in statuses)
        {
            if (status.IconID is 0)
                continue;

            LociIcon.Draw((uint)status.IconID, status.Stacks, size);
            LociEx.AttachTooltip(status, LociCache.Data);

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
}
