using GagSpeak.Achievements.Services;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.Toybox.Services;
using GagSpeak.UI;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.UpdateMonitoring.Chat.ChatMonitors;
using GagSpeak.UpdateMonitoring.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace GagSpeak;
/// <summary> 
/// The main class for the GagSpeak plugin.
/// <para>
/// I've been looking into better structures for a service collection than otters approach,
/// and ive taken a liking to this one as it gives me an easier understanding for including
/// interfaces and scoped services and hosted services.
/// </para>
/// </summary>

public class GagSpeakHost : MediatorSubscriberBase, IHostedService
{
    private readonly OnFrameworkService _frameworkUtils;
    private readonly ClientMonitorService _clientService;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IServiceScope? _runtimeServiceScope;
    private Task? _launchTask;
    public GagSpeakHost(ILogger<GagSpeak> logger, GagspeakMediator mediator,
        OnFrameworkService frameworkUtils,
        ClientConfigurationManager clientConfigs, ServerConfigurationManager serverConfigs,
        ClientMonitorService clientService, IServiceScopeFactory scopeFactory)
        : base(logger, mediator)
    {
        // Initialize the static logger.
        StaticLogger.Logger = logger;

        // set the services
        _frameworkUtils = frameworkUtils;
        _clientService = clientService;
        _clientConfigs = clientConfigs;
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

        // subscribe to the main UI message window for making the primary UI be the main UI interface.
        Mediator.Subscribe<SwitchToMainUiMessage>(this, (msg) =>
        {
            if (_launchTask == null || _launchTask.IsCompleted) _launchTask = Task.Run(WaitForPlayerAndLaunchCharacterManager);
        });

        // subscribe to the login and logout messages
        Mediator.Subscribe<DalamudLoginMessage>(this, (_) => DalamudUtilOnLogIn());
        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) => DalamudUtilOnLogOut());

        // start processing the mediator queue.
        Mediator.StartQueueProcessing();

        // return that the startAsync has been completed.
        return Task.CompletedTask;
    }

    /// <summary> The task to run when the plugin is stopped (called from the disposal)
    public Task StopAsync(CancellationToken cancellationToken)
    {
        UnsubscribeAll();

        DalamudUtilOnLogOut();

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
        Logger?.LogDebug("Client login");
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
        Logger?.LogDebug("Client logout");
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
        while (!await _clientService.IsPresentAsync().ConfigureAwait(false))
        {
            await Task.Delay(100).ConfigureAwait(false);
        }

        // then launch the managers for the plugin to function at a base level
        try
        {
            Logger?.LogDebug("Launching Managers");
            // before we do lets recreate the runtime service scope
            _runtimeServiceScope?.Dispose();
            _runtimeServiceScope = _serviceScopeFactory.CreateScope();
            // startup services that have no other services that call on them, yet are essential.
            _runtimeServiceScope.ServiceProvider.GetRequiredService<UiService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<CommandManager>();

            // if the client does not have a valid setup or config, switch to the intro ui
            if (!_clientConfigs.GagspeakConfig.HasValidSetup() || !_serverConfigs.HasValidConfig())
            {
                Logger?.LogDebug("Has Valid Setup: {setup} Has Valid Config: {config}", _clientConfigs.GagspeakConfig.HasValidSetup(), _serverConfigs.HasValidConfig());
                // publish the switch to intro ui message to the mediator

                if(_serverConfigs.AuthCount() <= 0)
                {
                    _clientConfigs.GagspeakConfig.ButtonUsed = false;
                }

                Mediator.Publish(new SwitchToIntroUiMessage());
                return;
            }

            // display changelog if we should.
            if (_clientConfigs.GagspeakConfig.LastRunVersion != Assembly.GetExecutingAssembly().GetName().Version!)
            {
                // update the version and toggle the UI.
                Logger?.LogInformation("Version was different, displaying UI");
                _clientConfigs.GagspeakConfig.LastRunVersion = Assembly.GetExecutingAssembly().GetName().Version!;
                Mediator.Publish(new UiToggleMessage(typeof(ChangelogUI)));
            }

            // get the required service for the online player manager (and notification service if we add it)
            _runtimeServiceScope.ServiceProvider.GetRequiredService<CacheCreationService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<AppearanceService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<OnlinePairManager>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<VisiblePairManager>();

            // boot up our chat services. (this don't work as hosted services because they are unsafe)
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ChatBoxMessage>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ChatSender>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ChatInputDetour>();

            // boot up update monitoring services (this don't work as hosted services because they are unsafe)
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ForcedStayCallback>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ActionMonitor>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<MovementMonitor>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<ActionEffectMonitor>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<OnEmote>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<EmoteMonitor>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<TriggerService>();

            // stuff that should probably be a hosted service but isnt yet.
            _runtimeServiceScope.ServiceProvider.GetRequiredService<AchievementsService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<DtrBarService>();
        }
        catch (Exception ex)
        {
            Logger?.LogCritical(ex, "Error during launch of managers");
        }
    }
}

