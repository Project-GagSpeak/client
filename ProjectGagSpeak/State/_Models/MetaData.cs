using CkCommons.Classes;
using Glamourer.Api.Enums;

namespace GagSpeak.State.Models;

public enum MetaIndex : byte
{
    Wetness     = 0,
    HatState    = 1,
    VisorState  = 2,
    WeaponState = 3,
}

public struct MetaDataStruct
{
    public TriStateBool Headgear { get; private set; }
    public TriStateBool Visor { get; private set; }
    public TriStateBool Weapon { get; private set; }

    public MetaDataStruct()
    {
        Headgear = TriStateBool.Null;
        Visor = TriStateBool.Null;
        Weapon = TriStateBool.Null;
    }

    public MetaDataStruct(TriStateBool headgear)
    {
        Headgear = headgear;
        Visor = TriStateBool.Null;
        Weapon = TriStateBool.Null;
    }
    
    public MetaDataStruct(TriStateBool headgear, TriStateBool visor)
    {
        Headgear = headgear;
        Visor = visor;
        Weapon = TriStateBool.Null;
    }

    public MetaDataStruct(TriStateBool headgear, TriStateBool visor, TriStateBool weapon)
    {
        Headgear = headgear;
        Visor = visor;
        Weapon = weapon;
    }

    public bool IsEmpty => !AnySet();


    public bool AnySet()
        => Headgear.HasValue || Visor.HasValue || Weapon.HasValue;

    public bool IsDifferent(MetaIndex index, TriStateBool newValue)
        => index switch
        {
            MetaIndex.HatState => !newValue.Equals(Headgear),
            MetaIndex.VisorState => !newValue.Equals(Visor),
            MetaIndex.WeaponState => !newValue.Equals(Weapon),
            _ => false,
        };

    public MetaDataStruct WithMeta(MetaIndex index, TriStateBool newValue)
    {
        return index switch
        {
            MetaIndex.HatState => new MetaDataStruct(newValue, Visor, Weapon),
            MetaIndex.VisorState => new MetaDataStruct(Headgear, newValue, Weapon),
            MetaIndex.WeaponState => new MetaDataStruct(Headgear, Visor, newValue),
            _ => this,
        };
    }

    public MetaDataStruct WithMetaIfDifferent(MetaIndex index, TriStateBool newValue)
    {
        if (!IsDifferent(index, newValue))
            return this;

        return index switch
        {
            MetaIndex.HatState => new MetaDataStruct(newValue, Visor, Weapon),
            MetaIndex.VisorState => new MetaDataStruct(Headgear, newValue, Weapon),
            MetaIndex.WeaponState => new MetaDataStruct(Headgear, Visor, newValue),
            _ => this,
        };
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

    public JObject ToJObject()
        => new JObject
        {
            ["Headgear"] = Headgear.ToString(),
            ["Visor"] = Visor.ToString(),
            ["Weapon"] = Weapon.ToString()
        };

    public static MetaDataStruct FromJObject(JToken? token)
    {
        if (token is null)
            return Empty;

        return new MetaDataStruct
        {
            Headgear = TriStateBool.FromJObject(token["Headgear"]),
            Visor = TriStateBool.FromJObject(token["Visor"]),
            Weapon = TriStateBool.FromJObject(token["Weapon"])
        };
    }
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
