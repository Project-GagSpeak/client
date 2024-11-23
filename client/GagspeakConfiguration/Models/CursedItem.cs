using GagSpeak.Utils;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Data.Struct;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public record CursedItem
{
    public Guid LootId { get; private set; } = Guid.NewGuid();

    public string Name { get; set; } = "Unamed Cursed Item";

    /// <summary>
    /// If the cursed item is in the active cursed item pool to pull from.
    /// </summary>
    public bool InPool { get; set; } = false;

    /// <summary>
    /// Determines if this item is currently applied. 
    /// Is MinValue when not active.
    /// Is set to DateTime.UtcNow as applied, to determine the order each set is applied.
    /// This allows us to know when the active sets were applied to know the order.
    /// </summary>
    public DateTimeOffset AppliedTime { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The time when the item is taken off from the player.
    /// Should be MinValue when not active.
    /// </summary>
    public DateTimeOffset ReleaseTime { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// If this cursed item can override other cursed items in the same slot. Requires precedence.
    /// </summary>
    public bool CanOverride { get; set; } = false;

    /// <summary>
    /// Level of precedence an item has when marking comparison for overriding.
    /// </summary>
    public Precedence OverridePrecedence { get; set; } = Precedence.Default;

    /// <summary>
    /// Determines if the cursed item is a glamour or gag item.
    /// </summary>
    public bool IsGag { get; set; } = false;

    /// <summary>
    /// What gag to apply, if IsGag is true.
    /// </summary>
    public GagType GagType { get; set; } = GagType.None;

    /// <summary>
    /// The Game Item to apply for this item.
    /// </summary>
    public EquipDrawData AppliedItem { get; set; }

    /// <summary>
    /// Any mod to apply with this item.
    /// Includes if it should redraw on item toggle of be toggled at all when disabled.
    /// </summary>
    public AssociatedMod AssociatedMod { get; set; } = new();

    /// <summary>
    /// If the moodle to apply is a status, preset, or neither.
    /// </summary>
    public IpcToggleType MoodleType { get; set; } = IpcToggleType.MoodlesStatus;

    /// <summary>
    /// The moodle status or preset to attempt applying with the item.
    /// </summary>
    public Guid MoodleIdentifier { get; set; } = Guid.Empty;

    public CursedItem()
    {
        AppliedItem = new EquipDrawData(ItemIdVars.NothingItem(EquipSlot.Head))
        {
            IsEnabled = false,
            Slot = EquipSlot.Head
        };
    }

    public LightCursedItem ToLightData()
    {
        return new LightCursedItem()
        {
            Identifier = LootId,
            Name = Name,
            GagType = GagType,
            AffectedSlot = new AppliedSlot()
            {

                Slot = (byte)AppliedItem.Slot,
                CustomItemId = AppliedItem.GameItem.Id.Id,
                Tooltip = "This Slot is Locked!--SEP--An item on this slot is securely locked in place after an encounter\nwith a dungeon mimic chest that contained Cursed Loot!",
            }
        };
    }

    public CursedItem DeepCloneItem()
    {
        return new CursedItem()
        {
            LootId = this.LootId,
            Name = this.Name,
            InPool = this.InPool,
            AppliedTime = this.AppliedTime,
            ReleaseTime = this.ReleaseTime,
            CanOverride = this.CanOverride,
            OverridePrecedence = this.OverridePrecedence,
            IsGag = this.IsGag,
            GagType = this.GagType,
            AppliedItem = this.AppliedItem.DeepCloneDrawData(),
            AssociatedMod = this.AssociatedMod.DeepClone(),
            MoodleType = this.MoodleType,
            MoodleIdentifier = this.MoodleIdentifier
        };
    }

    // parameterless constructor for serialization
    public JObject Serialize()
    {
        return new JObject()
        {
            ["LootId"] = LootId.ToString(),
            ["Name"] = Name,
            ["InPool"] = InPool,
            ["AppliedTime"] = AppliedTime.UtcDateTime.ToString("o"),
            ["ReleaseTime"] = ReleaseTime.UtcDateTime.ToString("o"),
            ["CanOverride"] = CanOverride,
            ["OverridePrecedence"] = OverridePrecedence.ToString(),
            ["IsGag"] = IsGag,
            ["GagType"] = GagType.ToString(),
            ["AppliedItem"] = new JObject()
            {
                ["IsEnabled"] = AppliedItem.IsEnabled,
                ["Slot"] = AppliedItem.Slot.ToString(),
                ["CustomItemId"] = AppliedItem.GameItem.Id.ToString(),
                ["GameStain"] = AppliedItem.GameStain.ToString()
            },
            ["AssociatedMod"] = AssociatedMod.Serialize(),
            ["MoodleType"] = MoodleType.ToString(),
            ["MoodleIdentifier"] = MoodleIdentifier.ToString()
        };
    }

    public void Deserialize(JObject jsonObject)
    {
        LootId = Guid.TryParse(jsonObject["LootId"]?.Value<string>(), out var guid) ? guid : Guid.NewGuid();
        Name = jsonObject["Name"]?.Value<string>() ?? "Unnamed Cursed Item";
        InPool = jsonObject["InPool"]?.Value<bool>() ?? false;

        var applyTime = jsonObject["AppliedTime"]?.Value<DateTime>() ?? DateTime.MinValue;
        AppliedTime = new DateTimeOffset(applyTime, TimeSpan.Zero); // Zero indicates UTC
        var releaseTime = jsonObject["ReleaseTime"]?.Value<DateTime>() ?? DateTime.MinValue;
        ReleaseTime = new DateTimeOffset(releaseTime, TimeSpan.Zero); // Zero indicates UTC

        CanOverride = jsonObject["CanOverride"]?.Value<bool>() ?? false;
        OverridePrecedence = Enum.TryParse<Precedence>(jsonObject["OverridePrecedence"]?.Value<string>(), out var precedence) ? precedence : Precedence.Default;

        IsGag = jsonObject["IsGag"]?.Value<bool>() ?? false;
        GagType = Enum.TryParse<GagType>(jsonObject["GagType"]?.Value<string>(), out var gagType) ? gagType : GagType.None;

        // applied item.
        if (jsonObject["AppliedItem"] is JObject appliedItemObj)
        {
            var slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), appliedItemObj["Slot"]?.Value<string>() ?? string.Empty);
            ulong customItemId = appliedItemObj["CustomItemId"]?.Value<ulong>() ?? 4294967164;
            var gameStainString = appliedItemObj["GameStain"]?.Value<string>() ?? "0,0";
            var stainParts = gameStainString.Split(',');

            StainIds gameStain;
            if (stainParts.Length == 2 && int.TryParse(stainParts[0], out int stain1) && int.TryParse(stainParts[1], out int stain2))
                gameStain = new StainIds((StainId)stain1, (StainId)stain2);
            else
                gameStain = StainIds.None;

            var drawData = new EquipDrawData(ItemIdVars.NothingItem(slot))
            {
                Slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), appliedItemObj["Slot"]?.Value<string>() ?? string.Empty),
                IsEnabled = appliedItemObj["IsEnabled"]?.Value<bool>() ?? false,
                GameItem = ItemIdVars.Resolve(slot, new CustomItemId(customItemId)),
                GameStain = gameStain
            };

            AppliedItem = drawData;
        }

        // applied mod
        if (jsonObject["AssociatedMod"] is JObject associatedModObj)
            AssociatedMod = associatedModObj.ToObject<AssociatedMod>() ?? new AssociatedMod();

        // applied moodle.
        MoodleType = Enum.TryParse<IpcToggleType>(jsonObject["MoodleType"]?.Value<string>(), out var moodleType) ? moodleType : IpcToggleType.MoodlesStatus;
        MoodleIdentifier = Guid.TryParse(jsonObject["MoodleIdentifier"]?.Value<string>(), out var moodleId) ? moodleId : Guid.Empty;


    }
}
