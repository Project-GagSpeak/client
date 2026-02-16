using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
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
using GagspeakAPI.Network;
using OtterGui.Text;
using OtterGuiInternal;
using System.Globalization;
using System.Reflection;

namespace GagSpeak.Gui.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MainUI : WindowMediatorSubscriberBase
{
    public const float AVATAR_SIZE = 190f;
    public const float MAIN_UI_WIDTH = 380f;

    private readonly MainConfig _config;
    private readonly AccountManager _account;
    private readonly MainHub _hub;
    private readonly MainMenuTabs _tabMenu;
    private readonly SidePanelService _sidePanel;
    private readonly RequestsManager _requests;
    private readonly KinksterManager _kinksters;
    private readonly TutorialService _guides;
    private readonly HomeTab _homepageTab;
    private readonly RequestsTab _requestsTab;
    private readonly WhitelistTab _whitelistTab;
    private readonly PatternHubTab _patternHubTab;
    private readonly MoodleHubTab _moodlesHubTab;
    private readonly GlobalChatTab _globalChatTab;

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
        _sidePanel = sidePanel;
        _requests = requestmanager;
        _kinksters = kinksters;
        _guides = guides;
        _homepageTab = home;
        _requestsTab = requests;
        _whitelistTab = whitelist;
        _patternHubTab = patternHub;
        _moodlesHubTab = moodlesHub;
        _globalChatTab = globalChat;

