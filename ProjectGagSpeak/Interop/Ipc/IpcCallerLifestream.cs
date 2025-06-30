using Dalamud.Plugin.Ipc;

namespace GagSpeak.Interop;

public sealed class IpcCallerLifestream : IIpcCaller
{
    // Getters?
    private readonly ICallGateSubscriber<AddressBookEntryTuple, bool>   IsHere; // If already at the desired location.
    private readonly ICallGateSubscriber<bool>                          IsBusy; // If a current task is running.
    private readonly ICallGateSubscriber                                Abort; // Abort the current task.
    // Events
    private readonly ICallGateSubscriber<Action>                        OnHouseEnterError; // when we fail entering the house. Is event.
    // Actions
    private readonly ICallGateSubscriber<AddressBookEntryTuple>         _goToAddress; // The housing Address to go to.

    private readonly ILogger<IpcCallerLifestream> _logger;
    public IpcCallerLifestream(ILogger<IpcCallerLifestream> logger)
    {
        _logger = logger;

        // Lifestream stuff i guess.
        IsHere = Svc.PluginInterface.GetIpcSubscriber<AddressBookEntryTuple, bool>("Lifestream.IsHere");
        IsBusy = Svc.PluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
        //Abort = Svc.PluginInterface.GetIpcSubscriber<void>("Lifestream.Abort");

        OnHouseEnterError = Svc.PluginInterface.GetIpcSubscriber<Action>("Lifestream.OnHouseEnterError");

        _goToAddress = Svc.PluginInterface.GetIpcSubscriber<AddressBookEntryTuple>("Lifestream.GoToAddress");

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

    public void Dispose() { }

    ///// <summary> Checks if we are at the desired address. </summary>
    //public bool IsAtAddress(AddressBookEntryTuple address)
    //{
    //    if (!APIAvailable) return false;
    //    try
    //    {
    //        return IsHere.InvokeFunc(address);
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogWarning("Could not check if at address: " + e, LoggerType.IpcLifestream);
    //        return false;
    //    }
    //}

    ///// <summary> Checks if we are busy with a task. </summary>
    //public bool IsCurrentlyBusy()
    //{
    //    if (!APIAvailable) return false;
    //    try
    //    {
    //        return IsBusy.InvokeFunc();
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogWarning("Could not check if busy: " + e, LoggerType.IpcLifestream);
    //        return false;
    //    }
    //}

    ///// <summary> Aborts the current task. </summary>
    //public void AbortCurrentTask()
    //{
    //    if (!APIAvailable) return;
    //    try
    //    {
    //        Abort.ToString(); /// ???????
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogWarning("Could not abort task: " + e, LoggerType.IpcLifestream);
    //    }
    //}

    ///// <summary> Attempts to go to the specified address. </summary>
    //public void GoToAddress(AddressBookEntryTuple address)
    //{
    //    if (!APIAvailable) return;
    //    try
    //    {
    //        _goToAddress.InvokeFunc(address);
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogWarning("Could not go to address: " + e, LoggerType.IpcLifestream);
    //    }
    //}

    ///// <summary> Subscribes to the house enter error event. </summary>
    //public void SubscribeToHouseEnterError(Action action)
    //{
    //    if (!APIAvailable) return;
    //    try
    //    {
    //        OnHouseEnterError.Subscribe(action);
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogWarning("Could not subscribe to house enter error: " + e, LoggerType.IpcLifestream);
    //    }
    //}
}
