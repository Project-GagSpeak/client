using CkCommons.Gui;
using CkCommons.RichText;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Chat;
using GagspeakAPI.Reporting;
using OtterGui.Text;
using OtterGuiInternal;
using System.Globalization;

namespace GagSpeak.Gui.Chat;

/// <summary>
///   Inherits from RichEmoteChatDrawer for catered functionality GlobalChat needs.
/// </summary>
public class GlobalChatDrawer : RichEmoteChatDrawer
{
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly PairService _pairService;

    private string _requestMsg = string.Empty;

    public GlobalChatDrawer(ILogger<GlobalChatDrawer> logger, GagspeakMediator mediator,
        GlobalChatLog globalChat, MainHub hub, ChatColors common, ChatConfig chatConfig,
        FavoritesConfig favorites, GsEmojiLoader emojis, ChatFontManager chatFont, 
        BlockService blocks, ChatService chatService, PairService pairs)
        : base("GlobalChat", logger, globalChat, common, chatConfig, favorites, emojis, chatFont, blocks, chatService)
    {
        _mediator = mediator;
        _hub = hub;
        _pairService = pairs;
    }

    protected override void FlushLocalData()
    {
        base.FlushLocalData();
        _requestMsg = string.Empty;
    }

    private void DrawMessagePrefix(NewGsChatMessage message)
    {
        // Image Icon, if visible.
        var supporterData = CosmeticService.GetSupporterInfo(message.Sender);
        if (supporterData.SupporterWrap is { } valid)
        {
            ImGui.Image(valid.Handle, new Vector2(ImGui.GetTextLineHeight()));
            CkGui.AttachTooltip(supporterData.Tooltip);
            ImUtf8.SameLineInner();
        }

        using var _ = ImRaii.Group();
        // Either get the senders name color, or assign it based on their UserData color settings.
        var nameColor = _common.GetOrCreateValue(message.Sender);
        CkGui.ColorText(message.DisplayName, nameColor);
        ImGui.SameLine(0, 0);
        ImGui.TextUnformatted(": ");
    }

    protected override void DrawChatMessage(NewGsChatMessage message, float width)
    {
        if (_blocks.IsMuted(message.SenderId))
        {
            DrawIgnoredMessageRow(message, width);
            return;
        }

        DrawMessagePrefix(message);
        HandleDetections(message);
        // Then draw the flow-wrapped message
        ImUtf8.SameLineInner();
        NewRichText.TextFlowWrappedOrDummy(message.Message, id: ChatLog!.ID + message.MsgId);
    }

    public override void DrawChatInputRow()
    {
        var radarLog = (GlobalChatLog)ChatLog!;
        using var _ = ImRaii.Group();
        
        var window = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();
        var scrollIcon = radarLog.AutoScroll ? FAI.ArrowDownUpLock : FAI.ArrowDownUpAcrossLine;
        var rWidth = CkGui.IconButtonSize(scrollIcon).X + CkGui.IconButtonSize(FAI.Heart).X + (ImUtf8.ItemInnerSpacing.X * 2);

        var width = ImGui.GetContentRegionAvail().X;
        var size = new Vector2(width, ImUtf8.FrameHeight);

        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, pos + size);
        var id = ImGui.GetID($"chat-input-{radarLog.ID}");

        // Reserve the layout space for the entire bar
        ImGuiInternal.ItemSize(size, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(bb, id))
            return;

        ImGuiInternal.RenderFrame(bb.Min, bb.Max, ImGui.GetColorU32(ImGuiCol.FrameBg), false, style.FrameRounding);
        using var s = ImRaii.PushColor(ImGuiCol.FrameBg, 0).Push(ImGuiCol.Border, 0);

        ImGui.SetCursorScreenPos(bb.Min);

        if (shouldFocusInput)
        {
            ImGui.SetWindowFocus();
            ImGui.SetKeyboardFocusHere(0);
            shouldFocusInput = false;
        }

        ImGui.SetNextItemWidth(width - rWidth);
        var preview = $"Message Global Chat...";
        ImGui.InputTextWithHint($"##chat-input-{radarLog.ID}", preview, ref previewMessage, 400, ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.CallbackAlways, OnChatInputCallback);
        // Process submission Prevent losing chat focus after pressing the Enter key.
        if (ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            SendMessage(previewMessage);
            shouldFocusInput = true;
        }

