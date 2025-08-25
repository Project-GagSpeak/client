using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Ipc;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;
using TerraFX.Interop.Windows;

namespace GagSpeak.Interop;

public sealed class IpcCallerCustomize : IIpcCaller
{
    // Version Checks.
    private readonly ICallGateSubscriber<(int, int)> ApiVersion;

    // Event Calls.
    public readonly ICallGateSubscriber<ushort, Guid, object>   OnProfileUpdate;

    // API Getters
    private readonly ICallGateSubscriber<ushort, (int, Guid?)>       GetActiveProfile; // get the active profile of a Kinkster via object index.
    private readonly ICallGateSubscriber<Guid, (int, string?)>       GetProfileById; // obtain that active profiles dataString by GUID.
    private readonly ICallGateSubscriber<IList<IPCProfileDataTuple>> GetProfileList; // grab the list of all client profile ids.

    // API Enactors
    private readonly ICallGateSubscriber<Guid, int>                     EnableProfile;
    private readonly ICallGateSubscriber<Guid, int>                     DisableProfile;
    private readonly ICallGateSubscriber<ushort, string, (int, Guid?)>  SetTempProfile; // set a temp profile for a character using their json and object idx. Returns the GUID.
    private readonly ICallGateSubscriber<Guid, int>                     DeleteTempProfile; // remove the generated temporary profile ID for the player.
    private readonly ICallGateSubscriber<ushort, int>                   RevertKinkster; // revert via object index.

    private readonly ILogger<IpcCallerCustomize> _logger;
    private readonly GagspeakMediator _mediator;

