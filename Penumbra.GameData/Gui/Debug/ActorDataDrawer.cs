using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using ImGuiClip = Dalamud.Interface.Utility.ImGuiClip;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draw all collected data for actor identification management. </summary>
public class ActorDataDrawer(NameDicts _dicts, DictBNpcNames _bNpcNames) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Actor Data";

    private string _bNpcFilter      = string.Empty;
    private string _eNpcFilter      = string.Empty;
    private string _companionFilter = string.Empty;
    private string _mountFilter     = string.Empty;
    private string _ornamentFilter  = string.Empty;
    private string _worldFilter     = string.Empty;

    /// <inheritdoc/>
    public void Draw()
    {
        DrawBNpcTable();
        DebugUtility.DrawNameTable("Event NPCs", ref _eNpcFilter,      true, _dicts.ENpcs.Select(kvp => ((ulong)kvp.Key.Id, kvp.Value)));
        DebugUtility.DrawNameTable("Companions", ref _companionFilter, true, _dicts.Companions.Select(kvp => ((ulong)kvp.Key.Id, kvp.Value)));
        DebugUtility.DrawNameTable("Mounts",     ref _mountFilter,     true, _dicts.Mounts.Select(kvp => ((ulong)kvp.Key.Id, kvp.Value)));
        DebugUtility.DrawNameTable("Ornaments",  ref _ornamentFilter,  true, _dicts.Ornaments.Select(kvp => ((ulong)kvp.Key.Id, kvp.Value)));
        DebugUtility.DrawNameTable("Worlds",     ref _worldFilter,     true, _dicts.Worlds.Select(kvp => ((ulong)kvp.Key.Id, kvp.Value)));
    }

    /// <summary> Draw a list of battle NPCs associated with names and base IDs. </summary>
    private void DrawBNpcTable()
    {
        using var tree = ImRaii.TreeNode("Battle NPCs");
        if (!tree)
            return;

        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref _bNpcFilter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);
        ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("2", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("3", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextRow();
        var data = _dicts.BNpcs.Select(kvp => (kvp.Key, kvp.Key.Id.ToString("D5"), kvp.Value));
        var remainder = ImGuiClip.FilteredClippedDraw(data, skips,
            p => p.Item2.Contains(_bNpcFilter) || p.Value.Contains(_bNpcFilter, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Item2);
                ImGuiUtil.DrawTableColumn(p.Value);
                var bNpcs = _bNpcNames.GetBNpcsFromName(p.Key.BNpcNameId);
                ImGuiUtil.DrawTableColumn(string.Join(", ", bNpcs.Select(b => b.Id.ToString())));
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }

    /// <inheritdoc/>
    public bool Disabled
        => !_dicts.Finished || !_bNpcNames.Finished;
}
