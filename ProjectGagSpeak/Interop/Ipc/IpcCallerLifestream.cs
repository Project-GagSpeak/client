using Dalamud.Plugin.Ipc;

namespace GagSpeak.Interop;

public sealed class IpcCallerLifestream : IIpcCaller
{
    // API Version

    // API Events
    private readonly ICallGateSubscriber<object> OnHouseEnterError;

    // API Getters
    private readonly ICallGateSubscriber<AddressBookEntryTuple, bool> GetIsAtAddress;
    private readonly ICallGateSubscriber<bool>                        GetIsBusy;

    // API Enactors
    // IPC Function Delegates (calls that instruct Lifestream to do something)
    private readonly ICallGateSubscriber<AddressBookEntryTuple, object> TravelToAddress;
    private readonly ICallGateSubscriber<object>                        AbortTask;

    public IpcCallerLifestream()
    {
        OnHouseEnterError = Svc.PluginInterface.GetIpcSubscriber<object>("Lifestream.OnHouseEnterError");

        GetIsBusy = Svc.PluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
        GetIsAtAddress = Svc.PluginInterface.GetIpcSubscriber<AddressBookEntryTuple, bool>("Lifestream.IsHere");

        TravelToAddress = Svc.PluginInterface.GetIpcSubscriber<AddressBookEntryTuple, object>("Lifestream.GoToHousingAddress");
        AbortTask = Svc.PluginInterface.GetIpcSubscriber<object>("Lifestream.Abort");

        // subscribe to event.
        OnHouseEnterError.Subscribe(OnErrorEnteringHouse);

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

    private void OnErrorEnteringHouse()
    {
        Svc.Logger.Warning("Lifestream reported an error entering the house. You may be stuck outside your house.");
    }

    /// <summary> Checks if we are at the desired address. </summary>
    public bool IsAtAddress(AddressBookEntryTuple address)
    {
        if (!APIAvailable)
            return false;

        return GetIsAtAddress.InvokeFunc(address);
    }

    /// <summary> Checks if we are busy with a task. </summary>
    public bool IsCurrentlyBusy()
    {
        if (!APIAvailable)
            return false;

        return GetIsBusy.InvokeFunc();
    }

    /// <summary> Aborts the current task. </summary>
    public void AbortCurrentTask()
    {
        if (!APIAvailable)
            return;

        AbortTask.InvokeAction();
    }

    /// <summary> Attempts to go to the specified address. </summary>
    public void GoToAddress(AddressBookEntryTuple address)
    {
        if (!APIAvailable)
            return;

        // invoke the action for the address.
        TravelToAddress.InvokeAction(address);
    }
}
