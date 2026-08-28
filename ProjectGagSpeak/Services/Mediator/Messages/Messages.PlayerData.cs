using GagSpeak.Kinksters;
using GagspeakAPI.User;

namespace GagSpeak.Services.Mediator;

public record TargetKinksterMessage(Kinkster Kinkster) : MessageBase; // called when publishing a targeted pair connection (see UI)
public record KinksterRemovedMessage(UserData UserData) : MessageBase; // a message indicating a pair has been removed.

// Effectively "becoming visible"
public record HandledUserRendered(UserData User, IntPtr Address) : SameThreadMessage;
// Technically "becoming invisible"
public record KinksterRendered(UserData User, IntPtr Address) : SameThreadMessage;
public record KinksterUnrendered(UserData User, IntPtr Address) : SameThreadMessage;

//public record KinksterRendered(KinksterHandler Handler, Kinkster Kinkster) : SameThreadMessage; // Effectively "becoming visible"
//public record KinksterUnrendered(IntPtr Address) : SameThreadMessage; // Effectively "becoming invisible"

// Maybe remove this down the line.
public record KinksterActiveGagsChanged(Kinkster Kinkster) : SameThreadMessage; // when the active gags of a kinkster change.

// Action spesific mediator calls
public record MufflerLanguageChanged : MessageBase;
public record HcStateCacheChanged : MessageBase;
public record NameplateClientChanged : MessageBase;



