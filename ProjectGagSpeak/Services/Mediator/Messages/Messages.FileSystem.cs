using GagSpeak.PlayerState.Models;

namespace GagSpeak.Services.Mediator;

// May remove this down the line if events turn out to be better, but we will see to be honest. For now stick with these.
public record ConfigGagRestrictionChanged(StorageItemChangeType Type, GarblerRestriction Item, string? OldString = null) : MessageBase;

public record ConfigRestrictionChanged(StorageItemChangeType Type, RestrictionItem Item, string? OldString = null) : MessageBase;

public record ConfigRestraintSetChanged(StorageItemChangeType Type, RestraintSet Item, string? OldString = null) : MessageBase;

public record ConfigCursedItemChanged(StorageItemChangeType Type, CursedItem Item, string? OldString = null) : MessageBase;

public record ConfigPatternChanged(StorageItemChangeType Type, Pattern Item, string? OldString = null) : MessageBase;

public record ConfigAlarmChanged(StorageItemChangeType Type, Alarm Item, string? OldString = null) : MessageBase;

public record ConfigTriggerChanged(StorageItemChangeType Type, Trigger Item, string? OldString = null) : MessageBase;

public record ReloadFileSystem(ModuleSection Module) : MessageBase;
