using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.PlayerData.Services;

/// <summary>
/// Unfortunately Mediators are slow enough to allow bratty 
/// Submissive girls out of restraints, so we need to use raw event handlers for this.
/// </summary>
public class IpcFastUpdates
{
    public delegate void GlamourFastUpdateHandler(GlamourUpdateType updateKind); // define the event handler
    public static event GlamourFastUpdateHandler? GlamourEventFired; // define the static event
    public static void InvokeGlamourer(GlamourUpdateType updateKind) 
        => GlamourEventFired?.Invoke(updateKind);



    public delegate void CustomizeFastUpdateHandler(Guid e);
    public static event CustomizeFastUpdateHandler? CustomizeEventFired;
    public static void InvokeCustomize(Guid Guid) 
        => CustomizeEventFired?.Invoke(Guid);



    public delegate void HardcoreRestraintTraitsHandler(NewState newState, RestraintSet restraintSet);
    public static event HardcoreRestraintTraitsHandler? HardcoreTraitsEventFired;
    public static void InvokeHardcoreTraits(NewState newState, RestraintSet restraintSet) 
        => HardcoreTraitsEventFired?.Invoke(newState, restraintSet);


    public delegate void MoodleStatusManagerChangedHandler(IntPtr playerCharaAddr);
    public static event MoodleStatusManagerChangedHandler? StatusManagerChangedEventFired;
    public static void InvokeStatusManagerChanged(IntPtr playerCharaAddr) 
        => StatusManagerChangedEventFired?.Invoke(playerCharaAddr);
}
