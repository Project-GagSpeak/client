using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.Achievements;
using GagSpeak.Achievements.Services;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Hardcore.Movement;
using GagSpeak.Interop;
using GagSpeak.Interop.Ipc;
using GagSpeak.Interop.IpcHelpers.Penumbra;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Factories;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Services;
using GagSpeak.PlayerState.Listener;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.StateManagers;
using GagSpeak.Toybox.Data;
using GagSpeak.Toybox.Services;
using GagSpeak.Toybox.SimulatedVibe;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UI.Components.Popup;
using GagSpeak.UI.Components.UserPairList;
using GagSpeak.UI.Handlers;
using GagSpeak.UI.MainWindow;
using GagSpeak.UI.Orders;
using GagSpeak.UI.Profile;
using GagSpeak.UI.UiGagSetup;
using GagSpeak.UI.UiOrders;
using GagSpeak.UI.UiPublications;
using GagSpeak.UI.UiPuppeteer;
using GagSpeak.UI.UiRemote;
using GagSpeak.UI.UiToybox;
using GagSpeak.UI.Wardrobe;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.UpdateMonitoring.Chat.ChatMonitors;
using GagSpeak.UpdateMonitoring.SpatialAudio.Loaders;
using GagSpeak.UpdateMonitoring.SpatialAudio.Managers;
using GagSpeak.UpdateMonitoring.SpatialAudio.Spawner;
using GagSpeak.UpdateMonitoring.Triggers;
using GagSpeak.WebAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;

namespace GagSpeak;

public sealed class GagSpeak : IDalamudPlugin
{
    private readonly IHost _host;  // the host builder for the plugin instance. (What makes everything work)

    // This is useful for classes that should be static for for reasons out of my control are not.
    // A little hack workaround for dealing with things like Glamourer's ItemData not being static and needing
    // to be used everywhere, even though its only created once as singleton and should behave like a static.
    // This stores a reference to the _host, so should not be storing duplicate of the data.
    public static IServiceProvider ServiceProvider { get; private set; }

    public GagSpeak(IDalamudPluginInterface pi, IPluginLog pluginLog, IAddonLifecycle addonLifecycle,
        IChatGui chatGui, IClientState clientState, ICommandManager commandManager, ICondition condition,
        IContextMenu contextMenu, IDataManager dataManager, IDtrBar dtrBar, IDutyState dutyState,
        IFramework framework, IGameGui gameGui, IGameInteropProvider gameInteropProvider, IGamepadState controllerBinds,
        IKeyState keyState, INotificationManager notificationManager, IObjectTable objectTable, IPartyList partyList,
        ISigScanner sigScanner, ITargetManager targetManager, ITextureProvider textureProvider)
    {
        // create the host builder for the plugin
        _host = ConstructHostBuilder(pi, pluginLog, addonLifecycle, chatGui, clientState, commandManager, condition,
            contextMenu, dataManager, dtrBar, dutyState, framework, gameGui, gameInteropProvider, controllerBinds,
            keyState, notificationManager, objectTable, partyList, sigScanner, targetManager, textureProvider);

        // store the service provider for the plugin
        ServiceProvider = _host.Services;

        // start up the host
        _ = _host.StartAsync();
    }

