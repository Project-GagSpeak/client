using Dalamud.Plugin.Services;
using GagSpeak.UpdateMonitoring;
using ImGuiNET;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.EditorCombos;

/// <summary> Capable of displaying every valid emote, along with its icon and all command variants. </summary>
public sealed class JobActionCombo : CkFilterComboCache<ClientMonitor.ActionRowLight>
{
    private readonly ITextureProvider _iconDrawer;
    public JobActionCombo(int jobId, ITextureProvider iconDrawer, ILogger log)
        : base(GetActionsForJob(jobId), log)
    {
        _iconDrawer = iconDrawer;
        SearchByParts = true;
    }

    private static IReadOnlyList<ClientMonitor.ActionRowLight> GetActionsForJob(int jobId)
        => ClientMonitor.LoadedActions[jobId].OrderBy(a => a.RowId).ToList();

    public bool Draw(string label, float width, float innerWidthScaler, float itemH, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        InnerWidth = width * innerWidthScaler;
        // if we have a new item selected we need to update some conditionals.
        var previewLabel = CurrentSelection.Name ?? "Select an Action...";
        return Draw(label, previewLabel, string.Empty, width, itemH, flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var lightJobAction = Items[globalIdx];

        // Draw a ghost selectable at first.
        var startPos = ImGui.GetCursorPos();
        var ret = ImGui.Selectable("##JobAction" + globalIdx, selected, ImGuiSelectableFlags.None, new Vector2(0, 24));
        var endPos = ImGui.GetCursorPos();

        // Use these positions to go back over and draw it properly this time.
        ImGui.SetCursorPos(startPos);
        using (ImRaii.Group())
        {
            var icon = _iconDrawer.GetFromGameIcon(lightJobAction.GetIconId()).GetWrapOrDefault();
            if(icon is { } wrap)
                ImGui.Image(icon.ImGuiHandle, new Vector2(24, 24));

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(lightJobAction.Name);
        }
        // Correct cursor position.
        ImGui.SetCursorPos(endPos);
        return ret;
    }
    protected override string ToString(ClientMonitor.ActionRowLight jobAction) => jobAction.Name;

    protected override void OnClosePopup()
    {
        var split = Filter.Text.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2 || !ushort.TryParse(split[0], out var setId) || !byte.TryParse(split[1], out var variant))
            return;
    }
}

