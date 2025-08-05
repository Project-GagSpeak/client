using Buttplug.Client;
using GagSpeak.Gui.Remote;
using GagSpeak.State.Models;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

#pragma warning disable MA0048, S2094
public record RemoteSelectedKeyChanged(UserPlotedDevices NewSelectedItem) : MessageBase;
public record PlayerLatestActiveItems(UserData User, CharaActiveGags GagsInfo, CharaActiveRestrictions RestrictionsInfo, CharaActiveRestraint RestraintInfo) : MessageBase;