    // Method that creates the host builder for the GagSpeak plugin
    public IHost ConstructHostBuilder(IDalamudPluginInterface pi, IPluginLog pl, IAddonLifecycle alc, IChatGui cg,
        IClientState cs, ICommandManager cm, ICondition con, IContextMenu cmu, IDataManager dm, IDtrBar bar,
        IDutyState ds, IFramework fw, IGameGui gg, IGameInteropProvider gip, IGamepadState gps, IKeyState ks, INotificationManager nm,
        IObjectTable ot, IPartyList plt, ISigScanner ss, ITargetManager tm, ITextureProvider tp)
    {
        // create a new host builder for the plugin
        return new HostBuilder()
            // get the content root for our plugin
            .UseContentRoot(GetPluginContentRoot(pi))
            // configure the logging for the plugin
            .ConfigureLogging((hostContext, loggingBuilder) => GetPluginLogConfiguration(loggingBuilder, pl))
            // get the plugin service collection for our plugin
            .ConfigureServices((hostContext, serviceCollection)
            => GetPluginServices(serviceCollection, pi, pl, alc, cg, cs, cm, con, cmu, dm, bar, ds, fw, gg, gip, gps, ks, nm, ot, plt, ss, tm, tp))
            // Build the host builder so it becomes an IHost object.
            .Build();
    }
    /// <summary> Gets the folder content location to know where the config files are saved. </summary>
    private string GetPluginContentRoot(IDalamudPluginInterface pi) => pi.ConfigDirectory.FullName;

    /// <summary> Gets the log configuration for the plugin. </summary>
    private void GetPluginLogConfiguration(ILoggingBuilder lb, IPluginLog pluginLog)
    {
        // clear our providers, add dalamud logging (the override that integrates ILogger into IPluginLog), and set the minimum level to trace
        lb.ClearProviders();
        lb.AddDalamudLogging(pluginLog);
        lb.SetMinimumLevel(LogLevel.Trace);
    }

    /// <summary> Gets the plugin services for the GagSpeak plugin. </summary>
    private IServiceCollection GetPluginServices(IServiceCollection collection, IDalamudPluginInterface pi, IPluginLog pl,
        IAddonLifecycle alc, IChatGui cg, IClientState cs, ICommandManager cm, ICondition con, IContextMenu cmu, IDataManager dm,
        IDtrBar bar, IDutyState ds, IFramework fw, IGameGui gg, IGameInteropProvider gip, IGamepadState gps, IKeyState ks,
        INotificationManager nm, IObjectTable ot, IPartyList plt, ISigScanner ss, ITargetManager tm, ITextureProvider tp)
    {
        return collection
            // add the general services to the collection
            .AddSingleton(new WindowSystem("GagSpeak"))
            .AddSingleton<FileDialogManager>()
            .AddSingleton(new Dalamud.Localization("GagSpeak.Localization.", "", useEmbedded: true))
            // add the generic services for GagSpeak
            .AddGagSpeakGeneric(pi, alc, cs, cg, con, cmu, dm, bar, ds, fw, ks, gg, gip, gps, nm, ot, plt, ss, tm, tp)
            // add the services related to the IPC calls for GagSpeak
            .AddGagSpeakIPC(pi, cs)
            // add the services related to the configs for GagSpeak
            .AddGagSpeakConfigs(pi)
            // add the scoped services for GagSpeak
            .AddGagSpeakScoped(cs, cm, pi, tp, nm, cg, dm)
            // add the hosted services for GagSpeak (these should all contain startAsync and stopAsync methods)
            .AddGagSpeakHosted();
    }

    public void Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}

public static class GagSpeakServiceExtensions
{
    #region GenericServices
    public static IServiceCollection AddGagSpeakGeneric(this IServiceCollection services, IDalamudPluginInterface pi,
        IAddonLifecycle alc, IClientState cs, IChatGui cg, ICondition con, IContextMenu cm, IDataManager dm, IDtrBar dtr,
        IDutyState ds, IFramework fw, IKeyState ks, IGameGui gg, IGameInteropProvider gip, IGamepadState gps,
        INotificationManager nm, IObjectTable ot, IPartyList pl, ISigScanner ss, ITargetManager tm, ITextureProvider tp)
    => services
        // Events Services
        .AddSingleton((s) => new EventAggregator(pi.ConfigDirectory.FullName, s.GetRequiredService<ILogger<EventAggregator>>(), 
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<PairManager>()))
        .AddSingleton((s) => new GagSpeakLoc(s.GetRequiredService<ILogger<GagSpeakLoc>>(), s.GetRequiredService<Dalamud.Localization>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<TutorialService>(), pi))

