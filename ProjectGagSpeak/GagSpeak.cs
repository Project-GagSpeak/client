using CkCommons;
using CkCommons.RichText;
using CkCommons.Textures;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.FileSystems;
using GagSpeak.Game.Readers;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Handlers;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Gui.Modules.Puppeteer;
using GagSpeak.Gui.Profile;
using GagSpeak.Gui.Publications;
using GagSpeak.Gui.Toybox;
using GagSpeak.Gui.UiRemote;
using GagSpeak.Gui.UiToybox;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.Kinksters.Factories;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Toybox;
using GagSpeak.UpdateMonitoring.SpatialAudio;
using GagSpeak.WebAPI;
using GagspeakAPI.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak;

// An intenal Static accessor for all DalamudPlugin interfaces, because im tired of interface includes.
// And the difference is neglegable and its basically implied to make them static with the PluginService attribute.

/// <summary>
///     A collection of internally handled Dalamud Interface static services
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class Svc
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] public static IPluginLog Logger { get; set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
    [PluginService] public static IAddonEventManager AddonEventManager { get; private set; }
    [PluginService] public static IAetheryteList AetheryteList { get; private set; }
    //[PluginService] public static ITitleScreenMenu TitleScreenMenu { get; private set; } = null!;
    //[PluginService] public static IBuddyList Buddies { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; set; } = null!;
    [PluginService] public static IClientState ClientState { get; set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; }
    [PluginService] public static ICondition Condition { get; private set; }
    [PluginService] public static IContextMenu ContextMenu { get; private set; }
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] public static IDutyState DutyState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    //[PluginService] public static IGameInventory GameInventory { get; private set; } = null!;
    //[PluginService] public static IGameNetwork GameNetwork { get; private set; } = null!;
    //[PluginService] public static IJobGauges Gauges { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IGameLifecycle GameLifeCycle { get; private set; } = null!;
    [PluginService] public static IGamepadState GamepadState { get; private set; } = null!;
    [PluginService] public static IKeyState KeyState { get; private set; } = null!;
    [PluginService] public static INotificationManager Notifications { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] public static IPartyList Party { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static ITargetManager Targets { get; private set; } = null!;
    [PluginService] public static ITextureProvider Texture { get; private set; } = null!;
    // [PluginService] public static IToastGui Toasts { get; private set; } = null!;
    // [PluginService] public static ITextureSubstitutionProvider TextureSubstitution { get; private set; } = null!;
}



public sealed class GagSpeak : IDalamudPlugin
{
    private readonly IHost _host;  // the host builder for the plugin instance. (What makes everything work)
    public GagSpeak(IDalamudPluginInterface pi)
    {
        pi.Create<Svc>();
        // init the CkCommons.
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
        // Dispose the Host.
        _host.Dispose();

    }
}

public static class GagSpeakServiceExtensions
{
    #region GenericServices
    public static IServiceCollection AddGagSpeakGeneric(this IServiceCollection services)
    => services
        // Nessisary Services
        .AddSingleton<ILoggerProvider, Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider>()
        .AddSingleton<GagSpeakHost>()
        .AddSingleton<EventAggregator>()
        .AddSingleton<GagSpeakLoc>()
        .AddSingleton<GagspeakEventManager>()

        // File System
        .AddSingleton<GagRestrictionFileSelector>()
        .AddSingleton<RestrictionFileSelector>()
        .AddSingleton<RestraintSetFileSelector>()
        .AddSingleton<CursedLootFileSelector>()
        .AddSingleton<PatternFileSelector>()
        .AddSingleton<AlarmFileSelector>()
        .AddSingleton<TriggerFileSelector>()
        .AddSingleton<GagFileSystem>()
        .AddSingleton<RestrictionFileSystem>()
        .AddSingleton<RestraintSetFileSystem>()
        .AddSingleton<CursedLootFileSystem>()
        .AddSingleton<PatternFileSystem>()
        .AddSingleton<AlarmFileSystem>()
        .AddSingleton<TriggerFileSystem>()

        // Game Internals
        .AddSingleton<MovementDetours>()
        .AddSingleton<StaticDetours>()

        // Game Monitors
        .AddSingleton<SelectStringPrompt>()
        .AddSingleton<YesNoPrompt>()
        .AddSingleton<RoomSelectPrompt>()

        // MufflerCore
        .AddSingleton<Ipa_EN_FR_JP_SP_Handler>()
        .AddSingleton<MufflerService>()

