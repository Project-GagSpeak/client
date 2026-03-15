using GagSpeak.PlayerClient;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;

namespace GagSpeak.State.Models;

public static class RestraintExtentions
{
    #region Glamour
    /// <summary>
    ///     Determines if an item should be ignored.
    /// </summary>
    /// <returns>
    ///     True when there is a "nothing" item _and_ it's set to be an overlay which means the slot should be ignored.
    /// </returns>
    public static bool IsOverlayItem(this IRestraintSlot s)
        => s.EquipItem.ItemId == ItemSvc.NothingItem(s.EquipSlot).ItemId && s.ApplyFlags.HasAny(RestraintFlags.IsOverlay);

    /// <summary> 
    ///     Determines if an item should be ignored. <br/>
    ///     <b>Assumes the Ref is not null.</b>
    /// </summary>
    /// <returns> 
    ///     True when there is a "nothing" item _and_ it's set to be an overlay which means the slot should be ignored.
    /// </returns>
    public static bool IsOverlayItem(this IRestrictionRef s)
        => s.Ref.Glamour.GameItem.ItemId == ItemSvc.NothingItem(s.Ref.Glamour.Slot).ItemId && s.ApplyFlags.HasAny(RestraintFlags.IsOverlay);

    /// <summary> Retrieves ONLY the base GlamourSlots. </summary>
    public static IEnumerable<GlamourSlot> GetBaseGlamours(this RestraintSet set)
        => IterateBaseGlamours(set.RestraintSlots.Values, new());

    /// <summary> Retrieves ONLY the layer Glamours. </summary>
    /// <remarks> Does not care if they are active or not, just what is stored. </remarks>
    public static IEnumerable<GlamourSlot> GetLayerGlamours(this RestraintSet set)
        => IterateLayerGlamours(set.Layers, new());

    /// <summary> Retrieves ONLY the layer glamours, and only for the active layers. </summary>
    public static IEnumerable<GlamourSlot> GetActiveLayerGlamours(this RestraintSet set, RestraintLayer active)
        => IterateLayerGlamours(set.Layers, new(), active);

    /// <summary> Retrieves all glamours from active layers (Layer 5 → 1) followed by base slots. </summary>
    /// <remarks> Does not care if the layer is active or not, this is a collective fetch. </remarks>
    public static IEnumerable<GlamourSlot> GetAllGlamours(this RestraintSet set)
        => GetAllGlamours(set, RestraintLayer.All);

    /// <summary> Retrieves all glamours from active layers (Layer 5 → 1) followed by base slots. </summary>
    public static IEnumerable<GlamourSlot> GetAllGlamours(this RestraintSet set, RestraintLayer active)
    {
        var seen = new HashSet<EquipSlot>();
        var result = new List<GlamourSlot>();
        result.AddRange(IterateLayerGlamours(set.Layers, seen, active));
        result.AddRange(IterateBaseGlamours(set.RestraintSlots.Values, seen));
        return result;
    }

    public static GlamourSlot? GetGlamourAtLayer(this RestraintSet set, int idx)
    {
        if (idx < 0 || idx >= set.Layers.Count)
            return null;

        return set.Layers[idx] is RestrictionLayer l && l.ApplyFlags.HasAny(RestraintFlags.Glamour) && l.IsValid() && !l.IsOverlayItem()
            ? new GlamourSlot(l.EquipSlot, l.EquipItem, l.Stains) : null;
    }

