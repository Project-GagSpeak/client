using CkCommons;
using CkCommons.Gui;
using CkCommons.RichChat;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using OtterGui.Text;
using OtterGuiInternal;

namespace GagSpeak.Gui.Chat;

/// <summary>
///   Centralized Chat Window for all SanctionChats, RadarChat, and Direct Messages.
/// </summary>
public class ChatWindowUI : WindowMediatorSubscriberBase
{
    private enum RightPanelState { None, Chats, Settings }

    private const string WIN_ID = "###GagSpeakChatWindow";
    private readonly GlobalChatLog _globalChat;
    private readonly HybridChatDrawer _chatDrawer;
    private readonly ChatConfig _chatConfig;
    private readonly ChatService _chatService;
    private readonly KinkPlateService _profiles;
    private readonly PairService _pairService;

    private RichChatLog<NewGsChatMessage>? _selected = null;
    private string _chatSearch = string.Empty;
    private RightPanelState _rightPanelState = RightPanelState.None;

    private uint _pinkFeint;
    private uint _pinkPressed;
    private uint _pinkActive;

    private float _transparency;
    private bool _isTransparent = false;

    public ChatWindowUI(ILogger<ChatWindowUI> logger, GagspeakMediator mediator,
        GlobalChatLog globalChat, HybridChatDrawer chatDrawer, ChatConfig chatConfig,
        ChatService chatService, KinkPlateService profiles, PairService pairService)
        : base(logger, mediator, "GagSpeak Chat Window" + WIN_ID)
    {
        _globalChat = globalChat;
        _chatDrawer = chatDrawer;
        _chatConfig = chatConfig;
        _chatService = chatService;
        _profiles = profiles;
        _pairService = pairService;

        _transparency = _chatConfig.Data.WindowOpacity;

        Mediator.Subscribe<ChatOpenChatWindow>(this, _ =>
        {
            IsOpen = true;
            _selected = _.ChatLog;
        });

        // Minimum boundaries should be set to a reasonable size, let user span it to larger area though.
        this.SetBoundaries(new(500, 300), ImGui.GetIO().DisplaySize);
    }

    public float SidebarMaxWidth => 150f * ImGuiHelpers.GlobalScale;

    public override bool DrawConditions()
    {
        // Respect draw condition rules.
        if (!_chatConfig.Data.ShowInUIHide && Svc.GameGui.GameUiHidden)
            return false;
        if (!_chatConfig.Data.ShowInCutscene && OnTickService.InCutscene)
            return false;
        if (!_chatConfig.Data.ShowInGroupPose && OnTickService.InGPose)
            return false;
        // Otherwise, draw.
        return true;
    }

