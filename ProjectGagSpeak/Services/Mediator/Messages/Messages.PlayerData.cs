using GagSpeak.Kinksters;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Mediator;

// Client Player or Player 
public record KinksterOnline(Kinkster Kinkster) : MessageBase; // Revise
public record KinksterOffline(Kinkster Kinkster) : MessageBase;
public record KinksterPlayerRendered(KinksterHandler Handler, Kinkster Kinkster) : SameThreadMessage; // Effectively "becoming visible"
public record KinksterPlayerUnrendered(IntPtr Address) : SameThreadMessage; // Effectively "becoming invisible"
public record KinksterActiveGagsChanged(Kinkster Kinkster) : SameThreadMessage; // when the active gags of a kinkster change.

public record KinksterRemovedMessage(UserData UserData) : MessageBase; // a message indicating a pair has been removed.
public record TargetKinksterMessage(Kinkster Kinkster) : MessageBase; // called when publishing a targeted pair connection (see UI)

public record WatchedObjectCreated(IntPtr Address) : SameThreadMessage;
public record WatchedObjectDestroyed(IntPtr Address) : SameThreadMessage;

public record MufflerLanguageChanged : MessageBase;
public record HcStateCacheChanged : MessageBase;
public record NameplateClientChanged : MessageBase;

// Kinkster Data Changes
public record AliasGlobalUpdateMessage(Guid AliasId, AliasTrigger? NewData) : SameThreadMessage;
public record AliasPairUpdateMessage(UserData IntendedUser, Guid AliasId, AliasTrigger? NewData) : SameThreadMessage;
public record ValidToysChangedMessage(List<ToyBrandName> ValidToys) : SameThreadMessage;
public record ActivePatternChangedMessage(DataUpdateType UpdateType, Guid NewActivePattern) : SameThreadMessage;
public record ActiveAlarmsChangedMessage(DataUpdateType UpdateType, List<Guid> ActiveAlarms, Guid ChangedItem) : SameThreadMessage;
public record ActiveTriggersChangedMessage(DataUpdateType UpdateType, List<Guid> ActiveTriggers, Guid ChangedItem) : SameThreadMessage;



