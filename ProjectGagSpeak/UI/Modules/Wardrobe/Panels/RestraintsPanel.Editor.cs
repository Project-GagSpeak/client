using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;


// The partial component responsible for editing a restraint set.
public partial class RestraintsPanel
{
    /// <summary> Calls all sub-functions here for the editor. </summary>
    private void DrawEditor(Vector2 remainingRegion)
    {
        // All subsequent functions here can use 'ActiveEditorItem!' since it will be valid.
        if (_manager.ActiveEditorItem is null)
            return;

        // Dont care about drawint the data in a pretty format right away, just care that we are
        // drawing the data at all.
        DrawInfo();
        ImGui.Separator();
        DrawAppearance();
        ImGui.Separator();
        DrawMoodles();
        DrawModPresets();
        ImGui.Separator();
        DrawSpatialAudio();
    }

    // Everything below here we use as reference
    private void DrawInfo()
    {
        var width = ImGui.GetContentRegionAvail().X * .7f;
        ImGui.Text("Set Name:");
        var refName = _manager.ActiveEditorItem!.Label;
        ImGui.SetNextItemWidth(width);
        if (ImGui.InputTextWithHint("##Name", "Set Name...", ref refName, 48))
            _manager.ActiveEditorItem!.Label = refName;
        UiSharedService.AttachToolTip("Gives the Restraint Set a name!");

        ImGui.Text("Restraint Set Description:");
        var desc = _manager.ActiveEditorItem!.Description;
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey2))
            if (ImGui.InputTextMultiline("##Description", ref desc, 150, new Vector2(width, 100f)))
                _manager.ActiveEditorItem!.Description = desc;
        UiSharedService.AttachToolTip("Gives the Restraint Set a description!");
    }

    // Needs major rework.
    private void DrawAppearance()
    {
        ImGui.Text("Appearance:");
        ImGui.Text("Placeholder for Appearance Selection.");
    }

    private void DrawModPresets()
    {
        ImGui.Text("Mod Presets:");
        ImGui.Text("Placeholder for Mod Preset Selection.");
    }

    private void DrawMoodles()
    {
        ImGui.Text("Moodles:");
        ImGui.Text("Placeholder for Moodle Selection.");
    }

    private void DrawSpatialAudio()
    {
        _uiShared.BigText("Select if Restraint Set Uses:\nRopes, Chains, Leather, Latex, ext* here.");
        ImGui.Text("They will then play immersive spatial audio on queue.");
    }
}
