using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.Achievements;
using GagSpeak.Achievements.Services;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Hardcore.Movement;
using GagSpeak.Interop;
using GagSpeak.Interop.Ipc;
using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.Interop.IpcHelpers.Penumbra;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Factories;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Migrations;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.StateManagers;
using GagSpeak.Toybox.Controllers;
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
using GagSpeak.UI.Profile;
using GagSpeak.UI.Simulation;
using GagSpeak.UI.Tabs.WardrobeTab;
using GagSpeak.UI.UiGagSetup;
using GagSpeak.UI.UiOrders;
using GagSpeak.UI.UiPublications;
using GagSpeak.UI.UiPuppeteer;
using GagSpeak.UI.UiRemote;
using GagSpeak.UI.UiToybox;
using GagSpeak.UI.UiWardrobe;
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
        .AddSingleton<IpcFastUpdates>()
        .AddSingleton((s) => new GagSpeakLoc(s.GetRequiredService<ILogger<GagSpeakLoc>>(), s.GetRequiredService<Dalamud.Localization>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<TutorialService>(), pi))

        // MufflerCore
        .AddSingleton((s) => new GagDataHandler(s.GetRequiredService<ILogger<GagDataHandler>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ClientConfigurationManager>(), pi))
        .AddSingleton((s) => new Ipa_EN_FR_JP_SP_Handler(s.GetRequiredService<ILogger<Ipa_EN_FR_JP_SP_Handler>>(),
            s.GetRequiredService<ClientConfigurationManager>(), pi))

        // Chat Services
        .AddSingleton((s) => new ChatBoxMessage(s.GetRequiredService<ILogger<ChatBoxMessage>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<ClientData>(), s.GetRequiredService<PuppeteerHandler>(),
            s.GetRequiredService<ChatSender>(), s.GetRequiredService<DeathRollService>(), s.GetRequiredService<TriggerService>(), cg, cs, dm))
        .AddSingleton((s) => new ChatSender(ss))
        .AddSingleton((s) => new ChatInputDetour(s.GetRequiredService<ILogger<ChatInputDetour>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<ClientData>(), s.GetRequiredService<GagGarbler>(), s.GetRequiredService<EmoteMonitor>(), ss, gip))

        // Hardcore services.
        .AddSingleton((s) => new SelectStringPrompt(s.GetRequiredService<ILogger<SelectStringPrompt>>(), s.GetRequiredService<ClientConfigurationManager>(),
            s.GetRequiredService<ForcedStayCallback>(), alc, gip, tm))
        .AddSingleton((s) => new YesNoPrompt(s.GetRequiredService<ILogger<YesNoPrompt>>(), s.GetRequiredService<ClientConfigurationManager>(), alc, tm))
        .AddSingleton((s) => new RoomSelectPrompt(s.GetRequiredService<ILogger<RoomSelectPrompt>>(), s.GetRequiredService<ClientConfigurationManager>(), alc, tm))
        .AddSingleton<SettingsHardcore>()

        // PlayerData Services
        .AddSingleton<GagGarbler>()
        .AddSingleton<AppearanceManager>()
        .AddSingleton<PublishStateService>()
        .AddSingleton<ToyboxManager>()
        .AddSingleton<CursedLootHandler>()
        .AddSingleton((s) => new GameItemStainHandler(s.GetRequiredService<ILogger<GameItemStainHandler>>(), s.GetRequiredService<ItemData>(),
            s.GetRequiredService<DictBonusItems>(), s.GetRequiredService<DictStain>(), s.GetRequiredService<TextureService>(), dm))
        .AddSingleton<PatternHandler>()
        .AddSingleton<WardrobeHandler>()
        .AddSingleton((s) => new HardcoreHandler(s.GetRequiredService<ILogger<HardcoreHandler>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<ClientData>(), s.GetRequiredService<AppearanceManager>(),
            s.GetRequiredService<PairManager>(), s.GetRequiredService<MainHub>(), s.GetRequiredService<MoveController>(),
            s.GetRequiredService<ChatSender>(), s.GetRequiredService<EmoteMonitor>(), tm))
        .AddSingleton<ClientData>()
        .AddSingleton<ClientDataChanges>()
        .AddSingleton<GameObjectHandlerFactory>()
        .AddSingleton<PairFactory>()
        .AddSingleton<PairHandlerFactory>()
        .AddSingleton<ParticipantFactory>()
        .AddSingleton<PrivateRoomFactory>()
        .AddSingleton((s) => new PairManager(s.GetRequiredService<ILogger<PairManager>>(), s.GetRequiredService<GagspeakMediator>(), 
            s.GetRequiredService<PairFactory>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<ServerConfigurationManager>(), cm))
        .AddSingleton<PrivateRoomManager>()
        .AddSingleton<OnConnectedService>()
        .AddSingleton<ClientCallbackService>()

        // Toybox Services
        .AddSingleton<ConnectedDevice>()
        .AddSingleton<ToyboxFactory>()
        .AddSingleton<DeviceService>()
        .AddSingleton<PatternPlayback>()
        .AddSingleton<AlarmHandler>()
        .AddSingleton<TriggerHandler>()
        .AddSingleton((s) => new DeathRollService(s.GetRequiredService<ILogger<DeathRollService>>(), s.GetRequiredService<ClientData>(),
            s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<TriggerService>(), s.GetRequiredService<ClientMonitorService>(), cg))

        // Unlocks / Achievements
        .AddSingleton((s) => new AchievementManager(s.GetRequiredService<ILogger<AchievementManager>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<ClientData>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<ClientMonitorService>(),
            s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<CosmeticService>(), s.GetRequiredService<KinkPlateService>(), s.GetRequiredService<VibratorService>(), s.GetRequiredService<UnlocksEventManager>(), nm, ds))
        .AddSingleton<UnlocksEventManager>()

        // UpdateMonitoring Services
        .AddSingleton((s) => new ClientMonitorService(s.GetRequiredService<ILogger<ClientMonitorService>>(), s.GetRequiredService<GagspeakMediator>(), cs, con, dm, fw, gg, pl))
        .AddSingleton((s) => new ActionMonitor(s.GetRequiredService<ILogger<ActionMonitor>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<HardcoreHandler>(), s.GetRequiredService<WardrobeHandler>(),
            s.GetRequiredService<ClientMonitorService>(), gip))

        .AddSingleton((s) => new MovementMonitor(s.GetRequiredService<ILogger<MovementMonitor>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<HardcoreHandler>(),
            s.GetRequiredService<WardrobeHandler>(), s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<SelectStringPrompt>(), s.GetRequiredService<YesNoPrompt>(),
            s.GetRequiredService<RoomSelectPrompt>(), s.GetRequiredService<ClientMonitorService>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<EmoteMonitor>(),
            s.GetRequiredService<MoveController>(), ks, ot, tm))
        .AddSingleton((s) => new MoveController(s.GetRequiredService<ILogger<MoveController>>(), gip, ot))
        .AddSingleton((s) => new ForcedStayCallback(s.GetRequiredService<ILogger<ForcedStayCallback>>(), s.GetRequiredService<ClientConfigurationManager>(), ss, gip))

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
        .AddSingleton((s) => new EmoteMonitor(s.GetRequiredService<ILogger<EmoteMonitor>>(), s.GetRequiredService<ClientMonitorService>(), dm))
        .AddSingleton<TriggerService>()

        .AddSingleton((s) => new DtrBarService(s.GetRequiredService<ILogger<DtrBarService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<EventAggregator>(),
            s.GetRequiredService<PairManager>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<ClientMonitorService>(), dm, dtr))

        // Utilities Services
        .AddSingleton<ILoggerProvider, Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider>()
        .AddSingleton<StaticLoggerInit>()
        .AddSingleton<GagSpeakHost>()
        .AddSingleton<ModAssociations>()
        .AddSingleton<MoodlesAssociations>()
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
        .AddSingleton<SetPreviewComponent>()
        .AddSingleton<MainMenuTabs>()
        .AddSingleton<AchievementTabs>()
        .AddSingleton((s) => new AccountsTab(s.GetRequiredService<ILogger<AccountsTab>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<ServerConfigurationManager>(), s.GetRequiredService<ClientMonitorService>(),
            s.GetRequiredService<UiSharedService>(), pi.ConfigDirectory.FullName))
        .AddSingleton<DebugTab>()
        .AddSingleton((s) => new AccountInfoExchanger(pi.ConfigDirectory.FullName))

        // UI Simulation Services
        .AddSingleton<StruggleStamina>()
        .AddSingleton<StruggleItem>()
        .AddSingleton<ProgressBar>()
        .AddSingleton<LockPickingMinigame>()


        // UI general services
        .AddSingleton<ActiveGagsPanel>()
        .AddSingleton<GagStoragePanel>()
        .AddSingleton<LockPickerSim>()

        // Wardrobe UI
        .AddSingleton<RestraintSetManager>()
        .AddSingleton<RestraintSetEditor>()
        .AddSingleton<StruggleSim>()
        .AddSingleton<CursedDungeonLoot>()
        .AddSingleton<MoodlesManager>()
        .AddSingleton((s) => new MoodlesService(s.GetRequiredService<ILogger<MoodlesService>>(), dm, tp))

        // Puppeteer UI
        .AddSingleton<PuppeteerHandler>()
        .AddSingleton<PuppeteerComponents>()

        // Toybox UI
        .AddSingleton<ToyboxOverview>()
        .AddSingleton<ToyboxPatterns>()
        .AddSingleton<ToyboxPrivateRooms>()
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
        .AddSingleton<SelectPairForTagUi>()
        .AddSingleton<TagHandler>()
        .AddSingleton<UserPairListHandler>()
        .AddSingleton<DrawRequests>()
        .AddSingleton<MainUiHomepage>()
        .AddSingleton<MainUiWhitelist>()
        .AddSingleton<MainUiPatternHub>()
        .AddSingleton<MainUiMoodlesHub>()
        .AddSingleton<MainUiChat>()
        .AddSingleton((s) => new MainUiAccount(s.GetRequiredService<ILogger<MainUiAccount>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<UiSharedService>(), s.GetRequiredService<OnFrameworkService>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<KinkPlateService>(),
            s.GetRequiredService<TutorialService>(), pi))

        // WebAPI Services
        .AddSingleton<MainHub>()
        .AddSingleton<ToyboxHub>()
        .AddSingleton<HubFactory>()
        .AddSingleton<TokenProvider>()
        .AddSingleton<PiShockProvider>()

        // Service Services
        .AddSingleton<ActionExecutor>()
        .AddSingleton<TutorialService>()
        .AddSingleton<Tutorial>()
        .AddSingleton<AchievementsService>()
        .AddSingleton((s) => new CursedLootService(s.GetRequiredService<ILogger<CursedLootService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<ClientData>(), s.GetRequiredService<CursedLootHandler>(),
            s.GetRequiredService<AppearanceManager>(), s.GetRequiredService<ClientMonitorService>(), s.GetRequiredService<OnFrameworkService>(), gip))
        .AddSingleton<SafewordService>()
        .AddSingleton<VibratorService>()
        .AddSingleton<ToyboxRemoteService>()
        .AddSingleton<TriggerService>()
        .AddSingleton<AppearanceService>()
        .AddSingleton<ClientConfigurationManager>()
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
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ClientMonitorService>(), dm, fw, ot, tm))
        .AddSingleton((s) => new CosmeticService(s.GetRequiredService<ILogger<CosmeticService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<OnFrameworkService>(), pi, tp))
        .AddSingleton((s) => new NotificationService(s.GetRequiredService<ILogger<NotificationService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<ClientData>(), cg, nm));
    #endregion GenericServices

    #region IpcServices
    public static IServiceCollection AddGagSpeakIPC(this IServiceCollection services, IDalamudPluginInterface pi, IClientState cs)
    => services
        .AddSingleton((s) => new IpcCallerMare(s.GetRequiredService<ILogger<IpcCallerMare>>(), pi,
            s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<GagspeakMediator>()))
        .AddSingleton((s) => new IpcCallerMoodles(s.GetRequiredService<ILogger<IpcCallerMoodles>>(), pi,
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ClientMonitorService>(), s.GetRequiredService<OnFrameworkService>()))
        .AddSingleton((s) => new IpcCallerPenumbra(s.GetRequiredService<ILogger<IpcCallerPenumbra>>(), pi,
            s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<GagspeakMediator>()))
        .AddSingleton((s) => new IpcCallerGlamourer(s.GetRequiredService<ILogger<IpcCallerGlamourer>>(), s.GetRequiredService<GagspeakMediator>(),
            pi, s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<ClientData>(), s.GetRequiredService<ClientMonitorService>(), 
            s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<IpcFastUpdates>()))
        .AddSingleton((s) => new IpcCallerCustomize(s.GetRequiredService<ILogger<IpcCallerCustomize>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<OnFrameworkService>(),
            s.GetRequiredService<IpcFastUpdates>(), pi, cs))

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
        .AddSingleton((s) => new GagspeakConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new GagStorageConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new WardrobeConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new CursedLootConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new AliasConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new PatternConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new AlarmConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new TriggerConfigService(pi.ConfigDirectory.FullName))
        // server-end configs
        .AddSingleton((s) => new ServerConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new NicknamesConfigService(pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new ServerTagConfigService(pi.ConfigDirectory.FullName))

        // Configuration Migrators
        .AddSingleton((s) => new MigrateRestraintSets(s.GetRequiredService<ILogger<MigrateRestraintSets>>(),
            s.GetRequiredService<ClientConfigurationManager>(), pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new MigratePatterns(s.GetRequiredService<ILogger<MigratePatterns>>(),
            s.GetRequiredService<ClientConfigurationManager>(), pi.ConfigDirectory.FullName));

    #endregion ConfigServices
    #region ScopedServices
    public static IServiceCollection AddGagSpeakScoped(this IServiceCollection services, IClientState cs,
        ICommandManager cm, IDalamudPluginInterface pi, ITextureProvider tp, INotificationManager nm,
        IChatGui cg, IDataManager dm)
    => services
        // Service Services
        .AddScoped<DrawEntityFactory>()
        .AddScoped<UiFactory>()
        .AddScoped<SelectTagForPairUi>()
        .AddScoped<WindowMediatorSubscriberBase, SettingsUi>()
        .AddScoped<WindowMediatorSubscriberBase, IntroUi>()
        .AddScoped<WindowMediatorSubscriberBase, KinkPlatePreviewUI>()
        .AddScoped<WindowMediatorSubscriberBase, AchievementsUI>((s) => new AchievementsUI(s.GetRequiredService<ILogger<AchievementsUI>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<AchievementManager>(), s.GetRequiredService<AchievementTabs>(), s.GetRequiredService<CosmeticService>(), s.GetRequiredService<UiSharedService>(), pi))
        .AddScoped<WindowMediatorSubscriberBase, MainWindowUI>((s) => new MainWindowUI(s.GetRequiredService<ILogger<MainWindowUI>>(), s.GetRequiredService<GagspeakMediator>(), 
            s.GetRequiredService<UiSharedService>(), s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<PairManager>(), 
            s.GetRequiredService<ServerConfigurationManager>(), s.GetRequiredService<MainUiHomepage>(), s.GetRequiredService<MainUiWhitelist>(),
            s.GetRequiredService<MainUiPatternHub>(), s.GetRequiredService<MainUiMoodlesHub>(), s.GetRequiredService<MainUiChat>(), s.GetRequiredService<MainUiAccount>(),
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
            s.GetRequiredService<ClientConfigurationManager>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<UiSharedService>(), pi))
        .AddScoped<WindowMediatorSubscriberBase, KinkPlateEditorUI>()
        .AddScoped<WindowMediatorSubscriberBase, ProfilePictureEditor>()
        .AddScoped<WindowMediatorSubscriberBase, PopupHandler>()
        .AddScoped<IPopupHandler, VerificationPopupHandler>()
        .AddScoped<IPopupHandler, SavePatternPopupHandler>()
        .AddScoped<IPopupHandler, ReportPopupHandler>()
        .AddScoped<CacheCreationService>()
        .AddScoped<TextureService>()
        .AddScoped<OnlinePairManager>()
        .AddScoped<VisiblePairManager>()
        .AddScoped((s) => new TextureService(pi.UiBuilder, dm, tp))
        .AddScoped((s) => new UiService(s.GetRequiredService<ILogger<UiService>>(), pi.UiBuilder, s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<WindowSystem>(), s.GetServices<WindowMediatorSubscriberBase>(), s.GetRequiredService<UiFactory>(), s.GetRequiredService<MainMenuTabs>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<FileDialogManager>(), s.GetRequiredService<PenumbraChangedItemTooltip>()))
        .AddScoped((s) => new CommandManager(s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<ServerConfigurationManager>(), s.GetRequiredService<ChatBoxMessage>(), s.GetRequiredService<DeathRollService>(), cg, cs, cm))
        .AddScoped((s) => new UiSharedService(s.GetRequiredService<ILogger<UiSharedService>>(), s.GetRequiredService<MainHub>(), s.GetRequiredService<ClientConfigurationManager>(), 
            s.GetRequiredService<ServerConfigurationManager>(), s.GetRequiredService<UiFontService>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<IpcManager>(), pi, tp));
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
        .AddHostedService(p => p.GetRequiredService<CursedLootService>())
        .AddHostedService(p => p.GetRequiredService<CosmeticService>())

        // add our main Plugin.cs file as a hosted ;
        .AddHostedService<GagSpeakHost>();
    #endregion HostedServices
}
