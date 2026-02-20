using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.GameInternals;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

// Calls used by detour detection that does not need immidiate resolution.
// Typically used for things like Trigger detection.

// Could make samethread if we really wanted to. Idk.
public record GameChatMessage(InputChannel Channel, string SenderNameWorld, SeString Msg) : MessageBase;

// For all things emote. Run on SameThreadMessage to ensure pointer validity.
public record EmoteDetected(ushort ID, nint CallerAddr, nint TargetAddr) : SameThreadMessage;

/// Health Percent Triggers. Run on SameThreadMessage for pointer validity.
public record HpMonitorTriggered(nint PlayerAddr, HealthPercentTrigger HpTrigger) : SameThreadMessage;

// Could make a more generic method for Minigames, but for now just per-activity is fine.
public record DeathrollMessage(XivChatType Type, string SenderNameWorld, SeString Msg) : MessageBase;
public record DeathrollResult(string WinnerNameWorld, string LoserNameWorld) : MessageBase;

/// <summary>
///     If <paramref name="State"/> Is <see cref="NewState.Enabled"/> or <see cref="NewState.Locked"/>, Data is current. <para />
///     If <paramref name="State"/> Is <see cref="NewState.Disabled"/> or <see cref="NewState.Unlocked"/>, Data is previous.
/// </summary>
public record GagStateChanged(NewState State, int Layer, ActiveGagSlot Data, string Enactor, string Target) : MessageBase;

/// <summary>
///     If <paramref name="State"/> Is <see cref="NewState.Enabled"/> or <see cref="NewState.Locked"/>, Data is current. <para />
///     If <paramref name="State"/> Is <see cref="NewState.Disabled"/> or <see cref="NewState.Unlocked"/>, Data is previous.
/// </summary>
public record RestrictionStateChanged(NewState State, int Layer, ActiveRestriction Data, string Enactor, string Target) : MessageBase;

/// <summary>
///     If <paramref name="State"/> Is <see cref="NewState.Enabled"/> or <see cref="NewState.Locked"/>, Data is current. <para />
///     If <paramref name="State"/> Is <see cref="NewState.Disabled"/> or <see cref="NewState.Unlocked"/>, Data is previous.
/// </summary>
public record RestraintStateChanged(NewState State, CharaActiveRestraint Data, string Enactor, string Target) : MessageBase;

/// <summary>
///     Informs of an update in where an active restraint set had its layers changed, and what got added and removed.
/// </summary>
public record RestraintLayersChanged(CharaActiveRestraint Data, RestraintLayer Added, RestraintLayer Removed, string Enactor, string Target) : MessageBase;
