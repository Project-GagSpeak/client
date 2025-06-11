using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui.MainWindow;
using GagSpeak.CkCommons.Gui.Permissions;
using GagSpeak.CkCommons.Gui.Profile;

namespace GagSpeak.Services;

/// <summary> A sealed class dictating the UI service for the plugin. </summary>
public sealed class UiService : DisposableMediatorSubscriberBase
{
    private readonly List<WindowMediatorSubscriberBase> _createdWindows = [];
    private readonly MainMenuTabs _mainTabMenu;

    private readonly ILogger<UiService> _logger;
    private readonly MainConfigService _mainConfig;
    private readonly ServerConfigService _serverConfig;
    private readonly UiFactory _uiFactory;
    private readonly WindowSystem _windowSystem;
    private readonly UiFileDialogService _fileService;
    private readonly IUiBuilder _uiBuilder;

    public UiService(ILogger<UiService> logger, GagspeakMediator mediator,
        MainConfigService mainConfig, ServerConfigService serverConfig,
        WindowSystem windowSystem, IEnumerable<WindowMediatorSubscriberBase> windows,
        UiFactory uiFactory, MainMenuTabs menuTabs, UiFileDialogService fileDialog,
        IUiBuilder uiBuilder) : base(logger, mediator)
    {
        _logger = logger;
        _uiBuilder = uiBuilder;
        _mainConfig = mainConfig;
        _windowSystem = windowSystem;
        _uiFactory = uiFactory;
        _serverConfig = serverConfig;
        _fileService = fileDialog;
        _mainTabMenu = menuTabs;

        // disable the UI builder while in g-pose 
        _uiBuilder.DisableGposeUiHide = true;
        // add the event handlers for the UI builder's draw event
        _uiBuilder.Draw += Draw;
        // subscribe to the UI builder's open config UI event
        _uiBuilder.OpenConfigUi += ToggleUi;
        // subscribe to the UI builder's open main UI event
        _uiBuilder.OpenMainUi += ToggleMainUi;

        // for each window in the collection of window mediator subscribers
        foreach (var window in windows)
        {
            // add the window to the window system.
            _windowSystem.AddWindow(window);
        }

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (msg) =>
        {
            var pairPermissionWindows = _createdWindows
                .Where(p => p is PairStickyUI)
                .ToList();

            foreach (var window in pairPermissionWindows)
            {
                _logger.LogTrace("Closing pair permission window for pair "+((PairStickyUI)window).SPair.UserData.AliasOrUID, LoggerType.StickyUI);
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


        Mediator.Subscribe<OpenPairPerms>(this, (msg) =>
        {
            // if we are forcing the main UI, do so.
            if (msg.ForceOpenMainUI)
            {
                // if the mainUI window is not null, set the tab selection to whitelist.
                if (_createdWindows.FirstOrDefault(p => p is MainUI) is not null)
                {

                    _logger.LogTrace("Forcing main UI to whitelist tab", LoggerType.StickyUI);
                    _mainTabMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist;
                }
                else
                {
                    Mediator.Publish(new UiToggleMessage(typeof(MainUI), ToggleType.Show));
                    _mainTabMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist;
                }
            }

            // Attempt to locate the existing PairStickyUI window.
            if (_createdWindows.OfType<PairStickyUI>().FirstOrDefault(w => w.SPair.UserData.UID == msg.Pair?.UserData.UID) is PairStickyUI stickyUI)
            {
                // Attempt to change the draw-type. But if it is the same draw-type as the current, toggle the window.
                if (stickyUI.DrawType == msg.PermsWindowType)
                    stickyUI.Toggle();
                else
                {
                    stickyUI.DrawType = msg.PermsWindowType;
                    if (!stickyUI.IsOpen)
                        stickyUI.Toggle();
                }
                stickyUI.DrawType = msg.PermsWindowType;
            }
            else // We are attempting to open a stickyPairUi for another pair. Let's first destroy the current pairStickyUI's if they exist.
            {
                _logger.LogDebug("Destroying other pair's sticky UI's and recreating UI for new pair.", LoggerType.UI);
                foreach (var window in _createdWindows.OfType<PairStickyUI>())
                {
                    _windowSystem.RemoveWindow(window);
                    _createdWindows.Remove(window);
                    window?.Dispose();
                }

                // Create a new sticky pair perms window for the pair
                _logger.LogTrace("Creating new sticky window for pair "+msg.Pair?.UserData.AliasOrUID, LoggerType.StickyUI);
                var newWindow = _uiFactory.CreateStickyPairPerms(msg.Pair!, msg.PermsWindowType);
                _createdWindows.Add(newWindow);
                _windowSystem.AddWindow(newWindow);
            }
        });

        Mediator.Subscribe<OpenThumbnailBrowser>(this, (msg) =>
        {
            if (_createdWindows.FirstOrDefault(p => p is ThumbnailUI ui && ui.ImageBase.Kind == msg.MetaData.Kind) is ThumbnailUI match)
            {
                _logger.LogTrace("Toggling existing thumbnail browser for type " + msg.MetaData.Kind, LoggerType.StickyUI);
                match.Toggle();
            }
            else
            {
                // If other windows do exist, but do not match our current purpose, then we should close them, as only one should be worked on at a time.
                _logger.LogDebug("Destroying other thumbnail browsers and recreating UI.", LoggerType.UI);
                foreach (var window in _createdWindows.OfType<ThumbnailUI>().ToList())
                {
                    _windowSystem.RemoveWindow(window);
                    _createdWindows.Remove(window);
                    window?.Dispose();
                }

                // Create a new thumbnail browser for the type
                _logger.LogTrace("Creating new thumbnail browser for type " + msg.MetaData.Kind, LoggerType.UI);
                var newWindow = _uiFactory.CreateThumbnailUi(msg.MetaData);
                _createdWindows.Add(newWindow);
                _windowSystem.AddWindow(newWindow);
            }
        });

        Mediator.Subscribe<PairWasRemovedMessage>(this, (msg) => CloseExistingPairWindow());
        Mediator.Subscribe<ClosedMainUiMessage>(this, (msg) => CloseExistingPairWindow());
        Mediator.Subscribe<MainWindowTabChangeMessage>(this, (msg) => { if (msg.NewTab != MainMenuTabs.SelectedTab.Whitelist) CloseExistingPairWindow(); });
    }

    private void CloseExistingPairWindow()
    {
        var pairPermissionWindows = _createdWindows
            .Where(p => p is PairStickyUI)
            .ToList();

        foreach (var window in pairPermissionWindows)
        {
            _logger.LogTrace("Closing pair permission window for pair " + ((PairStickyUI)window).SPair.UserData.AliasOrUID, LoggerType.StickyUI);
            _windowSystem.RemoveWindow(window);
            _createdWindows.Remove(window);
            window.Dispose();
        }
    }

    /// <summary> Method to toggle the main UI for the plugin. </summary>
    /// <remarks> Checks if user has valid setup, and opens introUI or MainUI </remarks>
    public void ToggleMainUi()
    {
        if (_mainConfig.Config.HasValidSetup() && _serverConfig.Storage.HasValidSetup())
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
        if (_mainConfig.Config.HasValidSetup() && _serverConfig.Storage.HasValidSetup())
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
        _uiBuilder.Draw -= Draw;
        _uiBuilder.OpenConfigUi -= ToggleUi;
        _uiBuilder.OpenMainUi -= ToggleMainUi;
    }

    /// <summary> Draw the windows system and file dialogue managers </summary>
    private void Draw()
    {
        _windowSystem.Draw();
        _fileService.Draw();
    }
}
