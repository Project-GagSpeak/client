using Dalamud.Game.ClientState.Objects.Enums;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using Penumbra.String;

namespace Penumbra.GameData.Actors;

/// <summary> Extension methods to convert an actor identifier to and from JSON. </summary>
public static class ActorIdentifierJson
{
    /// <summary> Write an actor identifier to a new JObject. </summary>
    public static JObject ToJson(this ActorIdentifier identifier)
    {
        var ret = new JObject { { nameof(identifier.Type), identifier.Type.ToString() } };
        switch (identifier.Type)
        {
            case IdentifierType.Player:
                ret.Add(nameof(identifier.PlayerName), identifier.PlayerName.ToString());
                ret.Add(nameof(identifier.HomeWorld),  identifier.HomeWorld.Id);
                return ret;
            case IdentifierType.Retainer:
                ret.Add(nameof(identifier.PlayerName), identifier.PlayerName.ToString());
                ret.Add(nameof(identifier.Retainer),   identifier.Retainer.ToString());
                return ret;
            case IdentifierType.Owned:
                ret.Add(nameof(identifier.PlayerName), identifier.PlayerName.ToString());
                ret.Add(nameof(identifier.HomeWorld),  identifier.HomeWorld.Id);
                ret.Add(nameof(identifier.Kind),       identifier.Kind.ToString());
                ret.Add(nameof(identifier.DataId),     identifier.DataId.Id);
                return ret;
            case IdentifierType.Special:
                ret.Add("Special", ((ScreenActor)identifier.Index.Index).ToString());
                return ret;
            case IdentifierType.Npc:
                ret.Add(nameof(identifier.Kind), identifier.Kind.ToString());
                if (identifier.Index.Index != ushort.MaxValue)
                    ret.Add(nameof(identifier.Index), identifier.Index.Index);
                ret.Add(nameof(identifier.DataId), identifier.DataId.Id);
                return ret;
            case IdentifierType.UnkObject:
                ret.Add(nameof(identifier.PlayerName), identifier.PlayerName.ToString());
                ret.Add(nameof(identifier.Index),      identifier.Index.Index);
                return ret;
        }

        return ret;
    }

    /// <summary>
    /// Try to create an ActorIdentifier from an already parsed JObject <paramref name="data"/>.
    /// </summary>
    /// <param name="actorManager"> The actor manager using it. </param>
    /// <param name="data">A parsed JObject</param>
    /// <returns>ActorIdentifier.Invalid if the JObject can not be converted, a valid ActorIdentifier otherwise.</returns>
    public static ActorIdentifier FromJson(this ActorIdentifierFactory actorManager, JObject? data)
    {
        if (data == null)
            return ActorIdentifier.Invalid;

        var type = data[nameof(ActorIdentifier.Type)]?.ToObject<IdentifierType>() ?? IdentifierType.Invalid;
        switch (type)
        {
            case IdentifierType.Player:
            {
                var name      = ByteString.FromStringUnsafe(data[nameof(ActorIdentifier.PlayerName)]?.ToObject<string>(), false);
                var homeWorld = data[nameof(ActorIdentifier.HomeWorld)]?.ToObject<ushort>() ?? 0;
                return actorManager.CreatePlayer(name, homeWorld);
            }
            case IdentifierType.Retainer:
            {
                var name = ByteString.FromStringUnsafe(data[nameof(ActorIdentifier.PlayerName)]?.ToObject<string>(), false);
                var retainerType = data[nameof(ActorIdentifier.Retainer)]?.ToObject<ActorIdentifier.RetainerType>()
                 ?? ActorIdentifier.RetainerType.Both;
                return actorManager.CreateRetainer(name, retainerType);
            }
            case IdentifierType.Owned:
            {
                var name      = ByteString.FromStringUnsafe(data[nameof(ActorIdentifier.PlayerName)]?.ToObject<string>(), false);
                var homeWorld = data[nameof(ActorIdentifier.HomeWorld)]?.ToObject<ushort>() ?? 0;
                var kind      = data[nameof(ActorIdentifier.Kind)]?.ToObject<ObjectKind>() ?? ObjectKind.CardStand;
                var dataId    = data[nameof(ActorIdentifier.DataId)]?.ToObject<uint>() ?? 0;
                return actorManager.CreateOwned(name, homeWorld, kind, dataId);
            }
            case IdentifierType.Special:
            {
                var special = data["Special"]?.ToObject<ScreenActor>() ?? 0;
                return actorManager.CreateSpecial(special);
            }
            case IdentifierType.Npc:
            {
                var index  = data[nameof(ActorIdentifier.Index)]?.ToObject<ushort>() ?? ushort.MaxValue;
                var kind   = data[nameof(ActorIdentifier.Kind)]?.ToObject<ObjectKind>() ?? ObjectKind.CardStand;
                var dataId = data[nameof(ActorIdentifier.DataId)]?.ToObject<uint>() ?? 0;
                return actorManager.CreateNpc(kind, dataId, index);
            }
            case IdentifierType.UnkObject:
            {
                var index = data[nameof(ActorIdentifier.Index)]?.ToObject<ushort>() ?? ushort.MaxValue;
                var name  = ByteString.FromStringUnsafe(data[nameof(ActorIdentifier.PlayerName)]?.ToObject<string>(), false);
                return actorManager.CreateIndividualUnchecked(IdentifierType.UnkObject, name, index, ObjectKind.None, 0);
            }
            default: return ActorIdentifier.Invalid;
        }
    }
}
