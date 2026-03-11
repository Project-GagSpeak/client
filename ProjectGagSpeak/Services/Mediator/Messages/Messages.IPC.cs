using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Services.Mediator;

public record TooltipSetItemToEditorMessage(EquipSlot Slot, EquipItem Item) : MessageBase;

public record PenumbraInitialized : MessageBase;
public record PenumbraDirectoryChanged(string? NewDirectory) : MessageBase;
public record PenumbraSettingsChanged : MessageBase;
public record PenumbraDisposed : MessageBase;

public record SundouleiaReady : MessageBase;
public record SundouleiaDisposed : MessageBase;

public record MoodlesReady : MessageBase;
public record MoodlesDisposed : MessageBase;

public record LociReady : MessageBase;
public record LociDisposed : MessageBase;

public record GlamourerReady : MessageBase;
public record GlamourerChanged : MessageBase;

public record CustomizeReady : MessageBase;
public record CustomizeProfileListRequest : MessageBase;

