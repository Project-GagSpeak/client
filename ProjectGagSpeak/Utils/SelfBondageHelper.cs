using GagSpeak.Services;
using GagSpeak.State.Listeners;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;

namespace GagSpeak.Utils;

/// <summary>
///     Used for any calls performed by the client themselves, and not other Kinksters,
///     to their own active data. <para />
///     
///     This includes Gags, Restrictions, Restraints, and Collars. <para />
///     
///     These updates return the new state of the data in the return call, 
///     which are passed into the visual listener.
/// </summary>
public static class SelfBondageHelper
{
    public static void GagUpdateTask(int layer, ActiveGagSlot newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        UiService.SetUITask(async () =>
        {
            if (await dds.PushNewActiveGagSlot(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => visuals.SwapGag(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Applied => visuals.ApplyGag(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Locked => visuals.LockGag(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Unlocked => visuals.UnlockGag(layer, MainHub.PlayerUserData),
                    DataUpdateType.Removed => visuals.RemoveGag(layer, MainHub.PlayerUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
            }
        });
    }

    public static async Task<bool> GagUpdateRetTask(int layer, ActiveGagSlot newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        return await UiService.SetUITaskWithReturn(async () =>
        {
            if (await dds.PushNewActiveGagSlot(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => visuals.SwapGag(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Applied => visuals.ApplyGag(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Locked => visuals.LockGag(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Unlocked => visuals.UnlockGag(layer, MainHub.PlayerUserData),
                    DataUpdateType.Removed => visuals.RemoveGag(layer, MainHub.PlayerUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
                return true;
            }
            return false;
        });
    }

    public static void RestrictionUpdateTask(int layer, ActiveRestriction newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        UiService.SetUITask(async () =>
        {
            if (await dds.PushNewActiveRestriction(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => visuals.SwapRestriction(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Applied => visuals.ApplyRestriction(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Locked => visuals.LockRestriction(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestriction(layer, MainHub.PlayerUserData),
                    DataUpdateType.Removed => visuals.RemoveRestriction(layer, MainHub.PlayerUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
            }
        });
    }

    public static async Task<bool> RestrictionUpdateRetTask(int layer, ActiveRestriction newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        return await UiService.SetUITaskWithReturn(async () =>
        {
            if (await dds.PushNewActiveRestriction(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => visuals.SwapRestriction(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Applied => visuals.ApplyRestriction(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Locked => visuals.LockRestriction(layer, retData, MainHub.PlayerUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestriction(layer, MainHub.PlayerUserData),
                    DataUpdateType.Removed => visuals.RemoveRestriction(layer, MainHub.PlayerUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
                return true;
            }
            return false;
        });
    }

    public static void RestraintUpdateTask(CharaActiveRestraint newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        UiService.SetUITask(async () =>
        {
            if (await dds.PushNewActiveRestraint(newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => visuals.SwapRestraint(retData, MainHub.PlayerUserData),
                    DataUpdateType.Applied => visuals.ApplyRestraint(retData, MainHub.PlayerUserData),
                    DataUpdateType.LayersChanged => visuals.SwapRestraintLayers(retData, MainHub.PlayerUserData),
                    DataUpdateType.LayersApplied => visuals.ApplyRestraintLayers(retData, MainHub.PlayerUserData),
                    DataUpdateType.Locked => visuals.LockRestraint(retData, MainHub.PlayerUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestraint(MainHub.PlayerUserData),
                    DataUpdateType.LayersRemoved => visuals.RemoveRestraintLayers(retData, MainHub.PlayerUserData),
                    DataUpdateType.Removed => visuals.RemoveRestraint(MainHub.PlayerUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
            }
        });
    }

    public static async Task<bool> RestraintUpdateRetTask(CharaActiveRestraint newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        return await UiService.SetUITaskWithReturn(async () =>
        {
            if (await dds.PushNewActiveRestraint(newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => visuals.SwapRestraint(retData, MainHub.PlayerUserData),
                    DataUpdateType.Applied => visuals.ApplyRestraint(retData, MainHub.PlayerUserData),
                    DataUpdateType.LayersChanged => visuals.SwapRestraintLayers(retData, MainHub.PlayerUserData),
                    DataUpdateType.LayersApplied => visuals.ApplyRestraintLayers(retData, MainHub.PlayerUserData),
                    DataUpdateType.Locked => visuals.LockRestraint(retData, MainHub.PlayerUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestraint(MainHub.PlayerUserData),
                    DataUpdateType.LayersRemoved => visuals.RemoveRestraintLayers(retData, MainHub.PlayerUserData),
                    DataUpdateType.Removed => visuals.RemoveRestraint(MainHub.PlayerUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
                return true;
            }
            return false;
        });
    }

    // Only for updates and removals that we do ourselves.
    public static void CollarUpdateTask(CharaActiveCollar newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        // Do not allow accepted requests here.
        if (type is DataUpdateType.RequestAccepted)
            return;

        UiService.SetUITask(async () =>
        {
            if (await dds.PushNewActiveCollar(newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.CollarRemoved => visuals.RemoveCollar(MainHub.PlayerUserData),
                    // everything else is an update.
                    _ => visuals.UpdateActiveCollar(retData, MainHub.PlayerUserData, type)
                };
                await applierTask.ConfigureAwait(false);
            }
        });
    }
}
