using Dalamud.Interface.Colors;
using Dalamud.Utility;
using GagSpeak.Achievements;
using GagSpeak.CkCommons.Gui;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using Penumbra.GameData.Enums;
using System.Globalization;

namespace GagSpeak.CkCommons.Gui.Profile;
public partial class KinkPlateUI : WindowMediatorSubscriberBase
{
    private readonly PairManager _pairManager;
    private readonly KinkPlateService _profileService;
    private readonly CosmeticService _cosmetics;
    private readonly TextureService _textures;

    private bool ThemePushed = false;
    public KinkPlateUI(ILogger<KinkPlateUI> logger, GagspeakMediator mediator,
        PairManager pairManager, KinkPlateService profileService, CosmeticService cosmetics,
        TextureService textureService, Pair pair) 
        : base(logger, mediator, pair.UserData.AliasOrUID + "'s KinkPlate##GagspeakKinkPlateUI" + pair.UserData.AliasOrUID)
    {
        _pairManager = pairManager;
        _profileService = profileService;
        _cosmetics = cosmetics;
        _textures = textureService;
        Pair = pair;

        Flags = WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoTitleBar;
        Size = new Vector2(750, 450);
        IsOpen = true;
    }

    private bool HoveringCloseButton { get; set; } = false;
    public Pair Pair { get; init; } // The pair this profile is being drawn for.
    private string DisplayName => Pair.UserData.AliasOrUID;
    private string PairUID => Pair.UserData.UID;

