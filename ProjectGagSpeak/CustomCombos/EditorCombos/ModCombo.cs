using Dalamud.Interface.Utility;
using GagSpeak.Interop;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Extensions;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class ModCombo : CkFilterComboCache<ModInfo>
{
    private string _currentPath;
    public ModCombo(ILogger log, Func<IReadOnlyList<ModInfo>> generator)
    : base(generator, log)
    {
        SearchByParts = false;
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current?.DirPath == _currentPath)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.DirPath == _currentPath);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, string currentModItem, float width, float innerWidthScaler)
        => Draw(label, currentModItem, width, innerWidthScaler, CFlags.None);

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, string currentModItem, float width, float innerWidthScaler, CFlags flags)
    {
        InnerWidth = width * innerWidthScaler;
        _currentPath = currentModItem;
        var previewLabel = Items.FirstOrDefault(i => i.DirPath == _currentPath)?.Name ?? "Select a Mod...";
        return Draw(label, previewLabel, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags);
    }

    protected override string ToString(ModInfo Mod)
        => Mod.Name;

    protected override bool IsVisible(int globalIndex, LowerString filter)
        => filter.IsContained(Items[globalIndex].Name) || filter.IsContained(Items[globalIndex].DirPath);

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var mod = Items[globalIdx];
        var ret = ImUtf8.Selectable(mod.Name, selected);

        // draws a fancy box when the mod is hovered giving you the details about the mod.
        if (ImGui.IsItemHovered())
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            var namesDifferent = mod.Name != mod.DirPath;
            ImGui.Dummy(new Vector2(300 * ImGuiHelpers.GlobalScale, 0));
            ImUtf8.TextFrameAligned("Directory Name");
            ImGui.SameLine(Math.Max(ImGui.GetItemRectSize().X + 3 * ImGui.GetStyle().ItemSpacing.X, 150 * ImGuiHelpers.GlobalScale));
            ImUtf8.TextFrameAligned(mod.DirPath);
        }
        return ret;
    }
}
