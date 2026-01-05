using Buttplug.Client;
using GagSpeak.Kinksters;
using GagspeakAPI.Network;
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
public record MoodlesChanged(IntPtr Address) : MessageBase;
public record MoodleAccessPermsChanged(Kinkster Kinkster) : MessageBase;
public record MoodlesApplyStatusToPair(ApplyMoodleStatus ApplyStatusTupleDto) : MessageBase;

public record GlamourerReady : MessageBase;
public record GlamourerChanged : MessageBase;

public record CustomizeReady : MessageBase;
public record CustomizeProfileListRequest : MessageBase;


// Intiface IPC
public record BuzzToyAdded(ButtplugClientDevice Device) : MessageBase;
public record BuzzToyRemoved(ButtplugClientDevice Device) : MessageBase;
public record DeviceScanFinished : MessageBase; // Unsure how much this is actually needed?
public record IntifaceClientConnected : MessageBase;
public record IntifaceClientDisconnected : MessageBase;
