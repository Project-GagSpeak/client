using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using OtterGui.Text;

namespace GagSpeak.Gui;

public class GlobalChatPopoutUI : WindowMediatorSubscriberBase
{
    private readonly PopoutGlobalChatlog _chat;
    private bool _themePushed = false;

    public GlobalChatPopoutUI(ILogger<GlobalChatPopoutUI> logger, GagspeakMediator mediator, PopoutGlobalChatlog chat) 
        : base(logger, mediator, "Global Chat Popout UI")
    {
        _chat = chat;

        IsOpen = false;
        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(380, 500), new Vector2(700, 2000));
    }
    protected override void PreDrawInternal()
    {
        if (!_themePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            _themePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (_themePushed)
        {
            ImGui.PopStyleColor(2);
            _themePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        using var font = Fonts.Default150Percent.Push();
        using var col = ImRaii.PushColor(ImGuiCol.ScrollbarBg, GsCol.LushPinkButton.Uint())
            .Push(ImGuiCol.ScrollbarGrab, GsCol.VibrantPink.Uint())
            .Push(ImGuiCol.ScrollbarGrabHovered, GsCol.VibrantPinkHovered.Uint());

        var min = ImGui.GetCursorScreenPos();
        var max = min + ImGui.GetContentRegionAvail();
        // Add some CkRichText variant here later.
        CkGui.ColorTextCentered("GagSpeak Global Chat", GsCol.VibrantPink.Uint());
        ImGui.Separator();

        // Restrict drawing the chat if their not verified or blocked from using it.
        var chatTL = ImGui.GetCursorScreenPos();
        var disable = GlobalChatLog.AccessBlocked;
        // if not verified, show the chat, but disable it.
        _chat.SetDisabledStates(disable, disable);
        DrawChatContents();

        // If blocked, draw the warning.
        if (GlobalChatLog.ChatBlocked)
        {
            ImGui.GetWindowDrawList().AddRectFilledMultiColor(min, max, 0x11000000, 0x11000000, 0x77000000, 0x77000000);
            ImGui.SetCursorScreenPos(chatTL);
            DrawChatUseBlockedWarning();
        }
        else if (GlobalChatLog.NotVerified)
        {
            ImGui.GetWindowDrawList().AddRectFilledMultiColor(min, max, 0x11000000, 0x11000000, 0x77000000, 0x77000000);
            ImGui.SetCursorScreenPos(chatTL);
            DrawNotVerifiedHelp();
        }
    }

    private void DrawChatContents()
    {
        _chat.DrawChat(ImGui.GetContentRegionAvail());

        if (GlobalChatLog.NotVerified)
            CkGui.AttachToolTip("Cannot use chat, your account is not verified!");
    }

    private void DrawChatUseBlockedWarning()
    {
        var errorHeight = CkGui.CalcFontTextSize("A", Fonts.UidFont).Y * 2 + CkGui.CalcFontTextSize("A", Fonts.Default150Percent).Y + ImUtf8.ItemSpacing.Y * 2;
        var centerDrawHeight = (ImGui.GetContentRegionAvail().Y - ImUtf8.FrameHeightSpacing - errorHeight) / 2;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerDrawHeight);
        CkGui.FontTextCentered("Blocked Via Bad Reputation!", Fonts.UidFont, ImGuiColors.DalamudRed);
        CkGui.FontTextCentered("Unable to view chat anymore.", Fonts.UidFont, ImGuiColors.DalamudRed);
        CkGui.FontTextCentered($"You have [{MainHub.Reputation.ChatStrikes}] chat strikes.", Fonts.Default150Percent, ImGuiColors.DalamudRed);
    }

    private void DrawNotVerifiedHelp()
    {
        var errorHeight = CkGui.CalcFontTextSize("A", Fonts.UidFont).Y * 2 + CkGui.CalcFontTextSize("A", Fonts.Default150Percent).Y * 2 + ImUtf8.TextHeight * 3 + ImUtf8.ItemSpacing.Y * 6;
        var centerDrawHeight = (ImGui.GetContentRegionAvail().Y - errorHeight) / 2;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerDrawHeight);
        CkGui.FontTextCentered("Must Claim Account To Chat!", Fonts.UidFont, ImGuiColors.DalamudRed);
        CkGui.FontTextCentered("For Moderation & Safety Reasons", Fonts.Default150Percent, ImGuiColors.DalamudGrey);
        CkGui.FontTextCentered("Only Verified Users Get Social Features.", Fonts.Default150Percent, ImGuiColors.DalamudGrey);
        ImGui.Spacing();
        CkGui.CenterText("You can verify via GagSpeak's Discord Bot.");
        CkGui.CenterText("Verification is easy & doesn't interact with lodestone");
        CkGui.CenterText("or any other SE properties.");
    }
}