    /// <summary> Core internal Iterator that collects the GlamourSlot items from the base slots. </summary>
    /// <returns> An enumerable of GlamourSlot items retrieves, and seen equipslots updated. </returns>
    private static IEnumerable<GlamourSlot> IterateBaseGlamours(IEnumerable<IRestraintSlot> slots, HashSet<EquipSlot> seen)
    {
        var applied = new List<GlamourSlot>();
        foreach (var item in slots)
        {
            if (!item.ApplyFlags.HasAny(RestraintFlags.Glamour))
                continue;

            // If we already saw this slot, skip over it.
            if (!seen.Add(item.EquipSlot))
                continue;

            // If the item is 'nothing', and set as an overlay, ignore it.
            if (item.IsOverlayItem())
                continue;

            // Store the item. (no need to worry about slots since we only have one of each here)
            if (item is RestraintSlotBasic b)
                applied.Add(b.Glamour);
            else if (item is RestraintSlotAdvanced a && a.IsValid() && a.ApplyFlags.HasAny(RestraintFlags.Glamour))
                applied.Add(new GlamourSlot(a.EquipSlot, a.EquipItem, a.Stains));
        }
        return applied;
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> An enumerable of GlamourSlot items from the layers, and seen equipslots updated. </returns>
    private static IEnumerable<GlamourSlot> IterateLayerGlamours(List<IRestraintLayer> layers, HashSet<EquipSlot> seen, RestraintLayer active = RestraintLayer.All)
    {
        var applied = new List<GlamourSlot>();
        foreach (var i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= layers.Count)
                continue;
            // Ensure it satisfies the conditions for a valid layer.
            if (layers[i] is not RestrictionLayer layer)
                continue;

            // If not a glamour layer, or valid, skip.
            if (!layer.ApplyFlags.HasAny(RestraintFlags.Glamour) || !layer.IsValid())
                continue;

            // If we already saw this slot, skip over it.
            if (!seen.Add(layer.EquipSlot))
                continue;

            // If the item is 'nothing', and set as an overlay, ignore it.
            if (layer.IsOverlayItem())
                continue;

            applied.Add(new GlamourSlot(layer.EquipSlot, layer.EquipItem, layer.Stains));
        }
        return applied;
    }
    #endregion Glamour

    #region Mods
    public static IEnumerable<ModSettingsPreset> GetBaseMods(this RestraintSet set)
        => IterateBaseMods(set, new());

    public static IEnumerable<ModSettingsPreset> GetLayerMods(this RestraintSet set)
        => IterateLayerMods(set.Layers, new());

    public static IEnumerable<ModSettingsPreset> GetActiveLayerMods(this RestraintSet set, RestraintLayer active)
        => IterateLayerMods(set.Layers, new(), active);

    public static IEnumerable<ModSettingsPreset> GetAllMods(this RestraintSet set)
        => GetAllMods(set, RestraintLayer.All);

    public static IEnumerable<ModSettingsPreset> GetAllMods(this RestraintSet set, RestraintLayer active)
    {
        var result = new List<ModSettingsPreset>();
        var seen = new HashSet<ModSettingsPreset>();
        result.AddRange(IterateLayerMods(set.Layers, seen, active));
        result.AddRange(IterateBaseMods(set, seen));
        return result;
    }

    public static ModSettingsPreset? GetModAtLayer(this RestraintSet set, int idx)
    {
        if (idx < 0 || idx >= set.Layers.Count)
            return null;

        if(set.Layers[idx] is RestrictionLayer l && l.ApplyFlags.HasAny(RestraintFlags.Mod) && l.IsValid() && l.Ref.Mod.HasData)
            return l.Ref.Mod;
        else if (set.Layers[idx] is ModPresetLayer mpl && mpl.Mod.HasData)
            return mpl.Mod;
        return null;
    }


