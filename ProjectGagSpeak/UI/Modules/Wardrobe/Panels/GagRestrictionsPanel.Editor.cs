using Dalamud.Interface.ImGuiFileDialog;
using GagSpeak.FileSystems;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Raii;

namespace GagSpeak.UI.Wardrobe;
public partial class GagRestrictionsPanel
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

        // Draw out the metadata toggles & C+ preset drawer.
        ImGui.Text("Headgear State:");
        ImGui.Text("Visor State:");
        ImGui.Text("Customize Plus Profile: ");
        ImGui.Text("Profile Priority: ");
        ImGui.Text("Redraw On Application?");
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
    }

    private void DrawGeneralInfo()
    {
        // Draw the general information about the restriction.
        ImGui.Text("GagType: " + _manager.ActiveEditorItem!.GagType);

        var state = _manager.ActiveEditorItem!.IsEnabled;
        if(ImGui.Checkbox("Visuals Enabled", ref state))
            _manager.ActiveEditorItem!.IsEnabled = state;

        var redraw = _manager.ActiveEditorItem!.DoRedraw;
        if(ImGui.Checkbox("Redraw On Apply", ref redraw))
            _manager.ActiveEditorItem!.DoRedraw = redraw;
    }

    private void DrawGagGlamour()
    {
/*        using (ImRaii.Group())
        {
            UnsavedDrawData.GameItem.DrawIcon(_itemStainHandler.IconData, IconSize, UnsavedDrawData.Slot);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Logger.LogTrace($"Item changed to {ItemService.NothingItem(UnsavedDrawData.Slot)} [{ItemService.NothingItem(UnsavedDrawData.Slot).ItemId}] " +
                    $"from {UnsavedDrawData.GameItem} [{UnsavedDrawData.GameItem.ItemId}]");
                UnsavedDrawData.GameItem = ItemService.NothingItem(UnsavedDrawData.Slot);
            }
            // right beside it, draw a secondary group of 3
            ImGui.SameLine(0, 6);
            using (var group = ImRaii.Group())
            {
                // display the wardrobe slot for this gag
                var refValue = Array.IndexOf(EquipSlotExtensions.EqdpSlots.ToArray(), UnsavedDrawData.Slot);
                ImGui.SetNextItemWidth(ComboLength);
                if (ImGui.Combo(" Equipment Slot##WardrobeEquipSlot", ref refValue,
                    EquipSlotExtensions.EqdpSlots.Select(slot => slot.ToName()).ToArray(), EquipSlotExtensions.EqdpSlots.Count))
                {
                    // Update the selected slot when the combo box selection changes
                    UnsavedDrawData.Slot = EquipSlotExtensions.EqdpSlots[refValue];
                    // reset display and/or selected item to none.
                    UnsavedDrawData.GameItem = ItemService.NothingItem(UnsavedDrawData.Slot);
                }

                DrawEquip(GameItemCombo, StainCombo, ComboLength);
            }
        }*/
    }

    private void DrawGagMeta()
    {

    }

    private void DrawGagCustomizePlus()
    {

    }

    private void DrawGagModAssociation()
    {

    }

    private void DrawGagMoodle()
    {
        /*try
        {
            using var table = ImRaii.Table("MoodlesSelections", 2, ImGuiTableFlags.BordersInnerV);
            if (!table) return;

            ImGui.TableSetupColumn("MoodleSelection", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("FinalizedPreviewList", ImGuiTableColumnFlags.WidthFixed, 200f);

            ImGui.TableNextRow(); ImGui.TableNextColumn();

            using (var child = ImRaii.Child("##RestraintMoodleStatusSelection", new(ImGui.GetContentRegionAvail().X - 1f, ImGui.GetContentRegionAvail().Y / 2), false))
            {
                if (!child) return;
                _relatedMoodles.DrawMoodlesStatusesListForItem(UnsavedDrawData, LastCreatedCharacterData, cellPaddingY, false);
            }
            ImGui.Separator();
            using (var child2 = ImRaii.Child("##RestraintMoodlePresetSelection", -Vector2.One, false))
            {
                if (!child2) return;
                _relatedMoodles.DrawMoodlesStatusesListForItem(UnsavedDrawData, LastCreatedCharacterData, cellPaddingY, true);
            }


            ImGui.TableNextColumn();
            // Filter the MoodlesStatuses list to get only the moodles that are in AssociatedMoodles
            var associatedMoodles = LastCreatedCharacterData.MoodlesStatuses
                .Where(moodle => UnsavedDrawData.AssociatedMoodles.Contains(moodle.GUID))
                .ToList();
            // draw out all the active associated moodles in the restraint set with thier icon beside them.
            CkGui.ColorText("Moodles Applied with Set:", ImGuiColors.ParsedPink);
            ImGui.Separator();
            foreach (var moodle in associatedMoodles)
            {
                using (var group = ImRaii.Group())
                {

                    var currentPos = ImGui.GetCursorPos();
                    if (moodle.IconID != 0 && currentPos != Vector2.Zero)
                    {
                        var statusIcon = CkGui.GetGameStatusIcon((uint)((uint)moodle.IconID + moodle.Stacks - 1));

                        if (statusIcon is { } wrap)
                        {
                            ImGui.SetCursorPos(currentPos);
                            ImGui.Image(statusIcon.ImGuiHandle, MoodlesService.StatusSize);
                        }
                    }
                    ImGui.SameLine();
                    var shiftAmmount = (MoodlesService.StatusSize.Y - ImGui.GetTextLineHeight()) / 2;
                    ImGui.SetCursorPosY(currentPos.Y + shiftAmmount);
                    ImGui.Text(moodle.Title);
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error Drawing Moodles Options for Selected Gag.");
        }*/
    }

    private void DrawGagTraits()
    {

    }

}
