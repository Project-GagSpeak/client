using GagSpeak.Services;
using ImGuiNET;
using Penumbra.GameData.Structs;

namespace GagSpeak.CustomCombos;

/// <summary> A combo for selecting worlds WITHOUT the Any World entry. Used for Lifestream IPC. </summary>
public sealed class WorldCombo : CkFilterComboCache<KeyValuePair<ushort, string>>
{
    private ushort _current;
    public WorldCombo(ILogger log) : base(() => OnFrameworkService.WorldData.ToList(), log)
    {
        // Start with the Any World entry selected.
        Current             = Items.FirstOrDefault();
        CurrentSelectionIdx = 0;
    }

    protected override string ToString(KeyValuePair<ushort, string> obj)
        => obj.Value;

    /// <summary> Simple draw invoke. </summary>
    public bool Draw(ushort currentWorld, float width)
    {
        InnerWidth = width * 1.3f;
        _current = currentWorld;
        string previewName = Items.FirstOrDefault(x => x.Key == _current).Value ?? "Select World...";
        return Draw("##worldCombo", previewName, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }
}
