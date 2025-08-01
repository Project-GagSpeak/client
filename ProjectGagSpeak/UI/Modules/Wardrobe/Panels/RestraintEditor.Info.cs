using Dalamud.Interface.Utility.Raii;
using CkCommons.Widgets;
using GagSpeak.Services.Tutorial;
using GagSpeak.Gui.Components;
using ImGuiNET;
using CkCommons.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using GagSpeak.State.Managers;
using GagspeakAPI.Attributes;
using CkCommons;

namespace GagSpeak.Gui.Wardrobe;

public class RestraintEditorInfo : IFancyTab
{
    private readonly RestraintManager _manager;
    private readonly AttributeDrawer _attributeDrawer;
    private readonly TutorialService _guides;
    public RestraintEditorInfo(RestraintManager manager, AttributeDrawer traitsDrawer, TutorialService guides)
    {
        _attributeDrawer = traitsDrawer;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Info & Traits";
    public string   Tooltip     => "View and edit the traits and information of the selected item.";
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        if (_manager.ItemInEditor is not { } item)
            return;

        DrawDescription();
        _attributeDrawer.DrawAttributesChild(item, width, 8, Traits.All);
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.HardcoreTraits, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.Arousal, ImGui.GetWindowPos(), ImGui.GetWindowSize());
    }

    private void DrawDescription()
    {
        using var _ = CkRaii.HeaderChild("Description", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 4));

        // Draw out the inner description field.
        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint(), CkStyle.ChildRounding(), 2 * ImGuiHelpers.GlobalScale))
        {
            using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0x00000000);
            var description = _manager.ItemInEditor!.Description;
            if (ImGui.InputTextMultiline("##DescriptionField", ref description, 200, ImGui.GetContentRegionAvail()))
                _manager.ItemInEditor!.Description = description;

            // Draw a hint if no text is present.
            if (description.IsNullOrWhitespace())
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, 0xFFBBBBBB, "Input a description in the space provided...");
        }
    }
}
