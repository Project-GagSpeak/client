using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;

public class ColorPayload : RichPayload
{
    public uint Color { get; }
    public ColorPayload(uint color)
        => Color = color;

    public static ColorPayload Off => new(0);

    public override void Draw(CkRaii.RichColor c)
    {
        if (Color != 0)
            c.Push(CkRichCol.Text, Color);
        else
            c.Pop(CkRichCol.Text); // Pop the color if it's off.
        ImRaii.Color
    }

    public override void UpdateCache(ImFontPtr _, float __, ref float ___)
    { }
}
