using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui.Text;
using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace GagSpeak.UI.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MainWindowUI : WindowMediatorSubscriberBase
{
    private readonly UiSharedService _uiShared;
    private readonly MainHub _apiHubMain;
    private readonly GagspeakConfigService _configService;
    private readonly PairManager _pairManager;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly MainMenuTabs _tabMenu;
    private readonly MainUiHomepage _homepage;
    private readonly MainUiWhitelist _whitelist;
    private readonly MainUiPatternHub _patternHub;
    private readonly MainUiMoodlesHub _moodlesHub;
    private readonly MainUiChat _globalChat;
    private readonly MainUiAccount _account;
    private readonly TutorialService _guides;
    private readonly IDalamudPluginInterface _pi;
    private float _windowContentWidth;
    private bool _addingNewUser = false;
    public string _pairToAdd = string.Empty; // the pair to add
    public string _pairToAddMessage = string.Empty; // the message attached to the pair to add

    // Attributes related to the drawing of the whitelist / contacts / pair list
    private Pair? _lastAddedUser;
    private string _lastAddedUserComment = string.Empty;
    // If we should draw sticky perms for the currently selected user pair.
    private bool _showModalForUserAddition;

    public MainWindowUI(ILogger<MainWindowUI> logger, GagspeakMediator mediator,
        UiSharedService uiShared, MainHub apiHubMain, GagspeakConfigService configService,
        PairManager pairManager, ServerConfigurationManager serverConfigs, MainUiHomepage homepage,
        MainUiWhitelist whitelist, MainUiPatternHub patternHub, MainUiMoodlesHub moodlesHub,
        MainUiChat globalChat, MainUiAccount account, MainMenuTabs tabMenu, TutorialService tutorialService,
        IDalamudPluginInterface pi) : base(logger, mediator, "###GagSpeakMainUI")
    {
        _apiHubMain = apiHubMain;
        _configService = configService;
        _pairManager = pairManager;
        _serverConfigs = serverConfigs;
        _homepage = homepage;
        _whitelist = whitelist;
        _patternHub = patternHub;
        _moodlesHub = moodlesHub;
        _globalChat = globalChat;
        _account = account;
        _guides = tutorialService;
        _pi = pi;
        _uiShared = uiShared;
        _tabMenu = tabMenu;

        AllowPinning = false;
        AllowClickthrough = false;
        TitleBarButtons = new()
        {
            new TitleBarButton()
            {
                Icon = FontAwesomeIcon.Cog,
                Click = (msg) =>
                {
                    Mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
                },

                IconOffset = new(2,1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Open Gagspeak Settings");
                    ImGui.EndTooltip();
                }
            },
            new TitleBarButton()
            {
                Icon = FontAwesomeIcon.Book,
                Click = (msg) =>
                {
                    Mediator.Publish(new UiToggleMessage(typeof(ChangelogUI)));
                },
                IconOffset = new(2,1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Changelog");
                    ImGui.EndTooltip();
                }
            },
            new TitleBarButton()
            {
                Icon = FontAwesomeIcon.QuestionCircle,
                Click = (msg) =>
                {
                    if(_guides.IsTutorialActive(TutorialType.MainUi))
                    {
                        _guides.SkipTutorial(TutorialType.MainUi);
                        _logger.LogInformation("Skipping Main UI Tutorial");
                    }
                    else
                    {
                        _guides.StartTutorial(TutorialType.MainUi);
                        _logger.LogInformation("Starting Main UI Tutorial");
                    }
                },
                IconOffset = new(2,1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Start/Stop MainUI Tutorial");
                    ImGui.EndTooltip();
                }
            }
        };

        // display info about the folders
        var ver = Assembly.GetExecutingAssembly().GetName().Version!;
        WindowName = $"GagSpeak Open Beta ({ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision})###GagSpeakMainUI";

        // Default to open if the user desires for it to be open.
        Toggle();

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (_) => IsOpen = true);
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) => IsOpen = false);

        Flags |= ImGuiWindowFlags.NoDocking;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(380, 500),
            MaximumSize = new Vector2(380, 2000),
        };
    }

    private bool ThemePushed = false;
    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        // get the width of the window content region we set earlier
        _windowContentWidth = UiSharedService.GetWindowContentRegionWidth();

        if (MainHub.ServerStatus is (ServerState.NoSecretKey or ServerState.VersionMisMatch or ServerState.Unauthorized))
        {
            using (ImRaii.PushId("header")) DrawUIDHeader();

            var errorTitle = MainHub.ServerStatus switch
            {
                ServerState.NoSecretKey => "INVALID/NO KEY",
                ServerState.VersionMisMatch => "UNSUPPORTED VERSION",
                ServerState.Unauthorized => "UNAUTHORIZED",
                _ => "UNK ERROR"
            };
            var errorText = MainHub.ServerStatus switch
            {
                ServerState.NoSecretKey => "No secret key is set for this current character. " +
                "\nTo create UID's for your alt characters, be sure to claim your account in the CK discord." +
                "\n\nOnce you have inserted a secret key, reload the plugin to be registered with the servers.",
                ServerState.VersionMisMatch => "Current Ver: " + MainHub.ClientVerString + Environment.NewLine
                + "Expected Ver: " + MainHub.ExpectedVerString +
                "\n\nThis Means that your client is outdated, and you need to update it." +
                "\n\nIf there is no update Available, then this message Likely Means Cordy is running some last minute tests " +
                "to ensure everyone doesn't crash with the latest update. Hang in there!",
                ServerState.Unauthorized => "You are Unauthorized to access GagSpeak Servers with this account due to an " +
                "Unauthorization. \n\nDetails:\n" + GagspeakHubBase.AuthFailureMessage,
                _ => "Unknown Reasoning for this error."
            };
            // push the notice that we are unsupported
            using (_uiShared.UidFont.Push())
            {
                var uidTextSize = ImGui.CalcTextSize(errorTitle);
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X + ImGui.GetWindowContentRegionMin().X) / 2 - uidTextSize.X / 2);
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(ImGuiColors.ParsedPink, errorTitle);
            }
            // the wrapped text explanation based on the error.
            UiSharedService.ColorTextWrapped(errorText, ImGuiColors.DalamudWhite);
        }
        else
        {
            using (ImRaii.PushId("serverstatus")) DrawServerStatus();
        }
        // separate our UI once more.
        ImGui.Separator();

        // store a ref to the end of the content drawn.
        float menuComponentEnd = ImGui.GetCursorPosY();

        // if we are connected, draw out our menus based on the tab selection.
        if (MainHub.ServerStatus is ServerState.Connected)
        {
            if (_addingNewUser)
            {
                using (ImRaii.PushId("AddPair")) DrawAddPair(_windowContentWidth, ImGui.GetStyle().ItemInnerSpacing.X);
            }
            // draw the bottom tab bar
            using (ImRaii.PushId("MainMenuTabBar")) _tabMenu.Draw();

            // display content based on the tab selected
            switch (_tabMenu.TabSelection)
            {
                case MainMenuTabs.SelectedTab.Homepage:
                    using (ImRaii.PushId("homepageComponent")) _homepage.DrawHomepageSection();
                    break;
                case MainMenuTabs.SelectedTab.Whitelist:
                    using (ImRaii.PushId("whitelistComponent")) _whitelist.DrawWhitelistSection();
                    break;
                case MainMenuTabs.SelectedTab.PatternHub:
                    using (ImRaii.PushId("patternHubComponent"))
                    {
                        _patternHub.DrawPatternHub();
                        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternHub, ImGui.GetWindowPos(), ImGui.GetWindowSize());
                    }
                    break;
                case MainMenuTabs.SelectedTab.MoodlesHub:
                    using (ImRaii.PushId("moodlesHubComponent")) _moodlesHub.DrawMoodlesHub();
                    break;
                case MainMenuTabs.SelectedTab.GlobalChat:
                    using (ImRaii.PushId("globalChatComponent")) _globalChat.DrawDiscoverySection();
                    break;
                case MainMenuTabs.SelectedTab.MySettings:
                    using (ImRaii.PushId("accountSettingsComponent")) _account.DrawAccountSection();
                    break;
            }
        }

        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        if (_uiShared.LastMainUIWindowSize != size || _uiShared.LastMainUIWindowPosition != pos)
        {
            _uiShared.LastMainUIWindowSize = size;
            _uiShared.LastMainUIWindowPosition = pos;
            Mediator.Publish(new CompactUiChange(size, pos));
        }
    }

    public void DrawAddPair(float availableXWidth, float spacingX)
    {
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableXWidth - buttonSize - spacingX);
        ImGui.InputTextWithHint("##otheruid", "Other players UID/Alias", ref _pairToAdd, 20);
        ImUtf8.SameLineInner();
        bool existingUser = _pairManager.DirectPairs.Exists(p => string.Equals(p.UserData.UID, _pairToAdd, StringComparison.Ordinal) || string.Equals(p.UserData.Alias, _pairToAdd, StringComparison.Ordinal));
        using (ImRaii.Disabled(existingUser || string.IsNullOrEmpty(_pairToAdd)))
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.UserPlus, "Add", buttonSize, false, _pairToAdd.IsNullOrEmpty()))
            {
                // call the UserAddPair function on the server with the user data transfer object
                _ = _apiHubMain.UserSendPairRequest(new(new(_pairToAdd), _pairToAddMessage));
                _pairToAdd = string.Empty;
                _pairToAddMessage = string.Empty;
                _addingNewUser = false;
            }
        }
        UiSharedService.AttachToolTip("Pair with " + (_pairToAdd.IsNullOrEmpty() ? "other user" : _pairToAdd));
        // draw a attached message field as well if they want.
        ImGui.SetNextItemWidth(availableXWidth);
        ImGui.InputTextWithHint("##pairAddOptionalMessage", "Attach Msg to Request (Optional)", ref _pairToAddMessage, 100);        
        ImGui.Separator();
    }

    private void DrawUIDHeader()
    {
        // fetch the Uid Text of yourself
        var uidText = _uiShared.GetUidText();

        // push the big boi font for the UID
        using (_uiShared.UidFont.Push())
        {
            var uidTextSize = ImGui.CalcTextSize(uidText);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - uidTextSize.X / 2);
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(_uiShared.GetUidColor(), uidText);
        }

        // if we are connected
        if (MainHub.ServerStatus is ServerState.Connected)
        {
            UiSharedService.CopyableDisplayText(MainHub.DisplayName);

            // if the UID does not equal the display name
            if (!string.Equals(MainHub.DisplayName, MainHub.UID, StringComparison.Ordinal))
            {
                // grab the original text size for the UID in the api controller
                var origTextSize = ImGui.CalcTextSize(MainHub.UID);
                // adjust the cursor and redraw the UID (really not sure why this is here but we can trial and error later.
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - origTextSize.X / 2);
                ImGui.TextColored(_uiShared.GetUidColor(), MainHub.UID);
                // give it the same functionality.
                UiSharedService.CopyableDisplayText(MainHub.UID);
            }
        }
    }


    /// <summary>
    /// Helper function for drawing the current status of the server, including the number of people online.
    /// </summary>
    private void DrawServerStatus()
    {
        var windowPadding = ImGui.GetStyle().WindowPadding;
        var addUserIcon = FontAwesomeIcon.UserPlus;
        var connectionButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.Link);
        var addUserButtonSize = _uiShared.GetIconButtonSize(addUserIcon);

        var userCount = MainHub.MainOnlineUsers.ToString(CultureInfo.InvariantCulture);
        var userSize = ImGui.CalcTextSize(userCount);
        var textSize = ImGui.CalcTextSize("Kinksters Online");
        var serverText = "Main GagSpeak Server";
        var shardTextSize = ImGui.CalcTextSize(serverText);
        var totalHeight = ImGui.GetTextLineHeight()*2 + ImGui.GetStyle().ItemSpacing.Y;

        // create a table
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Table("ServerStatusMainUI", 3))
        {
            // define the column lengths.
            ImGui.TableSetupColumn("##addUser", ImGuiTableColumnFlags.WidthFixed, addUserButtonSize.X);
            ImGui.TableSetupColumn("##serverState", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##connectionButton", ImGuiTableColumnFlags.WidthFixed, connectionButtonSize.X);

            // draw the add user button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - addUserButtonSize.Y) / 2);
            if (_uiShared.IconButton(addUserIcon, disabled: !MainHub.IsConnected))
            {
                _addingNewUser = !_addingNewUser;
            }
            UiSharedService.AttachToolTip("Add New User to Whitelist");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.AddingKinksters, ImGui.GetWindowPos(), ImGui.GetWindowSize());

            // in the next column, draw the centered status.
            ImGui.TableNextColumn();

            if (MainHub.IsConnected)
            {
                // fancy math shit for clean display, adjust when moving things around
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth())
                    / 2 - (userSize.X + textSize.X) / 2 - ImGui.GetStyle().ItemSpacing.X / 2);
                ImGui.TextColored(ImGuiColors.ParsedPink, userCount);
                ImGui.SameLine();
                ImGui.TextUnformatted("Kinksters Online");
            }
            // otherwise, if we are not connected, display that we aren't connected.
            else
            {
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth())
                    / 2 - ImGui.CalcTextSize("Not connected to any server").X / 2 - ImGui.GetStyle().ItemSpacing.X / 2);
                ImGui.TextColored(ImGuiColors.DalamudRed, "Not connected to any server");
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth()) / 2 - shardTextSize.X / 2);
            ImGui.TextUnformatted(serverText);

            // draw the connection link button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - addUserButtonSize.Y) / 2);
            // now we need to display the connection link button beside it.
            var color = _uiShared.GetServerStateColor();
            var connectedIcon = _uiShared.GetServerStateIcon(MainHub.ServerStatus);

            // if the server is reconnecting or disconnecting
            using (ImRaii.Disabled(MainHub.ServerStatus is ServerState.Reconnecting or ServerState.Disconnecting))
            {
                // we need to turn the button from the connected link to the disconnected link.
                using (ImRaii.PushColor(ImGuiCol.Text, color))
                {
                    // then display it
                    if (_uiShared.IconButton(connectedIcon))
                    {
                        // If its true, make sure our ServerStatus is Connected, or if its false, make sure our ServerStatus is Disconnected or offline.
                        if (MainHub.ServerStatus is ServerState.Connected)
                        {
                            // If we are connected, we want to disconnect.
                            _serverConfigs.CurrentServer.FullPause = true;
                            _serverConfigs.Save();
                            _ = _apiHubMain.Disconnect(ServerState.Disconnected);
                        }
                        else if (MainHub.ServerStatus is (ServerState.Disconnected or ServerState.Offline))
                        {
                            // If we are disconnected, we want to connect.
                            _serverConfigs.CurrentServer.FullPause = false;
                            _serverConfigs.Save();
                            _ = _apiHubMain.Connect();
                        }
                    }
                }
                // attach the tooltip for the connection / disconnection button)
                UiSharedService.AttachToolTip(MainHub.IsConnected
                    ? "Disconnect from " + _serverConfigs.CurrentServer.ServerName + "--SEP--Current Status: " + MainHub.ServerStatus
                    : "Connect to " + _serverConfigs.CurrentServer.ServerName + "--SEP--Current Status: " + MainHub.ServerStatus);
            }
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ConnectionState, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        }
    }


    /// <summary> 
    /// Retrieves the various server error messages based on the current server state.
    /// </summary>
    /// <returns> The error message of the server.</returns>
    private string GetServerError()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Connecting => "Attempting to connect to the server.",
            ServerState.Reconnecting => "Connection to server interrupted, attempting to reconnect to the server.",
            ServerState.Disconnected => "Currently disconnected from the GagSpeak server.",
            ServerState.Disconnecting => "Disconnecting from server",
            ServerState.Unauthorized => "Server Response: " + MainHub.AuthFailureMessage,
            ServerState.Offline => "The GagSpeak server is currently offline.",
            ServerState.VersionMisMatch => "Your plugin is out of date. Please update your plugin to fix.",
            ServerState.Connected => string.Empty,
            ServerState.NoSecretKey => "No secret key is set for this current character. " +
            "\nTo create UID's for your alt characters, be sure to claim your account in the CK discord." +
            "\n\nOnce you have inserted a secret key, reload the plugin to be registered with the servers.",
            _ => string.Empty
        };
    }

    public override void OnClose()
    {
        Mediator.Publish(new ClosedMainUiMessage());
        base.OnClose();
    }
}
