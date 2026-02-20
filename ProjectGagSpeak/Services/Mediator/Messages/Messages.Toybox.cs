using Buttplug.Client;
using GagSpeak.Gui.Remote;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

// Intiface IPC
public record BuzzToyAdded(ButtplugClientDevice Device) : MessageBase;
public record BuzzToyRemoved(ButtplugClientDevice Device) : MessageBase;
public record DeviceScanFinished : MessageBase; // Unsure how much this is actually needed?
public record IntifaceClientConnected : MessageBase;
public record IntifaceClientDisconnected : MessageBase;

public record RemoteSelectedKeyChanged(UserPlotedDevices NewSelectedItem) : MessageBase;
public record PlayerLatestActiveItems(UserData User, CharaActiveGags GagsInfo, CharaActiveRestrictions RestrictionsInfo, CharaActiveRestraint RestraintInfo) : MessageBase;
