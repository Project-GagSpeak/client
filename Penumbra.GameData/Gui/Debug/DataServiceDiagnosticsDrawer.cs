using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui;
using OtterGui.Services;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draws a table of all data containers and their diagnostic data. </summary>
public class DataServiceDiagnosticsDrawer(ServiceManager manager) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Diagnostics";

    private string _filter = string.Empty;
    private int    _orderBy;

    /// <inheritdoc/>
    public void Draw()
    {
        ImGui.InputTextWithHint("##filter", "Filter...", ref _filter, 64);
        DrawSortCombo();
        using var table = ImRaii.Table("services", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("Name",           ImGuiTableColumnFlags.WidthFixed, 300 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Available",      ImGuiTableColumnFlags.WidthFixed, 55 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Setup Time",     ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Total Items",    ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Approx. Memory", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale);

        ImGui.TableHeadersRow();
        ImGui.TableSetupScrollFreeze(0, 1);
        var services = manager.GetServicesImplementing<IDataContainer>();
        services = _orderBy switch
        {
            1 => services.OrderByDescending(c => c.Time),
            2 => services.OrderByDescending(c => c.Memory),
            3 => services.OrderByDescending(c => c.TotalCount),
            _ => services.OrderBy(c => c.Name),
        };

        foreach (var c in services)
        {
            if (!c.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                continue;

            var finished = c is not IAsyncService a || a.Finished;
            ImGuiUtil.DrawTableColumn(c.Name);
            ImGuiUtil.DrawTableColumn(finished.ToString());
            if (!finished)
            {
                ImGui.TableNextRow();
            }
            else
            {
                ImGuiUtil.DrawTableColumn($"{c.Time / 1000}.{c.Time % 1000:D3} s");
                ImGuiUtil.DrawTableColumn(c.TotalCount.ToString());
                ImGuiUtil.DrawTableColumn(Functions.HumanReadableSize(c.Memory));
            }
        }
    }

    /// <inheritdoc/>
    public bool Disabled
        => false;

    /// <summary> Sort the table. </summary>
    private void DrawSortCombo()
    {
        string[] names = ["Name", "Time", "Memory", "Items"];

        using var combo = ImRaii.Combo("Sort Order", names[_orderBy]);
        if (!combo)
            return;

        foreach (var (name, idx) in names.WithIndex())
        {
            if (ImGui.Selectable(name, _orderBy == idx))
                _orderBy = idx;
        }
    }
}
