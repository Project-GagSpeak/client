using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI;

public class AccountsTab
{
    private readonly ILogger<AccountsTab> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly GagspeakConfigService _mainConfig;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly ConfigFileProvider _configFiles;
    private readonly ClientMonitor _clientMonitor;
    private readonly UiSharedService _uiShared;

    private bool DeleteAccountConfirmation = false;
    private int ShowKeyIdx = -1;
    private int EditingIdx = -1;
    public AccountsTab(ILogger<AccountsTab> logger, GagspeakMediator mediator, MainHub hub,
        GagspeakConfigService mainConfig, ServerConfigurationManager serverConfigs,
        ConfigFileProvider configDirectory, ClientMonitor clientMonitor, UiSharedService uiShared)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _mainConfig = mainConfig;
        _serverConfigs = serverConfigs;
        _configFiles = configDirectory;
        _clientMonitor = clientMonitor;
        _uiShared = uiShared;

        _configFiles = configDirectory;
    }

    public void DrawManager()
    {
        _uiShared.GagspeakBigText(GSLoc.Settings.Accounts.PrimaryLabel);
        var localContentId = _clientMonitor.ContentId;

        // obtain the primary account auth.
        var primaryAuth = _serverConfigs.ServerStorage.Authentications.FirstOrDefault(c => c.IsPrimary);
        if (primaryAuth is null)
        {
            UiSharedService.ColorText("No primary account setup to display", ImGuiColors.DPSRed);
            return;
        }

        // Draw out the primary account.
        DrawAccount(int.MaxValue, primaryAuth, primaryAuth.CharacterPlayerContentId == localContentId);

        // display title for account management
        _uiShared.GagspeakBigText(GSLoc.Settings.Accounts.SecondaryLabel);
        if (_serverConfigs.HasAnyAltAuths())
        {
            // order the list of alts by prioritizing ones with successful connections first.
            var secondaryAuths = _serverConfigs.ServerStorage.Authentications
                .Where(c => !c.IsPrimary)
                .OrderByDescending(c => c.SecretKey.HasHadSuccessfulConnection)
                .ToList();

            for (var i = 0; i < secondaryAuths.Count; i++)
                DrawAccount(i, secondaryAuths[i], secondaryAuths[i].CharacterPlayerContentId == localContentId);

            return;
        }
        // display this if we have no alts.
        UiSharedService.ColorText(GSLoc.Settings.Accounts.NoSecondaries, ImGuiColors.DPSRed);
    }

    private void DrawAccount(int idx, Authentication account, bool isOnlineUser = false)
    {
        bool isPrimary = account.IsPrimary;
        // push rounding window corners
        using var windowRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f);
        // push a pink border color for the window border.
        using var borderColor = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        // create the child window.

        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        using var child = ImRaii.Child($"##AuthAccountListing" + idx + account.CharacterPlayerContentId, new Vector2(ImGui.GetContentRegionAvail().X, height), true, ImGuiWindowFlags.ChildWindow);
        if (!child) return;

        using (var group = ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            _uiShared.IconText(FontAwesomeIcon.UserCircle);
            ImUtf8.SameLineInner();
            UiSharedService.ColorText(account.CharacterName, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink);
            UiSharedService.AttachToolTip(GSLoc.Settings.Accounts.CharaNameLabel);

            // head over to the end to make the delete button.
            var cannotDelete = (!(KeyMonitor.CtrlPressed() && KeyMonitor.ShiftPressed()) || !(MainHub.IsServerAlive && MainHub.IsConnected && isOnlineUser));
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Trash, GSLoc.Settings.Accounts.DeleteButtonLabel));

            var hadEstablishedConnection = account.SecretKey.HasHadSuccessfulConnection;

            if (_uiShared.IconTextButton(FontAwesomeIcon.Trash, "Delete Account", isInPopup: true, disabled: !hadEstablishedConnection || cannotDelete, id: "DeleteAccount" + account.CharacterPlayerContentId))
            {
                DeleteAccountConfirmation = true;
                ImGui.OpenPopup("Delete your account?");
            }
            UiSharedService.AttachToolTip("THIS BUTTON CAN BE A BIT BUGGY AND MAY REMOVE YOUR PRIMARY WITHOUT NOTICE ON ACCIDENT. LOOKING INTO WHY IN 1.1.1.0\n" +
                (!hadEstablishedConnection
                ? GSLoc.Settings.Accounts.DeleteButtonDisabledTT : isPrimary
                    ? GSLoc.Settings.Accounts.DeleteButtonTT + GSLoc.Settings.Accounts.DeleteButtonPrimaryTT
                    : GSLoc.Settings.Accounts.DeleteButtonTT, color: ImGuiColors.DalamudRed));

        }
        // next line:
        using (var group2 = ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            _uiShared.IconText(FontAwesomeIcon.Globe);
            ImUtf8.SameLineInner();
            UiSharedService.ColorText(OnFrameworkService.WorldData.Value[(ushort)account.WorldId], isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink);
            UiSharedService.AttachToolTip(GSLoc.Settings.Accounts.CharaWorldLabel);

            var isOnUserSize = _uiShared.GetIconData(FontAwesomeIcon.Fingerprint);
            var successfulConnection = _uiShared.GetIconData(FontAwesomeIcon.PlugCircleCheck);
            var rightEnd = ImGui.GetContentRegionAvail().X - successfulConnection.X - isOnUserSize.X - 2 * ImGui.GetStyle().ItemInnerSpacing.X;
            ImGui.SameLine(rightEnd);

            _uiShared.BooleanToColoredIcon(isOnlineUser, false, FontAwesomeIcon.Fingerprint, FontAwesomeIcon.Fingerprint, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink, ImGuiColors.DalamudGrey3);
            UiSharedService.AttachToolTip(account.IsPrimary ? GSLoc.Settings.Accounts.FingerprintPrimary : GSLoc.Settings.Accounts.FingerprintSecondary);
            _uiShared.BooleanToColoredIcon(account.SecretKey.HasHadSuccessfulConnection, true, FontAwesomeIcon.PlugCircleCheck, FontAwesomeIcon.PlugCircleXmark, ImGuiColors.ParsedGreen, ImGuiColors.DalamudGrey3);
            UiSharedService.AttachToolTip(account.SecretKey.HasHadSuccessfulConnection ? GSLoc.Settings.Accounts.SuccessfulConnection : GSLoc.Settings.Accounts.NoSuccessfulConnection);
        }

        // next line:
        using (var group3 = ImRaii.Group())
        {
            string keyDisplayText = (ShowKeyIdx == idx) ? account.SecretKey.Key : account.SecretKey.Label;
            ImGui.AlignTextToFramePadding();
            _uiShared.IconText(FontAwesomeIcon.Key);
            if (ImGui.IsItemClicked())
            {
                ShowKeyIdx = (ShowKeyIdx == idx) ? -1 : idx;
            }
            UiSharedService.AttachToolTip(GSLoc.Settings.Accounts.CharaKeyLabel);
            // we shoul draw an inputtext field here if we can edit it, and a text field if we cant.
            if (EditingIdx == idx)
            {
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - _uiShared.GetIconButtonSize(FontAwesomeIcon.PenSquare).X - ImGui.GetStyle().ItemSpacing.X);
                string key = account.SecretKey.Key;
                if (ImGui.InputTextWithHint("##SecondaryAuthKey" + account.CharacterPlayerContentId, "Paste Secret Key Here...", ref key, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    key = key.Trim(); // Trim any leading or trailing whitespace

                    // Check if the key exists in any of the authentications
                    var keyExists = _serverConfigs.ServerStorage.Authentications
                        .Any(auth => string.Equals(auth.SecretKey.Key, key, StringComparison.OrdinalIgnoreCase));

                    if (keyExists)
                    {
                        _logger.LogWarning("Key " + key + " already exists in another account. Setting to blank.");
                        account.SecretKey.Label = string.Empty;
                        account.SecretKey.Key = string.Empty;
                    }
                    else
                    {
                        if (account.SecretKey.Label.IsNullOrEmpty())
                            account.SecretKey.Label = "Alt Character Key for " + account.CharacterName + " on " + OnFrameworkService.WorldData.Value[(ushort)account.WorldId];
                        account.SecretKey.Key = key;
                    }

                    EditingIdx = -1;
                    _serverConfigs.Save();
                }
            }
            else
            {
                ImUtf8.SameLineInner();
                UiSharedService.ColorText(keyDisplayText, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink);
                if (ImGui.IsItemClicked()) ImGui.SetClipboardText(account.SecretKey.Key);
                UiSharedService.AttachToolTip(GSLoc.Settings.Accounts.CopyKeyToClipboard);
            }

            if (idx != int.MaxValue)
            {
                var insertKey = _uiShared.GetIconData(FontAwesomeIcon.PenSquare);
                var rightEnd = ImGui.GetContentRegionAvail().X - insertKey.X;
                ImGui.SameLine(rightEnd);
                var col = account.SecretKey.HasHadSuccessfulConnection ? ImGuiColors.DalamudRed : ImGuiColors.DalamudGrey3;
                _uiShared.BooleanToColoredIcon(EditingIdx == idx, false, FontAwesomeIcon.PenSquare, FontAwesomeIcon.PenSquare, ImGuiColors.ParsedPink, col);
                if (ImGui.IsItemClicked() && !account.SecretKey.HasHadSuccessfulConnection)
                    EditingIdx = EditingIdx == idx ? -1 : idx;
                UiSharedService.AttachToolTip(account.SecretKey.HasHadSuccessfulConnection ? GSLoc.Settings.Accounts.EditKeyNotAllowed : GSLoc.Settings.Accounts.EditKeyAllowed);
            }
        }

        if (ImGui.BeginPopupModal("Delete your account?", ref DeleteAccountConfirmation, UiSharedService.PopupWindowFlags))
        {
            if (isPrimary)
            {
                UiSharedService.ColorTextWrapped(GSLoc.Settings.Accounts.RemoveAccountPrimaryWarning, ImGuiColors.DalamudRed);
                ImGui.Spacing();
            }
            // display normal warning
            UiSharedService.TextWrapped(GSLoc.Settings.Accounts.RemoveAccountWarning);
            ImGui.TextUnformatted(GSLoc.Settings.Accounts.RemoveAccountConfirm);
            ImGui.Separator();
            ImGui.Spacing();

            var buttonSize = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - ImGui.GetStyle().ItemSpacing.X) / 2;

            if (_uiShared.IconTextButton(FontAwesomeIcon.Trash, GSLoc.Settings.Accounts.DeleteButtonLabel, buttonSize, false, (!(KeyMonitor.CtrlPressed() && KeyMonitor.ShiftPressed()))))
            {
                _ = RemoveAccountAndRelog(account, isPrimary);
            }
            UiSharedService.AttachToolTip("CTRL+SHIFT Required");

            ImGui.SameLine();

            if (ImGui.Button("Cancel##cancelDelete", new Vector2(buttonSize, 0)))
                DeleteAccountConfirmation = false;

            UiSharedService.SetScaledWindowSize(325);
            ImGui.EndPopup();
        }
    }

    private async Task RemoveAccountAndRelog(Authentication account, bool isPrimary)
    {
        // grab the uid before we delete the user.
        var uid = MainHub.UID;

        // remove the current authentication.
        try
        {
            _logger.LogInformation("Removing Authentication for current character.");
            _serverConfigs.ServerStorage.Authentications.Remove(account);
            if (isPrimary)
            {
                _serverConfigs.ServerStorage.Authentications.Clear();
                _mainConfig.Config.AcknowledgementUnderstood = false;
                _mainConfig.Config.AccountCreated = false;
            }
            _mainConfig.Config.LastUidLoggedIn = "";
            _mainConfig.UpdateConfigs(string.Empty); // Should be changing the UID's file the config file provider service.
            _mainConfig.Save();

            _logger.LogInformation("Deleting Account from Server.");
            await _hub.UserDelete();           
            DeleteAccountConfirmation = false;

            if (!isPrimary)
            {
                var accountProfileFolder = _configFiles.CurrentPlayerDirectory;
                if (Directory.Exists(accountProfileFolder))
                {
                    _logger.LogDebug("Deleting Account Profile Folder for current character.", LoggerType.ApiCore);
                    Directory.Delete(accountProfileFolder, true);
                }
                await _hub.Reconnect(false);
            }
            else
            {
                var allFolders = Directory.GetDirectories(_configFiles.GagSpeakDirectory).Where(c => !c.Contains("eventlog") && !c.Contains("audiofiles")).ToList();
                foreach (var folder in allFolders) 
                    Directory.Delete(folder, true);
                
                _logger.LogInformation("Removed all deleted account folders.");
                await _hub.Disconnect(ServerState.Disconnected, false);
                _mediator.Publish(new SwitchToIntroUiMessage());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete account from server." + ex);
        }
    }
}
