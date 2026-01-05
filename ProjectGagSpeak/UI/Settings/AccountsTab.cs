using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using OtterGui.Text;

namespace GagSpeak.Gui;

public class AccountManagerTab
{
    private readonly ILogger<AccountManagerTab> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _mainConfig;
    private readonly AccountManager _account;
    private readonly ConfigFileProvider _fileProvider;
    
    private AccountProfile? _exposedKeyProfile = null;
    private AccountProfile? _inEditMode = null;

    public AccountManagerTab(ILogger<AccountManagerTab> logger, GagspeakMediator mediator, 
        MainHub hub, MainConfig config, AccountManager account, ConfigFileProvider fileProvider)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _mainConfig = config;
        _account = account;
        _fileProvider = fileProvider;
    }

    public void DrawManager()
    {
        CkGui.FontText(GSLoc.Settings.Accounts.PrimaryLabel, UiFontService.UidFont);
        var localContentId = PlayerData.CID;

        // obtain the primary account auth.
        if (!_account.TryGetMainProfile(out var mainProfile))
        {
            CkGui.ColorText("No primary account setup to display", ImGuiColors.DPSRed);
            return;
        }

        // Draw out the primary account.
        DrawProfile(mainProfile);

        // display title for account management
        if (_account.HasAltProfiles)
        {
            CkGui.FontText(GSLoc.Settings.Accounts.SecondaryLabel, UiFontService.UidFont);
            foreach (var altProfile in _account.GetAltProfiles().ToList())
                DrawProfile(altProfile);
        }
        else
        {
            // display this if we have no alts.
            CkGui.ColorText(GSLoc.Settings.Accounts.NoSecondaries, ImGuiColors.DPSRed);
        }
    }

    private bool CtrlShiftPressed() => ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl;
    private bool CanUseDeleteButton(AccountProfile profile)
        => MainHub.ServerStatus is ServerState.ConnectedDataSynced && CtrlShiftPressed() && profile.ContentId == PlayerData.CID && profile.HadValidConnection;

    private void DrawProfile(AccountProfile profile)
    {
        var isPrimary = profile.IsMainProfile;
        var isCurConnected = MainHub.UID == profile.ProfileUID;
        var itemColor = isPrimary ? ImGuiColors.ParsedGold : ImGuiColors.ParsedPink;
        var showProfileKey = _exposedKeyProfile == profile;
        var editingKey = _inEditMode == profile;

        // Container stylizations.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f);
        using var col = ImRaii.PushColor(ImGuiCol.Border, itemColor)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        // Customize Height based on State.
        var containerSize = new Vector2(ImGui.GetContentRegionAvail().X, CkStyle.ThreeRowHeight().AddWinPadY());
        using var _ = ImRaii.Child($"##AuthListing{profile.ContentId}", containerSize, true, WFlags.ChildWindow);
        if (!_) return;

        // Profile Character Name, and Account Deletion Button.
        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.UserCircle);
            CkGui.ColorTextInline(profile.PlayerName, itemColor);
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.CharaNameLabel);

            // head over to the end to make the delete button.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconTextButtonSize(FAI.Trash, GSLoc.Settings.Accounts.DeleteButtonLabel));

            if (CkGui.IconTextButton(FAI.Trash, "Delete Account", null, true, !CanUseDeleteButton(profile), id: $"DeleteAccount-{profile.ContentId}"))
                ImGui.OpenPopup("Delete Account Confirmation");
            CkGui.AttachToolTip(!profile.HadValidConnection
                ? GSLoc.Settings.Accounts.DeleteButtonDisabledTT : isPrimary
                    ? GSLoc.Settings.Accounts.DeleteButtonTT + GSLoc.Settings.Accounts.DeleteButtonPrimaryTT
                    : GSLoc.Settings.Accounts.DeleteButtonTT, color: ImGuiColors.DalamudRed);
        }
        // Profile Character World, ValidConnectionState, If current connected.
        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            CkGui.IconText(FAI.Globe);
            var worldName = ItemSvc.WorldData.TryGetValue(profile.WorldId, out var name) ? name : "UNKNOWN WORLD";
            CkGui.ColorTextInline(worldName, itemColor);
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.CharaWorldLabel);

            // Shift to right for icons.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.Fingerprint).X - CkGui.IconSize(FAI.PlugCircleCheck).X - 2 * ImUtf8.ItemInnerSpacing.X);
            CkGui.BooleanToColoredIcon(isCurConnected, false, FAI.Fingerprint, FAI.Fingerprint, itemColor, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(isPrimary ? GSLoc.Settings.Accounts.FingerprintPrimary : GSLoc.Settings.Accounts.FingerprintSecondary);
            
            CkGui.BooleanToColoredIcon(profile.HadValidConnection, true, FAI.PlugCircleCheck, FAI.PlugCircleXmark, ImGuiColors.ParsedGreen, ImGuiColors.DalamudGrey3);
            CkGui.AttachToolTip(profile.HadValidConnection ? GSLoc.Settings.Accounts.SuccessfulConnection : GSLoc.Settings.Accounts.NoSuccessfulConnection);
        }
        // Profile Secret Key display.
        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            CkGui.HoverIconText(FAI.Key, itemColor.ToUint());
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.CharaKeyLabel);
            if (ImGui.IsItemClicked())
                _exposedKeyProfile = showProfileKey ? null : profile;

            // Draw based on what should be displayed.
            using (ImRaii.PushFont(UiBuilder.MonoFont))
            {
                if (editingKey)
                {
                    ImUtf8.SameLineInner();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - CkGui.IconButtonSize(FAI.PenSquare).X - ImUtf8.ItemSpacing.X);
                    var key = profile.SecretKey;
                    if (ImGui.InputTextWithHint("##SecretKeyEditor", "Paste SecretKey Here...", ref key, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        key = key.Trim();
                        // Fail if the key already exists.
                        if (_account.TryUpdateSecretKey(profile, key))
                            _logger.LogDebug($"Updated SecretKey for {profile.PlayerName}");
                        // exit edit mode.
                        _inEditMode = null;
                    }
                }
                else
                {
                    var keyDisplayText = showProfileKey ? profile.SecretKey : new string('*', profile.SecretKey.Length);
                    CkGui.ColorTextInline(keyDisplayText, itemColor);
                    CkGui.AttachToolTip(GSLoc.Settings.Accounts.CopyKeyToClipboard);
                    if (ImGui.IsItemClicked()) 
                        ImGui.SetClipboardText(profile.SecretKey);

                }
            }

            // Add the edit button.
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - CkGui.IconSize(FAI.PenSquare).X);
            using (ImRaii.PushColor(ImGuiCol.Text, profile.HadValidConnection ? ImGuiColors.DalamudRed : ImGuiColors.DalamudGrey3))
                if (CkGui.IconButton(FAI.PenSquare, null, $"KeyEditor-{profile.ContentId}", profile.HadValidConnection, true))
                    _inEditMode = editingKey ? null : profile;
            CkGui.AttachToolTip(profile.HadValidConnection ? GSLoc.Settings.Accounts.EditKeyNotAllowed : GSLoc.Settings.Accounts.EditKeyAllowed);
        }

        AccountDeletionPopup(profile);
    }

    public void AccountDeletionPopup(AccountProfile profile)
    {
        if (!ImGui.IsPopupOpen("Delete Account Confirmation"))
            return;

        // center the hardcore window.
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f));
        // set the size of the popup.
        var size = new Vector2(600f, 345f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextWindowSize(size);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.WindowRounding, 12f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudGrey2);

        using var pop = ImRaii.Popup("Delete Account Confirmation", WFlags.Modal | WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoMove);
        if (!pop)
            return;

        using (ImRaii.Group())
        {
            CkGui.FontTextCentered("WARNING", UiFontService.GagspeakTitleFont, ImGuiColors.DalamudRed);
            CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X);

            if (profile.IsMainProfile)
            {
                CkGui.IconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);
                CkGui.TextInline("You are about to delete your PRIMARY account.");
                CkGui.IconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);
                CkGui.ColorTextInline("THIS WILL ALSO DELETE ALL YOUR ALT PROFILES.", ImGuiColors.DalamudYellow);
                ImGui.Spacing();
                CkGui.IconText(FAI.Exclamation, ImGuiColors.DalamudRed);
                CkGui.TextInline("This is effectively a FACTORY RESET of GagSpeak!");
                CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X - ImGui.GetStyle().WindowPadding.X);
            }

            CkGui.IconText(FAI.InfoCircle);
            CkGui.TextInline("Removing your profile erases all stored data associated with it, including:");

            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("All Configured Permissions");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("Applied Bondage Data");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("Collar Data");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("Ownership over ShareHub publications. (Uploads persist under your Anon. name)");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("KinkPlate™ Data");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("Saved Achievement Data");
        }
        var yesButton = $"I Understand, Delete Profile for {profile.PlayerName}({profile.ProfileUID})";
        var noButton = "Oh my, take me back!";
        var yesSize = ImGuiHelpers.GetButtonSize(yesButton);
        var noSize = ImGuiHelpers.GetButtonSize(noButton);
        var offsetX = (size.X - (yesSize.X + noSize.X + ImUtf8.ItemSpacing.X).RemoveWinPadX()) / 2;
        CkGui.SeparatorSpaced();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);
        using (ImRaii.Disabled(!CtrlShiftPressed()))
        {
            if (ImGui.Button(yesButton))
            {
                UiService.SetUITask(() => RemoveProfileAndReload(profile));
                ImGui.CloseCurrentPopup();
            }
        }
        CkGui.AttachToolTip("Must hold CTRL+SHIFT to select!");

        ImGui.SameLine();
        if (ImGui.Button(noButton))
            ImGui.CloseCurrentPopup();
    }

    private async Task RemoveProfileAndReload(AccountProfile profile)
    {
        // grab the uid before we delete the user.
        var uid = MainHub.UID;
        var isMain = profile.IsMainProfile;
        // Remove the profile from the account config.
        try
        {
            _logger.LogInformation("Removing Authentication for current character.");
            _account.RemoveProfile(profile);
             // The server automatically handles cleanup of alt profiles, so just clear the manager.
            if (isMain)
            {
                _logger.LogInformation("Removed Primary Profile, removing all other profiles.");
                _account.ClearAllProfiles();
            }

            // Update the last logged in UID. (Could do this from the provider but this feels safest for now)
            _mainConfig.Current.LastUidLoggedIn = string.Empty;
            _mainConfig.Save();

            // Before we do any servercalls, snag the list of UID's from our current connection response.
            var accountUids = MainHub.ConnectionResponse?.AccountProfileUids ?? [];

            _logger.LogInformation("Deleting Account from Server.");
            await _hub.UserDelete();

            // Delete the folders based off our profile type that was deleted.
            if (isMain)
            {
                var toDelete = Directory.GetDirectories(ConfigFileProvider.GagSpeakDirectory)
                    .Where(d => accountUids.Contains(d, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                foreach (var folder in toDelete)
                    Directory.Delete(folder, true);

                _logger.LogInformation("Removed all deleted account folders.");
                // Cleanup the remaining UID's
                _fileProvider.ClearUidConfigs();
                // Fully disconnect and switch back to the intro UI.
                await _hub.Disconnect(ServerState.Disconnected, DisconnectIntent.Reload, false);
                _mediator.Publish(new SwitchToIntroUiMessage());
            }
            else
            {
                var toDelete = _fileProvider.CurrentPlayerDirectory;
                if (Directory.Exists(toDelete))
                {
                    _logger.LogDebug("Deleting Config Folder for removed profile.", LoggerType.ApiCore);
                    Directory.Delete(toDelete, true);
                }
                _fileProvider.ClearUidConfigs();
                await _hub.Reconnect(DisconnectIntent.Reload, false);
            }
        }
        catch (Bagagwa ex)
        {
            _logger.LogError("Failed to delete account from server." + ex);
        }
    }
}
