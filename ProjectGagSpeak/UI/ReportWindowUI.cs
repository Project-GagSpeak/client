using CkCommons;
using CkCommons.Gui;
using CkCommons.RichChat;
using CkCommons.RichText;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Chat;
using GagspeakAPI.Data;
using GagspeakAPI.Profiles;
using GagspeakAPI.Reporting;
using GagspeakAPI.User;
using OtterGui.Text;
using OtterGuiInternal;
using SundouleiaAPI.Reporting;

namespace GagSpeak.Gui.Profiles;

public class ReportWindowUI : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly ChatColors _common;
    private readonly KinksterManager _kinksters;
    private readonly KinkPlateService _profiles;
    private readonly PairService _pairService;

    // General report state
    private ReportKind? _reportType;
    private UserData? _reportedUser;
    private string _reportedDisplayName = string.Empty;
    private string _reportedSubName = string.Empty;
    private string _reportReason = "Describe your report here";

    // For Profiles.
    private IDalamudTextureWrap? _iconWrap;
    private IDalamudTextureWrap? _bgWrap;
    private KinkPlateContent? _profileContentSnapshot; // <-- Placeholder
    private UserProfileV1? _profileSnapshot;

    // For Chat
    private RichChatLog<NewGsChatMessage>? _chatLog;
    private NewGsChatMessage? _reportedMsg;

    public ReportWindowUI(ILogger<ReportWindowUI> logger, GagspeakMediator mediator,
        MainHub hub, ChatColors chatColors, KinksterManager kinksters, 
        KinkPlateService profiles, PairService pairService)
        : base(logger, mediator, "GagSpeak Report Overlay###GagSpeakReportUI")
    {
        _hub = hub;
        _common = chatColors;
        _kinksters = kinksters;
        _profiles = profiles;
        _pairService = pairService;

        Flags = WFlags.NoSavedSettings | WFlags.NoTitleBar;

        IsOpen = false;
        Size = new Vector2(800, 750);
        this.SetBoundaries(new Vector2(800, 750));
        this.PinningClickthroughFalse();

        Mediator.Subscribe<OpenReportUIMessage>(this, msg =>
        {
            _reportedUser = msg.User;
            _reportType = msg.Kind;
            _reportedDisplayName = _pairService.GetProfileDisplayName(msg.User);
            var showUID = _reportedUser.UID == MainHub.UID || _kinksters.Contains(_reportedUser);
            _reportedSubName = showUID  ? _reportedUser.UID : _reportedUser.AnonName;
            _reportReason = "Describe your report here";

            // Snapshot the data based on what enum is being reported
            if (_reportType is ReportKind.Profile)
            {
                var profile = _profiles.GetUserProfile(_reportedUser);
                _iconWrap = profile.GetIconWrapOrDefault();
                _bgWrap = profile.GetBgWrapOrDefault();
                _profileContentSnapshot = profile.Data;
                // _profileSnapshot = profile.Data.NewtonsoftDeepClone(ProfilesJsonEx.Settings);
            }
            else if (_reportType is ReportKind.GlobalChat)
            {
                if (msg.ChatLog is null || msg.MsgId is null)
                    return;
                if (msg.ChatLog.Messages.FirstOrDefault(m => m.MsgId == msg.MsgId) is not { } chatMsg)
                    return;
                _chatLog = msg.ChatLog;
                _reportedMsg = chatMsg;
            }
            IsOpen = true;
        });
    }

    public override void OnClose()
    {
        ResetAllData();
        base.OnClose();
    }

    public override bool DrawConditions()
        => _reportType != null && _reportedUser != null && MainHub.IsConnected;

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 16f * ImGuiHelpers.GlobalScale);
        // Center the window if appearing.
        CkGui.CenterNextWindow(Size!.Value.X, Size!.Value.Y, ImGuiCond.Appearing);
        base.PreDraw();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        base.PostDraw();
    }

    protected override void DrawInternal()
    {
        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();

        // Account for border. (If no border, is just InnerRect.Min/Max)
        var min = winPtr.InnerRect.Min;
        var max = winPtr.InnerRect.Max;
        var size = max - min;
        // Push the clipRect to the full window space including the borders.
        winPtr.DrawList.PushClipRect(min, max, false);

        // TopbarH
        var gap = style.WindowPadding.Y * 2;
        var labelH = CkGui.CalcFontTextSize("A", Fonts.SubtitleFont).Y;
        var topbarH = labelH + gap;
        var rWidth = 400f * ImGuiHelpers.GlobalScale + gap * 2;
        var leftW = size.X - rWidth - gap;
        var stroke = 1.5f * ImGuiHelpers.GlobalScale;

        // Define the topBar space.
        var headerMax = min + new Vector2(leftW + gap, topbarH);
        var contentMin = new Vector2(min.X, headerMax.Y);
        var contentMax = new Vector2(headerMax.X, max.Y);
        var previewMin = new Vector2(headerMax.X, min.Y);

        // Report Content BG
        winPtr.DrawList.AddRectFilled(min, contentMax, GsColors.BgCol.ToUint(), style.WindowRounding, ImDrawFlags.RoundCornersLeft);
        // Header Label BG
        winPtr.DrawList.AddRectFilled(min, headerMax, GsColors.RibbonTop.ToUint(), style.WindowRounding, ImDrawFlags.RoundCornersTopLeft);

        ImGui.SetCursorScreenPos(min + style.WindowPadding);
        DrawHeader(new Vector2(leftW, labelH));

        var previewBoxMin = previewMin + style.WindowPadding;
        var outerRegion = max - style.WindowPadding - previewBoxMin;
        var profileScaledHeight = ProfilesEx.BASE_HEIGHT * ImGuiHelpers.GlobalScale;
        var innerHeight = MathF.Min(profileScaledHeight, outerRegion.Y);
        var imgPreviewSize = new Vector2(MathF.Min(ProfilesEx.BASE_WIDTH * ImGuiHelpers.GlobalScale, outerRegion.X), innerHeight);
        // Display Report Contents Box
        ImGui.SetCursorScreenPos(contentMin + style.WindowPadding);
        DrawContents(leftW);

        // Then the preview, only when valid.
        ImGui.SetCursorScreenPos(previewBoxMin);
        DrawReportedContent(outerRegion, imgPreviewSize);

        // Finally, draw out the line dividers.
        // - Line dividing ribbon and report form
        winPtr.DrawList.AddLine(new(min.X, headerMax.Y), headerMax, GsColors.BorderSoft.ToUint(), stroke);
        // - Line dividing report form and reported content
        winPtr.DrawList.AddLine(new(headerMax.X, min.Y), new(headerMax.X, contentMax.Y), GsColors.BorderSoft.ToUint(), stroke);

        // Enveloping Border
        winPtr.DrawList.AddRect(min, max, GsColors.BorderSoft.ToUint(), style.WindowRounding, ImDrawFlags.RoundCornersAll, 3f * ImGuiHelpers.GlobalScale);
        winPtr.DrawList.PopClipRect();
    }

    private void DrawHeader(Vector2 size)
    {
        using var _ = ImRaii.Child("header-region", size);
        if (_reportType!.Value is ReportKind.Profile)   
            CkGui.FontText("Profile Report", Fonts.SubtitleFont, CkCol.TriStateCross.Vec4());
        else if (_reportType!.Value is ReportKind.GlobalChat)
            CkGui.FontText("Global Chat Report", Fonts.SubtitleFont, CkCol.TriStateCross.Vec4());

        var winPtr = ImGuiInternal.GetCurrentWindow();
        if (winPtr.SkipItems)
            return;
        // Button on the far right of this child, scaled to fit the height of the child
        var btnSize = new Vector2(size.Y);
        var pos = new Vector2(winPtr.InnerRect.Max.X - btnSize.X, winPtr.InnerRect.Min.Y);
        var hitbox = new ImRect(pos, pos + btnSize);
        var id = ImGui.GetID("header_close_btn");
        winPtr.DC.CursorPos = pos;
        ImGuiInternal.ItemSize(btnSize, 0);
        if (!ImGuiP.ItemAdd(hitbox, id, null))
            return;
        
        bool hovered = false, active = false;
        var clicked = ImGuiP.ButtonBehavior(hitbox, id, ref hovered, ref active);
        ImGuiP.RenderNavHighlight(hitbox, id);
        var buttonCol = (hovered, active) switch
        {
            (true, true) => ColorHelpers.Darken(uint.MaxValue, 0.25f),
            (true, false) => ColorHelpers.Lighten(uint.MaxValue, 0.3f),
            _ => uint.MaxValue,
        };

        using (Fonts.IconFramedFont.Push())
        {
            var iconStr = FAI.TimesCircle.ToIconString();
            var font = ImGui.GetFont();
            var baseFontSize = ImGui.GetFontSize();
            var drawnFontSize = size.Y * 0.75f; // Scale icon to 75% of the button height
            var textSize = ImGui.CalcTextSize(iconStr) * (drawnFontSize / baseFontSize);
            var drawPos = pos + (btnSize - textSize) * 0.5f; // Perfectly center the icon

            // Manually draw the shadow, then the scaled text over it
            winPtr.DrawList.AddText(font, drawnFontSize, drawPos + new Vector2(1, 1), 0xFF000000, iconStr);
            winPtr.DrawList.AddText(font, drawnFontSize, drawPos, buttonCol, iconStr);
        }

        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            CkGui.AttachTooltip("Discard Report Draft & Close Window.");
        }

        if (clicked)
            IsOpen = false;
    }

    #region Report Fourm
    private void DrawContents(float width)
    {
        using var __ = ImRaii.Child("contents-panel", new Vector2(width, -1));
        
        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();
        var btnHeight = ImGuiHelpers.GetButtonSize("Submit").Y;

        CkGui.FontText("Reportable Actions", Fonts.DefaultScaled, ImGuiColors.ParsedGold);
        ImGui.Spacing();
        if (_reportType!.Value is ReportKind.Profile)
        {
            CkGui.TextWrapped("• Harassment or targeted abuse, directly or indirectly.");
            CkGui.TextWrapped("• Impersonating another player or community staff.");
            CkGui.TextWrapped("• Sharing topics or links that violate community rules.");
        }
        else if (_reportType!.Value is ReportKind.GlobalChat)
        {
            CkGui.TextWrapped("• Exhibiting a severe lack of common sense or basic decency.");
            CkGui.TextWrapped("• Discussion of prohibited NSFL topics (Gore/Vore/Scat/Ageplay).");
            CkGui.TextWrapped("• Impersonating another player or community staff when undesired.");
            CkGui.TextWrapped("• Disrespecting or ignoring Cordy's final word on moderation decisions.");
            ImGui.Spacing();
            CkGui.ColorTextWrapped("The sent report includes 20 messages before and " +
                "5 after the selected chat message as report context.", ImGuiColors.DalamudGrey2);
        }

        ImGui.Spacing();
        CkGui.ColorTextWrapped("Note: Abuse/Misuse of reporting can result in a strike to your account.", ImGuiColors.DalamudRed);

        using (Fonts.DefaultScaled.Push())
            CkGui.TextShadowed("Report Details", 0xFF000000, new Vector2(3f), 2f);
        var labelPos = ImGui.GetItemRectMin();
        var labelSize = ImGui.GetItemRectSize();
        var linePos = new Vector2(labelPos.X, labelPos.Y + labelSize.Y);
        winPtr.DrawList.PathLineTo(linePos);
        linePos.X += winPtr.InnerClipRect.GetSize().X;
        winPtr.DrawList.PathLineTo(linePos);
        winPtr.DrawList.PathStroke(uint.MaxValue);

        var inputHeight = ImGui.GetContentRegionAvail().Y - btnHeight - style.ItemSpacing.Y * 2;
        ImGui.InputTextMultiline("##reportReason", ref _reportReason, 2000, new Vector2(-1, inputHeight));

        var dis = string.IsNullOrWhiteSpace(_reportReason) || _reportReason.Equals("Describe your report here", StringComparison.OrdinalIgnoreCase);
        if (CkGui.ButtonEx($"Submit Report Against {_reportedDisplayName}", CkCol.TriStateCross.Vec4(), new Vector2(-1, 0), dis))
        {
            var reason = _reportReason;
            if (_reportType!.Value is ReportKind.Profile)
            {
                _ = _hub.UserReportProfile(new(_reportedUser!, reason));
                IsOpen = false;
            }
            else if (_reportType!.Value is ReportKind.GlobalChat)
            {
                if (_chatLog != null && _reportedMsg != null)
                {
                    _ = _hub.UserReportChat(new(_reportedUser!, ChatlogId.GlobalChat, _reportedMsg.MsgId, reason));
                    IsOpen = false;
                }
            }
        }
        CkGui.AttachTooltip($"Submit report to the CK Team.");
    }
    #endregion

    #region Reported Content
    private void DrawReportedContent(Vector2 outerRegion, Vector2 innerRegion)
    {
        using var _ = ImRaii.Child("reported-snapshot-outer", outerRegion);

        var inset = new Vector2(MathF.Max(0f, (outerRegion.X - innerRegion.X) * .5f), MathF.Max(0f, (outerRegion.Y - innerRegion.Y) * .5f));
        ImGui.SetCursorPos(ImGui.GetCursorPos() + inset);
        if (_reportType!.Value is ReportKind.Profile)
            DrawReportedProfileSnapshot(innerRegion);
        else if (_reportType!.Value is ReportKind.GlobalChat)
            DrawReportedChatSnapshot(innerRegion);
    }

    private void DrawReportedProfileSnapshot(Vector2 region)
    {
        if (_profileSnapshot is null || _iconWrap is null || _bgWrap is null)
            return;

        var gScale = ImGuiHelpers.GlobalScale;
        var borderVec = ImGuiHelpers.ScaledVector2(3f);
        var rounding = 24f * gScale;
        // Center inner window
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, rounding);
        using var __ = ImRaii.Child("profile-snapshot", region, false, WFlags.NoScrollbar | WFlags.NoScrollWithMouse);

        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();
        var clipMin = winPtr.InnerRect.Min + borderVec;
        var clipMax = winPtr.InnerRect.Max - borderVec;

        winPtr.DrawList.PushClipRect(clipMin, clipMax, false);
        // Build the profile and display it.
        //if (!_config.Data.AllowNSFW && _snapshotBgIsNSFW && !_bypassBgWarn)
        //    DrawNsfwWarnBg(winPtr, style, CosmeticService.CoreTextures.Cache[CoreTexture.DefaultUserBg], _profileSnapshot, 0f);
        //else
        //    ProfileBuilder.DrawUserBG(winPtr, style, _bgWrap, _profileSnapshot, ProfileBuilder.ROUNDING, gScale);

        //ProfileBuilder.DrawUserFade(winPtr, _profileSnapshot, gScale);
        //ProfileBuilder.DrawUserShapes(winPtr, style, _iconWrap, _profileSnapshot.Theme, gScale);
        //winPtr.DrawList.PopClipRect();
        //// Frame shouldnt be clipped.
        //ProfileBuilder.DrawUserFrame(winPtr, _profileSnapshot, ProfileBuilder.ROUNDING, gScale);

        //winPtr.DrawList.PushClipRect(clipMin, clipMax, false);
        //ProfileBuilder.DrawUserDisplayName(winPtr, _profileSnapshot, _reportedDisplayName, _reportedSubName, _reportedUser!.UID, gScale, false);
        //ProfileBuilder.DrawUserActivities(winPtr, style, _profileSnapshot, gScale);

        //if (!_config.Data.AllowNSFW && _snapshotDescIsNSFW && !_bypassDescWarn)
        //    DrawNsfwWarnDesc(winPtr, style, _profileSnapshot, gScale);
        //else
        //    ProfileBuilder.DrawUserDescription(winPtr, style, _profileSnapshot, gScale);

        //ProfileBuilder.DrawVanityIcon(winPtr, style, _profileSnapshot, _reportedUser, gScale);
        winPtr.DrawList.PopClipRect();
    }

    private void DrawReportedChatSnapshot(Vector2 region)
    {
        using var c = ImRaii.PushColor(ImGuiCol.Border, 0xFFCCCCCC);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 2f * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.ChildRounding, 12f * ImGuiHelpers.GlobalScale);
        using var __ = ImRaii.Child("chat-snapshot", region, true);

        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();

        if (_chatLog is null || _reportedMsg is null)
            return;

        var messages = _chatLog.Messages.ToList();
        var targetIdx = messages.FindIndex(m => m.MsgId == _reportedMsg.MsgId);
        if (targetIdx is -1)
            return;

        // Wrap the 10 messages above and below the target message
        var startIdx = Math.Max(0, targetIdx - 15);
        var endIdx = Math.Min(messages.Count - 1, targetIdx + 15);

        for (var i = startIdx; i <= endIdx; i++)
        {
            var msg = messages[i];
            var isTarget = msg.MsgId == _reportedMsg.MsgId;
            winPtr.DrawList.ChannelsSplit(2);
            winPtr.DrawList.ChannelsSetCurrent(1);
            using (ImRaii.Group())
            {
                DrawMessagePrefix(msg);
                ImUtf8.SameLineInner();
                NewRichText.TextFlowWrappedOrDummy(msg.Message, id: _chatLog.ID + msg.MsgId);
            }

            // Highlightable area.
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            min.X = winPtr.InnerRect.Min.X;
            max.X = winPtr.InnerRect.Max.X;
            min.Y -= 2f;
            max.Y += 2f;
            winPtr.DrawList.ChannelsSetCurrent(0);
            var hovered = ImGui.IsMouseHoveringRect(min, max);
            if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _reportedMsg = msg;
                _reportedUser = msg.Sender;
                _reportedDisplayName = _pairService.GetProfileDisplayName(msg.Sender);
            }

            if (isTarget)
            {
                winPtr.DrawList.AddRectFilled(min, max, 0x2E57C1E8);
                winPtr.DrawList.AddRect(min, max, 0x6657C1E8);
            }
            else if (hovered)
            {
                winPtr.DrawList.AddRectFilled(min, max, 0x2AAC8E02);
                winPtr.DrawList.AddRect(min, max, 0xBBAC8E02);
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                CkGui.AttachTooltip("Mark message as target for report context.");
            }

            winPtr.DrawList.ChannelsMerge();

            void DrawMessagePrefix(NewGsChatMessage message)
            {
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
        }
    }
    #endregion

    private void ResetAllData()
    {
        _reportType = null;
        _reportedUser = null;
        _reportedDisplayName = string.Empty;
        _reportReason = string.Empty;
        _iconWrap = null;
        _bgWrap = null;
        _profileSnapshot = null;
    }
}
