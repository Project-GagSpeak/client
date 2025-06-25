using GagSpeak.CkCommons.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;

public class NewLinePayload : RichPayload
{
    public NewLinePayload()
    { }

    public override void Draw(CkRaii.RichColor _)
        => ImGui.Spacing(); // we do this over ImGui.NewLine() to prevent excessive spacing, as this payload is meant to serve as a "text area split"

    public override void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth)
    {
        curLineWidth = 0f;
    }
}
