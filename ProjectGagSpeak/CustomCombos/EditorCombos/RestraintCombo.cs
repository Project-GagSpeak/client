using Dalamud.Interface.Colors;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class RestraintCombo : CkFilterComboCache<RestraintSet>
{
    private readonly FavoritesManager _favorites;
    public Guid _currentRestraint { get; private set; }
    public RestraintCombo(ILogger log, FavoritesManager favorites, Func<IReadOnlyList<RestraintSet>> restraintsGenerator)
        : base(restraintsGenerator, log)
    {
        _favorites = favorites;
        _currentRestraint = Guid.Empty;
        SearchByParts = true;
    }

    protected override string ToString(RestraintSet obj)
        => obj.Label;

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current?.Identifier == _currentRestraint)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.Identifier == _currentRestraint);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null;
        return CurrentSelectionIdx;
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, Guid current, float width)
        => Draw(label, current, width, ImGuiComboFlags.None);

    public bool Draw(string label, Guid current, float width, ImGuiComboFlags flags)
    {
        InnerWidth = width * 1.25f;
        _currentRestraint = current;
        var preview = Items.FirstOrDefault(i => i.Identifier == current)?.Label ?? "Select Restraint...";
        return Draw(label, preview, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restraint = Items[globalIdx];

        if (Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, restraint.Identifier) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            Current = default;
        }
        ImUtf8.SameLineInner();
        var ret = ImGui.Selectable(restraint.Label, selected);
        return ret;
    }
}
