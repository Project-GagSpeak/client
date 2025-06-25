using GagSpeak.CkCommons.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;

public class NewLinePayload : RichPayload
{
    public override void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth)
    {
        curLineWidth = 0f;
    }
}
