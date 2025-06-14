using Buttplug.Client;
using GagSpeak.State.Models;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

#pragma warning disable MA0048, S2094
public record ToyScanStarted : MessageBase; // for when the toybox scan is started.
public record ToyScanFinished : MessageBase; // for when the toybox scan is finished.
public record VibratorModeToggled(VibratorEnums VibratorMode) : MessageBase; // for when the vibrator mode is toggled.
public record ToyDeviceAdded(ButtplugClientDevice Device) : MessageBase; // for when a device is added.
public record ToyDeviceRemoved(ButtplugClientDevice Device) : MessageBase; // for when a device is removed.
public record ButtplugClientDisconnected : MessageBase; // for when the buttplug client disconnects.
public record ToyboxActiveDeviceChangedMessage(int DeviceIndex) : MessageBase;
public record PlaybackStateToggled(Guid PatternId, NewState NewState) : MessageBase; // for when a pattern is activated.
public record PatternRemovedMessage(Guid PatternId) : MessageBase; // for when a pattern is removed.
public record TriggersModifiedMessage : MessageBase;
public record ExecuteHealthPercentTriggerMessage(HealthPercentTrigger Trigger) : MessageBase;
public record PlayerLatestActiveItems(UserData User, CharaActiveGags GagsInfo, CharaActiveRestrictions RestrictionsInfo, CharaActiveRestraint RestraintInfo) : MessageBase;
