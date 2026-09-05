using CkCommons.Gui;
using CkCommons.RichChat;
using CkCommons.RichText;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Chat;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.Reporting;
using GagspeakAPI.User;
using OtterGui.Text;
using OtterGuiInternal;
using System.Globalization;

namespace GagSpeak.Gui.Chat;

// A Drawer for direct, sanction, and radar chats.
public class HybridChatDrawer : RichEmoteChatDrawer
{
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly KinkPlateService _profiles;
    private readonly PairService _pairService;

    private string _requestMsg = string.Empty;

    // Profile requesting.
    private float _lastScrollY = -1f;
    private DateTime _lastScrollIdle = DateTime.MinValue;
    private HashSet<UserData> _profileRequestBatch = new(UserDataComparer.Instance);
    private double _debounceMs = 750;

    public HybridChatDrawer(ILogger<HybridChatDrawer> logger, GagspeakMediator mediator,
        MainHub hub, ChatColors common, ChatConfig chatConfig, FavoritesConfig favorites,
        GsEmojiLoader emotes, ChatFontManager chatFont, BlockService blocks, 
        ChatService chatService, PairService pairService, KinkPlateService profiles)
        : base("GagSpeakHybridChat", logger, common, chatConfig, favorites, emotes, chatFont, blocks, chatService)
    {
        _mediator = mediator;
        _hub = hub;
        _profiles = profiles;
        _pairService = pairService;
    }

    public bool UseDiscordFormat { get; set; } = true;

