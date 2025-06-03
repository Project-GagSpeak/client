using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.ChatMessages;
using GagSpeak.Services.Events;
using GagspeakAPI.Dto;
using GagspeakAPI.Network;

namespace GagSpeak.Services.Mediator;

/// <summary> Every time we need to compose a message for the notification message, this is fired. </summary>
/// <param name="Title"> The title of the notification. </param>
/// <param name="Message"> The message of the notification. </param>
/// <param name="Type"> INFO, WARNING, or ERROR? </param>
/// <param name="TimeShownOnScreen"> How long it is displayed for. </param>
public record NotificationMessage(string Title, string Message, NotificationType Type, TimeSpan? TimeShownOnScreen = null) : MessageBase;

/// <summary> Used to tell the Notification Manager to print out a chat message. </summary>
/// <param name="Message"> The message to be displayed. </param>
/// <param name="Type"> INFO, WARNING, or ERROR? </param>
public record NotifyChatMessage(SeString Message, NotificationType Type) : MessageBase;

/// <summary> Sends to the ActionNotifier that an interaction event occurred. </summary>
/// <param name="Event"> The event that was triggered. </param>
public record EventMessage(InteractionEvent Event) : MessageBase;

/// <summary> Fires whenever the client is disconnected from the GagSpeak Hub. </summary>
public record MainHubDisconnectedMessage : SameThreadMessage;

/// <summary> Fires whenever the client is attempting to reconnect to the GagSpeak Hub. </summary>
public record MainHubReconnectingMessage(Exception? Exception) : SameThreadMessage;

/// <summary> Fires whenever the client has reconnected to the GagSpeak Hub. </summary>
public record MainHubReconnectedMessage(string? Arg) : SameThreadMessage;

/// <summary> Fires whenever the GagSpeak Hub closes. </summary>
public record MainHubClosedMessage(Exception? Exception) : SameThreadMessage;

/// <summary> Fires whenever the client has connected to the GagSpeak Hub. </summary>
public record MainHubConnectedMessage : MessageBase;

/// <summary> Fires once all of our paired Kinkster Data have been loaded into our PairManager. </summary>
public record OnlinePairsLoadedMessage : MessageBase;

/// <summary> Contains the message content of a Global Chat message. </summary>
public record GlobalChatMessage(ChatMessageGlobal Message, bool FromSelf) : MessageBase;

/// <summary> Informs other places in GagSpeak about each important chat message fired. </summary>
/// <param name="Channel"> The channel the message was sent to. </param>
/// <param name="Message"> The message that was sent. </param>
/// <remarks> This is mostly for death-roll detection. </remarks>
public record ClientSentChat(ChatChannel.Channels Channel, string Message) : MessageBase;

/// <summary> Fires once we trigger the safeword command. </summary>
/// <param name="UID"> The UID of the user we want to safeword for. </param>
public record SafewordUsedMessage(string UID = "") : MessageBase;

/// <summary> Fires once we trigger the hardcore safeword command. </summary>
/// <param name="UID"> The UID of the user we want to safeword for. </param>
public record SafewordHardcoreUsedMessage(string UID = "") : MessageBase;

// Whenever we removed a vfx actor from the scene.
// NOTE: This should likely be removed, and handled with a vfxManager class instead.
public record VfxActorRemoved(IntPtr data) : MessageBase;