    /// <summary> Core internal Iterator that collects the ModSettingPresets items from the base slots. </summary>
    /// <returns> An enumerable of ModSettingPresets items retrieves, and seen mods updated. </returns>
    private static IEnumerable<ModSettingsPreset> IterateBaseMods(RestraintSet set, HashSet<ModSettingsPreset> seen)
    {
        var applied = new List<ModSettingsPreset>();
        foreach (var slot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
        {
            if (!slot.IsValid())
                continue;

            var mod = slot.Ref.Mod;
            if (slot.ApplyFlags.HasAny(RestraintFlags.Mod) && mod.HasData && seen.Add(mod))
                applied.Add(mod);
        }
        // The base mods appended, if we have not yet already added them.
        foreach (var mod in set.RestraintMods)
            if (mod.HasData && seen.Add(mod))
                applied.Add(mod);

        return applied;
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> An enumerable of ModSettingsPreset items from the layers, and seen mods updated. </returns>
    private static IEnumerable<ModSettingsPreset> IterateLayerMods(List<IRestraintLayer> layers, HashSet<ModSettingsPreset> seen, RestraintLayer active = RestraintLayer.All)
    {
        var applied = new List<ModSettingsPreset>();
        foreach (var i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= layers.Count)
                continue;

            if (layers[i] is RestrictionLayer binder && binder.Ref is not null)
            {
                var mod = binder.Ref.Mod;
                if (binder.ApplyFlags.HasAny(RestraintFlags.Mod) && mod.HasData && seen.Add(mod))
                    applied.Add(mod);
            }
            else if (layers[i] is ModPresetLayer mpl)
            {
                if (mpl.Mod.HasData && seen.Add(mpl.Mod))
                    applied.Add(mpl.Mod);
            }
        }
        return applied;
    }
    #endregion Mods

    #region LociData
    public static IEnumerable<LociItem> GetBaseLociData(this RestraintSet set)
        => IterateBaseLociData(set, new());

    public static IEnumerable<LociItem> GetLayerLociData(this RestraintSet set)
        => IterateLayerLociData(set.Layers, new());

    public static IEnumerable<LociItem> GetActiveLayerLociData(this RestraintSet set, RestraintLayer active)
        => IterateLayerLociData(set.Layers, new(), active);

    public static IEnumerable<LociItem> GetAllLociData(this RestraintSet set)
        => GetAllLociData(set, RestraintLayer.All);

    public static IEnumerable<LociItem> GetAllLociData(this RestraintSet set, RestraintLayer active)
    {
        var seen = new HashSet<LociItem>();
        var result = new List<LociItem>();
        result.AddRange(IterateLayerLociData(set.Layers, seen, active));
        result.AddRange(IterateBaseLociData(set, seen));
        return result;
    }

    public static LociItem? GetLociDataAtLayer(this RestraintSet set, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= set.Layers.Count)
            return null;

        return set.Layers[layerIndex] is RestrictionLayer l && l.ApplyFlags.HasAny(RestraintFlags.Loci) && l.IsValid()
            ? l.Ref.LociData : null;
    }

    /// <summary> Core internal Iterator that collects the LociItems from the base slots. </summary>
    /// <returns> An enumerable of applied LociItems, and seen LociItems updated. </returns>
    private static IEnumerable<LociItem> IterateBaseLociData(RestraintSet set, HashSet<LociItem> seen)
    {
        var applied = new List<LociItem>();
        foreach (var slot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
        {
            if (!slot.IsValid())
                continue;

            if (slot.ApplyFlags.HasAny(RestraintFlags.Loci) && seen.Add(slot.Ref.LociData))
                applied.Add(slot.Ref.LociData);
        }
        // The base LociData appended, if we have not yet already added them.
        foreach (var lociItem in set.RestraintLociData)
            if (seen.Add(lociItem))
                applied.Add(lociItem);

        return applied;
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> All LociItems from the passed in layers, and seen LociItems updated. </returns>
    private static IEnumerable<LociItem> IterateLayerLociData(List<IRestraintLayer> layers, HashSet<LociItem> seen, RestraintLayer active = RestraintLayer.All)
    {
        var applied = new List<LociItem>();
        foreach (var i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= layers.Count)
                continue;

            // Ensure it satisfies the conditions for a valid layer.
            if (layers[i] is not RestrictionLayer l 
                || !l.ApplyFlags.HasAny(RestraintFlags.Loci) 
                || !l.IsValid() 
                || !seen.Add(l.Ref.LociData))
                continue;

            applied.Add(l.Ref.LociData);
        }
        return applied;
    }
    #endregion LociData

    #region Traits
    public static Traits GetBaseTraits(this RestraintSet set)
    => CollectBaseTraits(set);

    public static Traits GetLayerTraits(this RestraintSet set)
        => CollectLayerTraits(set.Layers);

    public static Traits GetActiveLayerTraits(this RestraintSet set, RestraintLayer active)
        => CollectLayerTraits(set.Layers, active);

    public static Traits GetAllTraits(this RestraintSet set)
        => GetAllTraits(set, RestraintLayer.All);

    public static Traits GetAllTraits(this RestraintSet set, RestraintLayer active)
        => CollectLayerTraits(set.Layers, active) | CollectBaseTraits(set);

    public static Traits GetTraitsForLayer(this RestraintSet set, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= set.Layers.Count)
            return Traits.None;
        return set.Layers[layerIndex] is RestrictionLayer l && l.IsValid() && l.ApplyFlags.HasAny(RestraintFlags.Trait)
            ? l.Ref.Traits : Traits.None;
    }

