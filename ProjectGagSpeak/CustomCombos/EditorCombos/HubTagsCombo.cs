using Dalamud.Bindings.ImGui;

namespace GagSpeak.CustomCombos.Editor;

// Filter search combo for various hub tags. Allows for current to be cleared immidiately.
public sealed class HubTagsCombo : CkFilterComboCache<string>
{
    public HubTagsCombo(ILogger log, Func<IReadOnlyList<string>> generator)
    : base(generator, log)
    {
        SearchByParts = true;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, float width, float innerWidthScaler)
        => Draw(label, width, innerWidthScaler, CFlags.None);

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, float width, float innerWidthScaler, CFlags flags)
    {
        InnerWidth = width * innerWidthScaler;
        var previewLabel = "Select a Tag..";
        return Draw(label, previewLabel, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags);
    }

    public bool DrawPopup(string label, float width, Vector2 drawPos, uint? searchBg = null)
    {
        InnerWidth = width * 1.25f;
        return DrawPopup(label, drawPos, ImGui.GetTextLineHeightWithSpacing(), searchBg);
    }
}
