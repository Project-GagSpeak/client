using Glamourer.Api.Enums;
using OtterGui.Classes;

namespace GagSpeak.PlayerState.Components;

public enum MetaIndex : byte
{
    Wetness     = 0,
    HatState    = 1,
    VisorState  = 2,
    WeaponState = 3,
}

public struct MetaDataStruct
{
    public OptionalBool Headgear { get; private set; }
    public OptionalBool Visor { get; private set; }
    public OptionalBool Weapon { get; private set; }

    public MetaDataStruct()
    {
        Headgear = OptionalBool.Null;
        Visor = OptionalBool.Null;
        Weapon = OptionalBool.Null;
    }

    public MetaDataStruct(OptionalBool headgear = default, OptionalBool visor = default, OptionalBool weapon = default)
    {
        Headgear = headgear == default ? OptionalBool.Null : headgear;
        Visor = visor == default ? OptionalBool.Null : visor;
        Weapon = weapon == default ? OptionalBool.Null : weapon;
    }

    public bool AnySet()
        => Headgear.HasValue || Visor.HasValue || Weapon.HasValue;

    public bool SetMeta(MetaIndex index, OptionalBool newValue)
    {
        switch (index)
        {
            case MetaIndex.HatState:
                if (!Headgear.Equals(newValue))
                    Headgear = newValue;
                return true;

            case MetaIndex.VisorState:
                if (!Visor.Equals(newValue))
                    Visor = newValue; 
                return true;

            case MetaIndex.WeaponState:
                if (!Weapon.Equals(newValue))
                    Weapon = newValue;
                return true;
        }
        return false;
    }

    public MetaFlag OnFlags()
    {
        MetaFlag flags = 0;
        if (Headgear.HasValue && Headgear.Value is true) flags |= MetaFlag.HatState;
        if (Visor.HasValue && Visor.Value is true) flags |= MetaFlag.VisorState;
        if (Weapon.HasValue && Weapon.Value is true) flags |= MetaFlag.WeaponState;
        return flags;
    }

    public MetaFlag OffFlags()
    {
        MetaFlag flags = 0;
        if (Headgear.HasValue && Headgear.Value is false) flags |= MetaFlag.HatState;
        if (Visor.HasValue && Visor.Value is false) flags |= MetaFlag.VisorState;
        if (Weapon.HasValue && Weapon.Value is false) flags |= MetaFlag.WeaponState;
        return flags;
    }

    public static MetaDataStruct Empty => new MetaDataStruct();
}


public static class MetaEx
{
    public static MetaIndex ToMetaIndex(this MetaFlag flag)
        => flag switch
        {
            MetaFlag.HatState => MetaIndex.HatState,
            MetaFlag.VisorState => MetaIndex.VisorState,
            MetaFlag.WeaponState => MetaIndex.WeaponState,
            _ => MetaIndex.Wetness,
        };

    public static MetaFlag ToFlag(this MetaIndex index)
    => index switch
    {
        MetaIndex.Wetness => MetaFlag.Wetness,
        MetaIndex.HatState => MetaFlag.HatState,
        MetaIndex.VisorState => MetaFlag.VisorState,
        MetaIndex.WeaponState => MetaFlag.WeaponState,
        _ => (MetaFlag)byte.MaxValue,
    };

    public static IEnumerable<MetaIndex> ToIndices(this MetaFlag index)
    {
        if (index.HasFlag(MetaFlag.Wetness))
            yield return MetaIndex.Wetness;
        if (index.HasFlag(MetaFlag.HatState))
            yield return MetaIndex.HatState;
        if (index.HasFlag(MetaFlag.VisorState))
            yield return MetaIndex.VisorState;
        if (index.HasFlag(MetaFlag.WeaponState))
            yield return MetaIndex.WeaponState;
    }

    public static MetaFlag FromIndices(this IEnumerable<MetaIndex> indices)
    {
        MetaFlag result = 0;
        foreach (var index in indices)
            result |= index.ToFlag();
        return result;
    }

    public static string ToName(this MetaFlag flag)
        => flag switch
        {
            MetaFlag.HatState => "Hat Visible",
            MetaFlag.VisorState => "Visor Toggled",
            MetaFlag.WeaponState => "Weapon Visible",
            _ => "Unknown Meta",
        };

    public static string ToTooltip(this MetaFlag flag)
        => flag switch
        {
            MetaFlag.HatState => "Hide or show the character's head gear.",
            MetaFlag.VisorState => "Toggle the visor state of the character's head gear.",
            MetaFlag.WeaponState => "Hide or show the character's weapons when not drawn.",
            _ => string.Empty,
        };
}
