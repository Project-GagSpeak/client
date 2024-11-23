using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using ImGuiClip = Dalamud.Interface.Utility.ImGuiClip;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Utility for drawing debug data. </summary>
public static class DebugUtility
{
    /// <summary> Draw a filtered table for a set of IDs and names. </summary>
    /// <param name="label"> The label for the object. </param>
    /// <param name="filter"> A filter for the displayed names. Can also filter for the IDs. </param>
    /// <param name="withTree"> Whether to wrap the table into a tree node. </param>
    /// <param name="names"> The enumerable list of names and IDs. </param>
    public static void DrawNameTable(string label, ref string filter, bool withTree, IEnumerable<(ulong, string)> names)
    {
        using var tree = withTree ? ImRaii.TreeNode(label) : null;
        if (withTree && !tree!.Success)
            return;

        using var _           = ImRaii.PushId(label);
        var       resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref filter, 256);
        var       height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextColumn();
        var f = filter;
        var remainder = ImGuiClip.FilteredClippedDraw(names.Select(p => (p.Item1.ToString("D5"), p.Item2)), skips,
            p => p.Item1.Contains(f) || p.Item2.Contains(f, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Item1);
                ImGuiUtil.DrawTableColumn(p.Item2);
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }
}
