using CkCommons;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using GagSpeak.FileSystems;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Handlers;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Gui.Modules.Puppeteer;
using GagSpeak.Gui.Profile;
using GagSpeak.Gui.Publications;
using GagSpeak.Gui.Remote;
using GagSpeak.Gui.Toybox;
using GagSpeak.Gui.UiToybox;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.Kinksters.Factories;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GagSpeak;
public sealed class GagSpeak : IDalamudPlugin
{
    private readonly IHost _host;  // the host builder for the plugin instance. (What makes everything work)
    public GagSpeak(IDalamudPluginInterface pi)
    {
        pi.Create<Svc>();
        // init the CkCommons.
        ItemSvc.Init(pi);
        CkCommonsHost.Init(pi, this);
        // create the host builder for the plugin
        _host = ConstructHostBuilder(pi);
        // start up the host
        _ = _host.StartAsync();
    }

    // Method that creates the host builder for the GagSpeak plugin
    public IHost ConstructHostBuilder(IDalamudPluginInterface pi)
    {
        // create a new host builder for the plugin
        return new HostBuilder()
            // Get the content root for our plugin
            .UseContentRoot(pi.ConfigDirectory.FullName)
            // Configure the logging for the plugin
            .ConfigureLogging((hostContext, loggingBuilder) => GetPluginLogConfiguration(loggingBuilder))
            // Get the plugin service collection for our plugin
            .ConfigureServices((hostContext, serviceCollection) =>
            {
                var services = GetPluginServices(serviceCollection);
                //services.ValidateDependancyInjector();
            }) 
            .Build();

    }

    /// <summary> Gets the log configuration for the plugin. </summary>
    private void GetPluginLogConfiguration(ILoggingBuilder lb)
    {
        // clear our providers, add dalamud logging (the override that integrates ILogger into IPluginLog), and set the minimum level to trace
        lb.ClearProviders();
        lb.AddDalamudLogging();
        lb.SetMinimumLevel(LogLevel.Trace);
    }

    /// <summary> Gets the plugin services for the GagSpeak plugin. </summary>
    public IServiceCollection GetPluginServices(IServiceCollection collection)
    {
        return collection
            // add the general services to the collection
            .AddSingleton(new WindowSystem("GagSpeak"))
            .AddSingleton<FileDialogManager>()
            .AddSingleton<UiFileDialogService>()
            .AddSingleton<UiThumbnailService>()
            .AddSingleton(new Dalamud.Localization("GagSpeak.Localization.", "", useEmbedded: true))
            // add the generic services for GagSpeak
            .AddGagSpeakGeneric()
            // add the services related to the IPC calls for GagSpeak
            .AddGagSpeakIPC()
            // add the services related to the configs for GagSpeak
            .AddGagSpeakConfigs()
            // add the scoped services for GagSpeak
            .AddGagSpeakScoped()
            // add the hosted services for GagSpeak (these should all contain startAsync and stopAsync methods)
            .AddGagSpeakHosted();
    }

    public void Dispose()
    {
        // Stop the host.
        _host.StopAsync().GetAwaiter().GetResult();
        // Dispose of CkCommons.
        CkCommonsHost.Dispose();
        // Dispose of ItemSvc.
        ItemSvc.Dispose();
        // Dispose the Host.
        _host.Dispose();

    }
}

public static class GagSpeakServiceExtensions
{
    #region GenericServices
    public static IServiceCollection AddGagSpeakGeneric(this IServiceCollection services)
    => services
        // Necessary Services
        .AddSingleton<ILoggerProvider, Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider>()
        .AddSingleton<GagSpeakHost>()
        .AddSingleton<EventAggregator>()
        .AddSingleton<GagSpeakLoc>()
        .AddSingleton<GagspeakEventManager>()

