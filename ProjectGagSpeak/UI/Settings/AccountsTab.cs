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
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using OtterGui.Text;
using OtterGuiInternal;

namespace GagSpeak.Gui;

public class AccountManagerTab
{
    private readonly ILogger<AccountManagerTab> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly MainConfig _mainConfig;
    private readonly AccountManager _account;
    private readonly KinkPlateService _kinkPlates;
    private readonly ConfigFileProvider _fileProvider;

    private readonly Queue<Action> _postDrawActions = new();
    private AccountProfile? _exposedKeyProfile = null;
    private AccountProfile? _inEditMode = null;

    public AccountManagerTab(ILogger<AccountManagerTab> logger, GagspeakMediator mediator,
        MainHub hub, MainConfig config, AccountManager account, KinkPlateService kinkPlates, ConfigFileProvider fileProvider)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _mainConfig = config;
        _account = account;
        _kinkPlates = kinkPlates;
        _fileProvider = fileProvider;
    }

    // Cached Internal Helpers (May change overtime.)
    private AccountProfile? _selected = null;
    private AccountProfile? _editingSecretKey = null;
    private AccountProfile? _showingKey = null;

    private float GetAvatarScaleRatio(float width)
    {
        var baseWidth = ImGuiHelpers.ScaledVector2(154).X;
        return width <= baseWidth ? 1f : width / baseWidth;
    }

    // Updates the cached style references for this frame.
    private void InitStyle()
    {
        _wdl = ImGui.GetWindowDrawList();
        _style = ImGui.GetStyle();
        _frameH = ImUtf8.FrameHeight;
        _frameHSpacingWidth = ImUtf8.FrameHeight + ImUtf8.ItemInnerSpacing.X;

        _ckFrameCol = CkColor.VibrantPink.Uint();
        _clientCol = CkColor.ElementSplit.Uint(); // gold if we need it?

        _txtCol = ImGui.GetColorU32(ImGuiCol.Text);
        _txtDisableCol = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        _frameBgCol = ImGui.GetColorU32(ImGuiCol.FrameBg);
        _frameBgHoverCol = ImGui.GetColorU32(ImGuiCol.FrameBgHovered);

        _bendS = _style.FrameRounding * 1.25f;
        _bendM = _style.FrameRounding * 1.75f;
        _bendL = _style.FrameRounding * 2f;
        _bendXL = _style.FrameRounding * 3f;

        _shadowSize = ImGuiHelpers.ScaledVector2(1);
        _styleOffset = ImGuiHelpers.ScaledVector2(2);
        _buttonPadding = _styleOffset + _style.FramePadding;

        _playerNodeH = ImUtf8.TextHeightSpacing + ImUtf8.TextHeight + ImUtf8.FramePadding.Y * 2 + _styleOffset.Y * 2;
    }

    private ImDrawListPtr _wdl;
    private ImGuiStylePtr _style;

    private float _frameH;
    private float _frameHSpacingWidth;

    private uint _ckFrameCol;
    private uint _clientCol;

    private uint _txtCol;
    private uint _txtDisableCol;
    private uint _frameBgCol;
    private uint _frameBgHoverCol;

    private float _bendS;
    private float _bendM;
    private float _bendL;
    private float _bendXL;
    private Vector2 _shadowSize;
    private Vector2 _styleOffset;
    private Vector2 _buttonPadding;

    private float _playerNodeH;
    private float _lineH => 5 * ImGuiHelpers.GlobalScale;

    // Cached profile display data. (Size is deterministic of other factors).
    private float Ratio { get; set; } = 1f;
    private Vector2 ProfileSizeBase => ImGuiHelpers.ScaledVector2(154);
    private Vector2 ProfileSize => ImGuiHelpers.ScaledVector2(154 * Ratio);
    private Vector2 RectMin { get; set; } = Vector2.Zero;
    private Vector2 AvatarPos => RectMin + ImGuiHelpers.ScaledVector2(4.2f * Ratio);
    private Vector2 AvatarSize => ImGuiHelpers.ScaledVector2(145.6f * Ratio); // Default

    public void DrawContent()
    {
        InitStyle();
        // Immidiately get the drawlist, position, size, and area available for drawing.
        var pos = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail();
        var max = pos + size;
        var halfY = pos with { Y = pos.Y + size.Y / 2f };

        // Draw out the scalable background style, then fill it with a backdrop.
        // (can reference the actual profile instead once we reference the profile and not the index.
        if (CosmeticService.TryGetBackground(PlateElement.Plate, KinkPlateBG.Default, out var plateBG))
            _wdl.AddDalamudImageRounded(plateBG, pos, size, _bendS);
        _wdl.AddRectFilledMultiColor(halfY, max, uint.MinValue, uint.MinValue, 0x44000000, 0x44000000);

        // Now border this with the color framing.
        using (var _ = CkRaii.FramedChildPaddedWH("Account", size, 0, _ckFrameCol, _bendM, wFlags: WFlags.NoScrollbar))
        {
            DrawProfileList(_.InnerRegion.Y);
            ImGui.SameLine();
            DrawProfilePanel(ImGui.GetContentRegionAvail());
        }

        // Perform any post-draw actions we need to.
        while (_postDrawActions.TryDequeue(out Action? action))
        {
            // Safely execute each post-draw action until the queue is empty.
            Generic.Safe(() => action());
        }
    }

    // Profile Elements -/- Components.

    // Draws out all elements in the list, along with a add and remove profile button.
    private static float ProfileListWidth => 150f * ImGuiHelpers.GlobalScale;
    private void DrawProfileList(float height)
    {
        // The Profile list in itself is a child (padding)
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, ImGui.GetStyle().WindowPadding * .75f);
        using var _ = CkRaii.FramedChildPaddedH("profile-list", ProfileListWidth, height, 0, _ckFrameCol, _bendL);

        // ProfileSelection is itself a nested child (no padding)
        var listSize = _.InnerRegion - new Vector2(0, (CkGui.GetFancyButtonHeight() + ImUtf8.ItemSpacing.Y));
        using (CkRaii.Child("profiles", listSize, wFlags: WFlags.NoScrollbar))
        {
            var size = new Vector2(listSize.X, ImUtf8.FrameHeight + ImUtf8.TextHeightSpacing);
            foreach (var profile in _account.Profiles)
            {
                if (SelectableProfile(profile, size))
                    SetSelectedProfile(profile);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    _selected = null;
                    _editingSecretKey = null;
                    _showingKey = null;
                }
            }
        }

        // Draw the remove button.
        if (CkGui.FancyButton(FAI.Minus, GSLoc.Settings.Accounts.DeleteButtonLabel, listSize.X, (!ImGui.GetIO().KeyCtrl || !ImGui.GetIO().KeyShift)))
        {
            if (_selected is not null)
            {
                if (_selected.HadValidConnection)
                    ImGui.OpenPopup("Delete Account Confirmation");
                else
                {
                    _postDrawActions.Enqueue(() =>
                    {
                        // We can just remove it plainly as it is not bound to any profile serverside.
                        _account.Profiles.Remove(_selected);
                        _account.Save();
                    });
                }
            }
        }
        CkGui.AttachToolTip(GSLoc.Settings.Accounts.DeleteButtonTT);

        // Fire if true.
        AccountDeletionPopup(_selected);
    }

    #region DEBUGGER
    public static void DumpButtonColors()
    {
        var states = new[]
        {
            (active: false, hovered: false, disabled: false, name: "Idle"),
            (active: false, hovered: true,  disabled: false, name: "Hovered"),
            (active: true,  hovered: false, disabled: false, name: "Active"),
            (active: false, hovered: false, disabled: true,  name: "Disabled"),
        };

        foreach (var s in states)
        {
            uint shadowCol = 0x64000000;
            uint borderCol = CkGui.ApplyAlpha(0xDCDCDCDC, CkGui.GetBorderAlpha(s.active, s.hovered, s.disabled));
            uint bgCol = CkGui.ApplyAlpha(0x64000000, CkGui.GetBgAlpha(s.active, s.hovered, s.disabled));
            uint textFade = CkGui.ApplyAlpha(0xFF1E191E, s.disabled ? 0.5f : 1f);
            uint textCol = CkGui.ApplyAlpha(0xFFFFFFFF, s.disabled ? 0.5f : 1f);

            LogColors(
                s.name,
                shadowCol,
                borderCol,
                bgCol,
                textFade,
                textCol
            );
        }

        void LogColors(string state, uint shadow, uint border, uint bg, uint textFade, uint text)
        {
            static string Hex(uint v) => $"0x{v:X8}";
            Svc.Logger.Information($"[{state}][Shadow: {Hex(shadow)}][Border: {Hex(border)}][BG: {Hex(bg)}][TextFade: {Hex(textFade)}][Text: {Hex(text)}]");
        }
    }
    #endregion DEBUGGER

    // Shift these stylizations to be calculated prior to our draws so we can use them throughout the drawframe without calculating every time.
    private bool SelectableProfile(AccountProfile profile, Vector2 size)
    {
        var window = ImGuiInternal.GetCurrentWindow();
        if (window.SkipItems)
            return false;

        // Aquire our ID for this new internal item.
        var id = ImGui.GetID(profile.ContentId.ToString());

        // Get the position and styles for our draw-space.
        var pos = window.DC.CursorPos;

        // Get the offsets and true height.
        var trueH = size.Y + _styleOffset.Y * 2;

        // Aquire a valid bounding box for this button interaction
        var itemSize = new Vector2(size.X, trueH);
        var hitbox = new ImRect(pos, pos + itemSize);
        var drawArea = new ImRect(hitbox.Min + _buttonPadding, hitbox.Max - _buttonPadding);

        // Add the item to ImGuiInternal via ImGuiP for direct integration.
        // (Note that the 2nd paramater tells us how far to shift for the text)
        ImGuiInternal.ItemSize(itemSize, _style.FramePadding.Y + _styleOffset.Y);
        if (!ImGuiP.ItemAdd(hitbox, id, null))
            return false;

        // Process interactions for our created 'Button'
        var hovered = false;
        var active = false;
        var clicked = ImGuiP.ButtonBehavior(hitbox, id, ref hovered, ref active);

        // Define our colors based on states. (Update with static values later)
        uint shadowCol = 0x64000000;
        uint borderCol = CkGui.ApplyAlpha(0xDCDCDCDC, active ? 0.7f : hovered ? 0.63f : 0.39f);
        uint bgCol = CkGui.ApplyAlpha(0x64000000, active ? 0.19f : hovered ? 0.26f : 0.39f);

        // (Picture draw order like placing sticky notes on our monitor, stacking them towards us)
        window.DrawList.AddRectFilled(hitbox.Min, hitbox.Max, shadowCol, _bendM, ImDrawFlags.RoundCornersAll);
        // Draw over with inner border, greyish look.
        window.DrawList.AddRectFilled(hitbox.Min + _shadowSize, hitbox.Max - _shadowSize, borderCol, _bendM, ImDrawFlags.RoundCornersAll);
        // Draw over again with the bgColor.
        window.DrawList.AddRectFilled(hitbox.Min + _styleOffset, hitbox.Max - _styleOffset, bgCol, _bendM, ImDrawFlags.RoundCornersAll);


        // Allow for 'ImGui.IsItemHovered' to be reconized by this hitbox.
        ImGuiP.RenderNavHighlight(hitbox, id);

        // Now we need to draw out the actual contents within this area.
        var iconSize = CkGui.IconSize(FAI.CheckCircle);
        var txtSize = ImGui.CalcTextSize(profile.PlayerName);
        var innerClip = new ImRect(hitbox.Min + _styleOffset, new Vector2(hitbox.Max.X - iconSize.X - _style.ItemSpacing.X * 2 - _styleOffset.X, hitbox.Max.Y - _styleOffset.Y));

        ImGuiInternal.RenderTextClipped(window.DrawList, drawArea.Min, drawArea.Max, profile.PlayerName, Vector2.Zero, txtSize, innerClip, true);

        var iconPosTR = new Vector2(drawArea.Max.X - iconSize.X, drawArea.Min.Y);
        var iconPosBL = new Vector2(drawArea.Min.X, drawArea.Min.Y + ImUtf8.TextHeightSpacing);
        using (Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            window.DrawList.AddText(FAI.CheckCircle.ToIconString(), iconPosTR, profile.HadValidConnection ? CkColor.TriStateCheck.Uint() : _frameBgHoverCol);
        if (ImGui.IsMouseHoveringRect(iconPosTR, iconPosTR + iconSize))
            CkGui.ToolTipInternal(profile.HadValidConnection ? "Had a Successful Connection & Aquired UID." : "Profile has not yet connected to the server.");

        using (Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            window.DrawList.AddText(FAI.IdCard.ToIconString(), iconPosBL, ImGuiColors.DalamudGrey.ToUint());

        window.DrawList.AddText(iconPosBL + new Vector2(iconSize.X + _style.ItemInnerSpacing.X, 0),
            profile.HadValidConnection ? ImGuiColors.DalamudGrey2.ToUint() : ImGuiColors.DalamudRed.ToUint(),
            profile.HadValidConnection ? profile.UserUID : "No UID Assigned");
        return clicked;
    }

    /// <summary>
    ///     Draws out the content for the currently selected profile, and the lower, registered players area.
    /// </summary>
    private void DrawProfilePanel(Vector2 region)
    {
        // Outer group
        using var _ = ImRaii.Child("profile-panel", region);
        var cursorMin = ImGui.GetCursorPos();
        var leftGapY = 0f;
        var rightGapY = 0f;
        // If no profile is selected, just draw nothing is selected and return.
        if (_selected is not { } profile)
        {
            CkGui.FontText("No Profile Selected", UiFontService.UidFont);
            return;
        }
        // We need to determine the maximum left width to know how to scale the profile image.
        var maxLeft = _frameH + CkGui.IconButtonSize(FAI.Edit).X + _style.ItemInnerSpacing.X * 2;
        using (ImRaii.PushFont(UiBuilder.MonoFont)) maxLeft += ImGui.CalcTextSize(new string('*', 32)).X;

        // Check if we exceed the maximum left side or not when compared against the base avatar size, which determines if we scale it.
        var exceedsLeftMax = region.X - ProfileSizeBase.X - _style.ItemSpacing.X > maxLeft;
        var leftWidth = exceedsLeftMax ? maxLeft : region.X - ProfileSizeBase.X - _style.ItemSpacing.X;

        using (ImRaii.Group())
        {
            CkGui.FontText(profile.PlayerName, UiFontService.UidFont);
            var lineSize = new Vector2(leftWidth, _lineH);
            _wdl.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.AchievementLineSplit], ImGui.GetCursorScreenPos(), lineSize);
            ImGui.Dummy(lineSize);
            DrawLabelUidAndKey(profile, leftWidth);
            leftGapY = ImGui.GetCursorPosY() + _style.ItemSpacing.Y;
        }

        // We're not doing anything particularly fancy with the avatar here
        ImGui.NewLine();

        //// Take the leftmax, subtract lineH, and divide by 2, this will be how it divides the splits.
        //var maxY = cursorMin.Y + region.Y;
        //var playerDrawRegionsH = (maxY - leftGapY - _lineH) / 2;

        //// Get the Y pos of the line, by cursorMinY + region.Y - lowerHalfY
        //var splitLinePosY = maxY - playerDrawRegionsH - _lineH;

        //// Ensure the ratio does not exceed the max allowed height.
        //var maxRatioValue = (splitLinePosY - cursorMin.Y) / ProfileSizeBase.Y;
        //Ratio = exceedsLeftMax ? Math.Clamp(GetAvatarScaleRatio(ImGui.GetContentRegionAvail().X), 1f, maxRatioValue) : 1f;

        // Correctly draw the profile.
        DrawAvatar(profile);
        //var rightPos = ImGui.GetCursorScreenPos() + new Vector2(0, ProfileSize.Y);
        //// Using the cursorPos from min, add the scaled ProfileSizeY to get the YPos.
        //rightGapY = cursorMin.Y + ProfileSize.Y + _style.ItemSpacing.Y;

        //// Determine the height on the left via LinePosY - LeftHeightEndY
        //var leftTopAvailH = splitLinePosY - leftGapY;
        //// Determine the height on the right by using LinePosY - ProfilePosY
        //var rightTopAvailH = splitLinePosY - rightGapY;

        //var leftOverRight = (leftTopAvailH >= rightTopAvailH * 1.5f);
        //var canFitOnRight = (rightTopAvailH >= (_playerNodeH));
        //var shouldDrawLeftBox = leftOverRight || !canFitOnRight;
        //// Get the deterministic dimentions.
        //var linkedWidth = shouldDrawLeftBox ? leftWidth : region.X;
        //var newPos = shouldDrawLeftBox ? new Vector2(cursorMin.X, leftGapY) : new Vector2(cursorMin.X, rightGapY);
    }

    private void DrawLabelUidAndKey(AccountProfile profile, float width)
    {
        var showEditor = _editingSecretKey == profile;
        var showKey = _showingKey == profile;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);

        CkGui.FramedIconText(FAI.Globe);
        CkGui.TextFrameAlignedInline("World:");
        // i am positive there is a lookup for worldid > name... and so i search
        CkGui.ColorTextFrameAlignedInline(ItemSvc.WorldData[profile.WorldId], ImGuiColors.TankBlue);

        CkGui.FramedIconText(FAI.IdBadge);
        CkGui.TextFrameAlignedInline("UID:");
        var noUid = string.IsNullOrEmpty(profile.UserUID);
        CkGui.ColorTextFrameAlignedInline(noUid ? "Not Yet Assigned" : profile.UserUID, noUid ? ImGuiColors.DalamudRed : ImGuiColors.TankBlue);
        CkGui.AttachToolTip("Once you successfully connect with the inserted secret key below, your UID will be set!");

        CkGui.FramedHoverIconText(FAI.Key, ImGuiColors.TankBlue.ToUint());
        CkGui.AttachToolTip(GSLoc.Settings.Accounts.CharaKeyLabel);
        if (ImGui.IsItemClicked())
            _showingKey = _showingKey == profile ? null : profile;

        // Draw based on what should be displayed.
        ImUtf8.SameLineInner();
        var innerWidth = width - _frameHSpacingWidth - CkGui.IconButtonSize(FAI.PenSquare).X;

        if (showEditor)
        {
            ImGui.SetNextItemWidth(innerWidth);
            var key = profile.Key;
            if (ImGui.InputTextWithHint("##KeyEditor", "Paste SecretKey Here...", ref key, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                key = key.Trim();
                //Fail if the key already exists.
                if (_account.TryUpdateSecretKey(profile, key))
                    _logger.LogDebug($"Updated SecretKey for {profile.PlayerName}");
                // exit edit mode.
                _editingSecretKey = null;
            }
        }
        else
        {
            var pos = ImGui.GetCursorScreenPos();
            var txtSize = new Vector2(innerWidth, ImUtf8.TextHeight);
            var txtRect = new ImRect(pos, pos + txtSize);
            var txt = showKey ? profile.Key : new string('*', Math.Clamp(profile.Key.Length, 0, 32));
            ImGuiInternal.RenderTextClipped(_wdl, txtRect.Min + _style.FramePadding, txtRect.Max - _style.FramePadding, txt, Vector2.Zero, txtSize, txtRect, true);
            ImGui.Dummy(txtSize);
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.CopyKeyToClipboard);
            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(profile.Key);
        }
        // Add the edit button.
        if (profile.HadValidConnection)
        {
            ImGui.SameLine(width - ImUtf8.FrameHeight, 0);
            CkGui.FramedIconText(FAI.CheckCircle);
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.EditKeyNotAllowed);
        }
        else
        {
            ImGui.SameLine(width - CkGui.IconButtonSize(FAI.PenSquare).X, 0);
            if (CkGui.IconButton(FAI.PenSquare, inPopup: true))
                _editingSecretKey = showEditor ? null : profile;
            CkGui.AttachToolTip(GSLoc.Settings.Accounts.EditKeyAllowed);
        }
    }

    // Scalable avatar display, the other measurements should adapt to this scale.
    private void DrawAvatar(AccountProfile profile)
    {
        if (!MainHub.IsConnectionDataSynced || !profile.HadValidConnection)
            return;

        var profileData = _kinkPlates.GetKinkPlate(new(profile.UserUID));
        var avatar = profileData.GetProfileOrDefault();
        RectMin = ImGui.GetCursorScreenPos();
        // Draw out the avatar image.
        _wdl.AddDalamudImageRounded(avatar, AvatarPos, AvatarSize, AvatarSize.Y / 2);
        // draw out the border for the profile picture
        if (CosmeticService.TryGetBorder(PlateElement.Avatar, profileData.Info.AvatarBorder, out var pfpBorder))
            _wdl.AddDalamudImageRounded(pfpBorder, RectMin, ProfileSize, ProfileSize.Y / 2);
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

    public void AccountDeletionPopup(AccountProfile? profile)
    {
        if (profile is null)
            return;

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
            CkGui.FontTextCentered("WARNING", UiFontService.UidFont, ImGuiColors.DalamudRed);
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
            CkGui.TextInline("All Configured Permissions");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("Your Paired Users");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("Your uploaded SMA Protected Data");
            CkGui.IconText(FAI.ArrowRight);
            CkGui.TextInline("Saved Achievement Data");
        }
        var yesButton = $"I Understand, Delete Profile for {profile.PlayerName}({profile.UserUID})";
        var noButton = "Uhh... Take me back!";
        var yesSize = ImGuiHelpers.GetButtonSize(yesButton);
        var noSize = ImGuiHelpers.GetButtonSize(noButton);
        var offsetX = (size.X - (yesSize.X + noSize.X + ImUtf8.ItemSpacing.X).RemoveWinPadX()) / 2;
        CkGui.SeparatorSpaced();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);
        using (ImRaii.Disabled(!(ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl)))
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
        var isMain = profile.IsPrimary;
        // Remove the profile from the account config.
        try
        {
            _logger.LogInformation("Removing Authentication for current character.");
            _account.Profiles.Remove(profile);
            // The server automatically handles cleanup of alt profiles, so just clear the manager.
            if (isMain)
            {
                _logger.LogInformation("Removed Primary Profile, removing all other profiles.");
                _account.Profiles.Clear();
            }

            // Update the last logged in UID.
            _mainConfig.Current.LastUidLoggedIn = string.Empty;
            _mainConfig.Save();

            // Extract the UID's so that we know what folders to delete in our config. (If we want to, we could keep them as a backup, idk)
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

                _logger.LogInformation("Removed all deleted profile-related folders.");
                // Cleanup the remaining UID's
                _fileProvider.ClearUidConfigs();
                // Fully disconnect and switch back to the intro UI.
                await _hub.Disconnect(ServerState.Disconnected, DisconnectIntent.Reload);
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
                await _hub.Reconnect(DisconnectIntent.Reload);
            }
        }
        catch (Bagagwa ex)
        {
            _logger.LogError("Failed to delete account from server." + ex);
        }
    }
}

/*
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
} */
