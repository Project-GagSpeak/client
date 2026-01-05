using Dalamud.Interface.ImGuiNotification;
using GagSpeak.GameInternals;
using GagSpeak.Kinksters;
using GagSpeak.Services.Events;
using GagspeakAPI.Data;
using GagspeakAPI.Network;

namespace GagSpeak.Services.Mediator;

public enum GlobalChatMsgSource
{
    MainUi,
    Popout,
}

/// <summary> Every time we need to compose a message for the notification message, this is fired. </summary>
/// <param name="Title"> The title of the notification. </param>
/// <param name="Message"> The message of the notification. </param>
/// <param name="Type"> INFO, WARNING, or ERROR? </param>
/// <param name="TimeShownOnScreen"> How long it is displayed for. </param>
public record NotificationMessage(string Title, string Message, NotificationType Type, TimeSpan? TimeShownOnScreen = null) : MessageBase;

/// <summary> Sends to the ActionNotifier that an interaction event occurred. </summary>
/// <param name="Event"> The event that was triggered. </param>
public record EventMessage(InteractionEvent Event) : MessageBase;



/// <summary> Fires whenever the client is disconnected from the GagSpeak Hub. </summary>
public record DisconnectedMessage(DisconnectIntent Intent) : SameThreadMessage;

/// <summary> Fires whenever the client is attempting to reconnect to the GagSpeak Hub. </summary>
public record ReconnectingMessage(Exception? Exception) : SameThreadMessage;

/// <summary> Fires whenever the client has reconnected to the GagSpeak Hub. </summary>
public record ReconnectedMessage(string? Arg) : SameThreadMessage;

/// <summary> Fires whenever the GagSpeak Hub closes. </summary>
public record HubClosedMessage(Exception? Exception) : SameThreadMessage;

/// <summary> Fires whenever the client has connected to the GagSpeak Hub. </summary>
public record ConnectedMessage : MessageBase;

/// <summary> Fired once all personal data related to sharehubs and invites are received after connection. </summary>
public record ConnectedDataSyncedMessage(LobbyAndHubInfoResponse Info) : MessageBase;


/// <summary> When we want to send off our current Achievement Data. </summary>
public record SendAchievementData : MessageBase;

/// <summary> When we want to update the total achievement count. </summary>
public record UpdateCompletedAchievements: MessageBase;

/// <summary> Contains the message content of a Global Chat message. </summary>
public record GlobalChatMessage(ChatMessageGlobal Message, bool FromSelf) : MessageBase;

/// <summary> Notifies you that a Kinkster in the VibeRoom has sent a message. </summary>
/// <param name="User"> The Kinkster that sent the message. </param>
public record VibeRoomChatMessage(UserData Kinkster, string Message) : MessageBase;

/// <summary> Contains the message content of a Chatbox message. </summary>
public record ChatboxMessageFromSelf(InputChannel channel, string message) : MessageBase;

/// <summary> Contains the message content of a Chatbox message. </summary>
public record ChatboxMessageFromKinkster(Kinkster kinkster, InputChannel channel, string message) : MessageBase;
