using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using ImGuiNET;
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
    private bool _readFirstPage = true;
    private Task? _fetchAccountDetailsTask;
    private string _aquiredUID = string.Empty;
    private string _secretKey = string.Empty;

    public IntroUi(ILogger<IntroUi> logger, GagspeakMediator mediator, MainHub mainHub,
        GagspeakConfigService configService, ServerConfigurationManager serverConfigs,
        ClientMonitorService clientService, UiSharedService uiShared)
        : base(logger, mediator, "Welcome to GagSpeak! ♥")
    {
        _apiHubMain = mainHub;
        _configService = configService;
        _serverConfigs = serverConfigs;
        _clientService = clientService;
        _uiShared = uiShared;

        IsOpen = false;
        ShowCloseButton = false;
        RespectCloseHotkey = false;
        AllowPinning = false;
        AllowClickthrough = false;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(600, 500),
            MaximumSize = new Vector2(600, 2000),
        };

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (_) => IsOpen = false);
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) => IsOpen = true);
    }

    protected override void PreDrawInternal() { }
    protected override void PostDrawInternal() { }

    protected override void DrawInternal()
    {
        // if the user has not accepted the agreement and they have not read the first page,
        // Then show the first page (everything in this if statement)
        if (!_configService.Current.AcknowledgementUnderstood && !_readFirstPage)
        {
            _uiShared.GagspeakBigText("Welcome to CK's GagSpeak Plugin!");
            ImGui.Separator();
            UiSharedService.ColorTextWrapped("We are currently nearing the end of closed beta, and as such during " +
                "our final testing period, new account creation has been closed off.", ImGuiColors.ParsedPink);

            // seperator before the next button is available
            ImGui.Separator();
            // add a button to switch to the account setup page
            /*            if (ImGui.Button("Setup Account##toAgreement"))
                            _readFirstPage = true;*/
        }
        // if they have read the first page but not yet created an account, we will need to present the account setup page for them.
        else if (!_configService.Current.AcknowledgementUnderstood && _readFirstPage)
        {
            // display the button to agree to the acknowledgement.
            if (ImGui.Button("I Understand.##toSetup"))
            {
                _configService.Current.AcknowledgementUnderstood = true;
                _configService.Save();
            }
        }
        // if the user has read the acknowledgements and the server is not alive, display the account creation window.
        else if (!MainHub.IsServerAlive || !_configService.Current.AccountCreated)
        {
            // title for this page of the intro UI
            _uiShared.GagspeakBigText("Account Registration / Creation");
            ImGui.Separator();

            UiSharedService.TextWrapped("Because this is a fresh install, you may generate one primary account key below. Once the key " +
                "has been generated, you may hit 'Login' to log into your account.");

            UiSharedService.TextWrapped("If you already have an account and are on a new computer or had to reinstall for any reason, " +
                "paste the generated secret key for your account in the text field below and hit 'Login'.");

            ImGui.Separator();

            // display the fields for generation and creation
            var oneTimeKeyGenButtonText = "One-Time Primary Key Generator";
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
            ImGui.AlignTextToFramePadding();
            // display the text field for the secret key
            ImGui.SetNextItemWidth(UiSharedService.GetWindowContentRegionWidth() - ImGui.GetWindowContentRegionMin().X);
            ImGui.TextUnformatted(text);
            ImGui.SetNextItemWidth(UiSharedService.GetWindowContentRegionWidth() - ImGui.GetWindowContentRegionMin().X - textSize.X);
            ImGui.InputTextWithHint("", text, ref _secretKey, 64);
            // if the key inserted is not 64 characters long, display a warning.
            if (_secretKey.Length > 0 && _secretKey.Length != 64)
            {
                UiSharedService.ColorTextWrapped("Your secret key must be exactly 64 characters long.", ImGuiColors.DalamudRed);
            }
            // if the key is 64 characters long, display the button to create the account.
            else if (_secretKey.Length == 64)
            {
                // display the create account button.
                if (ImGui.Button(buttonText))
                {
                    _logger.LogInformation("Recreating Authentication for current character.");
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
                    _ = Task.Run(() => _apiHubMain.Connect());
                    _secretKey = string.Empty;
                    _configService.Current.AccountCreated = true; // set the account created flag to true
                    _configService.Save(); // save the configuration
                }
                UiSharedService.AttachToolTip("THIS WILL CREATE YOUR PRIMARY ACCOUNT. ENSURE YOUR KEY IS CORRECT.");
            }

            ImGui.Separator();
            // display the secret key and UID
            ImGui.InputText("Secret Key", ref _secretKey, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.InputText("UID", ref _aquiredUID, 64, ImGuiInputTextFlags.ReadOnly);
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
