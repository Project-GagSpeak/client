using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagspeakAPI.User;

namespace GagSpeak.Services.Mediator;

/// <summary>
///   Whenever a NON-CLIENT OWNED OBJECT is created. Intended for Kinksters.
/// </summary>
public record WatchedObjectCreated(IntPtr Address, CharacterInfo Info) : SameThreadMessage;

/// <summary>
///   Whenever a NON-CLIENT OWNED OBJECT is destroyed. Intended for Kinksters.
/// </summary>
public record WatchedObjectDestroyed(IntPtr Address, CharacterInfo Info, bool WasClientActor, UserData? User) : SameThreadMessage;

/// <summary>
///   Whenever a GPose Actor is created.
/// </summary>
public record GPoseObjectCreated(IntPtr Address, CharacterInfo Info) : SameThreadMessage;

/// <summary>
///   Whenever a GPose Actor is destroyed.
/// </summary>
public record GPoseObjectDestroyed(IntPtr Address, Character DataSnapshot, UserData? User) : SameThreadMessage;



