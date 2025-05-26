using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.CkCommons.Gui.Components;
using ImGuiNET;
using Penumbra.GameData.Enums;

namespace GagSpeak.CkCommons.Gui.Wardrobe;

public class RestraintEditorEquipment : IFancyTab
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
        if (_manager.ItemInEditor is not { } item)
            return;

        // Calculate the size of the left and right windows by the region - spacing of window padding for equal distribution.
        var subWindowSize = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / 2, ImGui.GetContentRegionAvail().Y);

        // Draw out one area for the equipment. (And customization management.)
        using (CkRaii.IconButtonHeaderChild("Equipment", FAI.FileImport, subWindowSize, ImportEquipment, FancyTabBar.Rounding, 
            HeaderFlags.CR_HeaderCentered, "Import current Equipment on your Character."))
        {
            var region = ImGui.GetContentRegionAvail();
            var innerWidth = region.X;
            foreach (var slot in EquipSlotExtensions.EquipmentSlots)
            {
                ImGui.Spacing();
                _equipDrawer.DrawRestraintSlot(_manager.ItemInEditor.RestraintSlots, slot, innerWidth);
            }
        }

        ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X);
        // Draw out one area for the accessories. (And glasses management.)
        using (CkRaii.IconButtonHeaderChild("Accessories", FAI.FileImport, subWindowSize, ImportAccessories, FancyTabBar.Rounding,
            HeaderFlags.CR_HeaderCentered, "Import current Accessories on your Character."))
        {
            var region = ImGui.GetContentRegionAvail();
            var innerWidth = region.X;

            foreach (var slot in EquipSlotExtensions.AccessorySlots)
            {
                ImGui.Spacing();
                _equipDrawer.DrawRestraintSlot(_manager.ItemInEditor.RestraintSlots, slot, innerWidth);
            }

            // Draw out the glasses management.
            ImGui.Spacing();
            _equipDrawer.DrawGlassesSlot(_manager.ItemInEditor.Glasses, innerWidth);
        }
    }

    private void ImportEquipment()
    {
        if(_equipDrawer.TryImportCurrentGear(out var curGear))
            foreach (var (slot, item) in curGear) // Update each slot.
                _manager.ItemInEditor!.RestraintSlots[slot] = item;
        else
            _logger.LogWarning("Failed to import current gear from the character.");
    }

    private void ImportAccessories()
    {
        if (_equipDrawer.TryImportCurrentAccessories(out var curAcc))
            foreach (var (slot, item) in curAcc) // Update each slot.
                _manager.ItemInEditor!.RestraintSlots[slot] = item;
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
