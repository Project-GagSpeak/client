using GagSpeak.CkCommons.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;

public abstract class RichPayload
{
    public bool IsInline { get; protected set; } = false;
    /// <summary>
    ///     Draws the payload to the ImGui context.
    /// </summary>
    public abstract void Draw(CkRaii.RichColor colorStack);

    /// <summary>
    ///     Updates the _splitCache with the given <see cref="ImFontPtr"/> and <paramref name="wrapWidth"/>.
    ///     Calculation begins on the line's start width of <paramref name="startWidth"/>.
    /// </summary>
    /// <returns> The width of the last line in the split cache, or 0 if no lines were created. </returns>
    public abstract void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth);
}
