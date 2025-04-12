using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Drawing;

namespace GagSpeak.UI.Wardrobe;

public class RestraintEditorEquipment : ICkTab
{
    private readonly ILogger<RestraintEditorEquipment> _logger;
    private readonly RestraintSetFileSelector _selector;
    private readonly RestraintManager _manager;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public RestraintEditorEquipment(ILogger<RestraintEditorEquipment> logger, 
        RestraintSetFileSelector selector, EquipmentDrawer equipDrawer, 
        RestraintManager manager, CosmeticService cosmetics, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _equipDrawer = equipDrawer;
        _manager = manager;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    public string   Label       => "Equip Slots";
    public string   Tooltip     => "Configure Gear and Customizations.";
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ActiveEditorItem is not { } item)
            return;

        // Calculate the size of the left and right windows by the region - spacing of window padding for equal distribution.
        var subWindowSize = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / 2, ImGui.GetContentRegionAvail().Y);

        // Draw out one area for the equipment. (And customization management.)
        using (CkComponents.ButtonHeaderChild("Equipment", "Equipment", subWindowSize, FancyTabBar.Rounding, WFlags.AlwaysUseWindowPadding,
            FAI.FileImport, "Import current Equipment on your Character.", ImportEquipment))
        {
            var region = ImGui.GetContentRegionAvail();
            var innerWidth = region.X;
            foreach (var slot in EquipSlotExtensions.EquipmentSlots)
            {
                ImGui.Spacing();
                _equipDrawer.DrawRestraintSlot(_manager.ActiveEditorItem.RestraintSlots, slot, innerWidth);
            }
        }

        ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X);
        // Draw out one area for the accessories. (And glasses management.)
        using (CkComponents.ButtonHeaderChild("Accessories", "Accessories", subWindowSize, FancyTabBar.Rounding, WFlags.AlwaysUseWindowPadding,
            FAI.FileImport, "Import current Accessories on your Character.", ImportAccessories))
        {
            var region = ImGui.GetContentRegionAvail();
            var innerWidth = region.X;

            foreach (var slot in EquipSlotExtensions.AccessorySlots)
            {
                ImGui.Spacing();
                _equipDrawer.DrawRestraintSlot(_manager.ActiveEditorItem.RestraintSlots, slot, innerWidth);
            }

            // Draw out the glasses management.
            ImGui.Spacing();
            _equipDrawer.DrawGlassesSlot(_manager.ActiveEditorItem.Glasses, innerWidth);
        }
    }

    private void ImportEquipment()
    {
        if(_equipDrawer.TryImportCurrentGear(out Dictionary<EquipSlot, RestraintSlotBasic> curGear))
            foreach (var (slot, item) in curGear) // Update each slot.
                _manager.ActiveEditorItem!.RestraintSlots[slot] = item;
        else
            _logger.LogWarning("Failed to import current gear from the character.");
    }

    private void ImportAccessories()
    {
        if (_equipDrawer.TryImportCurrentAccessories(out Dictionary<EquipSlot, RestraintSlotBasic> curAcc))
            foreach (var (slot, item) in curAcc) // Update each slot.
                _manager.ActiveEditorItem!.RestraintSlots[slot] = item;
        else
            _logger.LogWarning("Failed to import current accessories from the character.");
    }

    private void ImportCustomizationData()
    {

    }

    private void ClearCustomizationData()
    {

    }
}
