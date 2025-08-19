using GagSpeak.Kinksters;
using GagSpeak.Kinksters.Handlers;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;

namespace GagSpeak.Services.Mediator;

// Client Player or Player Data 
public record PairWentOnlineMessage(UserData UserData) : MessageBase; // a message indicating a pair has gone online.
public record PairHandlerVisibleMessage(PairHandler Player) : MessageBase; // a message indicating the visibility of a pair handler.
public record PairWasRemovedMessage(UserData UserData) : MessageBase; // a message indicating a pair has been removed.
public record TargetPairMessage(Kinkster Pair) : MessageBase; // called when publishing a targeted pair connection (see UI)
public record CreateCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase;
public record ClearCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase; // called when we should clear a GameObject from cache creation service.
public record MufflerLanguageChanged : MessageBase; // called whenever the client language changes to a new language.
public record HcStateCacheChanged : MessageBase;

// technically not needed since we can monitor this externally as a background service!
public record HardcoreStateExpired(HcAttribute Attribute) : SameThreadMessage;


// Kinkster Data Changes
public record ActiveGagsChangeMessage(DataUpdateType UpdateType, int Layer, ActiveGagSlot NewData) : SameThreadMessage;
public record ActiveRestrictionsChangeMessage(DataUpdateType UpdateType, int Layer, ActiveRestriction NewData) : SameThreadMessage;
public record ActiveRestraintChangedMessage(DataUpdateType UpdateType, CharaActiveRestraint NewData) : SameThreadMessage;
public record ActiveCollarChangedMessage(DataUpdateType UpdateType, CharaActiveCollar NewData) : SameThreadMessage;
public record AliasGlobalUpdateMessage(Guid AliasId, AliasTrigger? NewData) : SameThreadMessage;
public record AliasPairUpdateMessage(UserData IntendedUser, Guid AliasId, AliasTrigger? NewData) : SameThreadMessage;
public record ValidToysChangedMessage(List<ToyBrandName> ValidToys) : SameThreadMessage;
public record ActivePatternChangedMessage(DataUpdateType UpdateType, Guid NewActivePattern) : SameThreadMessage;
public record ActiveAlarmsChangedMessage(DataUpdateType UpdateType, List<Guid> ActiveAlarms, Guid ChangedItem) : SameThreadMessage;
public record ActiveTriggersChangedMessage(DataUpdateType UpdateType, List<Guid> ActiveTriggers, Guid ChangedItem) : SameThreadMessage;

public record GameObjectHandlerCreatedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;
public record GameObjectHandlerDestroyedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;



