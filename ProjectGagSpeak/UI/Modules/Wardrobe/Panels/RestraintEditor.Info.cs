using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;

public class RestraintEditorInfo : ICkTab
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
        if (_manager.ActiveEditorItem is not { } item)
            return;

        DrawDescription();

        _traitsDrawer.DrawOneRowTraits(item, ImGui.GetContentRegionAvail().X, Traits.None, false);
    }

    private void DrawDescription()
    {
        var childSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 4);
        using var _ = CkComponents.CenterHeaderChild("Description_BG", "Description", childSize, WFlags.AlwaysUseWindowPadding);

        using (CkComponents.FramedChild("DescriptionField", CkColor.FancyHeaderContrast.Uint(), ImGui.GetContentRegionAvail()))
        {
            using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0x00000000);
            var description = _manager.ActiveEditorItem!.Description;
            if (ImGui.InputTextMultiline("##DescriptionField", ref description, 200, ImGui.GetContentRegionAvail()))
                _manager.ActiveEditorItem!.Description = description;

            // Draw a hint if no text is present.
            if (description.IsNullOrWhitespace())
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, 0xFFBBBBBB, "Input a description in the space provided...");
        }
    }
}
