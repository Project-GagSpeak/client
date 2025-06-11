using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Tutorial;
using GagSpeak.CkCommons.Gui.Components;
using ImGuiNET;
using GagSpeak.CkCommons.Raii;
using Dalamud.Interface.Utility;
using GagspeakAPI.Attributes;

namespace GagSpeak.CkCommons.Gui.Wardrobe;

public class RestraintEditorInfo : IFancyTab
{
    private readonly RestraintManager _manager;
    private readonly TraitsDrawer _traitsDrawer;
    private readonly TutorialService _guides;
    public RestraintEditorInfo(RestraintManager manager, TraitsDrawer traitsDrawer, TutorialService guides)
    {
        _traitsDrawer = traitsDrawer;
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

        _traitsDrawer.DrawOneRowTraits(item, ImGui.GetContentRegionAvail().X, Traits.None, false);
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