        // File System
        .AddSingleton<GagRestrictionFileSelector>()
        .AddSingleton<RestrictionFileSelector>()
        .AddSingleton<RestraintSetFileSelector>()
        .AddSingleton<CollarFileSelector>()
        .AddSingleton<CursedLootFileSelector>()
        .AddSingleton<BuzzToyFileSelector>()
        .AddSingleton<PatternFileSelector>()
        .AddSingleton<AlarmFileSelector>()
        .AddSingleton<TriggerFileSelector>()
        .AddSingleton<ModPresetFileSelector>()
        .AddSingleton<GagFileSystem>()
        .AddSingleton<RestrictionFileSystem>()
        .AddSingleton<RestraintSetFileSystem>()
        .AddSingleton<CollarFileSystem>()
        .AddSingleton<CursedLootFileSystem>()
        .AddSingleton<BuzzToyFileSystem>()
        .AddSingleton<PatternFileSystem>()
        .AddSingleton<AlarmFileSystem>()
        .AddSingleton<TriggerFileSystem>()
        .AddSingleton<ModPresetFileSystem>()

        // Game Internals
        .AddSingleton<StaticDetours>()
        .AddSingleton<MovementDetours>()
        .AddSingleton<ResourceDetours>()

        // MufflerCore
        .AddSingleton<Ipa_EN_FR_JP_SP_Handler>()

        // Player Control
        .AddSingleton<HcTaskManager>()
        .AddSingleton<AutoPromptController>()
        .AddSingleton<BlindfoldService>()
        .AddSingleton<ChatboxController>()
        .AddSingleton<HotbarActionHandler>()
        .AddSingleton<HypnoService>()
        .AddSingleton<KeystateController>()
        .AddSingleton<MovementController>()
        .AddSingleton<POVController>()

        // Player Client
        .AddSingleton<ClientAchievements>()
        .AddSingleton<AchievementEventHandler>()
        .AddSingleton<FavoritesManager>()
        .AddSingleton<HypnoEffectManager>()
        .AddSingleton<ClientData>()
        .AddSingleton<TraitAllowanceManager>()

        // Player Kinkster
        .AddSingleton<KinksterGameObjFactory>()
        .AddSingleton<PairFactory>()
        .AddSingleton<PairHandlerFactory>()
        .AddSingleton<KinksterManager>()

        // Services (Deathroll)
        .AddSingleton<DeathRollService>()

        // Services (Mediator)
        .AddSingleton<GagspeakMediator>()

        // Services (KinkPlates)
        .AddSingleton<KinkPlateFactory>()
        .AddSingleton<KinkPlateService>()

        // Services (Player Control)
        .AddSingleton<BlindfoldService>()
        .AddSingleton<HypnoService>()

        // Services (Textures)
        .AddSingleton<CosmeticService>()

        // Services (Tutorial)
        .AddSingleton<TutorialService>()

        // Services (UI)
        .AddSingleton<UiFontService>()

        // Services [Other]
        .AddSingleton<AchievementsService>()
        .AddSingleton<ArousalService>()
        .AddSingleton<AutoUnlockService>()
        .AddSingleton<ChatService>()
        .AddSingleton<ConnectionSyncService>()
        .AddSingleton<DistributorService>()
        .AddSingleton<KinksterSyncService>()
        .AddSingleton<DtrBarService>()
        .AddSingleton<EmoteService>()
        .AddSingleton<InteractionsService>()
        .AddSingleton<MufflerService>()
        .AddSingleton<NameplateService>()
        .AddSingleton<NotificationService>()
        .AddSingleton<OnFrameworkService>()
        .AddSingleton<RemoteService>()
        .AddSingleton<SafewordService>()
        .AddSingleton<ShareHubService>()
        .AddSingleton<SpellActionService>()
        .AddSingleton<TriggerActionService>()
        .AddSingleton<VibeLobbyDistributionService>()

        // Spatial Audio
        .AddSingleton<VfxSpawnManager>()

        // State (Caches)
        .AddSingleton<CustomizePlusCache>()
        .AddSingleton<GlamourCache>()
        .AddSingleton<ModCache>()
        .AddSingleton<MoodleCache>()
        .AddSingleton<OverlayCache>()
        .AddSingleton<PlayerControlCache>()
        .AddSingleton<TraitsCache>()
        .AddSingleton<SpatialAudioCache>()

        // State (Handlers)
        .AddSingleton<RemoteHandler>()
        .AddSingleton<CustomizePlusHandler>()
        .AddSingleton<GlamourHandler>()
        .AddSingleton<PlayerCtrlHandler>()
        .AddSingleton<LootHandler>()
        .AddSingleton<ModHandler>()
        .AddSingleton<MoodleHandler>()
        .AddSingleton<TriggerHandler>()
        .AddSingleton<TraitsHandler>()
        .AddSingleton<OverlayHandler>()

