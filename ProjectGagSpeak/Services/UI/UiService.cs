using Dalamud.Interface.Windowing;
using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Gui.Profile;
using GagSpeak.Gui.Remote;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;

namespace GagSpeak.Services;

/// <summary> A sealed class dictating the UI service for the plugin. </summary>
public sealed class UiService : DisposableMediatorSubscriberBase
{
    private static readonly List<WindowMediatorSubscriberBase> _createdWindows = [];
    private readonly MainMenuTabs _mainTabMenu;

    private readonly ILogger<UiService> _logger;
    private readonly MainConfig _mainConfig;
    private readonly ServerConfigService _serverConfig;
    private readonly UiFactory _uiFactory;
    private readonly WindowSystem _windowSystem;
    private readonly UiFileDialogService _fileService;

    // The universal UiBlocking interaction task.
    public static Task? UiTask { get; private set; }
    public static bool DisableUI => UiTask is not null && !UiTask.IsCompleted;

    public UiService(ILogger<UiService> logger, GagspeakMediator mediator,
        MainConfig mainConfig, ServerConfigService serverConfig,
        WindowSystem windowSystem, IEnumerable<WindowMediatorSubscriberBase> windows,
        UiFactory uiFactory, MainMenuTabs menuTabs, UiFileDialogService fileDialog)
        : base(logger, mediator)
    {
        _logger = logger;
        _mainConfig = mainConfig;
        _windowSystem = windowSystem;
        _uiFactory = uiFactory;
        _serverConfig = serverConfig;
        _fileService = fileDialog;
        _mainTabMenu = menuTabs;

        // disable the UI builder while in g-pose 
        Svc.PluginInterface.UiBuilder.DisableGposeUiHide = true;
        // add the event handlers for the UI builder's draw event
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        // subscribe to the UI builder's open config UI event
        Svc.PluginInterface.UiBuilder.OpenConfigUi += ToggleUi;
        // subscribe to the UI builder's open main UI event
        Svc.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // for each window in the collection of window mediator subscribers
        foreach (var window in windows)
        {
            // add the window to the window system.
            _windowSystem.AddWindow(window);
        }

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (msg) =>
        {
            var pairPermissionWindows = _createdWindows.OfType<KinksterInteractionsUI>().ToList();
            foreach (var window in pairPermissionWindows)
            {
                _logger.LogTrace("Closing KinksterInteractions window.", LoggerType.StickyUI);
                _windowSystem.RemoveWindow(window);
                _createdWindows.Remove(window);
                window.Dispose();
            }
        });

        // subscribe to the event message for removing a window
        Mediator.Subscribe<RemoveWindowMessage>(this, (msg) =>
        {
            // Check if the window is registered in the WindowSystem before removing it
            if (_windowSystem.Windows.Contains(msg.Window))
                _windowSystem.RemoveWindow(msg.Window);
            else
                _logger.LogWarning("Attempted to remove a window that is not registered in the WindowSystem: " + msg.Window.WindowName, LoggerType.UI);

            _createdWindows.Remove(msg.Window);
            msg.Window.Dispose();
        });

        /* ---------- The following subscribers are for factory made windows, meant to be unique to each pair ---------- */
        Mediator.Subscribe<KinkPlateOpenStandaloneMessage>(this, (msg) =>
        {
            if (!_createdWindows.Exists(p => p is KinkPlateUI ui
                && string.Equals(ui.Pair.UserData.UID, msg.Pair.UserData.UID, StringComparison.Ordinal)))
            {
                var window = _uiFactory.CreateStandaloneKinkPlateUi(msg.Pair);
                _createdWindows.Add(window);
                _windowSystem.AddWindow(window);
            }
        });

