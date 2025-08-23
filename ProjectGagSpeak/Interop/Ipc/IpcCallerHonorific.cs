using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using GagSpeak.Kinksters;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

public sealed class IpcCallerHonorific : IIpcCaller
{
    // API Calls
    private readonly ICallGateSubscriber<(uint, uint)> ApiVersion;
    // API Events
    private readonly ICallGateSubscriber<object> Ready;
    private readonly ICallGateSubscriber<object> Disposing;
    private readonly ICallGateSubscriber<string, object> OnTitleChange; // When the client changed their honorific title.
    // API Getters
    private readonly ICallGateSubscriber<string> GetClientTitle;
    // API Enactors
    private readonly ICallGateSubscriber<int, string, object> SetKinksterTitle;
    private readonly ICallGateSubscriber<int, object> ClearKinksterTitle;

    private readonly ILogger<IpcCallerHonorific> _logger;
    private readonly GagspeakMediator _mediator;

    public IpcCallerHonorific(ILogger<IpcCallerHonorific> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        // API Version.
        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<(uint, uint)>("Honorific.ApiVersion");
        // Events
        Ready = Svc.PluginInterface.GetIpcSubscriber<object>("Honorific.Ready");
        Disposing = Svc.PluginInterface.GetIpcSubscriber<object>("Honorific.Disposing");
        OnTitleChange = Svc.PluginInterface.GetIpcSubscriber<string, object>("Honorific.LocalCharacterTitleChanged");
        // Getters
        GetClientTitle = Svc.PluginInterface.GetIpcSubscriber<string>("Honorific.GetLocalCharacterTitle");
        // Enactors
        SetKinksterTitle = Svc.PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
        ClearKinksterTitle = Svc.PluginInterface.GetIpcSubscriber<int, object>("Honorific.ClearCharacterTitle");

        OnTitleChange.Subscribe(OnTitleChanged);
        Ready.Subscribe(OnHonorificReady);
        Disposing.Subscribe(OnHonorificDisposing);

        CheckAPI();
    }

    public bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            APIAvailable = ApiVersion.InvokeFunc() is { Item1: 3, Item2: >= 1 };
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public void Dispose()
    {
        OnTitleChange.Unsubscribe(OnTitleChanged);
        Ready.Unsubscribe(OnHonorificReady);
        Disposing.Unsubscribe(OnHonorificDisposing);
    }
    private void OnHonorificReady()
    {
        CheckAPI();
        _mediator.Publish(new HonorificReady());
    }
    private void OnHonorificDisposing()
        => _mediator.Publish(new HonorificTitleChanged(string.Empty));

    private void OnTitleChanged(string titleJson)
    {
        string titleData = string.IsNullOrEmpty(titleJson) ? string.Empty : Convert.ToBase64String(Encoding.UTF8.GetBytes(titleJson));
        _mediator.Publish(new HonorificTitleChanged(titleData));
    }

    public async Task<string> GetTitle()
    {
        if (!APIAvailable) return string.Empty;

        var title = await Svc.Framework.RunOnFrameworkThread(() => GetClientTitle.InvokeFunc()).ConfigureAwait(false);
        return string.IsNullOrEmpty(title) ? string.Empty : Convert.ToBase64String(Encoding.UTF8.GetBytes(title));
    }

    public async Task SetTitleAsync(PairHandler kinkster, string titleDataBase64)
    {
        if (!APIAvailable || kinkster.PairObject is not { } visibleObj) return;

        await Generic.Safe(async () =>
        {
            await Svc.Framework.RunOnFrameworkThread(() =>
            {
                _logger.LogTrace($"Applying title to {visibleObj.Name}");
                string titleData = string.IsNullOrEmpty(titleDataBase64) ? string.Empty : Encoding.UTF8.GetString(Convert.FromBase64String(titleDataBase64));
                if (string.IsNullOrEmpty(titleData))
                    ClearKinksterTitle.InvokeAction(visibleObj.ObjectIndex);
                else
                    SetKinksterTitle.InvokeAction(visibleObj.ObjectIndex, titleData);
            }).ConfigureAwait(false);
        });
    }

    public async Task ClearTitleAsync(PairHandler kinkster)
    {
        if (!APIAvailable || kinkster.PairObject is not { } visibleObj) return;

        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            _logger.LogTrace($"Removing title for {visibleObj.Name}");
            ClearKinksterTitle.InvokeAction(visibleObj.ObjectIndex);
        }).ConfigureAwait(false);
    }
}
