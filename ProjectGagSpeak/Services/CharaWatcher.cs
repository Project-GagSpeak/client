using CkCommons;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.Services.Mediator;
using Microsoft.Extensions.Hosting;
using GagSpeak.Services;

namespace GagSpeak.Watchers;

// Unique Indexes in the sorted object table reserved for select actors.
public enum SpecialActorIdx : ushort
{
    CutsceneStart = 200,
    GroupPosePlayer = 201,
    CutsceneEnd = 440,
    ExamineScreen = 441,
}

/// <summary> 
///   ClientState.LocalPlayer doesn't allow us to get player data outside the games framework thread. <para />
///   This service tracks all Client-Owned Object Creation, Destruction, and Notifiers. <para />
///   This allows us to cache an address that we can guarantee will always be the current 
///   valid state without checking every tick.
/// </summary>
public unsafe class CharaWatcher : IHostedService, IDisposable
{
    internal Hook<Character.Delegates.OnInitialize> OnCharaInitializeHook;
    internal Hook<Character.Delegates.Dtor> OnCharaDestroyHook;
    internal Hook<Character.Delegates.Terminate> OnCharaTerminateHook;

    private readonly ILogger<CharaWatcher> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly VisibilityWatcher _visibleActors;

    // For sanity checking to prevent exploitive manipulation in VisibilityWatcher
    private static readonly HashSet<nint> _validRendered = [];
    public CharaWatcher(ILogger<CharaWatcher> logger, GagspeakMediator mediator, VisibilityWatcher visible)
    {
        _logger = logger;
        _mediator = mediator;
        _visibleActors = visible;

        OnCharaInitializeHook = Svc.Hook.HookFromAddress<Character.Delegates.OnInitialize>((nint)Character.StaticVirtualTablePointer->OnInitialize, InitializeCharacter);
        OnCharaTerminateHook = Svc.Hook.HookFromAddress<Character.Delegates.Terminate>((nint)Character.StaticVirtualTablePointer->Terminate, TerminateCharacter);
        OnCharaDestroyHook = Svc.Hook.HookFromAddress<Character.Delegates.Dtor>((nint)Character.StaticVirtualTablePointer->Dtor, DestroyCharacter);

        OnCharaInitializeHook.SafeEnable();
        OnCharaTerminateHook.SafeEnable();
        OnCharaDestroyHook.SafeEnable();

        CollectInitialData();
    }

    public static IReadOnlySet<nint> Rendered => _validRendered;
    public IntPtr TrackedPlayer   { get; private set; } = nint.Zero;
    public static nint TargetAddress => Svc.Targets.Target?.Address ?? nint.Zero;
    public static bool LocalPlayerRendered => Rendered.Contains(PlayerData.Address);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Collect data from existing objects
        CollectInitialData();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        OnCharaInitializeHook?.SafeDispose();
        OnCharaTerminateHook?.SafeDispose();
        OnCharaDestroyHook?.SafeDispose();
    }

    private void CollectInitialData()
    {
        var objManager = GameObjectManager.Instance();
        // Standard Actor Handling.
        for (var i = 0; i < 200; i++)
        {
            GameObject* obj = objManager->Objects.IndexSorted[i];
            if (obj is null)
                continue;
            // Only process characters.
            if (!obj->IsCharacter())
                continue;
            if (obj->GetObjectKind() is not (ObjectKind.Pc))
                continue;

            Character* chara = (Character*)obj;
            NewCharacterRendered(chara);
        }

        // If in GPose, collect all GPose actors.
        if (GameMain.IsInGPose())
        {
            for (var i = 201; i < objManager->Objects.IndexSorted.Length; i++)
            {
                GameObject* obj = objManager->Objects.IndexSorted[i];
                if (obj is null) continue;
                // Only process characters.
                if (!obj->IsCharacter()) continue;
                if (obj->GetObjectKind() is not (ObjectKind.Pc))
                    continue;
                Character* chara = (Character*)obj;
                NewCharacterRendered(chara);
            }
        }
    }

    /// <summary>
    ///   Entry point for initialized characters.
    /// </summary>
    private void NewCharacterRendered(Character* chara)
    {
        var addr = (nint)chara;
        // Do not track if not a valid object type. (Maybe move to after gpose actor adding)
        if (chara->GetObjectKind() is not (ObjectKind.Pc))
            return;

        // Add it to the list of valid rendered characters.
        _validRendered.Add(addr);
        // _logger.LogTrace($"Valid Actor Rendered: {addr:X} - {chara->GetName()}", LoggerType.OwnedObjects);

        // If it was a GPose actor, early return.
        if (_visibleActors.TryAddTrackedGPoseActor(chara))
            return;

        if (addr == OwnedObjects.PlayerAddress)
            TrackedPlayer = addr;

        _visibleActors.AddTrackedActor(chara);
    }

    private void CharacterRemoved(Character* chara)
    {
        var addr = (nint)chara;
        if (_visibleActors.TryRemoveTrackedGPoseActor(chara))
        {
            _validRendered.Remove(addr);
            return;
        }

        var wasClient = addr == TrackedPlayer;
        if (wasClient)
            TrackedPlayer = nint.Zero;

        _visibleActors.RemoveTrackedActor(chara, wasClient);
        _validRendered.Remove(addr);
    }

    // Init with original first, than handle so it is present in our other lookups.
    private void InitializeCharacter(Character* chara)
    {
        try { OnCharaInitializeHook!.OriginalDisposeSafe(chara); }
        catch (Exception e) { _logger.LogError($"Error: {e}"); }
        _ = Svc.Framework.Run(() => NewCharacterRendered(chara));
    }

    private void TerminateCharacter(Character* chara)
    {
        CharacterRemoved(chara);
        try { OnCharaTerminateHook!.OriginalDisposeSafe(chara); }
        catch (Exception e) { _logger.LogError($"Error: {e}"); }
    }

    private GameObject* DestroyCharacter(Character* chara, byte freeMemory)
    {
        CharacterRemoved(chara);
        try { return OnCharaDestroyHook!.OriginalDisposeSafe(chara, freeMemory); }
        catch (Exception e) { _logger.LogError($"Error: {e}"); return null; }
    }
}
