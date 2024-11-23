using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel.Sheets;
using OtterGui.Classes;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Action = Lumina.Excel.Sheets.Action;
using Emote = Lumina.Excel.Sheets.Emote;
using ModelChara = Lumina.Excel.Sheets.ModelChara;

namespace Penumbra.GameData.Data;

public interface IIdentifiedObjectData
{
    public object? ToInternalObject();

    public (ChangedItemType Type, uint Id) ToApiObject();

    public string ToName(string key);

    public string AdditionalData { get; }

    public bool FilteredOut(string key, LowerString filter);

    public ChangedItemIcon Icon { get; }
}

public static class IdentifiedObjectExtensions
{
    public static (ChangedItemType Type, uint Id) ToApiObject(this object? value)
        => value switch
        {
            null                      => (ChangedItemType.None, 0),
            EquipItem it              => (it.Type.IsOffhandType() ? ChangedItemType.ItemOffhand : ChangedItemType.Item, it.Id.Item.Id),
            (Item i, FullEquipType t) => (t.IsOffhandType() ? ChangedItemType.ItemOffhand : ChangedItemType.Item, i.RowId),
            Action a                  => (ChangedItemType.Action, a.RowId),
            ModelChara m              => (ChangedItemType.Model, m.RowId),
            Emote e                   => (ChangedItemType.Emote, e.RowId),
            (ModelRace r, Gender g, CustomizeIndex t, CustomizeValue v) => (ChangedItemType.Customization,
                IdentifiedCustomization.Combine(r, g, t, v)),
            _ => (ChangedItemType.Unknown, 0),
        };

    public static bool IsFilteredOut(this IIdentifiedObjectData? data, string key, LowerString filter)
        => data?.FilteredOut(key, filter) ?? !filter.IsContained(key);

    public static ChangedItemIcon GetIcon(this IIdentifiedObjectData? data)
        => data?.Icon ?? ChangedItemIcon.Unknown;

    public static ChangedItemIcon GetCategoryIcon(this FullEquipType type)
        => type switch
        {
            FullEquipType.Unknown         => ChangedItemIcon.Unknown,
            FullEquipType.Head            => ChangedItemIcon.Head,
            FullEquipType.Body            => ChangedItemIcon.Body,
            FullEquipType.Hands           => ChangedItemIcon.Hands,
            FullEquipType.Legs            => ChangedItemIcon.Legs,
            FullEquipType.Feet            => ChangedItemIcon.Feet,
            FullEquipType.Ears            => ChangedItemIcon.Ears,
            FullEquipType.Neck            => ChangedItemIcon.Neck,
            FullEquipType.Wrists          => ChangedItemIcon.Wrists,
            FullEquipType.Finger          => ChangedItemIcon.Finger,
            FullEquipType.Fists           => ChangedItemIcon.Mainhand,
            FullEquipType.FistsOff        => ChangedItemIcon.Offhand,
            FullEquipType.Sword           => ChangedItemIcon.Mainhand,
            FullEquipType.Axe             => ChangedItemIcon.Mainhand,
            FullEquipType.Bow             => ChangedItemIcon.Mainhand,
            FullEquipType.BowOff          => ChangedItemIcon.Offhand,
            FullEquipType.Lance           => ChangedItemIcon.Mainhand,
            FullEquipType.Staff           => ChangedItemIcon.Mainhand,
            FullEquipType.Wand            => ChangedItemIcon.Mainhand,
            FullEquipType.Book            => ChangedItemIcon.Mainhand,
            FullEquipType.Daggers         => ChangedItemIcon.Mainhand,
            FullEquipType.DaggersOff      => ChangedItemIcon.Offhand,
            FullEquipType.Broadsword      => ChangedItemIcon.Mainhand,
            FullEquipType.Gun             => ChangedItemIcon.Mainhand,
            FullEquipType.GunOff          => ChangedItemIcon.Offhand,
            FullEquipType.Orrery          => ChangedItemIcon.Mainhand,
            FullEquipType.OrreryOff       => ChangedItemIcon.Offhand,
            FullEquipType.Katana          => ChangedItemIcon.Mainhand,
            FullEquipType.KatanaOff       => ChangedItemIcon.Offhand,
            FullEquipType.Rapier          => ChangedItemIcon.Mainhand,
            FullEquipType.RapierOff       => ChangedItemIcon.Offhand,
            FullEquipType.Cane            => ChangedItemIcon.Mainhand,
            FullEquipType.Gunblade        => ChangedItemIcon.Mainhand,
            FullEquipType.Glaives         => ChangedItemIcon.Mainhand,
            FullEquipType.GlaivesOff      => ChangedItemIcon.Offhand,
            FullEquipType.Scythe          => ChangedItemIcon.Mainhand,
            FullEquipType.Nouliths        => ChangedItemIcon.Mainhand,
            FullEquipType.Shield          => ChangedItemIcon.Offhand,
            FullEquipType.Saw             => ChangedItemIcon.Mainhand,
            FullEquipType.CrossPeinHammer => ChangedItemIcon.Mainhand,
            FullEquipType.RaisingHammer   => ChangedItemIcon.Mainhand,
            FullEquipType.LapidaryHammer  => ChangedItemIcon.Mainhand,
            FullEquipType.Knife           => ChangedItemIcon.Mainhand,
            FullEquipType.Needle          => ChangedItemIcon.Mainhand,
            FullEquipType.Alembic         => ChangedItemIcon.Mainhand,
            FullEquipType.Frypan          => ChangedItemIcon.Mainhand,
            FullEquipType.Pickaxe         => ChangedItemIcon.Mainhand,
            FullEquipType.Hatchet         => ChangedItemIcon.Mainhand,
            FullEquipType.FishingRod      => ChangedItemIcon.Mainhand,
            FullEquipType.ClawHammer      => ChangedItemIcon.Offhand,
            FullEquipType.File            => ChangedItemIcon.Offhand,
            FullEquipType.Pliers          => ChangedItemIcon.Offhand,
            FullEquipType.GrindingWheel   => ChangedItemIcon.Offhand,
            FullEquipType.Awl             => ChangedItemIcon.Offhand,
            FullEquipType.SpinningWheel   => ChangedItemIcon.Offhand,
            FullEquipType.Mortar          => ChangedItemIcon.Offhand,
            FullEquipType.CulinaryKnife   => ChangedItemIcon.Offhand,
            FullEquipType.Sledgehammer    => ChangedItemIcon.Offhand,
            FullEquipType.GardenScythe    => ChangedItemIcon.Offhand,
            FullEquipType.Gig             => ChangedItemIcon.Offhand,
            FullEquipType.Brush           => ChangedItemIcon.Mainhand,
            FullEquipType.Palette         => ChangedItemIcon.Offhand,
            FullEquipType.Twinfangs       => ChangedItemIcon.Mainhand,
            FullEquipType.TwinfangsOff    => ChangedItemIcon.Offhand,
            FullEquipType.Whip            => ChangedItemIcon.Mainhand,
            FullEquipType.UnknownMainhand => ChangedItemIcon.Mainhand,
            FullEquipType.UnknownOffhand  => ChangedItemIcon.Offhand,
            FullEquipType.Glasses         => ChangedItemIcon.Head,
            _                             => ChangedItemIcon.Unknown,
        };
}

