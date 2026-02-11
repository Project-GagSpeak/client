using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class GlobalChatTab : DisposableMediatorSubscriberBase
{
    private readonly GlobalChatLog _chat;
    private readonly KinkPlateService _plateManager;
    private readonly TutorialService _guides;
    private readonly MainMenuTabs _tabmenu;

    public GlobalChatTab(ILogger<GlobalChatTab> logger, GagspeakMediator mediator,
        GlobalChatLog globalChat, KinkPlateService plateManager, TutorialService guides, MainMenuTabs tabMenu)
        : base(logger, mediator)
    {
        _chat = globalChat;
        _plateManager = plateManager;
        _tabmenu = tabMenu;
        _guides = guides;
    }

    public void DrawSection()
    {
        var min = ImGui.GetCursorScreenPos();
        var max = min + ImGui.GetContentRegionAvail();
        // Add some CkRichText variant here later.
        CkGui.ColorTextCentered("GagSpeak Global Chat", GsCol.VibrantPink.Uint());
        ImGui.Separator();

        using var col = ImRaii.PushColor(ImGuiCol.ScrollbarBg, GsCol.LushPinkButton.Uint())
            .Push(ImGuiCol.ScrollbarGrab, GsCol.VibrantPink.Uint())
            .Push(ImGuiCol.ScrollbarGrabHovered, GsCol.VibrantPinkHovered.Uint());

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
        using (ImRaii.Group())
        {
            _chat.DrawChat(ImGui.GetContentRegionAvail());
        }
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.UsingGlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ChatMessageExamine, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () => _tabmenu.TabSelection = MainMenuTabs.SelectedTab.Homepage);

        if (GlobalChatLog.NotVerified)
            CkGui.AttachToolTip("Cannot use chat, your account is not verified!");
        // Attach tutorials.
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.GlobalChat, ImGui.GetWindowPos(), ImGui.GetWindowSize());
    }

    private void DrawChatUseBlockedWarning()
    {
        var errorHeight = CkGui.CalcFontTextSize("A", UiFontService.UidFont).Y * 2 + CkGui.CalcFontTextSize("A", UiFontService.Default150Percent).Y + ImUtf8.ItemSpacing.Y * 2;
        var centerDrawHeight = (ImGui.GetContentRegionAvail().Y - ImUtf8.FrameHeightSpacing - errorHeight) / 2;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerDrawHeight);
        CkGui.FontTextCentered("Blocked Via Bad Reputation!", UiFontService.UidFont, ImGuiColors.DalamudRed);
        CkGui.FontTextCentered("Unable to view chat anymore.", UiFontService.UidFont, ImGuiColors.DalamudRed);
        CkGui.FontTextCentered($"You have [{MainHub.Reputation.ChatStrikes}] chat strikes.", UiFontService.Default150Percent, ImGuiColors.DalamudRed);
    }

    private void DrawNotVerifiedHelp()
    {
        var errorHeight = CkGui.CalcFontTextSize("A", UiFontService.UidFont).Y * 2 + CkGui.CalcFontTextSize("A", UiFontService.Default150Percent).Y * 2 + ImUtf8.TextHeight * 3 + ImUtf8.ItemSpacing.Y * 6;
        var centerDrawHeight = (ImGui.GetContentRegionAvail().Y - errorHeight) / 2;

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerDrawHeight);
        CkGui.FontTextCentered("Must Claim Account To Chat!", UiFontService.UidFont, ImGuiColors.DalamudRed);
        CkGui.FontTextCentered("For Moderation & Safety Reasons", UiFontService.Default150Percent, ImGuiColors.DalamudGrey);
        CkGui.FontTextCentered("Only Verified Users Get Social Features.", UiFontService.Default150Percent, ImGuiColors.DalamudGrey);
        ImGui.Spacing();
        CkGui.CenterText("You can verify via GagSpeak's Discord Bot.");
        CkGui.CenterText("Verification is easy & doesn't interact with lodestone");
        CkGui.CenterText("or any other SE properties.");
    }
}

