using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Structs;

namespace GagSpeak.CustomCombos;

/// <summary> A combo for selecting worlds WITHOUT the Any World entry. Used for Lifestream IPC. </summary>
public sealed class WorldCombo : CkFilterComboCache<KeyValuePair<WorldId, string>>
{
    private ushort _current;
    public WorldCombo(ILogger log) : base(ItemSvc.WorldData.OrderBy(kvp => kvp.Value), log)
    {
        Current = new KeyValuePair<WorldId, string>(WorldId.AnyWorld, "Select World..");
        CurrentSelectionIdx = 0;
    }

    protected override string ToString(KeyValuePair<WorldId, string> obj)
        => obj.Value;

    /// <summary> Simple draw invoke. </summary>
    public bool Draw(ushort currentWorld, float width, CFlags flags = CFlags.None)
    {
        InnerWidth = width * 1.3f;
        _current = currentWorld;
        string previewName = Items.FirstOrDefault(x => x.Key == _current).Value ?? "Select World...";
        return Draw("##worldCombo", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags);
    }

    public bool DrawPopup(ushort currentWorld, float comboWidth, Vector2 drawPos, uint? searchBg = null)
    {
        InnerWidth = comboWidth;
        _current = currentWorld;

        return DrawPopup("##worldCombo", drawPos, ImGui.GetTextLineHeightWithSpacing(), searchBg);
    }
}
