using Dalamud.Interface.Colors;
using Dalamud.Utility;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;
using System.Globalization;
using GagSpeak.Kinksters;
using CkCommons.Gui;

namespace GagSpeak.Gui.Profile;
public partial class KinkPlatePreviewUI : WindowMediatorSubscriberBase
{
    private readonly KinksterManager _pairs;
    private readonly KinkPlateService _profiles;
    private readonly CosmeticService _cosmetics;
    private readonly TextureService _textures;


    private bool ThemePushed = false;
    public KinkPlatePreviewUI(ILogger<KinkPlatePreviewUI> logger, GagspeakMediator mediator,
        KinksterManager pairs, KinkPlateService kinkPlates, CosmeticService cosmetics,
        TextureService textures) : base(logger, mediator, "Own KinkPlate##PlatePreviewUI")
    {
        _pairs = pairs;
        _profiles = kinkPlates;
        _cosmetics = cosmetics;
        _textures = textures;

        Flags = WFlags.NoResize | WFlags.NoScrollbar | WFlags.NoTitleBar;
        Size = new Vector2(750, 450);
        IsOpen = false;
    }

    private bool HoveringCloseButton { get; set; } = false;

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
        var drawList = ImGui.GetWindowDrawList();
        RectMin = drawList.GetClipRectMin();
        RectMax = drawList.GetClipRectMax();

        // obtain the profile for this userPair.
        var KinkPlate = _profiles.GetKinkPlate(MainHub.PlayerUserData);

