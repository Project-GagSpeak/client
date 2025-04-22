namespace GagSpeak.Services.Mediator;

/// <summary> Invoked upon Client Player Login. </summary>
public record DalamudLoginMessage : MessageBase;

/// <summary> Invoked upon Client Player Logout. </summary>
/// <param name="type"> The type of logout. </param>
/// <param name="code"> The code of logout. </param>
public record DalamudLogoutMessage(int type, int code) : MessageBase;

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
public record ZoneSwitchStartMessage(ushort prevZone) : MessageBase;

/// <summary> Once the Client Player has finished changing Zones. </summary>
public record ZoneSwitchEndMessage : MessageBase;

/// <summary> Once the Client Player has changed jobs. </summary>
public record JobChangeMessage(uint jobId) : MessageBase;

/// <summary> Fires whenever the Client Player's Commendation count increases by any amount after swapping zones. </summary>
public record CommendationsIncreasedMessage(int amount) : MessageBase;

