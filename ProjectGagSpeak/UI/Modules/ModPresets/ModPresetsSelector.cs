using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UI.Components;
using ImGuiNET;
using ImGuizmoNET;
using OtterGui.Classes;
using OtterGui.Text;
namespace GagSpeak.UI.Wardrobe;

public class ModPresetSelector(ModSettingPresetManager manager)
{
    public Mod SelectedMod => _selectedMod;

    private string  _searchValue    = string.Empty;
    private Mod     _selectedMod    = new Mod(string.Empty, string.Empty);
    private float   _listItemWidth  = 0;
    public void DrawSearch()
    {
        DrawerHelpers.FancySearchFilter("ModSearch", ImGui.GetContentRegionAvail().X, "Search for a Mod", ref _searchValue, 128);
    }

    public void DrawModSelector()
    {
        using var style         = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2f));
        using var buttonKiller  = ImRaii.PushColor(ImGuiCol.Button, 0xFF000000)
            .Push(ImGuiCol.ButtonHovered, 0xFF000000).Push(ImGuiCol.ButtonActive, 0xFF000000);

        _listItemWidth = ImGui.GetContentRegionAvail().X;
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetFrameHeight());
        var remainder = ImGuiClip.FilteredClippedDraw(manager._modList.OrderBy(x => x.DirectoryName), skips, CheckFilter, DrawSelectable);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetFrameHeight());
    }

    private bool CheckFilter(Mod modItem)
    {
        return string.IsNullOrWhiteSpace(_searchValue) 
            || modItem.Name.Contains(_searchValue, StringComparison.OrdinalIgnoreCase)
            || modItem.DirectoryName.Contains(_searchValue, StringComparison.OrdinalIgnoreCase);
    }

    private void DrawSelectable(Mod modItem)
    {
        // must be a valid drag-drop source, so use invisible button.
        var selected = _selectedMod.DirectoryName == modItem.DirectoryName;
        ImGui.InvisibleButton("##"+ modItem.DirectoryName, new Vector2(_listItemWidth, ImGui.GetFrameHeight()));
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = selected
            ? CkColor.ElementBG.Uint()
            : hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        // If it was double clicked, open it in the editor.
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            _selectedMod = modItem;

        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);
        if (selected)
        {
            ImGui.GetWindowDrawList().AddRectFilled(rectMin, new Vector2(rectMin.X + ImGuiHelpers.GlobalScale * 3, rectMax.Y),
                CkGui.Color(ImGuiColors.ParsedPink), 5);
        }

        using (ImRaii.Group())
        {
            ImGui.SetCursorScreenPos(rectMin);
            CkGui.FramedIconText(FAI.FileArchive);
            ImGui.SameLine();
            ImUtf8.TextFrameAligned(modItem.Name);
        }

        if (hovered)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            ImGui.Text(modItem.DirectoryName);
        }
    }
}
