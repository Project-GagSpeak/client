using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.State.Models;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

// May remove this down the line if events turn out to be better, but we will see to be honest. For now stick with these.
public record ConfigGagRestrictionChanged(StorageChangeType Type, GarblerRestriction Item, string? OldString = null) : MessageBase;

public record ConfigRestrictionChanged(StorageChangeType Type, RestrictionItem Item, string? OldString = null) : MessageBase;

public record ConfigRestraintSetChanged(StorageChangeType Type, RestraintSet Item, string? OldString = null) : MessageBase;

public record ConfigCollarChanged(StorageChangeType Type, GagSpeakCollar Item, string? OldString = null) : MessageBase;

public record ConfigCursedItemChanged(StorageChangeType Type, CursedItem Item, string? OldString = null) : MessageBase;

public record ConfigAliasItemChanged(StorageChangeType Type, AliasTrigger Item, string? OldString = null) : MessageBase;

public record ConfigSexToyChanged(StorageChangeType Type, BuzzToy Item, string? OldString = null) : MessageBase;

public record ConfigPatternChanged(StorageChangeType Type, Pattern Item, string? OldString = null) : MessageBase;

public record ConfigAlarmChanged(StorageChangeType Type, Alarm Item, string? OldString = null) : MessageBase;

public record ConfigTriggerChanged(StorageChangeType Type, Trigger Item, string? OldString = null) : MessageBase;

public record ConfigModPresetChanged(StorageChangeType Type, ModPresetContainer Item, string? OldDirString = null) : MessageBase;

public record ReloadFileSystem(GSModule Module) : MessageBase;
