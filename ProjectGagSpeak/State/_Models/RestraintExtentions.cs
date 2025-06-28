using FFXIVClientStructs;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;
using System.Diagnostics.CodeAnalysis;

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
        foreach (var glam in IterateLayerGlamours(set.Layers, seen, active))
            yield return glam;
        foreach (var glam in IterateBaseGlamours(set.RestraintSlots.Values, seen))
            yield return glam;
    }

    /// <summary> Core internal Iterator that collects the GlamourSlot items from the base slots. </summary>
    /// <returns> An enumerable of GlamourSlot items retrieves, and seen equipslots updated. </returns>
    private static IEnumerable<GlamourSlot> IterateBaseGlamours(IEnumerable<IRestraintSlot> slots, HashSet<EquipSlot> seen)
    {
        foreach (var item in slots)
        {
            if (!item.ApplyFlags.HasFlag(RestraintFlags.Glamour)
                || item.IsOverlayItem()
                || !seen.Add(item.EquipSlot))
                continue;

            // Store the item. (no need to worry about slots since we only have one of each here)
            if (item is RestraintSlotBasic b)
                yield return b.Glamour;
            else if (item is RestraintSlotAdvanced a && a.Ref != null)
                yield return a.Ref.Glamour;
        }
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> An enumerable of GlamourSlot items from the layers, and seen equipslots updated. </returns>
    private static IEnumerable<GlamourSlot> IterateLayerGlamours(List<IRestraintLayer> layers, HashSet<EquipSlot> seen, RestraintLayer active = RestraintLayer.All)
    {
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            // Ensure it is an active layer.
            var layerBit = (RestraintLayer)(1 << i);
            if (!active.HasAny(layerBit))
                continue;
            // Ensure it satisfies the conditions for a valid layer.
            if (layers[i] is not RestrictionLayer layer
                || !layer.ApplyFlags.HasFlag(RestraintFlags.Glamour)
                || !layer.IsValid()
                || layer.IsOverlayItem()
                || !seen.Add(layer.EquipSlot))
                continue;

            yield return layer.Ref.Glamour;
        }
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
        var seen = new HashSet<ModSettingsPreset>();
        foreach (var mod in IterateLayerMods(set.Layers, seen, active))
            yield return mod;
        foreach (var mod in IterateBaseMods(set, seen))
            yield return mod;
    }


    /// <summary> Core internal Iterator that collects the ModSettingPresets items from the base slots. </summary>
    /// <returns> An enumerable of ModSettingPresets items retrieves, and seen mods updated. </returns>
    private static IEnumerable<ModSettingsPreset> IterateBaseMods(RestraintSet set, HashSet<ModSettingsPreset> seen)
    {
        foreach (var slot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
        {
            if (!slot.IsValid())
                continue;

            var mod = slot.Ref.Mod;
            if (slot.ApplyFlags.HasFlag(RestraintFlags.Mod) && mod.HasData && seen.Add(mod))
                yield return mod;
        }
        // The base mods appended, if we have not yet already added them.
        foreach (var mod in set.RestraintMods)
            if (mod.HasData && seen.Add(mod))
                yield return mod;
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> An enumerable of ModSettingsPreset items from the layers, and seen mods updated. </returns>
    private static IEnumerable<ModSettingsPreset> IterateLayerMods(List<IRestraintLayer> layers, HashSet<ModSettingsPreset> seen, RestraintLayer active = RestraintLayer.All)
    {
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layerBit = (RestraintLayer)(1 << i);
            if (!active.HasAny(layerBit))
                continue;

            if (layers[i] is RestrictionLayer binder && binder.Ref is not null)
            {
                var mod = binder.Ref.Mod;
                if (binder.ApplyFlags.HasFlag(RestraintFlags.Mod) && mod.HasData && seen.Add(mod))
                    yield return mod;
            }
            else if (layers[i] is ModPresetLayer mpl)
            {
                if (mpl.Mod.HasData && seen.Add(mpl.Mod))
                    yield return mpl.Mod;
            }
        }
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
        foreach (var moodles in IterateLayerMoodles(set.Layers, seen, active))
            yield return moodles;
        foreach (var moodles in IterateBaseMoodles(set, seen))
            yield return moodles;
    }


    /// <summary> Core internal Iterator that collects the Moodles from the base slots. </summary>
    /// <returns> An enumerable of applied Moodles, and seen Moodles updated. </returns>
    private static IEnumerable<Moodle> IterateBaseMoodles(RestraintSet set, HashSet<Moodle> seen)
    {
        foreach (var slot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
        {
            if (!slot.IsValid())
                continue;

            if (slot.ApplyFlags.HasFlag(RestraintFlags.Moodle) && seen.Add(slot.Ref.Moodle))
                yield return slot.Ref.Moodle;
        }
        // The base Moodles appended, if we have not yet already added them.
        foreach (var moodle in set.RestraintMoodles)
            if (seen.Add(moodle))
                yield return moodle;
    }

    /// <summary> Iterate through all layers of a restraint set. </summary>
    /// <returns> All Moodles from the layers, and seen Moodles updated. </returns>
    private static IEnumerable<Moodle> IterateLayerMoodles(List<IRestraintLayer> layers, HashSet<Moodle> seen, RestraintLayer active = RestraintLayer.All)
    {
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layerBit = (RestraintLayer)(1 << i);
            if (!active.HasAny(layerBit))
                continue;

            // Ensure it satisfies the conditions for a valid layer.
            if (layers[i] is not RestrictionLayer layer
                || !layer.ApplyFlags.HasFlag(RestraintFlags.Moodle)
                || !layer.IsValid()
                || !seen.Add(layer.Ref.Moodle))
                continue;

            yield return layer.Ref.Moodle;
        }
    }
    #endregion Moodles

    /// <summary> Retrieves the priority Blindfold Overlay if one exists from active layers (Layer 5 → 1) followed by base slots. </summary>
    public static BlindfoldOverlay? GetPriorityBlindfold(this RestraintSet set)
    {
        for (var i = set.Layers.Count - 1; i >= 0; i--)
        {
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

    public static BlindfoldOverlay? GetBlindfoldAtLayer(this RestraintSet set, int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < set.Layers.Count)
        {
            if (set.Layers[layerIndex] is RestrictionLayer l && l.Ref is BlindfoldRestriction br && br.HasValidPath())
                return br.Properties;
        }
        else if (layerIndex == -1)
        {
            // Get the first blindfold found in base slots if -1.
            foreach (var advSlot in set.RestraintSlots.Values.OfType<RestraintSlotAdvanced>())
                if (advSlot.Ref is BlindfoldRestriction br && br.HasValidPath())
                    return br.Properties;
        }

        return null;
    }

    /// <summary> Retrieves the priority Hypno Overlay if one exists from active layers (Layer 5 → 1) followed by base slots. </summary>
    public static HypnoticOverlay? GetPriorityHypnoEffect(this RestraintSet set)
    {
        // return the first found effect in the layers.
        for (var i = set.Layers.Count - 1; i >= 0; i--)
        {
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

    public static HypnoticOverlay? GetHypnoEffectAtLayer(this RestraintSet set, int layerIdx)
    {
        // return the first found effect in the layers.
        for (var i = set.Layers.Count - 1; i >= 0; i--)
        {
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




