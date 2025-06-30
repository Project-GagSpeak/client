using Dalamud.Plugin.Ipc;

namespace GagSpeak.Interop;

public sealed class IpcCallerLifestream : IIpcCaller
{
    // IPC Function Callers (We want to know what lifestream is doing
    private readonly ICallGateSubscriber<AddressBookEntryTuple, bool> _isHere;
    private readonly ICallGateSubscriber<bool> _isBusy;

    // IPC Function Delegates (calls that instruct Lifestream to do something)
    private readonly ICallGateSubscriber<AddressBookEntryTuple, object> _goToAddress;
    private readonly ICallGateSubscriber<object> _abortTask;

    // Events (Calls Lifestream invokes when things happen.
    private readonly ICallGateSubscriber<object>  OnHouseEnterError;

    public IpcCallerLifestream()
    {
        _isHere = Svc.PluginInterface.GetIpcSubscriber<AddressBookEntryTuple, bool>("Lifestream.IsHere");
        _isBusy = Svc.PluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");

        _goToAddress = Svc.PluginInterface.GetIpcSubscriber<AddressBookEntryTuple, object>("Lifestream.GoToAddress");
        _abortTask = Svc.PluginInterface.GetIpcSubscriber<object>("Lifestream.Abort");

        OnHouseEnterError = Svc.PluginInterface.GetIpcSubscriber<object>("Lifestream.OnHouseEnterError");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        var lifestreamPlugin = Svc.PluginInterface.InstalledPlugins.FirstOrDefault(p => string.Equals(p.InternalName, "lifestream", StringComparison.OrdinalIgnoreCase));
        if (lifestreamPlugin is null)
        {
            APIAvailable = false;
            return;
        }
        // lifestream is installed, so see if it is on.
        APIAvailable = lifestreamPlugin.IsLoaded ? true : false;
        return;
    }

    public void Dispose() 
    { }

    /// <summary> Checks if we are at the desired address. </summary>
    public bool IsAtAddress(AddressBookEntryTuple address)
    {
        if (!APIAvailable)
            return false;

        return _isHere.InvokeFunc(address);
    }

    /// <summary> Checks if we are busy with a task. </summary>
    public bool IsCurrentlyBusy()
    {
        if (!APIAvailable)
            return false;

        return _isBusy.InvokeFunc();
    }

    /// <summary> Aborts the current task. </summary>
    public void AbortCurrentTask()
    {
        if (!APIAvailable)
            return;

        _abortTask.InvokeAction();
    }

    /// <summary> Attempts to go to the specified address. </summary>
    public void GoToAddress(AddressBookEntryTuple address)
    {
        if (!APIAvailable)
            return;

        // invoke the action for the address.
        _goToAddress.InvokeAction(address);
    }
}
