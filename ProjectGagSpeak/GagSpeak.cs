using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.Achievements;
using GagSpeak.Achievements.Services;
using GagSpeak.CkCommons.Gui;
using GagSpeak.FileSystems;
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
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Controllers;
using GagSpeak.PlayerState.Listener;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Restrictions;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.StateManagers;
using GagSpeak.Toybox.Services;
using GagSpeak.Toybox.SimulatedVibe;
using GagSpeak.Triggers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui.Handlers;
using GagSpeak.CkCommons.Gui.MainWindow;
using GagSpeak.CkCommons.Gui.Modules.Puppeteer;
using GagSpeak.CkCommons.Gui.Profile;
using GagSpeak.CkCommons.Gui.Publications;
using GagSpeak.CkCommons.Gui.Toybox;
using GagSpeak.CkCommons.Gui.UiRemote;
using GagSpeak.CkCommons.Gui.UiToybox;
using GagSpeak.CkCommons.Gui.Wardrobe;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.UpdateMonitoring.SpatialAudio;
using GagSpeak.UpdateMonitoring.Triggers;
using GagSpeak.VibeLobby;
using GagSpeak.WebAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using System.Reflection;

namespace GagSpeak;

public sealed class GagSpeak : IDalamudPlugin
{
    private readonly IHost _host;  // the host builder for the plugin instance. (What makes everything work)
    public static Serilog.ILogger StaticLog { get; private set; } = Serilog.Log.ForContext("Dalamud.PluginName", "UNKN");

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

