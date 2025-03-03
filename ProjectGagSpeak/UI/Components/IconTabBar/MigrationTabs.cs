using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.UI.Components;

public class MigrationTabs : IconTabBarBase<MigrationTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Gags,
        Restrictions,
        Restraints,
        CursedLoot,
        HardcoreTraitAllowances,
        Alarms,
        Triggers,
    }

    public MigrationTabs()
    {
        AddDrawButton(FontAwesomeIcon.CommentDots, SelectedTab.Gags, "Transfer Gag Storage Data" +
            "--SEP--Migrate Gag Storage from another Account!");

        AddDrawButton(FontAwesomeIcon.Ring, SelectedTab.Restrictions, "Transfer Restriction Storage" +
            "--SEP--Migrate Restrictions from another Account!");

        AddDrawButton(FontAwesomeIcon.DoorOpen, SelectedTab.Restraints, "Transfer Restraint Storage" +
            "--SEP--Migrate Restraints from another Account!");

        AddDrawButton(FontAwesomeIcon.Gem, SelectedTab.CursedLoot, "Transfer Cursed Loot Storage" +
            "--SEP--Migrate Cursed Loot from another Account!");

/*        AddDrawButton(FontAwesomeIcon.Lock, SelectedTab.HardcoreTraitAllowances, "Transfer Pair Trait Allowances" +
            "--SEP--Migrate Hardcore Trait Allowances from another Account!");*/

        AddDrawButton(FontAwesomeIcon.Bell, SelectedTab.Alarms, "Transfer Alarm Storage" +
            "--SEP--Migrate Alarms from another Account!");

        AddDrawButton(FontAwesomeIcon.Bullseye, SelectedTab.Triggers, "Transfer Trigger Storage" +
            "--SEP--Migrate Triggers from another Account!");
    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        using var btncolor = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = CkGui.IconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, drawList);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        btncolor.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

}
