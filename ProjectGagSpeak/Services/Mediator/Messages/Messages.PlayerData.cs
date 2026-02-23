using GagSpeak.Kinksters;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

// Client Player or Player 
public record KinksterOnline(Kinkster Kinkster) : MessageBase; // Revise
public record KinksterOffline(Kinkster Kinkster) : MessageBase;
public record KinksterPlayerRendered(KinksterHandler Handler, Kinkster Kinkster) : SameThreadMessage; // Effectively "becoming visible"
public record KinksterPlayerUnrendered(IntPtr Address) : SameThreadMessage; // Effectively "becoming invisible"

// Maybe remove this down the line.
public record KinksterActiveGagsChanged(Kinkster Kinkster) : SameThreadMessage; // when the active gags of a kinkster change.

public record KinksterRemovedMessage(UserData UserData) : MessageBase; // a message indicating a pair has been removed.

public record TargetKinksterMessage(Kinkster Kinkster) : MessageBase; // called when publishing a targeted pair connection (see UI)

// Object Management
public record WatchedObjectCreated(IntPtr Address) : SameThreadMessage;
public record WatchedObjectDestroyed(IntPtr Address) : SameThreadMessage;

// Action spesific mediator calls
public record MufflerLanguageChanged : MessageBase;
public record HcStateCacheChanged : MessageBase;
public record NameplateClientChanged : MessageBase;



