using Buttplug.Client;
using GagSpeak.Gui.Remote;
using GagSpeak.State.Models;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

#pragma warning disable MA0048, S2094
public record RemoteSelectedKeyChanged(UserPlotedDevices NewSelectedItem) : MessageBase;
public record PlaybackStateToggled(Guid PatternId, NewState NewState) : MessageBase; // for when a pattern is activated.
public record PatternRemovedMessage(Guid PatternId) : MessageBase; // for when a pattern is removed.
public record TriggersModifiedMessage : MessageBase;
public record ExecuteHealthPercentTriggerMessage(HealthPercentTrigger Trigger) : MessageBase;
public record PlayerLatestActiveItems(UserData User, CharaActiveGags GagsInfo, CharaActiveRestrictions RestrictionsInfo, CharaActiveRestraint RestraintInfo) : MessageBase;
