using GagSpeak.CkCommons.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;
public class SeperatorPayload : RichPayload
{
    public SeperatorPayload()
    { }

    public override void Draw(CkRaii.RichColor _)
        => ImGui.Separator();

    public override void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth)
    {
        // reset the current line width to 0 since we move to a new line.
        curLineWidth = 0f;
    }
}
