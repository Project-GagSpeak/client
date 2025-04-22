using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;

namespace GagSpeak.Services.Mediator;

// Client Player or Player Data 
public record UpdateAllOnlineWithCompositeMessage : MessageBase; // for updating all online pairs with composite data.
public record PairWentOnlineMessage(UserData UserData) : MessageBase; // a message indicating a pair has gone online.
public record PairHandlerVisibleMessage(PairHandler Player) : MessageBase; // a message indicating the visibility of a pair handler.
public record PairWasRemovedMessage(UserData UserData) : MessageBase; // a message indicating a pair has been removed.
public record TargetPairMessage(Pair Pair) : MessageBase; // called when publishing a targeted pair connection (see UI)
public record CreateCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase;
public record ClearCacheForObjectMessage(GameObjectHandler ObjectToCreateFor) : MessageBase; // called when we should clear a GameObject from cache creation service.
public record MufflerLanguageChanged : MessageBase; // called whenever the client language changes to a new language.
public record AppearanceImpactingSettingChanged : MessageBase; // called whenever an appearance impacting setting is changed.
public record HardcoreActionMessage(InteractionType type, NewState State) : MessageBase;
public record HardcoreRemoveBlindfoldMessage : MessageBase;
public record PiShockExecuteOperation(string shareCode, int OpCode, int Intensity, int Duration) : MessageBase;


// Kinkster Data Changes
public record IpcDataChangedMessage(DataUpdateType UpdateType, CharaIPCData NewIpcData) : SameThreadMessage;
public record GagDataChangedMessage(DataUpdateType UpdateType, int Layer, ActiveGagSlot NewData) : SameThreadMessage;
public record RestrictionDataChangedMessage(DataUpdateType UpdateType, int Layer, ActiveRestriction NewData) : SameThreadMessage;
public record RestraintDataChangedMessage(DataUpdateType UpdateType, CharaActiveRestraint NewData) : SameThreadMessage;
public record OrdersDataChangedMessage(DataUpdateType UpdateType) : SameThreadMessage;
public record AliasDataChangedMessage(DataUpdateType UpdateType, UserData IntendedUser, CharaAliasData NewData) : SameThreadMessage;
public record ToyboxDataChangedMessage(DataUpdateType UpdateType, CharaToyboxData NewData, Guid InteractionId) : SameThreadMessage;
public record LightStorageDataChangedMessage(CharaLightStorageData CharacterStorageData) : SameThreadMessage;
public record GameObjectHandlerCreatedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;
public record GameObjectHandlerDestroyedMessage(GameObjectHandler GameObjectHandler, bool OwnedObject) : MessageBase;



