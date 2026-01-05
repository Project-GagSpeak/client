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
                    DataUpdateType.Swapped => visuals.SwapGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => visuals.ApplyGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => visuals.LockGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => visuals.UnlockGag(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => visuals.RemoveGag(layer, MainHub.OwnUserData),
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
                    DataUpdateType.Swapped => visuals.SwapGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => visuals.ApplyGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => visuals.LockGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => visuals.UnlockGag(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => visuals.RemoveGag(layer, MainHub.OwnUserData),
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
                    DataUpdateType.Swapped => visuals.SwapRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => visuals.ApplyRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => visuals.LockRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestriction(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => visuals.RemoveRestriction(layer, MainHub.OwnUserData),
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
                    DataUpdateType.Swapped => visuals.SwapRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => visuals.ApplyRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => visuals.LockRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestriction(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => visuals.RemoveRestriction(layer, MainHub.OwnUserData),
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
                    DataUpdateType.Swapped => visuals.SwapRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => visuals.ApplyRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersChanged => visuals.SwapRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersApplied => visuals.ApplyRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => visuals.LockRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestraint(MainHub.OwnUserData),
                    DataUpdateType.LayersRemoved => visuals.RemoveRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Removed => visuals.RemoveRestraint(MainHub.OwnUserData),
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
                    DataUpdateType.Swapped => visuals.SwapRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => visuals.ApplyRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersChanged => visuals.SwapRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersApplied => visuals.ApplyRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => visuals.LockRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => visuals.UnlockRestraint(MainHub.OwnUserData),
                    DataUpdateType.LayersRemoved => visuals.RemoveRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Removed => visuals.RemoveRestraint(MainHub.OwnUserData),
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
                    DataUpdateType.CollarRemoved => visuals.RemoveCollar(MainHub.OwnUserData),
                    // everything else is an update.
                    _ => visuals.UpdateActiveCollar(retData, MainHub.OwnUserData, type)
                };
                await applierTask.ConfigureAwait(false);
            }
        });
    }
}