        // MufflerCore
        .AddSingleton((s) => new GagDataHandler(s.GetRequiredService<ILogger<GagDataHandler>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GagspeakConfigService>(), pi))
        .AddSingleton((s) => new Ipa_EN_FR_JP_SP_Handler(s.GetRequiredService<ILogger<Ipa_EN_FR_JP_SP_Handler>>(),
            s.GetRequiredService<GagspeakConfigService>(), pi))

        // Chat Services
        .AddSingleton((s) => new ChatMonitor(s.GetRequiredService<ILogger<ChatMonitor>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<GlobalData>(), s.GetRequiredService<ChatSender>(),
            s.GetRequiredService<PuppeteerManager>(), s.GetRequiredService<MiscellaneousListener>(), s.GetRequiredService<ClientMonitor>(),
            s.GetRequiredService<DeathRollService>(), cg))
        .AddSingleton((s) => new ChatSender(ss))
        .AddSingleton((s) => new ChatInputDetour(s.GetRequiredService<ILogger<ChatInputDetour>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<GagGarbler>(), s.GetRequiredService<EmoteMonitor>(), ss, gip))

        // Hardcore services.
        .AddSingleton((s) => new SelectStringPrompt(s.GetRequiredService<ILogger<SelectStringPrompt>>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<ForcedStayCallback>(), alc, gip, tm))
        .AddSingleton((s) => new YesNoPrompt(s.GetRequiredService<ILogger<YesNoPrompt>>(), s.GetRequiredService<GagspeakConfigService>(), alc, tm))
        .AddSingleton((s) => new RoomSelectPrompt(s.GetRequiredService<ILogger<RoomSelectPrompt>>(), s.GetRequiredService<GagspeakConfigService>(), alc, tm))
        .AddSingleton<SettingsHardcore>()

        // PlayerData Services
        .AddSingleton<GagGarbler>()
        .AddSingleton((s) => new HardcoreHandler(s.GetRequiredService<ILogger<HardcoreHandler>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<MoveController>(), s.GetRequiredService<ChatSender>(), s.GetRequiredService<EmoteMonitor>(), tm))
        .AddSingleton<GlobalData>()
        .AddSingleton<GameObjectHandlerFactory>()
        .AddSingleton<PairFactory>()
        .AddSingleton<PairHandlerFactory>()
        .AddSingleton((s) => new PairManager(s.GetRequiredService<ILogger<PairManager>>(), s.GetRequiredService<GagspeakMediator>(), 
            s.GetRequiredService<PairFactory>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<ServerConfigurationManager>(), cm))
        .AddSingleton<OnConnectedService>()

        // Toybox Services
        .AddSingleton<ButtPlugDevice>()
        .AddSingleton<ToyboxFactory>()
        .AddSingleton((s) => new DeathRollService(s.GetRequiredService<ILogger<DeathRollService>>(), s.GetRequiredService<GlobalData>(),
            s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<TriggerManager>(), s.GetRequiredService<TriggerApplier>(), cg))

        // Unlocks / Achievements
        .AddSingleton((s) => new AchievementManager(s.GetRequiredService<ILogger<AchievementManager>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>(),
            s.GetRequiredService<CosmeticService>(), s.GetRequiredService<KinkPlateService>(), s.GetRequiredService<SexToyManager>(), s.GetRequiredService<UnlocksEventManager>(), nm, ds))
        .AddSingleton<UnlocksEventManager>()

        // UpdateMonitoring Services
        .AddSingleton((s) => new ClientMonitor(s.GetRequiredService<ILogger<ClientMonitor>>(), s.GetRequiredService<GagspeakMediator>(), cs, con, dm, fw, gg, pl))
        .AddSingleton((s) => new ActionMonitor(s.GetRequiredService<ILogger<ActionMonitor>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<TraitsManager>(), s.GetRequiredService<HardcoreHandler>(), s.GetRequiredService<ClientMonitor>(), gip))

