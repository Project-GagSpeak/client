using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using OtterGui.Text;
using OtterGuiInternal;

namespace GagSpeak.Gui.Components;

public class VerificationUi : WindowMediatorSubscriberBase
{
    private string _verificationCode = string.Empty;
    private bool _openPopup = false;

    public VerificationUi(ILogger<VerificationUi> logger, GagspeakMediator mediator)
        : base(logger, mediator, "GagSpeak Verification Overlay")
    {
        Flags = WFlags.NoBringToFrontOnFocus
            | WFlags.NoDecoration
            | WFlags.NoInputs
            | WFlags.NoSavedSettings
            | WFlags.NoBackground
            | WFlags.NoMove
            | WFlags.NoTitleBar;
        IsOpen = false;

        Mediator.Subscribe<VerificationPopupMessage>(this, msg =>
        {
            _verificationCode = msg.VerificationCode.Code;
            _openPopup = true;
            IsOpen = true;
        });
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 16f * ImGuiHelpers.GlobalScale);
        base.PreDraw();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        base.PostDraw();
    }

    public override bool DrawConditions()
        => MainHub.IsConnected && _verificationCode.Length > 0;

    protected override void DrawInternal()
    {
        if (_openPopup)
        {
            ImGui.OpenPopup("VerificationModal");
            _openPopup = false;
        }

        var viewportSize = ImGui.GetWindowViewport().Size;
        ImGui.SetNextWindowSize(new Vector2(600, 160) * ImGuiHelpers.GlobalScale);
        ImGui.SetNextWindowPos(viewportSize / 2, ImGuiCond.Always, new Vector2(0.5f));

        // Open the popup
        using var popup = ImRaii.Popup("VerificationModal", WFlags.Modal);
        if (!popup) return;

        var winPtr = ImGuiInternal.GetCurrentWindow();
        var style = ImGui.GetStyle();
        var min = winPtr.InnerRect.Min;
        var max = winPtr.InnerRect.Max;
        var innerSize = winPtr.InnerRect.GetSize();

        var rounding = 16f * ImGuiHelpers.GlobalScale;
        var borderCol = ImGui.GetColorU32(ImGuiCol.FrameBg);

        // BG Fill
        winPtr.DrawList.PushClipRect(min, max, true);
        winPtr.DrawList.AddRectFilled(min, max, 0xFF4411CC, rounding, ImDrawFlags.RoundCornersAll);
        winPtr.DrawList.AddRect(min, max, borderCol, rounding, ImDrawFlags.RoundCornersAll, 3f * ImGuiHelpers.GlobalScale);
        winPtr.DrawList.PopClipRect();

        var spacingBetween = style.ItemSpacing.Y * 2;
        var totalWidth = innerSize.X * 0.9f;
        var startX = min.X + (innerSize.X * 0.05f);

        Vector2 btnSize;
        float fontHeight;
        using (Fonts.SubtitleFont.Push())
        {
            fontHeight = ImGui.CalcTextSize("A").Y;
            var titleText = $"Verification Code for {MainHub.OwnUserData?.AliasOrUID ?? "Loading..."}";
            var titleWidth = ImGui.CalcTextSize(titleText).X;

            var totalContentHeight = (fontHeight * 2) + spacingBetween;
            var startY = min.Y + (innerSize.Y - totalContentHeight) * 0.5f;

            winPtr.DC.CursorPos = new Vector2(min.X + (innerSize.X - titleWidth) * 0.5f, startY);
            ImGui.TextUnformatted(titleText);

            var btnDim = fontHeight + style.FramePadding.Y * 2;
            btnSize = new Vector2(btnDim, btnDim);
            var inputWidth = totalWidth - btnDim - style.ItemSpacing.X;

            // Ensure correct input width.
            winPtr.DC.CursorPos = new Vector2(startX, startY + fontHeight + spacingBetween);
            ImGui.SetNextItemWidth(inputWidth);
            var tempCode = _verificationCode;
            using (ImRaii.PushColor(ImGuiCol.FrameBg, 0xAA222222))
                ImGui.InputText("##copiable-code", ref tempCode, 64, ImGuiInputTextFlags.ReadOnly);

            // Click-to-Copy overlay logic
            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(_verificationCode);
            CkGui.AttachTooltip("Click to copy verification code to clipboard");
        }

        ImUtf8.SameLineInner();
        CodeCopyButton();

        if (DrawCloseButton(winPtr, style))
        {
            _verificationCode = string.Empty;
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachTooltip("--SEP--Be sure verification is successful before closing.");

        void CodeCopyButton()
        {
            if (winPtr.SkipItems)
                return;

            var copyBtnId = ImGui.GetID("copy_verification_btn");
            var copyPos = winPtr.DC.CursorPos;
            var copyHitbox = new ImRect(copyPos, copyPos + btnSize);

            ImGuiInternal.ItemSize(btnSize, style.FramePadding.Y);
            if (!ImGuiP.ItemAdd(copyHitbox, copyBtnId, null))
                return;

            bool copyHovered = false, copyActive = false;
            var copyClicked = ImGuiP.ButtonBehavior(copyHitbox, copyBtnId, ref copyHovered, ref copyActive);
            ImGuiP.RenderNavHighlight(copyHitbox, copyBtnId);

            var tankBlueUint = ImGuiColors.TankBlue.ToUint();
            var copyBtnCol = (copyHovered, copyActive) switch
            {
                (true, true) => ColorHelpers.Darken(tankBlueUint, 0.25f),
                (true, false) => ColorHelpers.Lighten(tankBlueUint, 0.3f),
                _ => tankBlueUint,
            };

            winPtr.DrawList.AddRectFilled(copyHitbox.Min, copyHitbox.Max, copyBtnCol, style.FrameRounding);

            using (Fonts.IconFont.Push())
            {
                var iconStr = FAI.Copy.ToIconString();
                var baseFontSize = ImGui.GetFontSize();
                var drawnFontSize = fontHeight * 0.75f;
                var textSize = ImGui.CalcTextSize(iconStr) * (drawnFontSize / baseFontSize);
                var drawPos = copyPos + (btnSize - textSize) * 0.5f;
                winPtr.DrawList.AddText(ImGui.GetFont(), drawnFontSize, drawPos, 0xFFFFFFFF, iconStr);
            }

            if (copyHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                CkGui.ToolTipInternal("Click to copy verification code to clipboard");
            }

            if (copyClicked)
                ImGui.SetClipboardText(_verificationCode);
        }
    }

    private bool DrawCloseButton(ImGuiWindowPtr winPtr, ImGuiStylePtr style)
    {
        if (winPtr.SkipItems)
            return false;

        var id = ImGui.GetID("close");
        var padding = style.FramePadding.Y * 2;
        var iconSize = CkGui.IconSize(FAI.TimesCircle);
        var itemSize = new Vector2(iconSize.X + style.FramePadding.X * 2, ImUtf8.FrameHeight);
        var pos = new Vector2(winPtr.InnerRect.Max.X - itemSize.X - padding, winPtr.InnerRect.Min.Y + padding);
        var hitbox = new ImRect(pos, pos + itemSize);

        ImGuiInternal.ItemSize(itemSize, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(hitbox, id, null))
            return false;

        bool hovered = false, active = false;
        var clicked = ImGuiP.ButtonBehavior(hitbox, id, ref hovered, ref active);
        ImGuiP.RenderNavHighlight(hitbox, id);

        var buttonCol = (hovered, active) switch
        {
            (true, true) => ColorHelpers.Darken(ImGuiColors.DalamudYellow.ToUint(), 0.25f),
            (true, false) => ColorHelpers.Lighten(ImGuiColors.DalamudYellow.ToUint(), 0.3f),
            _ => ImGuiColors.DalamudYellow.ToUint(),
        };

        using (Fonts.IconFramedFont.Push())
            winPtr.DrawList.AddTextShadowed(FAI.TimesCircle.ToIconString(), pos + style.FramePadding, buttonCol, 0xFF000000);

        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            CkGui.ToolTipInternal("Close Window");
        }

        return clicked;
    }
}