    /// <summary> Collect all traits from the base slots and base set flags. </summary>
    private static Traits CollectBaseTraits(RestraintSet set)
    {
        var result = set.Traits;
        foreach (var slot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
        {
            if (!slot.IsValid() || !slot.ApplyFlags.HasAny(RestraintFlags.Trait))
                continue;

            result |= slot.Ref.Traits;
        }

        return result;
    }

    /// <summary> Collect all traits from layers. </summary>
    private static Traits CollectLayerTraits(List<IRestraintLayer> layers, RestraintLayer active = RestraintLayer.All)
    {
        var result = Traits.None;

        foreach (var i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= layers.Count)
                continue;

            if (layers[i] is not RestrictionLayer l || !l.IsValid() || !l.ApplyFlags.HasAny(RestraintFlags.Trait))
                continue;

            result |= l.Ref.Traits;
        }

        return result;
    }
    #endregion Traits

    #region Arousal
    public static IEnumerable<Arousal> GetBaseArousals(this RestraintSet set)
        => IterateBaseArousal(set);

    public static IEnumerable<Arousal> GetLayerArousals(this RestraintSet set)
        => IterateLayerArousal(set.Layers);

    public static IEnumerable<Arousal> GetActiveLayerArousals(this RestraintSet set, RestraintLayer active)
        => IterateLayerArousal(set.Layers, active);

    public static IEnumerable<Arousal> GetAllArousals(this RestraintSet set)
        => set.GetAllArousals(RestraintLayer.All);

    public static IEnumerable<Arousal> GetAllArousals(this RestraintSet set, RestraintLayer active)
    {
        var arousals = new List<Arousal>();
        arousals.AddRange(IterateLayerArousal(set.Layers, active));
        arousals.AddRange(IterateBaseArousal(set));
        return arousals;
    }

    public static Arousal GetArousalForLayer(this RestraintSet set, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= set.Layers.Count)
            return Arousal.None;