        .AddSingleton((s) => new MovementMonitor(s.GetRequiredService<ILogger<MovementMonitor>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<HardcoreHandler>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<SelectStringPrompt>(), s.GetRequiredService<YesNoPrompt>(), s.GetRequiredService<RoomSelectPrompt>(),
            s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<EmoteMonitor>(), s.GetRequiredService<MoveController>(), ks, ot, tm))
        .AddSingleton((s) => new MoveController(s.GetRequiredService<ILogger<MoveController>>(), gip, ot))
        .AddSingleton((s) => new ForcedStayCallback(s.GetRequiredService<ILogger<ForcedStayCallback>>(), s.GetRequiredService<GagspeakConfigService>(), ss, gip))

        .AddSingleton((s) => new ResourceLoader(s.GetRequiredService<ILogger<ResourceLoader>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<AvfxManager>(), s.GetRequiredService<ScdManager>(), dm, ss, gip))
        .AddSingleton((s) => new AvfxManager(s.GetRequiredService<ILogger<AvfxManager>>(), dm, pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new ScdManager(s.GetRequiredService<ILogger<ScdManager>>(), dm, pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new VfxSpawns(s.GetRequiredService<ILogger<VfxSpawns>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ResourceLoader>(), cs, tm))

        .AddSingleton((s) => new ActionEffectMonitor(s.GetRequiredService<ILogger<ActionEffectMonitor>>(),
            s.GetRequiredService<GagspeakConfigService>(), ss, gip))
        .AddSingleton((s) => new OnEmote(s.GetRequiredService<ILogger<OnEmote>>(), s.GetRequiredService<HardcoreHandler>(), s.GetRequiredService<OnFrameworkService>(), ss, gip))
        .AddSingleton((s) => new EmoteMonitor(s.GetRequiredService<ILogger<EmoteMonitor>>(), s.GetRequiredService<ClientMonitor>(), dm))

        .AddSingleton((s) => new DtrBarService(s.GetRequiredService<ILogger<DtrBarService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<EventAggregator>(),
            s.GetRequiredService<PairManager>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<ClientMonitor>(), dm, dtr))

        // Utilities Services
        .AddSingleton<ILoggerProvider, Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider>()
        .AddSingleton<StaticLoggerInit>()
        .AddSingleton<GagSpeakHost>()
        .AddSingleton<KinkPlateFactory>()

        // Register ItemData and everything else because apparently dependancy injection hates Penumbra.GameData???
        .AddSingleton<ItemData>()
        .AddSingleton((s) => new DictBonusItems(pi, new Logger(), dm))
        .AddSingleton((s) => new DictStain(pi, new Logger(), dm))
        .AddSingleton((s) => new ItemsByType(pi, new Logger(), dm, s.GetRequiredService<DictBonusItems>()))
        .AddSingleton((s) => new ItemsPrimaryModel(pi, new Logger(), dm, s.GetRequiredService<ItemsByType>()))
        .AddSingleton((s) => new ItemsSecondaryModel(pi, new Logger(), dm, s.GetRequiredService<ItemsByType>()))
        .AddSingleton((s) => new ItemsTertiaryModel(pi, new Logger(), dm, s.GetRequiredService<ItemsByType>(), s.GetRequiredService<ItemsSecondaryModel>()))


        // UI Helpers
        .AddSingleton<MainMenuTabs>()
        .AddSingleton<AchievementTabs>()
        /*
         *     public AccountsTab(ILogger<AccountsTab> logger, GagspeakMediator mediator, MainHub hub,
        GagspeakConfigService mainConfig, ServerConfigurationManager serverConfigs,
        ConfigFileProvider configDirectory, ClientMonitor clientMonitor, UiSharedService uiShared)
         */
        .AddSingleton((s) => new AccountsTab(s.GetRequiredService<ILogger<AccountsTab>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<ServerConfigurationManager>(),
            s.GetRequiredService<ConfigFileProvider>(), s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<UiSharedService>()))
        .AddSingleton<DebugTab>()
        .AddSingleton((s) => new AccountInfoExchanger(pi.ConfigDirectory.FullName))

        // UI general services
        .AddSingleton<ActiveGagsPanel>()
        .AddSingleton<PuppeteerComponents>()

        // Toybox UI
        .AddSingleton<ToyboxOverview>()
        .AddSingleton<ToyboxPatterns>()
        .AddSingleton<ToyboxVibeRooms>()
        .AddSingleton<ToyboxTriggerManager>()
        .AddSingleton<ToyboxAlarmManager>()
        .AddSingleton((s) => new VibeSimAudio(s.GetRequiredService<ILogger<VibeSimAudio>>(), pi))

        // Orders UI
        .AddSingleton<OrdersViewActive>()
        .AddSingleton<OrdersCreator>()
        .AddSingleton<OrdersAssigner>()

        // Publications UI
        .AddSingleton<PublicationsManager>()

        // UI Components
        .AddSingleton<PairCombos>()
        .AddSingleton<IdDisplayHandler>()
        .AddSingleton<KinkPlateLight>()
        .AddSingleton<UserPairListHandler>()
        .AddSingleton<DrawRequests>()
        .AddSingleton<HomepageTab>()
        .AddSingleton<WhitelistTab>()
        .AddSingleton<PatternHubTab>()
        .AddSingleton<MoodleHubTab>()
        .AddSingleton<GlobalChatTab>()
        .AddSingleton((s) => new AccountTab(s.GetRequiredService<ILogger<AccountTab>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<UiSharedService>(), s.GetRequiredService<OnFrameworkService>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<KinkPlateService>(),
            s.GetRequiredService<TutorialService>(), pi))

        // WebAPI Services
        .AddSingleton<MainHub>()
        .AddSingleton<HubFactory>()
        .AddSingleton<TokenProvider>()
        .AddSingleton<PiShockProvider>()

        // Service Services
        .AddSingleton<TutorialService>()
        .AddSingleton<Tutorial>()
        .AddSingleton<AchievementsService>()
        .AddSingleton((s) => new CursedLootMonitor(s.GetRequiredService<ILogger<CursedLootMonitor>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<GagRestrictionManager>(),
            s.GetRequiredService<CursedLootManager>(), s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>(), gip))
        .AddSingleton<SafewordService>()
        .AddSingleton<SexToyManager>()
        .AddSingleton<GagspeakConfigService>()
        .AddSingleton<ServerConfigurationManager>()
        .AddSingleton((s) => new UiFontService(pi))
        .AddSingleton<GagspeakMediator>()
        .AddSingleton((s) => new DiscoverService(pi.ConfigDirectory.FullName, s.GetRequiredService<ILogger<DiscoverService>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(), s.GetRequiredService<MainMenuTabs>(), 
            s.GetRequiredService<PairManager>(), s.GetRequiredService<CosmeticService>()))
        .AddSingleton<ShareHubService>()
        .AddSingleton<PermissionPresetLogic>()
        .AddSingleton((s) => new KinkPlateService(s.GetRequiredService<ILogger<KinkPlateService>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<KinkPlateFactory>()))
        .AddSingleton((s) => new OnFrameworkService(s.GetRequiredService<ILogger<OnFrameworkService>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ClientMonitor>(), dm, fw, ot, tm))
        .AddSingleton((s) => new CosmeticService(s.GetRequiredService<ILogger<CosmeticService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<OnFrameworkService>(), pi, tp))
        .AddSingleton((s) => new NotificationService(s.GetRequiredService<ILogger<NotificationService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<GlobalData>(), cg, nm));
    #endregion GenericServices

    #region IpcServices
    public static IServiceCollection AddGagSpeakIPC(this IServiceCollection services, IDalamudPluginInterface pi, IClientState cs)
    => services
        .AddSingleton((s) => new IpcCallerMare(s.GetRequiredService<ILogger<IpcCallerMare>>(), pi,
            s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<GagspeakMediator>()))
        .AddSingleton((s) => new IpcCallerMoodles(s.GetRequiredService<ILogger<IpcCallerMoodles>>(), pi,
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>()))
        .AddSingleton((s) => new IpcCallerPenumbra(s.GetRequiredService<ILogger<IpcCallerPenumbra>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<OnFrameworkService>(), pi))
        .AddSingleton((s) => new IpcCallerGlamourer(s.GetRequiredService<ILogger<IpcCallerGlamourer>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GlobalData>(), 
            s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>(), pi))
        .AddSingleton((s) => new IpcCallerCustomize(s.GetRequiredService<ILogger<IpcCallerCustomize>>(), s.GetRequiredService<GagspeakMediator>(), pi))
        .AddSingleton((s) => new IpcManager(s.GetRequiredService<ILogger<IpcManager>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<IpcCallerCustomize>(),
            s.GetRequiredService<IpcCallerGlamourer>(), s.GetRequiredService<IpcCallerPenumbra>(),
            s.GetRequiredService<IpcCallerMoodles>(), s.GetRequiredService<IpcCallerMare>()))

        .AddSingleton((s) => new IpcProvider(s.GetRequiredService<ILogger<IpcProvider>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<PairManager>(),
            s.GetRequiredService<OnFrameworkService>(), pi))
        .AddSingleton((s) => new PenumbraChangedItemTooltip(s.GetRequiredService<ILogger<PenumbraChangedItemTooltip>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<IpcCallerPenumbra>(), cs,
            s.GetRequiredService<ItemData>()));

    #endregion IpcServices
    #region ConfigServices
    public static IServiceCollection AddGagSpeakConfigs(this IServiceCollection services, IDalamudPluginInterface pi)
    => services
        // client-end configs
        .AddSingleton((s) => new GagspeakConfigService(s.GetRequiredService<HybridSaveService>()));
        // the rest of the config stuff here is not migrated into other parts so see about how we will sort this later.

    #endregion ConfigServices
    #region ScopedServices
    public static IServiceCollection AddGagSpeakScoped(this IServiceCollection services, IClientState cs,
        ICommandManager cm, IDalamudPluginInterface pi, ITextureProvider tp, INotificationManager nm,
        IChatGui cg, IDataManager dm)
    => services
        // Service Services
        .AddScoped<DrawEntityFactory>()
        .AddScoped<UiFactory>()
        .AddScoped<WindowMediatorSubscriberBase, SettingsUi>()
        .AddScoped<WindowMediatorSubscriberBase, IntroUi>()
        .AddScoped<WindowMediatorSubscriberBase, KinkPlatePreviewUI>()
        .AddScoped<WindowMediatorSubscriberBase, AchievementsUI>((s) => new AchievementsUI(s.GetRequiredService<ILogger<AchievementsUI>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<AchievementManager>(), s.GetRequiredService<AchievementTabs>(), s.GetRequiredService<CosmeticService>(), s.GetRequiredService<UiSharedService>(), pi))
        .AddScoped<WindowMediatorSubscriberBase, MainUI>((s) => new MainUI(s.GetRequiredService<ILogger<MainUI>>(), s.GetRequiredService<GagspeakMediator>(), 
            s.GetRequiredService<UiSharedService>(), s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<PairManager>(), 
            s.GetRequiredService<ServerConfigurationManager>(), s.GetRequiredService<HomepageTab>(), s.GetRequiredService<WhitelistTab>(),
            s.GetRequiredService<PatternHubTab>(), s.GetRequiredService<MoodleHubTab>(), s.GetRequiredService<GlobalChatTab>(), s.GetRequiredService<AccountTab>(),
            s.GetRequiredService<MainMenuTabs>(), s.GetRequiredService<TutorialService>(), pi))
        .AddScoped<WindowMediatorSubscriberBase, PopoutKinkPlateUi>()
        .AddScoped<WindowMediatorSubscriberBase, InteractionEventsUI>()
        .AddScoped<WindowMediatorSubscriberBase, DtrVisibleWindow>()
        .AddScoped<WindowMediatorSubscriberBase, ChangelogUI>()
        .AddScoped<WindowMediatorSubscriberBase, MigrationsUI>()
        .AddScoped<WindowMediatorSubscriberBase, RemotePersonal>()
        .AddScoped<WindowMediatorSubscriberBase, RemotePatternMaker>()
        // RemoteController made via the factory is defined via the factory and not here.
        .AddScoped<WindowMediatorSubscriberBase, GagSetupUI>()
        .AddScoped<WindowMediatorSubscriberBase, WardrobeUI>()
        .AddScoped<WindowMediatorSubscriberBase, PuppeteerUI>()
        .AddScoped<WindowMediatorSubscriberBase, ToyboxUI>()
        .AddScoped<WindowMediatorSubscriberBase, OrdersUI>()
        .AddScoped<WindowMediatorSubscriberBase, PublicationsUI>()
        .AddScoped<WindowMediatorSubscriberBase, GlobalChatPopoutUI>()
        .AddScoped<WindowMediatorSubscriberBase, BlindfoldUI>((s) => new BlindfoldUI(s.GetRequiredService<ILogger<BlindfoldUI>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<UiSharedService>(), pi))
        .AddScoped<WindowMediatorSubscriberBase, KinkPlateEditorUI>()
        .AddScoped<WindowMediatorSubscriberBase, ProfilePictureEditor>()
        .AddScoped<WindowMediatorSubscriberBase, PopupHandler>()
        .AddScoped<IPopupHandler, VerificationPopupHandler>()
        .AddScoped<IPopupHandler, SavePatternPopupHandler>()
        .AddScoped<IPopupHandler, ReportPopupHandler>()
        .AddScoped<TextureService>()
        .AddScoped<OnlinePairManager>()
        .AddScoped<VisiblePairManager>()
        .AddScoped((s) => new TextureService(pi.UiBuilder, dm, tp))
        .AddScoped((s) => new UiService(s.GetRequiredService<ILogger<UiService>>(), pi.UiBuilder, s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<WindowSystem>(), s.GetServices<WindowMediatorSubscriberBase>(), s.GetRequiredService<UiFactory>(), s.GetRequiredService<MainMenuTabs>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<FileDialogManager>(), s.GetRequiredService<PenumbraChangedItemTooltip>()))
        .AddScoped((s) => new CommandManager(s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<ChatMonitor>(), s.GetRequiredService<DeathRollService>(), cg, cs, cm))
        .AddScoped((s) => new UiSharedService(s.GetRequiredService<ILogger<UiSharedService>>(), s.GetRequiredService<MainHub>(), s.GetRequiredService<ServerConfigurationManager>(),
            s.GetRequiredService<UiFontService>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<IpcManager>(), pi, tp));
    #endregion ScopedServices
    #region HostedServices
    public static IServiceCollection AddGagSpeakHosted(this IServiceCollection services)
    => services
        .AddHostedService(p => p.GetRequiredService<GagSpeakLoc>())
        .AddHostedService(p => p.GetRequiredService<StaticLoggerInit>())
        .AddHostedService(p => p.GetRequiredService<GagspeakMediator>())
        .AddHostedService(p => p.GetRequiredService<NotificationService>())
        .AddHostedService(p => p.GetRequiredService<OnFrameworkService>())
        .AddHostedService(p => p.GetRequiredService<EventAggregator>())
        .AddHostedService(p => p.GetRequiredService<IpcProvider>())
        .AddHostedService(p => p.GetRequiredService<SafewordService>())
        .AddHostedService(p => p.GetRequiredService<OnConnectedService>())
        .AddHostedService(p => p.GetRequiredService<CosmeticService>())

        // add our main Plugin.cs file as a hosted ;
        .AddHostedService<GagSpeakHost>();
    #endregion HostedServices
}