    protected override void PostDraw(Vector2 inputMin)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));
        var drawTextPreview = !string.IsNullOrWhiteSpace(previewMessage);
        if (drawTextPreview)
            DrawTextPreview(previewMessage, inputMin, "hybrid-text-preview");

        if (selectingEmotes)
        {
            var drawPos = drawTextPreview ? ImGui.GetItemRectMin() : inputMin;
            DrawQuickEmoteWindow(drawPos);
        }
    }

    /// <summary>
    ///   Ensures the following draw calls after operate on the spesified log.
    /// </summary>
    public void EnsureChatLog(RichChatLog<NewGsChatMessage>? chatLog)
    {
        if (string.Equals(chatLog?.ID, ChatLog?.ID, StringComparison.Ordinal))
            return;

        ChatLog = chatLog;
        FlushLocalData();
    }

    public void Draw(RichChatLog<NewGsChatMessage>? chatLog, WFlags flags = WFlags.None)
    {
        EnsureChatLog(chatLog);
        Draw(flags);
    }

    protected override void DrawHistoryInternal(IEnumerable<NewGsChatMessage> messages, float width)
    {
        var currentScrollY = ImGui.GetScrollY();
        var isScrolling = currentScrollY != _lastScrollY;
        if (isScrolling)
        {
            // User is actively scrolling: wipe the batch so fly-by profiles are discarded
            _profileRequestBatch.Clear();
            _lastScrollY = currentScrollY;
            _lastScrollIdle = DateTime.MinValue;
        }

        using var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImUtf8.ItemSpacing with { Y = 1 });
        _chatFont.PushFont();
        try
        {
            _iconSize = ImGuiHelpers.ScaledVector2(ImUtf8.TextHeight * 2);
            _offsetX = _iconSize.X + ImUtf8.ItemSpacing.X;
            _winPtr = ImGuiInternal.GetCurrentWindow();
            base.DrawHistoryInternal(messages, width);
        }
        finally
        {
            _chatFont.PopFont();
        }

        // If the scroll position has settled (user stopped scrolling)
        if (_profileRequestBatch.Count is 0 || isScrolling)
            return;
        // Otherwise, if not scrolling process batch profile requesting.
        if (_lastScrollIdle == DateTime.MinValue && _profileRequestBatch.Count > 0)
            _lastScrollIdle = DateTime.UtcNow;
        // Once settled for the debounce duration, fire the bulk request for the resting view
        if (_profileRequestBatch.Count > 0 && _lastScrollIdle != DateTime.MinValue)
        {
            if ((DateTime.UtcNow - _lastScrollIdle).TotalMilliseconds >= _debounceMs)
            {
                _profiles.GetUserProfiles(_profileRequestBatch.ToList());
                _profileRequestBatch.Clear();
                _lastScrollIdle = DateTime.MinValue;
            }
        }
    }


    protected override void DrawChatMessage(NewGsChatMessage message, float width)
    {
        if (_blocks.IsMuted(message.SenderId))
            DrawIgnoredMessageRow(message, width);
        else if (UseDiscordFormat)
            DrawDiscordFormattedMessage(message, width);
        else
            DrawRadarFormattedMessage(message, width);
    }

    private void DrawRadarFormattedMessage(NewGsChatMessage message, float width)
    {
        // Image Icon, if visible.
        var supporterData = CosmeticService.GetSupporterInfo(message.Sender);
        if (supporterData.SupporterWrap is { } valid)
        {
            ImGui.Image(valid.Handle, new Vector2(ImGui.GetTextLineHeight()));
            CkGui.AttachTooltip(supporterData.Tooltip);
            ImUtf8.SameLineInner();
        }

        using (ImRaii.Group())
        {
            // Either get the senders name color, or assign it based on their UserData color settings.
            var nameColor = _common.GetOrCreateValue(message.Sender);
            CkGui.ColorText(message.DisplayName, nameColor);
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(": ");
        }
        HandleDetections(message);
        // Then draw the flow-wrapped message
        ImUtf8.SameLineInner();
        NewRichText.TextFlowWrappedOrDummy(message.Message, id: ChatLog!.ID + message.MsgId + "hybrid-drawer");
    }

    private void DrawDiscordFormattedMessage(NewGsChatMessage message, float width)
    {
        // If the message is not the first in the chain, just print it normally at the offset.
        if (!message.FirstInMsgChain)
        {
            ImGui.SetCursorPosX(_offsetX);
            NewRichText.TextWrappedOrDummy(message.Message, id: ChatLog!.ID + message.MsgId + "hybrid-drawer");
            return;
        }

        var avatarPos = _winPtr.DC.CursorPos;
        ImGui.Dummy(_iconSize);
        if (CkGuiClip.WasItemVisible())
        {
            // If visible check if they were in the service.
            if (_profiles.Contains(message.Sender))
            {
                var profile = _profiles.GetUserProfile(message.Sender);
                _winPtr.DrawList.AddDalamudImageRounded(profile.GetIconWrapOrDefault(), avatarPos, _iconSize, _iconSize.X);
                if (ImGui.IsItemHovered())
                {
                    if (profile.GetIconWrapOrDefault() is { } wrap)
                    {
                        var ttSize = _iconSize * 3;
                        using (ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, ttSize.Y * .5f).Push(ImGuiStyleVar.WindowPadding, Vector2.Zero))
                        using (ImRaii.Tooltip())
                        {
                            var pos = ImGui.GetCursorScreenPos();
                            ImGui.GetWindowDrawList().AddImageRounded(profile.GetIconWrapOrDefault().Handle, pos, pos + ttSize, Vector2.Zero, Vector2.One, uint.MaxValue, ttSize.Y * .5f);
                            ImGui.Dummy(ttSize);
                        }
                    }
                }
            }
            else
            {
                // Otherwise append them to the next profile request batch.
                _profileRequestBatch.Add(message.Sender);
                _winPtr.DrawList.AddDalamudImageRounded(CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg], avatarPos, _iconSize, _iconSize.X);
            }
        }
        ImGui.SameLine(_offsetX);

        // DisplayName, SupporterIcon, Timestamp, followed by message, all in a group.
        using var _ = ImRaii.Group();

        CkGui.ColorText(message.DisplayName, _common.GetOrCreateValue(message.Sender));
        HandleDetections(message);

        var supporterData = CosmeticService.GetSupporterInfo(message.Sender);
        if (supporterData.SupporterWrap is { } valid)
        {
            ImUtf8.SameLineInner();
            ImGui.Image(valid.Handle, new Vector2(ImGui.GetTextLineHeight()));
            CkGui.AttachTooltip(supporterData.Tooltip);
        }

        if (_chatConfig.Data.Timestamps)
            CkGui.ColorTextInline(message.TimestampUTC.ToLocalTime().ToString("h:mmtt"), ImGui.GetColorU32(ImGuiCol.TextDisabled));
        ImGui.SetCursorPosX(_offsetX);
        NewRichText.TextWrappedOrDummy(message.Message, id: ChatLog!.ID + message.MsgId + "hybrid-drawer");
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
            // RadarChat requires access.
            if (ChatLog is GlobalChatLog gcl)
            {
                if (gcl.ChatUsers.GetValueOrDefault(msg.Sender, (false, ChatFlags.None)).Item2.HasAny(ChatFlags.AllowProfileViewing))
                    _mediator.Publish(new OpenUserLightProfileMessage(msg.Sender));
            }
            else
            {
                // Otherwise just open the profile.
                _mediator.Publish(new OpenUserLightProfileMessage(msg.Sender));
            }
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            inPopup = msg;
            ImGui.OpenPopup($"ckchatlog-{ChatLog!.ID}-msg-actions");
        }
    }

    public override void DrawChatInputRow()
    {
        var window = ImGuiInternal.GetCurrentWindow();
        if (window.SkipItems)
            return;

        using var _ = ImRaii.Group();
        var style = ImGui.GetStyle();
        var scrollIcon = ChatLog!.AutoScroll ? FAI.ArrowDownUpLock : FAI.ArrowDownUpAcrossLine;
        var rWidth = CkGui.IconButtonSize(scrollIcon).X + CkGui.IconButtonSize(FAI.Heart).X + (ImUtf8.ItemInnerSpacing.X * 2);

        var width = ImGui.GetContentRegionAvail().X;
        var size = new Vector2(width, ImUtf8.FrameHeight);

        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, pos + size);
        var id = ImGui.GetID($"chat-input-{ChatLog!.ID}");

        // Reserve the layout space for the entire bar
        ImGuiInternal.ItemSize(size, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(bb, id))
            return;

        ImGuiInternal.RenderFrame(bb.Min, bb.Max, ImGui.GetColorU32(ImGuiCol.FrameBg), false, style.FrameRounding);
        using var s = ImRaii.PushColor(ImGuiCol.FrameBg, 0).Push(ImGuiCol.Border, 0);

        ImGui.SetCursorScreenPos(bb.Min);

        if (shouldFocusInput && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            ImGui.SetKeyboardFocusHere(0);
            shouldFocusInput = false;
        }

        ImGui.SetNextItemWidth(width - rWidth);
        ImGui.InputTextWithHint($"##chat-input-{ChatLog!.ID}", GetChatInputHint(), ref previewMessage, 400, ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.CallbackAlways, OnChatInputCallback);
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
            ChatLog!.AutoScroll = !ChatLog!.AutoScroll;
        CkGui.AttachTooltip($"Toggles AutoScroll (Current: {(ChatLog!.AutoScroll ? "Enabled" : "Disabled")})");
    }

    private string GetChatInputHint() => ChatLog switch
    {
        DMChatLog dmLog => $"Message {_pairService.GetChatNameLabel(dmLog.TargetUser)}...",
        GlobalChatLog globalLog => "Message Global Chat...",
        _ => "Message..."
    };

    protected override void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        
        if (ChatLog is GlobalChatLog globalChat)
        {
            _chatService.SendGlobalChatMessage(new ChatlogId(GsChatKind.Global, "GlobalChat"), message);
            base.SendMessage(message);
        }
        else if (ChatLog is DMChatLog dmChatLog)
        {
            _chatService.SendTell(dmChatLog.TargetUser, message);
            base.SendMessage(message);
        }
    }

    protected override void DrawContentMenu(NewGsChatMessage msg)
    {
        // Radar chat is different here!
        if (ChatLog is GlobalChatLog globalChat)
        {
            DrawChatContentMenu(globalChat, msg);
            return;
        }

        var ctrlHeld = ImGui.GetIO().KeyCtrl;
        var isOwnMsg = msg.Sender.UID == MainHub.UID;
        var disableSilence = !ctrlHeld || isOwnMsg;
        var dispName = _pairService.GetNickAliasOrUid(msg.Sender);

        using (Fonts.GameFont.Push())
            CkGui.TextUnderlined(dispName);

        if (MainHub.Reputation.CanViewProfiles)
        {
            if (ImGui.Selectable("Open Profile"))
            {
                _mediator.Publish(new OpenUserLightProfileMessage(msg.Sender));
                ImGui.CloseCurrentPopup();
            }
            CkGui.AttachTooltip($"Open {dispName}'s profile.--NL----COL--Shortcut: Middle-Click--COL--", ImGuiColors.DalamudGrey2, ImGuiHoveredFlags.AllowWhenDisabled);
        }

        if (isOwnMsg)
            return;

        if (CkGui.SelectableEx($"Mute {dispName}", disableSilence || isOwnMsg))
        {
            _blocks.MuteUser(msg.Sender.UID);
            ImGui.CloseCurrentPopup();
            return;
        }
        CkGui.AttachTooltip($"Hides messages from {dispName} until unmuted or plugin restart.--NL--" +
            $"--COL--Must hold CTRL to select.--COL--", ImGuiColors.ParsedGrey, ImGuiHoveredFlags.AllowWhenDisabled);
    }

    private void DrawChatContentMenu(GlobalChatLog globalChat, NewGsChatMessage msg)
    {
        var shiftHeld = ImGui.GetIO().KeyShift;
        var ctrlHeld = ImGui.GetIO().KeyCtrl;
        var isOwnMsg = msg.Sender.UID == MainHub.UID;
        var disableSilence = !ctrlHeld || isOwnMsg;
        var disableBlock = !(ctrlHeld && shiftHeld) || isOwnMsg;

        var flags = globalChat.ChatUsers.GetValueOrDefault(msg.Sender, (false, ChatFlags.None)).Item2;
        var dispName = msg.DisplayName;

        using (Fonts.HeaderFont.Push())
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
            using var c = ImRaii.PushColor(ImGuiCol.Border, GsCol.LushPinkButton.Vec4());
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

        if (CkGui.SelectableEx("Send PairRequest", disableReq))
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
            _mediator.Publish(new OpenReportUIMessage(ReportKind.GlobalChat, msg.Sender, globalChat, msg.MsgId));
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachTooltip($"Report {dispName} for their chat behavior.");
    }
}
