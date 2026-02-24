using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Profile;
using GagSpeak.Gui.Publications;
using GagSpeak.Gui.Remote;
using GagSpeak.Gui.Toybox;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using OtterGui.Text;
using System.Globalization;

namespace GagSpeak.Gui.MainWindow;

/// <summary>
///     The landing page of GagSpeak. Pretty UI, and polished design.
/// </summary>
public class HomeTab
{
    private const string SUPPORTER_NAME_TOOLTIP = "Your Profile's Alias / UID." +
    "--SEP----COL--[L-Click]--COL--Copy your UID" +
    "--NL----COL--[CTRL + L-Click]--COL--Copy your Alias";
    private const string NAME_TOOLTIP = "Your Profile's Alias / UID." +
        "--SEP----COL--[L-Click]--COL--Copy your UID";

    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly KinkPlateService _service;
    private readonly TutorialService _guides;

    private bool _editingSafeword = false;

    public HomeTab(GagspeakMediator mediator, MainConfig config,
        KinkPlateService service, TutorialService guides)
    {
        _mediator = mediator;
        _config = config;
        _service = service;
        _guides = guides;
    }

    // Profile Draw Helpers.
    private Vector2 ProfileSize => ImGuiHelpers.ScaledVector2(201);
    private Vector2 RectMin { get; set; } = Vector2.Zero;
    private Vector2 AvatarPos => RectMin + ImGuiHelpers.ScaledVector2(6f);
    private Vector2 AvatarSize => ImGuiHelpers.ScaledVector2(MainUI.AVATAR_SIZE);
    private Vector2 EditBorderSize => ImGuiHelpers.ScaledVector2(44f);
    private Vector2 EditBorderPos => RectMin + ImGuiHelpers.ScaledVector2(156f, 2f);
    private Vector2 EditIconPos => RectMin + ImGuiHelpers.ScaledVector2(165f, 11f);
    private Vector2 EditIconSize => ImGuiHelpers.ScaledVector2(27f);

    public void DrawSection()
    {
        var wdl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail();
        var max = pos + size;
        var halfPos = pos with { Y = pos.Y + size.Y / 2f };
        var profile = _service.GetKinkPlate(MainHub.OwnUserData);
        // Background
        if (CosmeticService.TryGetBackground(PlateElement.Plate, profile.Info.PlateBG, out var plateBG))
            wdl.AddDalamudImageRounded(plateBG, pos, size, CkStyle.ChildRounding());

        // Gradient backdrop
        wdl.AddRectFilledMultiColor(halfPos, max, uint.MinValue, uint.MinValue, 0x44000000, 0x44000000);

        using var _ = CkRaii.FramedChildPaddedWH("Account", size, 0, GsCol.VibrantPink.Uint(), CkStyle.ChildRounding(), wFlags: WFlags.NoScrollbar);

        DrawProfileInfo(_.InnerRegion, profile);
        ImGui.Spacing();
        DrawMenuOptions();
    }

