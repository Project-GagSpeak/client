using CkCommons;
using CkCommons.Gui;
using CkCommons.Helpers;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui;

public class AccountManagerTab
{
    private readonly ILogger<AccountManagerTab> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _mainConfig;
    private readonly ServerConfigManager _serverConfigs;
    private readonly ConfigFileProvider _configFiles;
    
    private bool DeleteAccountConfirmation = false;
    private int ShowKeyIdx = -1;
    private int EditingIdx = -1;
    public AccountManagerTab(ILogger<AccountManagerTab> logger, GagspeakMediator mediator, MainHub hub,
        MainConfig config, ServerConfigManager serverConfigs, ConfigFileProvider configFiles)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _mainConfig = config;
        _serverConfigs = serverConfigs;
        _configFiles = configFiles;
    }

    public void DrawManager()
    {
        CkGui.FontText(GSLoc.Settings.Accounts.PrimaryLabel, UiFontService.UidFont);
        var localContentId = PlayerData.ContentId;

        // obtain the primary account auth.
        var primaryAuth = _serverConfigs.ServerStorage.Authentications.FirstOrDefault(c => c.IsPrimary);
        if (primaryAuth is null)
        {
            CkGui.ColorText("No primary account setup to display", ImGuiColors.DPSRed);
            return;
        }

        // Draw out the primary account.
        DrawAccount(int.MaxValue, primaryAuth, primaryAuth.CharacterPlayerContentId == localContentId);

        // display title for account management
        CkGui.FontText(GSLoc.Settings.Accounts.SecondaryLabel, UiFontService.UidFont);
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
        CkGui.ColorText(GSLoc.Settings.Accounts.NoSecondaries, ImGuiColors.DPSRed);
    }

    private void DrawAccount(int idx, Authentication account, bool isOnlineUser = false)
    {
        var isPrimary = account.IsPrimary;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f);
        using var col = ImRaii.PushColor(ImGuiCol.Border, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
                    
        var height = ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2;
        using var child = ImRaii.Child($"##AuthAccountListing" + idx + account.CharacterPlayerContentId, new Vector2(ImGui.GetContentRegionAvail().X, height), true, WFlags.ChildWindow);
        if (!child) return;

        using (var group = ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.UserCircle);
            ImUtf8.SameLineInner();
            CkGui.ColorText(account.CharacterName, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink);
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.CharaNameLabel);

            // head over to the end to make the delete button.
            var cannotDelete = (!(KeyMonitor.CtrlPressed() && KeyMonitor.ShiftPressed()) || !(MainHub.IsServerAlive && MainHub.IsConnected && isOnlineUser));
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconTextButtonSize(FAI.Trash, GSLoc.Settings.Accounts.DeleteButtonLabel));

            var hadEstablishedConnection = account.SecretKey.HasHadSuccessfulConnection;

            if (CkGui.IconTextButton(FAI.Trash, "Delete Account", isInPopup: true, disabled: !hadEstablishedConnection || cannotDelete, id: "DeleteAccount" + account.CharacterPlayerContentId))
            {
                DeleteAccountConfirmation = true;
                ImGui.OpenPopup("Delete your account?");
            }
            CkGui.AttachToolTip("THIS BUTTON CAN BE A BIT BUGGY AND MAY REMOVE YOUR PRIMARY WITHOUT NOTICE ON ACCIDENT. LOOKING INTO WHY IN 1.1.1.0\n" +
                (!hadEstablishedConnection
                ? GSLoc.Settings.Accounts.DeleteButtonDisabledTT : isPrimary
                    ? GSLoc.Settings.Accounts.DeleteButtonTT + GSLoc.Settings.Accounts.DeleteButtonPrimaryTT
                    : GSLoc.Settings.Accounts.DeleteButtonTT, color: ImGuiColors.DalamudRed));

        }
        // next line:
        using (var group2 = ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.Globe);
            ImUtf8.SameLineInner();
            CkGui.ColorText(ItemSvc.WorldData.TryGetValue(account.WorldId, out var name) ? name : "UNKNOWN WORLD", isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink);
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.CharaWorldLabel);

            var isOnUserSize = CkGui.IconSize(FAI.Fingerprint);
            var successfulConnection = CkGui.IconSize(FAI.PlugCircleCheck);
            var rightEnd = ImGui.GetContentRegionAvail().X - successfulConnection.X - isOnUserSize.X - 2 * ImGui.GetStyle().ItemInnerSpacing.X;
            ImGui.SameLine(rightEnd);

            CkGui.BooleanToColoredIcon(isOnlineUser, false, FAI.Fingerprint, FAI.Fingerprint, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(account.IsPrimary ? GSLoc.Settings.Accounts.FingerprintPrimary : GSLoc.Settings.Accounts.FingerprintSecondary);
            CkGui.BooleanToColoredIcon(account.SecretKey.HasHadSuccessfulConnection, true, FAI.PlugCircleCheck, FAI.PlugCircleXmark, ImGuiColors.ParsedGreen, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(account.SecretKey.HasHadSuccessfulConnection ? GSLoc.Settings.Accounts.SuccessfulConnection : GSLoc.Settings.Accounts.NoSuccessfulConnection);
        }

        // next line:
        using (var group3 = ImRaii.Group())
        {
            var keyDisplayText = (ShowKeyIdx == idx) ? account.SecretKey.Key : account.SecretKey.Label;
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.Key);
            if (ImGui.IsItemClicked())
            {
                ShowKeyIdx = (ShowKeyIdx == idx) ? -1 : idx;
            }
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.CharaKeyLabel);
            // we shoul draw an inputtext field here if we can edit it, and a text field if we cant.
            if (EditingIdx == idx)
            {
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.PenSquare).X - ImGui.GetStyle().ItemSpacing.X);
                var key = account.SecretKey.Key;
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
                            account.SecretKey.Label = $"Alt Character Key for {account.CharacterName} on {OnFrameworkService.WorldData[account.WorldId]}";
                        account.SecretKey.Key = key;
                    }

                    EditingIdx = -1;
                    _serverConfigs.Save();
                }
            }
            else
            {
                ImUtf8.SameLineInner();
                CkGui.ColorText(keyDisplayText, isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink);
                if (ImGui.IsItemClicked()) ImGui.SetClipboardText(account.SecretKey.Key);
                CkGui.AttachToolTip(GSLoc.Settings.Accounts.CopyKeyToClipboard);
            }

            if (idx != int.MaxValue)
            {
                var insertKey = CkGui.IconSize(FAI.PenSquare);
                var rightEnd = ImGui.GetContentRegionAvail().X - insertKey.X;
                ImGui.SameLine(rightEnd);
                var boolCol = account.SecretKey.HasHadSuccessfulConnection ? ImGuiColors.DalamudRed : ImGuiColors.DalamudGrey3;
                CkGui.BooleanToColoredIcon(EditingIdx == idx, false, FAI.PenSquare, FAI.PenSquare, ImGuiColors.ParsedPink, boolCol);
                if (ImGui.IsItemClicked() && !account.SecretKey.HasHadSuccessfulConnection)
                    EditingIdx = EditingIdx == idx ? -1 : idx;
                CkGui.AttachToolTip(account.SecretKey.HasHadSuccessfulConnection ? GSLoc.Settings.Accounts.EditKeyNotAllowed : GSLoc.Settings.Accounts.EditKeyAllowed);
            }
        }

        if (ImGui.BeginPopupModal("Delete your account?", ref DeleteAccountConfirmation, WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoScrollWithMouse))
        {
            if (isPrimary)
            {
                CkGui.ColorTextWrapped(GSLoc.Settings.Accounts.RemoveAccountPrimaryWarning, ImGuiColors.DalamudRed);
                ImGui.Spacing();
            }
            // display normal warning
            CkGui.TextWrapped(GSLoc.Settings.Accounts.RemoveAccountWarning);
            ImGui.TextUnformatted(GSLoc.Settings.Accounts.RemoveAccountConfirm);
            ImGui.Separator();
            ImGui.Spacing();

            var buttonSize = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - ImGui.GetStyle().ItemSpacing.X) / 2;

            if (CkGui.IconTextButton(FAI.Trash, GSLoc.Settings.Accounts.DeleteButtonLabel, buttonSize, false, !(KeyMonitor.CtrlPressed() && KeyMonitor.ShiftPressed())))
            {
                _ = RemoveAccountAndRelog(account, isPrimary);
            }
            CkGui.AttachToolTip("CTRL+SHIFT Required");

            ImGui.SameLine();

            if (ImGui.Button("Cancel##cancelDelete", new Vector2(buttonSize, 0)))
                DeleteAccountConfirmation = false;

            CkGui.SetScaledWindowSize(325);
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
            // In theory this should have automatic cleanup once the new account management stuff works out.
            if (isPrimary)
            {
                _serverConfigs.ServerStorage.Authentications.Clear();
                _mainConfig.Current.AcknowledgementUnderstood = false;
            }
            _mainConfig.Current.LastUidLoggedIn = "";
            _mainConfig.Save();
            _logger.LogInformation("Deleting Account from Server.");
            await _hub.UserDelete();           
            DeleteAccountConfirmation = false;

            if (isPrimary)
            {
                var allFolders = Directory.GetDirectories(ConfigFileProvider.GagSpeakDirectory)
                    .Where(c => !c.Contains("eventlog") && !c.Contains("audiofiles"))
                    .ToList();

                foreach (var folder in allFolders)
                    Directory.Delete(folder, true);

                _logger.LogInformation("Removed all deleted account folders.");
                _configFiles.ClearUidConfigs();
                await _hub.Disconnect(ServerState.Disconnected, false);
                _mediator.Publish(new SwitchToIntroUiMessage());
            }
            else
            {
                var accountProfileFolder = _configFiles.CurrentPlayerDirectory;
                if (Directory.Exists(accountProfileFolder))
                {
                    _logger.LogDebug("Deleting Account Profile Folder for current character.", LoggerType.ApiCore);
                    Directory.Delete(accountProfileFolder, true);
                }
                _configFiles.ClearUidConfigs();
                await _hub.Reconnect(false);
            }
        }
        catch (Bagagwa ex)
        {
            _logger.LogError("Failed to delete account from server." + ex);
        }
    }
}
