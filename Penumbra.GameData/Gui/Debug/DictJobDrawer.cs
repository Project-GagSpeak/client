using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.DataContainers;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draw all collected jobs. </summary>
public class DictJobDrawer(DictJob _jobs) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Jobs";


    /// <inheritdoc/>
    public void Draw()
    {
        using var table = ImRaii.Table("##jobs", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        foreach (var (id, job) in _jobs)
        {
            ImGuiUtil.DrawTableColumn(id.Id.ToString("D3"));
            ImGuiUtil.DrawTableColumn(job.Name);
            ImGuiUtil.DrawTableColumn(job.Abbreviation);
        }
    }

    /// <inheritdoc/>
    public bool Disabled
        => false;
}
