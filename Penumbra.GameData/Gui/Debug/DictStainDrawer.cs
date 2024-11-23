using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.DataContainers;
using ImGuiClip = OtterGui.ImGuiClip;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draw collected stain data. </summary>
public class DictStainDrawer(DictStain _stains) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Stains";

    /// <inheritdoc/>
    public bool Disabled
        => !_stains.Finished;

    private string _stainFilter = string.Empty;

    /// <inheritdoc/>
    public void Draw()
    {
        var resetScroll = ImGui.InputTextWithHint("##filter", "Filter...", ref _stainFilter, 256);
        var height      = ImGui.GetTextLineHeightWithSpacing() + 2 * ImGui.GetStyle().CellPadding.Y;
        using var table = ImRaii.Table("##table", 4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.SizingFixedFit,
            new Vector2(-1, 10 * height));
        if (!table)
            return;

        if (resetScroll)
            ImGui.SetScrollY(0);

        ImGui.TableNextColumn();
        var skips = ImGuiClip.GetNecessarySkips(height);
        ImGui.TableNextRow();
        var remainder = ImGuiClip.FilteredClippedDraw(_stains, skips,
            p => p.Key.Id.ToString().Contains(_stainFilter) || p.Value.Name.Contains(_stainFilter, StringComparison.OrdinalIgnoreCase),
            p =>
            {
                ImGuiUtil.DrawTableColumn(p.Key.Id.ToString("D3"));
                ImGui.TableNextColumn();
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetCursorScreenPos(),
                    ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()),
                    p.Value.RgbaColor, 5 * ImGuiHelpers.GlobalScale);
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
                ImGuiUtil.DrawTableColumn(p.Value.Name);
                ImGuiUtil.DrawTableColumn($"#{p.Value.R:X2}{p.Value.G:X2}{p.Value.B:X2}{(p.Value.Gloss ? ", Glossy" : string.Empty)}");
            });
        ImGuiClip.DrawEndDummy(remainder, height);
    }
}
