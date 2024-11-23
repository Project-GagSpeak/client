using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Object = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;
using ObjectType = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.ObjectType;

namespace Penumbra.GameData.Interop;

public readonly unsafe struct Model : IEquatable<Model>
{
    private Model(nint address)
        => Address = address;

    public readonly nint Address;

    public static readonly Model Null = new(0);

    public DrawObject* AsDrawObject
        => (DrawObject*)Address;

    public CharacterBase* AsCharacterBase
        => (CharacterBase*)Address;

    public Weapon* AsWeapon
        => (Weapon*)Address;

    public Human* AsHuman
        => (Human*)Address;

    public static implicit operator Model(nint? pointer)
        => new(pointer ?? nint.Zero);

    public static implicit operator Model(Object* pointer)
        => new((nint)pointer);

    public static implicit operator Model(DrawObject* pointer)
        => new((nint)pointer);

    public static implicit operator Model(Human* pointer)
        => new((nint)pointer);

    public static implicit operator Model(CharacterBase* pointer)
        => new((nint)pointer);

    public static implicit operator nint(Model model)
        => model.Address;

    public bool Valid
        => Address != nint.Zero;

    public bool IsCharacterBase
        => Valid && AsDrawObject->Object.GetObjectType() == ObjectType.CharacterBase;

    public bool IsHuman
        => IsCharacterBase && AsCharacterBase->GetModelType() == CharacterBase.ModelType.Human;

    public bool IsWeapon
        => IsCharacterBase && AsCharacterBase->GetModelType() == CharacterBase.ModelType.Weapon;

    public static implicit operator bool(Model actor)
        => actor.Address != nint.Zero;

    public static bool operator true(Model actor)
        => actor.Address != nint.Zero;

    public static bool operator false(Model actor)
        => actor.Address == nint.Zero;

    public static bool operator !(Model actor)
        => actor.Address == nint.Zero;

    public bool Equals(Model other)
        => Address == other.Address;

    public override bool Equals(object? obj)
        => obj is Model other && Equals(other);

    public override int GetHashCode()
        => Address.GetHashCode();

    public static bool operator ==(Model lhs, Model rhs)
        => lhs.Address == rhs.Address;

    public static bool operator !=(Model lhs, Model rhs)
        => lhs.Address != rhs.Address;

    /// <summary> Only valid for humans. </summary>
    public CharacterArmor GetArmor(EquipSlot slot)
        => ((CharacterArmor*)&AsHuman->Head)[slot.ToIndex()];

    public CharacterArmor GetBonus(BonusItemFlag slot)
        => ((CharacterArmor*)&AsHuman->Glasses0)[slot.ToIndex()];

    public CustomizeArray GetCustomize()
        => *(CustomizeArray*)&AsHuman->Customize;

    public (Model Address, CharacterWeapon Data) GetMainhand()
    {
        Model weapon = AsDrawObject->Object.ChildObject;
        return !weapon.IsWeapon
            ? (Null, CharacterWeapon.Empty)
            : (weapon,
                new CharacterWeapon(weapon.AsWeapon->ModelSetId, weapon.AsWeapon->SecondaryId, (Variant)weapon.AsWeapon->Variant,
                    new StainIds(weapon.AsWeapon->Stain0, weapon.AsWeapon->Stain1)));
    }

    public (Model Address, CharacterWeapon Data) GetOffhand()
    {
        var mainhand = AsDrawObject->Object.ChildObject;
        if (mainhand == null)
            return (Null, CharacterWeapon.Empty);

        Model offhand = mainhand->NextSiblingObject;
        if (offhand == mainhand || !offhand.IsWeapon)
            return (Null, CharacterWeapon.Empty);

        return (offhand,
            new CharacterWeapon(offhand.AsWeapon->ModelSetId, offhand.AsWeapon->SecondaryId, (Variant)offhand.AsWeapon->Variant,
                new StainIds(offhand.AsWeapon->Stain0, offhand.AsWeapon->Stain1)));
    }

    /// <summary> Obtain the mainhand and offhand and their data by using the drawdata container from the corresponding actor. </summary>
    public (Model Mainhand, Model Offhand, CharacterWeapon MainData, CharacterWeapon OffData) GetWeapons(Actor actor)
    {
        if (!Valid || !actor.IsCharacter || actor.Model.Address != Address)
            return (Null, Null, CharacterWeapon.Empty, CharacterWeapon.Empty);

        Model main     = actor.AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.MainHand).DrawObject;
        var   mainData = CharacterWeapon.Empty;
        if (main.IsWeapon)
            mainData = new CharacterWeapon(main.AsWeapon->ModelSetId, main.AsWeapon->SecondaryId, (Variant)main.AsWeapon->Variant,
                new StainIds(main.AsWeapon->Stain0, main.AsWeapon->Stain1)); // TODO stain
        else
            main = Null;
        Model off     = actor.AsCharacter->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).DrawObject;
        var   offData = CharacterWeapon.Empty;
        if (off.IsWeapon)
            offData = new CharacterWeapon(off.AsWeapon->ModelSetId, off.AsWeapon->SecondaryId, (Variant)off.AsWeapon->Variant,
                new StainIds(off.AsWeapon->Stain0, off.AsWeapon->Stain1)); // TODO stain
        else
            off = Null;
        return (main, off, mainData, offData);
    }

    public override string ToString()
        => $"0x{Address:X}";
}
