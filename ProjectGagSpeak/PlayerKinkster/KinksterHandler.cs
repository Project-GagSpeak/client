using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using GagSpeak.Interop;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.Watchers;
using GagspeakAPI.Data;

namespace GagSpeak.Kinksters;

/// <summary>
///     Handles the cached data for a player, and their current rendered status. <para />
///     The Rendered status should be handled differently from the alterations. <para />
///     Every kinkster has their own instance of this data.
/// </summary>
public sealed class KinksterHandler : DisposableMediatorSubscriberBase
{
    private readonly IpcManager _ipc;
    private readonly CharaObjectWatcher _watcher;

    // Ensure a proper runtimeCTS and semaphore for applications is present.
    private readonly SemaphoreSlim  _dataLock   = new(1, 1);
    private CancellationTokenSource _runtimeCTS = new();

    public Kinkster Kinkster { get; init; } // Self-Parent reference.
    private unsafe Character* _player = null;
    // cached data for appearnace.
    public MoodleData MoodlesData { get; private set; } = new();

    public KinksterHandler(Kinkster kinkster, ILogger<KinksterHandler> logger, GagspeakMediator mediator,
        IpcManager ipc, CharaObjectWatcher watcher)
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
    public unsafe IntPtr DrawObjAddress => (nint)_player->DrawObject;
    public unsafe ulong RenderFlags => (ulong)_player->RenderFlags;
    public unsafe bool HasModelInSlotLoaded => ((CharacterBase*)_player->DrawObject)->HasModelInSlotLoaded != 0;
    public unsafe bool HasModelFilesInSlotLoaded => ((CharacterBase*)_player->DrawObject)->HasModelFilesInSlotLoaded != 0;

    public string NameString { get; private set; } = string.Empty; // Manual, to assist timeout tasks.
    public string NameWithWorld { get; private set; } = string.Empty; // Manual, to assist timeout tasks.
    public unsafe bool IsRendered => _player != null;

    #region Rendering
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
            Mediator.Publish(new KinksterPlayerRendered(this, Kinkster));
            Mediator.Publish(new FolderUpdateKinkster());
            await ReInitializeInternal().ConfigureAwait(false);
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
        Mediator.Publish(new KinksterPlayerRendered(this, Kinkster));

