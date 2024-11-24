using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak;
// had to make this to get a workaround for the fact that the json readwrite doesnt read from private readonly structs, and equipItem is a private readonly struct
public class EquipItemSurrogate
{
    public string Name { get; set; } = string.Empty;
    public CustomItemId Id { get; set; }
    public IconId IconId { get; set; }
    public PrimaryId PrimaryId { get; set; }
    public SecondaryId SecondaryId { get; set; }
    public Variant Variant { get; set; }
    public FullEquipType Type { get; set; }
    public ItemFlags Flags { get; set; }
    public CharacterLevel Level { get; set; }
    public JobGroupId JobRestrictions { get; set; }

    public static implicit operator EquipItem(EquipItemSurrogate surrogate)
    {
        return new EquipItem(surrogate.Name, surrogate.Id, surrogate.IconId, surrogate.PrimaryId, surrogate.SecondaryId, surrogate.Variant, surrogate.Type, surrogate.Flags, surrogate.Level, surrogate.JobRestrictions);
    }

    public static implicit operator EquipItemSurrogate(EquipItem equipItem)
    {
        return new EquipItemSurrogate
        {
            Name = equipItem.Name,
            Id = equipItem.Id,
            IconId = equipItem.IconId,
            PrimaryId = equipItem.PrimaryId,
            SecondaryId = equipItem.SecondaryId,
            Variant = equipItem.Variant,
            Type = equipItem.Type,
            Flags = equipItem.Flags,
            Level = equipItem.Level,
            JobRestrictions = equipItem.JobRestrictions
        };
    }
}