/*
                                 _,..----.._
                               _/  . -.     \_
                              /     \  \      \_
                             /    _  \ _`-.  -. \
                            /  _-'_.  /    \   \ \
                           /  /  /     \-_  \_ /  .
                          /  /  /     / \_\_  '   `.
                         (   `._'    /    \ \_     |
                          \         /      ; |    -.
                          /  /     /       \ |._   |
                         (  / ._.-'         )/ |  ||
                          \`|  ---.  .---. //  ' .'|
                          . \  `-' )  `''  '  /  ' |
                         /  | (   /          // /  '
                         `. |\ \  ._.       // /  /____
                           \|| |\ ____     // '/  /    `-
                            '| \ \`..'  / / .-'  /       \
                             | |  \_  _/ / '( ) |         \
                ___..__      | /    `'  /  `./  \          \
             _-'       `-.   |      /   \   /  / \          .
           _/             `- |  // /   .-  /  /   \         `
          /   _.-           `'.   .-' /     _// /| \_
         /   /        _    )   `./    \ .--'-' / /\_ \       \
        /   /      .-' `-./      |     `-'__.-' /  \\|
       /    |   -\ |      - ._   \  _          '    /'
       |    /  / | |       \  )   -' .-.            \         :
       |   / . | | |   .--.|  /  /  /o |             \        `
       |  / /  | : |   .--.| .  /   \_/               \        \
       / / (   | \ |  `._O'| ! .                       \        .
      // .  `  |  \ \      |.' |                       .        |
      /|  -._  |   \|   )  |   `              /       . \       `
       |     \ |           '  ) \            /        '  .       .
     _/     -._ \  .----. /  /   \._     _.-'        .   \       \
  .-'_-'-,     \ \  `--' /  (     . `---'            '    \       \
 |.-'  _/       \ \     / .-.\  \\ \                /     \        \
 \\   /          ) )-._.'/    `.  \|               |       \  _     )
  \|  /|     _.-'//     /       `-.|               |        -'      |
      |\ \  /    / _.-'/           -.              |        |       |
      |   `-.    \'  .'              \             \        '       '
      \\    `.   |  /                 `.            \      .        '
      /      -  _/                      `.           `.    |        '
      \   _.'  /                          -.          |    |       ,
     / -.     /           _.-               `.        |    |       '
    /    -   _.              `\               -.      `.   |      /
    \ -.   .'                  `._              \      |   !     ,
     |  ._/                       -.             `. .-=\  .'
     |   /._            |           `.             \-'  |.'     /
     |  /,o ;                        |-            _`.--'       ;
     \ .|`.'            |            | `-_      _.'_.          /
     -' |               '            |    `.   (_ .           /
    /   \              /             |      `-_ _' _         /`.
   /     \           .'              |      /(_' _'         .' !
  .       `._     _.'                |     / ( -'_.-'     _.'  |
  (       |  `---'                    \-._'   (._ _.- _.-'      .
  `.      |  \                         \      |: `---'  |       !
    \     |   \                         \     ||        |        .
     `.__.|    \                         \`-._/`.       |        !
          |                               \   \ |       |         .
           \                               \_  \|       |         |
            \                            .-' `. `.      |         `
*/