    public IpcCallerCustomize(ILogger<IpcCallerCustomize> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        // API Version Check
        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.General.GetApiVersion");
        // API Events
        OnProfileUpdate = Svc.PluginInterface.GetIpcSubscriber<ushort, Guid, object>("CustomizePlus.Profile.OnUpdate");
        // API Getter Functions
        GetActiveProfile = Svc.PluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        GetProfileById = Svc.PluginInterface.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");
        GetProfileList = Svc.PluginInterface.GetIpcSubscriber<IList<IPCProfileDataTuple>>("CustomizePlus.Profile.GetList");
        // API Enactor Functions
        EnableProfile = Svc.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.EnableByUniqueId");
        SetTempProfile = Svc.PluginInterface.GetIpcSubscriber<ushort, string, (int, Guid?)>("CustomizePlus.Profile.SetTemporaryProfileOnCharacter");
        DisableProfile = Svc.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DisableByUniqueId");
        DeleteTempProfile = Svc.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DeleteTemporaryProfileByUniqueId");
        RevertKinkster = Svc.PluginInterface.GetIpcSubscriber<ushort, int>("CustomizePlus.Profile.DeleteTemporaryProfileOnCharacter");

        OnProfileUpdate.Subscribe(ProfileUpdated);
        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void Dispose()
        => OnProfileUpdate.Unsubscribe(ProfileUpdated);
    
    private void ProfileUpdated(ushort objIdx, Guid id)
    {
        if (!APIAvailable) return;
        // get obj ref for address and publish change.
        Generic.Safe(() =>
        {
            if (Svc.Objects[objIdx] is { } obj)
                _mediator.Publish(new CustomizeProfileChange(obj.Address, id));
        });
    }

    public void CheckAPI()
    {
        try
        {
            var version = ApiVersion.InvokeFunc();
            var success = version.Item1 == 6 && version.Item2 >= 0;
            if(!APIAvailable && success)
                _mediator.Publish(new CustomizeReady());
            APIAvailable = success;
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public CustomizeProfile CurrentActiveProfile()
    {
        if (!APIAvailable) return CustomizeProfile.Empty;

        var result = GetActiveProfile.InvokeFunc(0);
        if (result.Item2 is null)
            return CustomizeProfile.Empty;
        _logger.LogDebug($"Retrieved active profile [{result.Item2}] with EC: [{result.Item1}]", LoggerType.IpcCustomize);
        return new(result.Item2.Value, result.Item1);
    }

    public async Task<string> GetClientProfile()
    {
        if (!APIAvailable) return string.Empty;
        
        var profileStr = await Svc.Framework.RunOnFrameworkThread(() =>
        {
            var res = GetActiveProfile.InvokeFunc(0);
            if (res.Item1 != 0 || res.Item2 is null)
                return string.Empty;
            // get the valid str.
            return GetProfileById.InvokeFunc(res.Item2.Value).Item2 ?? string.Empty;
        }).ConfigureAwait(false);

        if (string.IsNullOrEmpty(profileStr))
            return string.Empty;
        // return the valid profile string.
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(profileStr));
    }

    public async Task<string?> GetKinksterProfile(nint kinksterPtr)
    {
        if (!APIAvailable) return null;
        var profileStr = await Svc.Framework.RunOnFrameworkThread(() =>
        {
            // Only accept requests to obtain profiles for players.
            if (Svc.Objects.CreateObjectReference(kinksterPtr) is { } obj && obj is IPlayerCharacter)
            {
                var res = GetActiveProfile.InvokeFunc(obj.ObjectIndex);
                _logger.LogTrace($"Received active profile for player [{obj.Name}] with EC: [{res.Item1}]", LoggerType.IpcCustomize);
                
                if (res.Item1 != 0 || res.Item2 is null) 
                    return string.Empty;
        
                // get the valid data by ID.
                return GetProfileById.InvokeFunc(res.Item2.Value).Item2;
            }
            // default return.
            return string.Empty;
        }).ConfigureAwait(false);

        if (string.IsNullOrEmpty(profileStr))
            return string.Empty;
        // return the valid profile string.
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(profileStr));
    }

    public async Task<Guid?> SetKinksterProfile(PairHandler kinkster, string profileData)
    {
        if (!APIAvailable || kinkster.PairObject is not { } visibleObj) return null;

        return await Svc.Framework.RunOnFrameworkThread(() =>
        {
            var decodedScale = Encoding.UTF8.GetString(Convert.FromBase64String(profileData));
            _logger.LogTrace($"Applying Profile to {visibleObj.Name}");
            // revert the character if the new data to set was empty.
            if (string.IsNullOrEmpty(profileData))
            {
                RevertKinkster.InvokeFunc(visibleObj.ObjectIndex);
                return null;
            }
            // Otherwise set the new profile data.
            else
            {
                return SetTempProfile.InvokeFunc(visibleObj.ObjectIndex, decodedScale).Item2;
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    ///     Deletes another Kinkster's Temporary profile, reverting them to their original state.
    /// </summary>
    public async Task RevertKinksterProfile(Guid? profileId)
    {
        if (!APIAvailable || profileId is null) return;
        await Svc.Framework.RunOnFrameworkThread(() => DeleteTempProfile.InvokeFunc(profileId.Value)).ConfigureAwait(false);
    }

    public List<CustomizeProfile> GetClientProfiles()
    {
        if (!APIAvailable) return new List<CustomizeProfile>();

        _logger.LogTrace("IPC-Customize is fetching profile list.", LoggerType.IpcCustomize);
        var res = GetProfileList.InvokeFunc();
        return res.Select(tuple => new CustomizeProfile(tuple.UniqueId, tuple.Priority, tuple.Name)).ToList();
    }

    /// <summary>
    ///     Enables one of the client's personal profiles by GUID.
    /// </summary>
    public void EnableClientProfile(Guid profileIdentifier)
    {
        if (!APIAvailable) return;

        _logger.LogTrace("IPC-Customize is enabling profile "+ profileIdentifier, LoggerType.IpcCustomize);
        Generic.Safe(() => EnableProfile.InvokeFunc(profileIdentifier));
    }

    /// <summary>
    ///     Disables one of the client's personal profiles by GUID.
    /// </summary>
    public void DisableClientProfile(Guid profileIdentifier)
    {
        if (!APIAvailable) return;
        _logger.LogTrace("IPC-Customize is disabling profile ["+profileIdentifier+"]", LoggerType.IpcCustomize);
        Generic.Safe(() => DisableProfile!.InvokeFunc(profileIdentifier));
    }
}