public sealed class IdentifiedItem(EquipItem item) : IIdentifiedObjectData
{
    public EquipItem Item = item;

    public object ToInternalObject()
        => Item;

    public (ChangedItemType Type, uint Id) ToApiObject()
    {
        if (Item.Id.IsItem)
            return (Item.Type.IsOffhandType() ? ChangedItemType.ItemOffhand : ChangedItemType.Item, Item.ItemId.Id);

        if (Item.Type.ToSlot() is EquipSlot.MainHand or EquipSlot.OffHand)
            return (ChangedItemType.Unknown, 0);

        return (ChangedItemType.CustomArmor, Item.PrimaryId.Id | ((uint)Item.Variant.Id << 16) | ((uint)Item.Type << 24));
    }

    public string ToName(string key)
        => Item.Name;

    public string AdditionalData
        => Item.ModelString;

    public bool FilteredOut(string key, LowerString filter)
        => !filter.IsContained(key) && !filter.IsContained(AdditionalData);

    public ChangedItemIcon Icon
        => Item.Type.GetCategoryIcon();

    public static (PrimaryId Model, Variant Variant, FullEquipType Type) Split(uint id)
        => ((PrimaryId)id, (Variant)(id >> 16), (FullEquipType)(id >> 24));
}

public sealed class IdentifiedCustomization : IIdentifiedObjectData
{
    public ModelRace      Race   = ModelRace.Unknown;
    public Gender         Gender = Gender.Unknown;
    public CustomizeIndex Type;
    public CustomizeValue Value;

    public static IdentifiedCustomization FacePaint(CustomizeValue value)
        => new()
        {
            Type  = CustomizeIndex.FacePaint,
            Value = value,
        };

    public static IdentifiedCustomization Hair(ModelRace race, Gender gender, CustomizeValue value)
        => new()
        {
            Race   = race,
            Gender = gender,
            Type   = CustomizeIndex.Hairstyle,
            Value  = value,
        };