    private static Vector4 Gold = new Vector4(1f, 0.851f, 0.299f, 1f);

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 25f);

            ThemePushed = true;
        }
    }
    protected override void PostDrawInternal()
    {
        // include our personalized theme for this window here if we have themes enabled.
        if (ThemePushed)
        {
            ImGui.PopStyleVar(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        var wdl = ImGui.GetWindowDrawList();
        RectMin = wdl.GetClipRectMin();
        RectMax = wdl.GetClipRectMax();
        //_logger.LogDebug("RectMin: {rectMin}, RectMax: {rectMax}", rectMin, rectMax);

        // obtain the profile for this userPair.
        var KinkPlate = _profileService.GetKinkPlate(Pair.UserData);

        // Draw KinkPlateUI Function here.
        DrawKinkPlatePair(wdl, KinkPlate);
    }

    // Size = 750 by 450
    private void DrawKinkPlatePair(ImDrawListPtr wdl, KinkPlate profile)
    {
        DrawPlate(wdl, profile.KinkPlateInfo);

        DrawProfilePic(wdl, profile);

        DrawIconSummary(wdl, profile);

        DrawDescription(wdl, profile);

        // Now let's draw out the chosen achievement Name..
        using (UiFontService.GagspeakTitleFont.Push())
        {
            var titleName = AchievementManager.GetTitleById(profile.KinkPlateInfo.ChosenTitleId);
            var titleHeightGap = TitleLineStartPos.Y - (RectMin.Y + 4f);
            var chosenTitleSize = ImGui.CalcTextSize(titleName);
            // calculate the Y height it should be drawn on by taking the gap height and dividing it by 2 and subtracting the text height.
            var yHeight = (titleHeightGap - chosenTitleSize.Y) / 2;

            ImGui.SetCursorScreenPos(new Vector2(TitleLineStartPos.X + TitleLineSize.X / 2 - chosenTitleSize.X / 2, TitleLineStartPos.Y - chosenTitleSize.Y - yHeight));
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedGold, titleName);
        }
        // move over to the top area to draw out the achievement title line wrap.
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.AchievementLineSplit], TitleLineStartPos, TitleLineSize);

        DrawGagInfo(wdl, profile.KinkPlateInfo);

        DrawStats(wdl, profile.KinkPlateInfo);

        DrawBlockedSlots(wdl, profile.KinkPlateInfo);
    }

    private void DrawPlate(ImDrawListPtr wdl, KinkPlateContent info)
    {
        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.Plate, info.PlateBackground, out var plateBG))
            wdl.AddDalamudImageRounded(plateBG, RectMin, PlateSize, 25f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.Plate, info.PlateBorder, out var plateBorder))
            wdl.AddDalamudImageRounded(plateBorder, RectMin, PlateSize, 20f);

        // Draw the close button.
        CloseButton(wdl);
        CkGui.AttachToolTipRect(CloseButtonPos, CloseButtonSize, "Close " + DisplayName + "'s KinkPlate™");
    }

    private void DrawProfilePic(ImDrawListPtr wdl, KinkPlate profile)
    {
        // We should always display the default GagSpeak Logo if the profile is either flagged or disabled.
        if (profile.TempDisabled)
        {
            wdl.AddDalamudImageRounded(_cosmetics.CoreTextures[CoreTexture.Icon256Bg], ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
        }
        else // But otherwise can draw normal image.
        {
            var pfpWrap = profile.GetCurrentProfileOrDefault();
            wdl.AddDalamudImageRounded(pfpWrap, ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
        }

        // draw out the border for the profile picture
        if (_cosmetics.TryGetBorder(ProfileComponent.ProfilePicture, profile.KinkPlateInfo.ProfilePictureBorder, out var pfpBorder))
            wdl.AddDalamudImageRounded(pfpBorder, ProfilePictureBorderPos, ProfilePictureBorderSize, ProfilePictureSize.Y / 2);

        // Draw out Supporter Icon Black BG base.
        wdl.AddCircleFilled(SupporterIconBorderPos + SupporterIconBorderSize / 2,
            SupporterIconBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));

        // Draw out Supporter Icon.
        var supporterInfo = _cosmetics.GetSupporterInfo(Pair.UserData);
        if (supporterInfo.SupporterWrap is { } wrap)
            wdl.AddDalamudImageRounded(wrap, SupporterIconPos, SupporterIconSize, SupporterIconSize.Y / 2, supporterInfo.Tooltip);
        // Draw out the border for the icon.
        wdl.AddCircle(SupporterIconBorderPos + SupporterIconBorderSize / 2, SupporterIconBorderSize.X / 2,
            ImGui.GetColorU32(ImGuiColors.ParsedPink), 0, 4f);


        // draw out the UID here. We must make it centered. To do this, we must fist calculate how to center it.
        var widthToCenterOn = ProfilePictureBorderSize.X;
        // determine the height gap between the icon overview and bottom of the profile picture.
        var gapHeight = IconOverviewListPos.Y - (ProfilePictureBorderPos.Y + ProfilePictureBorderSize.Y);
        var ttText = DisplayName == PairUID ? "This Pairs UID" : "This Pairs Alias --SEP-- Their UID is: " + PairUID;
        using (UiFontService.UidFont.Push())
        {
            var aliasOrUidSize = ImGui.CalcTextSize(DisplayName);
            var yHeight = (gapHeight - aliasOrUidSize.Y) / 2;

            ImGui.SetCursorScreenPos(new Vector2(ProfilePictureBorderPos.X + widthToCenterOn / 2 - aliasOrUidSize.X / 2, ProfilePictureBorderPos.Y + ProfilePictureBorderSize.Y + yHeight));
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedPink, DisplayName);
        }
        CkGui.AttachToolTip(ttText);
        CkGui.CopyableDisplayText(Pair.UserData.UID);
    }

    private void DrawIconSummary(ImDrawListPtr wdl, KinkPlate profile)
    {
        var iconWidthPlusSpacing = 38;
        var iconOverviewPos = IconOverviewListPos;

        // draw out the icon row. For each item, we will first determine the color, and its tooltip text.
        var vibeColor = Pair.PairGlobals.ToysAreConnected ? Gold : ImGuiColors.DalamudGrey3;
        var vibeTT = Pair.PairGlobals.ToysAreConnected
            ? DisplayName + " has a Sex Toy connected and active."
            : DisplayName + " does not have any Sex Toys connected and active.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Vibrator], iconOverviewPos, Vector2.One * 34, vibeColor, vibeTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var shockColor = Pair.PairGlobals.HasValidShareCode() ? Gold : ImGuiColors.DalamudGrey3;
        var shockTT = Pair.PairGlobals.HasValidShareCode()
            ? DisplayName + " is connected to their Shock Collar while in Hardcore Mode."
            : DisplayName + " has not connected a Shock Collar.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.ShockCollar], iconOverviewPos, Vector2.One * 34, shockColor, shockTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var forcedFollowColor = !Pair.PairGlobals.ForcedFollow.NullOrEmpty() ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var forcedFollowTT = !Pair.PairGlobals.ForcedFollow.NullOrEmpty()
            ? DisplayName + " is being leashed around by another pair while in Hardcore Mode."
            : DisplayName + " is not following anyone.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Leash], iconOverviewPos, Vector2.One * 34, forcedFollowColor, forcedFollowTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var forcedEmoteColor = !Pair.PairGlobals.ForcedEmoteState.NullOrEmpty() ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var forcedEmoteTT = !Pair.PairGlobals.ForcedEmoteState.NullOrEmpty()
            ? DisplayName + " is being put on display for another pair while in Hardcore Mode."
            : DisplayName + " is not on display for anyone.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.ForcedEmote], iconOverviewPos, Vector2.One * 34, forcedEmoteColor, forcedEmoteTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var forcedStayColor =!Pair.PairGlobals.ForcedStay.NullOrEmpty() ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var forcedStayTT = !Pair.PairGlobals.ForcedStay.NullOrEmpty()
            ? DisplayName + " has been ordered to stay put for another pair while in Hardcore Mode."
            : DisplayName + " has not been ordered to stay put by anyone.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.ForcedStay], iconOverviewPos, Vector2.One * 34, forcedStayColor, forcedStayTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var chatManipulated = Pair.PairGlobals.IsChatHidden() || Pair.PairGlobals.IsChatInputHidden() || Pair.PairGlobals.IsChatInputBlocked();
        var chatBlockedColor = chatManipulated ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var chatBlockedTT = chatManipulated
            ? DisplayName + " is having their chat access restricted by another pair while in Hardcore Mode."
            : DisplayName + " is not under any chat restrictions.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.ChatBlocked], iconOverviewPos, Vector2.One * 34, chatBlockedColor, chatBlockedTT);
    }

    private void DrawDescription(ImDrawListPtr wdl, KinkPlate profile)
    {
        // draw out the description background.
        if (_cosmetics.TryGetBackground(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionBackground, out var descBG))
            wdl.AddDalamudImageRounded(descBG, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description border
        if (_cosmetics.TryGetBorder(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionBorder, out var descBorder))
            wdl.AddDalamudImageRounded(descBorder, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description overlay.
        if (_cosmetics.TryGetOverlay(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionOverlay, out var descOverlay))
            wdl.AddDalamudImageRounded(descOverlay, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // draw out the description text here. What displays is affected by if it is flagged or not.
        ImGui.SetCursorScreenPos(DescriptionBorderPos + Vector2.One * 10f);
        // shadowban them by displaying the default text if flagged or disabled.
        var description = profile.TempDisabled ? "Profile is currently disabled."
            : profile.KinkPlateInfo.Description.IsNullOrEmpty()
            ? "No Description Was Set.." : profile.KinkPlateInfo.Description;
        var color = (profile.KinkPlateInfo.Description.IsNullOrEmpty() || profile.TempDisabled) 
            ? ImGuiColors.DalamudGrey2 : ImGuiColors.DalamudWhite;
        DrawLimitedDescription(description, color, DescriptionBorderSize - Vector2.One * 12f);
    }

    private void DrawGagInfo(ImDrawListPtr wdl, KinkPlateContent info)
    {
        // Draw out the background for the gag layer one item.
        if (_cosmetics.TryGetBackground(ProfileComponent.GagSlot, info.GagSlotBackground, out var gagSlotBG))
        {
            wdl.AddDalamudImageRounded(gagSlotBG, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            wdl.AddDalamudImageRounded(gagSlotBG, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            wdl.AddDalamudImageRounded(gagSlotBG, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }
        else
        {
            wdl.AddRectFilled(GagSlotOneBorderPos, GagSlotOneBorderPos + GagSlotBorderSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f)), 15f);
            wdl.AddRectFilled(GagSlotTwoBorderPos, GagSlotTwoBorderPos + GagSlotBorderSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f)), 15f);
            wdl.AddRectFilled(GagSlotThreeBorderPos, GagSlotThreeBorderPos + GagSlotBorderSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f)), 15f);
        }

        // draw out the borders.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, info.GagSlotBorder, out var gagSlotBorder))
        {
            wdl.AddDalamudImageRounded(gagSlotBorder, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            wdl.AddDalamudImageRounded(gagSlotBorder, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            wdl.AddDalamudImageRounded(gagSlotBorder, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }

        // we should draw out the gag images here if valid.
        if (Pair.LastGagData is not null)
        {
            if (Pair.LastGagData.GagSlots[0].GagItem is not GagType.None)
            {
                var gagImage = _cosmetics.GetImageFromAssetsFolder("GagImages\\" + Pair.LastGagData.GagSlots[0].GagItem.GagName() + ".png" ?? $"ItemMouth\\None.png");
                wdl.AddDalamudImageRounded(gagImage, GagSlotOnePos, GagSlotSize, 10f);
            }
            if (Pair.LastGagData.GagSlots[1].GagItem is not GagType.None)
            {
                var gagImage = _cosmetics.GetImageFromAssetsFolder("GagImages\\" + Pair.LastGagData.GagSlots[1].GagItem.GagName() + ".png" ?? $"ItemMouth\\None.png");
                wdl.AddDalamudImageRounded(gagImage, GagSlotTwoPos, GagSlotSize, 10f);
            }
            if (Pair.LastGagData.GagSlots[2].GagItem is not GagType.None)
            {
                var gagImage = _cosmetics.GetImageFromAssetsFolder("GagImages\\" + Pair.LastGagData.GagSlots[2].GagItem.GagName() + ".png" ?? $"ItemMouth\\None.png");
                wdl.AddDalamudImageRounded(gagImage, GagSlotThreePos, GagSlotSize, 10f);
            }
        }

        // draw out the overlays.
        if (_cosmetics.TryGetOverlay(ProfileComponent.GagSlot, info.GagSlotOverlay, out var gagSlotOverlay))
        {
            wdl.AddDalamudImageRounded(gagSlotOverlay, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            wdl.AddDalamudImageRounded(gagSlotOverlay, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            wdl.AddDalamudImageRounded(gagSlotOverlay, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }

        // draw out the padlock backgrounds.
        if (_cosmetics.TryGetBackground(ProfileComponent.Padlock, info.PadlockBackground, out var padlockBG))
        {
            wdl.AddDalamudImageRounded(padlockBG, GagLockOneBorderPos, GagLockBorderSize, 10f);
            wdl.AddDalamudImageRounded(padlockBG, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            wdl.AddDalamudImageRounded(padlockBG, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }
        else
        {
            wdl.AddCircleFilled(GagLockOneBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
            wdl.AddCircleFilled(GagLockTwoBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
            wdl.AddCircleFilled(GagLockThreeBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
        }

        // we should draw out the lock images here if valid.
        if (Pair.LastGagData is not null)
        {
            if (Pair.LastGagData.GagSlots[0].Padlock is not Padlocks.None)
            {
                var padlockImage = _cosmetics.GetImageFromAssetsFolder("PadlockImages\\" + Pair.LastGagData.GagSlots[0].Padlock + ".png" ?? "Padlocks\\None.png");
                wdl.AddDalamudImageRounded(padlockImage, GagLockOnePos, GagLockSize, GagLockSize.X / 2);
            }
            if (Pair.LastGagData.GagSlots[1].Padlock is not Padlocks.None)
            {
                var padlockImage = _cosmetics.GetImageFromAssetsFolder("PadlockImages\\" + Pair.LastGagData.GagSlots[1].Padlock + ".png" ?? "Padlocks\\None.png");
                wdl.AddDalamudImageRounded(padlockImage, GagLockTwoPos, GagLockSize, GagLockSize.X / 2);
            }
            if (Pair.LastGagData.GagSlots[2].Padlock is not Padlocks.None)
            {
                var padlockImage = _cosmetics.GetImageFromAssetsFolder("PadlockImages\\" + Pair.LastGagData.GagSlots[2].Padlock + ".png" ?? "Padlocks\\None.png");
                wdl.AddDalamudImageRounded(padlockImage, GagLockThreePos, GagLockSize, GagLockSize.X / 2);
            }
        }

        // draw out the padlock borders.
        if (_cosmetics.TryGetBorder(ProfileComponent.Padlock, info.PadlockBorder, out var padlockBorder))
        {
            wdl.AddDalamudImageRounded(padlockBorder, GagLockOneBorderPos, GagLockBorderSize, 10f);
            wdl.AddDalamudImageRounded(padlockBorder, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            wdl.AddDalamudImageRounded(padlockBorder, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }

        // draw out the padlock overlays.
        if (_cosmetics.TryGetOverlay(ProfileComponent.Padlock, info.PadlockOverlay, out var padlockOverlay))
        {
            wdl.AddDalamudImageRounded(padlockOverlay, GagLockOneBorderPos, GagLockBorderSize, 10f);
            wdl.AddDalamudImageRounded(padlockOverlay, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            wdl.AddDalamudImageRounded(padlockOverlay, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }
    }

    private void DrawStats(ImDrawListPtr wdl, KinkPlateContent info)
    {
        // jump down to where we should draw out the stats, and draw out the achievement icon.
        var statsPos = StatsPos;
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Clock], statsPos, Vector2.One * 20, ImGuiColors.ParsedGold);
        // set the cursor screen pos to the right of the clock, and draw out the joined date.
        statsPos += new Vector2(24, 0);

        ImGui.SetCursorScreenPos(statsPos);
        var formattedDate = Pair.UserData.CreatedOn ?? DateTime.MinValue;
        var createdDate = formattedDate != DateTime.MinValue ? formattedDate.ToString("d", CultureInfo.CurrentCulture) : "MM-DD-YYYY";

        CkGui.ColorText(createdDate, ImGuiColors.ParsedGold);
        var textWidth = ImGui.CalcTextSize($"MM-DD-YYYY").X;
        statsPos += new Vector2(textWidth + 4, 0);
        // to the right of this, draw out the achievement icon.
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Achievement], statsPos, Vector2.One * 20, ImGuiColors.ParsedGold);
        // to the right of this, draw the players total earned achievements scoring.
        statsPos += new Vector2(24, 0);
        ImGui.SetCursorScreenPos(statsPos);
        CkGui.ColorText(info.CompletedAchievementsTotal + "/" + AchievementManager.Total, ImGuiColors.ParsedGold);
        CkGui.AttachToolTip("The total achievements " + DisplayName + " has earned.");
    }

    private void DrawBlockedSlots(ImDrawListPtr wdl, KinkPlateContent info)
    {
        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.BlockedSlots, info.BlockedSlotsBackground, out var lockedSlotsPanelBG))
            wdl.AddDalamudImageRounded(lockedSlotsPanelBG, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.BlockedSlots, info.BlockedSlotsBorder, out var lockedSlotsPanelBorder))
            wdl.AddDalamudImageRounded(lockedSlotsPanelBorder, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the overlay on top of that.
        if (_cosmetics.TryGetOverlay(ProfileComponent.BlockedSlots, info.BlockedSlotsOverlay, out var lockedSlotsPanelOverlay))
            wdl.AddDalamudImageRounded(lockedSlotsPanelOverlay, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the blocked causes icon row.
        var blockedAffecterPos = LockAffectersRowPos;
        var restrainedColor = Pair.LastRestraintData.Identifier.IsEmptyGuid() ? ImGuiColors.DalamudGrey3 : Gold;
        var restrainedTT = Pair.LastRestraintData.Identifier.IsEmptyGuid() ? DisplayName + " is not wearing a restraint set." : DisplayName + " has an active restraint set.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Restrained], blockedAffecterPos, LockAffecterIconSize, restrainedColor, restrainedTT);
        blockedAffecterPos.X += LockAffecterIconSize.X + LockAffecterSpacing.X;

        var mimicColor = Pair.ActiveCursedItems.Any() ? Gold : ImGuiColors.DalamudGrey3;
        var mimicTT = Pair.ActiveCursedItems.Any() ? DisplayName + " is restrained by Cursed Loot!" : DisplayName + " is not restrained with Cursed Loot.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.CursedLoot], blockedAffecterPos, LockAffecterIconSize, mimicColor, mimicTT);
        blockedAffecterPos.X += LockAffecterIconSize.X + 11f;

        var blindfoldedColor = /*Pair.PairGlobals.IsBlindfolded() && Pair.LastLightStorage?.BlindfoldItem is not null ? Gold :*/ ImGuiColors.DalamudGrey3;
        var blindfoldedTT = /*Pair.PairGlobals.IsBlindfolded() && Pair.LastLightStorage?.BlindfoldItem is not null
            ? DisplayName + " is blindfolded."
            : */DisplayName + " is not blindfolded.";
        wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Blindfolded], blockedAffecterPos, LockAffecterIconSize, blindfoldedColor, blindfoldedTT);
        // we will need to draw out all of the slot icons here from the game, based on the pairs locked slot status.
        if (!Pair.LockedSlots.IsNullOrEmpty())
        {
            var blockedSlotsPos = LockedSlotsGroupPos;
            // Iterate through each equip slot type
            foreach (var equipSlot in EquipSlotExtensions.EqdpSlots)
            {
                // Determine if the slot is locked and set the appropriate icon
                if (Pair.LockedSlots.ContainsKey(equipSlot))
                {
                    // Get the locked item icon and tooltip
                    var (ptr, textureSize, empty) = _textures.GetIcon(Pair.LockedSlots[equipSlot].Item1, equipSlot);
                    if (!empty)
                    {
                        wdl.AddImageRounded(ptr, blockedSlotsPos, blockedSlotsPos + LockedSlotSize, Vector2.Zero, Vector2.One, 0xFFFFFFFF, 15f);
                        CkGui.AttachToolTipRect(blockedSlotsPos, LockedSlotSize, Pair.LockedSlots[equipSlot].Item2);
                    }
                }
                else
                {
                    // Draw the empty icon if the slot is not locked
                    var (ptr, textureSize, empty) = _textures.GetIcon(ItemService.NothingItem(equipSlot), equipSlot);
                    if (!empty)
                        wdl.AddImageRounded(ptr, blockedSlotsPos, blockedSlotsPos + LockedSlotSize, Vector2.Zero, Vector2.One, 0xFFFFFFFF, 15f);
                }

                // Update the position for the next slot
                blockedSlotsPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
                if (blockedSlotsPos.Y >= LockedSlotsGroupPos.Y + (LockedSlotSize.Y + LockedSlotSpacing.Y) * 5) // Assuming a 5-row layout
                {
                    blockedSlotsPos.Y = LockedSlotsGroupPos.Y;
                    blockedSlotsPos.X += LockedSlotSize.X + LockedSlotSpacing.X;
                }
            }
        }

        // draw out the background for the head slot.
        if (_cosmetics.TryGetBorder(ProfileComponent.BlockedSlot, info.BlockedSlotBorder, out var blockedSlotBG))
        {
            // obtain the start position, then start drawing all of the borders at once.
            var blockedSlotBorderPos = LockedSlotsGroupPos;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
        }

        // draw out the background for the head slot.
        if (_cosmetics.TryGetOverlay(ProfileComponent.BlockedSlot, info.BlockedSlotOverlay, out var blockedSlotOverlay))
        {
            // obtain the start position, then start drawing all of the overlays at once.
            var blockedSlotOverlayPos = LockedSlotsGroupPos;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
            // scooch down and repeat.

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            wdl.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
        }

        // draw the icon list underneath that displays the hardcore traits and shit
        var hardcoreTraitsPos = HardcoreTraitsRowPos;
        // likely cache this somehow in internal calculations on updates from latest light storage.
        /*if (Pair.LastWardrobeData is not null && activeSetLight is not null && activeSetLight.HardcoreTraits.TryGetValue(Pair.LastWardrobeData.ActiveSetEnabledBy, out var traits))
        {
            if (traits.ArmsRestrained || traits.ArmsRestrained)
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.RestrainedArmsLegs], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Arms/Legs Restrained--SEP--Restricts Actions that require the use of arms/legs, whichever option is enabled.");
            else
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.RestrainedArmsLegs], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Arms/Legs Restrained");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.Gagged)
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Gagged], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Gagged--SEP--Restricts Actions that have your character shout/speak");
            else
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Gagged], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Gagged");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.Blindfolded)
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.SightLoss], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Blindfolded--SEP--Restricts Actions that require sight to be used.");
            else
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.SightLoss], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Blindfolded");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.Weighty)
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Weighty], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Weighty--SEP--With heavy bondage applied, this trait forces them to only walk.");
            else
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Weighty], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Weighty");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.StimulationLevel is not StimulationLevel.None)
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Stimulated], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Stimulated--SEP--Distracted with stimulation, you care for combat less, increasing GCD Cooldown time in proportion to arousal level.");
            else
                wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Stimulated], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Stimulated");
        }
        else*/
        {
            wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.RestrainedArmsLegs], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, "Hardcore Trait: Arms/Legs Restrained");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Gagged], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, "Hardcore Trait: Gagged");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.SightLoss], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, "Hardcore Trait: Blindfolded");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Weighty], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, "Hardcore Trait: Weighty");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            wdl.AddDalamudImage(_cosmetics.CoreTextures[CoreTexture.Stimulated], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, "Hardcore Trait: Stimulated");
        }
    }

    private void CloseButton(ImDrawListPtr wdl)
    {
        var btnPos = CloseButtonPos;
        var btnSize = CloseButtonSize;

        var closeButtonColor = HoveringCloseButton ? ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)) : ImGui.GetColorU32(ImGuiColors.ParsedPink);

        wdl.AddLine(btnPos, btnPos + btnSize, closeButtonColor, 3);
        wdl.AddLine(new Vector2(btnPos.X + btnSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + btnSize.Y), closeButtonColor, 3);


        ImGui.SetCursorScreenPos(btnPos);
        if (ImGui.InvisibleButton($"CloseButton##KinkPlateClose" + Pair.UserData.UID, btnSize))
        {
            this.IsOpen = false;
        }
        HoveringCloseButton = ImGui.IsItemHovered();
    }

    public override void OnClose()
    {
        Mediator.Publish(new RemoveWindowMessage(this));
    }
}