        // Player Client
        .AddSingleton<ClientAchievements>()
        .AddSingleton<AchievementEventHandler>()
        .AddSingleton<FavoritesManager>()
        .AddSingleton<GlobalPermissions>()
        .AddSingleton<KinksterRequests>()
        .AddSingleton<TraitAllowanceManager>()

        // Player Kinkster
        .AddSingleton<GameObjectHandlerFactory>()
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
        .AddSingleton<AutoPromptController>()
        .AddSingleton<BlindfoldService>()
        .AddSingleton<ChatboxController>()
        .AddSingleton<HotbarActionController>()
        .AddSingleton<HypnoService>()
        .AddSingleton<KeystateController>()
        .AddSingleton<MovementController>()
        .AddSingleton<OverlayController>()

        // Services (Textures)
        .AddSingleton<CosmeticService>()

        // Services (Tutorial)
        .AddSingleton<TutorialService>()

        // Services (UI)
        .AddSingleton<UiFontService>()

        // Services [Other]
        .AddSingleton<AchievementsService>()
        .AddSingleton<ArousalService>()
        .AddSingleton<ChatService>()
        .AddSingleton<ConnectionSyncService>()
        .AddSingleton<DataDistributionService>()
        .AddSingleton<DiscoverService>()
        .AddSingleton<DtrBarService>()
        .AddSingleton<EmoteService>()
        .AddSingleton<ItemService>()
        .AddSingleton<MufflerService>()
        .AddSingleton<NotificationService>()
        .AddSingleton<OnFrameworkService>()
        .AddSingleton<SafewordService>()
        .AddSingleton<ShareHubService>()
        .AddSingleton<SpellActionService>()
        .AddSingleton<TriggerActionService>()

        // Spatial Audio
        .AddSingleton<ResourceLoader>()
        .AddSingleton<AvfxManager>()
        .AddSingleton<ScdManager>()
        .AddSingleton<VfxSpawns>()

        // State (Controllers)
        .AddSingleton<IntifaceController>()

        // State (Caches)
        .AddSingleton<CustomizePlusCache>()
        .AddSingleton<GlamourCache>()
        .AddSingleton<ModCache>()
        .AddSingleton<MoodleCache>()
        .AddSingleton<TraitsCache>()
        .AddSingleton<OverlayCache>()

        // State (Handlers)
        .AddSingleton<CustomizePlusHandler>()
        .AddSingleton<GlamourHandler>()
        .AddSingleton<HardcoreHandler>()
        .AddSingleton<LootHandler>()
        .AddSingleton<ModHandler>()
        .AddSingleton<MoodleHandler>()
        .AddSingleton<PatternHandler>()
        .AddSingleton<TriggerHandler>()
        .AddSingleton<TraitsHandler>()
        .AddSingleton<OverlayHandler>()

        // State (Listeners)
        .AddSingleton<CustomizePlusListener>()
        .AddSingleton<GlamourListener>()
        .AddSingleton<ModListener>()
        .AddSingleton<MoodleListener>()
        .AddSingleton<PlayerHpListener>()
        .AddSingleton<PuppeteerListener>()
        .AddSingleton<ToyboxStateListener>()
        .AddSingleton<VisualStateListener>()

        // State (Managers)
        .AddSingleton<AlarmManager>()
        .AddSingleton<CacheStateManager>()
        .AddSingleton<CursedLootManager>()
        .AddSingleton<GagRestrictionManager>()
        .AddSingleton<ModSettingPresetManager>()
        .AddSingleton<OwnGlobalsManager>()
        .AddSingleton<PatternManager>()
        .AddSingleton<PuppeteerManager>()
        .AddSingleton<RestraintManager>()
        .AddSingleton<RestrictionManager>()
        .AddSingleton<TriggerManager>()

        // Toybox
        .AddSingleton<VibeRoomManager>()
        .AddSingleton<SexToyManager>()
        .AddSingleton<ToyboxFactory>()
        .AddSingleton<VibeSimAudio>()

        // UI (Probably mostly in Scoped)
        .AddSingleton<HypnoEffectEditor>()
        .AddSingleton<IdDisplayHandler>()
        .AddSingleton<AccountInfoExchanger>()

        // WebAPI (Server stuff)
        .AddSingleton<MainHub>()
        .AddSingleton<HubFactory>()
        .AddSingleton<TokenProvider>()
        .AddSingleton<PiShockProvider>()