        Mediator.Subscribe<KinkPlateOpenStandaloneLightMessage>(this, (msg) =>
        {
            if (_createdWindows.FirstOrDefault(p => p is KinkPlateLightUI ui && ui.UserDataToDisplay.UID == msg.UserData.UID) is { } match)
            {
                match.Toggle();
            }
            else
            {
                var window = _uiFactory.CreateStandaloneKinkPlateLightUi(msg.UserData);
                _createdWindows.Add(window);
                _windowSystem.AddWindow(window);
            }
        });
    }

    /// <summary>
    ///     Offloads a UI task to the thread pool to not halt ImGui. 
    ///     When the task is finished DisableUI will be set to false.
    /// </summary>
    public static void SetUITask(Task task)
    {
        if (DisableUI)
        {
            Svc.Logger.Warning("Attempted to assign a new UI blocking task while one is already running.", LoggerType.UI);
            return;
        }

        UiTask = task;
        Svc.Logger.Verbose("Assigned new UI blocking task: " + task, LoggerType.UI);
    }

    /// <summary>
    ///     Offloads a UI task to the thread pool to not halt ImGui. 
    ///     When the task is finished DisableUI will be set to false.
    /// </summary>
    public static void SetUITask(Func<Task> asyncAction)
    {
        if (DisableUI)
        {
            Svc.Logger.Warning("Attempted to assign a new UI blocking task while one is already running.", LoggerType.UI);
            return;
        }

        UiTask = Task.Run(asyncAction);
        Svc.Logger.Verbose("Assigned new UI blocking task.", LoggerType.UI);
    }

    /// <summary>
    ///     Offloads a UI Task to the thread pool so ImGui is not halted. It
    ///     contains an inner task function that can return <typeparamref name="T"/>.
    /// </summary>
    /// <returns> A task that can be awaited, returning a value of type <typeparamref name="T"/>. </returns>
    public static async Task<T> SetUITaskWithReturn<T>(Func<Task<T>> asyncTask)
    {
        if (DisableUI)
        {
            Svc.Logger.Warning("Attempted to assign a new UI blocking task while one is already running.", LoggerType.UI);
            return default(T)!;
        }

        var taskToRun = Task.Run(asyncTask);
        UiTask = taskToRun;
        Svc.Logger.Verbose("Assigned new UI blocking task.", LoggerType.UI);
        return await taskToRun.ConfigureAwait(false);
    }

    public static bool IsRemoteUIOpen() => _createdWindows.OfType<BuzzToyRemoteUI>().FirstOrDefault() is { } m && m.IsOpen;


    /// <summary> Method to toggle the main UI for the plugin. </summary>
    /// <remarks> Checks if user has valid setup, and opens introUI or MainUI </remarks>
    public void ToggleMainUi()
    {
        if (_mainConfig.Current.HasValidSetup() && _serverConfig.Storage.HasValidSetup())
        {
            Mediator.Publish(new UiToggleMessage(typeof(MainUI)));
        }
        else
        {
            Mediator.Publish(new UiToggleMessage(typeof(IntroUi)));
        }
    }

    /// <summary>
    /// Method to toggle the subset UI for the plugin. AKA Settings window.
    /// <para>
    /// This will check to see if the user has a valid setup
    /// (meaning it sees if they are up to date), and will either
    /// open the settings window UI or the intro UI
    /// </para>
    /// </summary>
    public void ToggleUi()
    {
        if (_mainConfig.Current.HasValidSetup() && _serverConfig.Storage.HasValidSetup())
            Mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        else
            Mediator.Publish(new UiToggleMessage(typeof(IntroUi)));
    }

    /// <summary> Disposes of the UI service. </summary>
    protected override void Dispose(bool disposing)
    {
        // dispose of the base class
        base.Dispose(disposing);

        _logger.LogTrace("Disposing "+GetType().Name, LoggerType.UI);
        _windowSystem.RemoveAllWindows();
        foreach (var window in _createdWindows)
            window.Dispose();

        // unsubscribe from the draw, open config UI, and main UI
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= ToggleUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
    }

    /// <summary> Draw the windows system and file dialogue managers </summary>
    private void Draw()
    {
        _windowSystem.Draw();
        _fileService.Draw();
    }
}
