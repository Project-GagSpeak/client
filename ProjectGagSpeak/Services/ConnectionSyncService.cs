using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Connection;

namespace GagSpeak.Services;

/// <summary> A class that helps ensure all client data is synced with the currently connected user.
/// <para> The intention here is to make it so that there is no desync with information between logins </para>
/// </summary>
/// <remarks> Helps update config folder locations, update stored data, and update achievement data status. </remarks>
public sealed class ConnectionSyncService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly AccountConfig _accountConfig;
    private readonly ConnectionsConfig _connections;
    private readonly OverlayHandler _overlays;
    private readonly PlayerCtrlHandler _playerControl;
    private readonly RestraintManager _restraints;
    private readonly RestrictionManager _restrictions;
    private readonly GagRestrictionManager _gags;
    private readonly CollarManager _collar;
    private readonly CursedLootManager _cursedLoot;
    private readonly PuppeteerManager _puppeteer;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    private readonly ClientDataListener _clientDatListener;
    private readonly CallbackHandler _visuals;
    private readonly GsFiles _fileNames;
    private readonly AchievementsService _achievements;

    public ConnectionSyncService(
        ILogger<ConnectionSyncService> logger,
        GagspeakMediator mediator,
        MainConfig config,
        AccountConfig accounts,
        ConnectionsConfig connections,
        RestraintManager restraints,
        RestrictionManager restrictions,
        GagRestrictionManager gags,
        CollarManager collar,
        CursedLootManager cursedLoot,
        PuppeteerManager puppeteer,
        AlarmManager alarms,
        TriggerManager triggers,
        OverlayHandler overlays,
        PlayerCtrlHandler playerControl,
        ClientDataListener clientDatListener,
        CallbackHandler visuals,
        GsFiles fileNames,
        AchievementsService achievements)
        : base(logger, mediator)
    {
        _config = config;
        _accountConfig = accounts;
        _connections = connections;
        _overlays = overlays;
        _playerControl = playerControl;
        _restraints = restraints;
        _restrictions = restrictions;
        _gags = gags;
        _collar = collar;
        _cursedLoot = cursedLoot;
        _puppeteer = puppeteer;
        _alarms = alarms;
        _triggers = triggers;
        _clientDatListener = clientDatListener;
        _visuals = visuals;
        _fileNames = fileNames;
        _achievements = achievements;

        Svc.ClientState.Logout += (_,_) => OnLogout();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ClientState.Logout -= (_, _) => OnLogout();
    }

    private void OnLogout()
    {
        Logger.LogInformation("Clearing Client Data for Profile on Logout!");
        _connections.SetCurrentProfile(string.Empty);
    }

    /// <summary>
    ///   By awaiting this, we know it will be distribute data once complete.
    /// </summary>
    public async Task SetClientDataForProfile(ConnectionResponse? response)
    {
        if (response is null)
            return;

        Logger.LogDebug($"ConnectionResponse: {response}, response UID {response.User.UID}, curProfile {_connections.CurrentProfileUID}");
        
        var curProfile = _connections.CurrentProfileUID;
        if (curProfile != response.User.UID)
        {
            // Profile was different, process changes and send to IntroUI if nessisary.
            Logger.LogInformation($"Profile UID changed: {curProfile} -> {response.User.UID}");
            // This ensures all of the below configs get properly loaded in after this change occurs, so we can track when it finishes loading in everything.
            _connections.SetCurrentProfile(response.User.UID);
        }

        // Send them to the intro screen if the service account is not valid.
        if (!_config.Data.HasValidSetup() || !_accountConfig.Current.HasValidSetup())
            Mediator.Publish(new SwitchToIntroUiMessage());

        // 1. Load in the updated config storages for the profile.
        Logger.LogInformation($"[SYNC PROGRESS]: Updating FileProvider for Profile ({MainHub.UID})");
        _connections.SetCurrentProfile(MainHub.UID);

        // 2. Load in Profile-specific Configs.
        Logger.LogInformation($"[SYNC PROGRESS]: Loading Configs for Profile!");
        _gags.Load();
        _restrictions.Load();
        _restraints.Load();
        _collar.Load();
        _cursedLoot.Load();
        _puppeteer.Load();
        _alarms.Load();
        _triggers.Load();

        // 3. Load in the data from the server into our storages.
        Logger.LogInformation("[SYNC PROGRESS]: Syncing ClientData GlobalPerms & HardcoreStatus!");
        _clientDatListener.ChangeAllClientGlobals(response.User, response.GlobalPerms, response.HardcoreState);

        // 4. Sync overlays with the global permissions & metadata.
        Logger.LogInformation("[SYNC PROGRESS]: Applying Custom Hypnosis Data if Any!");
        await _overlays.ReapplySavedActiveEffect();

        // 5. Sync Visual Cache with active state.
        Logger.LogInformation("[SYNC PROGRESS]: Syncing Visual Cache With Display");
        await _visuals.SyncServerData(response);

        // 6. Update the achievement manager with the latest UID and the latest data.
        Logger.LogInformation($"[SYNC PROGRESS]: Syncing Achievement Data ({MainHub.UID})");
        _achievements.OnServerConnection(response.UserAchievements);

        Logger.LogInformation("[SYNC PROGRESS]: Done!");
    }
}
