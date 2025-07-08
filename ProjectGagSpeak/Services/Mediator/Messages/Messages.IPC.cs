using Buttplug.Client;
using GagspeakAPI.Network;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Services.Mediator;

public record PenumbraInitializedMessage : MessageBase;
public record PenumbraDisposedMessage : MessageBase;
public record MoodlesReady : MessageBase;
public record GlamourerReady : MessageBase;
public record CustomizeReady : MessageBase;
public record CustomizeProfileListRequest : MessageBase;
public record CustomizeDispose : MessageBase;
public record TooltipSetItemToEditorMessage(EquipSlot Slot, EquipItem Item) : MessageBase;
public record MoodlesStatusManagerUpdate : MessageBase;
public record MoodlesStatusModified(Guid Guid) : MessageBase; // when we change one of our moodles settings.
public record MoodlesPresetModified(Guid Guid) : MessageBase; // when we change one of our moodles presets.
public record MoodlesApplyStatusToPair(MoodlesApplierByStatus StatusDto) : MessageBase;
public record VisibleKinkstersChanged : MessageBase; // for pinging the moodles.
public record MoodlesPermissionsUpdated(string NameWithWorld) : MessageBase;
// Intiface IPC
public record BuzzToyAdded(ButtplugClientDevice Device) : MessageBase;
public record BuzzToyRemoved(ButtplugClientDevice Device) : MessageBase;
public record DeviceScanFinished : MessageBase; // Unsure how much this is actually needed?
public record IntifaceClientConnected : MessageBase;
public record IntifaceClientDisconnected : MessageBase;
