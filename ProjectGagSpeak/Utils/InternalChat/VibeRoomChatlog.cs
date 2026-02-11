using CkCommons;
using CkCommons.Chat;
using CkCommons.Gui;
using CkCommons.RichText;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using System.Globalization;

namespace GagSpeak.Utils;
public class VibeRoomChatlog : CkChatlog<GagSpeakChatMessage>, IMediatorSubscriber, IDisposable
{
    private static RichTextFilter AllowedTypes = RichTextFilter.All & ~RichTextFilter.Images;

    private readonly ILogger<VibeRoomChatlog> _logger;
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly ClientData _clientData;
    private readonly GagRestrictionManager _gags;
    private readonly VibeLobbyManager _lobbyManager;
    private readonly MufflerService _garbler;

    public VibeRoomChatlog(ILogger<VibeRoomChatlog> logger, GagspeakMediator mediator,
        MainHub hub, MainConfig config, ClientData clientData, GagRestrictionManager gags,
        VibeLobbyManager lobbyManager, MufflerService garbler) 
        : base(0, "VibeRoom Chat", 1000)
    {
        _logger = logger;
        Mediator = mediator;
        _hub = hub;
        _config = config;
        _clientData = clientData;
        _gags = gags;
        _lobbyManager = lobbyManager;
        _garbler = garbler;

        Mediator.Subscribe<VibeRoomChatMessage>(this, AddVibeRoomMessage);
    }

    public GagspeakMediator Mediator { get; }

    void IDisposable.Dispose()
    {
        Mediator.UnsubscribeAll(this);
        GC.SuppressFinalize(this);
    }

    public void SetAutoScroll (bool newState)
        => DoAutoScroll = newState;

    // Do not reveal extra info, respect privacy!!!
    protected override string ToTooltip(GagSpeakChatMessage message)
        => $"Sent @ {message.Timestamp.ToString("T", CultureInfo.CurrentCulture)}";

    // Add what we put in here soon.
    public void AddVibeRoomMessage(VibeRoomChatMessage message)
    {
        // get the display name by polling from the current vibe lobby participants.
        // If the user is not found do not send the message.
        var dispName = "UNKNOWN";
        if (message.Kinkster.Tier is CkSupporterTier.KinkporiumMistress)
            dispName = $"Mistress Cordy";
        // construct the chat message struct to add, and append it.
        AddMessage(new GagSpeakChatMessage(message.Kinkster, dispName, message.Message));
    }

    protected override void AddMessage(GagSpeakChatMessage newMsg)
    {
        _logger.LogDebug($"Adding Message: {newMsg.Message} from {newMsg.Name} ({newMsg.UID})", LoggerType.GlobalChat);
        // Cordy is special girl :3
        if (newMsg.Tier is CkSupporterTier.KinkporiumMistress)
        {
            // Force set the uid color to her favorite color.
            UserColors[newMsg.UID] = GsCol.ShopKeeperColor.Vec4();
            // allow any rich text tags, as she is a special case.
            var prefix = $"[img=RequiredImages\\Tier4Icon][rawcolor={GsCol.ShopKeeperColor.Uint()}]{newMsg.Name}[/rawcolor]: ";
            Messages.PushBack(newMsg with { Message = prefix + newMsg.Message });
            unreadSinceScroll++;
        }
        else
        {
            // Assign the sender color
            var col = ColorHelpers.RgbaVector4ToUint(AssignSenderColor(newMsg));
            // strip out the modifiers that are not allowed to prevent chaos in global chat.
            var sanitizedMsg = CkRichText.StripDisallowedRichTags(newMsg.Message, AllowedTypes);
            // append special formatting to the start of the message based on supporter type.
            var prefix = newMsg.Tier switch
            {
                CkSupporterTier.DistinguishedConnoisseur => $"[img=RequiredImages\\Tier3Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.EsteemedPatron => $"[img=RequiredImages\\Tier2Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.ServerBooster => $"[img=RequiredImages\\TierBoosterIcon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.IllustriousSupporter => $"[img=RequiredImages\\Tier1Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                _ => $"[rawcolor={col}]{newMsg.Name}[/rawcolor]: "
            };
            Messages.PushBack(newMsg with { Message = prefix + sanitizedMsg });
            unreadSinceScroll++;
        }
    }

    public override void DrawChatInputRow()
    {
        using var _ = ImRaii.Group();

        var Icon = DoAutoScroll ? FAI.ArrowDownUpLock : FAI.ArrowDownUpAcrossLine;
        var width = ImGui.GetContentRegionAvail().X;

        // Set keyboard focus to the chat input box if needed
        if (shouldFocusChatInput)
        {
            // if we currently are focusing the window this is present on, set the keyboard focus.
            if (ImGui.IsWindowFocused())
            {
                ImGui.SetKeyboardFocusHere(0);
                shouldFocusChatInput = false;
            }
        }

        ImGui.SetNextItemWidth(width - CkGui.IconButtonSize(Icon).X - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.InputTextWithHint($"##ChatInput{Label}{ID}", "type here...", ref previewMessage, 400);

        // Process submission Prevent losing chat focus after pressing the Enter key.
        if (ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            shouldFocusChatInput = true;
            OnSendMessage(previewMessage);
        }

        // Toggle AutoScroll functionality
        ImUtf8.SameLineInner();
        if (CkGui.IconButton(Icon))
            DoAutoScroll = !DoAutoScroll;
        CkGui.AttachToolTip($"Toggles AutoScroll (Current: {(DoAutoScroll ? "Enabled" : "Disabled")})");
    }

    protected override void OnMiddleClick(GagSpeakChatMessage message)
    { }

    protected override void OnSendMessage(string message)
    {
        shouldFocusChatInput = true;
        if (string.IsNullOrWhiteSpace(previewMessage))
            return;

        // Process message if gagged
        if ((_gags.ServerGagData?.IsGagged() ?? false) && (ClientData.Globals?.ChatGarblerActive ?? false))
            previewMessage = _garbler.ProcessMessage(previewMessage);

        // Send message to the server
        _logger.LogTrace($"Sending Message: {previewMessage}", LoggerType.GlobalChat);
        _hub.RoomSendChat(new(MainHub.OwnUserData, _config.Current.NicknameInVibeRooms, previewMessage)).ConfigureAwait(false);

        // Clear message and trigger achievement event
        previewMessage = string.Empty;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.VibeRoomChatSent);
    }

    protected override void DrawPopupInternal()
    {
        if (LastInteractedMsg is null)
            return;

        ImGui.Text("Test to see if the default string got in here at least.");
    }
}
