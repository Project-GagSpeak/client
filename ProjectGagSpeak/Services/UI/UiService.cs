using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.Interop.IpcHelpers.Penumbra;
using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using GagSpeak.UI.MainWindow;
using GagSpeak.UI.Permissions;
using GagSpeak.UI.Profile;

namespace GagSpeak.Services;

/// <summary> A sealed class dictating the UI service for the plugin. </summary>
public sealed class UiService : DisposableMediatorSubscriberBase
{
    private readonly List<WindowMediatorSubscriberBase> _createdWindows = [];
    private readonly MainMenuTabs _mainTabMenu;

    private readonly ILogger<UiService> _logger;
    private readonly GagspeakConfigService _mainConfig;
    private readonly ServerConfigService _serverConfig;
    private readonly UiFactory _uiFactory;
    private readonly WindowSystem _windowSystem;
    private readonly FileDialogManager _fileDialog;
    private readonly IUiBuilder _uiBuilder;

    public UiService(ILogger<UiService> logger, GagspeakMediator mediator,
        GagspeakConfigService mainConfig, ServerConfigService serverConfig,
        WindowSystem windowSystem, IEnumerable<WindowMediatorSubscriberBase> windows,
        UiFactory uiFactory, MainMenuTabs menuTabs, FileDialogManager fileDialog,
        IUiBuilder uiBuilder) : base(logger, mediator)
    {
        _logger = logger;
        _uiBuilder = uiBuilder;
        _mainConfig = mainConfig;
        _windowSystem = windowSystem;
        _uiFactory = uiFactory;
        _serverConfig = serverConfig;
        _fileDialog = fileDialog;
        _mainTabMenu = menuTabs;

        // disable the UI builder while in gpose 
        _uiBuilder.DisableGposeUiHide = true;
        // add the event handlers for the UI builder's draw event
        _uiBuilder.Draw += Draw;
        // subscribe to the UI builder's open config UI event
        _uiBuilder.OpenConfigUi += ToggleUi;
        // subscribe to the UI builder's open main UI event
        _uiBuilder.OpenMainUi += ToggleMainUi;

        // for eachn window in the collection of window mediator subscribers
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
                _logger.LogTrace("Closing pair permission window for pair "+((PairStickyUI)window).SPair.UserData.AliasOrUID, LoggerType.Permissions);
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
            {
                _windowSystem.RemoveWindow(msg.Window);
            }
            else
            {
                _logger.LogWarning("Attempted to remove a window that is not registered in the WindowSystem: " + msg.Window.WindowName, LoggerType.UiCore);
            }

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
            if (!_createdWindows.Exists(p => p is KinkPlateLightUI ui
                && string.Equals(ui.UserDataToDisplay.UID, msg.UserData.UID, StringComparison.Ordinal)))
            {
                var window = _uiFactory.CreateStandaloneKinkPlateLightUi(msg.UserData);
                _createdWindows.Add(window);
                _windowSystem.AddWindow(window);
            }
            else
            {
                // the window does exist, so toggle its open state.
                var window = _createdWindows.FirstOrDefault(p => p is KinkPlateLightUI ui
                    && string.Equals(ui.UserDataToDisplay.UID, msg.UserData.UID, StringComparison.Ordinal));
                if (window != null)
                {
                    window.Toggle();
                }
            }
        });


        Mediator.Subscribe<OpenUserPairPermissions>(this, (msg) =>
        {
            // if we are forcing the main UI, do so.
            if (msg.ForceOpenMainUI)
            {
                // fetch the mainUI window.
                var mainUi = _createdWindows.FirstOrDefault(p => p is MainUI);
                // if the mainUI window is not null, set the tab selection to whitelist.
                if (mainUi != null)
                {

                    _logger.LogTrace("Forcing main UI to whitelist tab", LoggerType.Permissions);
                    _mainTabMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist;
                }
                else
                {
                    Mediator.Publish(new UiToggleMessage(typeof(MainUI), ToggleType.Show));
                    _mainTabMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist;
                }
            }

            // Find existing PairStickyUI windows with the same window type and pair UID
            var existingWindow = _createdWindows
                .FirstOrDefault(p => p is PairStickyUI stickyWindow &&
                                     stickyWindow.SPair.UserData.AliasOrUID == msg.Pair?.UserData.AliasOrUID &&
                                     stickyWindow.DrawType == msg.PermsWindowType);

            if (existingWindow != null && !msg.ForceOpenMainUI)
            {
                // If a matching window is found, toggle it
                _logger.LogTrace("Toggling existing sticky window for pair "+msg.Pair?.UserData.AliasOrUID, LoggerType.Permissions);
                // if it is open, destroy it.
                if (existingWindow.IsOpen)
                {
                    _windowSystem.RemoveWindow(existingWindow);
                    _createdWindows.Remove(existingWindow);
                    existingWindow.Dispose();
                }
                else
                {
                    existingWindow.Toggle();
                }
            }
            else
            {
                // Close and dispose of any other PairStickyUI windows
                var otherWindows = _createdWindows
                    .Where(p => p is PairStickyUI)
                    .ToList();

                foreach (var window in otherWindows)
                {
                    _logger.LogTrace("Disposing existing sticky window for pair "+((PairStickyUI)window).SPair.UserData.AliasOrUID, LoggerType.Permissions);
                    _windowSystem.RemoveWindow(window);
                    _createdWindows.Remove(window);
                    window.Dispose();
                }

                // Create a new sticky pair perms window for the pair
                _logger.LogTrace("Creating new sticky window for pair "+msg.Pair?.UserData.AliasOrUID, LoggerType.Permissions);
                var newWindow = _uiFactory.CreateStickyPairPerms(msg.Pair!, msg.PermsWindowType);
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
            _logger.LogTrace("Closing pair permission window for pair " + ((PairStickyUI)window).SPair.UserData.AliasOrUID, LoggerType.Permissions);
            _windowSystem.RemoveWindow(window);
            _createdWindows.Remove(window);
            window.Dispose();
        }
    }

    /// <summary>
    /// Method to toggle the main UI for the plugin.
    /// <para>
    /// This will check to see if the user has a valid setup 
    /// (meaning it sees if they are up to date), and will either 
    /// open the introUI or the main window UI
    /// </para>
    /// </summary>
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
        {
            Mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        }
        else
        {
            Mediator.Publish(new UiToggleMessage(typeof(IntroUi)));
        }
    }

    /// <summary> Disposes of the UI service. </summary>
    protected override void Dispose(bool disposing)
    {
        // dispose of the base class
        base.Dispose(disposing);

        _logger.LogTrace("Disposing "+GetType().Name, LoggerType.UiCore);

        // then remove all windows from the windows system
        _windowSystem.RemoveAllWindows();

        // for each of the created windows, dispose of them.
        foreach (var window in _createdWindows)
        {
            window.Dispose();
        }

        // unsubscribe from the draw, open config UI, and main UI
        _uiBuilder.Draw -= Draw;
        _uiBuilder.OpenConfigUi -= ToggleUi;
        _uiBuilder.OpenMainUi -= ToggleMainUi;
    }

    /// <summary> Draw the windows system and file dialogue managers </summary>
    private void Draw()
    {
        _windowSystem.Draw();
        _fileDialog.Draw();
    }
}
