using Dalamud.Game;
using Penumbra.GameData.Enums;

namespace Penumbra.GameData.Structs;

/// <summary> A summary of what a game identity a game path represents. </summary>
[StructLayout(LayoutKind.Explicit)]
public struct GameObjectInfo : IComparable
{
    /// <summary> Equipment paths contain up to a file type, primary ID, race code, slot and variant. </summary>
    public static GameObjectInfo Equipment(FileType type, PrimaryId primaryId, GenderRace gr = GenderRace.Unknown
        , EquipSlot slot = EquipSlot.Unknown, Variant variant = default)
        => new()
        {
            FileType   = type,
            ObjectType = slot.IsAccessory() ? ObjectType.Accessory : ObjectType.Equipment,
            PrimaryId  = primaryId,
            GenderRace = gr,
            Variant    = variant,
            EquipSlot  = slot,
        };

    /// <summary> Weapon paths contain up to a file type, primary ID, secondary ID and variant. </summary>
    public static GameObjectInfo Weapon(FileType type, PrimaryId primaryId, SecondaryId weaponId, Variant variant = default)
        => new()
        {
            FileType    = type,
            ObjectType  = ObjectType.Weapon,
            PrimaryId   = primaryId,
            SecondaryId = weaponId,
            Variant     = variant,
        };

    /// <summary> Customizations contain up to a file type, the type of customization, a primary ID, a race code, a body slot and a variant. </summary>
    public static GameObjectInfo Customization(FileType type, CustomizationType customizationType, PrimaryId id = default
        , GenderRace gr = GenderRace.Unknown, BodySlot bodySlot = BodySlot.Unknown, Variant variant = default)
        => new()
        {
            FileType          = type,
            ObjectType        = ObjectType.Character,
            PrimaryId         = id,
            GenderRace        = gr,
            BodySlot          = bodySlot,
            Variant           = variant,
            CustomizationType = customizationType,
        };

    /// <summary> Monsters contain up to a file type, primary ID, secondary ID and variant. </summary>
    public static GameObjectInfo Monster(FileType type, PrimaryId monsterId, SecondaryId bodyId, Variant variant = default)
        => new()
        {
            FileType    = type,
            ObjectType  = ObjectType.Monster,
            PrimaryId   = monsterId,
            SecondaryId = bodyId,
            Variant     = variant,
        };

    /// <summary> Demihumans contain up to a file type, primary ID, secondary ID, slot and variant. </summary>
    public static GameObjectInfo DemiHuman(FileType type, PrimaryId demiHumanId, SecondaryId bodyId, EquipSlot slot = EquipSlot.Unknown,
        Variant variant = default)
        => new()
        {
            FileType    = type,
            ObjectType  = ObjectType.DemiHuman,
            PrimaryId   = demiHumanId,
            SecondaryId = bodyId,
            Variant     = variant,
            EquipSlot   = slot,
        };

    /// <summary> Maps contain multiple unknown bytes, a variant and a suffix. </summary>
    public static GameObjectInfo Map(FileType type, byte c1, byte c2, byte c3, byte c4, Variant variant, byte suffix = 0)
        => new()
        {
            FileType   = type,
            ObjectType = ObjectType.Map,
            MapC1      = c1,
            MapC2      = c2,
            MapC3      = c3,
            MapC4      = c4,
            MapSuffix  = suffix,
            Variant    = variant,
        };

    /// <summary> Icons contain up to an ID, a high quality flag, a high resolution flag, and a language. </summary>
    public static GameObjectInfo Icon(FileType type, uint iconId, bool hq, bool hr, ClientLanguage lang = ClientLanguage.English)
        => new()
        {
            FileType   = type,
            ObjectType = ObjectType.Icon,
            IconId     = iconId,
            IconHqHr   = (byte)(hq ? hr ? 3 : 1 : hr ? 2 : 0),
            Language   = lang,
        };


    /// <summary> The full value representing the object.
    /// 
    /// </summary>
    [FieldOffset(0)]
    public readonly ulong Identifier;

    /// <summary> The file type. </summary>
    [FieldOffset(0)]
    public FileType FileType;

    /// <summary> The object type. </summary>
    [FieldOffset(1)]
    public ObjectType ObjectType;

    /// <summary> The primary ID. Used by Equipment, Weapon, Customization, Monster, DemiHuman. </summary>
    [FieldOffset(2)]
    public PrimaryId PrimaryId;

    /// <summary> The icon ID. Used by Icons. </summary>
    [FieldOffset(2)]
    public uint IconId;

    /// <summary> The first unknown byte for Maps. </summary>
    [FieldOffset(2)]
    public byte MapC1;

    /// <summary> The second unknown byte for Maps. </summary>
    [FieldOffset(3)]
    public byte MapC2;

    /// <summary> The secondary ID. Used by Weapon, Monster, Demihuman. </summary>
    [FieldOffset(4)]
    public SecondaryId SecondaryId;

    /// <summary> The third unknown byte for Maps. </summary>
    [FieldOffset(4)]
    public byte MapC3;

    /// <summary> The gender race code reduced to a single byte. Used by Equipment and Customization. </summary>
    [FieldOffset(4)]
    private byte _genderRaceByte;

    /// <summary> The gender race code as actual enum value. </summary>
    public GenderRace GenderRace
    {
        readonly get => Names.GenderRaceFromByte(_genderRaceByte);
        set => _genderRaceByte = value.ToByte();
    }

    /// <summary> The body slot. Used by Customization. </summary>
    [FieldOffset(5)]
    public BodySlot BodySlot;

    /// <summary> The fourth unknown byte for Maps. </summary>
    [FieldOffset(5)]
    public byte MapC4;

    /// <summary> The variant. Used by materials and textures of Equipment, Weapon, Customization, Map, Monster, Demihuman. </summary>
    [FieldOffset(6)]
    public Variant Variant;

    /// <summary> Icon display flags. 2 for high resolution, 1 for high quality, 3 for both. Used for Icon. </summary>
    [FieldOffset(6)]
    public byte IconHqHr;

    /// <summary> The equip slot. Used by Equipment and Demihuman. </summary>
    [FieldOffset(7)]
    public EquipSlot EquipSlot;

    /// <summary> The customization type. Used by Customization. </summary>
    [FieldOffset(7)]
    public CustomizationType CustomizationType;

    /// <summary> The language. Used by Icon. </summary>
    [FieldOffset(7)]
    public ClientLanguage Language;

    /// <summary> The map suffix. Used by Map. </summary>
    [FieldOffset(7)]
    public byte MapSuffix;

    /// <inheritdoc/>
    public override readonly int GetHashCode()
        => Identifier.GetHashCode();

    /// <inheritdoc/>
    public readonly int CompareTo(object? r)
        => Identifier.CompareTo(r);
}
