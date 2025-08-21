using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using TerraFX.Interop.Windows;

namespace GagSpeak.Services;

/// <summary> A class that helps ensure all client data is synced with the currently connected user.
/// <para> The intention here is to make it so that there is no desync with information between logins </para>
/// </summary>
/// <remarks> Helps update config folder locations, update stored data, and update achievement data status. </remarks>
public sealed class ConnectionSyncService : DisposableMediatorSubscriberBase
{
    private readonly OverlayHandler _overlays;
    private readonly PlayerCtrlHandler _playerControl;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PuppeteerManager _puppeteer;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    private readonly ClientDataListener _clientDatListener;
    private readonly VisualStateListener _visuals;
    private readonly ConfigFileProvider _fileNames;
    private readonly AchievementsService _achievements;

    public ConnectionSyncService(
        ILogger<ConnectionSyncService> logger,
        GagspeakMediator mediator,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        PuppeteerManager puppeteer,
        AlarmManager alarms,
        TriggerManager triggers,
        OverlayHandler overlays,
        PlayerCtrlHandler playerControl,
        ClientDataListener clientDatListener,
        VisualStateListener visuals,
        ConfigFileProvider fileNames,
        AchievementsService achievements)
        : base(logger, mediator)
    {
        _overlays = overlays;
        _playerControl = playerControl;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
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
        _fileNames.ClearUidConfigs();
    }

    /// <summary>
    ///     By awaiting this, we know it will be distribute data once complete.
    /// </summary>
    public async Task SetClientDataForProfile()
    {
        // if the ConnectionResponse for whatever reason was null, dont process any of this.
        // (this theoretically should never happen, but just in case)
        if (MainHub.ConnectionResponse is not { } connectionInfo)
            return;

        // 1. Load in the updated config storages for the profile.
        Logger.LogInformation($"[SYNC PROGRESS]: Updating FileProvider for Profile ({MainHub.UID})");
        _fileNames.UpdateConfigs(MainHub.UID);

        // 2. Load in Profile-specific Configs.
        Logger.LogInformation($"[SYNC PROGRESS]: Loading Configs for Profile!");
        _gags.Load();
        _restrictions.Load();
        _restraints.Load();
        _cursedLoot.Load();
        _puppeteer.Load();
        _alarms.Load();
        _triggers.Load();

        // 3. Load in the data from the server into our storages.
        Logger.LogInformation("[SYNC PROGRESS]: Syncing ClientData GlobalPerms & HardcoreState!");
        _clientDatListener.ChangeAllClientGlobals(connectionInfo.User, connectionInfo.GlobalPerms, connectionInfo.HardcoreState);

        // 4. Sync overlays with the global permissions & metadata.
        Logger.LogInformation("[SYNC PROGRESS]: Applying Custom Hypnosis Data if Any!");
        await _overlays.SyncOverlayWithMetaData();

        // 5. Sync Visual Cache with active state.
        Logger.LogInformation("[SYNC PROGRESS]: Syncing Visual Cache With Display");
        await _visuals.SyncServerData(connectionInfo);

        // 6. Update the achievement manager with the latest UID and the latest data.
        Logger.LogInformation($"[SYNC PROGRESS]: Syncing Achievement Data ({MainHub.UID})");
        _achievements.OnServerConnection(connectionInfo.UserAchievements);

        Logger.LogInformation("[SYNC PROGRESS]: Done!");
    }
}
