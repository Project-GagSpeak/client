using CkCommons;
using Dalamud.Plugin.Ipc;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

public sealed class IpcCallerPetNames : IIpcCaller
{
    // API Version
    private readonly ICallGateSubscriber<(uint, uint)> ApiVersion;

    // API Events
    private readonly ICallGateSubscriber<object> OnReady;
    private readonly ICallGateSubscriber<object> OnDisposed;
    private readonly ICallGateSubscriber<string, object> OnNicknamesChanged;
    // API Getters
    private readonly ICallGateSubscriber<bool> GetIsEnabled;
    private readonly ICallGateSubscriber<string> GetNicknameData;
    // API Enactors
    private readonly ICallGateSubscriber<string, object> SetNicknameData;
    private readonly ICallGateSubscriber<ushort, object> ClearNicknameData;

    private readonly ILogger<IpcCallerPetNames> _logger;
    private readonly GagspeakMediator _mediator;
    public IpcCallerPetNames(ILogger<IpcCallerPetNames> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        // API Version.
        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<(uint, uint)>("PetRenamer.ApiVersion");
        // Events
        OnReady = Svc.PluginInterface.GetIpcSubscriber<object>("PetRenamer.OnReady");
        OnDisposed = Svc.PluginInterface.GetIpcSubscriber<object>("PetRenamer.OnDisposing");
        OnNicknamesChanged = Svc.PluginInterface.GetIpcSubscriber<string, object>("PetRenamer.OnPlayerDataChanged");
        // Getters
        GetIsEnabled = Svc.PluginInterface.GetIpcSubscriber<bool>("PetRenamer.IsEnabled");
        GetNicknameData = Svc.PluginInterface.GetIpcSubscriber<string>("PetRenamer.GetPlayerData");
        // Enactors
        SetNicknameData = Svc.PluginInterface.GetIpcSubscriber<string, object>("PetRenamer.SetPlayerData");
        ClearNicknameData = Svc.PluginInterface.GetIpcSubscriber<ushort, object>("PetRenamer.ClearPlayerData");

        OnReady.Subscribe(OnIpcReady);
        OnDisposed.Subscribe(OnDispose);
        OnNicknamesChanged.Subscribe(OnNicknamesChange);

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            APIAvailable = GetIsEnabled?.InvokeFunc() ?? false;
            if (APIAvailable)
                APIAvailable = ApiVersion?.InvokeFunc() is { Item1: 4, Item2: >= 0 };
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public void Dispose()
    {
        OnReady.Unsubscribe(OnIpcReady);
        OnDisposed.Unsubscribe(OnDispose);
        OnNicknamesChanged.Unsubscribe(OnNicknamesChange);
    }

    private void OnIpcReady()
    {
        CheckAPI();
        _mediator.Publish(new PetNamesReady());
    }

    private void OnDispose()
        => _mediator.Publish(new PetNamesDataChanged(string.Empty));

    private void OnNicknamesChange(string newData)
        => _mediator.Publish(new PetNamesDataChanged(newData));

    public string GetPetNicknames()
    {
        if (!APIAvailable) return string.Empty;

        Generic.Safe(() =>
        {
            var localNameData = GetNicknameData.InvokeFunc();
            return string.IsNullOrEmpty(localNameData) ? string.Empty : localNameData;
        });

        return string.Empty;
    }

    public async Task SetKinksterPetNames(PairHandler kinkster, string nicknameData)
    {
        if (!APIAvailable || kinkster.PairObject is not { } visibleObj) return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() =>
            {
                _logger.LogTrace($"Applying pet Nickname updates to {kinkster.PlayerName}'s Pets!");
                // if the data is empty, clear the nicknames.
                if (string.IsNullOrEmpty(nicknameData))
                    ClearNicknameData.InvokeAction(visibleObj.ObjectIndex);
                // otherwise, set the nicknames.
                else
                    SetNicknameData.InvokeAction(nicknameData);
            }).ConfigureAwait(false);
        });
    }

    public async Task ClearKinksterPetNames(PairHandler kinkster)
    {
        if (!APIAvailable || kinkster.PairObject is not { } visibleObj) return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() =>
            {
                _logger.LogTrace($"Clearing Nicknames from {kinkster.PlayerName}'s pets!");
                ClearNicknameData.InvokeAction(visibleObj.ObjectIndex);
            }).ConfigureAwait(false);
        });
    }
}