        // display info about the folders
        var ver = Assembly.GetExecutingAssembly().GetName().Version!;
        WindowName = $"GagSpeak v{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}###GagSpeakMainUI";
        Flags |= WFlags.NoDocking;

        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(MAIN_UI_WIDTH, 548), new Vector2(MAIN_UI_WIDTH, 2000));
        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.Book, "Changelog", () => Mediator.Publish(new UiToggleMessage(typeof(ChangelogUI))))
            .Add(FAI.Cog, "Settings", () => Mediator.Publish(new UiToggleMessage(typeof(SettingsUi))))
            .AddTutorial(_guides, TutorialType.MainUi)
            .Build();

        // Default to open if the user desires for it to be open.
        if (_config.Current.OpenMainUiOnStartup)
            Toggle();
        // Update the tab menu selection.
        _tabMenu.TabSelection = _config.Current.MainUiTab;

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (_) => IsOpen = true);
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) => IsOpen = false);

        // make sure opening the side panel also opens the main ui and selects whitelist tab
        Mediator.Subscribe<OpenKinksterSidePanel>(this, _ =>
        {
            IsOpen = true;
            _tabMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist;
        });
    }

    public static Vector2 LastPos { get; private set; } = Vector2.Zero;
    public static Vector2 LastSize { get; private set; } = Vector2.Zero;
    public static Vector2 LastBottomTabMenuPos { get; private set; } = Vector2.Zero;

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
        var winContentWidth = CkGui.GetWindowContentRegionWidth();

        var disableButtons = MainHub.ServerStatus is (ServerState.NoSecretKey or ServerState.VersionMisMatch or ServerState.Unauthorized);
        DrawTopBar();

        LastPos = ImGui.GetWindowPos();
        LastSize = ImGui.GetWindowSize();

        // If we are not connected, then do not draw any further.
        if (!MainHub.IsConnected)
        {
            if (disableButtons)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Separator();
                // the wrapped text explanation based on the error.
                CkGui.ColorTextWrapped(GetServerError(), ImGuiColors.DalamudWhite);
            }
            return;
        }


        // If we are creating a request to send to another user, draw this first.
        if (_creatingRequest)
            DrawRequestCreator(winContentWidth, ImUtf8.ItemInnerSpacing.X);

        // draw the bottom tab bar
        _tabMenu.Draw(winContentWidth);

        // display content based on the tab selected
        switch (_tabMenu.TabSelection)
        {
            case MainMenuTabs.SelectedTab.Homepage:
                _homepageTab.DrawSection();
                break;
            case MainMenuTabs.SelectedTab.Requests:
                _requestsTab.DrawSection();
                break;
            case MainMenuTabs.SelectedTab.Whitelist:
                _whitelistTab.DrawSection();
                break;
            case MainMenuTabs.SelectedTab.PatternHub:
                _patternHubTab.DrawPatternHub();
                _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.PatternResults, LastPos, LastSize,
                    () => { _tabMenu.TabSelection = MainMenuTabs.SelectedTab.MoodlesHub; });
                break;
            case MainMenuTabs.SelectedTab.MoodlesHub:
                _moodlesHubTab.DrawMoodlesHub();
                break;
            case MainMenuTabs.SelectedTab.GlobalChat:
                _globalChatTab.DrawSection();
                break;
        }
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
                // Add the request if it was successful!
                if (res.ErrorCode is GagSpeakApiEc.Success)
                    _requests.AddNewRequest(res.Value!);

                // Clear values
                _uidToSentTo = string.Empty;
                _requestMessage = string.Empty;
                _creatingRequest = false;
            });
        }
        if (!string.IsNullOrEmpty(_uidToSentTo))
            CkGui.AttachToolTip($"Send Pair Request to {_uidToSentTo}");

        // draw a attached message field as well if they want.
        ImGui.SetNextItemWidth(availableXWidth);
        ImGui.InputTextWithHint("##pairAddOptionalMessage", "Attach Msg to Request (Optional)", ref _requestMessage, 100);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.AttachingMessages, WindowPos, WindowSize, () =>
        {
            _creatingRequest = !_creatingRequest;
            _tabMenu.TabSelection = MainMenuTabs.SelectedTab.Requests;
        });
        ImGui.Separator();
    }

    private void DrawTopBar()
    {
        var disableButtons = MainHub.ServerStatus is (ServerState.NoSecretKey or ServerState.VersionMisMatch or ServerState.Unauthorized);
        // Get the window pointer before we draw.
        var winPtr = ImGuiInternal.GetCurrentWindow();
        // Expand the region of the topbar to cross the full width.
        var winPadding = ImGui.GetStyle().WindowPadding;
        // ImGui hides the actual possible clip-rect-min from going to 0,0.
        // This is because the ClipRect skips over the titlebar, so if WinPadding is 8,8
        // then the content region min returns 8,40
        // Note to only subtract the X padding. ClipRectMin gets Y correctly.
        var winClipX = winPadding.X / 2;
        var minPos = winPtr.DrawList.GetClipRectMin() + new Vector2(-winClipX, winPadding.Y);
        var maxPos = winPtr.DrawList.GetClipRectMax() + new Vector2(winClipX, 0);
        // Expand the area for our custom header.
        winPtr.DrawList.PushClipRect(minPos, maxPos, false);

        // Get the expanded width
        var topBarWidth = maxPos.X - minPos.X;
        var sideWidth = ImGui.CalcTextSize("Connecting").X + CkGui.IconSize(FAI.Satellite).X + ImUtf8.ItemSpacing.X * 3;
        var height = CkGui.CalcFontTextSize("A", UiFontService.Default150Percent).Y;

        if (DrawAddUser(winPtr, new Vector2(sideWidth, height), minPos, disableButtons || !MainHub.IsConnected))
            _creatingRequest = !_creatingRequest;
        CkGui.AttachToolTip("Add a new Kinkster");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.AddingKinksters, WindowPos, WindowSize, () => _creatingRequest = !_creatingRequest);

        ImGui.SetCursorScreenPos(minPos + new Vector2(sideWidth, 0));
        DrawConnectedUsers(winPtr, new Vector2(topBarWidth - sideWidth * 2, height), topBarWidth);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.InitialWelcome, WindowPos, WindowSize);

        ImGui.SameLine(topBarWidth - sideWidth);
        var blockStateChange = MainHub.ServerStatus is ServerState.Reconnecting or ServerState.Disconnecting;
        if (DrawConnection(winPtr, new Vector2(sideWidth, height), ImGui.GetCursorScreenPos(), disableButtons || blockStateChange))
        {
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
        CkGui.AttachToolTip($"{(MainHub.IsConnected ? "Disconnect from" : "Connect to")} {MainHub.MAIN_SERVER_NAME}--SEP--Current Status: {MainHub.ServerStatus}");
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ConnectionState, WindowPos, WindowSize, () => _tabMenu.TabSelection = MainMenuTabs.SelectedTab.Whitelist);

        winPtr.DrawList.PopClipRect();
    }

    private bool DrawAddUser(ImGuiWindowPtr winPtr, Vector2 region, Vector2 minPos, bool disabled)
    {
        if (winPtr.SkipItems)
            return false;

        var id = ImGui.GetID("add-kinkster");
        var style = ImGui.GetStyle();
        var shadowSize = ImGuiHelpers.ScaledVector2(1);
        var styleOffset = ImGuiHelpers.ScaledVector2(2f);
        var buttonPadding = styleOffset + ImUtf8.FramePadding;
        var bend = region.Y * .5f;
        var min = minPos;
        var hitbox = new ImRect(min, min + region);

        ImGuiInternal.ItemSize(region);
        if (!ImGuiP.ItemAdd(hitbox, id, null))
            return false;

        // Process interaction with this 'button'
        var hovered = false;
        var active = false;
        var clicked = ImGuiP.ButtonBehavior(hitbox, id, ref hovered, ref active);

        // Render possible nav highlight space over the bounding box region.
        ImGuiP.RenderNavHighlight(hitbox, id);
        // Define our colors based on states.
        uint shadowCol = 0x64000000;
        uint borderCol = CkGui.ApplyAlpha(0xDCDCDCDC, GetBorderAlpha(active, hovered, disabled));
        uint bgCol = CkGui.ApplyAlpha(0x64000000, GetBgAlpha(active, hovered, disabled));
        uint textCol = CkGui.ApplyAlpha(ImGui.GetColorU32(ImGuiCol.Text), disabled ? 0.5f : 1f);

        winPtr.DrawList.AddRectFilled(min, hitbox.Max, shadowCol, bend, ImDrawFlags.RoundCornersRight);
        winPtr.DrawList.AddRectFilled(min + new Vector2(0, shadowSize.Y), hitbox.Max - shadowSize, borderCol, bend, ImDrawFlags.RoundCornersRight);
        winPtr.DrawList.AddRectFilled(min + new Vector2(0, styleOffset.Y), hitbox.Max - styleOffset, bgCol, bend, ImDrawFlags.RoundCornersRight);

        // Text computation.
        var textSize = ImGui.CalcTextSize("Add Kinkster");
        var textPos = min + ((region - textSize) / 2f);
        winPtr.DrawList.AddText("Add Kinkster", textPos, textCol);

        return clicked && !disabled;
    }

    private void DrawConnectedUsers(ImGuiWindowPtr winPtr, Vector2 region, float topBarWidth)
    {
        using var font = UiFontService.Default150Percent.Push();

        var userCount = MainHub.OnlineUsers.ToString(CultureInfo.InvariantCulture);
        var text = MainHub.IsConnected ? $"{userCount} Online" : GagspeakEx.GetCenterStateText();
        var textSize = ImGui.CalcTextSize(text);
        var offsetX = (topBarWidth - textSize.X - ImUtf8.ItemInnerSpacing.X) / 2;

        // Make two gradients from the left and right, based on region.
        var posMin = winPtr.DC.CursorPos;
        var posMax = posMin + region;
        var halfRegion = region with { X = region.X * .5f };
        var innerCol = ColorHelpers.Fade(ImGui.GetColorU32(ImGuiCol.TextDisabled), .75f);
        var outerCol = ColorHelpers.Fade(ImGui.GetColorU32(ImGuiCol.TextDisabled), .99f);

        winPtr.DrawList.AddRectFilledMultiColor(posMin, posMin + halfRegion, outerCol, innerCol, innerCol, outerCol);
        winPtr.DrawList.AddRectFilledMultiColor(posMin with { X = posMin.X + halfRegion.X }, posMax, innerCol, outerCol, outerCol, innerCol);

        ImGui.SetCursorPosX(offsetX);
        using (ImRaii.Group())
        {
            if (MainHub.IsConnected)
            {
                CkGui.ColorText(userCount, GsCol.VibrantPink.Vec4Ref());
                CkGui.TextInline("Online");
            }
            else
            {
                CkGui.ColorText(text, GagspeakEx.ServerStateColor());
            }
        }
    }

    private bool DrawConnection(ImGuiWindowPtr winPtr, Vector2 region, Vector2 minPos, bool disabled)
    {
        if (winPtr.SkipItems)
            return false;

        var id = ImGui.GetID("change-state");
        var style = ImGui.GetStyle();
        var shadowSize = ImGuiHelpers.ScaledVector2(1);
        var styleOffset = ImGuiHelpers.ScaledVector2(2f);
        var buttonPadding = styleOffset + ImUtf8.FramePadding;
        var bend = region.Y * .5f;
        var min = minPos;
        var hitbox = new ImRect(min, min + region);

        ImGuiInternal.ItemSize(region);
        if (!ImGuiP.ItemAdd(hitbox, id, null))
            return false;

        // Process interaction with this 'button'
        var hovered = false;
        var active = false;
        var clicked = ImGuiP.ButtonBehavior(hitbox, id, ref hovered, ref active);

        // Render possible nav highlight space over the bounding box region.
        ImGuiP.RenderNavHighlight(hitbox, id);

        // Define our colors based on states.
        uint shadowCol = 0x64000000;
        uint borderCol = CkGui.ApplyAlpha(0xDCDCDCDC, GetBorderAlpha(active, hovered, disabled));
        uint bgCol = CkGui.ApplyAlpha(0x64000000, GetBgAlpha(active, hovered, disabled));

        winPtr.DrawList.AddRectFilled(min, hitbox.Max, shadowCol, bend, ImDrawFlags.RoundCornersLeft);
        winPtr.DrawList.AddRectFilled(min + shadowSize, hitbox.Max - new Vector2(0, shadowSize.Y), borderCol, bend, ImDrawFlags.RoundCornersLeft);
        winPtr.DrawList.AddRectFilled(min + styleOffset, hitbox.Max - new Vector2(0, styleOffset.Y), bgCol, bend, ImDrawFlags.RoundCornersLeft);

        // Text computation.
        var icon = GagspeakEx.ServerStateIcon(MainHub.ServerStatus);
        var text = GagspeakEx.GetButtonStateText();
        var stateCol = GagspeakEx.ServerStateColor().ToUint();
        var iconSize = CkGui.IconSize(icon);
        var textSize = ImGui.CalcTextSize(text);
        var iconTextWidth = iconSize.X + style.ItemInnerSpacing.X + textSize.X;
        var iconPos = min + new Vector2((region.X - iconTextWidth) / 2f, (region.Y - textSize.Y) / 2f);
        var textPos = iconPos + new Vector2(iconSize.X + style.ItemInnerSpacing.X, 0);
        // Then draw out the icon and text.
        using (Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            winPtr.DrawList.AddText(icon.ToIconString(), iconPos, stateCol);
        winPtr.DrawList.AddText(text, textPos, stateCol);
        // If its true, make sure our ServerStatus is Connected, or if its false, make sure our ServerStatus is Disconnected or offline.
        return clicked && !disabled;
    }

    // For Border we want it to be brighter the more active it is.
    public static float GetBorderAlpha(bool active, bool hovered, bool disabled)
        => disabled ? 0.27f : active ? 0.7f : hovered ? 0.63f : 0.39f;

    // For the background we want it to have less alpha the brighter we want it.
    public static float GetBgAlpha(bool active, bool hovered, bool disabled)
        => disabled ? 0.44f : active ? 0.19f : hovered ? 0.26f : 0.39f;

    /// <summary>
    ///     Retrieves the various server error messages based on the current server state.
    /// </summary>
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
        _sidePanel.ClearDisplay();
        base.OnClose();
    }
}
