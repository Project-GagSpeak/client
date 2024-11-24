using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Localization;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI;

public class AccountsTab
{
    private readonly ILogger<AccountsTab> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _apiHubMain;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly ClientMonitorService _clientService;
    private readonly UiSharedService _uiShared;

    private string ConfigDirectory { get; init; } = string.Empty;
    private bool DeleteAccountConfirmation = false;
    private int ShowKeyIdx = -1;
    private int EditingIdx = -1;
    public AccountsTab(ILogger<AccountsTab> logger, GagspeakMediator mediator, MainHub apiHubMain,
        ClientConfigurationManager clientConfigs, ServerConfigurationManager serverConfigs,
        ClientMonitorService clientService, UiSharedService uiShared, string configDirectory)
    {
        _logger = logger;
        _mediator = mediator;
        _apiHubMain = apiHubMain;
        _clientConfigs = clientConfigs;
        _serverConfigs = serverConfigs;
        _clientService = clientService;
        _uiShared = uiShared;

        ConfigDirectory = configDirectory;
    }

    public void DrawManager()
    {
        _uiShared.GagspeakBigText(GSLoc.Settings.Accounts.PrimaryLabel);
        var localContentId = _clientService.ContentId;

        // obtain the primary account auth.
        var primaryAuth = _serverConfigs.CurrentServer.Authentications.FirstOrDefault(c => c.IsPrimary);
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
            var secondaryAuths = _serverConfigs.CurrentServer.Authentications
                .Where(c => !c.IsPrimary)
                .OrderByDescending(c => c.SecretKey.HasHadSuccessfulConnection)
                .ToList();

            for (int i = 0; i < secondaryAuths.Count; i++)
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

        float height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
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
            UiSharedService.AttachToolTip(!hadEstablishedConnection
                ? GSLoc.Settings.Accounts.DeleteButtonDisabledTT : isPrimary
                    ? GSLoc.Settings.Accounts.DeleteButtonTT + GSLoc.Settings.Accounts.DeleteButtonPrimaryTT
                    : GSLoc.Settings.Accounts.DeleteButtonTT, color: ImGuiColors.DalamudRed);

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
            float rightEnd = ImGui.GetContentRegionAvail().X - successfulConnection.X - isOnUserSize.X - 2 * ImGui.GetStyle().ItemInnerSpacing.X;
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
                    if (account.SecretKey.Label.IsNullOrEmpty())
                        account.SecretKey.Label = "Alt Character Key for " + account.CharacterName + " on " + OnFrameworkService.WorldData.Value[(ushort)account.WorldId];
                    // set the key and save the changes.
                    account.SecretKey.Key = key;
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
                float rightEnd = ImGui.GetContentRegionAvail().X - insertKey.X;
                ImGui.SameLine(rightEnd);
                Vector4 col = account.SecretKey.HasHadSuccessfulConnection ? ImGuiColors.DalamudRed : ImGuiColors.DalamudGrey3;
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
            // remove all patterns belonging to this account.
            _logger.LogInformation("Removing Patterns for current character.");
            _clientConfigs.RemoveAllPatternsFromUID(uid);

            _logger.LogInformation("Removing Authentication for current character.");
            _serverConfigs.CurrentServer.Authentications.Remove(account);
            if (isPrimary)
            {
                _serverConfigs.CurrentServer.Authentications.Clear();
                _clientConfigs.GagspeakConfig.AcknowledgementUnderstood = false;
                _clientConfigs.GagspeakConfig.AccountCreated = false;
            }
            _clientConfigs.GagspeakConfig.LastUidLoggedIn = "";
            _clientConfigs.UpdateConfigs(string.Empty);
            _clientConfigs.Save();

            _logger.LogInformation("Deleting Account from Server.");
            await _apiHubMain.UserDelete();           
            DeleteAccountConfirmation = false;

            if (!isPrimary)
            {
                var accountProfileFolder = Path.Combine(ConfigDirectory, uid);
                if (Directory.Exists(accountProfileFolder))
                {
                    _logger.LogDebug("Deleting Account Profile Folder for current character.", LoggerType.ApiCore);
                    Directory.Delete(accountProfileFolder, true);
                }
                await _apiHubMain.Reconnect(false);
            }
            else
            {
                var allFolders = Directory.GetDirectories(ConfigDirectory).Where(c => !c.Contains("eventlog") && !c.Contains("audiofiles")).ToList();
                foreach (var folder in allFolders) 
                    Directory.Delete(folder, true);
                
                _logger.LogInformation("Removed all deleted account folders.");
                await _apiHubMain.Disconnect(ServerState.Disconnected, false);
                _mediator.Publish(new SwitchToIntroUiMessage());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete account from server." + ex);
        }
    }
}