    public static IdentifiedCustomization Tail(ModelRace race, Gender gender, CustomizeValue value)
        => new()
        {
            Race   = race,
            Gender = gender,
            Type   = CustomizeIndex.TailShape,
            Value  = value,
        };

    public static IdentifiedCustomization Ears(ModelRace race, Gender gender, CustomizeValue value)
        => new()
        {
            Race   = race,
            Gender = gender,
            Type   = CustomizeIndex.TailShape,
            Value  = value,
        };

    public static IdentifiedCustomization Face(ModelRace race, Gender gender, CustomizeValue value)
        => new()
        {
            Race   = race,
            Gender = gender,
            Type   = CustomizeIndex.Face,
            Value  = value,
        };

    public object ToInternalObject()
        => (Race, Gender, Type, Value);

    public (ChangedItemType Type, uint Id) ToApiObject()
        => (ChangedItemType.Customization, Combine(Race, Gender, Type, Value));

    public static uint Combine(ModelRace r, Gender g, CustomizeIndex t, CustomizeValue v)
        => (uint)r | ((uint)g << 8) | ((uint)t << 16) | ((uint)v.Value << 24);

    public static (ModelRace r, Gender g, CustomizeIndex t, CustomizeValue v) Split(uint value)
        => ((ModelRace)(value & 0xFF), (Gender)(value >> 8), (CustomizeIndex)(value >> 16), (CustomizeValue)(value >> 24));

    public string ToName(string key)
        => key;

    public string AdditionalData
        => string.Empty;

    public bool FilteredOut(string key, LowerString filter)
        => !filter.IsContained(key);

    public ChangedItemIcon Icon
        => ChangedItemIcon.Customization;
}

public sealed class IdentifiedCounter(int counter = 1) : IIdentifiedObjectData
{
    public int Counter = counter;

    public static IdentifiedCounter operator +(IdentifiedCounter lhs, IdentifiedCounter rhs)
        => new(lhs.Counter + rhs.Counter);

    public object ToInternalObject()
        => Counter;

    public (ChangedItemType Type, uint Id) ToApiObject()
        => (ChangedItemType.None, 0);

    public string ToName(string key)
        => $"{Counter} Files Manipulating {key}s";

    public string AdditionalData
        => string.Empty;

    public bool FilteredOut(string key, LowerString filter)
        => !filter.IsContained(key) && !filter.IsContained(Counter.ToString()) && !filter.IsContained(" Files Manipulating ");

    public ChangedItemIcon Icon
        => ChangedItemIcon.Unknown;
}

public sealed class IdentifiedModel(ModelChara model) : IIdentifiedObjectData
{
    public readonly ModelChara Model = model;

    public object ToInternalObject()
        => Model;

    public (ChangedItemType Type, uint Id) ToApiObject()
        => (ChangedItemType.Model, Model.RowId);

    public string ToName(string key)
        => key;

    public string AdditionalData
        => $"({((CharacterBase.ModelType)Model.Type).ToName()} {Model.Model}-{Model.Base}-{Model.Variant})";

    public bool FilteredOut(string key, LowerString filter)
        => !filter.IsContained(key) && !filter.IsContained(AdditionalData);

    public ChangedItemIcon Icon
        => (CharacterBase.ModelType)Model.Type switch
        {
            CharacterBase.ModelType.DemiHuman => ChangedItemIcon.Demihuman,
            CharacterBase.ModelType.Monster   => ChangedItemIcon.Monster,
            _                                 => ChangedItemIcon.Unknown,
        };
}

public sealed class IdentifiedAction(Action action) : IIdentifiedObjectData
{
    public readonly Action Action = action;

    public object ToInternalObject()
        => Action;

    public (ChangedItemType Type, uint Id) ToApiObject()
        => (ChangedItemType.Action, Action.RowId);

    public string ToName(string key)
        => key;

    public string AdditionalData
        => $"({Action.RowId})";

    public bool FilteredOut(string key, LowerString filter)
        => !filter.IsContained(key) && !filter.IsContained(AdditionalData);

    public ChangedItemIcon Icon
        => ChangedItemIcon.Action;
}

public sealed class IdentifiedEmote(Emote emote) : IIdentifiedObjectData
{
    public readonly Emote Emote = emote;

    public object ToInternalObject()
        => Emote;

    public (ChangedItemType Type, uint Id) ToApiObject()
        => (ChangedItemType.Emote, Emote.RowId);

    public string ToName(string key)
        => key;

    public string AdditionalData
        => $"({Emote.RowId})";

    public bool FilteredOut(string key, LowerString filter)
        => !filter.IsContained(key) && !filter.IsContained(AdditionalData);

    public ChangedItemIcon Icon
        => ChangedItemIcon.Emote;
}
