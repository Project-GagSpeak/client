using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CkCommons.Gui;

/// <summary> The introduction UI that will be shown the first time that the user starts the plugin. </summary>
public class IntroUi : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly MainConfig _configService;
    private readonly ServerConfigManager _serverConfigs;
    private readonly TutorialService _guides;

    private bool ThemePushed = false;
    private bool _readFirstPage = false; // mark as false so nobody sneaks into official release early.
    private Task? _fetchAccountDetailsTask = null;
    private Task? _initialAccountCreationTask = null;
    private string _secretKey = string.Empty;

    public IntroUi(ILogger<IntroUi> logger, GagspeakMediator mediator, MainHub mainHub,
        MainConfig config, ServerConfigManager serverConfigs, TutorialService guides)
        : base(logger, mediator, "Welcome to GagSpeak! ♥")
    {
        _hub = mainHub;
        _configService = config;
        _serverConfigs = serverConfigs;
        _guides = guides;

        IsOpen = false;
        ShowCloseButton = false;
        RespectCloseHotkey = false;
        AllowPinning = false;
        AllowClickthrough = false;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(600, 500),
            MaximumSize = new Vector2(600, 1000),
        };
        Flags = WFlags.NoScrollbar;

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (_) => IsOpen = false);
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) => IsOpen = true);
    }

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        // 

        // if the user has not accepted the agreement and they have not read the first page,
        // Then show the first page (everything in this if statement)
        if (!_configService.Current.AcknowledgementUnderstood && !_readFirstPage)
        {
            DrawWelcomePage();
        }
        // if they have read the first page but not yet created an account, we will need to present the account setup page for them.
        else if (!_configService.Current.AcknowledgementUnderstood && _readFirstPage)
        {
            DrawAcknowledgement();
        }
        // if the user has read the acknowledgements and the server is not alive, display the account creation window.
        else if (!MainHub.IsServerAlive || !_serverConfigs.HasValidConfig())
        {
            DrawAccountSetup();
        }
        // otherwise, if the server is alive, meaning we are validated, then boot up the main UI.
        else
        {
            _logger.LogDebug("Switching to main UI");
            // call the main UI event via the mediator
            Mediator.Publish(new SwitchToMainUiMessage());
            // toggle this intro UI window off.
            IsOpen = false;
        }
    }

    private void DrawWelcomePage()
    {
        CkGui.GagspeakTitleText("Welcome to Project GagSpeak!");

        ImGui.Separator();
        ImGui.TextWrapped("Project GagSpeak is a highly ambitious project that has been devloped over the course of a year in closed Beta, " +
            "aiming to provide kinksters with an all-in-one BDSM plugin free of charge to enjoy.");
        ImGui.Spacing();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink)) CkGui.GagspeakBigText("The Plugin Contains a variety of Modules, such as:");
        // if the title text is pressed, proceed.
        if (ImGui.IsItemClicked()) _readFirstPage = true;

        CkGui.ColorText("- KinkPlates™", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Customizable GagSpeak AdventurePlate to present yourself and/or predicament!");

        CkGui.ColorText("- Puppeteer", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" A PuppetMaster Variant with Alias Lists, Per-Player triggers, and more!");

        CkGui.ColorText("- Triggers & Patterns", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Improved FFXIV-VibePlugin Variant with *all* functionalities");

        CkGui.ColorText("- Alarms", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Behaves just like Lovense Alarms");

        CkGui.ColorText("- GagSpeak Vibe Remotes", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Mimiced Replica of Lovense Remote with Keybinds");

        CkGui.ColorText("- Realistic Gag Garble Speech", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Can account for up to 3 gags!");

        CkGui.ColorText("- Restriction Sets", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Lockable Glamour's on your Character.");

        CkGui.ColorText("- Cursed Loot", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Brings Bondage Mimic Chests to FFXIV duties!");

        CkGui.ColorText("- Hardcore Control", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Maximize Immersion and Helplessness with others (at your own risk!)");

        ImGui.Spacing();
        ImGui.TextWrapped("Clicking The large pink text above will advance you to the acknowledgements page. " +
            "I Hope you enjoy all the features that this plugin has to offer, and have a great time using it. ♥");
    }

    private void DrawAcknowledgement()
    {
        using (UiFontService.GagspeakTitleFont.Push())
        {
            ImGuiUtil.Center("Acknowledgement Of Usage & Privacy");
        }
        ImGui.Separator();
        using (UiFontService.UidFont.Push())
        {
            CkGui.ColorTextCentered("YOU WILL ONLY SEE THIS PAGE ONCE.", ImGuiColors.DalamudRed);
            CkGui.ColorTextCentered("PLEASE READ CAREFULLY BEFORE PROCEEDING.", ImGuiColors.DalamudRed);
        }
        ImGui.Separator();

        ImGui.TextWrapped("Being a Server-Side Plugin, and a plugin full of kinky individuals, we all know there will always be some of *those* people " +
            "who will try to ruin the fun for everyone.");
        ImGui.Spacing();
        CkGui.ColorTextWrapped("As Such, by joining GagSpeak, you must acknowledge & accept the following:", ImGuiColors.DalamudRed);

        using var borderColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudRed);
        using var scrollbarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);

        using (ImRaii.Child("AgreementWindow", new Vector2(ImGui.GetContentRegionAvail().X, 300f), true))
        {
            ImGui.Spacing();
            CkGui.ColorText("Consent:", ImGuiColors.ParsedGold);
            ImGui.TextWrapped("BDSM, at its foundation, highly values the aspest of consent. By using GagSpeak, you understand that you must abide by boundaries " +
                "others set for you, and the limits that they define. If you push these limits against their will or pressure them to give you more than they are comfortable with, it will not be tolerated.");
            ImGui.Spacing();
            ImGui.Spacing();


            CkGui.ColorText("Privacy:", ImGuiColors.ParsedGold);
            ImGui.TextWrapped("You Acknowledge that when using GagSpeak, your personal information such as Character Name and Homeworld are censored and replaced with " +
                "an anonymous identity. If you give up this information about you and it is used against you, that is of your own fault. Be responsible about how you go about meeting up with others.");
            ImGui.Spacing();
            ImGui.Spacing();

            CkGui.ColorText("Hardcore Control:", ImGuiColors.ParsedGold);
            ImGui.TextWrapped("Hardcore Functionality in GagSpeak directly affects your game at a core level, such as preventing you from typing, blocking your sight, restricting you from movement, " +
                "forcing you to perform emotes, blocking out certain actions from being used, and controling the GCD's of your actions.");
            ImGui.Spacing();
            ImGui.TextWrapped("While these are all safe to use, be careful who you give these permissions access to. I have granted you with a great degree of control towards others, you are expected to " +
                "take care of these individuals if granted access over them, and to not be wreckless.");
            ImGui.Spacing();
            ImGui.Spacing();

            CkGui.ColorText("Predatory Behavior:", ImGuiColors.DalamudRed);
            ImGui.TextWrapped("The Main Dev of GagSpeak has endured years of manipulative predatory abuse, and as such with firsthand experience, is familiar with what kinds of reports and behaviors to " +
                "identify as 'bait reports' or 'actual reports'. Reports are handled very carefully by our team, and taken very seriously.");
            ImGui.Spacing();
            ImGui.TextWrapped("If you notice any such behavior occuring, report it in detail. We have intentionally designed our flagged Kinksters to remain unaffected on their s end, so they wont be " +
                "able to deduce who reported them until it is too late.");
            ImGui.Spacing();
        }

        ImGui.Spacing();
        CkGui.ColorTextCentered("Click this Button below once you have read and understood the above.", ImGuiColors.DalamudRed);
        if(ImGui.Button("Proceed To Account Creation.", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeightWithSpacing())))
        {
            _configService.Current.AcknowledgementUnderstood = true;
            _configService.Save();
        }
        ImGui.Spacing();
    }

    private void DrawAccountSetup()
    {
        using (UiFontService.GagspeakTitleFont.Push())
        {
            ImGuiUtil.Center("Primary Account Creation");
        }
        ImGui.Separator();
        ImGui.Spacing();

        CkGui.ColorText("Generating your Primary Account", ImGuiColors.ParsedGold);
        ImGui.TextWrapped("You can ONLY PRESS THE BUTTON BELOW ONCE.");
        CkGui.ColorTextWrapped("The Primary Account IS LINKED TO YOUR CURRENTLY LOGGED IN CHARACTER.", ImGuiColors.DalamudRed);
        ImGui.TextWrapped("If you wish have your primary account on another character, log into them first!");
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        CkGui.ColorText("Generate Primary Account: ", ImGuiColors.ParsedGold);

        // Under the condition that we are not recovering an account, display the primary account generator:
        if (_secretKey.IsNullOrWhitespace())
        {
            // generate a secret key for the user and attempt initial connection when pressed.
            if (CkGui.IconTextButton(FAI.UserPlus, "Primary Account Generator (One-Time Use!)", disabled: _configService.Current.ButtonUsed))
            {
                _configService.Current.ButtonUsed = true;
                _configService.Save();
                _fetchAccountDetailsTask = FetchAccountDetailsAsync();
            }
            // while we are awaiting to fetch the details and connect display a please wait text.
            if (_fetchAccountDetailsTask != null && !_fetchAccountDetailsTask.IsCompleted)
            {
                CkGui.ColorTextWrapped("Fetching details, please wait...", ImGuiColors.DalamudYellow);
            }
        }

        // here we will draw out a seperator line.
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Below this we will provide the user with a space to insert an existing UID & Key to
        // log back into a account they already have if they needed to reset for any reason.
        CkGui.GagspeakBigText("Does your Character already have a Primary Account?");
        CkGui.ColorText("Retreive the key from where you saved it, or the discord bot, and insert it below.", ImGuiColors.ParsedGold);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * .85f);
        ImGui.InputText("Key##RefNewKey", ref _secretKey, 64);

        ImGui.Spacing();
        CkGui.ColorText("ServerState (For Debug Purposes): " + MainHub.ServerStatus, ImGuiColors.DalamudGrey);
        CkGui.ColorText("Auth Exists for character (Debug): " + _serverConfigs.AuthExistsForCurrentLocalContentId(), ImGuiColors.DalamudGrey);
        if(_secretKey.Length == 64)
        {
            CkGui.ColorText("Connect with existing Key?", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            if (CkGui.IconTextButton(FAI.Signal, "Yes! Log me in!", disabled: _initialAccountCreationTask is not null))
            {
                _logger.LogInformation("Creating Authentication for current character.");
                try
                {
                    if (_serverConfigs.AuthExistsForCurrentLocalContentId())
                    {
                        throw new InvalidOperationException("Auth already exists for current character, cannot create new Primary auth if one already exists!");
                    }

                    // if the auth does not exist for the current character, we can create a new one.
                    _serverConfigs.GenerateAuthForCurrentCharacter();

                    // set the key to that newly added authentication
                    SecretKey newKey = new()
                    {
                        Label = $"GagSpeak Main Account Secret Key - ({DateTime.Now:yyyy-MM-dd})",
                        Key = _secretKey,
                    };

                    // set the secret key for the character
                    _serverConfigs.SetSecretKeyForCharacter(PlayerData.ContentId, newKey);

                    // run the create connections and set our account created to true
                    _initialAccountCreationTask = PerformFirstLoginAsync();



                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create authentication for current character.");
                }
            }
            CkGui.AttachToolTip("THIS WILL CREATE YOUR PRIMARY ACCOUNT. ENSURE YOUR KEY IS CORRECT.");
        }

        if (_initialAccountCreationTask is not null && !_initialAccountCreationTask.IsCompleted)
        {
            CkGui.ColorTextWrapped("Attempting to connect for First Login, please wait...", ImGuiColors.DalamudYellow);
        }
    }

    private async Task PerformFirstLoginAsync()
    {
        try
        {
            _logger.LogInformation("Attempting to connect to the server for the first time.");
            await _hub.Connect();
            _logger.LogInformation("Connection Attempt finished, marking account as created.");
            if (MainHub.IsConnected)
            {
                _guides.StartTutorial(TutorialType.MainUi);
            }
            _configService.Save(); // save the configuration
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to the server for the first time.");
            _configService.Save();
        }
        finally
        {
            _initialAccountCreationTask = null;
        }
    }

    private async Task FetchAccountDetailsAsync()
    {
        try
        {
            // Begin by fetching the account details for the player. If this fails we will throw to the catch statement and perform an early return.
            var accountDetails = await _hub.FetchFreshAccountDetails();

            // if we are still in the try statement by this point we have successfully retrieved our new account details.
            // This means that we can not create the new authentication and validate our account as created.

            // However, if an auth already exists for the current content ID, and we are trying to create a new primary account, this should not be possible, so early throw.
            if (_serverConfigs.AuthExistsForCurrentLocalContentId())
            {
                throw new InvalidOperationException("Auth already exists for current character, cannot create new Primary auth if one already exists!");
            }

            // if the auth does not exist for the current character, we can create a new one.
            _serverConfigs.GenerateAuthForCurrentCharacter();

            // set the key to that newly added authentication
            SecretKey newKey = new()
            {
                Label = $"GagSpeak Main Account Secret Key - ({DateTime.Now:yyyy-MM-dd})",
                Key = accountDetails.Item2,
            };

            // set the secret key for the character
            _serverConfigs.SetSecretKeyForCharacter(PlayerData.ContentId, newKey);
            _configService.Save();
            // Log the details.
            _logger.LogInformation("UID: " + accountDetails.Item1);
            _logger.LogInformation("Secret Key: " + accountDetails.Item2);
            _logger.LogInformation("Fetched Account Details Successfully and finished creating Primary Account.");

        }
        catch (Exception)
        {
            // Log the error
            _logger.LogError("Failed to fetch account details and create the primary authentication. Performing early return.");
            _configService.Current.ButtonUsed = false;
            _configService.Save();

            // set the task back to null and return.
            _fetchAccountDetailsTask = null;
            return;
        }

        // Next step is to attempt a initial connection to the server with this now primary authentication.
        // If it suceeds then it will mark the initialConnectionSuccessful flag to true (This is done in the connection function itself)
        try
        {
            _logger.LogInformation("Attempting to connect to the server for the first time.");
            await _hub.Connect();
            _logger.LogInformation("Connection Attempt finished.");

            if (MainHub.IsConnected) _guides.StartTutorial(TutorialType.MainUi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to the server for the first time.");
        }
        finally
        {
            _fetchAccountDetailsTask = null;
        }
    }
}
