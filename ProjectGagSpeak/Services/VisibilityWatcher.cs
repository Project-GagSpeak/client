using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.Watchers;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.User;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Services;

public readonly record struct CharacterInfo(nint Address, ulong CID, string HashedCID, uint EntityId);

// Watcher that manages the visibility state of actors.
// Used to identify visible states of potentially paired or unpaired UserDatas / Actors.
public unsafe sealed class VisibilityWatcher : DisposableMediatorSubscriberBase
{  
    // Stores the struct information for a rendered character between indexes 0-199.
    private static readonly Dictionary<nint, CharacterInfo> _renderedCharas = [];

    // Stores the struct information for the rendered actors in gpose from 201-439.
    // Note that these are all 'copy actors' and do not have a ref to their base actors.
    // The only way to know whose actor they are is via penumbras GetCutsceneParentIndex func,
    // or via tracking it ourselves in a gpose manager.
    private static readonly Dictionary<nint, CharacterInfo> _gPoseActors = [];

    // Utility function to go from a hashedIdent to an address. Useful for onlineUser lookups.
    private static readonly Dictionary<string, nint> _hashedIdentToAddr = [];

    // Stores the lookup table from an address to a linked online user.
    // Can be possibly paired with ownedObject to get direct pairing.
    // Also useful for things such as the ClientUpdateDistributor.
    private readonly Dictionary<nint, UserData> _visibleUsers = [];

    // Used for ClientDataDistribution (which we can refactor later)
    // Users that just became visible since the latest update push, and need full data.
    private readonly HashSet<UserData> _newlyVisible = new(UserDataComparer.Instance);

    public VisibilityWatcher(ILogger<VisibilityWatcher> logger, GagspeakMediator mediator)
        : base (logger, mediator)
    {
        Mediator.Subscribe<ConnectedMessage>(this, _ => _newlyVisible.UnionWith(_visibleUsers.Values));
    }

    internal static IReadOnlyDictionary<nint, CharacterInfo> Rendered => _renderedCharas;
    internal static IReadOnlyDictionary<nint, CharacterInfo> GPoseRendered => _gPoseActors;
    internal static IReadOnlyDictionary<string, nint> HashedIdentLookup => _hashedIdentToAddr;
    public IReadOnlyDictionary<nint, UserData> VisibleUsers => _visibleUsers;
    public IReadOnlySet<UserData> NewlyVisible => _newlyVisible;

    // Simply adds a user to the visible users dictionary.
    public void AddVisibleUser(UserData user, nint addr)
    {
        if (_visibleUsers.TryAdd(addr, user))
        {
            Logger.LogTrace($"AddVisibleUser New: {user.AliasOrUID} - {addr:X}", LoggerType.VisiblePairs);
            Mediator.Publish(new HandledUserRendered(user, addr));
        }
    }

    public void AddVisibleHandledUser(UserData user, nint addr, bool forceNew = false)
    {
        var addrAdded = _visibleUsers.TryAdd(addr, user);
        if (addrAdded)
        {
            Logger.LogTrace($"New Visible User: {user.AnonName} - {addr:X}", LoggerType.VisiblePairs);
            Mediator.Publish(new HandledUserRendered(user, addr));
        }

        // Append to newly visible if forced, or added. (for DataDistribution)
        if (forceNew || addrAdded)
        {
            Logger.LogDebug($"NewlyVisible Added (Forced={forceNew}) : {user.AnonName} - {addr:X}", LoggerType.VisiblePairs);
            _newlyVisible.Add(user);
        }
    }

    public bool RemoveVisibleUser(nint addr, [NotNullWhen(true)] out UserData? removed)
    {
        if (!_visibleUsers.Remove(addr, out removed))
            return false;
        // Can always use the OUT from the REMOVE if this gets ambiguous.
        _newlyVisible.Remove(removed);
        return true;
    }

    public void ClearNewlyVisible()
        => _newlyVisible.Clear();

    public void AddTrackedActor(Character* chara)
    {
        var addr = (nint)chara;

        // Ensure validity to our claim, and ONLY track Player Characters.
        if (!CharaWatcher.Rendered.Contains(addr) || chara->ObjectKind is not ObjectKind.Pc)
            return;
        var hashedCID = GagSpeakSecurity.GetIdentHashByCharacterPtr(addr);
        // Assuming CharacterInfo constructor is updated to remove OwnedKind
        var info = new CharacterInfo(addr, chara->ContentId, hashedCID, chara->EntityId);
        _renderedCharas.TryAdd(addr, info);

        // If the HashedIdent was not empty, append it to the lookup table.
        if (info.HashedCID.Length is not 0)
            _hashedIdentToAddr.TryAdd(info.HashedCID, addr);

        Logger.LogDebug($"New TrackedActor: {addr:X} - {chara->GetName()} | Info: {info}", LoggerType.GameObjects);
        // Assuming WatchedObjectCreated is updated to remove the Parent IntPtr
        Mediator.Publish(new WatchedObjectCreated(addr, info));
    }

    public void RemoveTrackedActor(Character* chara, bool wasClientActor)
    {
        var addr = (nint)chara;
        if (!_renderedCharas.Remove(addr, out var removed))
            return;
        // Remove the lookup and other possible entries.
        _hashedIdentToAddr.Remove(removed.HashedCID);
        RemoveVisibleUser(addr, out var userData);
        Logger.LogDebug($"Removed TrackedActor: {addr:X} - {chara->GetName()}", LoggerType.GameObjects);
        Mediator.Publish(new WatchedObjectDestroyed(addr, removed, wasClientActor, userData));
    }

    public bool TryAddTrackedGPoseActor(Character* chara)
    {
        var addr = (nint)chara;
        // Ensure validity, ensure GPose, and ONLY track Player Characters.
        if (!CharaWatcher.Rendered.Contains(addr) || !GameMain.IsInGPose() || chara->ObjectKind is not ObjectKind.Pc)
            return false;
        if (chara->ObjectIndex is < (ushort)SpecialActorIdx.CutsceneStart or >= (ushort)SpecialActorIdx.CutsceneEnd)
            return false;
        if (_gPoseActors.ContainsKey(addr))
            return false;

        var hashedCID = GagSpeakSecurity.GetIdentHashByCharacterPtr(addr);
        var info = new CharacterInfo(addr, chara->ContentId, hashedCID, chara->EntityId);
        // Append to the GPose actors, do not append to lookup table.
        _gPoseActors.Add(addr, info);
        Logger.LogDebug($"New TrackedGPoseActor: {addr:X} - {chara->NameString}", LoggerType.GameObjects);
        Mediator.Publish(new GPoseObjectCreated(addr, info));
        return true;
    }

    public bool TryRemoveTrackedGPoseActor(Character* chara)
    {
        // Ensure we are in actor range
        if (chara->ObjectIndex is < (ushort)SpecialActorIdx.CutsceneStart or >= (ushort)SpecialActorIdx.CutsceneEnd)
            return false;

        // Return if we successfully remove the GPose actor.
        var addr = (nint)chara;
        if (!_gPoseActors.Remove(addr, out var removed))
            return false;

        // Attempt removal from other sources.
        RemoveVisibleUser(addr, out var userData);
        // Log the removal. No need to remove from the mapping since all GPose players are CopyCharacters.
        Logger.LogDebug($"Tracked GPoseActor Removed: {addr:X} - {chara->NameString}", LoggerType.GameObjects);
        Mediator.Publish(new GPoseObjectDestroyed(addr, *chara, userData));
        return true;
    }
}
