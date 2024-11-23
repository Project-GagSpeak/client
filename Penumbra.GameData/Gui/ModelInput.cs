using Dalamud.Interface.Utility;
using ImGuiNET;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Gui;

public static class ModelInput
{
    /// <summary> Draw numerical input for a CharacterArmors set ID and variant, without the stain. </summary>
    /// <param name="armor"> The input/output armor. </param>
    /// <returns> Whether the armor was changed. </returns>
    public static bool DrawArmorInput(ref CharacterArmor armor)
        => DrawInputModelSet(ref armor.Set, ref armor.Variant);

    /// <summary> Draw numerical input for a CharacterWeapons skeleton ID, weapon ID and variant, without the stain. </summary>
    /// <param name="weapon"> The input/output weapon. </param>
    /// <returns> Whether the weapon was changed. </returns>
    public static bool DrawWeaponInput(ref CharacterWeapon weapon)
        => DrawInputModelSet(ref weapon.Skeleton, ref weapon.Weapon, ref weapon.Variant);

    /// <inheritdoc cref="DrawInputModelSetIntern"/>
    public static bool DrawInputModelSet(ref PrimaryId primaryId, ref Variant variant)
    {
        SecondaryId sec = 0;
        return DrawInputModelSetIntern(false, ref primaryId, ref sec, ref variant);
    }

    /// <inheritdoc cref="DrawInputModelSetIntern"/>
    public static bool DrawInputModelSet(ref PrimaryId primaryId, ref SecondaryId secondaryId, ref Variant variant)
        => DrawInputModelSetIntern(true, ref primaryId, ref secondaryId, ref variant);

    /// <summary> Draw input for a set of primary ID, optional secondary ID and variant. </summary>
    /// <param name="withWeapon"> Whether to draw the secondary ID input. </param>
    /// <param name="primaryId"> Primary ID input/output. </param>
    /// <param name="secondaryId"> Secondary ID input/output. </param>
    /// <param name="variant"> Variant input/output. </param>
    private static bool DrawInputModelSetIntern(bool withWeapon, ref PrimaryId primaryId, ref SecondaryId secondaryId, ref Variant variant)
    {
        var ret      = false;
        var intValue = (int)primaryId.Id;
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("##SetId", ref intValue, 0, 0))
        {
            var value = (PrimaryId)(ushort)Math.Clamp(intValue, 0, ushort.MaxValue);
            ret       = primaryId.Id != intValue;
            primaryId = value;
        }

        if (withWeapon)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            intValue = secondaryId.Id;
            if (ImGui.InputInt("##TypeId", ref intValue, 0, 0))
            {
                var value = (SecondaryId)(ushort)Math.Clamp(intValue, 0, ushort.MaxValue);
                ret         = secondaryId.Id != intValue;
                secondaryId = value;
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
        intValue = variant.Id;
        if (ImGui.InputInt("##Variant", ref intValue, 0, 0))
        {
            var value = (Variant)(ushort)Math.Clamp(intValue, 0, byte.MaxValue);
            ret     = variant.Id != intValue;
            variant = value;
        }

        return ret;
    }
}