    public override void PreDraw()
    {
        base.PreDraw();
        WindowName = GetWindowLabel() + WIN_ID;

        _isTransparent = _transparency < 1f;
        if (_isTransparent)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _transparency);
    }

    public override void PostDraw()
    {
        base.PostDraw();
        if (_isTransparent)
            ImGui.PopStyleVar();

    }

    // Main Draw Method
    protected override void DrawInternal()
    {
        // Update transparency.
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            _transparency = Math.Min(_chatConfig.Data.WindowOpacity, _transparency + _chatConfig.Data.OpacityShiftDelta);
        else
            _transparency = Math.Max(_chatConfig.Data.UnfocusedWindowOpacity, _transparency - _chatConfig.Data.OpacityShiftDelta);


        if (_selected is null)
            _rightPanelState = RightPanelState.Chats;

        // Draw using Internal methods.
        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();

        // Account for border. (If no border, is just InnerRect.Min/Max)
        var min = winPtr.InnerRect.Min + new Vector2(style.WindowBorderSize, 0);
        var max = winPtr.InnerRect.Max - new Vector2(style.WindowBorderSize);
        var size = max - min;

        var expandPanel = _rightPanelState is not RightPanelState.None;

        // Cover the full window without removing the padding in pre-draw, do not intersect, assert priority.
        winPtr.DrawList.PushClipRect(min, max, false);

        var rightWidthInner = expandPanel ? SidebarMaxWidth : CkGui.IconButtonSize(FAI.ArrowLeftLong).X;

        var rightW = rightWidthInner + style.WindowPadding.X * 2;
        var ribbonH = CkGui.CalcFontTextSize("A", Fonts.HeaderFont).Y + style.WindowPadding.Y;
        var stroke = 1.5f * ImGuiHelpers.GlobalScale;

        var ribbonMax = new Vector2(max.X, min.Y + ribbonH);
        var chatMin = new Vector2(min.X, ribbonMax.Y);
        var sidebarMin = new Vector2(max.X - rightW, ribbonMax.Y);

        // RibbonMax
        // Draw the top bar out.
        winPtr.DrawList.AddRectFilled(min, ribbonMax, GsColors.SurfaceCol.ToUint());

        // Draw the background for the panel if expanded.
        winPtr.DrawList.AddRectFilled(sidebarMin, max, GsColors.SurfaceCol.ToUint(), style.WindowRounding, ImDrawFlags.RoundCornersBottomRight);
        winPtr.DrawList.AddLine(sidebarMin, sidebarMin with { Y = max.Y }, GsColors.BorderSoft.ToUint(), stroke);

        // Line for the ribbon frame.
        winPtr.DrawList.AddLine(min with { Y = ribbonMax.Y }, ribbonMax, GsColors.BorderSoft.ToUint(), stroke);

        ImGui.SetCursorScreenPos(min + new Vector2(style.ItemInnerSpacing.X, (stroke + style.ItemSpacing.Y) / 2));
        DrawTopBar(SidebarMaxWidth);

        // Shift down to draw.
        var contentMin = min + new Vector2(0, ribbonH) + style.WindowPadding;
        var contentWidth = ImGui.GetContentRegionAvail().X - rightWidthInner - style.WindowPadding.X * 2;
        ImGui.SetCursorScreenPos(contentMin);
        DrawChatArea(contentMin, contentWidth);

        // If showing Members, draw that out as well
        ImGui.SameLine(0, style.WindowPadding.X * 2);
        if (expandPanel)
            DrawRightPanelExpanded(style);
        else
            DrawCollapsedSideNav(style);
        winPtr.RenderCustomResizeGrips();
        winPtr.DrawList.PopClipRect();
    }

    private string GetWindowLabel()
        => _selected switch
        {
            GlobalChatLog => "GlobalChat",
            DMChatLog dmLog => $"Private Chat",
            _ => "GagSpeak Chat Window"
        };

    private string GetHeaderLabel()
        => _selected switch
        {
            GlobalChatLog rclog => $"Global Chat",
            DMChatLog dmLog => _pairService.GetChatNameLabel(dmLog.TargetUser),
            _ => "Nothing Selected"
        };

    // Draws the top row
    private void DrawTopBar(float rWidth)
    {
        if (_selected is null)
        {
            CkGui.FontText("Nothing Selected", Fonts.HeaderFont, CkCol.TriStateCross.Uint());
            ImGui.SameLine();
            DrawChatSearch(rWidth);
            return;
        }

        CkGui.FontText(GetHeaderLabel(), Fonts.HeaderFont);
        ImGui.SameLine();
        DrawChatSearch(rWidth);
    }

    private void DrawCollapsedSideNav(ImGuiStylePtr style)
    {
        using var _ = ImRaii.Child("side-nav", ImGui.GetContentRegionAvail());
        
        if (CkGui.IconButton(FAI.ArrowCircleLeft, inPopup: true))
            _rightPanelState = RightPanelState.Chats;
        CkGui.AttachTooltip("Expands the sidenav, revealing other channels.");

        ImGui.Spacing();

        // Settings.
        if (CkGui.IconButton(FAI.Cog, inPopup: true))
        {
            if (ImGui.GetIO().KeyShift)
                Mediator.Publish(new OpenSettingsUI(3, 1));
            else
                _rightPanelState = RightPanelState.Settings;
        }
        CkGui.AttachTooltip("Opens the chat settings in the settings UI" +
            "--NL----COL--Hold SHIFT to open in the Settings UI.--COL--", ImGuiColors.DalamudGrey2);
    }

    private void DrawRightPanelExpanded(ImGuiStylePtr style)
    {
        using var _ = ImRaii.Child("chats-panel", ImGui.GetContentRegionAvail());

        var winPtr = ImGuiInternal.GetCurrentWindow();
        var region = ImGui.GetContentRegionAvail();
        _pinkActive = GsCol.VibrantPink.Vec4().WithAlpha(0.25f).ToUint();
        _pinkPressed = GsCol.VibrantPink.Vec4().WithAlpha(0.35f).ToUint();
        _pinkFeint = GsCol.VibrantPink.Vec4().WithAlpha(0.05f).ToUint();

        if (CkGui.IconTextButton(FAI.ArrowCircleRight, "Collapse", region.X, true, _selected is null))
            _rightPanelState = RightPanelState.None;
        CkGui.AttachTooltip(_selected is null ? "Must select a chat to collapse!" : "Collapses the sidebar");
        ImGui.Separator();

        if (_rightPanelState is RightPanelState.Chats)
            DrawChatList(winPtr, style, region);
        else if (_rightPanelState is RightPanelState.Settings)
            DrawCompactSettings(winPtr, style, region);
    }

    private void DrawCompactSettings(ImGuiWindowPtr winPtr, ImGuiStylePtr style, Vector2 region)
    {
        ImGui.Text("Still a W.I.P!");
        if (CkGui.ButtonEx("Open Settings UI", ImGuiColors.TankBlue))
            Mediator.Publish(new OpenSettingsUI(3, 1));
    }

    private void DrawChatList(ImGuiWindowPtr winPtr, ImGuiStylePtr style, Vector2 region)
    {
        using var c = ImRaii.PushColor(ImGuiCol.Button, 0).Push(ImGuiCol.ButtonHovered, 0x19FFFFFF).Push(ImGuiCol.ButtonActive, 0x33FFFFFF);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0f, 0.5f));

        DrawChatTypeHeader(winPtr, "GlobalChat");
        if (DrawSelectableChatLog(winPtr, style, "Global Chat", region.X, _globalChat))
        {
            _selected = _globalChat;
            _chatDrawer.UseDiscordFormat = false;
            _logger.LogInformation($"Selected RadarChat", LoggerType.GlobalChat);
        }

        if (_chatService.DMChats.Count > 0)
        {
            ImGui.Spacing();
            DrawChatTypeHeader(winPtr, "Your DMs");
            foreach (var (logId, dmLog) in _chatService.DMChats)
            {
                var label = _pairService.GetChatNameLabel(dmLog.TargetUser);
                if (DrawSelectableChatLog(winPtr, style, label, region.X, dmLog))
                {
                    _selected = dmLog;
                    _chatDrawer.UseDiscordFormat = true;
                    _logger.LogInformation($"Selected DMChatLog: DMLog_{label}", LoggerType.GlobalChat);
                }
            }
        }
    }

    private bool DrawChatTypeHeader(ImGuiWindowPtr winPtr, string label, uint? color = null)
    {
        if (color.HasValue)
            CkGui.ColorText(label, color.Value);
        else
            ImGui.TextUnformatted(label);

        var pos = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        var lPos = new Vector2(pos.X, pos.Y + size.Y);
        winPtr.DrawList.PathLineTo(lPos);
        lPos.X += winPtr.InnerClipRect.GetSize().X;
        winPtr.DrawList.PathLineTo(lPos);
        winPtr.DrawList.PathStroke(uint.MaxValue);
        return false;
    }

    private bool DrawSelectableChatLog(ImGuiWindowPtr winPtr, ImGuiStylePtr style, string label, float width, RichChatLog<NewGsChatMessage> chatlog)
    {
        var isSelected = _selected == chatlog;

        var id = ImGui.GetID(chatlog.ID);
        var pos = winPtr.DC.CursorPos;
        var itemSize = new Vector2(width, ImUtf8.FrameHeight);
        var bb = new ImRect(pos, pos + itemSize);
        var drawBox = new ImRect(bb.Min + style.FramePadding, bb.Max - style.FramePadding);

        ImGuiInternal.ItemSize(itemSize, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(bb, id, null))
            return false;

        bool hovered = false, active = false;
        var clicked = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref active);
        // Render item
        ImGuiP.RenderNavHighlight(bb, id);
        ImGuiP.RenderFrame(bb.Min, bb.Max, 0);
        // Feint outline that respects the highlight states.
        var frameCol = ImGui.GetColorU32(active ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
        winPtr.DrawList.AddRect(bb.Min, bb.Max, CkGui.ApplyAlpha(frameCol, .075f), style.FrameRounding, ImDrawFlags.RoundCornersAll, 1.5f);
        winPtr.DrawList.AddRectFilled(bb.Min, bb.Max, CkGui.ApplyAlpha(frameCol, .1f), style.FrameRounding, ImDrawFlags.RoundCornersAll);

        // The button renders its "selected" visual state while hovered or pressed.
        if (isSelected || hovered || active)
        {
            // Darken the gold slightly if actively clicking it
            var bgMain = active ? _pinkPressed : _pinkActive;
            var seam = pos.X + width * 0.80f;
            winPtr.DrawList.AddRectFilledMultiColor(pos, new Vector2(seam, bb.Max.Y), bgMain, _pinkFeint, _pinkFeint, bgMain);
            winPtr.DrawList.AddRectFilledMultiColor(new Vector2(seam, bb.Min.Y), bb.Max, _pinkFeint, 0, 0, _pinkFeint);
            // Glowing Vertical left bar
            var gap = (bb.Max.Y - bb.Min.Y) * .15f;
            var barMin = new Vector2(bb.Min.X, bb.Min.Y + gap);
            var barMax = new Vector2(bb.Min.X + 3f * ImGuiHelpers.GlobalScale, bb.Max.Y - gap);
            var step = gap / 3f;

            for (int g = 3; g >= 1; g--)
            {
                var pad = g * step;
                var gMin = new Vector2(barMin.X - pad, barMin.Y - pad);
                var gMax = new Vector2(barMax.X + pad, barMax.Y + pad);
                var gCol = GsCol.VibrantPink.Vec4().WithAlpha(0.10f / g).ToUint();
                winPtr.DrawList.AddRectFilled(gMin, gMax, gCol);
            }
            winPtr.DrawList.AddRectFilled(barMin, barMax, GsCol.LushPinkButton.Uint());
        }

        var mentioned = chatlog.UnreadMentions > 0;
        var newMsgs = chatlog.UnreadMessages > 0;

        // Draw the main text label
        var drawPos = drawBox.Min;
        drawPos.X += style.ItemInnerSpacing.X;
        winPtr.DrawList.AddTextShadowed(label, drawPos, ImGui.GetColorU32(ImGuiCol.Text), 0xFF000000);

        if (newMsgs || mentioned)
        {
            // Mentions take priority over standard unread messages
            var count = mentioned ? chatlog.UnreadMentions : chatlog.UnreadMessages;
            var bubbleColor = mentioned ? CkCol.TriStateCross.Uint() : ImGuiColors.TankBlue.ToUint();
            var txt = count > 99 ? "99+" : count.ToString();

            // Calculate exact dimensions
            var textSize = ImGui.CalcTextSize(txt);
            var padding = new Vector2(6f * ImGuiHelpers.GlobalScale, 0);
            var bubbleSize = textSize + (padding * 2);

            // Position it immediately to the right of the label, vertically centered in the row
            var bubbleMin = new Vector2(drawBox.Max.X - bubbleSize.X - style.FramePadding.X, bb.Min.Y + (itemSize.Y - bubbleSize.Y) / 2);
            var bubbleMax = bubbleMin + bubbleSize;
            // Draw the pill-shaped background (rounding = half of the height)
            winPtr.DrawList.AddRectFilled(bubbleMin, bubbleMax, bubbleColor, bubbleSize.Y / 2);
            // Draw the text perfectly centered inside the pill
            winPtr.DrawList.AddTextShadowed(txt, bubbleMin + padding, uint.MaxValue, 0xFF000000);
        }

        return clicked;
    }

    private void DrawChatArea(Vector2 min, float width)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f * ImGuiHelpers.GlobalScale);
        using (ImRaii.Child("contents", new Vector2(width, -1)))
        {

            if (_selected is not { } validChatLog)
                return;

            // If this sanction does not have a chatlog configured, do not display the window.
            if (string.IsNullOrEmpty(validChatLog.ID))
            {
                var centerH = CkGui.CalcFontTextSize("A", Fonts.SubtitleFont).Y + CkGui.CalcFontTextSize("A", Fonts.DefaultScaled).Y + ImUtf8.ItemSpacing.Y;
                var centerDrawHeight = (ImGui.GetContentRegionAvail().Y - centerH) / 2;
                CkGui.FontTextCentered("ChatlogId is Invalid", Fonts.SubtitleFont, CkCol.TriStateCross.Uint());
                CkGui.FontTextCentered("Nothing to see here yet!", Fonts.DefaultScaled, ImGuiColors.DalamudGrey2);
                return;
            }

            // Otherwise we are good to run GetOrCreate on the chatlog.
            _chatDrawer.Draw(validChatLog);
        }
        var max = ImGui.GetItemRectMax();

        var showOverlay = !MainHub.Reputation.CanUseChat || !MainHub.Reputation.IsVerified;
        if (!showOverlay)
            return;

        ImGui.SetCursorScreenPos(min);
        using var overlay = ImRaii.Child("overlay-child", max - min, false);
        ImGui.GetWindowDrawList().AddRectFilledMultiColor(min, max, 0xCC000000, 0xCC000000, 0x99111111, 0x99111111);

        if (!MainHub.Reputation.CanUseChat)
        {
            var strikeText = $"You have [{MainHub.Reputation.ChatStrikes}] radar chat strikes.";
            var row1Size = CkGui.CalcFontTextSize("Blocked Via Bad Reputation!", Fonts.SubtitleFont);
            var row2Size = CkGui.CalcFontTextSize("Unable to view chat anymore.", Fonts.SubtitleFont);
            var row3Size = CkGui.CalcFontTextSize(strikeText, Fonts.DefaultScaled);

            var errorH = row1Size + row2Size + row3Size;
            var centerDrawHeight = (ImGui.GetContentRegionAvail().Y - errorH.Y) / 2;

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerDrawHeight);
            using (Fonts.SubtitleFont.Push())
            {
                CkGui.SetCursorXtoCenter(row1Size.X);
                CkGui.TextShadowed("Blocked Via Bad Reputation!", CkCol.TriStateCross.Uint(), 0xFF000000, Vector2.One, 4f, 8);

                CkGui.SetCursorXtoCenter(row2Size.X);
                CkGui.TextShadowed("Unable to view chat anymore.", CkCol.TriStateCross.Uint(), 0xFF000000, Vector2.One, 4f, 8);
            }
            using (Fonts.DefaultScaled.Push())
            {
                CkGui.SetCursorXtoCenter(row3Size.X);
                CkGui.TextShadowed(strikeText, CkCol.TriStateCross.Uint(), 0xFF000000, Vector2.One, 4f, 8);
            }
        }
        else if (!MainHub.Reputation.IsVerified)
        {
            var row1Size = CkGui.CalcFontTextSize("Must Claim Account To Chat!", Fonts.SubtitleFont);
            var row2Size = CkGui.CalcFontTextSize("For Moderation & Safety Reasons", Fonts.DefaultScaled);
            var row3Size = CkGui.CalcFontTextSize("Only Verified Users Get Social Features.", Fonts.DefaultScaled);
            var row4Size = CkGui.CalcFontTextSize("You can verify via GagSpeaks's Discord Bot.", Fonts.HeaderFont);
            var row5Size = CkGui.CalcFontTextSize("Verification is easy & doesn't interact", Fonts.HeaderFont);
            var row6Size = CkGui.CalcFontTextSize("with lodestone or other SE properties.", Fonts.HeaderFont);

            var errorH = row1Size + row2Size + row3Size + row4Size + row5Size + row6Size;
            var centerDrawHeight = (ImGui.GetContentRegionAvail().Y - errorH.Y) / 2;

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerDrawHeight);
            using (Fonts.SubtitleFont.Push())
            {
                CkGui.SetCursorXtoCenter(row1Size.X);
                CkGui.TextShadowed("Must Claim Account To Chat!", CkCol.TriStateCross.Uint(), 0xFF000000, Vector2.One, 4f, 8);
            }
            using (Fonts.DefaultScaled.Push())
            {
                CkGui.SetCursorXtoCenter(row2Size.X);
                CkGui.TextShadowed("For Moderation & Safety Reasons", 0x33FFFFFF, 0xFF000000, Vector2.One, 4f, 8);

                CkGui.SetCursorXtoCenter(row3Size.X);
                CkGui.TextShadowed("Only Verified Users Get Social Features.", 0x33FFFFFF, 0xFF000000, Vector2.One, 4f, 8);
            }
            ImGui.Spacing();
            using (Fonts.HeaderFont.Push())
            {
                CkGui.SetCursorXtoCenter(row4Size.X);
                CkGui.TextShadowed("You can verify via GagSpeak's Discord Bot.", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);
                CkGui.SetCursorXtoCenter(row5Size.X);
                CkGui.TextShadowed("Verification is easy & doesn't interact", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);
                CkGui.SetCursorXtoCenter(row6Size.X);
                CkGui.TextShadowed("with lodestone or other SE properties.", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);
            }
        }
    }

    private void DrawChatSearch(float searchWidth)
    {
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - searchWidth));
        FancySearchBar.Draw("##chat-search", "Filter Messages..", searchWidth, ref _chatSearch, 100);
        CkGui.AttachTooltip("Currently does not function, but will in the future.");
    }
}
