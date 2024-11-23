using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using Penumbra.String;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace Penumbra.GameData.Actors;

/// <summary> Handles resolving specific situations and actors to actor identifiers. </summary>
internal sealed unsafe class ActorResolver(IGameGui _gameGui, ObjectManager _objects, IClientState _clientState)
{
    /// <summary> Obtain an identifier for the current player. </summary>
    public ActorIdentifier GetCurrentPlayer(ActorIdentifierFactory factory)
    {
        var address = _objects[0];
        return !address.Valid
            ? ActorIdentifier.Invalid
            : factory.CreateIndividualUnchecked(IdentifierType.Player, address.Utf8Name, address.AsCharacter->HomeWorld,
                ObjectKind.None, uint.MaxValue);
    }

    /// <summary> Obtain an identifier for the currently inspected player. </summary>
    public ActorIdentifier GetInspectPlayer(ActorIdentifierFactory factory)
    {
        var addon = _gameGui.GetAddonByName("CharacterInspect");
        return addon == nint.Zero
            ? ActorIdentifier.Invalid
            : factory.CreatePlayer(InspectName, InspectWorldId);
    }

    /// <summary> Obtain an identifier for player banner actors. </summary>
    /// <param name="factory"></param>
    /// <param name="type"> The screen actor to check. </param>
    /// <param name="id"> The returned identifier. </param>
    /// <returns> Whether a party banner is open, regardless of the actors' existence. </returns>
    public bool ResolvePartyBannerPlayer(ActorIdentifierFactory factory, ScreenActor type, out ActorIdentifier id)
    {
        id = ActorIdentifier.Invalid;
        var module = Framework.Instance()->GetUIModule()->GetAgentModule();
        if (module == null)
            return false;

        var agent = (AgentBannerInterface*)module->GetAgentByInternalId(AgentId.BannerParty);
        if (agent == null || !agent->AgentInterface.IsAgentActive())
            agent = (AgentBannerInterface*)module->GetAgentByInternalId(AgentId.BannerMIP);
        if (agent == null || !agent->AgentInterface.IsAgentActive())
            return false;

        var idx = (ushort)type - (ushort)ScreenActor.CharacterScreen;
        if (agent->Data == null)
            return true;

        ref var character = ref agent->Data->Characters[idx];

        var name = new ByteString(character.Name1.StringPtr);
        id = factory.CreatePlayer(name, (WorldId)character.WorldId);
        return true;
    }

    /// <summary> Obtain an identifier for mahjong portrait actors. </summary>
    /// <param name="factory"></param>
    /// <param name="type"> The screen actor to check. </param>
    /// <param name="id"> The returned identifier. </param>
    /// <returns> Whether mahjong is open. </returns>
    public bool ResolveMahjongPlayer(ActorIdentifierFactory factory, ScreenActor type, out ActorIdentifier id)
    {
        id = ActorIdentifier.Invalid;
        if (_clientState.TerritoryType != 831 && _gameGui.GetAddonByName("EmjIntro") == nint.Zero)
            return false;

        var obj = _objects[(int)type];
        if (!obj.Valid)
            return false;

        id = type switch
        {
            ScreenActor.CharacterScreen => GetCurrentPlayer(factory),
            ScreenActor.ExamineScreen   => SearchPlayersCustomize(factory, obj, 2, 4, 6),
            ScreenActor.FittingRoom     => SearchPlayersCustomize(factory, obj, 4, 2, 6),
            ScreenActor.DyePreview      => SearchPlayersCustomize(factory, obj, 6, 2, 4),
            _                           => ActorIdentifier.Invalid,
        };
        return true;
    }

