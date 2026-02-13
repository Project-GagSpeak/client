using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak;

// An intenal Static accessor for all DalamudPlugin interfaces, because im tired of interface includes.
// And the difference is neglegable and its basically implied to make them static with the PluginService attribute.

/// <summary>
///     A collection of internally handled Dalamud Interface static services
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Svc
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] public static IPluginLog Logger { get; set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
    [PluginService] public static IAddonEventManager AddonEventManager { get; private set; }
    [PluginService] public static IAetheryteList AetheryteList { get; private set; }
    //[PluginService] public static ITitleScreenMenu TitleScreenMenu { get; private set; } = null!;
    [PluginService] public static IBuddyList Buddies { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; set; } = null!;
    [PluginService] public static IClientState ClientState { get; set; } = null!;
    [PluginService] public static IPlayerState PlayerState { get; set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; }
    [PluginService] public static ICondition Condition { get; private set; }
    [PluginService] public static IContextMenu ContextMenu { get; private set; }
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] public static IDutyState DutyState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    //[PluginService] public static IGameInventory GameInventory { get; private set; } = null!;
    //[PluginService] public static IGameNetwork GameNetwork { get; private set; } = null!;
    //[PluginService] public static IJobGauges Gauges { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IGameLifecycle GameLifeCycle { get; private set; } = null!;
    [PluginService] public static IGamepadState GamepadState { get; private set; } = null!;
    [PluginService] public static IKeyState KeyState { get; private set; } = null!;
    [PluginService] public static INotificationManager Notifications { get; private set; } = null!;
    [PluginService] public static INamePlateGui NamePlate { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] public static IPartyList Party { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static ITargetManager Targets { get; private set; } = null!;
    [PluginService] public static ITextureProvider Texture { get; private set; } = null!;
    [PluginService] public static IToastGui Toasts { get; private set; } = null!;
    [PluginService] public static ITextureSubstitutionProvider TextureSubstitution { get; private set; } = null!;
}

/// <summary>
///     Static container to help deal with penumbra.gamedata accessor hell, and simplify item resolving.
///     Because I want to sometimes curl up and cry resolving it normally.
/// </summary>
public static class ItemSvc
{
    public const string Nothing = EquipItem.Nothing;
    public const string SmallClothesNpc = "Smallclothes (NPC)";
    public const ushort SmallClothesNpcModel = 9903;

    public static DictWorld WorldData { get; private set; } = null!;
    public static DictBonusItems BonusData { get; private set; } = null!;
    public static DictStain Stains { get; private set; } = null!;
    public static ItemData ItemData { get; private set; } = null!;
    public static ItemsByType ItemsByType { get; private set; } = null!;
    public static ItemsPrimaryModel ItemsPrimary { get; private set; } = null!;
    public static ItemsSecondaryModel ItemsSecondary { get; private set; } = null!;
    public static ItemsTertiaryModel ItemsTertiary { get; private set; } = null!;

    public static bool _isInitialized = false;

    public static void Init(IDalamudPluginInterface pi)
    {
        if (_isInitialized)
            return;

        var logger = new Logger();

        WorldData = new DictWorld(pi, logger, Svc.Data);
        BonusData = new DictBonusItems(pi, logger, Svc.Data);
        Stains = new DictStain(pi, logger, Svc.Data);
        // init the precursors to itemData.
        ItemsByType = new ItemsByType(pi, logger, Svc.Data, BonusData);
        ItemsPrimary = new ItemsPrimaryModel(pi, logger, Svc.Data, ItemsByType);
        ItemsSecondary = new ItemsSecondaryModel(pi, logger, Svc.Data, ItemsByType);
        ItemsTertiary = new ItemsTertiaryModel(pi, logger, Svc.Data, ItemsByType, ItemsSecondary);
        // now that we have the precursors, we can init itemData.
        ItemData = new ItemData(ItemsByType, ItemsPrimary, ItemsSecondary, ItemsTertiary);

        _isInitialized = true;
    }

    public static void Dispose()
    {
        if (!_isInitialized)
            return;
        // Dispose of the precursors.
        ItemsByType.Dispose();
        ItemsPrimary.Dispose();
        ItemsSecondary.Dispose();
        ItemsTertiary.Dispose();
        // Dispose of the bonus data.
        BonusData.Dispose();
        // Dispose of the stains.
        Stains.Dispose();

        _isInitialized = false;
    }

    public static ItemId NothingId(EquipSlot slot)
    => uint.MaxValue - 128 - (uint)slot.ToSlot();

    public static ItemId SmallclothesId(EquipSlot slot)
        => uint.MaxValue - 256 - (uint)slot.ToSlot();

    public static ItemId NothingId(FullEquipType type)
        => uint.MaxValue - 384 - (uint)type;

    public static EquipItem NothingItem(EquipSlot slot)
        => new(Nothing, NothingId(slot), 0, 0, 0, 0, slot.ToEquipType(), 0, 0, 0);

    public static EquipItem NothingItem(FullEquipType type)
        => new(Nothing, NothingId(type), 0, 0, 0, 0, type, 0, 0, 0);

    public static EquipItem SmallClothesItem(EquipSlot slot)
        => new(SmallClothesNpc, SmallclothesId(slot), 0, SmallClothesNpcModel, 0, 1, slot.ToEquipType(), 0, 0, 0);