        // State (Listeners)
        .AddSingleton<CustomizePlusListener>()
        .AddSingleton<GlamourListener>()
        .AddSingleton<ModListener>()
        .AddSingleton<MoodleListener>()
        .AddSingleton<ClientDataListener>()
        .AddSingleton<PlayerHpListener>()
        .AddSingleton<IntifaceListener>()
        .AddSingleton<KinksterListener>()
        .AddSingleton<PuppeteerListener>()
        .AddSingleton<ToyboxStateListener>()
        .AddSingleton<VisualStateListener>()

        // State (Managers)
        .AddSingleton<AlarmManager>()
        .AddSingleton<CacheStateManager>()
        .AddSingleton<CursedLootManager>()
        .AddSingleton<GagRestrictionManager>()
        .AddSingleton<CollarManager>()
        .AddSingleton<ModPresetManager>()
        .AddSingleton<BuzzToyManager>()
        .AddSingleton<VibeLobbyManager>()
        .AddSingleton<PatternManager>()
        .AddSingleton<PuppeteerManager>()
        .AddSingleton<RestraintManager>()
        .AddSingleton<RestrictionManager>()
        .AddSingleton<TriggerManager>()
        .AddSingleton<VfxSpawnManager>()

        // UI (Probably mostly in Scoped)
        .AddSingleton<IdDisplayHandler>()
        .AddSingleton<AccountInfoExchanger>()
        .AddSingleton<GlobalChatLog>()
        .AddSingleton<PopoutGlobalChatlog>()
        .AddSingleton<VibeRoomChatlog>()
        .AddSingleton<MainMenuTabs>()


        // WebAPI (Server stuff)
        .AddSingleton<MainHub>()
        .AddSingleton<HubFactory>()
        .AddSingleton<TokenProvider>()
        .AddSingleton<PiShockProvider>();
    #endregion GenericServices

    public static IServiceCollection AddGagSpeakIPC(this IServiceCollection services)
    => services
        .AddSingleton<IpcCallerCustomize>()
        .AddSingleton<IpcCallerGlamourer>()
        .AddSingleton<IpcCallerHeels>()
        .AddSingleton<IpcCallerHonorific>()
        .AddSingleton<IpcCallerIntiface>()
        .AddSingleton<IpcCallerLifestream>()
        .AddSingleton<IpcCallerMoodles>()
        .AddSingleton<IpcCallerPenumbra>()
        .AddSingleton<IpcCallerPetNames>()
        .AddSingleton<IpcManager>()
        .AddSingleton<IpcProvider>()
        .AddSingleton<PenumbraChangedItemTooltip>();

    public static IServiceCollection AddGagSpeakConfigs(this IServiceCollection services)
    => services
        .AddSingleton<ConfigFileProvider>()
        .AddSingleton<MainConfig>()
        .AddSingleton<ServerConfigService>()
        .AddSingleton<NicknamesConfigService>()
        .AddSingleton<ServerConfigManager>()
        .AddSingleton<HybridSaveService>();

    #region ScopedServices
    public static IServiceCollection AddGagSpeakScoped(this IServiceCollection services)
    => services
        // Scoped Components
        .AddScoped<DrawKinksterRequests>()
        .AddScoped<EquipmentDrawer>()
        .AddScoped<AttributeDrawer>()
        .AddScoped<ModPresetDrawer>()
        .AddScoped<MoodleDrawer>()
        .AddScoped<ActiveItemsDrawer>()
        .AddScoped<AliasItemDrawer>()
        .AddScoped<ListItemDrawer>()
        .AddScoped<TriggerDrawer>()
        .AddScoped<ImageImportTool>()

        // Scoped Factories
        .AddScoped<DrawEntityFactory>()
        .AddScoped<UiFactory>()

        // Scoped Handlers
        .AddScoped<WindowMediatorSubscriberBase, ThumbnailUI>()
        .AddScoped<WindowMediatorSubscriberBase, PopupHandler>()
        .AddScoped<IPopupHandler, VerificationPopupHandler>()
        .AddScoped<IPopupHandler, SavePatternPopupHandler>()
        .AddScoped<IPopupHandler, ReportPopupHandler>()