    private void DrawProfileInfo(Vector2 region, KinkPlate profile)
    {
        var left = region.X - ProfileSize.X - ImUtf8.ItemSpacing.X;
        var wdl = ImGui.GetWindowDrawList();
        using (var _ = CkRaii.Child("##AccountInfo", new Vector2(region.X, ProfileSize.Y)))
        {
            var min = ImGui.GetCursorPos();
            ProfileDisplayName();
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ClientUID, MainUI.LastPos, MainUI.LastSize);
            // Line Splitter.
            var pos = ImGui.GetCursorScreenPos();
            var lineSize = new Vector2(region.X - ProfileSize.X - ImUtf8.ItemSpacing.X, 5 * ImGuiHelpers.GlobalScale);
            wdl.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.AchievementLineSplit], pos, lineSize);
            ImGui.Dummy(lineSize);

            ProfileInfoRow(FAI.UserSecret, MainHub.OwnUserData.AnonName, "Your Anonymous name used in Requests / Chats.");

            var formattedDate = MainHub.OwnUserData.CreatedOn ?? DateTime.MinValue;
            string createdDate = formattedDate != DateTime.MinValue ? formattedDate.ToString("d", CultureInfo.CurrentCulture) : "MM-DD-YYYY";
            ProfileInfoRow(FAI.Calendar, createdDate, "Date your GagSpeak account was made.");

            ProfileInfoRow(FAI.Award, $"{ClientAchievements.Completed}/{ClientAchievements.Total}", "Current Achievement Progress.");

            ProfileInfoRow(FAI.ExclamationTriangle, $"0 Strikes.", "Reflects current Account Standing." +
                "--SEP--Accumulating too many strikes may lead to restrictions or bans.");

            DrawSafewordRow(left);
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.Safewords, MainUI.LastPos, MainUI.LastSize);

            ImGui.SetCursorPos(min + new Vector2(ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - ProfileSize.X, 0));
            var avatar = profile.GetProfileOrDefault();
            RectMin = ImGui.GetCursorScreenPos();

            // Draw out the avatar image.
            wdl.AddDalamudImageRounded(avatar, AvatarPos, AvatarSize, AvatarSize.Y / 2);
            // draw out the border for the profile picture
            if (CosmeticService.TryGetBorder(PlateElement.Avatar, profile.Info.AvatarBorder, out var pfpBorder))
                wdl.AddDalamudImageRounded(pfpBorder, RectMin, ProfileSize, ProfileSize.Y / 2);

            // Draw out Supporter Icon Black BG base.
            ImGui.SetCursorScreenPos(EditBorderPos);
            if (ImGui.InvisibleButton("##EditProfileButton", EditBorderSize))
                _mediator.Publish(new UiToggleMessage(typeof(KinkPlateEditorUI)));
            CkGui.AttachToolTip("Open and Customize your KinkPlate™!");

            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.ProfileEditing, MainUI.LastPos, MainUI.LastSize,
                () => _mediator.Publish(new UiToggleMessage(typeof(KinkPlateEditorUI), ToggleType.Show)));

            var bgCol = ImGui.IsItemHovered() ? 0xFF444444 : 0xFF000000;
            wdl.AddCircleFilled(EditBorderPos + EditBorderSize / 2, EditBorderSize.X / 2, bgCol);
            // Draw out Edit Icon.
            wdl.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Edit], EditIconPos, EditIconSize);
            wdl.AddCircle(EditBorderPos + EditBorderSize / 2, EditBorderSize.X / 2, GsCol.VibrantPink.Uint(), 0, 3f * ImGuiHelpers.GlobalScale);
        }
    }

    private void ProfileDisplayName()
    {
        var isSupporter = MainHub.OwnUserData.Tier is not CkSupporterTier.NoRole;

        CkGui.FontText(MainHub.DisplayName, Fonts.UidFont);
        CkGui.AttachToolTip(isSupporter ? SUPPORTER_NAME_TOOLTIP : NAME_TOOLTIP);
        // Copy based on interaction type.
        if (isSupporter && ImGui.GetIO().KeyCtrl && ImGui.IsItemClicked())
            ImGui.SetClipboardText(MainHub.OwnUserData.Alias);
        else if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(MainHub.UID);
    }

    private void ProfileInfoRow(FAI icon, string text, string tooltip)
    {
        ImGui.Spacing();
        using (ImRaii.Group())
        {
            CkGui.IconTextAligned(icon);
            CkGui.TextFrameAlignedInline(text);
        }
        CkGui.AttachToolTip(tooltip);
    }

    private void DrawSafewordRow(float width)
    {
        using var _ = ImRaii.Group();

        CkGui.IconTextAligned(FAI.HandPaper);
        using var font = ImRaii.PushFont(UiBuilder.MonoFont);

        if (_editingSafeword)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(width - ImUtf8.ItemInnerSpacing.X - ImUtf8.FrameHeight);
            var safeword = _config.Current.Safeword;
            if (ImGui.InputTextWithHint("##safeword", "Set a Safeword..", ref safeword, 35, ITFlags.EnterReturnsTrue))
            {
                _config.Current.Safeword = safeword;
                _config.Save();
                _editingSafeword = false;
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                _editingSafeword = false;
            font.Dispose();
            CkGui.AttachToolTip("Enter to save, right-click to cancel.");
        }
        else
        {
            // Display based on if we have a safeword set or not.
            if (string.IsNullOrWhiteSpace(_config.Current.Safeword))
                CkGui.ColorTextFrameAlignedInline("Click to set Safeword..", ImGuiColors.DalamudGrey2, false);
            else
                CkGui.ColorTextFrameAlignedInline(_config.Current.Safeword, CkCol.TriStateCross.Uint(), false);
            font.Dispose(); // will affect tt and tutorial if not.
            CkGui.AttachToolTip("Your current safeword. Click to edit!");
            _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SettingSafeword, MainUI.LastPos, MainUI.LastSize);

            if (ImGui.IsItemClicked())
                _editingSafeword = !_editingSafeword;
        }
    }

    // Draw 1 or 2 rows based on the menu height.
    private void DrawMenuOptions()
    {
        var region = ImGui.GetContentRegionAvail();
        var buttonH = CkGui.GetFancyButtonHeight();
        // The threshold to draw 2 or 1 rows.
        var thresholdHeight = buttonH * 8 + ImUtf8.ItemSpacing.Y * 7;
        // if we draw compact (2 columns) or full (1 column)
        var showCompact = region.Y < thresholdHeight;
        // Finalized Height of the child.
        var finalHeight = buttonH * (showCompact ? 4 : 8) + ImUtf8.ItemSpacing.Y * (showCompact ? 3 : 7);

        if (showCompact)
            DrawCompactButtons(region);
        else
            DrawButtonList(region);
    }

    private void DrawCompactButtons(Vector2 region)
    {
        var buttonWidth = (region.X - ImUtf8.ItemInnerSpacing.X) / 2;
        using (ImRaii.Group())
        {
            WardrobeButton(buttonWidth);
            CursedLootButton(buttonWidth);
            PuppeteerButton(buttonWidth);
            TriggersButton(buttonWidth);
            ToyboxButton(buttonWidth);
            ModPresetsButton(buttonWidth);
        }
        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            SexToyRemoteButton(buttonWidth);
            PublicationsButton(buttonWidth);
            AchievementsButton(buttonWidth);
            KoFiButton(buttonWidth);
            PatreonButton(buttonWidth);

            FeedbackButton(buttonWidth);
        }
    }

    private void DrawButtonList(Vector2 region)
    {
        var buttonWidth = region.X;
        using (ImRaii.Group())
        {
            SexToyRemoteButton(buttonWidth);
            WardrobeButton(buttonWidth);
            CursedLootButton(buttonWidth);
            PuppeteerButton(buttonWidth);
            TriggersButton(buttonWidth);
            ToyboxButton(buttonWidth);
            ModPresetsButton(buttonWidth);
            PublicationsButton(buttonWidth);
            AchievementsButton(buttonWidth);
            KoFiButton(buttonWidth);
            PatreonButton(buttonWidth);
            FeedbackButton(buttonWidth);
        }
    }

    private void SexToyRemoteButton(float width)
    {
        var disabled = true;
#if DEBUG
        // TODO: Remove when this works
        disabled = false;
#endif
        if (CkGui.FancyButton(FAI.WaveSquare, "Sex Toy Remote", width, disabled))
            _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI)));
        CkGui.AttachToolTip("Control Simulated, or IRL Sex Toys! --COL--[WIP]--COL--");
    }

    private void WardrobeButton(float width)
    {
        if (CkGui.FancyButton(FAI.ToiletPortable, "Wardrobe", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(WardrobeUI)));
        CkGui.AttachToolTip("Restraint Sets, Restrictions, Gags, and Collars");
    }

    private void CursedLootButton(float width)
    {
        if (CkGui.FancyButton(FAI.Coins, "Cursed Loot", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(CursedLootUI)));
        CkGui.AttachToolTip("Gamble away your fortunes and freedom with Cursed Loot!");
    }

    private void PuppeteerButton(float width)
    {
        if (CkGui.FancyButton(FAI.PersonHarassing, "Puppeteer", width))
            _mediator.Publish(new UiToggleMessage(typeof(PuppeteerUI)));
        CkGui.AttachToolTip("Who's in control now? (Global & Per-Kinkster Control)");
    }

    private void TriggersButton(float width)
    {
        if (CkGui.FancyButton(FAI.Bolt, "Triggers", width))
            _mediator.Publish(new UiToggleMessage(typeof(TriggersUI)));
        CkGui.AttachToolTip("Monitor events and react to them");
    }

    private void ToyboxButton(float width)
    {
        if (CkGui.FancyButton(FAI.BoxOpen, "Toybox", width))
            _mediator.Publish(new UiToggleMessage(typeof(ToyboxUI)));
        CkGui.AttachToolTip("Contains your Toys, Patterns, and Alarms--COL--[WIP]--COL--");
    }

    private void ModPresetsButton(float width)
    {
        if (CkGui.FancyButton(FAI.FileAlt, "Mod Presets", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(ModPresetsUI)));
        CkGui.AttachToolTip("Configure presets for your Penumbra mod settings!" +
            "--SEP--Presets can be attached to restraints and restrictions!");
    }

    private void PublicationsButton(float width)
    {
        if (CkGui.FancyButton(FAI.CloudUploadAlt, "Publications", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(PublicationsUI)));
        CkGui.AttachToolTip("Publish created Patterns & Moodles for others to enjoy!");
    }

    private void AchievementsButton(float width)
    {
        if (CkGui.FancyButton(FAI.Trophy, "Achievements", width, false))
            _mediator.Publish(new UiToggleMessage(typeof(AchievementsUI)));
        CkGui.AttachToolTip("View Achievement Progress & Rewards.");
    }

    private void KoFiButton(float buttonWidth)
    {
        if (CkGui.FancyButton(FAI.Coffee, "Tip GagSpeak", buttonWidth, false))
        {
            try { Process.Start(new ProcessStartInfo { FileName = "https://www.ko-fi.com/cordeliamist", UseShellExecute = true }); }
            catch (Bagagwa e) { Svc.Logger.Error($"Failed to open the Patreon link. {e.Message}"); }
        }
        CkGui.AttachToolTip("This plugin took a massive toll on my life." +
            "--NL--As happy as I am to make this free for all of you to enjoy, any support is much appreciated ♥" +
            "--NL--Will open --COL--ko-fi.com--COL-- in a new browser window.", ImGuiColors.ParsedPink);
    }

    private void PatreonButton(float buttonWidth)
    {
        if (CkGui.FancyButton(FAI.Pray, "Support GagSpeak", buttonWidth, false))
        {
            try { Process.Start(new ProcessStartInfo { FileName = "https://www.patreon.com/CordeliaMist", UseShellExecute = true }); }
            catch (Bagagwa e) { Svc.Logger.Error($"Failed to open the Patreon link. {e.Message}"); }
        }
        CkGui.AttachToolTip("This plugin took a massive toll on my life." +
            "--NL--As happy as I am to make this free for all of you to enjoy, any support is much appreciated ♥" +
            "--NL--Will open --COL--patreon.com--COL-- in a new browser window.", ImGuiColors.ParsedPink);
        _guides.OpenTutorial(TutorialType.MainUi, StepsMainUi.SelfPlug, MainUI.LastPos, MainUI.LastSize);
    }

    private void FeedbackButton(float buttonWidth)
    {
        if (CkGui.FancyButton(FAI.ThumbsUp, "Positive Feedback", buttonWidth, false))
        {
            try { Process.Start(new ProcessStartInfo { FileName = "https://forms.gle/4AL43XUeWna2DtYK7", UseShellExecute = true }); }
            catch (Bagagwa e) { Svc.Logger.Error($"Failed to open the google form. {e.Message}"); }
        }
        CkGui.AttachToolTip("Opens a short 1 question positive feedback form ♥" +
            "--SEP--They're a nice way for me to reflect how my efforts are positively impacting others~");
    }
}
