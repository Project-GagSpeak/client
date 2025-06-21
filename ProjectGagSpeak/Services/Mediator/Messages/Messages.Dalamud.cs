namespace GagSpeak.Services.Mediator;

/// <summary> Invoked upon Client Player Login. </summary>
public record DalamudLoginMessage : MessageBase;

/// <summary> Every Game Framework Update, this fires. </summary>
public record FrameworkUpdateMessage : SameThreadMessage;

/// <summary> Every Second, on the next Framework Update, this fires. </summary>
public record DelayedFrameworkUpdateMessage : SameThreadMessage;

public record GPoseStartMessage : MessageBase;

public record GPoseEndMessage : MessageBase;

public record CutsceneBeginMessage : MessageBase;

public record CutsceneSkippedMessage : MessageBase;

public record ClientPlayerInCutscene : MessageBase;

public record CutsceneEndMessage : MessageBase;

/// <summary> Once the Client Player begins changing Zones. </summary>
/// <param name="prevZone"> the ID of the zone we are leaving. </param>
public record ZoneSwitchStartMessage(uint prevZone) : MessageBase;

/// <summary> Once the Client Player has finished changing Zones. </summary>
public record ZoneSwitchEndMessage : MessageBase;

/// <summary> Fires whenever the Client Player's Commendation count increases by any amount after swapping zones. </summary>
public record CommendationsIncreasedMessage(int amount) : MessageBase;