        // Emote and scroll-lock buttons.
        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, GsCol.VibrantPink.Uint(), selectingEmotes))
            if (CkGui.IconButton(FAI.Heart, inPopup: true))
                selectingEmotes = !selectingEmotes;

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(scrollIcon, inPopup: true))
            radarLog.AutoScroll = !radarLog.AutoScroll;
        CkGui.AttachTooltip($"Toggles AutoScroll (Current: {(radarLog.AutoScroll ? "Enabled" : "Disabled")})");
    }

    // We can branch off to sanction and radar chat both this way, yay!!
    protected override void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _chatService.SendGlobalChatMessage(new ChatlogId(GsChatKind.Global, "GlobalChat"), message);
        base.SendMessage(message);
    }

    protected override void HandleDetections(NewGsChatMessage msg)
    {
        if (ImGui.IsItemHovered())
        {
            lastHovered = msg;
            var tooltip = "--COL--Right-Click to view Interactions--COL--";
            if (_chatConfig.Data.Timestamps)
                tooltip = $"Sent @ {msg.TimestampUTC.ToLocalTime().ToString("T", CultureInfo.CurrentCulture):T}--NL--{tooltip}";
            CkGui.ToolTipInternal(tooltip, ImGuiColors.ParsedGrey);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Middle) && MainHub.Reputation.CanViewProfiles)
        {
            // Need to ensure we have valid access to open.
            var flags = ((GlobalChatLog)ChatLog!).ChatUsers.GetValueOrDefault(msg.Sender, (false, ChatFlags.None)).Item2;
            if (flags.HasAny(ChatFlags.AllowProfileViewing))
                _mediator.Publish(new OpenUserLightProfileMessage(msg.Sender));
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            inPopup = msg;
            ImGui.OpenPopup($"ckchatlog-{ChatLog!.ID}-msg-actions");
        }
    }

    protected override void DrawContentMenu(NewGsChatMessage msg)
    {
        var shiftHeld = ImGui.GetIO().KeyShift;
        var ctrlHeld = ImGui.GetIO().KeyCtrl;
        var isOwnMsg = msg.Sender.UID == MainHub.UID;
        var disableSilence = !ctrlHeld || isOwnMsg;
        var disableBlock = !(ctrlHeld && shiftHeld) || isOwnMsg;

        var flags = ((GlobalChatLog)ChatLog!).ChatUsers.GetValueOrDefault(msg.Sender, (false, ChatFlags.None)).Item2;
        var dispName = msg.DisplayName;

        using (Fonts.GameFont.Push())
            CkGui.TextUnderlined(dispName);

        if (MainHub.Reputation.CanViewProfiles)
        {
            var canProfile = flags.HasAny(ChatFlags.AllowProfileViewing);
            if (CkGui.SelectableEx("Open Profile", !canProfile))
            {
                _mediator.Publish(new OpenUserLightProfileMessage(msg.Sender));
                ImGui.CloseCurrentPopup();
            }
            CkGui.AttachTooltip(!canProfile ? "This user does not allow Profile Viewing." : $"Opens {dispName}'s profile.", ImGuiHoveredFlags.AllowWhenDisabled);
        }

        if (isOwnMsg)
            return;

        if (CkGui.SelectableEx("Send a Direct Message", !flags.HasAny(ChatFlags.AllowDirectMessages)))
        {
            var dmChatId = string.CompareOrdinal(MainHub.UID, msg.SenderId) < 0 ? $"{MainHub.UID}-{msg.SenderId}" : $"{msg.SenderId}-{MainHub.UID}";
            var chatlogId = new ChatlogId(GsChatKind.Direct, dmChatId);
            var openInChatUi = !_chatConfig.Data.ShowDMsInChatbox || ImGui.GetIO().KeyShift;
            if (openInChatUi)
            {
                _chatService.GetOrCreateDMLog(msg.Sender, chatlogId);
                _mediator.Publish(new UiToggleMessage(typeof(ChatWindowUI), ToggleType.Show));
            }
            else
            {
                _chatService.ChatlogOverride = chatlogId;
                ChatHooks.SetChatInputFocus();
            }
            ImGui.CloseCurrentPopup();
        }
        if (ImGui.IsItemHovered())
        {
            using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 6f)
                .Push(ImGuiStyleVar.WindowRounding, 4f)
                .Push(ImGuiStyleVar.PopupBorderSize, 1f);
            using var c = ImRaii.PushColor(ImGuiCol.Border, GsCol.VibrantPink.Vec4());
            using (ImRaii.Tooltip())
            {
                ImGui.TextUnformatted("Send user a DM");
                CkGui.ColorTextInline("(Direct Message)", ImGuiColors.ParsedGrey);
                if (_chatConfig.Data.ShowDMsInChatbox)
                {
                    CkGui.ColorText("[Shift + L-Click]:", ImGuiColors.DalamudOrange);
                    CkGui.TextInline("Open in UI instead of natively.");
                }
            }
        }

        if (CkGui.SelectableEx($"Mute {dispName}", disableSilence || isOwnMsg))
        {
            _blocks.MuteUser(msg.Sender.UID);
            ImGui.CloseCurrentPopup();
            return;
        }
        CkGui.AttachTooltip($"Hides messages from {dispName} until unmuted or plugin restart.--NL--" +
            $"--COL--Must hold CTRL to select.--COL--", ImGuiColors.ParsedGrey, ImGuiHoveredFlags.AllowWhenDisabled);

        var canPair = flags.HasAny(ChatFlags.AllowRequests);
        var requestExists = _pairService.RequestExistsFor(msg.Sender);
        var disableReq = !canPair || requestExists || !shiftHeld || string.IsNullOrWhiteSpace(_requestMsg);

        if (CkGui.SelectableEx("Send Request", disableReq))
        {
            _hub.UserCreatePairRequest(new(msg.Sender, true, _requestMsg)).ConfigureAwait(false);
            ImGui.CloseCurrentPopup();
        }
        var pairTooltip = !canPair ? "This kinkster does not accept Requests."
            : requestExists ? "A request already exists for this kinkster."
            : $"Send Request to {dispName}.--NL--" +
              $"--COL--Must hold SHIFT and attach a message to select.--COL--";
        CkGui.AttachTooltip(pairTooltip, ImGuiColors.ParsedGrey, ImGuiHoveredFlags.AllowWhenDisabled);
        if (!disableReq)
        {
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 20);
            ImGui.InputTextWithHint("##attachedPairMsg", "Attached Request Msg..", ref _requestMsg, 150);
            ImGui.Separator();
        }

        if (ImGui.Selectable($"Report {dispName}"))
        {
            _mediator.Publish(new OpenReportUIMessage(ReportKind.GlobalChat, msg.Sender, ChatLog, msg.MsgId));
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachTooltip($"Report {dispName} for their chat behavior.");
    }
}