        // Scoped MainUI (Home)
        .AddScoped<WindowMediatorSubscriberBase, IntroUi>()
        .AddScoped<WindowMediatorSubscriberBase, MainUI>()
        .AddScoped<HomepageTab>()
        .AddScoped<WhitelistTab>()
        .AddScoped<PatternHubTab>()
        .AddScoped<MoodleHubTab>()
        .AddScoped<GlobalChatTab>()
        .AddScoped<AccountTab>()

        // Scoped UI (Wardrobe)
        .AddScoped<WindowMediatorSubscriberBase, WardrobeUI>()
        .AddScoped<RestraintsPanel>()
        .AddScoped<RestraintEditorInfo>()
        .AddScoped<RestraintEditorEquipment>()
        .AddScoped<RestraintEditorLayers>()
        .AddScoped<RestraintEditorModsMoodles>()
        .AddScoped<RestrictionsPanel>()
        .AddScoped<GagRestrictionsPanel>()
        .AddScoped<CursedLootPanel>()

        // Scoped UI (Puppeteer)
        .AddScoped<WindowMediatorSubscriberBase, PuppeteerUI>()
        .AddScoped<PuppetVictimGlobalPanel>()
        .AddScoped<PuppetVictimUniquePanel>()
        .AddScoped<ControllerUniquePanel>()

        // Scoped UI (Toybox)
        .AddScoped<WindowMediatorSubscriberBase, ToyboxUI>()
        .AddScoped<ToysPanel>()
        .AddScoped<VibeLobbiesPanel>()
        .AddScoped<PatternsPanel>()
        .AddScoped<AlarmsPanel>()
        .AddScoped<TriggersPanel>()

        // Scoped UI (Mod Presets)
        .AddScoped<WindowMediatorSubscriberBase, ModPresetsUI>()
        .AddScoped<ModPresetsPanel>()

        // Scoped UI (Trait Allowances Presets)
        .AddScoped<WindowMediatorSubscriberBase, TraitAllowanceUI>()
        .AddScoped<TraitAllowanceSelector>()
        .AddScoped<TraitAllowancePanel>()

        // Scoped UI (Publications)
        .AddScoped<WindowMediatorSubscriberBase, PublicationsUI>()
        .AddScoped<PublicationsManager>()

        // Scoped UI (Achievements)
        .AddScoped<WindowMediatorSubscriberBase, AchievementsUI>()
        .AddScoped<AchievementTabs>()

        // StickyWindow
        .AddScoped<WindowMediatorSubscriberBase, KinksterInteractionsUI>()
        .AddScoped<PresetLogicDrawer>()
        .AddScoped<ClientPermsForKinkster>()
        .AddScoped<KinksterPermsForClient>()
        .AddScoped<KinksterHardcore>()
        .AddScoped<KinksterShockCollar>()

        // Scoped Migrations
        .AddScoped<WindowMediatorSubscriberBase, MigrationsUI>()

        // Scoped Profiles
        .AddScoped<WindowMediatorSubscriberBase, KinkPlatePreviewUI>()
        .AddScoped<WindowMediatorSubscriberBase, PopoutKinkPlateUi>()
        .AddScoped<WindowMediatorSubscriberBase, ProfilePictureEditor>()
        .AddScoped<WindowMediatorSubscriberBase, KinkPlateEditorUI>()
        .AddScoped<KinkPlateLight>()

        // Scoped Remotes
        .AddScoped<WindowMediatorSubscriberBase, BuzzToyRemoteUI>()

        // Scoped Settings
        .AddScoped<WindowMediatorSubscriberBase, SettingsUi>()
        .AddScoped<AccountManagerTab>()
        .AddScoped<DebugTab>()

        // Scoped Misc
        .AddScoped<WindowMediatorSubscriberBase, InteractionEventsUI>()
        .AddScoped<WindowMediatorSubscriberBase, DtrVisibleWindow>()
        .AddScoped<WindowMediatorSubscriberBase, ChangelogUI>()
        .AddScoped<WindowMediatorSubscriberBase, GlobalChatPopoutUI>()
        .AddScoped<WindowMediatorSubscriberBase, DebugStorageUI>()
        .AddScoped<WindowMediatorSubscriberBase, DebugPersonalDataUI>()
        .AddScoped<WindowMediatorSubscriberBase, DebugActiveStateUI>()

