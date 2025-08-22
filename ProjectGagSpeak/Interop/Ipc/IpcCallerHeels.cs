using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

public sealed class IpcCallerHeels : IIpcCaller
{   
    // Remember, all these are called only when OUR client changes. Not other pairs.
    private readonly ICallGateSubscriber<(int, int)> _heelsApiVersion;

    // API EVENTS.
    public readonly ICallGateSubscriber<string, object?>      OnOffsetUpdate; // Heels offset updated for a registered player.

    // API Getter Functions
    public readonly ICallGateSubscriber<string>               GetOffset; // get the offset for (someone?)

    // API Enactor Functions
    public readonly ICallGateSubscriber<int, string, object?> RegisterPlayer;
    public readonly ICallGateSubscriber<int, object?>         UnregisterPlayer; // Unregister a player from the heels system.

    private readonly GagspeakMediator _mediator;
    public IpcCallerHeels(GagspeakMediator mediator)
    {
        _mediator = mediator;
        _heelsApiVersion = Svc.PluginInterface.GetIpcSubscriber<(int, int)>("SimpleHeels.ApiVersion");

        // API Getter
        GetOffset = Svc.PluginInterface.GetIpcSubscriber<string>("SimpleHeels.GetLocalPlayer");

        // API Enactor
        RegisterPlayer = Svc.PluginInterface.GetIpcSubscriber<int, string, object?>("SimpleHeels.RegisterPlayer");
        UnregisterPlayer = Svc.PluginInterface.GetIpcSubscriber<int, object?>("SimpleHeels.UnregisterPlayer");

        // API Events
        OnOffsetUpdate = Svc.PluginInterface.GetIpcSubscriber<string, object?>("SimpleHeels.LocalChanged");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            APIAvailable = _heelsApiVersion.InvokeFunc() is { Item1: 2, Item2: >= 1 };
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public void Dispose()
        => OnOffsetUpdate.Unsubscribe(ClientOffsetChanged);

    private void ClientOffsetChanged(string newOffset)
        => _mediator.Publish(new HeelsOffsetChanged());

    // Requests the local offset of the client from SimpleHeels.
    public async Task<string> GetOffsetAsync()
    {
        if (!APIAvailable) return string.Empty;
        return await Svc.Framework.RunOnFrameworkThread(GetOffset.InvokeFunc).ConfigureAwait(false);
    }

    /// <summary>
    ///     Return the offsets of a target player pointer to their original state.
    /// </summary>
    /// <param name="character"> the pairHandler pointer to revert. </param>
    public async Task RestoreOffsetOfPlayer(IntPtr charaPtr)
    {
        if (!APIAvailable) return;
        
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (Svc.Objects.CreateObjectReference(charaPtr) is { } obj)
                UnregisterPlayer.InvokeAction(obj.ObjectIndex);
        }).ConfigureAwait(false);
    }

    /// <summary>
    ///     Sets the offset for the player by their pointer address to the provided data.
    /// </summary>
    public async Task SetOffsetForPlayer(IntPtr charaPtr, string offsetData)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (Svc.Objects.CreateObjectReference(charaPtr) is { } obj)
                RegisterPlayer.InvokeAction(obj.ObjectIndex, offsetData);
        }).ConfigureAwait(false);
    }
}
