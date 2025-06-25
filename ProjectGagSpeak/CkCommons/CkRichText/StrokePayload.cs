using GagSpeak.CkCommons.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;

public class StrokePayload : RichPayload
{
    public uint Color { get; }
    public StrokePayload(uint color)
        => Color = color;

    public static StrokePayload Off => new(0);

    public override void Draw(CkRaii.RichColor c)
    {
        if (Color != 0)
            c.Push(CkRichCol.Stroke, Color);
        else
            c.Pop(CkRichCol.Stroke); // Pop the color if it's off.
    }

    public override void UpdateCache(ImFontPtr _, float __, ref float ___)
    { }
}