        // ReInitialize our alterations for becoming visible again.
        ReInitializeInternal().ConfigureAwait(false);
    }

    private async Task ReInitializeInternal()
    {
        await _dataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // If they are online and have alterations, reapply them. Otherwise, exit.
            if (!Kinkster.IsOnline)
            {
                Logger.LogDebug($"[{Kinkster.GetNickAliasOrUid()}] reinit skipped: IsOnline: {Kinkster.IsOnline}.", LoggerType.PairHandlers);
                return;
            }

            // Await until we know the player has absolutely finished loading in.
            await _watcher.WaitUntilFinishedLoading(Address).ConfigureAwait(false);
            Logger.LogDebug($"[{Kinkster.GetNickAliasOrUid()}] finished loaded, reapplying alterations.", LoggerType.PairHandlers);
            await ApplyAlterationsInternal().ConfigureAwait(false);
        }
        finally
        {
            _dataLock.Release();
        }
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
        // Upon unrendering, clear any visual alterations along with their data.
        RevertAlterations(NameString, address).ConfigureAwait(false);

        Mediator.Publish(new KinksterPlayerUnrendered(address));
        Mediator.Publish(new FolderUpdateKinkster());
    }

    #endregion Rendering

    #region Altaration Control
    public async Task ReapplyAlterations()
    {
        await _dataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await ApplyAlterationsInternal();
        }
        finally
        {
            _dataLock.Release();
        }
    }

    /// <inheritdoc cref="RevertAlterations(string, nint, CancellationToken)"/>
    public async Task RevertAlterations(CancellationToken ct = default)
    {
        await _dataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // At the moment the only alteration we do is moodles, and we should not do that if sundouleia is active.
            if (IsRendered && !IpcCallerSundouleia.CurrentKinksters.Contains(Address))
                await _ipc.Moodles.ClearByPtr(Address).ConfigureAwait(false);

            // Clear the data up
            MoodlesData = new MoodleData();
        }
        finally
        {
            _dataLock.Release();
        }
    }
    /// <summary>
    ///     Reverts the rendered alterations on a player.<br/>
    ///     <b>This does not delete the alteration data. </b>
    /// </summary>
    private async Task RevertAlterations(string name, IntPtr address, CancellationToken token = default)
    {
        await _dataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // At the moment the only alteration we do is moodles,
            // and we should not do that if they are still a sundouleia user.
            if (IsRendered && !IpcCallerSundouleia.CurrentKinksters.Contains(address))
                await _ipc.Moodles.ClearByPtr(address).ConfigureAwait(false);

            // Clear the data up
            MoodlesData = new MoodleData();
        }
        finally
        {
            _dataLock.Release();
        }
    }

    public async void UpdateAndApplyMoodles(MoodleData newMoodleData)
    {
        await _dataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            MoodlesData = new MoodleData(newMoodleData);
            // Reapply the moodles.
            await ApplyAlterationsInternal().ConfigureAwait(false);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    public async void UpdateAndApplyMoodles(string DataString, IEnumerable<MoodlesStatusInfo> DataInfo)
    {
        await _dataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            MoodlesData.UpdateDataInfo(DataString, DataInfo);
            // Reapply the moodles.
            await ApplyAlterationsInternal().ConfigureAwait(false);
        }
        finally
        {
            _dataLock.Release();
        }
    }

    private async Task ApplyAlterationsInternal()
    {
        if (!IsRendered || MoodlesData.DataString.Length is 0)
            return;

        // Await until we know the player has absolutely finished loading in.
        await _watcher.WaitUntilFinishedLoading(Address).ConfigureAwait(false);
        await ApplyMoodles().ConfigureAwait(false);
    }

    private async Task ApplyMoodles()
    {
        Logger.LogDebug($"Setting moodles for {NameString}({Kinkster.GetNickAliasOrUid()})", LoggerType.PairHandlers);
        await _ipc.Moodles.SetByPtr(Address, MoodlesData.DataString).ConfigureAwait(false);
    }

    #endregion Alteration Control

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        IntPtr addr = IsRendered ? Address : IntPtr.Zero;
        ushort objIdx = IsRendered ? ObjIndex : (ushort)0;

        // Stop any actively running tasks.
        _runtimeCTS.SafeCancel();

        // If they were valid before, parse out the event message for their disposal.
        if (!string.IsNullOrEmpty(NameString))
        {
            Logger.LogDebug($"Disposing {NameString}({Kinkster.GetNickAliasOrUid()}) @ [{Address:X}]", LoggerType.PairHandlers);
            Mediator.Publish(new EventMessage(new(NameString, Kinkster.UserData.UID, InteractionType.VisibilityChange, "Disposed")));
        }

        // Do not dispose if the framework is unloading!
        // (means we are shutting down the game and cannot transmit calls to other ipcs without causing fatal errors!)
        if (Svc.Framework.IsFrameworkUnloading)
            return;

        // Process off the disposal thread. (Avoids deadlocking on plugin shutdown)
        // Everything in here, if it errors, should not crash the game as it is fire and forget.
        _ = Task.Run(async () =>
        {
            var nickAliasOrUid = Kinkster.GetNickAliasOrUid();
            var name = NameString;
            try
            {
                // If we are not zoning and not in a cutscene, run the revert with a 30s timeout.
                if (!PlayerData.IsZoning && !PlayerData.InCutscene)
                {
                    Logger.LogDebug($"{name}(({nickAliasOrUid}) is rendered, reverting by address/index.", LoggerType.PairHandlers);
                    using var timeoutCTS = new CancellationTokenSource();
                    timeoutCTS.CancelAfter(TimeSpan.FromSeconds(30));
                    await RevertAlterations(name, addr, timeoutCTS.Token);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reverting {name}({nickAliasOrUid} on shutdown: {ex}");
            }
            finally
            {
                // Clear internal data.
                MoodlesData = new();
                NameString = string.Empty;
                NameWithWorld = string.Empty;
                unsafe { _player = null; }
            }
        });
    }

    public void DrawDebugInfo()
    {
        using var node = ImRaii.TreeNode($"Player Alterations##{Kinkster.UserData.UID}-alterations");
        if (!node) return;

        using (var t = ImRaii.Table("kinkster-appearance", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter))
        {
            if (!t) return;
            ImGui.TableSetupColumn("Data Type");
            ImGui.TableSetupColumn("Reapply Test");
            ImGui.TableSetupColumn("Data Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            ImGui.TableNextColumn();
            ImGui.Text("Moodles");
            ImGui.TableNextColumn();
            if (CkGui.IconTextButton(FAI.Sync, "Reapply", disabled: MoodlesData.DataString.Length is 0, id: $"{Kinkster.UserData.UID}-moodles-reapply"))
                UiService.SetUITask(ApplyMoodles);
            ImGui.TableNextColumn();
            ImGui.Text(MoodlesData.DataString);
        }

        // Can draw additional debug information here that already exists in other debuggers maybe?
    }
}
