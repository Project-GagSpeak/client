using GagSpeak.CkCommons.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;

/// <summary>
///     All payloads have various attributes, but the one thing they must all share is a way to update their cache.
/// </summary>
/// <remarks>
///     It is intentional that these do not have a virtual draw method.
///     using a virtual draw method will increase drawtime by upwards of 200%-300%. <para/>
///     keep whatever is being called for drawframes NON-VIRTUAL.
/// </remarks>
public abstract class RichPayload
{
    /// <summary>
    ///     Updates the _splitCache with the given <see cref="ImFontPtr"/> and <paramref name="wrapWidth"/>.
    ///     Calculation begins on the line's start width of <paramref name="startWidth"/>.
    /// </summary>
    /// <returns> The width of the last line in the split cache, or 0 if no lines were created. </returns>
    public abstract void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth);
}
