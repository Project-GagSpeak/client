using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace Penumbra.GameData.Interop;

public readonly unsafe struct Actor : IEquatable<Actor>
{
    private Actor(nint address)
        => Address = address;

    public static readonly Actor Null = new(nint.Zero);

    public readonly nint Address;

    public GameObject* AsObject
        => (GameObject*)Address;

    public Character* AsCharacter
        => (Character*)Address;

    public bool Valid
        => Address != nint.Zero;

    public bool IsPlayer
        => Valid && AsObject->ObjectKind is ObjectKind.Pc;

    public bool IsCharacter
        => Valid && AsObject->IsCharacter();

    public static implicit operator Actor(nint? pointer)
        => new(pointer ?? nint.Zero);

    public static implicit operator Actor(GameObject* pointer)
        => new((nint)pointer);

    public static implicit operator Actor(Character* pointer)
        => new((nint)pointer);

    public static implicit operator nint(Actor actor)
        => actor.Address;

    public bool IsGPoseOrCutscene
        => Index.Index is >= (int)ScreenActor.CutsceneStart and < (int)ScreenActor.CutsceneEnd;

    public bool IsTransformed
        => AsCharacter->CharacterData.TransformationId != 0;

    public ActorIdentifier GetIdentifier(ActorManager actors)
        => actors.FromObject(this, out _, true, true, false);

    public ByteString Utf8Name
        => Valid ? new ByteString(AsObject->Name) : ByteString.Empty;

    /// <summary> Does not check for validity. </summary>
    public ushort HomeWorld
        => AsCharacter->HomeWorld;

    public bool Identifier(ActorManager actors, out ActorIdentifier ident)
    {
        if (Valid)
        {
            ident = GetIdentifier(actors);
            return ident.IsValid;
        }

        ident = ActorIdentifier.Invalid;
        return false;
    }

    public ObjectIndex Index
        => Valid ? AsObject->ObjectIndex : ObjectIndex.AnyIndex;

    public Model Model
        => Valid ? AsObject->DrawObject : null;

    public byte Job
        => IsCharacter ? AsCharacter->CharacterData.ClassJob : (byte)0;

    public static implicit operator bool(Actor actor)
        => actor.Address != nint.Zero;

    public static bool operator true(Actor actor)
        => actor.Address != nint.Zero;

    public static bool operator false(Actor actor)
        => actor.Address == nint.Zero;

    public static bool operator !(Actor actor)
        => actor.Address == nint.Zero;

    public bool Equals(Actor other)
        => Address == other.Address;

    public override bool Equals(object? obj)
        => obj is Actor other && Equals(other);

    public override int GetHashCode()
        => Address.GetHashCode();

    public static bool operator ==(Actor lhs, Actor rhs)
        => lhs.Address == rhs.Address;

    public static bool operator !=(Actor lhs, Actor rhs)
        => lhs.Address != rhs.Address;

    /// <summary> Only valid for characters. </summary>
    public CharacterArmor GetArmor(EquipSlot slot)
        => ((CharacterArmor*)Unsafe.AsPointer(ref AsCharacter->DrawData.EquipmentModelIds[0]))[slot.ToIndex()];

    /// <summary> Only valid for characters. </summary>
    public BonusItemId GetBonusItem(BonusItemFlag slot)
        => AsCharacter->DrawData.GlassesIds[(int)slot.ToIndex()];

    public bool GetCrest(CrestFlag slot)
        => CrestBitfield.HasFlag(slot);

    public CharacterWeapon GetMainhand()
        => new(AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.MainHand).ModelId.Value);

    public CharacterWeapon GetOffhand()
        => new(AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).ModelId.Value);

    public CustomizeArray* Customize
        => (CustomizeArray*)&AsCharacter->DrawData.CustomizeData;

    public ref CrestFlag CrestBitfield
        => ref *(CrestFlag*)&AsCharacter->DrawData.FreeCompanyCrestBitfield;

    public override string ToString()
        => $"0x{Address:X}";

    public OnlineStatus OnlineStatus
        => (OnlineStatus)AsCharacter->CharacterData.OnlineStatus;

    public bool IsGPoseWet
    {
        get => AsCharacter->Effects.StatusEffects.HasFlag(EffectContainer.StatusEffect.IsGPoseWet);
        set => AsCharacter->Effects.StatusEffects = value
            ? AsCharacter->Effects.StatusEffects | EffectContainer.StatusEffect.IsGPoseWet
            : AsCharacter->Effects.StatusEffects & ~EffectContainer.StatusEffect.IsGPoseWet;
    }
}

public enum OnlineStatus : byte
{
    Normal          = 0x00,
    Mentor          = 0x1B,
    PvEMentor       = 0x1C,
    TradeMentor     = 0x1D,
    PvPMentor       = 0x1E,
    Busy            = 0x0C,
    Away            = 0x11,
    MeldMateria     = 0x15,
    RolePlaying     = 0x16,
    LookingForGroup = 0x17,
}