    /// <summary> Obtain an identifier for pvp banner actors. </summary>
    /// <param name="factory"></param>
    /// <param name="type"> The screen actor to check. </param>
    /// <param name="id"> The returned identifier. </param>
    /// <returns> Whether a pvp banner is open, regardless of the actors' existence. </returns>
    public bool ResolvePvPBannerPlayer(ActorIdentifierFactory factory, ScreenActor type, out ActorIdentifier id)
    {
        id = ActorIdentifier.Invalid;
        if (!_clientState.IsPvPExcludingDen)
            return false;

        var addon = (AtkUnitBase*)_gameGui.GetAddonByName("PvPMap");
        if (addon == null || addon->IsVisible)
            return false;

        var obj = _objects[(int)type];
        if (!obj.Valid)
            return false;

        id = type switch
        {
            ScreenActor.CharacterScreen => SearchPlayersCustomize(factory, obj),
            ScreenActor.ExamineScreen   => SearchPlayersCustomize(factory, obj),
            ScreenActor.FittingRoom     => SearchPlayersCustomize(factory, obj),
            ScreenActor.DyePreview      => SearchPlayersCustomize(factory, obj),
            ScreenActor.Portrait        => SearchPlayersCustomize(factory, obj),
            _                           => ActorIdentifier.Invalid,
        };
        return true;
    }

    /// <summary> Obtain an identifier for the current card if it is open. </summary>
    public ActorIdentifier GetCardPlayer(ActorIdentifierFactory factory)
    {
        var agent = AgentCharaCard.Instance();
        if (agent == null || agent->Data == null)
            return ActorIdentifier.Invalid;

        return factory.CreatePlayer(new ByteString(agent->Data->Name.StringPtr), agent->Data->WorldId);
    }

    /// <summary> Obtain an identifier for glamour interface if it is open. </summary>
    public ActorIdentifier GetGlamourPlayer(ActorIdentifierFactory factory)
    {
        var addon = _gameGui.GetAddonByName("MiragePrismMiragePlate");
        return addon == nint.Zero ? ActorIdentifier.Invalid : GetCurrentPlayer(factory);
    }

    /// <summary> The home world of the currently inspected player. </summary>
    private static ushort InspectWorldId
        => (ushort)UIState.Instance()->Inspect.WorldId;

    /// <summary> The name of the currently inspected player. </summary>
    private static ByteString InspectName
        => new(UIState.Instance()->Inspect.Name);

    /// <summary> Check if a screen actor at a given index has the same customizations (up to height) as the given character, and return an identifier for the potential owner. </summary>
    private bool SearchPlayerCustomize(ActorIdentifierFactory factory, Actor character, ObjectIndex idx, out ActorIdentifier id)
    {
        var other = _objects[idx];
        if (!other.Valid || !CustomizeArray.ScreenActorEquals(character.Customize, other.Customize))
        {
            id = ActorIdentifier.Invalid;
            return false;
        }

        id = factory.FromObject(other, out _, false, true, false);
        return true;
    }

    /// <summary> Check if one of the three indices has the same customizations. Used for Mahjong Banners. </summary>
    private ActorIdentifier SearchPlayersCustomize(ActorIdentifierFactory factory, Actor gameObject, ObjectIndex idx1, ObjectIndex idx2,
        ObjectIndex idx3)
        => SearchPlayerCustomize(factory,  gameObject, idx1, out var ret)
         || SearchPlayerCustomize(factory, gameObject, idx2, out ret)
         || SearchPlayerCustomize(factory, gameObject, idx3, out ret)
                ? ret
                : ActorIdentifier.Invalid;

    /// <summary> Check if a cutscene actor has the same customizations. Used for PvP Banners. </summary>
    private ActorIdentifier SearchPlayersCustomize(ActorIdentifierFactory factory, Actor gameObject)
    {
        for (var i = 0; i < ObjectIndex.CutsceneStart.Index; i += 2)
        {
            var obj = _objects[i];
            if (obj.IsPlayer && Compare(gameObject, obj))
                return factory.FromObject(obj, out _, false, true, false);
        }

        return ActorIdentifier.Invalid;

        static bool Compare(Actor a, Actor b)
        {
            var data1  = (CustomizeArray*)&a.AsCharacter->DrawData.CustomizeData;
            var data2  = (CustomizeArray*)&b.AsCharacter->DrawData.CustomizeData;
            var equals = CustomizeArray.ScreenActorEquals(data1, data2);
            return equals;
        }
    }
}
