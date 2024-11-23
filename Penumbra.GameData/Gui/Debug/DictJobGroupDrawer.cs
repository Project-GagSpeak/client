using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Gui.Debug;

/// <summary> Draw either all collected or just the valid job groups. </summary>
public class DictJobGroupDrawer(DictJobGroup _jobGroups, DictJob _jobs) : IGameDataDrawer
{
    /// <inheritdoc/>
    public string Label
        => "Valid Job Groups";

    private bool _showAll;

    /// <inheritdoc/>
    public void Draw()
    {
        ImGui.Checkbox("Show All Job Groups", ref _showAll);

        using var table = ImRaii.Table("##groups", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        var enumerable = _showAll
            ? _jobGroups.AllJobGroups.Select(g => (g.Id.Id.ToString("D3"), g.Name, g.Count.ToString(), JobStrings(g)))
            : _jobGroups.Select(g => (g.Key.Id.ToString("D3"), g.Value.Name, g.Value.Count.ToString(), JobStrings(g.Value)));

        foreach (var (id, group, count, jobs) in enumerable)
        {
            ImGuiUtil.DrawTableColumn(id);
            ImGuiUtil.DrawTableColumn(group);
            ImGuiUtil.DrawTableColumn(count);
            ImGuiUtil.DrawTableColumn(jobs);
        }
    }

    private string JobStrings(JobGroup group)
        => string.Join(", ", group.Iterate().Select(j => $"{_jobs[j].Abbreviation} ({_jobs[j].Id.Id})"));

    /// <inheritdoc/>
    public bool Disabled
        => false;
}
