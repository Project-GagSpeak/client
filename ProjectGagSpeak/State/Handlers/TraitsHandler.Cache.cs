using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using System.Collections.Immutable;

namespace GagSpeak.PlayerState.Visual;

public sealed partial class TraitsHandler
{
    /// <summary> The currently banned actions determined by <see cref="_finalTraits"/></summary>
    private ImmutableDictionary<uint, Traits> BannedActions = ImmutableDictionary<uint, Traits>.Empty;

    /// <summary> The stored traits for every active gag, restriction, or restraint set. </summary>
    private SortedList<CombinedCacheKey, Traits> _traits = new();

    private Traits _finalTraits = Traits.None;
    public bool BoundArms => _finalTraits.HasAny(Traits.BoundArms);
    public bool BoundLegs => _finalTraits.HasAny(Traits.BoundLegs);
    public bool Gagged => _finalTraits.HasAny(Traits.Gagged);
    public bool Blindfolded => _finalTraits.HasAny(Traits.Blindfolded);
    public bool Immobile => _finalTraits.HasAny(Traits.Immobile);
    public bool Weighty => _finalTraits.HasAny(Traits.Weighty);

    public void Addtraits(CombinedCacheKey combinedKey, Traits traits)
    { }

    public bool AddAndUpdatetraits(CombinedCacheKey combinedKey, Traits traits)
    { return false; }

    public void Removetraits(CombinedCacheKey combinedKey)
    { }

    public bool RemoveAndUpdatetraits(CombinedCacheKey combinedKey, out Traits removed)
    { removed = Traits.None; return false; }

    private bool UpdateFinalCache()
    { return false; }

    #region DebugHelper
    public void DrawCacheTable()
    { }
    #endregion Debug Helper
}
