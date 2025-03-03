using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Restrictions;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;
public partial class RestrictionsPanel
{
    private void DrawEditor(Vector2 remainingRegion)
    {
        // All subsequent functions here can use 'ActiveEditorItem!' since it will be valid.
        if (_manager.ActiveEditorItem is null)
            return;

        // Dont care about drawint the data in a pretty format right away, just care that we are
        // drawing the data at all.

        // Sub-Function to call the general information (type selection, label and ID.)
        DrawGeneralInfo();
        ImGui.Separator();

        // Sub-Function using the EquipmentDrawer to manage the GlamourSlot item in then restriction.
        ImGui.Text("GlamourSlot Placeholder");
        ImGui.Separator();

        // Sub-Function to call the ModPresetDrawer to manage the ModAssociation item in the restriction.
        ImGui.Text("ModAssociation Placeholder");
        ImGui.Separator();

        // Sub-Function to call the MoodleDrawer to manage the Moodle item in the restriction.
        ImGui.Text("Moodle Placeholder");
        ImGui.Separator();

        // Sub-Function for drawing the Hardcore Traits applied by this restriction.
        ImGui.Text("Hardcore Traits Placeholder");
        ImGui.Separator();

        if(_manager.ActiveEditorItem is BlindfoldRestriction blindfold)
        {
            // Draw out the metadata toggles.
            ImGui.Text("Headgear State:");
            ImGui.Text("Visor State:");

            // Draw out the selection for the blindfold. If custom is selected, provide a dialog manager to import a path.
            ImGui.Text("Blindfold Selection:");

            if (blindfold.IsCustom)
            {
                ImGui.Text("Handling Custom Path:");
            }
        }
        else if(_manager.ActiveEditorItem is CollarRestriction collar)
        {
            // Draw out the information about it.
            ImGui.Text("Collar Owner: ");
            ImGui.SameLine();
            var ownerName = _pairs.TryGetNickAliasOrUid(collar.OwnerUID, out var name) ? name : collar.OwnerUID;
            CkGui.ColorText(ownerName, ImGuiColors.ParsedGold);

            ImGui.Text("Enscription: " + collar.CollarWriting);
        }
    }

    private void DrawGeneralInfo()
    {
        // Draw the general information about the restriction.
        ImGui.Text("Type: " + _manager.ActiveEditorItem!.Type);
        ImGui.Text("Label: " + _manager.ActiveEditorItem!.Label);
        ImGui.Text("ID: " + _manager.ActiveEditorItem!.Identifier);
    }
}
