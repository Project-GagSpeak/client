using FFXIVClientStructs;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;
using System.Diagnostics.CodeAnalysis;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace GagSpeak.State.Models;

public static class RestraintExtentions
{
    #region Glamour
    /// <summary> Determines if an item should be ignored. </summary>
    /// <returns> True if it satisfis the conditions, false otherwise. </returns>
    public static bool IsOverlayItem(this IRestraintSlot slot)
    {
        return slot.EquipItem.ItemId == ItemService.NothingItem(slot.EquipSlot).ItemId
            && slot.ApplyFlags.HasAny(RestraintFlags.IsOverlay);
    }

    /// <summary> Determines if an item should be ignored. <b>Assumes the Ref is not null.</b> </summary>
    /// <returns> True if it satisfis the conditions, false otherwise. </returns>
    public static bool IsOverlayItem(this IRestrictionRef slot)
    {
        return slot.Ref.Glamour.GameItem.ItemId == ItemService.NothingItem(slot.Ref.Glamour.Slot).ItemId
            && slot.ApplyFlags.HasAny(RestraintFlags.IsOverlay);
    }

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
            ? l.Ref.Glamour : null;
    }

    /// <summary> Core internal Iterator that collects the GlamourSlot items from the base slots. </summary>
    /// <returns> An enumerable of GlamourSlot items retrieves, and seen equipslots updated. </returns>
    private static IEnumerable<GlamourSlot> IterateBaseGlamours(IEnumerable<IRestraintSlot> slots, HashSet<EquipSlot> seen)
    {
        var applied = new List<GlamourSlot>();
        foreach (var item in slots)
        {
            if (!item.ApplyFlags.HasAny(RestraintFlags.Glamour)
                || item.IsOverlayItem()
                || !seen.Add(item.EquipSlot))
                continue;

            // Store the item. (no need to worry about slots since we only have one of each here)
            if (item is RestraintSlotBasic b)
                applied.Add(b.Glamour);
            else if (item is RestraintSlotAdvanced a && a.Ref != null)
                applied.Add(a.Ref.Glamour);
        }
        return applied;
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> An enumerable of GlamourSlot items from the layers, and seen equipslots updated. </returns>
    private static IEnumerable<GlamourSlot> IterateLayerGlamours(List<IRestraintLayer> layers, HashSet<EquipSlot> seen, RestraintLayer active = RestraintLayer.All)
    {
        var applied = new List<GlamourSlot>();
        foreach (int i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= layers.Count)
                continue;
            // Ensure it satisfies the conditions for a valid layer.
            if (layers[i] is not RestrictionLayer layer
                || !layer.ApplyFlags.HasAny(RestraintFlags.Glamour)
                || !layer.IsValid()
                || layer.IsOverlayItem()
                || !seen.Add(layer.EquipSlot))
                continue;

            applied.Add(layer.Ref.Glamour);
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

        return set.Layers[idx] is RestrictionLayer l && l.ApplyFlags.HasAny(RestraintFlags.Mod) && l.IsValid() && l.Ref.Mod.HasData
            ? l.Ref.Mod : null;
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
        foreach (int i in active.GetLayerIndices().OrderByDescending(i => i))
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

    #region Moodles
    public static IEnumerable<Moodle> GetBaseMoodles(this RestraintSet set)
    => IterateBaseMoodles(set, new());

    public static IEnumerable<Moodle> GetLayerMoodles(this RestraintSet set)
        => IterateLayerMoodles(set.Layers, new());

    public static IEnumerable<Moodle> GetActiveLayerMoodles(this RestraintSet set, RestraintLayer active)
        => IterateLayerMoodles(set.Layers, new(), active);

    public static IEnumerable<Moodle> GetAllMoodles(this RestraintSet set)
        => GetAllMoodles(set, RestraintLayer.All);

    public static IEnumerable<Moodle> GetAllMoodles(this RestraintSet set, RestraintLayer active)
    {
        var seen = new HashSet<Moodle>();
        var result = new List<Moodle>();
        result.AddRange(IterateLayerMoodles(set.Layers, seen, active));
        result.AddRange(IterateBaseMoodles(set, seen));
        return result;
    }

    public static Moodle? GetMoodleAtLayer(this RestraintSet set, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= set.Layers.Count)
            return null;

        return set.Layers[layerIndex] is RestrictionLayer l && l.ApplyFlags.HasAny(RestraintFlags.Moodle) && l.IsValid()
            ? l.Ref.Moodle : null;
    }

    /// <summary> Core internal Iterator that collects the Moodles from the base slots. </summary>
    /// <returns> An enumerable of applied Moodles, and seen Moodles updated. </returns>
    private static IEnumerable<Moodle> IterateBaseMoodles(RestraintSet set, HashSet<Moodle> seen)
    {
        var applied = new List<Moodle>();
        foreach (var slot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
        {
            if (!slot.IsValid())
                continue;

            if (slot.ApplyFlags.HasAny(RestraintFlags.Moodle) && seen.Add(slot.Ref.Moodle))
                applied.Add(slot.Ref.Moodle);
        }
        // The base Moodles appended, if we have not yet already added them.
        foreach (var moodle in set.RestraintMoodles)
            if (seen.Add(moodle))
                applied.Add(moodle);

        return applied;
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> All Moodles from the layers, and seen Moodles updated. </returns>
    private static IEnumerable<Moodle> IterateLayerMoodles(List<IRestraintLayer> layers, HashSet<Moodle> seen, RestraintLayer active = RestraintLayer.All)
    {
        var applied = new List<Moodle>();
        foreach (int i in active.GetLayerIndices().OrderByDescending(i => i))
        {
            if (i < 0 || i >= layers.Count)
                continue;

            // Ensure it satisfies the conditions for a valid layer.
            if (layers[i] is not RestrictionLayer l 
                || !l.ApplyFlags.HasAny(RestraintFlags.Moodle) 
                || !l.IsValid() 
                || !seen.Add(l.Ref.Moodle))
                continue;

            applied.Add(l.Ref.Moodle);
        }
        return applied;
    }
    #endregion Moodles
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
        Traits result = set.Traits;
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
        Traits result = Traits.None;

        foreach (int i in active.GetLayerIndices().OrderByDescending(i => i))
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
        foreach (int i in active.GetLayerIndices().OrderByDescending(i => i))
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
        foreach (int i in active.GetLayerIndices().OrderByDescending(i => i))
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
        foreach (int i in active.GetLayerIndices().OrderByDescending(i => i))
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

    // FIX LATER - Useful for Kinkster interaction.
    public static LightRestraintSet ToLightRestraint(this RestraintSet set)
    {
        var appliedSlots = new List<AppliedSlot>();
        foreach (var kv in set.RestraintSlots)
        {
            if (kv.Value.ApplyFlags.HasAny(RestraintFlags.Glamour) && kv.Value is RestraintSlotBasic basic)
                appliedSlots.Add(new AppliedSlot((byte)basic.EquipSlot, basic.Glamour.GameItem.Id.Id));
            else if (kv.Value is RestraintSlotAdvanced adv && adv.Ref != null)
                appliedSlots.Add(new AppliedSlot((byte)adv.EquipSlot, adv.EquipItem.ItemId.Id));
        }

        var attributes = new Attributes(RestraintFlags.Advanced, set.Traits, set.Arousal);
        return new LightRestraintSet(set.Identifier, set.Label, set.Description, appliedSlots, attributes);
    }
}




