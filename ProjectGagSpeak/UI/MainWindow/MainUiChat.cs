using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
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
    private readonly MainHub _apiHubMain;
    private readonly PlayerCharacterData _playerManager;
    private readonly GagManager _gagManager;
    private readonly KinkPlateService _kinkPlateManager;
    private readonly UiSharedService _uiSharedService;
    private readonly DiscoverService _discoveryService;
    private readonly TutorialService _guides;

    public MainUiChat(ILogger<MainUiChat> logger, GagspeakMediator mediator, 
        MainHub apiHubMain, PlayerCharacterData playerManager, GagManager gagManager,
        KinkPlateService kinkPlateManager, UiSharedService uiSharedService, 
        DiscoverService discoverService, TutorialService guides) : base(logger, mediator)
    {
        _apiHubMain = apiHubMain;
        _playerManager = playerManager;
        _gagManager = gagManager;
        _kinkPlateManager = kinkPlateManager;
        _uiSharedService = uiSharedService;
        _discoveryService = discoverService;
        _guides = guides;
    }

    public void DrawDiscoverySection() => DrawGlobalChatlog();

    private bool shouldFocusChatInput = false;
    private bool showMessagePreview = false;
    private string NextChatMessage = string.Empty;

    private void DrawGlobalChatlog()
    {
        using var scrollbar = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        // grab the content region of the current section
        var CurrentRegion = ImGui.GetContentRegionAvail();
        ImGuiUtil.Center("Global GagSpeak Chat");
        ImGui.Separator();

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
        var region = new Vector2(CurrentRegion.X, chatLogHeight - inputTextHeight);
        using (var chatlogChild = ImRaii.Child($"###ChatlogChildGlobal", region, false))
        {
            DiscoverService.GlobalChat.PrintChatLogHistory(showMessagePreview, NextChatMessage, region);
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.GlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize());

        // Now draw out the input text field
        var nextMessageRef = NextChatMessage;

        // Set keyboard focus to the chat input box if needed
        if (shouldFocusChatInput)
        {
            ImGui.SetKeyboardFocusHere(0);
            shouldFocusChatInput = false;
        }

        // Set width for input box and create it with a hint
        FontAwesomeIcon Icon = DiscoverService.GlobalChat.AutoScroll ? FontAwesomeIcon.ArrowDownUpLock : FontAwesomeIcon.ArrowDownUpAcrossLine;
        ImGui.SetNextItemWidth(CurrentRegion.X - _uiSharedService.GetIconButtonSize(Icon).X - ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGui.InputTextWithHint("##ChatInputBox", "chat message here...", ref nextMessageRef, 300))
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
            if (_playerManager.IsPlayerGagged)
                NextChatMessage = _gagManager.ProcessMessage(NextChatMessage);

            // Send message to the server
            Logger.LogTrace($"Sending Message: {NextChatMessage}");
            _apiHubMain.SendGlobalChat(new GlobalChatMessageDto(MainHub.PlayerUserData, NextChatMessage)).ConfigureAwait(false);

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
    }
}

