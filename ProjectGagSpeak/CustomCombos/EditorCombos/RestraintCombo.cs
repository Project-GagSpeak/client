using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;
using OtterGui.Extensions;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Editor;

public sealed class RestraintCombo : CkFilterComboCache<RestraintSet>, IMediatorSubscriber, IDisposable
{
    private readonly FavoritesManager _favorites;
    public Guid _currentRestraint { get; private set; }
    public RestraintCombo(ILogger log, GagspeakMediator mediator, FavoritesManager favorites, 
        Func<IReadOnlyList<RestraintSet>> generator) : base(generator, log)
    {
        _favorites = favorites;
        _currentRestraint = Guid.Empty;
        SearchByParts = true;

        Mediator = mediator;
        Mediator.Subscribe<ConfigRestraintSetChanged>(this, _ => RefreshCombo());
    }

    public GagspeakMediator Mediator { get; }

    void IDisposable.Dispose()
    {
        Mediator.Unsubscribe<ConfigRestraintSetChanged>(this);
        GC.SuppressFinalize(this);
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
    public bool Draw(string label, Guid current, float width, uint? customSearchBg = null)
        => Draw(label, current, width, CFlags.None, customSearchBg);

    public bool Draw(string label, Guid current, float width, CFlags flags, uint? customSearchBg = null)
    {
        InnerWidth = width * 1.25f;
        _currentRestraint = current;
        var preview = Items.FirstOrDefault(i => i.Identifier == current)?.Label ?? "Select Restraint...";
        return Draw(label, preview, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags, customSearchBg);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var restraint = Items[globalIdx];

        if (Icons.DrawFavoriteStar(_favorites, FavoriteIdContainer.Restraint, restraint.Identifier, false) && CurrentSelectionIdx == globalIdx)
        {
            CurrentSelectionIdx = -1;
            Current = default;
        }
        ImUtf8.SameLineInner();
        var ret = ImGui.Selectable(restraint.Label, selected);
        return ret;
    }
}