    public static EquipItem Resolve(EquipSlot slot, CustomItemId itemId)
    {
        slot = slot.ToSlot();
        if (itemId == NothingId(slot))
            return NothingItem(slot);
        if (itemId == SmallclothesId(slot))
            return SmallClothesItem(slot);

        if (!itemId.IsItem)
        {
            var item = EquipItem.FromId(itemId);
            return item;
        }
        else if (!ItemData.TryGetValue(itemId.Item, slot, out var item))
        {
            return EquipItem.FromId(itemId);
        }
        else
        {
            if (item.Type.ToSlot() != slot)
                return new EquipItem(string.Intern($"Invalid #{itemId}"), itemId, item.IconId, item.PrimaryId,
                    item.SecondaryId, item.Variant, 0, 0, 0, 0);

            return item;
        }
    }

    public static IReadOnlyList<EquipItem> GetBonusItems(BonusItemFlag slot)
    {
        var nothing = EquipItem.BonusItemNothing(slot);
        return ItemData.ByType[slot.ToEquipType()].OrderBy(i => i.Name).Prepend(nothing).ToList();
    }

    /// <summary> Returns whether an item id represents a valid item for a slot and gives the item. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsItemValid(EquipSlot slot, CustomItemId itemId, out EquipItem item)
    {
        item = Resolve(slot, itemId);
        return item.Valid;
    }

    /// <summary> Returns whether a bonus item id represents a valid item for a slot and gives the item. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsBonusItemValid(BonusItemFlag slot, BonusItemId itemId, out EquipItem item)
    {
        if (itemId.Id != 0)
            return BonusData.TryGetValue(itemId, out item) && slot == item.Type.ToBonus();

        item = EquipItem.BonusItemNothing(slot);
        return true;
    }

    public static EquipItem Resolve(BonusItemFlag slot, BonusItemId id)
        => IsBonusItemValid(slot, id, out var item) ? item : new EquipItem($"Invalid ({id.Id})", id, 0, 0, 0, 0, slot.ToEquipType(), 0, 0, 0);

    public static EquipItem Resolve(BonusItemFlag slot, CustomItemId id)
    {
        // Only from early designs as migration.
        if (!id.IsBonusItem || id.Id == 0)
        {
            IsBonusItemValid(slot, (BonusItemId)id.Id, out var item);
            return item;
        }

        if (!id.IsCustom)
        {
            if (IsBonusItemValid(slot, id.BonusItem, out var item))
                return item;

            return EquipItem.BonusItemNothing(slot);
        }

        var (model, variant, slot2) = id.SplitBonus;
        if (slot != slot2)
            return EquipItem.BonusItemNothing(slot);

        return Resolve(slot, id.BonusItem);
    }


    /// <summary> Serializes the GlamourSlot into a JToken. </summary>
    /// <param name="slot"> The GlamourSlot to serialize. </param>
    /// <returns> The JToken representing the GlamourSlot. </returns>
    public static JObject SerializeGlamourSlot(GlamourSlot slot)
        => new JObject
        {
            ["Slot"] = slot.Slot.ToString(),
            ["CustomItemId"] = slot.GameItem.Id.ToString(),
            ["Stains"] = slot.GameStain.ToString(),
        };


    /// <summary> Parses out the GlamourSlot from the JToken. </summary>
    /// <param name="item"> The JToken encapsulating a GlamourSlot Item. </param>
    /// <returns> The GlamourSlot item parsed from the token. </returns>
    /// <exception cref="InvalidDataException"> Thrown if the JToken is not a valid GlamourSlot object. </exception>"
    public static GlamourSlot ParseGlamourSlot(JToken? item)
    {
        if (item is not JObject json)
            throw new InvalidDataException("Invalid GlamourSlot JToken object.");

        var slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), json["Slot"]?.Value<string>() ?? string.Empty);
        return new GlamourSlot()
        {
            Slot = slot,
            GameItem = Resolve(slot, new CustomItemId(json["CustomItemId"]?.Value<ulong>() ?? 4294967164)),
            GameStain = GagspeakEx.ParseCompactStainIds(json)
        };
    }

    /// <summary> Serializes the GlamourBonusSlot into a JToken. </summary>
    /// <param name="slot"> The GlamourBonusSlot to serialize. </param>
    /// <returns> The JToken representing the GlamourBonusSlot. </returns>
    public static JObject SerializeBonusSlot(GlamourBonusSlot slot)
        => new JObject
        {
            ["Slot"] = slot.Slot.ToString(),
            ["CustomItemId"] = slot.GameItem.Id.BonusItem.ToString(),
        };

    public static GlamourBonusSlot ParseBonusSlot(JToken? item)
    {
        if (item is not JObject json)
            throw new InvalidDataException("Invalid GlamourBonusSlot JToken object.");

        var slot = (BonusItemFlag)Enum.Parse(typeof(BonusItemFlag), json["Slot"]?.Value<string>() ?? string.Empty);
        return new GlamourBonusSlot()
        {
            Slot = slot,
            GameItem = Resolve(slot, json["CustomItemId"]?.Value<ushort>() ?? ushort.MaxValue),
        };
    }
}
