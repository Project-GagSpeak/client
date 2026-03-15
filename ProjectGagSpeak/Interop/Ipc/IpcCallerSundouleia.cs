using Dalamud.Plugin.Ipc;

namespace GagSpeak.Interop;

// Transfer this detection elsewhere maybe.
public sealed class IpcCallerSundouleia : IIpcCaller
{
    private readonly ICallGateSubscriber<int> ApiVersion;

    public IpcCallerSundouleia()
    {
        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Sundouleia.GetApiVersion");
        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void Dispose()
    { }

    public void CheckAPI()
    {
        try
        {
            APIAvailable = ApiVersion.InvokeFunc() is 1;
        }
        catch
        {
            APIAvailable = false;
        }
    }
}
