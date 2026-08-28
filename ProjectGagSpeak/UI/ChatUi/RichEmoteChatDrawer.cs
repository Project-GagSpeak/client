using CkCommons.Gui;
using CkCommons.RichChat;
using CkCommons.RichText.Emoji;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using OtterGui.Text;
using OtterGuiInternal;

namespace GagSpeak.Gui.Chat;

public class RichEmoteChatDrawer : RichChatDrawer<NewGsChatMessage>
{
    protected readonly ChatColors _common;
    protected readonly ChatConfig _chatConfig;
    protected readonly FavoritesConfig _favorites;
    protected readonly GsEmojiLoader _emojis;
    protected readonly ChatFontManager _chatFont;
    protected readonly BlockService _blocks;
    protected readonly ChatService _chatService;

    public enum EmoteSegment { None, Favorites, All }

    protected bool selectingEmotes = false;
    protected bool stickerMode = false;
    protected string emoteFilter = string.Empty;

    protected EmoteSegment visibleEmoteSegment = EmoteSegment.None;
    protected EmoteSegment scrollToEmoteSegment = EmoteSegment.None;

    public RichEmoteChatDrawer(string label, ILogger logger, ChatColors common, 
        ChatConfig chatConfig, FavoritesConfig favorites, GsEmojiLoader emojis,
        ChatFontManager chatFont, BlockService blocks, ChatService chatService)
    {
        _common = common;
        _chatConfig = chatConfig;
        _favorites = favorites;
        _emojis = emojis;
        _chatFont = chatFont;
        _blocks = blocks;
        _chatService = chatService;
    }

    public RichEmoteChatDrawer(string label, ILogger logger, RichChatLog<NewGsChatMessage> chatLog,
        ChatColors common, ChatConfig chatConfig, FavoritesConfig favorites, GsEmojiLoader emojis,
        ChatFontManager chatFont, BlockService blocks, ChatService chatService)
        : base(chatLog)
    {
        _common = common;
        _chatConfig = chatConfig;
        _favorites = favorites;
        _emojis = emojis;
        _chatFont = chatFont;
        _blocks = blocks;
        _chatService = chatService;
    }

    protected override void FlushLocalData()
    {
        base.FlushLocalData();
        selectingEmotes = false;
        stickerMode = false;
    }

    // Called once over all messages
    protected float _offsetX;
    protected Vector2 _iconSize;
    protected ImGuiWindowPtr _winPtr;

