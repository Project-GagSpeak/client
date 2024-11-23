using Dalamud.Plugin.Services;
using OtterGui.Services;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace Penumbra.GameData.Actors;

/// <summary> A delegate to resolve a cutscene copy of a character to its owner. </summary>
public delegate short CutsceneResolver(ushort index);

/// <summary> Manage transformation, generation and conversion of actor identifiers. </summary>
public sealed class ActorManager : ActorIdentifierFactory, IDisposable, IAsyncService
{
    /// <summary> The names used for NPC types. </summary>
    public readonly NameDicts Data;

    private readonly ActorResolver _resolver;
    private readonly IClientState  _clientState;
    private          uint          _homeWorld;


    /// <summary> Waits for the NameDicts to be ready. </summary>
    public Task Awaiter
        => Data.Awaiter;

    /// <inheritdoc/>
    public bool Finished
        => Awaiter.IsCompletedSuccessfully;

    /// <summary> Unsets the static manager if it is set to this manager. </summary>
    public void Dispose()
    {
        if (ActorIdentifierExtensions.Manager == this)
            ActorIdentifierExtensions.Manager = null;
        _clientState.Login  -= OnLogin;
        _clientState.Logout -= OnLogout;
    }

    public ActorManager(NameDicts data,
        IFramework framework,
        ObjectManager objects,
        IClientState clientState,
        IGameGui gameGui,
        CutsceneResolver toParentIdx)
        : base(objects, framework, data, toParentIdx)
    {
        Data         = data;
        _clientState = clientState;
        _resolver    = new ActorResolver(gameGui, objects, clientState);
        // Set the static manager if it is unset.
        ActorIdentifierExtensions.Manager ??= this;
        _clientState.Login                +=  OnLogin;
        _clientState.Logout               +=  OnLogout;
        _homeWorld                        =   objects[0].IsCharacter ? objects[0].HomeWorld : (ushort)0;
    }

    private void OnLogin()
        => _homeWorld = _clientState.LocalPlayer?.HomeWorld.RowId ?? _homeWorld;

    private void OnLogout(int type, int code)
        => _homeWorld = 0;

    /// <inheritdoc cref="ActorResolver.GetCurrentPlayer"/>
    public ActorIdentifier GetCurrentPlayer()
        => _resolver.GetCurrentPlayer(this);

    /// <inheritdoc cref="ActorResolver.GetInspectPlayer"/>
    public ActorIdentifier GetInspectPlayer()
        => _resolver.GetInspectPlayer(this);

    /// <inheritdoc cref="ActorResolver.ResolvePartyBannerPlayer"/>
    public bool ResolvePartyBannerPlayer(ScreenActor type, out ActorIdentifier id)
        => _resolver.ResolvePartyBannerPlayer(this, type, out id);

    /// <inheritdoc cref="ActorResolver.ResolveMahjongPlayer"/>
    public bool ResolveMahjongPlayer(ScreenActor type, out ActorIdentifier id)
        => _resolver.ResolveMahjongPlayer(this, type, out id);

    /// <inheritdoc cref="ActorResolver.ResolvePvPBannerPlayer"/>
    public bool ResolvePvPBannerPlayer(ScreenActor type, out ActorIdentifier id)
        => _resolver.ResolvePvPBannerPlayer(this, type, out id);

    /// <inheritdoc cref="ActorResolver.GetCardPlayer"/>
    public ActorIdentifier GetCardPlayer()
        => _resolver.GetCardPlayer(this);

    /// <inheritdoc cref="ActorResolver.GetGlamourPlayer"/>
    public ActorIdentifier GetGlamourPlayer()
        => _resolver.GetGlamourPlayer(this);

    /// <summary> Use stored data to convert an ActorIdentifier to a string with more accurate data. </summary>
    public string ToString(ActorIdentifier id)
    {
        return id.Type switch
        {
            IdentifierType.Player => id.HomeWorld.Id != _homeWorld
                ? $"{id.PlayerName} ({Data.ToWorldName(id.HomeWorld)})"
                : id.PlayerName.ToString(),
            IdentifierType.Retainer => $"{id.PlayerName}{id.Retainer switch
            {
                ActorIdentifier.RetainerType.Bell      => " (Bell)",
                ActorIdentifier.RetainerType.Mannequin => " (Mannequin)",
                _                                      => " (Retainer)",
            }}",
            IdentifierType.Owned => id.HomeWorld.Id != _homeWorld
                ? $"{id.PlayerName} ({Data.ToWorldName(id.HomeWorld)})'s {Data.ToName(id.Kind, id.DataId)}"
                : $"{id.PlayerName}s {Data.ToName(id.Kind,                                     id.DataId)}",
            IdentifierType.Special => ((ScreenActor)id.Index.Index).ToName(),
            IdentifierType.Npc =>
                id.Index == ushort.MaxValue
                    ? Data.ToName(id.Kind, id.DataId)
                    : $"{Data.ToName(id.Kind, id.DataId)} at {id.Index}",
            IdentifierType.UnkObject => id.PlayerName.IsEmpty
                ? $"Unknown Object at {id.Index}"
                : $"{id.PlayerName} at {id.Index}",
            _ => "Invalid",
        };
    }

    /// <summary> Use stored data to convert an ActorIdentifier to a name only. </summary>
    public string ToName(ActorIdentifier id)
    {
        return id.Type switch
        {
            IdentifierType.Player    => id.PlayerName.ToString(),
            IdentifierType.Retainer  => id.PlayerName.ToString(),
            IdentifierType.Owned     => $"{id.PlayerName}s {Data.ToName(id.Kind, id.DataId)}",
            IdentifierType.Special   => ((ScreenActor)id.Index.Index).ToName(),
            IdentifierType.Npc       => Data.ToName(id.Kind, id.DataId),
            IdentifierType.UnkObject => id.PlayerName.IsEmpty ? id.PlayerName.ToString() : "Unknown Object",
            _                        => "Invalid",
        };
    }
}
