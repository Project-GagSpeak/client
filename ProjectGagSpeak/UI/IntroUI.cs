using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui;

/// <summary> The introduction UI that will be shown the first time that the user starts the plugin. </summary>
public class IntroUi : WindowMediatorSubscriberBase
{
    private bool ThemePushed = false;

    private enum IntroUiPage : byte
    {
        Welcome = 0,
        AttributionsAbout = 1,
        UsageAgreement = 2,
        AccountSetup = 4,
        Initialized = 5
    }

    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly AccountManager _account;
    private readonly TutorialService _guides;

    private IntroUiPage _currentPage = IntroUiPage.Welcome;
    private IntroUiPage _furthestPage = IntroUiPage.Welcome;
    private string _secretKey = string.Empty;

    public IntroUi(ILogger<IntroUi> logger, GagspeakMediator mediator, MainHub mainHub,
        MainConfig config, AccountManager serverConfigs, TutorialService guides)
        : base(logger, mediator, "Welcome to GagSpeak! ♥")
    {
        _hub = mainHub;
        _config = config;
        _account = serverConfigs;
        _guides = guides;

        this.PinningClickthroughFalse();
        this.SetBoundaries(new(630, 800));

        ShowCloseButton = false;
        RespectCloseHotkey = false;
        Flags = WFlags.NoScrollbar | WFlags.NoResize;

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (_) => IsOpen = false);
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) => IsOpen = true);

        // Make initial page assumptions.
        if (!_config.Current.AcknowledgementUnderstood)
        {
            _currentPage = IntroUiPage.Welcome;
            _furthestPage = IntroUiPage.Welcome;
        }
        else if (!MainHub.IsServerAlive || !_account.HasValidProfile())
        {
            _currentPage = IntroUiPage.AccountSetup;
            _furthestPage = IntroUiPage.AccountSetup;
        }
        else
        {
            _currentPage = IntroUiPage.Initialized;
            _furthestPage = IntroUiPage.Initialized;
        }

    }

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12f);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGui.GetColorU32(ImGuiCol.TitleBg));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGui.GetColorU32(ImGuiCol.TitleBg));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        if (_furthestPage is IntroUiPage.Initialized)
        {
            _logger.LogDebug("Switching to main UI");
            Mediator.Publish(new SwitchToMainUiMessage());
            IsOpen = false;
            return;
        }


        // Obtain the image wrap for the introduction screen header to draw it based on the current position via scaled ratio.
        var pos = ImGui.GetCursorScreenPos();
        var wdl = ImGui.GetWindowDrawList();
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        // push clip rect.
        var winPadding = ImGui.GetStyle().WindowPadding;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();

        // Push padding.
        var expandedMin = minPos - new Vector2(winClipX, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winClipX, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);
        var availX = expandedMax.X - expandedMin.X;

        // Grab image & get ratio.
        var headerImg = CosmeticService.CoreTextures.Cache[CoreTexture.WelcomeOverlay];

        // Scale the headerImage size to fit within the window size while maintaining aspect ratio.
        var scaledRatio = availX / headerImg.Size.X;
        var scaledSize = headerImg.Size * scaledRatio;
        // Draw out the welcome image over this area.
        wdl.AddDalamudImage(headerImg, expandedMin, scaledSize);

        // Validate the button.
        ImGui.SetCursorScreenPos(expandedMin);
        if (_furthestPage is IntroUiPage.Welcome)
        {
            if (ImGui.InvisibleButton("readingSkillCheck", scaledSize) && _currentPage == IntroUiPage.Welcome)
            {
                _currentPage = IntroUiPage.AttributionsAbout;
                _furthestPage = IntroUiPage.AttributionsAbout;
            }
        }
        else
        {
            ImGui.Dummy(scaledSize);
        }

        // Below this we can draw out the progress display, with a gradient multicolor.
        var progressH = ImUtf8.FrameHeight * 1.5f;
        var progressPos = expandedMin + new Vector2(0, scaledSize.Y - (ImUtf8.FrameHeight * 2).AddWinPadY());

        ImGui.SetCursorScreenPos(progressPos);
        using (CkRaii.ChildPaddedW("progress display", availX, ImUtf8.FrameHeight * 1.5f))
            DrawProgressDisplay();

        // Add a final gradient lining to the bottom of the progress display.
        var contentPos = expandedMin + new Vector2(0, scaledSize.Y);
        wdl.AddRectFilledMultiColor(contentPos, expandedMax, CkColor.FancyHeaderContrast.Uint(), CkColor.FancyHeaderContrast.Uint(), 0, 0);
        wdl.AddLine(contentPos, contentPos + new Vector2(scaledSize.X, 0), 0xFF000000, 1f);
        wdl.PopClipRect();

        // Draw the contents based on the page.
        ImGui.SetCursorScreenPos(contentPos);
        ImGui.Spacing();
        var contentArea = ImGui.GetContentRegionAvail() - new Vector2(0, (ImUtf8.FrameHeight + ImUtf8.ItemSpacing.Y * 3) + ImGui.GetStyle().WindowPadding.Y);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f).Push(ImGuiStyleVar.ScrollbarRounding, 2f);
        using (var _ = CkRaii.Child("IntroPageContents", contentArea))
        {
            switch (_currentPage)
            {
                case IntroUiPage.Welcome:
                    PageContentsWelcome(_.InnerRegion);
                    break;
                case IntroUiPage.AttributionsAbout:
                    PageContentsAbout(_.InnerRegion);
                    break;
                case IntroUiPage.UsageAgreement:
                    PageContentsUsage(_.InnerRegion);
                    break;
                case IntroUiPage.AccountSetup:
                    PageContentsAccountSetup(_.InnerRegion);
                    break;
            }
        }

        ImGui.Separator();
        // If on welcome page, do not show button.
        if (_currentPage is IntroUiPage.Welcome || _currentPage >= IntroUiPage.AccountSetup)
            return;

        var text = GetNextButtonText();
        var buttonSize = CkGui.IconTextButtonSize(FAI.ArrowRight, text);
        CkGui.SetCursorXtoCenter(buttonSize);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y);
        if (CkGui.IconTextButton(FAI.ArrowRight, text, disabled: DisableButton(_currentPage)))
            DoButtonAdvancement();
    }

    private void DoButtonAdvancement()
    {
        // Perform update & action based on _currentPage condition.
        if (_currentPage != _furthestPage)
        {
            _currentPage = (IntroUiPage)(byte)_currentPage + 1;
            return;
        }

        switch (_furthestPage)
        {
            case IntroUiPage.Welcome:
                _furthestPage = IntroUiPage.AttributionsAbout;
                _currentPage = IntroUiPage.AttributionsAbout;
                break;

            case IntroUiPage.AttributionsAbout:
                _furthestPage = IntroUiPage.UsageAgreement;
                _currentPage = IntroUiPage.UsageAgreement;
                break;

            case IntroUiPage.UsageAgreement:
                _config.Current.AcknowledgementUnderstood = true;
                _config.Save();
                _furthestPage = IntroUiPage.AccountSetup;
                _currentPage = IntroUiPage.AccountSetup;
                break;

            case IntroUiPage.AccountSetup:
                // Attempt to generate an account. If this is successful, advance the page to initialized.
                if (_account.HasValidProfile())
                {
                    _furthestPage = IntroUiPage.Initialized;
                    _currentPage = IntroUiPage.Initialized;
                }
                break;
        }
    }

    private bool DisableButton(IntroUiPage page)
    => page switch
    {
        IntroUiPage.AccountSetup => !_account.HasValidProfile(),
        _ => false
    };

    private string GetNextButtonText()
        => _currentPage switch
        {
            IntroUiPage.AttributionsAbout => "To Usage Agreement",
            IntroUiPage.UsageAgreement => "I Understand BDSM & GagSpeak's Importance On Privacy",
            IntroUiPage.AccountSetup => "Login to Sundouleia!",
            _ => string.Empty
        };

    private void DrawProgressDisplay()
    {
        var frameH = ImUtf8.FrameHeight;
        var buttonSize = new Vector2((ImGui.GetContentRegionAvail().X - (frameH * 4)) / 4, frameH * 1.5f);
        var offsetY = (buttonSize.Y - frameH) / 2;

        // Draw out the buttons.
        DrawSetupButton("Welcome", buttonSize, IntroUiPage.Welcome, null);
        ImGui.SameLine(0, 0);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offsetY);
        CkGui.FramedIconText(FAI.ChevronRight);

        ImGui.SameLine(0, 0);
        DrawSetupButton("About", buttonSize, IntroUiPage.AttributionsAbout, null);
        ImGui.SameLine(0, 0);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offsetY);
        CkGui.FramedIconText(FAI.ChevronRight);

        ImGui.SameLine(0, 0);
        DrawSetupButton("Usage", buttonSize, IntroUiPage.UsageAgreement, null);
        ImGui.SameLine(0, 0);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offsetY);
        CkGui.FramedIconText(FAI.ChevronRight);

        ImGui.SameLine(0, 0);
        DrawSetupButton("Create Account", buttonSize, IntroUiPage.AccountSetup, null);
    }

    private void DrawSetupButton(string label, Vector2 region, IntroUiPage page, Action? onClick)
    {
        using var dis = ImRaii.Disabled(page > _furthestPage);
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f);
        var color = _currentPage == page ? CkColor.VibrantPink.Vec4() : ImGuiColors.ParsedGrey.Darken(.15f).WithAlpha(.5f);
        using var col = ImRaii.PushColor(ImGuiCol.Button, color).Push(ImGuiCol.ButtonHovered, color).Push(ImGuiCol.ButtonActive, color);

        if (ImGui.Button(label, region))
            _currentPage = page;
    }

    private void PageContentsWelcome(Vector2 region)
    {
        CkGui.FontText("Welcome to Project GagSpeak!", UiFontService.UidFont);

        CkGui.ColorTextWrapped("Project GagSpeak is a highly ambitious project devloped for over a year in closed Beta, " +
            "aiming to provide kinksters with an all-in-one BDSM plugin free of charge to enjoy.", CkColor.VibrantPinkHovered.Uint());


        CkGui.FontText("Features:", UiFontService.Default150Percent);
        using (CkRaii.Child("FeaturesListScrollable", ImGui.GetContentRegionAvail()))
        {
            CkGui.BulletText("KinkPlates™");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Formatted in an AdventurePlate-Like style", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Allows avatar image imports of any size", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Pan, Rotate, Zoom, and Crop imported images", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("See your current predicament reflected in your KinkPlate!", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Customize Titles and Element backgrounds, borders, or overlays!", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Pair Requests");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Only need one code to pair", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Attach messages, and nickname preferences", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Realistic Chat Garbler");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Uses a self-designed algorithm that accurately garbles messages in any language.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Garbler can process up to 3 gags simotaniously", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Modify output with various Arousal effects (studder, cut-off, and more)", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Display current Gagged status beside your NamePlate, shown to other Kinksters in view!", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Restraint Sets");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Restrictions that can be applied to your Character.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Lock your Glamour, C+, Penumbra Mods, Moodles, Hardcore Traits, and more!", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Adjust variants of your Restraint Sets using Layers, granting dynamic control.", ImGuiColors.DalamudGrey2);
            }


            ImGui.Spacing();
            CkGui.BulletText("Mod Preset Control");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("For those who use different outfits that use the same mod, with different settings for each.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Store a 'Preset' of a Penumbra Mods settings to GagSpeak, and bind it to any attached Gag or Restriction.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Works with Restraint Set Layers, allowing for more dynamic control of Bondage Mod variants.", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Collars");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Assign a Moodle, Glamour item & Dyes, Penumbra Mod to your Collar.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Manage who owns your Collar, and who's Collars you own.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Set allowances for owner control over Collar contents.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Add Collar writing, which is displayable on KinkPlates!", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Cursed Loot");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Make Restriction items discoverable in Treasure Coffers throughout the game!", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Brings Bondage Mimic Chests to FFXIV duties!", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Customize discovery chance, and lock time range.", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Puppeteer");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("A Vastly modular form of PuppetMaster, with full integration to all of GagSpeak's Modules.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Command triggers can be set for individual pairs, or globally.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Alias Lists allow for complex commands to be executed with ease.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Bind Aliases to HP changes, Actions, Restriction Changes, Emotes, and more! ", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Respond with multiple, simotanious actions for maximum dynamic control!", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Triggers & Patterns");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("An Improved FFXIV-VibePlugin Variant with full access.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Create complex trigger patterns based on actions, emotes, status effects, and more!", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Bind Patterns to multiple devices, down to their individual motors.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Reassign Devices and motors on existing patterns where compatible.", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("GagSpeak Vibrator Remote");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("A handcrafted reflection of the mobile Lovense Remote.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Control your devices with keybinds, or let others control you!", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Supports Motor float, and Motor looping during control.", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Alarms");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Set Alarms to notify you with device vibrations at specific times!", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.BulletText("Hardcore Control");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Maximize Immersion and Helplessness with others (at your own risk!)", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Force others to follow you, perform Emotes, stay locked away, and more.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("An area of control that requires mutual trust and respect between both parties.", ImGuiColors.DalamudGrey2);
            }
        }
    }
    // Attributions, Acknowledgements, and 'What helped get GagSpeak to this point.'
    private void PageContentsAbout(Vector2 region)
    {
        using var _ = CkRaii.Child("innerAbout", region, wFlags: WFlags.AlwaysVerticalScrollbar);
        CkGui.FontText("Dedications", UiFontService.Default150Percent);

        CkGui.BulletText("TBD", CkColor.VibrantPink.Uint());
    }

    // Understanding Sundouleia Privacy & Usage Transparency
    private void PageContentsUsage(Vector2 region)
    {
        CkGui.FontTextCentered("READ CAREFULLY, YOU WILL ONLY SEE THIS ONCE", UiFontService.Default150Percent, ImGuiColors.DalamudRed);
        ImGui.Spacing();
        CkGui.CenterText("Acknowledgement Of Usage & Privacy");
        using (CkRaii.FramedChildPaddedWH("UsageAndPrivacy", ImGui.GetContentRegionAvail(), 0, CkColor.RemoteBgDark.Uint(), wFlags: WFlags.AlwaysVerticalScrollbar))
        {
            ImGui.TextWrapped("Being a Server-Side Plugin, and a plugin full of kinky individuals, we all know there " +
                "will always be some of *those* people who will try to ruin the fun for everyone.");
            CkGui.ColorTextWrapped("As Such, by joining GagSpeak, you must acknowledge & accept the following:", ImGuiColors.DalamudRed);

            // Consent Reminder.
            ImGui.Spacing();
            CkGui.FontText("Consent", UiFontService.Default150Percent, ImGuiColors.ParsedGold);
            ImGui.TextWrapped("BDSM, at its foundation, highly values the aspect of consent.");
            CkGui.BulletText("By using GagSpeak you MUST abide by the boundaries & limits others set for you.");
            CkGui.BulletText("If you push these limits against their will or pressure them to give you more than they " +
                "are comfortable with, it will not be tolerated.");

            // Privacy 
            ImGui.Spacing();
            CkGui.FontText("Privacy", UiFontService.Default150Percent, ImGuiColors.ParsedGold);
            ImGui.TextWrapped("By using GagSpeak, you understand and acknowledge the following about data sharing:");
            CkGui.BulletText("Personal information (Character Name & Homeworld) are censored with an anonymous identity.");
            CkGui.BulletText("If you give up this information about you and it is used against you, that is of your own fault.");
            CkGui.BulletText("Be responsible about how you go about meeting up with others.");
            CkGui.BulletText("Your paired Kinksters will be able to see your active bondage state");
            CkGui.BulletText("Your paired Kinksters can control any aspect of GagSpeak you gave them permission to control.");
            CkGui.BulletText("If GagPlates are enabled, other pairs visible to you will see your Gagged Icon.");

            // Account Rep.
            ImGui.Spacing();
            CkGui.FontText("Account Reputation", UiFontService.Default150Percent, ImGuiColors.ParsedGold);
            CkGui.TextWrapped("Reputation is shared across all profiles (all Characters) to prevent abuse of social features. " +
                "Valid reports may result in strikes. 3 in any category restrict access to that category, and too many total " +
                "strikes lead to a ban.");
            CkGui.BulletText("Verification / Ban Status");
            CkGui.BulletText("KinkPlate™ Viewing");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Controls your access to viewing other KinkPlates™.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Used for preventing Stalker behavior.", ImGuiColors.DalamudGrey2);
            }
            CkGui.BulletText("KinkPlate™ Editing");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Controls your access to modify your Profile.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("Used to prevent unwanted displays or behaviors in public profiles.", ImGuiColors.DalamudGrey2);
            }
            CkGui.BulletText("Global Chat Usage");
            using (ImRaii.PushIndent())
            {
                CkGui.BulletText("Controls your access to send / receive radar chat messages.", ImGuiColors.DalamudGrey2);
                CkGui.BulletText("A moderation utility for others breaking chat rules or displaying undesirable behaviors.", ImGuiColors.DalamudGrey2);
            }

            ImGui.Spacing();
            CkGui.FontText("Hardcore Control", UiFontService.Default150Percent, ImGuiColors.ParsedGold);
            CkGui.ColorTextWrapped("Hardcore Functionality in GagSpeak directly affects your game at a core level, such as preventing " +
                "you from typing, blocking your sight, restricting your movement, forcing you to perform emotes, blocking " +
                "out actions from being used, and controling the GCD's of your actions, and more.", new Vector4(1,0,0,1));
            CkGui.BulletText("While these are all safe to use, be careful who you give these permissions access to.");
            CkGui.BulletText("I have granted you with a great degree of control towards others, you are expected to take care of these " +
                "individuals if granted access over them, and to not be wreckless.");

            ImGui.Spacing();
            CkGui.FontText("Predatory Behavior", UiFontService.Default150Percent, ImGuiColors.DalamudRed);
            ImGui.TextWrapped("The Main Dev of GagSpeak has endured years of manipulative predatory abuse, and will ensure that any report made" +
                "is resolved thoughtfully and with consideration in a manner that will not cause the reported to go after the reporter.");
            CkGui.BulletText("This experience will help distiguish between 'bait reports' and 'actual reports'.");
            CkGui.BulletText("If you notice any reportable behavior, report it in detail.");
            CkGui.BulletText("Our report system is designed to leave flagged Kinksters unaffected on their end, so they wont be " +
                "able to deduce who reported them until it is too late.");

            ImGui.Spacing();
        }


        ImGui.Spacing();
        CkGui.ColorTextCentered("Click this Button below once you have read and understood the above.", ImGuiColors.DalamudRed);
        if(ImGui.Button("Proceed To Account Creation.", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeightWithSpacing())))
        {
            _config.Current.AcknowledgementUnderstood = true;
            _config.Save();
        }
        ImGui.Spacing();
    }

    // For Generating an Account.
    private void PageContentsAccountSetup(Vector2 region)
    {
        CkGui.FontText("Account Generation", UiFontService.UidFont);

        ImGui.Text("You are not required to join the discord to login. Instead, it is generated for you below.");

        ImGui.Spacing();
        CkGui.IconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);
        ImUtf8.SameLineInner();
        CkGui.ColorTextWrapped("NOTE: An unclaimed account can't access profiles, chats, or radars until claimed via the bot.", ImGuiColors.DalamudYellow);

        CkGui.ColorTextFrameAligned("You can claim your account after a successful login in settings", ImGuiColors.DalamudGrey2);

        // Account Generation Area.
        ImGui.Spacing();
        ImGui.Separator();
        DrawNewAccountGeneration();
        // Account Recovery / Existing Account Setting here.

        ImGui.Spacing();
        ImGui.Separator();
        DrawExistingAccountRecovery();
    }

    private void DrawNewAccountGeneration()
    {
        var generateWidth = CkGui.IconTextButtonSize(FAI.IdCardAlt, "Create Account (One-Time Use!)");
        var recoveryKeyInUse = !string.IsNullOrWhiteSpace(_secretKey);

        CkGui.FontText("Generate New Account", UiFontService.Default150Percent);
        var blockButton = _account.HasValidProfile() || recoveryKeyInUse || _config.Current.ButtonUsed || UiService.DisableUI;

        CkGui.FramedIconText(FAI.UserPlus);
        CkGui.TextFrameAlignedInline("Generate:");
        ImGui.SameLine();
        if (CkGui.SmallIconTextButton(FAI.IdCardAlt, "Create Account (One-Time Use!)", disabled: blockButton))
            FetchAccountDetailsAsync();

        // Next line to display the account UID.
        var uid = string.Empty;
        var key = string.Empty;
        if (_account.GetMainProfile() is { } profile)
        {
            uid = profile.UserUID;
            key = profile.Key;
        }
        CkGui.FramedIconText(FAI.IdBadge);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("UID##AccountUID", "Generated Account UID..", ref uid, 10, ImGuiInputTextFlags.ReadOnly);

        // Next Line to display account key.
        CkGui.FramedIconText(FAI.Key);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("Key##AccountKey", "Generated Account Secret Key..", ref key, 64, ImGuiInputTextFlags.ReadOnly);
        CkGui.HelpText("SAVE THIS KEY SOMEWHERE SAFE!--NL--" +
            "--COL--THIS IS THE ONLY WAY TO RECOVER YOUR ACCOUNT IF YOU LOSE ACCESS TO IT!--COL--", ImGuiColors.DalamudRed, true);

        // if we have valid profile details but failed to connect, allow the user to attempt connection again.
        if (_account.HasValidProfile() && !MainHub.IsConnected && _config.Current.ButtonUsed)
        {
            CkGui.FramedIconText(FAI.SatelliteDish);
            CkGui.TextFrameAlignedInline("Attempt Reconnection with Account Login:");
            ImGui.SameLine();
            if (CkGui.SmallIconTextButton(FAI.Wifi, "Connect with Login", disabled: UiService.DisableUI))
                UiService.SetUITask(TryConnectForInitialization);
        }
    }

    private void DrawExistingAccountRecovery()
    {
        CkGui.FontText("Use Existing Account / Recover Account", UiFontService.Default150Percent);
        // Warning Notice.
        CkGui.FramedIconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);
        CkGui.ColorTextInline("To use an existing account / login with a recovered key from the discord bot, use it here and connect.", ImGuiColors.DalamudYellow);

        CkGui.FramedIconText(FAI.ShieldHeart);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##RefRecoveryKey", "Existing Account Key / Recovered Account Key..", ref _secretKey, 64);
        ImUtf8.SameLineInner();

        var blockButton = string.IsNullOrWhiteSpace(_secretKey) || _secretKey.Length != 64 || _account.HasValidProfile() || UiService.DisableUI;
        if (CkGui.IconTextButton(FAI.Wrench, "Login with Key", disabled: blockButton))
            TryLoginWithExistingKeyAsync();
        CkGui.AttachToolTip("--COL--THIS WILL CREATE YOUR PRIMARY ACCOUNT. ENSURE YOUR KEY IS CORRECT.--COL--", ImGuiColors.DalamudRed);
    }

    private async Task TryConnectForInitialization()
    {
        try
        {
            _logger.LogInformation("Attempting to connect to the server for the first time.");
            await _hub.Connect();
            _logger.LogInformation("Connection Attempt finished, marking account as created.");
            if (MainHub.IsConnected)
            {
                _furthestPage = IntroUiPage.Initialized;
                _guides.StartTutorial(TutorialType.MainUi);
            }
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"Failed to connect to the server for the first time: {ex}");
        }
    }

    private void TryLoginWithExistingKeyAsync()
    {
        UiService.SetUITask(async () =>
        {
            try
            {
                if (_account.HasValidProfile())
                    throw new InvalidOperationException("Cannot recover account when a valid profile already exists!");

                if (_account.GetMainProfile() is not { } profile)
                    throw new InvalidOperationException("No Main Account existed!");

                // Assign the secret key to the profile.
                if (!_account.TryUpdateSecretKey(profile, _secretKey))
                    throw new Bagagwa("Failed to update secret key for main profile, key may already exist.");

                _logger.LogInformation("Updated Secret Key Successfully.");
                await TryConnectForInitialization();
            }
            catch (Bagagwa ex)
            {
                _logger.LogError($"Failed to recover account for current character: {ex}");
            }
        });
    }

    private void FetchAccountDetailsAsync()
    {
        UiService.SetUITask(async () =>
        {
            _config.Current.ButtonUsed = true;
            _config.Save();
            try
            {
                // Begin by fetching the account details. If this fails we will throw to the catch statement and perform an early return.
                var accountDetails = await _hub.FetchFreshAccountDetails();

                // if we are still in the try statement by this point we have successfully retrieved our new account details.
                // This means that we can not create the new authentication and validate our account as created.
                _logger.LogInformation("Fetched Account Details, proceeding to create Primary Account authentication.");

                // create a new profile and populate add'l one-time details, duplicate profiles not permitted.
                if (_account.AddNewProfile() is { } newProfile)
                {
                    newProfile.UserUID = accountDetails.Item1;
                    newProfile.Key = accountDetails.Item2;
                    newProfile.IsPrimary = true;
                    newProfile.HadValidConnection = true;
                }
                else
                    throw new InvalidOperationException("New profile creation failed. This character already has a valid account.");
                
                // Save profile data changes.
                _account.Save();

                _logger.LogInformation("Profile for Login Auth set successfully.");
                // Log the details.
                _logger.LogInformation($"UID: [{accountDetails.Item1}]");
                _logger.LogInformation($"Secret Key: [{accountDetails.Item2}]");
                _logger.LogInformation("Fetched Account Details Successfully and finished creating Primary Account.");
            }
            catch (Bagagwa ex)
            {
                _logger.LogError(ex, "Failed to fetch account details and create the primary authentication. Performing early return.");
                _config.Current.ButtonUsed = false;
                _config.Save();
                return;
            }

            // Next, attempt an initialization connection test.
            try
            {
                await TryConnectForInitialization();
            }
            catch (Bagagwa ex)
            {
                _logger.LogError($"Failed to fetch account details and create the primary authentication. Performing early return: {ex}");
            }
        });
    }
}
