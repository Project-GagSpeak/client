using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
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
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using OtterGui.Text;
using OtterGuiInternal;

namespace GagSpeak.Gui.Settings;

public partial class SettingsHubService
{
    private enum DeletePopupState { Confirmation, Processing, ServerFailed, Success }

    private static readonly string[] PASSKEYSTARS = [.. Enumerable.Range(0, 65).Select(i => new string('*', i))];

    private readonly Queue<Action> _postDrawActions = new();

    // Cached Internal Helpers (May change overtime.)
    private AccountProfile? _selected = null;
    private AccountProfile? _editingSecretKey = null;
    private AccountProfile? _showingKey = null;
    private DeletePopupState _deletePopupState = DeletePopupState.Confirmation;

    private ImDrawListPtr _wdl;
    private ImGuiStylePtr _style;

    private float _frameH;
    private float _frameHSpacingWidth;
    private uint _frameBgHoverCol;
    private float _bendM;
    private Vector2 _shadowSize;
    private Vector2 _styleOffset;
    private Vector2 _buttonPadding;

    private void InitStyle()
    {
        _wdl = ImGui.GetWindowDrawList();
        _style = ImGui.GetStyle();
        _frameH = ImUtf8.FrameHeight;
        _frameHSpacingWidth = ImUtf8.FrameHeight + ImUtf8.ItemInnerSpacing.X;
        _frameBgHoverCol = ImGui.GetColorU32(ImGuiCol.FrameBgHovered);
        _bendM = _style.FrameRounding * 1.75f;
        _shadowSize = ImGuiHelpers.ScaledVector2(1);
        _styleOffset = ImGuiHelpers.ScaledVector2(2);
        _buttonPadding = _styleOffset + _style.FramePadding;
    }

    // Attempt a new style here.
    public void DrawAccount()
    {
        InitStyle();

        using var s = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        DrawProfileSelector(150f * ImGuiHelpers.GlobalScale);
        ImGui.SameLine();
        DrawProfilePanelNew();

        // Perform any post-draw actions we need to.
        while (_postDrawActions.TryDequeue(out Action? action))
        {
            // Safely execute each post-draw action until the queue is empty.
            Generic.Safe(() => action());
        }
    }

    private void DrawProfilePanelNew()
    {
        using var _ = ImRaii.Child("profile-panel", ImGui.GetContentRegionAvail());
        if (_selected is not { } profile)
        {
            CkGui.FontText("No Profile Selected", Fonts.SubtitleFont);
            return;
        }

        using var id = ImRaii.PushId(profile.ContentId.ToString());

        // Precalculations
        var cursorMin = ImGui.GetCursorPos();
        var io = ImGui.GetIO();
        var labelToShow = string.IsNullOrWhiteSpace(profile.PlayerName) ? "UNK PLAYER" : profile.PlayerName;
        var labelSize = CkGui.CalcFontTextSize(labelToShow, Fonts.DefaultScaled);
        var deleteSize = 100 * ImGuiHelpers.GlobalScale;
        // Draw out the label
        CkGui.FontText(labelToShow, Fonts.DefaultScaled);

        // Shift all the way to the right and then draw the delete button.
        ImGui.SetCursorPos(cursorMin + new Vector2(ImGui.GetContentRegionAvail().X - deleteSize, (labelSize.Y - _frameH) / 2));
        var disable = _selected is null || !(io.KeyCtrl && io.KeyShift);
        using (ImRaii.PushColor(ImGuiCol.Button, CkCol.TriStateCross.Uint()))
            if (CkGui.IconTextButtonCentered(FAI.Trash, "Delete", deleteSize, disabled: disable))
            {
                if (_selected!.HadValidConnection)
                    ImGui.OpenPopup("profile-delete-confirmation");
                else
                {
                    _postDrawActions.Enqueue(() =>
                    {
                        // We can just remove it plainly as it is not bound to any profile serverside.
                        if (_account.RemoveProfile(_selected))
                        {
                            _account.Save();
                            _selected = null;
                            _editingSecretKey = null;
                            _showingKey = null;
                        }
                    });
                }
            }
        CkGui.AttachTooltip(GSLoc.Settings.Accounts.RemoveProfileTT, CkCol.TriStateCross.Vec4(), ImGuiHoveredFlags.AllowWhenDisabled);

        DrawSecretKey(profile);

        // Draw out other information about them
        using (ImRaii.PushFont(UiBuilder.MonoFont))
        {
            CkGui.FramedIconText(FAI.Crown);
            CkGui.TextFrameAlignedInline("Is Primary Profile:");
            CkGui.BoolIcon(profile.IsPrimary);

            CkGui.FramedIconText(FAI.CheckCircle);
            CkGui.TextFrameAlignedInline("Is Valid:");
            CkGui.BoolIcon(profile.HadValidConnection);

            CkGui.FramedIconText(FAI.Globe);
            CkGui.TextFrameAlignedInline("World:");
            // i am positive there is a lookup for worldid > name... and so i search
            CkGui.ColorTextFrameAlignedInline(ItemSvc.WorldData[profile.WorldId], ImGuiColors.TankBlue);

            CkGui.FramedIconText(FAI.IdBadge);
            CkGui.TextFrameAlignedInline("UID:");
            var noUid = string.IsNullOrEmpty(profile.UserUID);
            CkGui.ColorTextFrameAlignedInline(noUid ? "Not Yet Assigned" : profile.UserUID, noUid ? ImGuiColors.DalamudRed : ImGuiColors.TankBlue);
            CkGui.AttachTooltip("Once you successfully connect with the inserted secret key below, your UID will be set!");
        }

        // Fire if true (Very bottom)
        AccountDeletionPopup(_selected);
    }

