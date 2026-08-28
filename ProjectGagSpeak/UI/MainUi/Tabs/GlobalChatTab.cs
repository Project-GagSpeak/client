using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Chat;
using GagSpeak.Gui.Components;
using GagSpeak.Localization;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using OtterGui.Text;
using TerraFX.Interop.Windows;

namespace GagSpeak.Gui.MainWindow;

// this can easily become the "contact list" tab of the "main UI" window.
public class GlobalChatTab
{
    private readonly MainMenuTabs _tabMenu;
    private readonly GlobalChatLog _chat;
    private readonly GlobalChatDrawer _chatDrawer;
    private readonly GagspeakMediator _mediator;
    private readonly TutorialService _guides;

    private bool _showRules = false;

    public GlobalChatTab(GagspeakMediator mediator, MainMenuTabs tabmenu, 
        GlobalChatLog chat, GlobalChatDrawer chatDrawer, TutorialService guides)
    {
        _mediator = mediator;
        _tabMenu = tabmenu;
        _chat = chat;
        _chatDrawer = chatDrawer;
        _guides = guides;
    }

    public void DrawSection()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f * ImGuiHelpers.GlobalScale);
        using var col = ImRaii.PushColor(ImGuiCol.ScrollbarGrab, GsCol.VibrantPink.Uint()).Push(ImGuiCol.ScrollbarGrabHovered, GsCol.VibrantPinkHovered.Uint());

        var min = ImGui.GetCursorScreenPos();
        var max = min + ImGui.GetContentRegionAvail();

        var minX = ImGui.GetWindowContentRegionMin().X;
        var totalWidth = CkGui.GetWindowContentRegionWidth(); // Or ImGui.GetContentRegionAvail().X

        // Draw Left Settings
        CkGui.HoverIconText(FAI.Book, ImGuiColors.TankBlue.ToUint());
        CkGui.AttachTooltip("--COL--Global Chat Rules--COL--" +
            "--SEP----COL--1.--COL-- Have common sense please." +
            "--NL----COL--2.--COL-- No discussion of NSFL (Gore/Vore/Scat/Ageplay)" +
            "--NL----COL--2b.--COL-- The above is fine in Kinkplate™ descriptions." +
            "--NL----COL--3.--COL-- Respect Cordys word as it is final." +
            "--NL----COL--4.--COL-- Enjoy socializing with others ♥", ImGuiColors.ParsedGold);

        // Center Text
        var centerText = "GagSpeak Global Chat";
        var textWidth = ImGui.CalcTextSize(centerText).X;
        var centerX = minX + (totalWidth / 2) - (textWidth / 2);
        ImGui.SameLine(centerX);
        CkGui.ColorText(centerText, GsCol.VibrantPink.Uint());

        // Rules Icon
        ImGui.SameLine(minX + totalWidth - CkGui.IconSize(FAI.Book).X);
        CkGui.HoverIconText(FAI.Cog, ImGuiColors.TankBlue.ToUint());
        if (ImGui.IsItemClicked())
            _mediator.Publish(new UiToggleMessage(typeof(SettingsUi)));
        CkGui.AttachTooltip("View configurable settings for chat.");

        ImGui.Separator();
        DrawChatRegion(min, max);
    }

    private void DrawChatRegion(Vector2 absMin, Vector2 absMax)
    {
        var min = ImGui.GetCursorScreenPos();
        using (ImRaii.Group())
            _chatDrawer.Draw();

        CkGui.AttachTooltip("Cannot use chat, your account is not verified!", MainHub.Reputation.IsVerified);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.UsingGlobalChat, MainUI.LastPos, MainUI.LastSize);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ChatEmotes, MainUI.LastPos, MainUI.LastSize);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ChatScroll, MainUI.LastPos, MainUI.LastSize);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ChatMessageExamine, MainUI.LastPos, MainUI.LastSize, _ => _tabMenu.TabSelection = MainMenuTabs.SelectedTab.Homepage);
        

        var showOverlay = !MainHub.Reputation.CanUseChat || _showRules || !MainHub.Reputation.IsVerified;
        if (!showOverlay)
            return;

        ImGui.SetCursorScreenPos(min);
        using var _ = ImRaii.Child("overlay-child", absMax - min, false);
        ImGui.GetWindowDrawList().AddRectFilledMultiColor(absMin, absMax, 0xCC000000, 0xCC000000, 0x99111111, 0x99111111);

        if (!MainHub.Reputation.CanUseChat)
        {
            var strikeText = $"You have [{MainHub.Reputation.ChatStrikes}] chat strikes.";
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
            var row4Size = CkGui.CalcFontTextSize("You can verify via GagSpeak's Discord Bot.", Fonts.HeaderFont);
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
        else if (_showRules)
        {
            var titleHeight = CkGui.CalcFontTextSize("Global Chat Rules", Fonts.SubtitleFont);
            var rulesHeight = titleHeight.Y + (ImUtf8.TextHeightSpacing * 5);
            // Reusing your absolute positioning logic from the original rules block
            var centerDrawHeight = (absMax.Y - min.Y - rulesHeight) / 2;
            ImGui.SetCursorScreenPos(new Vector2(min.X, min.Y + centerDrawHeight));

            using (Fonts.SubtitleFont.Push())
            {
                CkGui.SetCursorXtoCenter(titleHeight.X);
                CkGui.TextShadowed("Global Chat Rules", GsCol.VibrantPinkHovered.Uint(), 0xFF000000, Vector2.One, 4f, 8);
            }

            ImGui.Spacing();
            CkGui.SetCursorXtoCenter(ImGui.CalcTextSize("1. Refrain from spamming, flooding, or sharing unsafe links.").X);
            CkGui.TextShadowed("1. Refrain from spamming, flooding, or sharing unsafe links.", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);

            CkGui.SetCursorXtoCenter(ImGui.CalcTextSize("2. Agressive toxicity and harassment are strictly prohibited.").X);
            CkGui.TextShadowed("2. Agressive toxicity and harassment are strictly prohibited.", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);

            CkGui.SetCursorXtoCenter(ImGui.CalcTextSize("3. Moderation team decisions regarding reports are final.").X);
            CkGui.TextShadowed("3. Moderation team decisions regarding reports are final.", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);

            CkGui.SetCursorXtoCenter(ImGui.CalcTextSize("4. Accruing 3 chat violations revokes RadarChat access.").X);
            CkGui.TextShadowed("4. Accruing 3 chat violations revokes RadarChat access.", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);

            CkGui.SetCursorXtoCenter(ImGui.CalcTextSize("5. Accruing 5 account violations totals results in an account ban.").X);
            CkGui.TextShadowed("5. Accruing 5 account violations totals results in an account ban.", uint.MaxValue, 0xFF000000, Vector2.One, 4f, 8);

        }
    }
}

