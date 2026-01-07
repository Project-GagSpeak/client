using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Hub;
using OtterGui.Text;
using System.Globalization;
using System.Reflection;

namespace GagSpeak.Gui.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MainUI : WindowMediatorSubscriberBase
{
    public const float MAIN_UI_WIDTH = 380f;

    private readonly MainConfig _config;
    private readonly AccountManager _account;
    private readonly MainHub _hub;
    private readonly MainMenuTabs _tabMenu;
    private readonly SidePanelService _sidePanelService;
    private readonly RequestsManager _requestManager;
    private readonly KinksterManager _kinksters;
    private readonly TutorialService _guides;
    private readonly HomeTab _homepage;
    private readonly RequestsTab _requests;
    private readonly WhitelistTab _whitelist;
    private readonly PatternHubTab _patternHub;
    private readonly MoodleHubTab _moodlesHub;
    private readonly GlobalChatTab _globalChat;
    private float _windowContentWidth;

    private bool _creatingRequest = false;
    public string _uidToSentTo = string.Empty;
    public string _requestMessage = string.Empty;

    private bool ThemePushed = false;

    public MainUI(ILogger<MainUI> logger, GagspeakMediator mediator, MainConfig config,
        AccountManager account, MainHub hub, MainMenuTabs tabMenu, SidePanelService sidePanel, RequestsManager requestmanager, 
        KinksterManager kinksters, TutorialService guides, HomeTab home, RequestsTab requests,
        WhitelistTab whitelist, PatternHubTab patternHub, MoodleHubTab moodlesHub, GlobalChatTab globalChat)         
        : base(logger, mediator, "###GagSpeakMainUI")
    {
        _config = config;
        _account = account;
        _hub = hub;
        _tabMenu = tabMenu;
        _sidePanelService = sidePanel;
        _requestManager = requestmanager;
        _kinksters = kinksters;
        _guides = guides;
        _homepage = home;
        _requests = requests;
        _whitelist = whitelist;
        _patternHub = patternHub;
        _moodlesHub = moodlesHub;
        _globalChat = globalChat;

        // display info about the folders
        var ver = Assembly.GetExecutingAssembly().GetName().Version!;
        WindowName = $"GagSpeak Closed Beta ({ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision})###GagSpeakMainUI";
        Flags |= WFlags.NoDocking;

        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(MAIN_UI_WIDTH, 500), new Vector2(MAIN_UI_WIDTH, 2000));
        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.Book, "Changelog", () => Mediator.Publish(new UiToggleMessage(typeof(ChangelogUI))))
            .Add(FAI.Bell, "Actions Notifier", () => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI))))
            .Add(FAI.Cog, "Settings", () => Mediator.Publish(new UiToggleMessage(typeof(SettingsUi))))
            .AddTutorial(_guides, TutorialType.MainUi)
            .Build();
        
        // Default to open if the user desires for it to be open.
        if(_config.Current.OpenMainUiOnStartup)
            Toggle();
        // Update the tab menu selection.
        _tabMenu.TabSelection = _config.Current.MainUiTab;

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (_) => IsOpen = true);
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) => IsOpen = false);
    }

    public static Vector2 LastPos { get; private set; } = Vector2.Zero;
    public static Vector2 LastSize { get; private set; } = Vector2.Zero;

    // for tutorial.
    private Vector2 WindowPos => ImGui.GetWindowPos();
    private Vector2 WindowSize => ImGui.GetWindowSize();

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
        _windowContentWidth = CkGui.GetWindowContentRegionWidth();

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
            var errorText = GetServerError();

            // push the notice that we are unsupported
            using (UiFontService.UidFont.Push())
            {
                var uidTextSize = ImGui.CalcTextSize(errorTitle);
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X + ImGui.GetWindowContentRegionMin().X) / 2 - uidTextSize.X / 2);
                CkGui.ColorTextFrameAligned(errorTitle, ImGuiColors.ParsedPink);
            }
            // the wrapped text explanation based on the error.
            CkGui.ColorTextWrapped(errorText, ImGuiColors.DalamudWhite);
        }
        else
        {
            DrawServerStatus();
        }
        // separate our UI once more.
        ImGui.Separator();

        // store a ref to the end of the content drawn.
        var menuComponentEnd = ImGui.GetCursorPosY();

        // if we are connected, draw out our menus based on the tab selection.
        if (MainHub.IsConnected)
        {
            if (_creatingRequest)
                DrawRequestCreator(_windowContentWidth, ImUtf8.ItemInnerSpacing.X);

            // draw the bottom tab bar
            using (ImRaii.PushId("MainMenuTabBar")) _tabMenu.Draw(_windowContentWidth);

            // display content based on the tab selected
            switch (_tabMenu.TabSelection)
            {
                case MainMenuTabs.SelectedTab.Homepage:
                    _homepage.DrawSection();
                    break;
                case MainMenuTabs.SelectedTab.Requests:
                    _requests.DrawSection();
                    break;
                case MainMenuTabs.SelectedTab.Whitelist:
                    _whitelist.DrawSection();
                    break;
                case MainMenuTabs.SelectedTab.PatternHub:
                    _patternHub.DrawPatternHub();
                    _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternResults, ImGui.GetWindowPos(), ImGui.GetWindowSize(), 
                        () => { _tabMenu.TabSelection = MainMenuTabs.SelectedTab.MoodlesHub; });
                    break;
                case MainMenuTabs.SelectedTab.MoodlesHub:
                    _moodlesHub.DrawMoodlesHub();
                    break;
                case MainMenuTabs.SelectedTab.GlobalChat:
                    _globalChat.DrawSection();
                    break;
            }
        }

        LastPos = ImGui.GetWindowPos();
        LastSize = ImGui.GetWindowSize();
    }

    public void DrawRequestCreator(float availableXWidth, float spacingX)
    {
        var buttonSize = CkGui.IconTextButtonSize(FAI.Upload, "Send Pair Request");
        ImGui.SetNextItemWidth(availableXWidth - buttonSize - spacingX);
        // Let the client say who they want the request to go to.
        ImGui.InputTextWithHint("##otherUid", "Other players UID/Alias", ref _uidToSentTo, 20);
        ImUtf8.SameLineInner();

        // Disable the add button if they are already added or nothing is in the field. (might need to also account for alias here)
        var allowSend = !string.IsNullOrEmpty(_uidToSentTo) && !_kinksters.ContainsKinkster(_uidToSentTo);
        if (CkGui.IconTextButton(FAI.Upload, "Send", buttonSize, false, !allowSend))
        {
            UiService.SetUITask(async () =>
            {
                var res = await _hub.UserSendKinksterRequest(new(new(_uidToSentTo), false, string.Empty, _requestMessage));
                _uidToSentTo = string.Empty;
                _requestMessage = string.Empty;
                _creatingRequest = false;
                // Add the request if it was successful!
                if (res.ErrorCode is GagSpeakApiEc.Success)
                    _requestManager.AddNewRequest(res.Value!);
            });
        }
        if (!string.IsNullOrEmpty(_uidToSentTo))
            CkGui.AttachToolTip($"Send Pair Request to {_uidToSentTo}");

        // draw a attached message field as well if they want.
        ImGui.SetNextItemWidth(availableXWidth);
        ImGui.InputTextWithHint("##pairAddOptionalMessage", "Attach Msg to Request (Optional)", ref _requestMessage, 100);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.AttachingMessages, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () =>
        {
            _creatingRequest = !_creatingRequest;
            _tabMenu.TabSelection = MainMenuTabs.SelectedTab.Homepage;
        });
        ImGui.Separator();
    }

    private void DrawUIDHeader()
    {
        var uidText = GsExtensions.GetUidText();
        using (UiFontService.UidFont.Push())
        {
            var uidTextSize = ImGui.CalcTextSize(uidText);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - uidTextSize.X / 2);
            ImGui.TextColored(GsExtensions.UidColor(), uidText);
        }

        // if we are connected
        if (MainHub.IsConnected)
        {
            CkGui.CopyableDisplayText(MainHub.DisplayName);
            if (!string.Equals(MainHub.DisplayName, MainHub.UID, StringComparison.Ordinal))
            {
                var originalTextSize = ImGui.CalcTextSize(MainHub.UID);
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - originalTextSize.X / 2);
                ImGui.TextColored(GsExtensions.UidColor(), MainHub.UID);
                CkGui.CopyableDisplayText(MainHub.UID);
            }
        }
    }


    /// <summary> Draws the current status of the server, including the number of people online. </summary>
    private void DrawServerStatus()
    {
        var windowPadding = ImGui.GetStyle().WindowPadding;
        var addUserIcon = FAI.UserPlus;
        var connectionButtonSize = CkGui.IconButtonSize(FAI.Link);
        var addUserButtonSize = CkGui.IconButtonSize(addUserIcon);

        var userCount = MainHub.OnlineUsers.ToString(CultureInfo.InvariantCulture);
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
            if (CkGui.IconButton(addUserIcon, disabled: !MainHub.IsConnected))
                _creatingRequest = !_creatingRequest;
            CkGui.AttachToolTip("Add New User to Whitelist");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.AddingKinksters, ImGui.GetWindowPos(), ImGui.GetWindowSize(), () => _creatingRequest = !_creatingRequest);

            // in the next column, draw the centered status.
            ImGui.TableNextColumn();

            if (MainHub.IsConnected)
            {
                // fancy math shit for clean display, adjust when moving things around
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth())
                    / 2 - (userSize.X + textSize.X) / 2 - ImGui.GetStyle().ItemSpacing.X / 2);
                using (ImRaii.Group())
                {
                    ImGui.TextColored(ImGuiColors.ParsedPink, userCount);
                    ImGui.SameLine();
                    ImGui.TextUnformatted("Kinksters Online");
                }
                _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.InitialWelcome, WindowPos, WindowSize);

            }
            // otherwise, if we are not connected, display that we aren't connected.
            else
            {
                ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth())
                    / 2 - ImGui.CalcTextSize("Not connected to any server").X / 2 - ImGui.GetStyle().ItemSpacing.X / 2);
                ImGui.TextColored(ImGuiColors.DalamudRed, "Not connected to any server");
            }

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth()) / 2 - shardTextSize.X / 2);
            ImGui.TextUnformatted(serverText);

            // draw the connection link button
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (totalHeight - addUserButtonSize.Y) / 2);
            // now we need to display the connection link button beside it.
            var color = GsExtensions.ServerStateColor();
            var connectedIcon = GsExtensions.ServerStateIcon(MainHub.ServerStatus);

            // if the server is reconnecting or disconnecting
            using (ImRaii.Disabled(MainHub.ServerStatus is ServerState.Reconnecting or ServerState.Disconnecting))
            {
                // we need to turn the button from the connected link to the disconnected link.
                using (ImRaii.PushColor(ImGuiCol.Text, color))
                {
                    // then display it
                    if (CkGui.IconButton(connectedIcon))
                    {
                        // If its true, make sure our ServerStatus is Connected, or if its false, make sure our ServerStatus is Disconnected or offline.
                        if (MainHub.IsConnected)
                        {
                            _config.SetPauseState(true);
                            UiService.SetUITask(_hub.Disconnect(ServerState.Disconnected, DisconnectIntent.Normal));
                        }
                        else if (MainHub.ServerStatus is (ServerState.Disconnected or ServerState.Offline))
                        {
                            _config.SetPauseState(false);
                            UiService.SetUITask(_hub.Connect());
                        }
                    }
                }
                // attach the tooltip for the connection / disconnection button)
                CkGui.AttachToolTip(MainHub.IsConnected
                    ? $"Disconnect from {MainHub.MAIN_SERVER_NAME}--SEP--Current Status: {MainHub.ServerStatus}"
                    : $"Connect to {MainHub.MAIN_SERVER_NAME}--SEP--Current Status: {MainHub.ServerStatus}");
            }
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ConnectionState, WindowPos, WindowSize, () => _tabMenu.TabSelection = MainMenuTabs.SelectedTab.Homepage);
        }
    }


    /// <summary> Retrieves the various server error messages based on the current server state. </summary>
    /// <returns> The error message of the server.</returns>
    private string GetServerError()
    {
        return MainHub.ServerStatus switch
        {
            ServerState.Connecting => "Attempting to connect to the server.",
            ServerState.Reconnecting => "Connection to server interrupted, attempting to reconnect to the server.",
            ServerState.Disconnected => "Currently disconnected from the GagSpeak server.",
            ServerState.Disconnecting => "Disconnecting from server",
            ServerState.Offline => "The GagSpeak server is currently offline.",
            ServerState.Connected => string.Empty,
            ServerState.ConnectedDataSynced => string.Empty,
            ServerState.NoSecretKey => "No secret key is set for this current character. " +
            "\nTo create UID's for your alt characters, be sure to claim your account in the CK discord." +
            "\n\nOnce you have inserted a secret key, reload the plugin to be registered with the servers.",
            ServerState.VersionMisMatch => "Current Ver: " + MainHub.ClientVerString + Environment.NewLine
            + "Expected Ver: " + MainHub.ExpectedVerString +
            "\n\nThis Means that your client is outdated, and you need to update it." +
            "\n\nIf there is no update Available, then this message Likely Means Cordy is running some last minute tests " +
            "to ensure everyone doesn't crash with the latest update. Hang in there!",
            ServerState.Unauthorized => "You are Unauthorized to access GagSpeak Servers with this account due to an " +
            "Unauthorized Access. \n\nDetails:\n" + MainHub.AuthFailureMessage,
            _ => string.Empty
        };
    }

    public override void OnClose()
    {
        Mediator.Publish(new ClosedMainUiMessage());
        _sidePanelService.ClearDisplay();
        base.OnClose();
    }
}