        // Scoped Services
        .AddScoped<CommandManager>()
        .AddScoped<TextureService>()
        .AddScoped<UiService>();
    #endregion ScopedServices

    /// <summary>
    ///     Services that must run logic on initialization to help with monitoring.
    ///     If it does not, it can also be an important monitor background service.
    /// </summary>
    /// <remarks> Services that simply monitor actions should be invoked in 'WaitForPlayerAndLaunchCharacterManager' </remarks>
    public static IServiceCollection AddGagSpeakHosted(this IServiceCollection services)
    => services
        .AddHostedService(p => p.GetRequiredService<HybridSaveService>())   // Begins the SaveCycle task loop
        .AddHostedService(p => p.GetRequiredService<CosmeticService>())     // Initializes our required textures so methods can work.
        .AddHostedService(p => p.GetRequiredService<GagspeakMediator>())    // Runs the task for monitoring mediator events.
        .AddHostedService(p => p.GetRequiredService<NotificationService>()) // Important Background Monitor.
        .AddHostedService(p => p.GetRequiredService<OnFrameworkService>())  // Starts & monitors the framework update cycle.

        // Cached Data That MUST be initialized before anything else for validity.
        .AddHostedService(p => p.GetRequiredService<CosmeticService>())     // Provides all Textures nessisary for the plugin.
        .AddHostedService(p => p.GetRequiredService<UiFontService>())       // Provides all fonts nessisary for the plugin.
        .AddHostedService(p => p.GetRequiredService<SpellActionService>())  // Provides all actions nessisary for the plugin.
        .AddHostedService(p => p.GetRequiredService<EmoteService>())        // Provides all emotes nessisary for the plugin.

        .AddHostedService(p => p.GetRequiredService<GagSpeakLoc>())         // Inits Localization with the current language.
        .AddHostedService(p => p.GetRequiredService<EventAggregator>())     // Forcibly calls the constructor, subscribing to the monitors.
        .AddHostedService(p => p.GetRequiredService<IpcProvider>())         // Required for IPC calls to work properly.

        .AddHostedService(p => p.GetRequiredService<CacheStateManager>())   // Manages control over our visual state caches.
        .AddHostedService(p => p.GetRequiredService<MainHub>())             // Required for beyond obvious reasons.
        .AddHostedService(p => p.GetRequiredService<SafewordService>())     // Can never have too many safeguards to ensure this is active.
        .AddHostedService(p => p.GetRequiredService<AchievementsService>()) // Nessisary to begin the task that listens for 

        // Monitors and update providers.
        .AddHostedService(p => p.GetRequiredService<AutoUnlockService>()) // Syncs the client data with the server.

        .AddHostedService(p => p.GetRequiredService<GagSpeakHost>());       // Make this always the final hosted service, initializing the startup.
}

public static class ValidateDependencyInjectorEx
{
    public static void ValidateDependencyInjector(this IServiceCollection services)
    {
        try
        {
            using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,  // Enforce validation on build
                ValidateScopes = false    // Ensure proper scope resolution
            });

            foreach (var service in services)
            {
                var serviceType = service.ServiceType;

                // Skip interfaces and abstract classes
/*                if (serviceType.IsInterface || serviceType.IsAbstract)
                    continue;*/

                var constructor = serviceType.GetConstructors().MaxBy(c => c.GetParameters().Length);
                if (constructor == null)
                    continue;

                var parameters = constructor.GetParameters()
                    .Select(p => serviceProvider.GetService(p.ParameterType))
                    .ToArray();

                // Skip services with unresolvable parameters instead of throwing an error
                if (parameters.Any(p => p == null))
                {
                    Svc.Logger.Warning($"[WARNING] Skipping {serviceType.Name} due to unresolvable parameters.");
                    continue;
                }

                constructor.Invoke(parameters);
            }


        }
        catch (AggregateException ex)
        {
            // join all the inner exception strings together by \n newline.
            var fullException = string.Join("\n\n", ex.InnerExceptions.Select(e => e.Message.ToString()));
            throw new InvalidOperationException(fullException);
        }
        catch (Bagagwa ex)
        {
            throw new InvalidOperationException("ValidateDependencyInjector error detected.", ex);
            // Log the exception to catch any circular dependencies
        }
    }
}
