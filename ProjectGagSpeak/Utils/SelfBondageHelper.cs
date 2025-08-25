using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.State.Listeners;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using GagspeakAPI.Util;
using OtterGui.Text.EndObjects;
using TerraFX.Interop.Windows;

namespace GagSpeak.Utils;

/// <summary>
///     WARNING: This class can bypass any special permissions that need to happen on value change, 
///     be sure to account for these, or else it will become problematic.
///     
///     This classes primary purpose is for the UI to display updated values before recieving the callback, and processing the callback after it gets it
///     to handle any achievement tracking or handlers.
///     
///     Either find a way to handle the callbacks automatically based on their changed state, or setup callbacks to never callback to the caller 
///     that made the change and process internally. Either way, do this AFTER the update, as it mostly saves on server cost for interactions.
/// </summary>
public static class SelfBondageHelper
{
    public static void GagUpdateTask(int layer, ActiveGagSlot newData, DataUpdateType type, DistributorService dds, VisualStateListener visuals)
    {
        UiService.SetUITask(async () =>
        {
            if (await dds.PushNewActiveGagSlot(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                Task applierTask = type switch
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
                Task applierTask = type switch
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
                Task applierTask = type switch
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
                Task applierTask = type switch
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
                Task applierTask = type switch
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
                Task applierTask = type switch
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
}