        // Penumbra.GameData pain. (I hate how this has to make the injection messy ;-;)
        .AddSingleton<ItemData>()
        .AddSingleton((s) => new DictBonusItems(Svc.PluginInterface, new Logger(), Svc.Data))
        .AddSingleton((s) => new DictStain(Svc.PluginInterface, new Logger(), Svc.Data))
        .AddSingleton((s) => new ItemsByType(Svc.PluginInterface, new Logger(), Svc.Data, s.GetRequiredService<DictBonusItems>()))
        .AddSingleton((s) => new ItemsPrimaryModel(Svc.PluginInterface, new Logger(), Svc.Data, s.GetRequiredService<ItemsByType>()))
        .AddSingleton((s) => new ItemsSecondaryModel(Svc.PluginInterface, new Logger(), Svc.Data, s.GetRequiredService<ItemsByType>()))
        .AddSingleton((s) => new ItemsTertiaryModel(Svc.PluginInterface, new Logger(), Svc.Data, s.GetRequiredService<ItemsByType>(), s.GetRequiredService<ItemsSecondaryModel>()));
    #endregion GenericServices

    public static IServiceCollection AddGagSpeakIPC(this IServiceCollection services)
    => services
        .AddSingleton<IpcCallerMare>()
        .AddSingleton<IpcCallerMoodles>()
        .AddSingleton<IpcCallerPenumbra>()
        .AddSingleton<IpcCallerGlamourer>()
        .AddSingleton<IpcCallerCustomize>()
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
        .AddScoped<DrawRequests>()
        .AddScoped<EquipmentDrawer>()
        .AddScoped<AttributeDrawer>()
        .AddScoped<ModPresetDrawer>()
        .AddScoped<MoodleDrawer>()
        .AddScoped<PlaybackDrawer>()
        .AddScoped<ActiveItemsDrawer>()
        .AddScoped<AliasItemDrawer>()
        .AddScoped<PuppeteerHelper>()
        .AddScoped<TriggerDrawer>()
        .AddScoped<ImageImportTool>()

        // Scoped Factories
        .AddScoped<DrawEntityFactory>()
        .AddScoped<UiFactory>()

        // Scoped Handlers
        .AddScoped<UserPairListHandler>()
        .AddScoped<WindowMediatorSubscriberBase, PopupHandler>()
        .AddScoped<IPopupHandler, VerificationPopupHandler>()
        .AddScoped<IPopupHandler, SavePatternPopupHandler>()
        .AddScoped<IPopupHandler, ReportPopupHandler>()

        // Scoped MainUI (Home)
        .AddScoped<WindowMediatorSubscriberBase, IntroUi>()
        .AddScoped<WindowMediatorSubscriberBase, MainUI>()
        .AddScoped<MainMenuTabs>()
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
        .AddScoped<PatternsPanel>()
        .AddScoped<AlarmsPanel>()
        .AddScoped<TriggersPanel>()

        // Scoped UI (Mod Presets)
        .AddScoped<WindowMediatorSubscriberBase, ModPresetsUI>()
        .AddScoped<ModPresetSelector>()
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
        .AddScoped<PresetLogicDrawer>()

        // Scoped Migrations
        .AddScoped<WindowMediatorSubscriberBase, MigrationsUI>()

        // Scoped Profiles
        .AddScoped<WindowMediatorSubscriberBase, KinkPlatePreviewUI>()
        .AddScoped<WindowMediatorSubscriberBase, PopoutKinkPlateUi>()
        .AddScoped<WindowMediatorSubscriberBase, ProfilePictureEditor>()
        .AddScoped<WindowMediatorSubscriberBase, KinkPlateEditorUI>()
        .AddScoped<KinkPlateLight>()

        // Scoped Remotes
        .AddScoped<WindowMediatorSubscriberBase, RemotePersonal>()
        .AddScoped<WindowMediatorSubscriberBase, RemotePatternMaker>()

        // Scoped Settings
        .AddScoped<WindowMediatorSubscriberBase, SettingsUi>()
        .AddScoped<SettingsHardcore>()
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

    #region HostedServices
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

        .AddHostedService(p => p.GetRequiredService<GagSpeakHost>());       // Make this always the final hosted service, initializing the startup.
    #endregion HostedServices
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
        catch (Exception ex)
        {
            throw new InvalidOperationException("ValidateDependencyInjector error detected.", ex);
            // Log the exception to catch any circular dependencies
        }
    }
}