    protected override void PostDraw(Vector2 inputMin)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));
        var drawTextPreview = !string.IsNullOrWhiteSpace(previewMessage);
        if (drawTextPreview)
            DrawTextPreview(previewMessage, inputMin);

        if (selectingEmotes)
        {
            var drawPos = drawTextPreview ? ImGui.GetItemRectMin() : inputMin;
            DrawQuickEmoteWindow(drawPos);
        }
    }

    protected override void DrawHistoryInternal(IEnumerable<NewGsChatMessage> messages, float width)
    {
        using var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImUtf8.ItemSpacing with { Y = 2 });
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
    }

    protected override void DrawIgnoredMessageRow(NewGsChatMessage message, float width)
    {
        var txtWidth = ImGui.CalcTextSize("Ignored Message");
        var lineW = (width - ImUtf8.ItemInnerSpacing.X * 2 - txtWidth.X) / 2;
        var min = ImGui.GetCursorScreenPos();
        var lineY = min.Y + (ImUtf8.TextHeight / 2);

        ImGui.GetWindowDrawList().AddLine(new Vector2(min.X, lineY), new Vector2(min.X + lineW, lineY), ImGuiColors.ParsedGrey.ToUint(), 2f);
        CkGui.ColorTextCentered("Ignored Message", ImGuiColors.ParsedGrey);
        CkGui.AttachTooltip($"Currently Ignoring msgs from {message.DisplayName}." +
            $"--NL----COL--Shift + Right-Click to unmute {message.DisplayName}.--COL--", ImGuiColors.DalamudGrey2);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyShift)
            _blocks.UnmuteUser(message.SenderId);
        ImGui.GetWindowDrawList().AddLine(new Vector2(min.X + width - lineW, lineY), new Vector2(min.X + width, lineY), ImGuiColors.ParsedGrey.ToUint(), 2f);
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

        if (shouldFocusInput)
        {
            ImGui.SetWindowFocus();
            ImGui.SetKeyboardFocusHere(0);
            shouldFocusInput = false;
        }

        ImGui.SetNextItemWidth(width - rWidth);
        ImGui.InputTextWithHint($"##chat-input-{ChatLog!.ID}", $"Message {ChatLog.ID}...", ref previewMessage, 400, ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.CallbackAlways, OnChatInputCallback);
        // Process submission Prevent losing chat focus after pressing the Enter key.
        if (ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            SendMessage(previewMessage);
            shouldFocusInput = true;
        }

        // Emote and scroll-lock buttons.
        ImUtf8.SameLineInner();
        using (ImRaii.PushColor(ImGuiCol.Text, GsCol.LushPinkButton.Uint(), selectingEmotes))
            if (CkGui.IconButton(FAI.Heart, inPopup: true))
                selectingEmotes = !selectingEmotes;

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(scrollIcon, inPopup: true))
            ChatLog!.AutoScroll = !ChatLog!.AutoScroll;
        CkGui.AttachTooltip($"Toggles AutoScroll (Current: {(ChatLog!.AutoScroll ? "Enabled" : "Disabled")})");
    }

    protected override void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        // Standardize the history push here so derived classes don't have to rewrite it
        sentHistory.Add(message);
        historyIdx = 0;
        lastInput = string.Empty;
        previewMessage = string.Empty;
        selectingEmotes = false;
    }

    protected virtual void DrawQuickEmoteWindow(Vector2 drawPos)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 11f * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.CellPadding, ImGui.GetStyle().CellPadding with { X = 7 * ImGuiHelpers.GlobalScale });

        var style = ImGui.GetStyle();
        var frameH = ImUtf8.FrameHeight;
        var spacing = style.ItemInnerSpacing;
        var padding = new Vector2(4f) * ImGuiHelpers.GlobalScale;
        var emoteSize = ImGuiHelpers.ScaledVector2(32);

        var totalWidth = ImGui.GetContentRegionAvail().X;
        var rightColWidth = totalWidth - (padding.X * 2) - frameH - (style.CellPadding.X * 4) - style.ScrollbarSize;
        var emotesPerRow = Math.Max(1, (int)((rightColWidth + spacing.X) / (emoteSize.X + spacing.X)));

        var totalEmotes = _emojis.Emotes.Count;
        var rows = (int)Math.Ceiling((float)totalEmotes / emotesPerRow);
        var displayRows = Math.Clamp(rows, 2, 4);

        // Padding + Search + Text Headers + Emote Rows + Spacing Buffer
        var innerHeight = ImGui.GetFrameHeightWithSpacing() + ((emoteSize.Y + spacing.Y) * displayRows) + (spacing.Y * 2);
        var winHeight = innerHeight + (padding.Y * 2);

        var winSize = new Vector2(totalWidth, winHeight);
        var winPos = drawPos - new Vector2(0, winHeight + 2f);
        ImGui.SetNextWindowPos(winPos);

        s.Push(ImGuiStyleVar.WindowPadding, padding);
        using var _ = ImRaii.Child("emote-selector", winSize, false, WFlags.AlwaysUseWindowPadding | WFlags.NoFocusOnAppearing | WFlags.NoScrollbar);

        var wdl = ImGui.GetWindowDrawList();
        var rounding = 5f * ImGuiHelpers.GlobalScale;
        // Calculate the visual split zones
        var headerH = padding.Y + frameH + (padding.Y * 0.75f);
        var sidebarW = padding.X + frameH + (spacing.X * 2);

        // Two-Tone BG
        wdl.PushClipRect(winPos, winPos + winSize, false);

        // Base BG for header and right content
        wdl.AddRectFilled(winPos, winPos + winSize, ImGui.GetColorU32(ImGuiCol.WindowBg), rounding);
        wdl.AddRectFilled(winPos, winPos + winSize, GsColors.BgCol.ToUint(), rounding);

        // Left sidebar BG
        var sbMin = new Vector2(winPos.X, winPos.Y + headerH);
        var sbMax = new Vector2(winPos.X + sidebarW, winPos.Y + winSize.Y);
        wdl.AddRectFilled(sbMin, sbMax, GsColors.ActionBar.ToUint(), rounding, ImDrawFlags.RoundCornersBottomLeft);

        // Subtle Outer Border
        wdl.AddRect(winPos, winPos + winSize, GsColors.BorderSoft.ToUint(), rounding);

        // Left sidebar Line
        wdl.AddLine(new(sbMax.X, sbMin.Y), sbMax, GsColors.BorderSoft.ToUint(), 1.5f * ImGuiHelpers.GlobalScale);

        wdl.PopClipRect();

        using var t = ImRaii.Table("emote-selector-content", 2, ImGuiTableFlags.None);
        if (!t) return;

        ImGui.TableSetupColumn("NavBar", ImGuiTableColumnFlags.WidthFixed, frameH);
        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        // Top left is a button to toggle sticker mode.
        ImGui.TableNextColumn();
        using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.TankBlue, stickerMode))
            if (CkGui.IconButton(FAI.StickyNote, inPopup: !stickerMode))
                stickerMode = !stickerMode;
        CkGui.AttachTooltip($"Toggle Sticker Mode. ({(stickerMode ? "Enabled" : "Disabled")})" +
            "--NL----COL--Makes selections stickers automatically--COL--", ImGuiColors.DalamudGrey2);

        // Search bar
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##emote_search", "Filter Emotes..", ref emoteFilter, 50);
        
        // Spanning Separator
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Separator();
        ImGui.TableNextColumn();
        ImGui.Separator();

        ImGui.TableNextRow();
        // Nav Bar Icons
        ImGui.TableNextColumn();
        ImGui.Spacing();

        var starColor = visibleEmoteSegment is EmoteSegment.Favorites ? uint.MaxValue : 0xFFA1928D;
        using (ImRaii.PushColor(ImGuiCol.Text, starColor))
            if (CkGui.IconButton(FAI.Star, inPopup: true))
                scrollToEmoteSegment = EmoteSegment.Favorites;
        CkGui.AttachTooltip("Favorites");

        ImGui.Spacing();
        var allColor = visibleEmoteSegment is EmoteSegment.All ? uint.MaxValue : 0xFF74605B;
        using (ImRaii.PushColor(ImGuiCol.Text, starColor))
            if (CkGui.IconButton(FAI.Smile, inPopup: true))
                scrollToEmoteSegment = EmoteSegment.All;
        CkGui.AttachTooltip("All Emotes");

        // Emote Grid Area
        ImGui.TableNextColumn();
        using var grid = ImRaii.Child("##emote-grid", Vector2.Zero, false, WFlags.NoFocusOnAppearing | WFlags.NoBackground);

        var winPtr = ImGuiInternal.GetCurrentWindow();
        if (winPtr.SkipItems)
            return;

        // Define clipping bounds.
        var width = ImGui.GetContentRegionAvail().X;
        var viewMinY = ImGui.GetWindowPos().Y;
        var viewMaxY = viewMinY + ImGui.GetWindowSize().Y;

        var visibleSegment = EmoteSegment.None;

        var favs = _emojis.Emotes.Where(x => FavoritesConfig.Emotes.Contains(x.Key)).ToList();
        if (favs.Count > 0)
        {
            if (scrollToEmoteSegment is EmoteSegment.Favorites)
            {
                ImGui.SetScrollHereY(0.0f);
                scrollToEmoteSegment = EmoteSegment.None;
            }

            ImGui.TextDisabled("Favorites");
            var favlabelPos = ImGui.GetItemRectMin();
            var favlabelSize = ImGui.GetItemRectSize();
            var favlinePos = new Vector2(favlabelPos.X, favlabelPos.Y + favlabelSize.Y);
            winPtr.DrawList.PathLineTo(favlinePos);
            favlinePos.X += winPtr.InnerClipRect.GetSize().X;
            winPtr.DrawList.PathLineTo(favlinePos);
            winPtr.DrawList.PathStroke(uint.MaxValue);
            DrawEmoteGrid(favs, EmoteSegment.Favorites);
        }

        // All
        ImGui.TextDisabled("All Emotes");
        var labelPos = ImGui.GetItemRectMin();
        var labelSize = ImGui.GetItemRectSize();
        var linePos = new Vector2(labelPos.X, labelPos.Y + labelSize.Y);
        winPtr.DrawList.PathLineTo(linePos);
        linePos.X += winPtr.InnerClipRect.GetSize().X;
        winPtr.DrawList.PathLineTo(linePos);
        winPtr.DrawList.PathStroke(uint.MaxValue);
        DrawEmoteGrid([.. _emojis.Emotes], EmoteSegment.All);

        // Update the global state with whatever was at the top of the visible area
        if (visibleSegment is not EmoteSegment.None)
            visibleEmoteSegment = visibleSegment;

        // Grid helper.
        void DrawEmoteGrid(IEnumerable<KeyValuePair<string, ImageFile>> emotes, EmoteSegment segment)
        {
            var count = 0;
            foreach (var (emoteName, wrap) in emotes)
            {
                if (!string.IsNullOrEmpty(emoteFilter) && !emoteName.Contains(emoteFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Help us perform a psuedoclip to prevent drawing excess images.
                if (!CkGuiClip.IsNextItemVisible(emoteSize))
                    ImGui.Dummy(emoteSize);
                else
                {
                    // In View! Record the segment if this is the highest visible item
                    if (visibleSegment is EmoteSegment.None)
                        visibleEmoteSegment = segment;

                    _emojis.DrawEmoji(emoteName, emoteSize);
                    if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        InsertEmote(emoteName);
                        shouldFocusInput = true;
                    }

                    if (ImGui.BeginPopupContextItem($"ctx_{emoteName}"))
                    {
                        var isFav = FavoritesConfig.Emotes.Contains(emoteName);
                        if (ImGui.Selectable(isFav ? "Remove from Favorites" : "Add to Favorites"))
                        {
                            if (isFav) _favorites.UnfavoriteEmote(emoteName);
                            else _favorites.FavoriteEmote(emoteName);
                        }
                        ImGui.EndPopup();
                    }
                }

                count++;
                if (count % emotesPerRow != 0)
                    ImUtf8.SameLineInner();
            }

            // Terminate the line if not moving to a new one at the end.
            if (count > 0 && count % emotesPerRow != 0)
                ImGui.NewLine();
        }
    }

    /// <summary>
    ///   Override for ChatInputCallback to handle both history and Cursor Tracking / Injection.
    /// </summary>
    protected override unsafe int OnChatInputCallback(ref ImGuiInputTextCallbackData dataPtr)
    {
        fixed (ImGuiInputTextCallbackData* data = &dataPtr)
        {
            // Handle message history cycling up and down between messages.
            if (data->EventFlag is ImGuiInputTextFlags.CallbackHistory)
            {
                // This will go from most recent to oldest sent messages
                if (data->EventKey is ImGuiKey.UpArrow)
                {
                    // If at the start, there is nothing to store.
                    if (historyIdx is 0)
                        lastInput = previewMessage;
                    // Otherwise, we should swap out the data.
                    if (historyIdx < sentHistory.Count)
                    {
                        historyIdx++;
                        data->DeleteChars(0, data->BufTextLen);
                        data->InsertChars(0, sentHistory[^historyIdx]);
                    }
                }
                // This moves back towards our most message.
                else if (data->EventKey is ImGuiKey.DownArrow)
                {
                    if (historyIdx > 0)
                    {
                        historyIdx--;
                        var text = historyIdx == 0 ? lastInput : sentHistory[^historyIdx];
                        data->DeleteChars(0, data->BufTextLen);
                        data->InsertChars(0, text);
                    }
                }
            }

            if (data->EventFlag is ImGuiInputTextFlags.CallbackAlways)
            {
                // Track exactly where the user is typing
                if (setChatCursorPos == -1)
                    lastChatCursorPos = data->CursorPos;
                // Force the cursor to a new position if an emote was just inserted
                if (setChatCursorPos > -1)
                {
                    if (setChatCursorPos <= data->BufTextLen)
                    {
                        data->CursorPos = setChatCursorPos;
                        data->SelectionStart = setChatCursorPos;
                        data->SelectionEnd = setChatCursorPos;
                    }
                    setChatCursorPos = -1;
                }
            }
        }

        return 0;
    }

    protected virtual void InsertEmote(string emoteKey)
    {
        var emoteText = stickerMode ? $":s~{emoteKey}:" : $":{emoteKey}:";
        // Insert the emote text at the current cursor position, then shift the cursor to after the inserted text.
        if (lastChatCursorPos >= 0 && lastChatCursorPos <= previewMessage.Length)
        {
            previewMessage = previewMessage.Insert(lastChatCursorPos, emoteText);
            setChatCursorPos = lastChatCursorPos + emoteText.Length;
        }
        // Or add the emote to the end of the message otherwise.
        else
        {
            previewMessage += emoteText;
            setChatCursorPos = previewMessage.Length;
        }
    }

}
