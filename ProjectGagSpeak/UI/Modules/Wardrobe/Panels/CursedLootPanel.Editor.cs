using Dalamud.Interface.Utility;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace GagSpeak.UI.Wardrobe;
public partial class CursedLootPanel
{
    private void DrawEditor(Vector2 remainingRegion)
    {
        // All subsequent functions here can use 'ActiveEditorItem!' since it will be valid.
        if (_manager.ActiveEditorItem is null)
            return;

        // Dont care about drawint the data in a pretty format right away, just care that we are
        // drawing the data at all.

        // Sub-Function to call the general information (Label, precedence, )
        DrawGeneralInfo();
        ImGui.Separator();

        // Restriction selector would go here.
    }

    private void DrawGeneralInfo()
    {
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        var itemName = _manager.ActiveEditorItem!.Label;
        if (ImGui.InputTextWithHint("##ItemName", "Item Name...", ref itemName, 36))
            _manager.ActiveEditorItem!.Label = itemName;
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.NamingCursedItems, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);


        var canOverride = _manager.ActiveEditorItem!.CanOverride;
        if (ImGui.Checkbox("Overridable", ref canOverride))
            _manager.ActiveEditorItem!.CanOverride = canOverride;
        UiSharedService.AttachToolTip("If this item can be overridden by another cursed item in the pool."
            + Environment.NewLine + "(Must have a higher Precedence to do so)");


        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Very Highmmm").X);
        var precedence = _manager.ActiveEditorItem!.Precedence;
        _ui.DrawCombo("##ItemPrecedence", ImGui.CalcTextSize("Very Highmmm").X, Enum.GetValues<Precedence>(),
            (clicked) => clicked.ToName(), onSelected: (i) => _manager.ActiveEditorItem!.Precedence = i,
            initialSelectedItem: _manager.ActiveEditorItem!.Precedence);
        UiSharedService.AttachToolTip("The Precedence of this item when comparing to other items in the pool."
            + Environment.NewLine + "Items with higher Precedence will be layered ontop of items in the same slot with lower Precedence.");
    }
}
