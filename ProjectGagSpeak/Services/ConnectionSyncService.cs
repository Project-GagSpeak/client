using GagSpeak.PlayerClient;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;

namespace GagSpeak.Services;

/// <summary> A class that helps ensure all client data is synced with the currently connected user.
/// <para> The intention here is to make it so that there is no desync with information between logins </para>
/// </summary>
/// <remarks> Helps update config folder locations, update stored data, and update achievement data status. </remarks>
public sealed class ConnectionSyncService : DisposableMediatorSubscriberBase
{
    private readonly GlobalPermissions _globals;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PuppeteerManager _puppeteer;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    private readonly VisualStateListener _visuals;
    private readonly ConfigFileProvider _fileNames;
    private readonly AchievementListener _achievements;

    public ConnectionSyncService(
        ILogger<ConnectionSyncService> logger,
        GagspeakMediator mediator,
        GlobalPermissions globals,
        ConfigFileProvider fileNames,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        PuppeteerManager puppeteer,
        AlarmManager alarms,
        TriggerManager triggers,
        VisualStateListener visuals,
        AchievementListener achievements)
        : base(logger, mediator)
    {
        _globals = globals;
        _fileNames = fileNames;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _puppeteer = puppeteer;
        _alarms = alarms;
        _triggers = triggers;
        _visuals = visuals;
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
        _fileNames.UpdateConfigs(MainHub.UID);

        Logger.LogInformation($"Loading Configs for [{MainHub.UID}]'s Profile!");
        _gags.Load();
        _restrictions.Load();
        _restraints.Load();
        _cursedLoot.Load();
        _puppeteer.Load();
        _alarms.Load();
        _triggers.Load();

        // 2. Load in the data from the server into our storages.
        Logger.LogInformation("Syncing Data with Connection DTO");
        _globals.ApplyFullDataChange(connectionInfo.GlobalPerms);
        await _visuals.SyncServerData(connectionInfo);

        // 3. Update the achievement manager with the latest UID and the latest data.
        Logger.LogInformation($"Loading Achievement Data for [{MainHub.UID}]");
        _achievements.OnServerConnection(connectionInfo.UserAchievements);

        Logger.LogInformation(">>> All Data is now Syncronized. <<<");
    }
}