    private void DrawSecretKey(AccountProfile profile)
    {
        // Precalculations
        var width = ImGui.GetContentRegionAvail().X;
        var showEditor = _editingSecretKey == profile;
        var showKey = _showingKey == profile;
        var editWidth = CkGui.IconButtonSize(FAI.PenSquare).X;
        var eyeWidth = CkGui.IconButtonSize(FAI.Eye).X;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        CkGui.FramedIconText(FAI.Key);
        ImUtf8.SameLineInner();
        if (showEditor)
        {
            ImGui.SetNextItemWidth(width - _frameHSpacingWidth - editWidth);
            var key = profile.Key;
            if (ImGui.InputTextWithHint("##KeyEditor", "Paste SecretKey Here...", ref key, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                key = key.Trim();
                if (_account.TryUpdateSecretKey(profile, key))
                {
                    _logger.LogInformation($"Updated SecretKey for {profile.PlayerName}");
                    if (PlayerData.Available && key.Length is 64 && MainHub.ServerStatus is ServerState.Unattached or ServerState.Unauthorized or ServerState.NoSecretKey)
                    {
                        // We should attempt a reconnect if we inserted a potentially valid secret key and are logged into the player we set the key for.
                        if (_account.GetCharaProfile() is { } tracked && tracked == profile)
                        {
                            _logger.LogInformation($"Attempting to reconnect to the server with the new SecretKey for {profile.PlayerName}");
                            UiService.SetUITask(async () => await _hub.Reconnect(DisconnectIntent.Reload).ConfigureAwait(false));
                        }
                    }
                }
                _editingSecretKey = null;
            }
        }
        else
        {
            var pos = ImGui.GetCursorScreenPos();
            var txtSize = new Vector2(width - _frameHSpacingWidth - editWidth, ImUtf8.FrameHeight);
            var txtRect = new ImRect(pos, pos + txtSize - _style.FramePadding);
            var keyLength = Math.Clamp(profile.Key.Length, 0, 64);
            var keyText = string.IsNullOrEmpty(profile.Key) ? "No SecretKey Added" : profile.Key;
            var txt = showKey ? keyText : PASSKEYSTARS[keyLength];
            ImGuiInternal.RenderTextClipped(_wdl, txtRect.Min + new Vector2(0, _style.FramePadding.Y), txtRect.Max, txt, Vector2.Zero, txtSize, txtRect, true);
            ImGui.Dummy(txtSize);
            CkGui.AttachTooltip(GSLoc.Settings.Accounts.CopyKeyTT);
            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(profile.Key);
        }
        // Eye or checkmark depending on which.
        if (profile.HadValidConnection)
        {
            ImGui.SameLine(width - eyeWidth, 0);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.TankBlue, showKey))
                if (CkGui.IconButton(FAI.Eye, inPopup: true))
                    _showingKey = _showingKey == profile ? null : profile;
            CkGui.AttachTooltip(GSLoc.Settings.Accounts.ProfileKey);
        }
        else
        {
            ImGui.SameLine(width - editWidth, 0);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.TankBlue, showEditor))
                if (CkGui.IconButton(FAI.PenSquare, inPopup: true))
                    _editingSecretKey = _editingSecretKey == profile ? null : profile;
            CkGui.AttachTooltip(GSLoc.Settings.Accounts.EditKeyTT);
        }
    }

    private void DrawProfileSelector(float width)
    {
        using (var _ = CkRaii.Child("profile-selector", new Vector2(width, -1)))
        {
            var rolesH = CkGui.CalcFontTextSize("A", Fonts.DefaultScaled).Y;
            var addOffset = CkGui.IconButtonSize(FAI.Plus).X;
            CkGui.FontText("Profiles", Fonts.DefaultScaled);
            ImGui.SetCursorPos(new Vector2(width - addOffset, (rolesH - _frameH) / 2));
            // Limit to 10 profiles max!
            if (CkGui.IconButton(FAI.Plus, disabled: _account.Profiles.Count >= 10, inPopup: true))
                _account.AddNewProfile();
            CkGui.AttachTooltip(GSLoc.Settings.Accounts.AddProfileTT);

            ImGui.Separator();
            // Inner child for remaining space.
            var inner = new Vector2(_.InnerRegion.X - 2 * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y);
            using (ImRaii.Child("profile-list", inner))
            {
                var size = new Vector2(ImGui.GetContentRegionAvail().X, _frameH + ImUtf8.TextHeightSpacing);
                foreach (var profile in _account.Profiles)
                {
                    if (SelectableProfile(profile, size, profile == _selected))
                        SetSelectedProfile(profile);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        _selected = null;
                        _editingSecretKey = null;
                        _showingKey = null;
                    }
                }
            }
        }
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var lineS = new Vector2(max.X, min.Y);
        _wdl.AddLine(lineS, max, ImGui.GetColorU32(ImGuiCol.Separator), ImGuiHelpers.GlobalScale);
    }

    private bool SelectableProfile(AccountProfile profile, Vector2 size, bool isSelected)
    {
        var window = ImGuiInternal.GetCurrentWindow();
        if (window.SkipItems)
            return false;
        // Aquire our ID for this new internal item.
        var id = ImGui.GetID(profile.ContentId.ToString());

        var pos = window.DC.CursorPos;
        // Get the offsets and true height.
        var trueH = size.Y + _styleOffset.Y * 2;
        // Aquire a valid bounding box for this button interaction
        var itemSize = new Vector2(size.X, trueH);
        var hitbox = new ImRect(pos, pos + itemSize);
        var drawArea = new ImRect(hitbox.Min + _buttonPadding, hitbox.Max - _buttonPadding);

        ImGuiInternal.ItemSize(itemSize, _style.FramePadding.Y + _styleOffset.Y);
        if (!ImGuiP.ItemAdd(hitbox, id, null))
            return false;

        // Process interactions for our created 'Button'
        var hovered = false;
        var active = false;
        var clicked = ImGuiP.ButtonBehavior(hitbox, id, ref hovered, ref active);

        uint shadowCol = 0x64000000;
        uint bgCol = GagspeakEx.GetFrameBg(hovered, active);
        uint borderCol = isSelected ? GsCol.VibrantPink.Uint() : CkGui.ApplyAlpha(0xDCDCDCDC, active ? 0.7f : hovered ? 0.63f : 0.39f);

        // Render frame
        window.DrawList.AddRect(hitbox.Min, hitbox.Max, shadowCol, _bendM, ImDrawFlags.RoundCornersAll, _shadowSize.X); // Shadow
        window.DrawList.AddRectFilled(hitbox.Min + _styleOffset, hitbox.Max - _styleOffset, bgCol, _bendM, ImDrawFlags.RoundCornersAll); // BG
        window.DrawList.AddRect(hitbox.Min + _shadowSize, hitbox.Max - _shadowSize, borderCol, _bendM, ImDrawFlags.RoundCornersAll, 1.25f * ImGuiHelpers.GlobalScale); // Border
        ImGuiP.RenderNavHighlight(hitbox, id);

        // Render Contents
        var iconSize = CkGui.IconSize(FAI.CheckCircle);
        var txtSize = ImGui.CalcTextSize(profile.PlayerName);
        var innerClip = new ImRect(hitbox.Min + _styleOffset, new Vector2(hitbox.Max.X - iconSize.X - _style.ItemSpacing.X * 2 - _styleOffset.X, hitbox.Max.Y - _styleOffset.Y));
        ImGuiInternal.RenderTextClipped(window.DrawList, drawArea.Min, drawArea.Max, profile.PlayerName, Vector2.Zero, txtSize, innerClip, true);

        var iconPosTR = new Vector2(drawArea.Max.X - iconSize.X, drawArea.Min.Y);
        using (Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            window.DrawList.AddText(FAI.CheckCircle.ToIconString(), iconPosTR, profile.HadValidConnection ? CkCol.TriStateCheck.Uint() : _frameBgHoverCol);
        if (ImGui.IsMouseHoveringRect(iconPosTR, iconPosTR + iconSize))
            CkGui.ToolTipInternal(profile.HadValidConnection ? "Had a valid Connection." : "Profile has yet to establish a valid connection with the server.");

        window.DrawList.AddText(drawArea.Min + new Vector2(0, ImUtf8.TextHeightSpacing),
            profile.HadValidConnection ? ImGuiColors.DalamudGrey2.ToUint() : ImGuiColors.DalamudRed.ToUint(),
            profile.HadValidConnection ? profile.UserUID : "No UID Assigned");
        return clicked;
    }

    private void SetSelectedProfile(AccountProfile profile)
    {
        // Do nothing if the same.
        if (_selected == profile)
            return;
        // Update the profile.
        _selected = profile;
        _showingKey = null;
        _editingSecretKey = profile.Key.IsNullOrWhitespace() ? profile : null;
    }

    #region Profile Removal Logic
    public void AccountDeletionPopup(AccountProfile? profile)
    {
        if (profile is null)
            return;

        if (!ImGui.IsPopupOpen("profile-delete-confirmation"))
        {
            _deletePopupState = DeletePopupState.Confirmation;
            return;
        }

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f));
        var size = new Vector2(600f, 345f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextWindowSize(size);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f).Push(ImGuiStyleVar.WindowRounding, 12f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudGrey2);
        using var pop = ImRaii.Popup("profile-delete-confirmation", WFlags.Modal | WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoMove);
        if (!pop) return;

        // Confirmation Window before account deletion.
        if (_deletePopupState is DeletePopupState.Confirmation)
        {
            using (ImRaii.Group())
            {
                CkGui.FontTextCentered("WARNING", Fonts.SubtitleFont, ImGuiColors.DalamudRed);
                CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X);

                if (profile.IsPrimary)
                {
                    CkGui.IconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);
                    CkGui.TextInline("You are about to delete your PRIMARY account.");
                    CkGui.IconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);
                    CkGui.ColorTextInline("THIS WILL ALSO DELETE ALL YOUR ALT PROFILES.", ImGuiColors.DalamudYellow);
                    ImGui.Spacing();
                    CkGui.IconText(FAI.Exclamation, ImGuiColors.DalamudRed);
                    CkGui.TextInline("This is effectively a FACTORY RESET of your Account!");
                    CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X - ImGui.GetStyle().WindowPadding.X);
                }

                CkGui.IconText(FAI.InfoCircle);
                CkGui.TextInline("Removing your profile erases all stored data associated with it, including:");
                CkGui.IconText(FAI.ArrowRight);
                CkGui.ColorTextInline("Owned Sanctions across all characters.", CkCol.TriStateCross.Uint());
                CkGui.IconText(FAI.ArrowRight);
                CkGui.TextInline("All Configured Permissions");
                CkGui.IconText(FAI.ArrowRight);
                CkGui.TextInline("Your Paired Users");
            }

            var yesButton = $"I Understand, Delete {profile.PlayerName}({profile.UserUID})";
            var noButton = "Uhh... Take me back!";
            var yesSize = CkGui.GetButtonSize(yesButton);
            var noSize = CkGui.GetButtonSize(noButton);

            ImGui.SetCursorPosY(size.Y - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y);
            CkGui.SetCursorXtoCenter(yesSize + noSize + ImUtf8.ItemSpacing.X);

            if (CkGui.ButtonEx(yesButton, CkCol.TriStateCross.Uint(), disabled: !(ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl)))
            {
                _deletePopupState = DeletePopupState.Processing;
                UiService.SetUITask(() => RemoveProfileAndReload(profile));
            }
            CkGui.AttachTooltip("Must hold CTRL+SHIFT to select!", ImGuiHoveredFlags.AllowWhenDisabled);

            ImGui.SameLine();
            if (CkGui.ButtonEx(noButton))
                ImGui.CloseCurrentPopup();
        }
        // If we are processing for removal, indicate this on display.
        else if (_deletePopupState is DeletePopupState.Processing)
        {
            using (Fonts.SubtitleFont.Push())
            {
                // Just display a loading state while the background task runs
                ImGui.SetCursorPosY((size.Y / 2) - (ImGui.GetTextLineHeight() / 2));
                CkGui.ColorTextCentered("Communicating with Server...", ImGuiColors.DalamudYellow);
            }
        }
        // Otherwise if the server failed, show a secondary popup window that allows the user to forcibly delete their data.
        // This would occur if they removed their profile via the discord bot and needed to re-sync their client.
        else if (_deletePopupState is DeletePopupState.ServerFailed)
        {
            using (ImRaii.Group())
            {
                CkGui.FontTextCentered("SERVER DELETION FAILED", Fonts.SubtitleFont, ImGuiColors.DalamudRed);
                CkGui.Separator(ImGuiColors.DalamudRed.ToUint(), size.X);

                CkGui.IconText(FAI.ExclamationTriangle, ImGuiColors.DalamudYellow);
                ImUtf8.SameLineInner();
                ImGui.TextWrapped("The server failed to delete this user. It may no longer exist on the server (e.g., removed via Discord), or there was a network error.");

                ImGui.Spacing();
                CkGui.IconText(FAI.QuestionCircle, ImGuiColors.TankBlue);
                ImUtf8.SameLineInner(); 
                ImGui.TextWrapped("Are you sure you want to forcibly remove the locally cached data?");

                ImGui.Spacing();
                CkGui.ColorTextWrapped("Note: Once you do this, you will need to generate a new secret key if this profile actually still exists on the server.", ImGuiColors.DalamudYellow);
            }

            var yesButton = "Yes, Force Delete Local Data";
            var noButton = "Cancel";
            var yesSize = CkGui.GetButtonSize(yesButton);
            var noSize = CkGui.GetButtonSize(noButton);

            ImGui.SetCursorPosY(size.Y - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y);
            CkGui.SetCursorXtoCenter(yesSize + noSize + ImUtf8.ItemSpacing.X);

            if (CkGui.ButtonEx(yesButton, CkCol.TriStateCross.Uint()))
            {
                UiService.SetUITask(() => CleanupLocalProfileData(profile));
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (CkGui.ButtonEx(noButton))
                ImGui.CloseCurrentPopup();
        }
    }

    private async Task RemoveProfileAndReload(AccountProfile profile)
    {
        try
        {
            if (MainHub.ServerStatus is ServerState.Connected or ServerState.ConnectedDataSynced)
            {
                _logger.LogInformation("Attempting to delete Account from Server.");
                await _hub.UserDelete();
            }
            else
            {
                throw new InvalidOperationException("Not connected to the server.");
            }

            // Properly remove the local data due to a successful removal.
            await CleanupLocalProfileData(profile);
            _deletePopupState = DeletePopupState.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Server deletion failed: {ex.Message}.");
            _deletePopupState = DeletePopupState.ServerFailed;
        }
    }

    private async Task CleanupLocalProfileData(AccountProfile profile)
    {
        var isMain = profile.IsPrimary;
        var accountUids = MainHub.ConnectionResponse?.AccountProfileUids ?? [];

        // Remove the authentications for the profile, or all profiles, if main.
        _logger.LogInformation("Removing Authentication for current character.");
        _account.Profiles.Remove(profile);
        if (isMain)
        {
            _logger.LogInformation("Removed Primary Profile, clearing all other profiles.");
            _account.Profiles.Clear();
        }

        _account.Save();

        // Update the profile folder directories for cleanup.
        if (isMain)
        {
            var toDelete = Directory.GetDirectories(GsFiles.ConfigDirectory)
                .Where(d => accountUids.Contains(d, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var folder in toDelete)
                Directory.Delete(folder, true);

            _logger.LogInformation("Removed all deleted profile-related folders.");
            _connections.SetCurrentProfile(string.Empty);

            await _hub.Disconnect(ServerState.Disconnected, DisconnectIntent.Reload);
            _mainConfig.Data.ButtonUsed = false;
            _mediator.Publish(new SwitchToIntroUiMessage());
        }
        else
        {
            var toDelete = _connections.CurrentProfileUID;
            if (Directory.Exists(toDelete))
            {
                _logger.LogDebug("Deleting Config Folder for removed profile.", LogFilter.MainHub);
                Directory.Delete(toDelete, true);
            }
            _connections.SetCurrentProfile(string.Empty);
            await _hub.Reconnect(DisconnectIntent.Reload);
        }

        // Refresh the tracked players and clear the selection.
        if (_selected == profile)
            _selected = null;
    }
    #endregion
}
