using GagSpeak.Gui;
using GagSpeak.GameInternals.Detours;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Listeners;
using GagSpeak.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using GagSpeak.UpdateMonitoring.SpatialAudio;

namespace GagSpeak;

/// <summary> The main class for the GagSpeak plugin. </summary>
public class GagSpeakHost : MediatorSubscriberBase, IHostedService
{
    private readonly OnFrameworkService _frameworkUtils;
    private readonly MainConfig _mainConfig;
    private readonly ServerConfigManager _serverConfigs;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IServiceScope? _runtimeServiceScope;
    private Task? _launchTask;
    public GagSpeakHost(ILogger<GagSpeakHost> logger, GagspeakMediator mediator,
        OnFrameworkService frameworkUtils, MainConfig mainConfig,
        ServerConfigManager serverConfigs, IServiceScopeFactory scopeFactory)
        : base(logger, mediator)
    {
        // set the services
        _frameworkUtils = frameworkUtils;
        _mainConfig = mainConfig;
        _serverConfigs = serverConfigs;
        _serviceScopeFactory = scopeFactory;
    }
    /// <summary> 
    /// The task to run after all services have been properly constructed.
    /// This will kickstart the server and begin all operations and verifications.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // set the version to the current assembly version
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        // log our version
        Logger.LogInformation("Launching {name} {major}.{minor}.{build}", "GagSpeak", version.Major, version.Minor, version.Build);

        // subscribe to the login and logout messages
        Svc.ClientState.Login += DalamudUtilOnLogIn;
        Svc.ClientState.Logout += (_, _) => DalamudUtilOnLogOut();

        // subscribe to the main UI message window for making the primary UI be the main UI interface.
        Mediator.Subscribe<SwitchToMainUiMessage>(this, (msg) =>
        {
            if (_launchTask == null || _launchTask.IsCompleted) _launchTask = Task.Run(WaitForPlayerAndLaunchCharacterManager);
        });

        // start processing the mediator queue.
        Mediator.StartQueueProcessing();

        // If already logged in, begin.
        if (PlayerData.IsLoggedIn)
            DalamudUtilOnLogIn();

        // return that the startAsync has been completed.
        return Task.CompletedTask;
    }

    /// <summary> The task to run when the plugin is stopped (called from the disposal)
    public Task StopAsync(CancellationToken cancellationToken)
    {
        UnsubscribeAll();

        DalamudUtilOnLogOut();

        Svc.ClientState.Login -= DalamudUtilOnLogIn;
        Svc.ClientState.Logout -= (_, _) => DalamudUtilOnLogOut();

        Logger.LogDebug("Halting GagspeakPlugin");
        return Task.CompletedTask;
    }

    /// <summary> What to execute whenever the user logs in.
    /// <para>
    /// For our plugin here, it will be to log that we logged in,
    /// And if the launch task is null or was completed, to launch the run task 
    /// </para>
    /// </summary>
    private void DalamudUtilOnLogIn()
    {
        Svc.Logger.Debug("Client login");
        if (_launchTask == null || _launchTask.IsCompleted) _launchTask = Task.Run(WaitForPlayerAndLaunchCharacterManager);
    }

    /// <summary> What to execute whenever the user logs out.
    /// <para>
    /// For our plugin here, it will be to log that we logged out,
    /// And to dispose of the runtime service scope.
    /// </para>
    /// </summary>
    private void DalamudUtilOnLogOut()
    {
        Svc.Logger.Debug("Client logout");
        _runtimeServiceScope?.Dispose();
    }

    /// <summary> The Task executed by the launchTask var from the main plugin.cs 
    /// <para>
    /// This task will await for the player to be present (they are logged in and visible),
    /// then will dispose of the runtime service scope and create a new one to fetch
    /// the required services for our plugin to function as a base level.
    /// </para>
    /// </summary>
    private async Task WaitForPlayerAndLaunchCharacterManager()
    {
        // wait for the player to be present
        while (PlayerData.AvailableThreadSafe is false)
        {
            Svc.Logger.Debug("Waiting for player to be present");
            await Task.Delay(100).ConfigureAwait(false);
        }

        // then launch the managers for the plugin to function at a base level
        try
        {
            Svc.Logger.Debug("Launching Managers");
            // before we do lets recreate the runtime service scope
            _runtimeServiceScope?.Dispose();
            _runtimeServiceScope = _serviceScopeFactory.CreateScope();

            // startup services that have no other services that call on them, yet are essential.
            _runtimeServiceScope.ServiceProvider.GetRequiredService<UiService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<CommandManager>();

            // display changelog if we should.
            if (_mainConfig.Current.LastRunVersion != Assembly.GetExecutingAssembly().GetName().Version!)
            {
                // update the version and toggle the UI.
                Logger?.LogInformation("Version was different, displaying UI");
                _mainConfig.Current.LastRunVersion = Assembly.GetExecutingAssembly().GetName().Version!;
                _mainConfig.Save();
                Mediator.Publish(new UiToggleMessage(typeof(ChangelogUI)));
            }

            // if the client does not have a valid setup or config, switch to the intro ui
            if (!_mainConfig.Current.HasValidSetup() || !_serverConfigs.ServerStorage.HasValidSetup())
            {
                Logger?.LogDebug("Has Valid Setup: {setup} Has Valid Config: {config}", _mainConfig.Current.HasValidSetup(), _serverConfigs.HasValidConfig());
                // publish the switch to intro ui message to the mediator
                _mainConfig.Current.ButtonUsed = false;

                Mediator.Publish(new SwitchToIntroUiMessage());
            }

            // Services that require an initial constructor call during bootup.
            _runtimeServiceScope.ServiceProvider.GetRequiredService<SpellActionService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<EmoteService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<UiFontService>();

            // Init our listeners for IPC.
            _runtimeServiceScope.ServiceProvider.GetRequiredService<CustomizePlusListener>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<GlamourListener>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ModListener>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<MoodleListener>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<PlayerHpListener>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<IntifaceListener>();

            // get the required service for the online player manager (and notification service if we add it)
            _runtimeServiceScope.ServiceProvider.GetRequiredService<DataDistributionService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ConnectionSyncService>();

            // boot up our chat services. (this don't work as hosted services because they are unsafe)
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ChatService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<StaticDetours>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<MovementDetours>();

            // stuff that should probably be a hosted service but isn't yet.
            _runtimeServiceScope.ServiceProvider.GetRequiredService<AchievementsService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<DtrBarService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<NameplateService>();

            // Try and make these not required to initialize:
            //_runtimeServiceScope.ServiceProvider.GetRequiredService<DiscoverService>();
        }
        catch (Exception ex)
        {
            Logger?.LogCritical(ex, "Error during launch of managers");
        }
    }
}
