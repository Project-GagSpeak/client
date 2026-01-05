using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Watchers;

/// <summary> 
///     ClientState.LocalPlayer doesn't allow us to get player data outside the games framework thread. <para />
///     This service tracks all Client-Owned Object Creation, Destruction, & Notifiers. <para />
///     This allows us to cache an address that we can guarantee will always be the current 
///     valid state without checking every tick. <para />
/// </summary>
public class CharaObjectWatcher : DisposableMediatorSubscriberBase
{
    internal Hook<Character.Delegates.OnInitialize> OnCharaInitializeHook;
    internal Hook<Character.Delegates.Dtor> OnCharaDestroyHook;
    internal Hook<Character.Delegates.Terminate> OnCharaTerminateHook;

    private readonly CancellationTokenSource _runtimeCTS = new();

    public unsafe CharaObjectWatcher(ILogger<CharaObjectWatcher> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        OnCharaInitializeHook = Svc.Hook.HookFromAddress<Character.Delegates.OnInitialize>((nint)Character.StaticVirtualTablePointer->OnInitialize, InitCharacter);
        OnCharaTerminateHook = Svc.Hook.HookFromAddress<Character.Delegates.Terminate>((nint)Character.StaticVirtualTablePointer->Terminate, TerminateCharacter);
        OnCharaDestroyHook = Svc.Hook.HookFromAddress<Character.Delegates.Dtor>((nint)Character.StaticVirtualTablePointer->Dtor, DestroyCharacter);

        OnCharaInitializeHook.SafeEnable();
        OnCharaTerminateHook.SafeEnable();
        OnCharaDestroyHook.SafeEnable();

        CollectInitialData();
    }

    // A persistent static cache holding all rendered Character pointers.
    public static HashSet<nint> Rendered { get; private set; } = new();

    public unsafe static Character* PlayerTarget => Svc.Targets.Target is IPlayerCharacter t ? AsCharacter(t.Address) : null;
    public static nint TargetAddress => Svc.Targets.Target?.Address ?? nint.Zero;
    public static bool LocalPlayerRendered => Rendered.Contains(PlayerData.Address);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Cancel, but do not dispose, the token.
        _runtimeCTS.SafeCancel();

