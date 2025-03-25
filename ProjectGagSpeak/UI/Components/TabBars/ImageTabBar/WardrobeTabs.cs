using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using ImGuiNET;

namespace GagSpeak.UI.Components;

public class WardrobeTabs : ImageTabBar<WardrobeTabs.SelectedTab>
{
    public enum SelectedTab
    {
        MyRestraints,
        MyRestrictions,
        MyGags,
        MyCursedLoot,
    }

    public WardrobeTabs() { }

    public override void Draw(Vector2 region)
    {
        if (_tabButtons.Count == 0)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var _ = ImRaii.Child("ImageTabBar", region, false, ImGuiWindowFlags.NoDecoration);

        var buttonSize = new Vector2(region.Y);
        var spacingBetweenButtons = (region.X - buttonSize.X * _tabButtons.Count) / (_tabButtons.Count + 1);

        var pos = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(new Vector2(pos.X + spacingBetweenButtons, pos.Y));

        foreach (var tab in _tabButtons)
        {
            DrawTabButton(tab, buttonSize, ImGui.GetWindowDrawList());
            ImGui.SameLine(0, spacingBetweenButtons);
        }

        ImGui.SetCursorScreenPos(pos);
    }

}
