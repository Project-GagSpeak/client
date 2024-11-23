using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;

namespace Penumbra.GameData.Structs;

[JsonConverter(typeof(Converter))]
public readonly record struct ModelCharaId(uint Id)
{
    public static implicit operator ModelCharaId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<ModelCharaId>
    {
        public override void WriteJson(JsonWriter writer, ModelCharaId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override ModelCharaId ReadJson(JsonReader reader, Type objectType, ModelCharaId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct BNpcId(uint Id)
{
    public static implicit operator BNpcId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<BNpcId>
    {
        public override void WriteJson(JsonWriter writer, BNpcId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override BNpcId ReadJson(JsonReader reader, Type objectType, BNpcId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct ENpcId(uint Id)
{
    public static implicit operator ENpcId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<ENpcId>
    {
        public override void WriteJson(JsonWriter writer, ENpcId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override ENpcId ReadJson(JsonReader reader, Type objectType, ENpcId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct BNpcNameId(uint Id)
{
    public static implicit operator BNpcNameId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<BNpcNameId>
    {
        public override void WriteJson(JsonWriter writer, BNpcNameId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override BNpcNameId ReadJson(JsonReader reader, Type objectType, BNpcNameId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct MountId(uint Id)
{
    public static implicit operator MountId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<MountId>
    {
        public override void WriteJson(JsonWriter writer, MountId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override MountId ReadJson(JsonReader reader, Type objectType, MountId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct CompanionId(uint Id)
{
    public static implicit operator CompanionId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<CompanionId>
    {
        public override void WriteJson(JsonWriter writer, CompanionId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override CompanionId ReadJson(JsonReader reader, Type objectType, CompanionId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct OrnamentId(uint Id)
{
    public static implicit operator OrnamentId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<OrnamentId>
    {
        public override void WriteJson(JsonWriter writer, OrnamentId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override OrnamentId ReadJson(JsonReader reader, Type objectType, OrnamentId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct NpcId(uint Id)
{
    public static implicit operator NpcId(uint id)
        => new(id);

    public static implicit operator NpcId(ENpcId id)
        => new(id.Id);

    public static implicit operator NpcId(BNpcNameId id)
        => new(id.Id);

    public static implicit operator NpcId(MountId id)
        => new(id.Id);

    public static implicit operator NpcId(CompanionId id)
        => new(id.Id);

    public static implicit operator NpcId(OrnamentId id)
        => new(id.Id);

    public ENpcId ENpcId
        => new(Id);

    public BNpcNameId BNpcNameId
        => new(Id);

    public MountId MountId
        => new(Id);

    public CompanionId CompanionId
        => new(Id);

    public OrnamentId OrnamentId
        => new(Id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<NpcId>
    {
        public override void WriteJson(JsonWriter writer, NpcId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override NpcId ReadJson(JsonReader reader, Type objectType, NpcId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct SecondaryId(ushort Id)
{
    public static implicit operator SecondaryId(ushort id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<SecondaryId>
    {
        public override void WriteJson(JsonWriter writer, SecondaryId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override SecondaryId ReadJson(JsonReader reader, Type objectType, SecondaryId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<ushort>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct PrimaryId(ushort Id) : IComparisonOperators<PrimaryId, PrimaryId, bool>
{
    public static implicit operator PrimaryId(ushort id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    public static bool operator >(PrimaryId left, PrimaryId right)
        => left.Id > right.Id;

    public static bool operator >=(PrimaryId left, PrimaryId right)
        => left.Id >= right.Id;

    public static bool operator <(PrimaryId left, PrimaryId right)
        => left.Id < right.Id;

    public static bool operator <=(PrimaryId left, PrimaryId right)
        => left.Id <= right.Id;

    private class Converter : JsonConverter<PrimaryId>
    {
        public override void WriteJson(JsonWriter writer, PrimaryId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override PrimaryId ReadJson(JsonReader reader, Type objectType, PrimaryId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<ushort>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct Variant(byte Id)
{
    public static readonly Variant None = new(0);

    public static implicit operator Variant(byte id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<Variant>
    {
        public override void WriteJson(JsonWriter writer, Variant value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override Variant ReadJson(JsonReader reader, Type objectType, Variant existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<byte>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct StainId(byte Id)
{
    public const int NumStains = 2;

    public static implicit operator StainId(byte id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<StainId>
    {
        public override void WriteJson(JsonWriter writer, StainId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override StainId ReadJson(JsonReader reader, Type objectType, StainId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<byte>(reader);
    }
}

public readonly record struct StainIds(StainId Stain1, StainId Stain2) : IReadOnlyList<StainId>
{
    public static readonly StainIds None = new();

    public StainIds(StainId stain1)
        : this(stain1, 0)
    { }

    public StainIds ChangeSecond(StainId stain2)
        => this with { Stain2 = stain2 };

    public static StainIds Second(StainId stain2)
        => new(0, stain2);

    public static implicit operator StainIds(StainId stain1)
        => new(stain1);

    public StainIds With(int idx, StainId stain)
    {
        return idx switch
        {
            0 => this with { Stain1 = stain },
            1 => this with { Stain2 = stain },
            _ => this,
        };
    }

    public IEnumerator<StainId> GetEnumerator()
    {
        yield return Stain1;
        yield return Stain2;
    }

    public override string ToString()
        => $"{Stain1.Id},{Stain2.Id}";

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static StainIds FromUShort(ushort value)
        => new((StainId)(value & 0xFF), (StainId)(value >> 8));

    public int Count
        => 2;

    public StainId this[int index]
        => index == 0 ? Stain1 : Stain2;

    public JObject AddToObject(JObject obj)
    {
        obj["Stain"] = Stain1.Id;
        var i = 2;
        foreach (var stain in this.Skip(1))
            obj[$"Stain{i++}"] = stain.Id;
        return obj;
    }

    public static StainIds ParseFromObject(JObject? obj)
    {
        if (obj == null)
            return None;

        var stain  = (StainId)(obj["Stain"]?.ToObject<byte>() ?? 0);
        var stain2 = (StainId)(obj["Stain2"]?.ToObject<byte>() ?? 0);
        return new StainIds(stain, stain2);
    }

    public static StainIds All(StainId stain)
        => new(stain, stain);

    public StainIds(IReadOnlyList<StainId> stains)
        : this(stains.Count > 0 ? stains[0] : 0, stains.Count > 1 ? stains[1] : 0)
    { }

    public StainIds(IReadOnlyList<byte> stains)
        : this(stains.Count > 0 ? (StainId)stains[0] : 0, stains.Count > 1 ? (StainId)stains[1] : 0)
    { }

    public StainIds(ReadOnlySpan<byte> stains)
        : this(stains.Length > 0 ? (StainId)stains[0] : 0, stains.Length > 1 ? (StainId)stains[1] : 0)
    { }

    public static StainIds FromGearsetItem(in RaptureGearsetModule.GearsetItem item)
        => new(item.Stain0Id, item.Stain1Id);

    public static StainIds FromGlamourPlate(in MirageManager.GlamourPlate plate, int idx)
        => new(plate.Stain0Ids[idx], plate.Stain1Ids[idx]);

    public static StainIds FromWeapon(in Weapon weapon)
        => new(weapon.Stain0, weapon.Stain1);
}

[JsonConverter(typeof(Converter))]
public readonly record struct BonusItemId(ushort Id)
{
    public static readonly BonusItemId Invalid = new(ushort.MaxValue);

    public static implicit operator BonusItemId(ushort id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<BonusItemId>
    {
        public override void WriteJson(JsonWriter writer, BonusItemId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override BonusItemId ReadJson(JsonReader reader, Type objectType, BonusItemId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<ushort>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct ItemId(uint Id) : IComparisonOperators<ItemId, ItemId, bool>
{
    public static implicit operator ItemId(uint id)
        => new(id);

    public ItemId StripModifiers
        => new(Id switch
        {
            > 1000000 => Id - 1000000,
            > 500000  => Id - 500000,
            _         => Id,
        });

    public override string ToString()
        => Id.ToString();

    public static bool operator >(ItemId left, ItemId right)
        => left.Id > right.Id;

    public static bool operator >=(ItemId left, ItemId right)
        => left.Id >= right.Id;

    public static bool operator <(ItemId left, ItemId right)
        => left.Id < right.Id;

    public static bool operator <=(ItemId left, ItemId right)
        => left.Id <= right.Id;

    private class Converter : JsonConverter<ItemId>
    {
        public override void WriteJson(JsonWriter writer, ItemId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override ItemId ReadJson(JsonReader reader, Type objectType, ItemId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct CustomItemId(ulong Id) : IComparisonOperators<CustomItemId, CustomItemId, bool>
{
    private const ulong CustomFlag    = 1ul << 48;
    private const ulong BonusItemFlag = 1ul << 49;

    public bool IsBonusItem
        => (Id & BonusItemFlag) == BonusItemFlag;

    public bool IsItem
        => Id < CustomFlag;

    public bool IsCustom
        => (Id & CustomFlag) == CustomFlag;

    public ItemId Item
        => IsItem ? (ItemId)Id : 0;

    public BonusItemId BonusItem
        => IsBonusItem ? (BonusItemId)Id : BonusItemId.Invalid;

    public (PrimaryId Model, SecondaryId WeaponType, Variant Variant, FullEquipType Type) Split
        => IsItem ? (0, 0, 0, FullEquipType.Unknown) : ((PrimaryId)Id, (SecondaryId)(Id >> 16), (Variant)(Id >> 32), (FullEquipType)(Id >> 40));

    public (PrimaryId Model, Variant Variant, BonusItemFlag Slot) SplitBonus
        => (Id & (CustomFlag | BonusItemFlag)) == (CustomFlag | BonusItemFlag)
            ? ((PrimaryId)Id, (Variant)(Id >> 16), (BonusItemFlag)(Id >> 24))
            : (0, 0, Enums.BonusItemFlag.Unknown);

    public CustomItemId(PrimaryId model, SecondaryId secondaryId, Variant variant, FullEquipType type)
        : this(model.Id | ((ulong)secondaryId.Id << 16) | ((ulong)variant.Id << 32) | ((ulong)type << 40) | CustomFlag)
    { }

    public CustomItemId(PrimaryId model, Variant variant, BonusItemFlag slot)
        : this(model.Id | ((ulong)variant.Id << 16) | ((ulong)slot << 24) | CustomFlag | BonusItemFlag)
    { }

    public static implicit operator CustomItemId(BonusItemId id)
        => new(id.Id | BonusItemFlag);

    public static implicit operator CustomItemId(ItemId id)
        => new(id.Id);

    public static implicit operator CustomItemId(ulong id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    public string ToDiscriminatingString()
    {
        if (IsItem)
            return Id.ToString();

        if (IsBonusItem)
        {
            if (IsCustom)
            {
                var (primary, variant, type) = SplitBonus;
                return $"{primary}-{variant}-{type.ToName()}";
            }

            return $"{BonusItem}B";
        }

        if (IsCustom)
        {
            var (primary, secondary, variant, type) = Split;
            return $"{primary}-{secondary}-{variant}-{type.ToName()}";
        }

        return Id.ToString();
    }

    public static bool operator >(CustomItemId left, CustomItemId right)
        => left.Id > right.Id;

    public static bool operator >=(CustomItemId left, CustomItemId right)
        => left.Id >= right.Id;

    public static bool operator <(CustomItemId left, CustomItemId right)
        => left.Id < right.Id;

    public static bool operator <=(CustomItemId left, CustomItemId right)
        => left.Id <= right.Id;

    private class Converter : JsonConverter<CustomItemId>
    {
        public override void WriteJson(JsonWriter writer, CustomItemId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override CustomItemId ReadJson(JsonReader reader, Type objectType, CustomItemId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<ulong>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct IconId(uint Id)
{
    public static implicit operator IconId(uint id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<IconId>
    {
        public override void WriteJson(JsonWriter writer, IconId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override IconId ReadJson(JsonReader reader, Type objectType, IconId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<uint>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct WorldId(ushort Id)
{
    public static readonly WorldId AnyWorld = new(ushort.MaxValue);

    public static implicit operator WorldId(ushort id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<WorldId>
    {
        public override void WriteJson(JsonWriter writer, WorldId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override WorldId ReadJson(JsonReader reader, Type objectType, WorldId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<ushort>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct JobId(byte Id)
{
    public static implicit operator JobId(byte value)
        => new(value);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<JobId>
    {
        public override void WriteJson(JsonWriter writer, JobId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override JobId ReadJson(JsonReader reader, Type objectType, JobId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<byte>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct JobGroupId(byte Id)
{
    public static implicit operator JobGroupId(byte id)
        => new(id);

    public override string ToString()
        => Id.ToString();

    private class Converter : JsonConverter<JobGroupId>
    {
        public override void WriteJson(JsonWriter writer, JobGroupId value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Id);

        public override JobGroupId ReadJson(JsonReader reader, Type objectType, JobGroupId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<byte>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct CharacterLevel(byte Value)
{
    public static implicit operator CharacterLevel(byte value)
        => new(value);

    public override string ToString()
        => Value.ToString();

    private class Converter : JsonConverter<CharacterLevel>
    {
        public override void WriteJson(JsonWriter writer, CharacterLevel value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Value);

        public override CharacterLevel ReadJson(JsonReader reader, Type objectType, CharacterLevel existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<byte>(reader);
    }
}

[JsonConverter(typeof(Converter))]
public readonly record struct ObjectIndex(ushort Index) : IComparisonOperators<ObjectIndex, ObjectIndex, bool>, IComparable<ObjectIndex>
{
    public static readonly ObjectIndex AnyIndex        = new(ushort.MaxValue);
    public static readonly ObjectIndex CutsceneStart   = new((ushort)ScreenActor.CutsceneStart);
    public static readonly ObjectIndex GPosePlayer     = new((ushort)ScreenActor.GPosePlayer);
    public static readonly ObjectIndex CharacterScreen = new((ushort)ScreenActor.CharacterScreen);
    public static readonly ObjectIndex ExamineScreen   = new((ushort)ScreenActor.ExamineScreen);
    public static readonly ObjectIndex FittingRoom     = new((ushort)ScreenActor.FittingRoom);
    public static readonly ObjectIndex DyePreview      = new((ushort)ScreenActor.DyePreview);
    public static readonly ObjectIndex Portrait        = new((ushort)ScreenActor.Portrait);
    public static readonly ObjectIndex Card6           = new((ushort)ScreenActor.Card6);
    public static readonly ObjectIndex Card7           = new((ushort)ScreenActor.Card7);
    public static readonly ObjectIndex Card8           = new((ushort)ScreenActor.Card8);
    public static readonly ObjectIndex IslandStart     = new(729);

    public static implicit operator ObjectIndex(ushort index)
        => new(index);

    public int CompareTo(ObjectIndex other)
        => Index.CompareTo(other.Index);

    public override string ToString()
        => Index.ToString();

    public static bool operator >(ObjectIndex left, ObjectIndex right)
        => left.Index > right.Index;

    public static bool operator >=(ObjectIndex left, ObjectIndex right)
        => left.Index >= right.Index;

    public static bool operator <(ObjectIndex left, ObjectIndex right)
        => left.Index < right.Index;

    public static bool operator <=(ObjectIndex left, ObjectIndex right)
        => left.Index <= right.Index;

    private class Converter : JsonConverter<ObjectIndex>
    {
        public override void WriteJson(JsonWriter writer, ObjectIndex value, JsonSerializer serializer)
            => serializer.Serialize(writer, value.Index);

        public override ObjectIndex ReadJson(JsonReader reader, Type objectType, ObjectIndex existingValue, bool hasExistingValue,
            JsonSerializer serializer)
            => serializer.Deserialize<ushort>(reader);
    }
}
