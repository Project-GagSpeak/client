using Buttplug.Client;
using GagSpeak.Kinksters;
using GagspeakAPI.Network;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Services.Mediator;

public record PenumbraInitialized : MessageBase;
public record PenumbraDirectoryChanged(string? NewDirectory) : MessageBase;
public record PenumbraDisposed : MessageBase;
public record MoodlesReady : MessageBase;
public record GlamourerReady : MessageBase;
public record GlamourerChanged : MessageBase; // Only sent for CLIENT Glamourer changes
public record CustomizeReady : MessageBase;
public record CustomizeProfileChange(IntPtr Address, Guid Id) : MessageBase;
public record CustomizeProfileListRequest : MessageBase;
public record CustomizeDisposed : MessageBase;
public record HeelsOffsetChanged : MessageBase; // Whenever the client's Heel offset changes.
public record HonorificReady : MessageBase;
public record HonorificTitleChanged(string NewTitle) : MessageBase;
public record PetNamesReady : MessageBase;
public record PetNamesDataChanged(string NicknamesData) : MessageBase;
public record TooltipSetItemToEditorMessage(EquipSlot Slot, EquipItem Item) : MessageBase;
public record MoodlesStatusManagerUpdate : MessageBase;
public record MoodlesStatusModified(Guid Guid) : MessageBase; // when we change one of our moodles settings.
public record MoodlesPresetModified(Guid Guid) : MessageBase; // when we change one of our moodles presets.
public record MoodlesApplyStatusToPair(MoodlesApplierByStatus StatusDto) : MessageBase;
public record VisibleKinkstersChanged : MessageBase; // for pinging the moodles.
public record MoodlesPermissionsUpdated(Kinkster Kinkster) : MessageBase;
// Intiface IPC
public record BuzzToyAdded(ButtplugClientDevice Device) : MessageBase;
public record BuzzToyRemoved(ButtplugClientDevice Device) : MessageBase;
public record DeviceScanFinished : MessageBase; // Unsure how much this is actually needed?
public record IntifaceClientConnected : MessageBase;
public record IntifaceClientDisconnected : MessageBase;