        var name = Assembly.GetCallingAssembly().GetName().Name ?? "Unknown";
        StaticLog = Serilog.Log.ForContext("Dalamud.PluginName", name);
        
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
            // Get the content root for our plugin
            .UseContentRoot(pi.ConfigDirectory.FullName)
            // Configure the logging for the plugin
            .ConfigureLogging((hostContext, loggingBuilder) => GetPluginLogConfiguration(loggingBuilder, pl))
            // Get the plugin service collection for our plugin
            .ConfigureServices((hostContext, serviceCollection) =>
            {
                var services = GetPluginServices(serviceCollection, pi, pl, alc, cg, cs, cm, con, cmu, dm, bar, ds, fw, gg, gip, gps, ks, nm, ot, plt, ss, tm, tp);
                //services.ValidateDependancyInjector();
            }) 
            .Build();

    }

    /// <summary> Gets the log configuration for the plugin. </summary>
    private void GetPluginLogConfiguration(ILoggingBuilder lb, IPluginLog pluginLog)
    {
        // clear our providers, add dalamud logging (the override that integrates ILogger into IPluginLog), and set the minimum level to trace
        lb.ClearProviders();
        lb.AddDalamudLogging(pluginLog);
        lb.SetMinimumLevel(LogLevel.Trace);
    }

    /// <summary> Gets the plugin services for the GagSpeak plugin. </summary>
    public IServiceCollection GetPluginServices(IServiceCollection collection, IDalamudPluginInterface pi, IPluginLog pl,
        IAddonLifecycle alc, IChatGui cg, IClientState cs, ICommandManager cm, ICondition con, IContextMenu cmu, IDataManager dm,
        IDtrBar bar, IDutyState ds, IFramework fw, IGameGui gg, IGameInteropProvider gip, IGamepadState gps, IKeyState ks,
        INotificationManager nm, IObjectTable ot, IPartyList plt, ISigScanner ss, ITargetManager tm, ITextureProvider tp)
    {
        return collection
            // add the general services to the collection
            .AddSingleton(new WindowSystem("GagSpeak"))
            .AddSingleton<FileDialogManager>()
            .AddSingleton<UiFileDialogService>()
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
        // Nessisary Services
        .AddSingleton<ILoggerProvider, Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider>()
        .AddSingleton<GagSpeakHost>()
        .AddSingleton((s) => new EventAggregator(pi.ConfigDirectory.FullName, s.GetRequiredService<ILogger<EventAggregator>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<PairManager>()))
        .AddSingleton((s) => new GagSpeakLoc(s.GetRequiredService<ILogger<GagSpeakLoc>>(), s.GetRequiredService<Dalamud.Localization>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<TutorialService>(), pi))


        // CkCommons


        // CustomCombos


        // File System
        .AddSingleton((s) => new GagRestrictionFileSelector(s.GetRequiredService<ILogger<GagRestrictionFileSelector>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<GagRestrictionManager>(), s.GetRequiredService<GagFileSystem>(), ks))
        .AddSingleton((s) => new RestrictionFileSelector(s.GetRequiredService<ILogger<RestrictionFileSelector>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<RestrictionManager>(), s.GetRequiredService<RestrictionFileSystem>(), ks))
        .AddSingleton((s) => new RestraintSetFileSelector(s.GetRequiredService<ILogger<RestraintSetFileSelector>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<RestraintManager>(), s.GetRequiredService<RestraintSetFileSystem>(), ks))
        .AddSingleton((s) => new CursedLootFileSelector(s.GetRequiredService<ILogger<CursedLootFileSelector>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<CursedLootManager>(), s.GetRequiredService<CursedLootFileSystem>(), ks))
        .AddSingleton((s) => new PatternFileSelector(s.GetRequiredService<ILogger<PatternFileSelector>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<PatternManager>(), s.GetRequiredService<PatternFileSystem>(), ks))
        .AddSingleton((s) => new AlarmFileSelector(s.GetRequiredService<ILogger<AlarmFileSelector>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<AlarmManager>(), s.GetRequiredService<AlarmFileSystem>(), ks))
        .AddSingleton((s) => new TriggerFileSelector(s.GetRequiredService<ILogger<TriggerFileSelector>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<TriggerManager>(), s.GetRequiredService<TriggerFileSystem>(), ks))
        .AddSingleton<GagFileSystem>()
        .AddSingleton<RestrictionFileSystem>()
        .AddSingleton<RestraintSetFileSystem>()
        .AddSingleton<CursedLootFileSystem>()
        .AddSingleton<PatternFileSystem>()
        .AddSingleton<AlarmFileSystem>()
        .AddSingleton<TriggerFileSystem>()

        // Hardcore
        .AddSingleton((s) => new SelectStringPrompt(s.GetRequiredService<ILogger<SelectStringPrompt>>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<ForcedStayCallback>(), alc, gip, tm))
        .AddSingleton((s) => new YesNoPrompt(s.GetRequiredService<ILogger<YesNoPrompt>>(), s.GetRequiredService<GagspeakConfigService>(), alc, tm))
        .AddSingleton((s) => new RoomSelectPrompt(s.GetRequiredService<ILogger<RoomSelectPrompt>>(), s.GetRequiredService<GagspeakConfigService>(), alc, tm))


        // MufflerCore
        .AddSingleton((s) => new GagDataHandler(s.GetRequiredService<ILogger<GagDataHandler>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GagspeakConfigService>(), pi))
        .AddSingleton((s) => new Ipa_EN_FR_JP_SP_Handler(s.GetRequiredService<ILogger<Ipa_EN_FR_JP_SP_Handler>>(),
            s.GetRequiredService<GagspeakConfigService>(), pi))
        .AddSingleton<GagGarbler>()


        // Player Client State (Listener)
        .AddSingleton((s) => new VisualStateListener(s.GetRequiredService<ILogger<VisualStateListener>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<IpcManager>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<RestraintManager>(),
            s.GetRequiredService<RestrictionManager>(), s.GetRequiredService<GagRestrictionManager>(), s.GetRequiredService<CursedLootManager>(),
            s.GetRequiredService<ModSettingPresetManager>(), s.GetRequiredService<TraitsManager>(), s.GetRequiredService<VisualApplierGlamour>(),
            s.GetRequiredService<VisualApplierPenumbra>(), s.GetRequiredService<VisualApplierMoodles>(), s.GetRequiredService<VisualApplierCPlus>(),
            s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>(), pi))
        .AddSingleton<ToyboxStateListener>()
        .AddSingleton<PuppeteerListener>()
        // Player Client State (Manager)
        .AddSingleton<GagRestrictionManager>()
        .AddSingleton<RestrictionManager>()
        .AddSingleton<RestraintManager>()
        .AddSingleton<CursedLootManager>()
        .AddSingleton<PatternManager>()
        .AddSingleton<AlarmManager>()
        .AddSingleton<TriggerManager>()
        .AddSingleton((s) => new ModSettingPresetManager(s.GetRequiredService<ILogger<ModSettingPresetManager>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<IpcCallerPenumbra>(), s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<ConfigFileProvider>(),
            s.GetRequiredService<HybridSaveService>(), pi))
        .AddSingleton<PuppeteerManager>()
        .AddSingleton<TraitsManager>()
        // Player Client State (Applier)
        .AddSingleton<PatternApplier>()
        .AddSingleton<TriggerApplier>()
        .AddSingleton<VisualApplierCPlus>()
        .AddSingleton((s) => new VisualApplierGlamour(s.GetRequiredService<ILogger<VisualApplierGlamour>>(), s.GetRequiredService<IpcCallerGlamourer>(),
            s.GetRequiredService<OnFrameworkService>(), gip))
        .AddSingleton<VisualApplierMoodles>()
        .AddSingleton<VisualApplierPenumbra>()
        // Player Client State (Monitor)
        .AddSingleton((s) => new CursedLootMonitor(s.GetRequiredService<ILogger<CursedLootMonitor>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<GagRestrictionManager>(),
            s.GetRequiredService<CursedLootManager>(), s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>(), gip))
        .AddSingleton((s) => new TriggerMonitor(s.GetRequiredService<ILogger<TriggerMonitor>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GlobalData>(),
            s.GetRequiredService<PairManager>(), s.GetRequiredService<GagRestrictionManager>(), s.GetRequiredService<PuppeteerManager>(), s.GetRequiredService<TriggerManager>(),
            s.GetRequiredService<TriggerApplier>(), s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<OnFrameworkService>()))

        // PlayerData
        .AddSingleton<GlobalData>()
        .AddSingleton<IntifaceController>()
        .AddSingleton<GameObjectHandlerFactory>()
        .AddSingleton<PairFactory>()
        .AddSingleton<PairHandlerFactory>()
        .AddSingleton((s) => new PairManager(s.GetRequiredService<ILogger<PairManager>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<PairFactory>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<ServerConfigurationManager>(), cm))
        .AddSingleton<ClientDataSync>()


        // Services
        .AddSingleton<TutorialService>()
        .AddSingleton<AchievementsService>()
        .AddSingleton<SafewordService>()
        .AddSingleton<SexToyManager>()
        .AddSingleton((s) => new UiFontService(pi))
        .AddSingleton<GagspeakMediator>()
        .AddSingleton((s) => new DiscoverService(pi.ConfigDirectory.FullName, s.GetRequiredService<ILogger<DiscoverService>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(), s.GetRequiredService<MainMenuTabs>(),
            s.GetRequiredService<PairManager>(), s.GetRequiredService<CosmeticService>()))
        .AddSingleton<ShareHubService>()
        .AddSingleton<KinkPlateFactory>()
        .AddSingleton((s) => new KinkPlateService(s.GetRequiredService<ILogger<KinkPlateService>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<KinkPlateFactory>()))
        .AddSingleton((s) => new OnFrameworkService(s.GetRequiredService<ILogger<OnFrameworkService>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ClientMonitor>(), dm, fw, ot, tm))
        .AddSingleton((s) => new CosmeticService(s.GetRequiredService<ILogger<CosmeticService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<OnFrameworkService>(), pi, tp))
        .AddSingleton((s) => new NotificationService(s.GetRequiredService<ILogger<NotificationService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<GlobalData>(), s.GetRequiredService<GagRestrictionManager>(), cg, nm))
        .AddSingleton((s) => new DtrBarService(s.GetRequiredService<ILogger<DtrBarService>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<EventAggregator>(),
            s.GetRequiredService<PairManager>(), s.GetRequiredService<OnFrameworkService>(), s.GetRequiredService<ClientMonitor>(), dm, dtr))
        .AddSingleton((s) => new DeathRollService(s.GetRequiredService<ILogger<DeathRollService>>(), s.GetRequiredService<GlobalData>(),
            s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<TriggerManager>(), s.GetRequiredService<TriggerApplier>(), cg))
        .AddSingleton<FavoritesManager>()
        .AddSingleton<ItemService>()
        .AddSingleton((s) => new SpellActionService(dm))
        .AddSingleton((s) => new EmoteService(dm))

        // Services (NEW)
        .AddSingleton<BlindfoldService>()
        .AddSingleton<HypnoService>()

        // Spatial Audio (Depricated)
        .AddSingleton((s) => new ResourceLoader(s.GetRequiredService<ILogger<ResourceLoader>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<AvfxManager>(), s.GetRequiredService<ScdManager>(), dm, ss, gip))
        .AddSingleton((s) => new AvfxManager(s.GetRequiredService<ILogger<AvfxManager>>(), dm, pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new ScdManager(s.GetRequiredService<ILogger<ScdManager>>(), dm, pi.ConfigDirectory.FullName))
        .AddSingleton((s) => new VfxSpawns(s.GetRequiredService<ILogger<VfxSpawns>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ResourceLoader>(), cs, tm))


        // Toybox
        .AddSingleton<VibeRoomManager>()
        .AddSingleton<ToyboxFactory>()
        .AddSingleton((s) => new VibeSimAudio(s.GetRequiredService<ILogger<VibeSimAudio>>(), pi))


        // UI Components
        .AddSingleton<PermissionData>()
        .AddSingleton<IdDisplayHandler>()
        .AddSingleton((s) => new AccountInfoExchanger(pi.ConfigDirectory.FullName))

        // Unlocks Achievements
        .AddSingleton((s) => new AchievementManager(s.GetRequiredService<ILogger<AchievementManager>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<ClientMonitor>(),
            s.GetRequiredService<UnlocksEventManager>(), s.GetRequiredService<GagRestrictionManager>(), s.GetRequiredService<RestrictionManager>(), s.GetRequiredService<RestraintManager>(),
            s.GetRequiredService<CursedLootManager>(), s.GetRequiredService<PatternManager>(), s.GetRequiredService<AlarmManager>(), s.GetRequiredService<TriggerManager>(),
            s.GetRequiredService<SexToyManager>(), s.GetRequiredService<TraitsManager>(), s.GetRequiredService<ItemService>(), s.GetRequiredService<OnFrameworkService>(),
            s.GetRequiredService<CosmeticService>(), s.GetRequiredService<KinkPlateService>(), nm, ds))
        .AddSingleton<UnlocksEventManager>()


        // Update Monitoring
        .AddSingleton((s) => new ActionEffectMonitor(s.GetRequiredService<ILogger<ActionEffectMonitor>>(), s.GetRequiredService<GagspeakConfigService>(), ss, gip))

        .AddSingleton((s) => new ChatMonitor(s.GetRequiredService<ILogger<ChatMonitor>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<GlobalData>(), s.GetRequiredService<ChatSender>(),
            s.GetRequiredService<PuppeteerManager>(), s.GetRequiredService<TriggerMonitor>(), s.GetRequiredService<ClientMonitor>(),
            s.GetRequiredService<DeathRollService>(), cg))
        .AddSingleton((s) => new ChatSender(ss))
        .AddSingleton((s) => new ChatInputDetour(s.GetRequiredService<ILogger<ChatInputDetour>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<GlobalData>(), s.GetRequiredService<GagGarbler>(),
            s.GetRequiredService<EmoteService>(), s.GetRequiredService<GagRestrictionManager>(), ss, gip))

        .AddSingleton((s) => new OnEmote(s.GetRequiredService<ILogger<OnEmote>>(), s.GetRequiredService<TraitsManager>(), s.GetRequiredService<OnFrameworkService>(), ss, gip))

        .AddSingleton((s) => new ActionMonitor(s.GetRequiredService<ILogger<ActionMonitor>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GlobalData>(),
            s.GetRequiredService<TraitsManager>(), s.GetRequiredService<ClientMonitor>(), gip))
        .AddSingleton((s) => new MovementMonitor(s.GetRequiredService<ILogger<MovementMonitor>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<MainHub>(),
            s.GetRequiredService<GlobalData>(), s.GetRequiredService<ChatSender>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<SelectStringPrompt>(),
            s.GetRequiredService<YesNoPrompt>(), s.GetRequiredService<RoomSelectPrompt>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<TraitsManager>(),
            s.GetRequiredService<ClientMonitor>(), s.GetRequiredService<EmoteService>(), s.GetRequiredService<MoveController>(), ks, ot, tm))
        .AddSingleton((s) => new MoveController(s.GetRequiredService<ILogger<MoveController>>(), gip, ot))
        .AddSingleton((s) => new ForcedStayCallback(s.GetRequiredService<ILogger<ForcedStayCallback>>(), s.GetRequiredService<GagspeakConfigService>(), ss, gip))

        .AddSingleton((s) => new IconDisplayer(s.GetRequiredService<ILogger<IconDisplayer>>(), dm, tp))

        .AddSingleton((s) => new ClientMonitor(s.GetRequiredService<ILogger<ClientMonitor>>(), s.GetRequiredService<GagspeakMediator>(), cs, con, dm, fw, gg, pl))
        .AddSingleton((s) => new OnFrameworkService(s.GetRequiredService<ILogger<OnFrameworkService>>(),
            s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<ClientMonitor>(), dm, fw, ot, tm))


        // WebAPI (Server Connectivity)
        .AddSingleton<MainHub>()
        .AddSingleton<HubFactory>()
        .AddSingleton<TokenProvider>()
        .AddSingleton<PiShockProvider>()


        // Penumbra.GameData pain.
        .AddSingleton<ItemData>()
        .AddSingleton((s) => new DictBonusItems(pi, new Logger(), dm))
        .AddSingleton((s) => new DictStain(pi, new Logger(), dm))
        .AddSingleton((s) => new ItemsByType(pi, new Logger(), dm, s.GetRequiredService<DictBonusItems>()))
        .AddSingleton((s) => new ItemsPrimaryModel(pi, new Logger(), dm, s.GetRequiredService<ItemsByType>()))
        .AddSingleton((s) => new ItemsSecondaryModel(pi, new Logger(), dm, s.GetRequiredService<ItemsByType>()))
        .AddSingleton((s) => new ItemsTertiaryModel(pi, new Logger(), dm, s.GetRequiredService<ItemsByType>(), s.GetRequiredService<ItemsSecondaryModel>()));
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
        // Config Management
        .AddSingleton((s) => new ConfigFileProvider(pi))
        .AddSingleton<GagspeakConfigService>()
        .AddSingleton<ServerConfigService>()
        .AddSingleton<NicknamesConfigService>()
        .AddSingleton<ServerConfigurationManager>()
        .AddSingleton<HybridSaveService>();
    // the rest of the config stuff here is not migrated into other parts so see about how we will sort this later.

    #endregion ConfigServices
    #region ScopedServices
    public static IServiceCollection AddGagSpeakScoped(this IServiceCollection services, IClientState cs,
        ICommandManager cm, IDalamudPluginInterface pi, ITextureProvider tp, INotificationManager nm,
        IChatGui cg, IDataManager dm)
    => services
        // Scoped Components
        .AddScoped<DrawRequests>()
        .AddScoped((s) => new EquipmentDrawer(s.GetRequiredService<ILogger<EquipmentDrawer>>(), s.GetRequiredService<IpcCallerGlamourer>(),
            s.GetRequiredService<RestrictionManager>(), s.GetRequiredService<FavoritesManager>(), s.GetRequiredService<ItemService>(),
            s.GetRequiredService<TextureService>(), s.GetRequiredService<CosmeticService>(), dm))
        .AddScoped<TraitsDrawer>()
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
        .AddScoped<WindowMediatorSubscriberBase, MainUI>((s) => new MainUI(s.GetRequiredService<ILogger<MainUI>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<ServerConfigurationManager>(),
            s.GetRequiredService<HomepageTab>(), s.GetRequiredService<WhitelistTab>(), s.GetRequiredService<PatternHubTab>(), s.GetRequiredService<MoodleHubTab>(),
            s.GetRequiredService<GlobalChatTab>(), s.GetRequiredService<AccountTab>(), s.GetRequiredService<MainMenuTabs>(), s.GetRequiredService<TutorialService>(), pi))
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
        .AddScoped<WindowMediatorSubscriberBase, AchievementsUI>((s) => new AchievementsUI(s.GetRequiredService<ILogger<AchievementsUI>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<AchievementManager>(), s.GetRequiredService<AchievementTabs>(), s.GetRequiredService<CosmeticService>(), pi))
        .AddScoped<AchievementTabs>()


        // StickyWindow
        .AddScoped<PermissionsDrawer>()
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
        .AddScoped((s) => new AccountManagerTab(s.GetRequiredService<ILogger<AccountManagerTab>>(), s.GetRequiredService<GagspeakMediator>(),
            s.GetRequiredService<MainHub>(), s.GetRequiredService<GagspeakConfigService>(), s.GetRequiredService<ServerConfigurationManager>(),
            s.GetRequiredService<ConfigFileProvider>(), s.GetRequiredService<ClientMonitor>()))
        .AddScoped<DebugTab>()
        .AddScoped<DebuggerBinds>()

        // Scoped Misc
        .AddScoped<WindowMediatorSubscriberBase, InteractionEventsUI>()
        .AddScoped<WindowMediatorSubscriberBase, DtrVisibleWindow>()
        .AddScoped<WindowMediatorSubscriberBase, ChangelogUI>()
        .AddScoped<WindowMediatorSubscriberBase, GlobalChatPopoutUI>()
        .AddScoped<WindowMediatorSubscriberBase, DebuggerStandaloneUI>()

        // Scoped Services
        .AddScoped((s) => new CommandManager(s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<ServerConfigService>(), s.GetRequiredService<ChatMonitor>(), s.GetRequiredService<DeathRollService>(), cg, cs, cm))
        .AddScoped<DataDistributionService>()
        .AddScoped((s) => new TextureService(pi.UiBuilder, dm, tp))
        .AddScoped((s) => new UiService(s.GetRequiredService<ILogger<UiService>>(), s.GetRequiredService<GagspeakMediator>(), s.GetRequiredService<GagspeakConfigService>(),
            s.GetRequiredService<ServerConfigService>(), s.GetRequiredService<WindowSystem>(), s.GetServices<WindowMediatorSubscriberBase>(), s.GetRequiredService<UiFactory>(),
            s.GetRequiredService<MainMenuTabs>(), s.GetRequiredService<UiFileDialogService>(), pi.UiBuilder))
        .AddScoped((s) => new CkGui(s.GetRequiredService<ILogger<CkGui>>(), s.GetRequiredService<ServerConfigurationManager>(), pi, tp));
    #endregion ScopedServices

    #region HostedServices
    public static IServiceCollection AddGagSpeakHosted(this IServiceCollection services)
    => services
        .AddHostedService(p => p.GetRequiredService<GagspeakMediator>())
        .AddHostedService(p => p.GetRequiredService<UiFontService>())
        .AddHostedService(p => p.GetRequiredService<HybridSaveService>())
        .AddHostedService(p => p.GetRequiredService<NotificationService>())
        .AddHostedService(p => p.GetRequiredService<ClientMonitor>())
        .AddHostedService(p => p.GetRequiredService<SpellActionService>())
        .AddHostedService(p => p.GetRequiredService<EmoteService>())
        .AddHostedService(p => p.GetRequiredService<OnFrameworkService>())
        .AddHostedService(p => p.GetRequiredService<GagSpeakLoc>())
        .AddHostedService(p => p.GetRequiredService<EventAggregator>())
        .AddHostedService(p => p.GetRequiredService<IpcProvider>())
        .AddHostedService(p => p.GetRequiredService<CosmeticService>())
        .AddHostedService(p => p.GetRequiredService<MainHub>())

        // add our main Plugin.cs file as a hosted ;
        .AddHostedService<GagSpeakHost>();
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
                    GagSpeak.StaticLog.Warning($"[WARNING] Skipping {serviceType.Name} due to unresolvable parameters.");
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
