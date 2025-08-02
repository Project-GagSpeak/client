using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.State.Models;

namespace GagSpeak.Services.Mediator;

// May remove this down the line if events turn out to be better, but we will see to be honest. For now stick with these.
public record ConfigGagRestrictionChanged(StorageChangeType Type, GarblerRestriction Item, string? OldString = null) : MessageBase;

public record ConfigRestrictionChanged(StorageChangeType Type, RestrictionItem Item, string? OldString = null) : MessageBase;

public record ConfigRestraintSetChanged(StorageChangeType Type, RestraintSet Item, string? OldString = null) : MessageBase;

public record ConfigCursedItemChanged(StorageChangeType Type, CursedItem Item, string? OldString = null) : MessageBase;

public record ConfigSexToyChanged(StorageChangeType Type, BuzzToy Item, string? OldString = null) : MessageBase;

public record ConfigPatternChanged(StorageChangeType Type, Pattern Item, string? OldString = null) : MessageBase;

public record ConfigAlarmChanged(StorageChangeType Type, Alarm Item, string? OldString = null) : MessageBase;

public record ConfigTriggerChanged(StorageChangeType Type, Trigger Item, string? OldString = null) : MessageBase;

public record ConfigModPresetChanged(StorageChangeType Type, ModPresetContainer Item, string? OldDirString = null) : MessageBase;

public record ReloadFileSystem(GagspeakModule Module) : MessageBase;

public record AllowancesChanged(GagspeakModule Module, IEnumerable<string> AllowedUids) : MessageBase;

public record SelectedModContainerChanged : MessageBase;