        return set.Layers[layerIndex].Arousal;
    }

    /// <summary> Yield Arousal flags from the base slots and set flags. </summary>
    private static IEnumerable<Arousal> IterateBaseArousal(RestraintSet set)
    {
        var arousals = new List<Arousal>();
        foreach (var slot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
        {
            if (!slot.IsValid() || !slot.ApplyFlags.HasAny(RestraintFlags.Arousal))
                continue;

            if (slot.Ref.Arousal != Arousal.None)
                arousals.Add(slot.Ref.Arousal);
        }

        if (set.Arousal != Arousal.None)
            arousals.Add(set.Arousal);

        return arousals;
    }

    /// <summary> Yield Arousal flags from valid layers. </summary>
    private static IEnumerable<Arousal> IterateLayerArousal(List<IRestraintLayer> layers, RestraintLayer active = RestraintLayer.All)
    {
        var arousals = new List<Arousal>();
        foreach (var i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= layers.Count)
                continue;

            if (layers[i] is not RestrictionLayer l || !l.IsValid() || !l.ApplyFlags.HasAny(RestraintFlags.Arousal))
                continue;

            if (l.Ref.Arousal != Arousal.None)
                arousals.Add(l.Ref.Arousal);
        }
        return arousals;
    }
    #endregion Arousal


    /// <summary> Retrieves the priority Blindfold Overlay if one exists from active layers (Layer 5 → 1) followed by base slots. </summary>
    public static BlindfoldOverlay? GetPriorityBlindfold(this RestraintSet set, RestraintLayer active)
    {
        foreach (var i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= set.Layers.Count)
                continue;

            if (set.Layers[i] is not RestrictionLayer layer
                || layer.Ref is not BlindfoldRestriction br
                || string.IsNullOrEmpty(br.Properties.OverlayPath))
                continue;
            // If Blindfold conditions are met, return the overlay.
            return br.Properties;
        }
        // Next try to fetch them from the RestraintSlots.
        foreach (var advSlot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
            if (advSlot.Ref is BlindfoldRestriction br && br.HasValidPath())
                return br.Properties;

        // If no Blindfold is found, return null.
        return null;
    }

    // Only scan the base slots for a blindfold overlay.
    public static BlindfoldOverlay? GetBaseBlindfold(this RestraintSet set)
    {
        // Next try to fetch them from the RestraintSlots.
        foreach (var advSlot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
            if (advSlot.Ref is BlindfoldRestriction br && br.HasValidPath())
                return br.Properties;
        // If no Blindfold is found, return null.
        return null;
    }

    public static BlindfoldOverlay? GetBlindfoldAtLayer(this RestraintSet set, int layerIndex)
    {
        // if the layer is not in bounds, return null.
        if (set.Layers == null || layerIndex < 0 || layerIndex >= set.Layers.Count)
            return null;
        // If the layer is in bounds, check if it is a RestrictionLayer and has a valid BlindfoldRestriction.
        return set.Layers[layerIndex] is RestrictionLayer layer && layer.Ref is BlindfoldRestriction br && br.HasValidPath()
            ? br.Properties
            : null;
    }

    /// <summary> Retrieves the priority Hypno Overlay if one exists from active layers (Layer 5 → 1) followed by base slots. </summary>
    public static HypnoticOverlay? GetPriorityHypnoEffect(this RestraintSet set, RestraintLayer active)
    {
        // return the first found effect in the layers.
        foreach (var i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= set.Layers.Count)
                continue;

            if (set.Layers[i] is RestrictionLayer l && l.Ref is HypnoticRestriction hr && hr.HasValidPath())
                return hr.Properties;
        }
        // Next try to fetch them from the RestraintSlots.
        foreach (var advSlot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
            if (advSlot.Ref is HypnoticRestriction hr && hr.HasValidPath())
                return hr.Properties;

        // If no Hypnotic Effect is found, return null.
        return null;
    }

    // Only scan the base slots for a HypnoEffect overlay.
    public static HypnoticOverlay? GetBaseHypnoEffect(this RestraintSet set)
    {
        // Next try to fetch them from the RestraintSlots.
        foreach (var advSlot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
            if (advSlot.Ref is HypnoticRestriction hr && hr.HasValidPath())
                return hr.Properties;
        // If no HypnoEffect is found, return null.
        return null;
    }

    public static HypnoticOverlay? GetHypnoEffectAtLayer(this RestraintSet set, int layerIdx)
    {
        // if the layer is not in bounds, return null.
        if (layerIdx < 0 || layerIdx >= set.Layers.Count)
            return null;

        return set.Layers[layerIdx] is RestrictionLayer l && l.Ref is HypnoticRestriction hr && hr.HasValidPath()
            ? hr.Properties
            : null;
    }
}




