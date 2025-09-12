using CkCommons;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;

namespace GagSpeak.Gui.Wardrobe;

public class CollarRequestsOutgoingTab : IFancyTab
{
    private readonly CollarManager _manager;
    private readonly ClientData _requests;
    private readonly TutorialService _guides;
    public CollarRequestsOutgoingTab(CollarManager manager, ClientData requests, TutorialService guides)
    {
        _manager = manager;
        _requests = requests;
        _guides = guides;
    }

    public string   Label       => "Outgoing Requests";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false; //_requests.ReqCollarOutgoing.Count is 0;


    public void DrawContents(float width)
    {
        DrawDummy();
    }

    private void DrawDummy()
    {
        using var _ = CkRaii.HeaderChild("DummyChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 4));

        // Draw out the inner description field.
        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint(), CkStyle.ChildRounding(), 2 * ImGuiHelpers.GlobalScale))
        {
            using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0x00000000);
            var dummy = string.Empty;
            ImGui.InputTextMultiline("##DummyField", ref dummy, 200, ImGui.GetContentRegionAvail());
            // Draw a hint if no text is present.
            if (string.IsNullOrEmpty(dummy))
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, 0xFFBBBBBB, "Dummy Input Help Text..");
        }
    }
}
