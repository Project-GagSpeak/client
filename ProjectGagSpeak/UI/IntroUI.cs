using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OtterGui;
using System.Numerics;

namespace GagSpeak.UI;

/// <summary> The introduction UI that will be shown the first time that the user starts the plugin. </summary>
public class IntroUi : WindowMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly GagspeakConfigService _configService;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly ClientMonitorService _clientService;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;
    private bool ThemePushed = false;
    private bool _readFirstPage = false; // mark as false so nobody sneaks into official release early.
    private Task? _fetchAccountDetailsTask = null;
    private Task? _initialAccountCreationTask = null;
    private string _aquiredUID = string.Empty;
    private string _secretKey = string.Empty;

    public IntroUi(ILogger<IntroUi> logger, GagspeakMediator mediator, MainHub mainHub,
        GagspeakConfigService configService, ServerConfigurationManager serverConfigs,
        ClientMonitorService clientService, UiSharedService uiShared, TutorialService guides)
        : base(logger, mediator, "Welcome to GagSpeak! ♥")
    {
        _apiHubMain = mainHub;
        _configService = configService;
        _serverConfigs = serverConfigs;
        _clientService = clientService;
        _uiShared = uiShared;
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
        Flags = ImGuiWindowFlags.NoScrollbar;

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
        else if (!MainHub.IsServerAlive || !_configService.Current.AccountCreated)
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
        _uiShared.GagspeakTitleText("Welcome to Project GagSpeak!");

        ImGui.Separator();
        ImGui.TextWrapped("Project GagSpeak is a highly ambitious project that has been devloped over the course of a year in closed Beta, " +
            "aiming to provide kinksters with an all-in-one BDSM plugin free of charge to enjoy.");
        ImGui.Spacing();
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedPink)) _uiShared.GagspeakBigText("The Plugin Contains a variety of Modules, such as:");
        // if the title text is pressed, proceed.
        if (ImGui.IsItemClicked()) _readFirstPage = true;

        UiSharedService.ColorText("- KinkPlates™", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Customizable GagSpeak AdventurePlate to present yourself and/or predicament!");

        UiSharedService.ColorText("- Puppeteer", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" A PuppetMaster Variant with Alias Lists, Per-Player triggers, and more!");

        UiSharedService.ColorText("- Triggers & Patterns", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Improved FFXIV-VibePlugin Variant with *all* functionalities");

        UiSharedService.ColorText("- Alarms", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Behaves just like Lovense Alarms");

        UiSharedService.ColorText("- GagSpeak Vibe Remotes", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Mimiced Replica of Lovense Remote with Keybinds");

        UiSharedService.ColorText("- Realistic Gag Garble Speech", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Can account for up to 3 gags!");

        UiSharedService.ColorText("- Restraint Sets", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Lockable Glamour's on your Character.");

        UiSharedService.ColorText("- Cursed Loot", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Brings Bondage Mimic Chests to FFXIV duties!");

        UiSharedService.ColorText("- Hardcore Control", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        ImGui.Text(" Maximize Immersion and Helplessness with others (at your own risk!)");

        ImGui.Spacing();
        ImGui.TextWrapped("Clicking The large pink text above will advance you to the acknowledgements page. " +
            "I Hope you enjoy all the features that this plugin has to offer, and have a great time using it. ♥");
    }

    private void DrawAcknowledgement()
    {
        using (_uiShared.GagspeakTitleFont.Push())
        {
            ImGuiUtil.Center("Acknowledgement Of Usage & Privacy");
        }
        ImGui.Separator();
        using (_uiShared.UidFont.Push())
        {
            UiSharedService.ColorTextCentered("YOU WILL ONLY SEE THIS PAGE ONCE.", ImGuiColors.DalamudRed);
            UiSharedService.ColorTextCentered("PLEASE READ CAREFULLY BEFORE PROCEEDING.", ImGuiColors.DalamudRed);
        }
        ImGui.Separator();

        ImGui.TextWrapped("Being a Server-Side Plugin, and a plugin full of kinky individuals, we all know there will always be some of *those* people " +
            "who will try to ruin the fun for everyone.");
        ImGui.Spacing();
        UiSharedService.ColorTextWrapped("As Such, by joining GagSpeak, you must acknowledge & accept the following:", ImGuiColors.DalamudRed);

        using var borderColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudRed);
        using var scrollbarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);

        using (ImRaii.Child("AgreementWindow", new Vector2(ImGui.GetContentRegionAvail().X, 300f), true))
        {
            ImGui.Spacing();
            UiSharedService.ColorText("Consent:", ImGuiColors.ParsedGold);
            ImGui.TextWrapped("BDSM, at its foundation, highly values the aspest of consent. By using GagSpeak, you understand that you must abide by boundaries " +
                "others set for you, and the limits that they define. If you push these limits against their will or pressure them to give you more than they are comfortable with, it will not be tolerated.");
            ImGui.Spacing();
            ImGui.Spacing();


            UiSharedService.ColorText("Privacy:", ImGuiColors.ParsedGold);
            ImGui.TextWrapped("You Acknowledge that when using GagSpeak, your personal information such as Character Name and Homeworld are censored and replaced with " +
                "an anonymous identity. If you give up this information about you and it is used against you, that is of your own fault. Be responsible about how you go about meeting up with others.");
            ImGui.Spacing();
            ImGui.Spacing();

            UiSharedService.ColorText("Hardcore Control:", ImGuiColors.ParsedGold);
            ImGui.TextWrapped("Hardcore Functionality in GagSpeak directly affects your game at a core level, such as preventing you from typing, blocking your sight, restricting you from movement, " +
                "forcing you to perform emotes, blocking out certain actions from being used, and controling the GCD's of your actions.");
            ImGui.Spacing();
            ImGui.TextWrapped("While these are all safe to use, be careful who you give these permissions access to. I have granted you with a great degree of control towards others, you are expected to " +
                "take care of these individuals if granted access over them, and to not be wreckless.");
            ImGui.Spacing();
            ImGui.Spacing();

            UiSharedService.ColorText("Predatory Behavior:", ImGuiColors.DalamudRed);
            ImGui.TextWrapped("The Main Dev of GagSpeak has endured years of manipulative predatory abuse, and as such with firsthand experience, is familiar with what kinds of reports and behaviors to " +
                "identify as 'bait reports' or 'actual reports'. Reports are handled very carefully by our team, and taken very seriously.");
            ImGui.Spacing();
            ImGui.TextWrapped("If you notice any such behavior occuring, report it in detail. We have intentionally designed our flagged Kinksters to remain unaffected on their s end, so they wont be " +
                "able to deduce who reported them until it is too late.");
            ImGui.Spacing();
        }

        ImGui.Spacing();
        UiSharedService.ColorTextCentered("Click this Button below once you have read and understood the above.", ImGuiColors.DalamudRed);
        if(ImGui.Button("Proceed To Account Creation.", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeightWithSpacing())))
        {
            _configService.Current.AcknowledgementUnderstood = true;
            _configService.Save();
        }
        ImGui.Spacing();
    }

    private void DrawAccountSetup()
    {
        using (_uiShared.GagspeakTitleFont.Push())
        {
            ImGuiUtil.Center("Primary Account Creation");
        }
        ImGui.Separator();
        ImGui.Spacing();

        UiSharedService.ColorText("Generating your Primary Account", ImGuiColors.ParsedGold);
        ImGui.TextWrapped("You can ONLY PRESS THE BUTTON BELOW ONCE.");
        UiSharedService.ColorTextWrapped("The Primary Account IS LINKED TO YOUR CURRENTLY LOGGED IN CHARACTER.", ImGuiColors.DalamudRed);
        ImGui.TextWrapped("If you wish have your primary account on another character, log into them first!");
        ImGui.Spacing();
        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText("Generate Primary Account: ", ImGuiColors.ParsedGold);
        // display the fields for generation and creation
        var oneTimeKeyGenButtonText = "Primary Account Generator (One-Time Use!)";
        if (_uiShared.IconTextButton(FontAwesomeIcon.UserPlus, oneTimeKeyGenButtonText, disabled: _configService.Current.ButtonUsed))
        {
            // toggle the account created flag to true
            _configService.Current.ButtonUsed = true;
            _configService.Save();
            // generate a secret key for the user.
            _fetchAccountDetailsTask = FetchAccountDetailsAsync();
        }

        if (_fetchAccountDetailsTask != null && !_fetchAccountDetailsTask.IsCompleted)
        {
            UiSharedService.ColorTextWrapped("Fetching details, please wait...", ImGuiColors.DalamudYellow);
        }

        // next place the text field for inserting the key, and then the button for creating the account.
        var text = "Account Secret Key: ";
        var buttonText = "Create / Sign into your Account with Secret Key";
        var buttonWidth = _secretKey.Length != 64 ? 0 : ImGuiHelpers.GetButtonSize(buttonText).X + ImGui.GetStyle().ItemSpacing.X;
        var textSize = ImGui.CalcTextSize(text);

        // if the primary account exists but does not have successful connection, load that into the information.
        if (_serverConfigs.TryGetPrimaryAuth(out Authentication primaryAuth))
        {
            if(primaryAuth.SecretKey.HasHadSuccessfulConnection is false)
            {
                _aquiredUID = "RECOVERED-FROM-FAILED-CONNECTION";
                _secretKey = primaryAuth.SecretKey.Key;
            }
            
        }


        // if we dont have the account details yet, early return.
        if (_aquiredUID == string.Empty || _secretKey == string.Empty)
            return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        // otherwise, display them.
        UiSharedService.ColorText("Primary Account UID: ", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Click the UID text to copy it to your Clipboard!");
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * .85f);
        ImGui.InputText("##RefNewUID", ref _aquiredUID, 64, ImGuiInputTextFlags.ReadOnly);
        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(_aquiredUID);
            _logger.LogInformation("Copied UID to Clipboard.");
        }
        ImGui.Spacing();
        UiSharedService.ColorText("Primary Account Key: ", ImGuiColors.ParsedGold);
        _uiShared.DrawHelpText("Click the Secret Key text to copy it to your Clipboard! I Recommend saving it somewhere!");
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * .85f);
        ImGui.InputText("##RefNewKey", ref _secretKey, 64, ImGuiInputTextFlags.ReadOnly);
        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(_secretKey);
            _logger.LogInformation("Copied Secret Key to Clipboard.");
        }
        ImGui.NewLine();

        UiSharedService.ColorText("ServerState (For Debug Purposes): " + MainHub.ServerStatus, ImGuiColors.DalamudGrey);
        UiSharedService.ColorText("Join as a new Kinkster?", ImGuiColors.ParsedGold);
        ImGui.SameLine();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Signal, "Yes! Log me in!", disabled: _initialAccountCreationTask is not null))
        {
            _logger.LogInformation("Creating Authentication for current character.");
            if (!_serverConfigs.AuthExistsForCurrentLocalContentId())
            {
                _logger.LogDebug("Character has no secret key, generating new auth for current character", LoggerType.ApiCore);
                _serverConfigs.GenerateAuthForCurrentCharacter();
            }
            // set the key to that newly added authentication
            SecretKey newKey = new()
            {
                Label = $"GagSpeak Main Account Secret Key - ({DateTime.Now:yyyy-MM-dd})",
                Key = _secretKey,
            };
            // set the secret key for the character
            _serverConfigs.SetSecretKeyForCharacter(_clientService.ContentId, newKey);

            // run the create connections and set our account created to true
            _initialAccountCreationTask = PerformFirstLoginAsync();

            if (_initialAccountCreationTask is not null && !_initialAccountCreationTask.IsCompleted)
            {
                UiSharedService.ColorTextWrapped("Attempting to connect for First Login, please wait...", ImGuiColors.DalamudYellow);
            }
        }
        UiSharedService.AttachToolTip("THIS WILL CREATE YOUR PRIMARY ACCOUNT. ENSURE YOUR KEY IS CORRECT.");
    }

    private async Task PerformFirstLoginAsync()
    {
        try
        {
            _configService.Current.AccountCreated = true; // set the account created flag to true
            _logger.LogInformation("Attempting to connect to the server for the first time.");
            await _apiHubMain.Connect();
            _logger.LogInformation("Connection Attempt finished, marking account as created.");
            if (MainHub.IsConnected)
            {
                _guides.StartTutorial(TutorialType.MainUi);
                _secretKey = string.Empty;
            }
            _configService.Save(); // save the configuration
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to the server for the first time.");
            _configService.Current.AccountCreated = false;
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
            var accountDetails = await _apiHubMain.FetchFreshAccountDetails();
            _aquiredUID = accountDetails.Item1;
            _secretKey = accountDetails.Item2;
            _fetchAccountDetailsTask = null;
        }
        catch (Exception)
        {
            // Log the error
            _logger.LogError("Failed to fetch account details Server is likely down. Resetting Button.");
            // Reset the button used flag
            _configService.Current.ButtonUsed = false;
            _configService.Save();
        }
    }
}
