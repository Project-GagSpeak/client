using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerClient;
using GagSpeak.State.Managers;
using ImGuiNET;
using OtterGui.Text;
namespace GagSpeak.CkCommons.Gui.Wardrobe;

public class ModPresetSelector(ModSettingPresetManager manager)
{
    public ModPresetContainer SelectedContainer => _selectedContainer;

    private string              _searchValue        = string.Empty;
    private ModPresetContainer  _selectedContainer  = new ModPresetContainer();
    private float               _listItemWidth      = 0;
    public void DrawSearch()
    {
        FancySearchBar.Draw("ModSearch", ImGui.GetContentRegionAvail().X, "Search for a Mod", ref _searchValue, 128);
    }

    public void DrawModSelector()
    {
        using var style         = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2f));
        using var buttonKiller  = ImRaii.PushColor(ImGuiCol.Button, 0xFF000000)
            .Push(ImGuiCol.ButtonHovered, 0xFF000000).Push(ImGuiCol.ButtonActive, 0xFF000000);

        _listItemWidth = ImGui.GetContentRegionAvail().X;
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetFrameHeight());
        var remainder = ImGuiClip.FilteredClippedDraw(manager.ModPresetStorage.OrderBy(x => x.DirectoryPath), skips, CheckFilter, DrawSelectable);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetFrameHeight());
    }

    private bool CheckFilter(ModPresetContainer modItem)
    {
        return string.IsNullOrWhiteSpace(_searchValue) 
            || modItem.ModName.Contains(_searchValue, StringComparison.OrdinalIgnoreCase)
            || modItem.DirectoryPath.Contains(_searchValue, StringComparison.OrdinalIgnoreCase);
    }

    private void DrawSelectable(ModPresetContainer modItem)
    {
        // must be a valid drag-drop source, so use invisible button.
        var selected = _selectedContainer.DirectoryPath == modItem.DirectoryPath;
        ImGui.InvisibleButton("##"+ modItem.DirectoryPath, new Vector2(_listItemWidth, ImGui.GetFrameHeight()));
        var hovered = ImGui.IsItemHovered();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = selected
            ? CkColor.ElementBG.Uint()
            : hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        // If it was double clicked, open it in the editor.
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            _selectedContainer = modItem;

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
            ImUtf8.TextFrameAligned(modItem.ModName);
        }

        if (hovered)
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            ImGui.Text(modItem.DirectoryPath);
        }
    }
}
