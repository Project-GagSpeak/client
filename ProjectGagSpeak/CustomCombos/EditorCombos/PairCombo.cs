using CkCommons.Widgets;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using Dalamud.Bindings.ImGui;
using OtterGui.Classes;
using OtterGui.Extensions;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class PairCombo : CkFilterComboCache<Kinkster>
{
    private readonly KinksterManager _pairs;
    private readonly FavoritesManager _favorites;
    public PairCombo(ILogger log, KinksterManager pairs, FavoritesManager favorites, Func<IReadOnlyList<Kinkster>> generator)
        : base(generator, log)
    {
        _pairs = pairs;
        _favorites = favorites;
        SearchByParts = true;
    }

    public void RefreshPairList()
    {
        var oldSelected = Current?.UserData.UID ?? string.Empty;
        Cleanup();
        if(!string.IsNullOrEmpty(oldSelected))
        {
            CurrentSelectionIdx = Items.IndexOf(i => i.UserData.UID == oldSelected);
            Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null;
        }
        else
        {
            CurrentSelectionIdx = -1;
            Current = null;
        }
    }

    public void ClearSelected() => UpdateSelection(null);

    protected override bool IsVisible(int globalIndex, LowerString filter)
    {
        return Items[globalIndex].UserData.AliasOrUID.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || (Items[globalIndex].GetNickname()?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (Items[globalIndex].PlayerName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    protected override string ToString(Kinkster obj)
        => obj.GetNickAliasOrUid();

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(float width, float innerScalar = 1.25f, uint? searchBg = null)
        => Draw(width, innerScalar, CFlags.None, searchBg);

    public bool Draw(float width, float innerScalar, CFlags flags, uint? searchBg = null)
    {
        InnerWidth = width * innerScalar;
        var preview = Current?.GetNickAliasOrUid() ?? "Select Pair...";
        return Draw("##PairCombo", preview, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags, searchBg);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var kinkster = Items[globalIdx];

        if(Icons.DrawFavoriteStar(_favorites, kinkster.UserData.UID) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            Current = default;
        }

        ImUtf8.SameLineInner();
        var ret = ImGui.Selectable(ToString(kinkster), selected);
        return ret;
    }

}
