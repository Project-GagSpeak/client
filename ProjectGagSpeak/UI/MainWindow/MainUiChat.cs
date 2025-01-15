using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.Toybox;
using ImGuiNET;
using Lumina.Text.ReadOnly;
using OtterGui;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class MainUiChat : DisposableMediatorSubscriberBase
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly MainHub _apiHubMain;
    private readonly ClientData _playerData;
    private readonly GagGarbler _garbler;
    private readonly KinkPlateService _kinkPlateManager;
    private readonly UiSharedService _uiSharedService;
    private readonly DiscoverService _discoveryService;
    private readonly TutorialService _guides;

    public MainUiChat(ILogger<MainUiChat> logger, GagspeakMediator mediator,
        GagspeakConfigService mainConfig, MainHub apiHubMain, 
        ClientData playerManager, GagGarbler garbler,
        KinkPlateService kinkPlateManager, UiSharedService uiSharedService, 
        DiscoverService discoverService, TutorialService guides) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _apiHubMain = apiHubMain;
        _playerData = playerManager;
        _garbler = garbler;
        _kinkPlateManager = kinkPlateManager;
        _uiSharedService = uiSharedService;
        _discoveryService = discoverService;
        _guides = guides;
    }

    public void DrawDiscoverySection()
    {
        ImGuiUtil.Center("Global GagSpeak Chat");
        ImGui.Separator();
        DrawGlobalChatlog("GS_MainGlobal");
    }

    private bool shouldFocusChatInput = false;
    private bool showMessagePreview = false;
    private string NextChatMessage = string.Empty;

    public void DrawGlobalChatlog(string windowId)
    {
        using var scrollbar = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        // grab the content region of the current section
        var CurrentRegion = ImGui.GetContentRegionAvail();

        // grab the profile object from the profile service.
        var profile = _kinkPlateManager.GetKinkPlate(MainHub.PlayerUserData);
        if(profile.KinkPlateInfo.Disabled)
        {
            ImGui.Spacing();
            UiSharedService.ColorTextCentered("Social Features have been Restricted", ImGuiColors.DalamudRed);
            ImGui.Spacing();
            UiSharedService.ColorTextCentered("Cannot View Global Chat because of this.", ImGuiColors.DalamudRed);
            return;
        }
        // Calculate the height for the chat log, leaving space for the input text field
        float inputTextHeight = ImGui.GetFrameHeightWithSpacing();
        float chatLogHeight = CurrentRegion.Y - inputTextHeight;

        // Create a child for the chat log
        var region = new Vector2(CurrentRegion.X, chatLogHeight);
        using (ImRaii.Child($"###ChatlogChildGlobal"+windowId, region, false))
        {
            DiscoverService.GlobalChat.PrintChatLogHistory(showMessagePreview, NextChatMessage, region, windowId);
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.GlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        // Now draw out the input text field
        var nextMessageRef = NextChatMessage;

        // Set keyboard focus to the chat input box if needed
        if (shouldFocusChatInput)
        {
            // if we currently are focusing the window this is present on, set the keyboard focus.
            if(ImGui.IsWindowFocused())
            {
                ImGui.SetKeyboardFocusHere(0);
                shouldFocusChatInput = false;
            }
        }

        // Set width for input box and create it with a hint
        FontAwesomeIcon Icon = DiscoverService.GlobalChat.AutoScroll ? FontAwesomeIcon.ArrowDownUpLock : FontAwesomeIcon.ArrowDownUpAcrossLine;
        ImGui.SetNextItemWidth(CurrentRegion.X - _uiSharedService.GetIconButtonSize(Icon).X*2 - ImGui.GetStyle().ItemInnerSpacing.X*2);
        if (ImGui.InputTextWithHint("##ChatInputBox" + windowId, "chat message here...", ref nextMessageRef, 300))
        {
            // Update stored message
            NextChatMessage = nextMessageRef;
        }

        // Check if the input text field is focused and Enter is pressed
        if (ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            shouldFocusChatInput = true;

            // If message is empty, return
            if (string.IsNullOrWhiteSpace(NextChatMessage))
                return;

            // Process message if gagged
            if (_playerData.IsPlayerGagged && (_playerData.GlobalPerms?.LiveChatGarblerActive ?? false))
                NextChatMessage = _garbler.ProcessMessage(NextChatMessage);

            // Send message to the server
            Logger.LogTrace($"Sending Message: {NextChatMessage}");
            _apiHubMain.SendGlobalChat(new GlobalChatMessageDto(MainHub.PlayerUserData, NextChatMessage, _mainConfig.Current.PreferThreeCharaAnonName)).ConfigureAwait(false);

            // Clear message and trigger achievement event
            NextChatMessage = string.Empty;
            UnlocksEventManager.AchievementEvent(UnlocksEvent.GlobalSent);
        }

        // Update preview display based on input field activity
        showMessagePreview = ImGui.IsItemActive();

        // Toggle AutoScroll functionality
        ImUtf8.SameLineInner();
        if (_uiSharedService.IconButton(Icon))
            DiscoverService.GlobalChat.AutoScroll = !DiscoverService.GlobalChat.AutoScroll;
        UiSharedService.AttachToolTip("Toggles the AutoScroll Functionality (Current: " + (DiscoverService.GlobalChat.AutoScroll ? "Enabled" : "Disabled") + ")");

        // draw the popout button
        ImUtf8.SameLineInner();
        if (_uiSharedService.IconButton(FontAwesomeIcon.Expand, disabled: !KeyMonitor.ShiftPressed()))
            Mediator.Publish(new UiToggleMessage(typeof(GlobalChatPopoutUI)));
        UiSharedService.AttachToolTip("Open the Global Chat in a Popout Window--SEP--Hold SHIFT to activate!");
    }
}