        OnCharaInitializeHook?.Dispose();
        OnCharaTerminateHook?.Dispose();
        OnCharaDestroyHook?.Dispose();
        // Clear out tracked pointers.
        Rendered.Clear();
    }

    private unsafe void CollectInitialData()
    {
        var objects = GameObjectManager.Instance();
        // Standard Actor Handling.
        for (var i = 0; i < 200; i++)
        {
            GameObject* obj = objects->Objects.IndexSorted[i];
            if (obj is null)
                continue;

            // Only process characters.
            if (!obj->IsCharacter())
                continue;

            if (obj->GetObjectKind() is not (ObjectKind.Pc))
            {
                Logger.LogTrace($"[CharaWatcher] Skipping found character of object kind {obj->GetObjectKind()} at index {i}", LoggerType.VisiblePairs);
                continue;
            }

            AddToWatcher((Character*)obj);
        }

        // Dont need to care about GPose actors, but if we do we can scan from
        // 201 to GameObjectManager.Instance()->Objects.IndexSorted.Length
    }

    /// <summary>
    ///     Obtain the Character* of a rendered address, or null if not present.
    /// </summary>
    public static unsafe Character* AsCharacter(nint address)
        => Rendered.Contains(address) ? (Character*)address : null;

    public static bool TryGetFirst(Func<Character, bool> predicate, [NotNullWhen(true)] out nint charaAddr)
    {
        unsafe
        {
            foreach (Character* addr in Rendered)
            {
                if (predicate(*addr))
                {
                    charaAddr = (nint)addr;
                    return true;
                }
            }
        }
        charaAddr = nint.Zero;
        return false;
    }

    public static unsafe bool TryGetFirstUnsafe(Func<Character, bool> predicate, [NotNullWhen(true)] out Character* character)
    {
        foreach (Character* addr in Rendered)
        {
            if (predicate(*addr))
            {
                character = addr;
                return true;
            }
        }
        character = null;
        return false;
    }

    /// <summary>
    ///     Obtain a Character* if rendered, returning false otherwise.
    /// </summary>
    public static unsafe bool TryGetValue(nint address, [NotNullWhen(true)] out Character* character)
    {
        if (Rendered.Contains(address))
        {
            character = (Character*)address;
            return true;
        }
        character = null;
        return false;
    }

    /// <summary>
    ///     Determine if a Kinkster's OnlineUser Ident matches to any of the currently rendered characters. <para />
    /// </summary>
    /// <returns> True if a match was found, false otherwise. If false, output is <see cref="IntPtr.Zero"/> </returns>
    public bool TryGetExisting(KinksterHandler handler, out IntPtr address)
    {
        address = IntPtr.Zero;

        if (handler.IsRendered && handler.Address != IntPtr.Zero)
        {
            address = handler.Address;
            return true;
        }
     
        // Grab the Ident, and then run a check against all rendered characters.
        var kinksterIdent = handler.Kinkster.Ident;
        foreach (var addr in Rendered)
        {
            // Check via their hashed ident. If it doesn't match, skip.
            var ident = GagSpeakSecurity.GetIdentHashByCharacterPtr(addr);
            if (ident != kinksterIdent)
                continue;

            // Ident match found, so call the handlers object rendered method.
            address = addr;
            return true;
        }
        return false;
    }

    public bool TryGetExisting(string identToCheck, out IntPtr address)
    {
        address = IntPtr.Zero;
        foreach (var addr in Rendered)
        {
            // Check via their hashed ident. If it doesn't match, skip.
            var charaIdent = GagSpeakSecurity.GetIdentHashByCharacterPtr(addr);
            if (charaIdent != identToCheck)
                continue;
            // Ident match found, so call the handlers object rendered method.
            address = addr;
            return true;
        }
        return false;
    }

    private unsafe void AddToWatcher(Character* chara)
    {
        if (chara is null || chara->ObjectIndex < 0 || chara->ObjectIndex >= 200 || !chara->IsCharacter() || chara->GetObjectKind() is not ObjectKind.Pc)
            return;

        if (Rendered.Add((nint)chara))
        {
            var charaNameWorld = chara->GetNameWithWorld();
            Logger.LogTrace($"Added rendered character: {(nint)chara:X} - {charaNameWorld}", LoggerType.VisiblePairs);
            Mediator.Publish(new WatchedObjectCreated((nint)chara));
        }
    }

    private unsafe void RemoveFromWatcher(Character* chara)
    {
        if (Rendered.Remove((nint)chara))
        {
            var charaNameWorld = chara->GetNameWithWorld();
            Logger.LogTrace($"Removing rendered character: {(nint)chara:X} - {charaNameWorld}", LoggerType.VisiblePairs);
            Mediator.Publish(new WatchedObjectDestroyed((nint)chara));
        }
    }

    // Init with original first, than handle so it is present in our other lookups.
    private unsafe void InitCharacter(Character* chara)
    {
        try { OnCharaInitializeHook!.OriginalDisposeSafe(chara); }
        catch (Exception e) { Logger.LogError($"Error: {e}"); }
        Svc.Framework.Run(() => AddToWatcher(chara));
    }

    private unsafe void TerminateCharacter(Character* chara)
    {
        RemoveFromWatcher(chara);
        try { OnCharaTerminateHook!.OriginalDisposeSafe(chara); }
        catch (Exception e) { Logger.LogError($"Error: {e}"); }
    }

    private unsafe GameObject* DestroyCharacter(Character* chara, byte freeMemory)
    {
        RemoveFromWatcher(chara);
        try { return OnCharaDestroyHook!.OriginalDisposeSafe(chara, freeMemory); }
        catch (Exception e) { Logger.LogError($"Error: {e}"); return null; }
    }

    public async Task WaitUntilFinishedLoading(IntPtr address, CancellationToken ct = default)
    {
        if (address == IntPtr.Zero) throw new ArgumentException("Address cannot be null.", nameof(address));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _runtimeCTS.Token);
        while (!cts.IsCancellationRequested)
        {
            // Yes, our clients loading state also impacts anyone else's loading. (that or we are faster than dalamud's object table)
            if (!PlayerData.IsZoning && IsObjectLoaded(address))
                return;
            await Task.Delay(100).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     There are conditions where an object can be rendered / created, but not drawable, or currently bring drawn. <para />
    ///     This mainly occurs on login or when transferring between zones, but can also occur during redraws and such.
    ///     We can get around this by checking for various draw conditions.
    /// </summary>
    public unsafe bool IsObjectLoaded(IntPtr gameObjectAddress)
    {
        var gameObj = (GameObject*)gameObjectAddress;
        // Invalid address.
        if (gameObjectAddress == IntPtr.Zero) return false;
        // DrawObject does not exist yet.
        if ((IntPtr)gameObj->DrawObject == IntPtr.Zero) return false;
        // RenderFlags are marked as 'still loading'.
        if ((ulong)gameObj->RenderFlags == 2048) return false;
        // There are models loaded into slots, still being applied.
        if(((CharacterBase*)gameObj->DrawObject)->HasModelInSlotLoaded != 0) return false;
        // There are model files loaded into slots, still being applied.
        if (((CharacterBase*)gameObj->DrawObject)->HasModelFilesInSlotLoaded != 0) return false;
        // Object is fully loaded.
        return true;
    }
}
