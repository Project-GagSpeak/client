using CkCommons;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.Watchers;

namespace GagSpeak.Kinksters;

/// <summary>
///     Handles the Kinksters current visibility state. <para />
///     Responsible for storing details on the linked visible kinkster.
/// </summary>
public sealed class KinksterHandler : DisposableMediatorSubscriberBase
{
    private readonly IpcManager _ipc;
    private readonly CharaObjectWatcher _watcher;

    public Kinkster Kinkster { get; init; } // Self-Parent reference.
    private unsafe Character* _player = null;

    public KinksterHandler(Kinkster kinkster, ILogger<KinksterHandler> logger, 
        GagspeakMediator mediator, IpcManager ipc, CharaObjectWatcher watcher)
        : base(logger, mediator)
    {
        Kinkster = kinkster;
        _ipc = ipc;
        _watcher = watcher;

        Mediator.Subscribe<WatchedObjectCreated>(this, msg => MarkVisibleForAddress(msg.Address));
        Mediator.Subscribe<WatchedObjectDestroyed>(this, msg => UnrenderPlayer(msg.Address));
    }

    // Public Accessors.
    public Character DataState { get { unsafe { return *_player; } } }
    public unsafe IntPtr Address => (nint)_player;
    public unsafe ulong EntityId => _player->EntityId;
    public unsafe ulong GameObjectId => _player->GetGameObjectId().ObjectId;
    public unsafe ushort ObjIndex => _player->ObjectIndex;

    public string NameString { get; private set; } = string.Empty; // Manual, to assist timeout tasks.
    public string NameWithWorld { get; private set; } = string.Empty; // Manual, to assist timeout tasks.
    public unsafe bool IsRendered => _player != null;

    // Initializes Player Rendering for this object if the address matches the OnlineUserIdent.
    // Called by the Watcher's mediator subscriber. Not intended for public access.
    // Assumes the passed in address is a visible Character*
    private void MarkVisibleForAddress(IntPtr address)
    {
        if (!Kinkster.IsOnline || Address != IntPtr.Zero) return; // Already exists or not online.
        if (string.IsNullOrEmpty(Kinkster.Ident)) return; // Must have valid CharaIdent.
        if (Kinkster.Ident != GagSpeakSecurity.GetIdentHashByCharacterPtr(address)) return;

        Logger.LogDebug($"Matched {Kinkster.GetNickAliasOrUid()} to a created object @ [{address:X}]", LoggerType.PairHandlers);
        MarkRenderedInternal(address);
    }

    // Publicly accessible method to try and identify the address of an online user to mark them as visible.
    internal async Task SetVisibleIfRendered()
    {
        if (!Kinkster.IsOnline) return; // Must be online.
        if (string.IsNullOrEmpty(Kinkster.Ident)) return; // Must have valid CharaIdent.
        // If already rendered, reapply alterations and return.
        if (IsRendered)
        {
            Logger.LogDebug($"{NameString}({Kinkster.GetNickAliasOrUid()}) is already rendered, reapplying alterations.", LoggerType.PairHandlers);
            Mediator.Publish(new KinksterRendered(this, Kinkster));
            Mediator.Publish(new FolderUpdateKinkster());
        }
        else if (_watcher.TryGetExisting(this, out IntPtr playerAddr))
        {
            Logger.LogDebug($"Matched {Kinkster.GetNickAliasOrUid()} to an existing object @ [{playerAddr:X}]", LoggerType.PairHandlers);
            MarkRenderedInternal(playerAddr);
        }
    }

    private unsafe void MarkRenderedInternal(IntPtr address)
    {
        // Set the game data.
        _player = (Character*)address;
        NameString = _player->NameString;
        NameWithWorld = _player->GetNameWithWorld();
        // Notify other services.
        Logger.LogInformation($"[{Kinkster.GetNickAliasOrUid()}] rendered!", LoggerType.PairHandlers);
        Mediator.Publish(new KinksterRendered(this, Kinkster));
    }

    /// <summary>
    ///     Fired whenever the player is unrendered from the game world. <para />
    /// </summary>
    private unsafe void UnrenderPlayer(IntPtr address)
    {
        if (Address == IntPtr.Zero || address != Address)
            return;

        Logger.LogDebug($"Marking {Kinkster.GetNickAliasOrUid()} as unrendered @ [{address:X}]", LoggerType.PairHandlers);
        _player = null;
        Mediator.Publish(new KinksterUnrendered(address));
        Mediator.Publish(new FolderUpdateKinkster());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // If they were valid before, post the disposal event message.
        if (!string.IsNullOrEmpty(NameString))
        {
            Logger.LogDebug($"Disposing {NameString}({Kinkster.GetNickAliasOrUid()}) @ [{Address:X}]", LoggerType.PairHandlers);
            Mediator.Publish(new EventMessage(new(NameString, Kinkster.UserData.UID, InteractionType.VisibilityChange, "Disposed")));
        }
        // Clear internal data.
        NameString = string.Empty;
        NameWithWorld = string.Empty;
        unsafe { _player = null; }
    }
}