        // Draw KinkPlateUI Function here.
        DrawKinkPlate(drawList, KinkPlate);
    }

    // Size = 750 by 450
    private void DrawKinkPlate(ImDrawListPtr drawList, KinkPlate profile)
    {
        DrawPlate(drawList, profile.KinkPlateInfo);

        DrawProfilePic(drawList, profile);

        DrawIconSummary(drawList, profile);

        DrawDescription(drawList, profile);

        // Now let's draw out the chosen achievement Name..
        using (UiFontService.GagspeakTitleFont.Push())
        {
            var titleName = ClientAchievements.GetTitleById(profile.KinkPlateInfo.ChosenTitleId);
            var titleHeightGap = TitleLineStartPos.Y - (RectMin.Y + 4f);
            var chosenTitleSize = ImGui.CalcTextSize(titleName);
            // calculate the Y height it should be drawn on by taking the gap height and dividing it by 2 and subtracting the text height.
            var yHeight = (titleHeightGap - chosenTitleSize.Y) / 2;

            ImGui.SetCursorScreenPos(new Vector2(TitleLineStartPos.X + TitleLineSize.X / 2 - chosenTitleSize.X / 2, TitleLineStartPos.Y - chosenTitleSize.Y - yHeight));
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedGold, titleName);
        }
        // move over to the top area to draw out the achievement title line wrap.
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.AchievementLineSplit], TitleLineStartPos, TitleLineSize);

        DrawGagInfo(drawList, profile.KinkPlateInfo);

        DrawStats(drawList, profile.KinkPlateInfo);

        DrawBlockedSlots(drawList, profile.KinkPlateInfo);
    }

    private void DrawPlate(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.Plate, info.PlateBackground, out var plateBG))
            drawList.AddDalamudImageRounded(plateBG, RectMin, PlateSize, 25f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.Plate, info.PlateBorder, out var plateBorder))
            drawList.AddDalamudImageRounded(plateBorder, RectMin, PlateSize, 20f);

        // Draw the close button.
        CloseButton(drawList);
        CkGui.AttachToolTipRect(CloseButtonPos, CloseButtonSize, "Close KinkPlate™ Preview");
    }

    private void DrawProfilePic(ImDrawListPtr drawList, KinkPlate profile)
    {
        // We should always display the default GagSpeak Logo if the profile is either flagged or disabled.
        var pfpWrap = profile.GetCurrentProfileOrDefault();
        drawList.AddDalamudImageRounded(pfpWrap, ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);

        // draw out the border for the profile picture
        if (_cosmetics.TryGetBorder(ProfileComponent.ProfilePicture, profile.KinkPlateInfo.ProfilePictureBorder, out var pfpBorder))
            drawList.AddDalamudImageRounded(pfpBorder, ProfilePictureBorderPos, ProfilePictureBorderSize, ProfilePictureSize.Y / 2);

        // Draw out Supporter Icon Black BG base.
        drawList.AddCircleFilled(SupporterIconBorderPos + SupporterIconBorderSize / 2,
            SupporterIconBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));

        // Draw out Supporter Icon.
        var supporterInfo = _cosmetics.GetSupporterInfo(MainHub.PlayerUserData);
        if (supporterInfo.SupporterWrap is { } wrap)
            drawList.AddDalamudImageRounded(wrap, SupporterIconPos, SupporterIconSize, SupporterIconSize.Y / 2, supporterInfo.Tooltip);

        // Draw out the border for the icon.
        drawList.AddCircle(SupporterIconBorderPos + SupporterIconBorderSize / 2, SupporterIconBorderSize.X / 2, CkGui.Color(ImGuiColors.ParsedPink), 0, 4f);


        // draw out the UID here. We must make it centered. To do this, we must fist calculate how to center it.
        var widthToCenterOn = ProfilePictureBorderSize.X;
        // determine the height gap between the icon overview and bottom of the profile picture.
        var gapHeight = IconOverviewListPos.Y - (ProfilePictureBorderPos.Y + ProfilePictureBorderSize.Y);
        using (UiFontService.UidFont.Push())
        {
            var aliasOrUidSize = ImGui.CalcTextSize(MainHub.PlayerUserData.AliasOrUID);
            var yHeight = (gapHeight - aliasOrUidSize.Y) / 2;

            ImGui.SetCursorScreenPos(new Vector2(ProfilePictureBorderPos.X + widthToCenterOn / 2 - aliasOrUidSize.X / 2, ProfilePictureBorderPos.Y + ProfilePictureBorderSize.Y + yHeight));
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedPink, MainHub.PlayerUserData.AliasOrUID);
        }
    }

    private void DrawIconSummary(ImDrawListPtr drawList, KinkPlate profile)
    {
        int iconWidthPlusSpacing = 38;
        var iconOverviewPos = IconOverviewListPos;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator], iconOverviewPos, Vector2.One * 34, ImGuiColors.DalamudGrey3);
        iconOverviewPos.X += iconWidthPlusSpacing;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.ShockCollar], iconOverviewPos, Vector2.One * 34, ImGuiColors.DalamudGrey3);
        iconOverviewPos.X += iconWidthPlusSpacing;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Leash], iconOverviewPos, Vector2.One * 34, ImGuiColors.DalamudGrey3);
        iconOverviewPos.X += iconWidthPlusSpacing;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.LockedEmote], iconOverviewPos, Vector2.One * 34, ImGuiColors.DalamudGrey3);
        iconOverviewPos.X += iconWidthPlusSpacing;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.IndoorConfinement], iconOverviewPos, Vector2.One * 34, ImGuiColors.DalamudGrey3);
        iconOverviewPos.X += iconWidthPlusSpacing;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.ChatBlocked], iconOverviewPos, Vector2.One * 34, ImGuiColors.DalamudGrey3);
    }

    private void DrawDescription(ImDrawListPtr drawList, KinkPlate profile)
    {
        // draw out the description background.
        if (_cosmetics.TryGetBackground(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionBackground, out var descBG))
            drawList.AddDalamudImageRounded(descBG, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description border
        if (_cosmetics.TryGetBorder(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionBorder, out var descBorder))
            drawList.AddDalamudImageRounded(descBorder, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description overlay.
        if (_cosmetics.TryGetOverlay(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionOverlay, out var descOverlay))
            drawList.AddDalamudImageRounded(descOverlay, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // draw out the description text here. What displays is affected by if it is flagged or not.
        ImGui.SetCursorScreenPos(DescriptionBorderPos + Vector2.One * 10f);
        // shadowban them by displaying the default text if flagged or disabled.
        var description = profile.TempDisabled ? "Profile is currently disabled."
            : profile.KinkPlateInfo.Description.IsNullOrEmpty()
            ? "No Description Was Set.." : profile.KinkPlateInfo.Description;
        var color = (profile.KinkPlateInfo.Description.IsNullOrEmpty() || profile.TempDisabled)
            ? ImGuiColors.DalamudGrey2 : ImGuiColors.DalamudWhite;
        KinkPlateUI.DrawLimitedDescription(description, color, DescriptionBorderSize - Vector2.One * 12f);
    }

    private void DrawGagInfo(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // Draw out the background for the gag layer one item.
        if (_cosmetics.TryGetBackground(ProfileComponent.GagSlot, info.GagSlotBackground, out var gagSlotBG))
        {
            drawList.AddDalamudImageRounded(gagSlotBG, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            drawList.AddDalamudImageRounded(gagSlotBG, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            drawList.AddDalamudImageRounded(gagSlotBG, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }
        else
        {
            drawList.AddRectFilled(GagSlotOneBorderPos, GagSlotOneBorderPos + GagSlotBorderSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f)), 15f);
            drawList.AddRectFilled(GagSlotTwoBorderPos, GagSlotTwoBorderPos + GagSlotBorderSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f)), 15f);
            drawList.AddRectFilled(GagSlotThreeBorderPos, GagSlotThreeBorderPos + GagSlotBorderSize, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f)), 15f);
        }

        // draw out the borders.
        if (_cosmetics.TryGetBorder(ProfileComponent.GagSlot, info.GagSlotBorder, out var gagSlotBorder))
        {
            drawList.AddDalamudImageRounded(gagSlotBorder, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            drawList.AddDalamudImageRounded(gagSlotBorder, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            drawList.AddDalamudImageRounded(gagSlotBorder, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }

        // draw out the overlays.
        if (_cosmetics.TryGetOverlay(ProfileComponent.GagSlot, info.GagSlotOverlay, out var gagSlotOverlay))
        {
            drawList.AddDalamudImageRounded(gagSlotOverlay, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            drawList.AddDalamudImageRounded(gagSlotOverlay, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            drawList.AddDalamudImageRounded(gagSlotOverlay, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }

        // draw out the padlock backgrounds.
        if (_cosmetics.TryGetBackground(ProfileComponent.Padlock, info.PadlockBackground, out var padlockBG))
        {
            drawList.AddDalamudImageRounded(padlockBG, GagLockOneBorderPos, GagLockBorderSize, 10f);
            drawList.AddDalamudImageRounded(padlockBG, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            drawList.AddDalamudImageRounded(padlockBG, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }
        else
        {
            drawList.AddCircleFilled(GagLockOneBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
            drawList.AddCircleFilled(GagLockTwoBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
            drawList.AddCircleFilled(GagLockThreeBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
        }

        // draw out the padlock borders.
        if (_cosmetics.TryGetBorder(ProfileComponent.Padlock, info.PadlockBorder, out var padlockBorder))
        {
            drawList.AddDalamudImageRounded(padlockBorder, GagLockOneBorderPos, GagLockBorderSize, 10f);
            drawList.AddDalamudImageRounded(padlockBorder, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            drawList.AddDalamudImageRounded(padlockBorder, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }

        // draw out the padlock overlays.
        if (_cosmetics.TryGetOverlay(ProfileComponent.Padlock, info.PadlockOverlay, out var padlockOverlay))
        {
            drawList.AddDalamudImageRounded(padlockOverlay, GagLockOneBorderPos, GagLockBorderSize, 10f);
            drawList.AddDalamudImageRounded(padlockOverlay, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            drawList.AddDalamudImageRounded(padlockOverlay, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }
    }

    private void DrawStats(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // jump down to where we should draw out the stats, and draw out the achievement icon.
        var statsPos = StatsPos;
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Clock], statsPos, Vector2.One * 20, ImGuiColors.ParsedGold);
        // set the cursor screen pos to the right of the clock, and draw out the joined date.
        statsPos += new Vector2(24, 0);

        ImGui.SetCursorScreenPos(statsPos);
        var formattedDate = MainHub.PlayerUserData.CreatedOn ?? DateTime.MinValue;
        string createdDate = formattedDate != DateTime.MinValue ? formattedDate.ToString("d", CultureInfo.CurrentCulture) : "MM-DD-YYYY";

        CkGui.ColorText(createdDate, ImGuiColors.ParsedGold);
        var textWidth = ImGui.CalcTextSize($"MM-DD-YYYY").X;
        statsPos += new Vector2(textWidth + 4, 0);
        // to the right of this, draw out the achievement icon.
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Achievement], statsPos, Vector2.One * 20, ImGuiColors.ParsedGold);
        // to the right of this, draw the players total earned achievements scoring.
        statsPos += new Vector2(24, 0);
        ImGui.SetCursorScreenPos(statsPos);
        CkGui.ColorText(info.CompletedAchievementsTotal + "/" + ClientAchievements.Total, ImGuiColors.ParsedGold);
        CkGui.AttachToolTip("The total achievements " + MainHub.PlayerUserData.AliasOrUID + " has earned.");
    }

    private void DrawBlockedSlots(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.BlockedSlots, info.BlockedSlotsBackground, out var lockedSlotsPanelBG))
            drawList.AddDalamudImageRounded(lockedSlotsPanelBG, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.BlockedSlots, info.BlockedSlotsBorder, out var lockedSlotsPanelBorder))
            drawList.AddDalamudImageRounded(lockedSlotsPanelBorder, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the overlay on top of that.
        if (_cosmetics.TryGetOverlay(ProfileComponent.BlockedSlots, info.BlockedSlotsOverlay, out var lockedSlotsPanelOverlay))
            drawList.AddDalamudImageRounded(lockedSlotsPanelOverlay, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the blocked causes icon row.
        var blockedAffecterPos = LockAffectersRowPos;
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained], blockedAffecterPos, LockAffecterIconSize, ImGuiColors.DalamudGrey3);
        blockedAffecterPos.X += LockAffecterIconSize.X + LockAffecterSpacing.X;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.CursedLoot], blockedAffecterPos, LockAffecterIconSize, ImGuiColors.DalamudGrey3);
        blockedAffecterPos.X += LockAffecterIconSize.X + 11f;

        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Blindfolded], blockedAffecterPos, LockAffecterIconSize, ImGuiColors.DalamudGrey3);

        // draw out the background for the head slot.
        if (_cosmetics.TryGetBorder(ProfileComponent.BlockedSlot, info.BlockedSlotBorder, out var blockedSlotBG))
        {
            // obtain the start position, then start drawing all of the borders at once.
            var blockedSlotBorderPos = LockedSlotsGroupPos;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
        }

        // draw out the background for the head slot.
        if (_cosmetics.TryGetOverlay(ProfileComponent.BlockedSlot, info.BlockedSlotOverlay, out var blockedSlotOverlay))
        {
            // obtain the start position, then start drawing all of the overlays at once.
            var blockedSlotOverlayPos = LockedSlotsGroupPos;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
            // scooch down and repeat.

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            drawList.AddDalamudImageRounded(blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
        }

        // draw the icon list underneath that displays the hardcore traits and shit
        var hardcoreTraitsPos = HardcoreTraitsRowPos;
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.RestrainedArmsLegs], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3);
        hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3);
        hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.SightLoss], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3);
        hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Weighty], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3);
        hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
        drawList.AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Stimulated], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3);
    }

    private void CloseButton(ImDrawListPtr drawList)
    {
        var btnPos = CloseButtonPos;
        var btnSize = CloseButtonSize;

        var closeButtonColor = HoveringCloseButton ? ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)) : ImGui.GetColorU32(ImGuiColors.ParsedPink);

        drawList.AddLine(btnPos, btnPos + btnSize, closeButtonColor, 3);
        drawList.AddLine(new Vector2(btnPos.X + btnSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + btnSize.Y), closeButtonColor, 3);


        ImGui.SetCursorScreenPos(btnPos);
        if (ImGui.InvisibleButton($"CloseButton##KinkPlateClosePreview" + MainHub.UID, btnSize))
        {
            this.IsOpen = false;
        }
        HoveringCloseButton = ImGui.IsItemHovered();
    }
}
