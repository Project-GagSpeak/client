using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;
using GagSpeak.Achievements;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using Penumbra.GameData.Enums;
using System.Globalization;
using System.Numerics;

namespace GagSpeak.UI.Profile;
public partial class KinkPlateUI : WindowMediatorSubscriberBase
{
    private readonly PairManager _pairManager;
    private readonly KinkPlateService _profileService;
    private readonly CosmeticService _cosmetics;
    private readonly TextureService _textures;
    private readonly UiSharedService _uiShared;

    private bool ThemePushed = false;
    public KinkPlateUI(ILogger<KinkPlateUI> logger, GagspeakMediator mediator,
        PairManager pairManager, KinkPlateService profileService,
        CosmeticService cosmetics, TextureService textureService, UiSharedService uiShared,
        Pair pair) : base(logger, mediator, pair.UserData.AliasOrUID + "'s KinkPlate##GagspeakKinkPlateUI" + pair.UserData.AliasOrUID)
    {
        _pairManager = pairManager;
        _profileService = profileService;
        _cosmetics = cosmetics;
        _textures = textureService;
        _uiShared = uiShared;
        Pair = pair;

        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar;
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
        var drawList = ImGui.GetWindowDrawList();
        RectMin = drawList.GetClipRectMin();
        RectMax = drawList.GetClipRectMax();
        //_logger.LogDebug("RectMin: {rectMin}, RectMax: {rectMax}", rectMin, rectMax);

        // obtain the profile for this userPair.
        var KinkPlate = _profileService.GetKinkPlate(Pair.UserData);

        // Draw KinkPlateUI Function here.
        DrawKinkPlatePair(drawList, KinkPlate);
    }

    // Size = 750 by 450
    private void DrawKinkPlatePair(ImDrawListPtr drawList, KinkPlate profile)
    {
        DrawPlate(drawList, profile.KinkPlateInfo);

        DrawProfilePic(drawList, profile);

        DrawIconSummary(drawList, profile);

        DrawDescription(drawList, profile);

        // Now let's draw out the chosen achievement Name..
        using (_uiShared.GagspeakTitleFont.Push())
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
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.AchievementLineSplit], TitleLineStartPos, TitleLineSize);

        DrawGagInfo(drawList, profile.KinkPlateInfo);

        DrawStats(drawList, profile.KinkPlateInfo);

        DrawBlockedSlots(drawList, profile.KinkPlateInfo);
    }

    private void DrawPlate(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.Plate, info.PlateBackground, out var plateBG))
            AddImageRounded(drawList, plateBG, RectMin, PlateSize, 25f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.Plate, info.PlateBorder, out var plateBorder))
            AddImageRounded(drawList, plateBorder, RectMin, PlateSize, 20f);

        // Draw the close button.
        CloseButton(drawList);
        AddRelativeTooltip(CloseButtonPos, CloseButtonSize, "Close " + DisplayName + "'s KinkPlate™");
    }

    private void DrawProfilePic(ImDrawListPtr drawList, KinkPlate profile)
    {
        // We should always display the default GagSpeak Logo if the profile is either flagged or disabled.
        if (profile.TempDisabled)
        {
            AddImageRounded(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Logo256bg], ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
        }
        else // But otherwise can draw normal image.
        {
            var pfpWrap = profile.GetCurrentProfileOrDefault();
            AddImageRounded(drawList, pfpWrap, ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
        }

        // draw out the border for the profile picture
        if (_cosmetics.TryGetBorder(ProfileComponent.ProfilePicture, profile.KinkPlateInfo.ProfilePictureBorder, out var pfpBorder))
            AddImageRounded(drawList, pfpBorder, ProfilePictureBorderPos, ProfilePictureBorderSize, ProfilePictureSize.Y / 2);

        // Draw out Supporter Icon Black BG base.
        drawList.AddCircleFilled(SupporterIconBorderPos + SupporterIconBorderSize / 2,
            SupporterIconBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));

        // Draw out Supporter Icon.
        var supporterInfo = _cosmetics.GetSupporterInfo(Pair.UserData);
        if (supporterInfo.SupporterWrap is { } wrap)
        {
            AddImageRounded(drawList, wrap, SupporterIconPos, SupporterIconSize, SupporterIconSize.Y / 2, true, supporterInfo.Tooltip);
        }
        // Draw out the border for the icon.
        drawList.AddCircle(SupporterIconBorderPos + SupporterIconBorderSize / 2, SupporterIconBorderSize.X / 2,
            ImGui.GetColorU32(ImGuiColors.ParsedPink), 0, 4f);


        // draw out the UID here. We must make it centered. To do this, we must fist calculate how to center it.
        var widthToCenterOn = ProfilePictureBorderSize.X;
        // determine the height gap between the icon overview and bottom of the profile picture.
        var gapHeight = IconOverviewListPos.Y - (ProfilePictureBorderPos.Y + ProfilePictureBorderSize.Y);
        string ttText = DisplayName == PairUID ? "This Pairs UID" : "This Pairs Alias --SEP-- Their UID is: " + PairUID;
        using (_uiShared.UidFont.Push())
        {
            var aliasOrUidSize = ImGui.CalcTextSize(DisplayName);
            var yHeight = (gapHeight - aliasOrUidSize.Y) / 2;

            ImGui.SetCursorScreenPos(new Vector2(ProfilePictureBorderPos.X + widthToCenterOn / 2 - aliasOrUidSize.X / 2, ProfilePictureBorderPos.Y + ProfilePictureBorderSize.Y + yHeight));
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedPink, DisplayName);
        }
        UiSharedService.AttachToolTip(ttText);
    }

    private void DrawIconSummary(ImDrawListPtr drawList, KinkPlate profile)
    {
        int iconWidthPlusSpacing = 38;
        var iconOverviewPos = IconOverviewListPos;

        // draw out the icon row. For each item, we will first determine the color, and its tooltip text.
        var vibeColor = Pair.PairGlobals.ToyIsActive ? Gold : ImGuiColors.DalamudGrey3;
        var vibeTT = Pair.PairGlobals.ToyIsActive
            ? DisplayName + " has a Sex Toy connected and active."
            : DisplayName + " does not have any Sex Toys connected and active.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Vibrator], iconOverviewPos, Vector2.One * 34, vibeColor, true, vibeTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var shockColor = Pair.PairGlobals.HasValidShareCode() ? Gold : ImGuiColors.DalamudGrey3;
        var shockTT = Pair.PairGlobals.HasValidShareCode()
            ? DisplayName + " is connected to their Shock Collar while in Hardcore Mode."
            : DisplayName + " has not connected a Shock Collar.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.ShockCollar], iconOverviewPos, Vector2.One * 34, shockColor, true, shockTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var forcedFollowColor = !Pair.PairGlobals.ForcedFollow.NullOrEmpty() ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var forcedFollowTT = !Pair.PairGlobals.ForcedFollow.NullOrEmpty()
            ? DisplayName + " is being leashed around by another pair while in Hardcore Mode."
            : DisplayName + " is not following anyone.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Leash], iconOverviewPos, Vector2.One * 34, forcedFollowColor, true, forcedFollowTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var forcedEmoteColor = !Pair.PairGlobals.ForcedEmoteState.NullOrEmpty() ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var forcedEmoteTT = !Pair.PairGlobals.ForcedEmoteState.NullOrEmpty()
            ? DisplayName + " is being put on display for another pair while in Hardcore Mode."
            : DisplayName + " is not on display for anyone.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.ForcedEmote], iconOverviewPos, Vector2.One * 34, forcedEmoteColor, true, forcedEmoteTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        var forcedStayColor = !Pair.PairGlobals.ForcedStay.NullOrEmpty() ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var forcedStayTT = !Pair.PairGlobals.ForcedStay.NullOrEmpty()
            ? DisplayName + " has been ordered to stay put for another pair while in Hardcore Mode."
            : DisplayName + " has not been ordered to stay put by anyone.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.ForcedStay], iconOverviewPos, Vector2.One * 34, forcedStayColor, true, forcedStayTT);
        iconOverviewPos.X += iconWidthPlusSpacing;

        bool chatManipulated = Pair.PairGlobals.IsChatHidden() || Pair.PairGlobals.IsChatInputHidden() || Pair.PairGlobals.IsChatInputBlocked();
        var chatBlockedColor = chatManipulated ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey3;
        var chatBlockedTT = chatManipulated
            ? DisplayName + " is having their chat access restricted by another pair while in Hardcore Mode."
            : DisplayName + " is not under any chat restrictions.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.ChatBlocked], iconOverviewPos, Vector2.One * 34, chatBlockedColor, true, chatBlockedTT);
    }

    private void DrawDescription(ImDrawListPtr drawList, KinkPlate profile)
    {
        // draw out the description background.
        if (_cosmetics.TryGetBackground(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionBackground, out var descBG))
            AddImageRounded(drawList, descBG, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description border
        if (_cosmetics.TryGetBorder(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionBorder, out var descBorder))
            AddImageRounded(drawList, descBorder, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description overlay.
        if (_cosmetics.TryGetOverlay(ProfileComponent.Description, profile.KinkPlateInfo.DescriptionOverlay, out var descOverlay))
            AddImageRounded(drawList, descOverlay, DescriptionBorderPos, DescriptionBorderSize, 2f);

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

    private void DrawGagInfo(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // Draw out the background for the gag layer one item.
        if (_cosmetics.TryGetBackground(ProfileComponent.GagSlot, info.GagSlotBackground, out var gagSlotBG))
        {
            AddImageRounded(drawList, gagSlotBG, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            AddImageRounded(drawList, gagSlotBG, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            AddImageRounded(drawList, gagSlotBG, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
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
            AddImageRounded(drawList, gagSlotBorder, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            AddImageRounded(drawList, gagSlotBorder, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            AddImageRounded(drawList, gagSlotBorder, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }

        // we should draw out the gag images here if valid.
        if (Pair.LastAppearanceData is not null)
        {
            if (Pair.LastAppearanceData.GagSlots[0].GagType.ToGagType() is not GagType.None)
            {
                var gagImage = _cosmetics.GetImageFromDirectoryFile("GagImages\\" + Pair.LastAppearanceData.GagSlots[0].GagType + ".png" ?? $"ItemMouth\\None.png");
                AddImageRounded(drawList, gagImage, GagSlotOnePos, GagSlotSize, 10f);
            }
            if (Pair.LastAppearanceData.GagSlots[1].GagType.ToGagType() is not GagType.None)
            {
                var gagImage = _cosmetics.GetImageFromDirectoryFile("GagImages\\" + Pair.LastAppearanceData.GagSlots[1].GagType + ".png" ?? $"ItemMouth\\None.png");
                AddImageRounded(drawList, gagImage, GagSlotTwoPos, GagSlotSize, 10f);
            }
            if (Pair.LastAppearanceData.GagSlots[2].GagType.ToGagType() is not GagType.None)
            {
                var gagImage = _cosmetics.GetImageFromDirectoryFile("GagImages\\" + Pair.LastAppearanceData.GagSlots[2].GagType + ".png" ?? $"ItemMouth\\None.png");
                AddImageRounded(drawList, gagImage, GagSlotThreePos, GagSlotSize, 10f);
            }
        }

        // draw out the overlays.
        if (_cosmetics.TryGetOverlay(ProfileComponent.GagSlot, info.GagSlotOverlay, out var gagSlotOverlay))
        {
            AddImageRounded(drawList, gagSlotOverlay, GagSlotOneBorderPos, GagSlotBorderSize, 10f);
            AddImageRounded(drawList, gagSlotOverlay, GagSlotTwoBorderPos, GagSlotBorderSize, 10f);
            AddImageRounded(drawList, gagSlotOverlay, GagSlotThreeBorderPos, GagSlotBorderSize, 10f);
        }

        // draw out the padlock backgrounds.
        if (_cosmetics.TryGetBackground(ProfileComponent.Padlock, info.PadlockBackground, out var padlockBG))
        {
            AddImageRounded(drawList, padlockBG, GagLockOneBorderPos, GagLockBorderSize, 10f);
            AddImageRounded(drawList, padlockBG, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            AddImageRounded(drawList, padlockBG, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }
        else
        {
            drawList.AddCircleFilled(GagLockOneBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
            drawList.AddCircleFilled(GagLockTwoBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
            drawList.AddCircleFilled(GagLockThreeBorderPos + GagLockBorderSize / 2, GagLockBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));
        }

        // we should draw out the lock images here if valid.
        if (Pair.LastAppearanceData is not null)
        {
            if (Pair.LastAppearanceData.GagSlots[0].Padlock.ToPadlock() is not Padlocks.None)
            {
                var padlockImage = _cosmetics.GetImageFromDirectoryFile("PadlockImages\\" + Pair.LastAppearanceData.GagSlots[0].Padlock.ToPadlock() + ".png" ?? "Padlocks\\None.png");
                AddImageRounded(drawList, padlockImage, GagLockOnePos, GagLockSize, GagLockSize.X / 2);
            }
            if (Pair.LastAppearanceData.GagSlots[1].Padlock.ToPadlock() is not Padlocks.None)
            {
                var padlockImage = _cosmetics.GetImageFromDirectoryFile("PadlockImages\\" + Pair.LastAppearanceData.GagSlots[1].Padlock.ToPadlock() + ".png" ?? "Padlocks\\None.png");
                AddImageRounded(drawList, padlockImage, GagLockTwoPos, GagLockSize, GagLockSize.X / 2);
            }
            if (Pair.LastAppearanceData.GagSlots[2].Padlock.ToPadlock() is not Padlocks.None)
            {
                var padlockImage = _cosmetics.GetImageFromDirectoryFile("PadlockImages\\" + Pair.LastAppearanceData.GagSlots[2].Padlock.ToPadlock() + ".png" ?? "Padlocks\\None.png");
                AddImageRounded(drawList, padlockImage, GagLockThreePos, GagLockSize, GagLockSize.X / 2);
            }
        }

        // draw out the padlock borders.
        if (_cosmetics.TryGetBorder(ProfileComponent.Padlock, info.PadlockBorder, out var padlockBorder))
        {
            AddImageRounded(drawList, padlockBorder, GagLockOneBorderPos, GagLockBorderSize, 10f);
            AddImageRounded(drawList, padlockBorder, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            AddImageRounded(drawList, padlockBorder, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }

        // draw out the padlock overlays.
        if (_cosmetics.TryGetOverlay(ProfileComponent.Padlock, info.PadlockOverlay, out var padlockOverlay))
        {
            AddImageRounded(drawList, padlockOverlay, GagLockOneBorderPos, GagLockBorderSize, 10f);
            AddImageRounded(drawList, padlockOverlay, GagLockTwoBorderPos, GagLockBorderSize, 10f);
            AddImageRounded(drawList, padlockOverlay, GagLockThreeBorderPos, GagLockBorderSize, 10f);
        }
    }

    private void DrawStats(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // jump down to where we should draw out the stats, and draw out the achievement icon.
        var statsPos = StatsPos;
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Clock], statsPos, Vector2.One * 20, ImGuiColors.ParsedGold);
        // set the cursor screen pos to the right of the clock, and draw out the joined date.
        statsPos += new Vector2(24, 0);

        ImGui.SetCursorScreenPos(statsPos);
        var formattedDate = Pair.UserData.createdOn ?? DateTime.MinValue;
        string createdDate = formattedDate != DateTime.MinValue ? formattedDate.ToString("d", CultureInfo.CurrentCulture) : "MM-DD-YYYY";

        UiSharedService.ColorText(createdDate, ImGuiColors.ParsedGold);
        var textWidth = ImGui.CalcTextSize($"MM-DD-YYYY").X;
        statsPos += new Vector2(textWidth + 4, 0);
        // to the right of this, draw out the achievement icon.
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Achievement], statsPos, Vector2.One * 20, ImGuiColors.ParsedGold);
        // to the right of this, draw the players total earned achievements scoring.
        statsPos += new Vector2(24, 0);
        ImGui.SetCursorScreenPos(statsPos);
        UiSharedService.ColorText(info.CompletedAchievementsTotal + "/" + AchievementManager.Total, ImGuiColors.ParsedGold);
        UiSharedService.AttachToolTip("The total achievements " + DisplayName + " has earned.");
    }

    private void DrawBlockedSlots(ImDrawListPtr drawList, KinkPlateContent info)
    {
        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.BlockedSlots, info.BlockedSlotsBackground, out var lockedSlotsPanelBG))
            AddImageRounded(drawList, lockedSlotsPanelBG, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.BlockedSlots, info.BlockedSlotsBorder, out var lockedSlotsPanelBorder))
            AddImageRounded(drawList, lockedSlotsPanelBorder, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the overlay on top of that.
        if (_cosmetics.TryGetOverlay(ProfileComponent.BlockedSlots, info.BlockedSlotsOverlay, out var lockedSlotsPanelOverlay))
            AddImageRounded(drawList, lockedSlotsPanelOverlay, LockedSlotsPanelBorderPos, LockedSlotsPanelBorderSize, 10f);

        // draw out the blocked causes icon row.
        var blockedAffecterPos = LockAffectersRowPos;
        var restrainedColor = Pair.LastWardrobeData?.ActiveSetId.IsEmptyGuid() ?? true ? ImGuiColors.DalamudGrey3 : Gold;
        var restrainedTT = Pair.LastWardrobeData?.ActiveSetId.IsEmptyGuid() ?? true
            ? DisplayName + " is not wearing a restraint set."
            : DisplayName + " has an active restraint set.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Restrained], blockedAffecterPos, LockAffecterIconSize, restrainedColor, true, restrainedTT);
        blockedAffecterPos.X += LockAffecterIconSize.X + LockAffecterSpacing.X;

        var mimicColor = (Pair.LastWardrobeData?.ActiveCursedItems.Any() ?? false) ? Gold : ImGuiColors.DalamudGrey3;
        var mimicTT = (Pair.LastWardrobeData?.ActiveCursedItems.Any() ?? false)
            ? DisplayName + " is restrained by Cursed Loot!"
            : DisplayName + " is not restrained with Cursed Loot.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.CursedLoot], blockedAffecterPos, LockAffecterIconSize, mimicColor, true, mimicTT);
        blockedAffecterPos.X += LockAffecterIconSize.X + 11f;

        var blindfoldedColor = Pair.PairGlobals.IsBlindfolded() && Pair.LastLightStorage?.BlindfoldItem is not null ? Gold : ImGuiColors.DalamudGrey3;
        var blindfoldedTT = Pair.PairGlobals.IsBlindfolded() && Pair.LastLightStorage?.BlindfoldItem is not null
            ? DisplayName + " is blindfolded."
            : DisplayName + " is not blindfolded.";
        AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Blindfolded], blockedAffecterPos, LockAffecterIconSize, blindfoldedColor, true, blindfoldedTT);
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
                        drawList.AddImageRounded(ptr, blockedSlotsPos, blockedSlotsPos + LockedSlotSize, Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 15f);
                        AddRelativeTooltip(blockedSlotsPos, LockedSlotSize, Pair.LockedSlots[equipSlot].Item2);
                    }
                }
                else
                {
                    // Draw the empty icon if the slot is not locked
                    var (ptr, textureSize, empty) = _textures.GetIcon(ItemIdVars.NothingItem(equipSlot), equipSlot);
                    if (!empty)
                    {
                        drawList.AddImageRounded(ptr, blockedSlotsPos, blockedSlotsPos + LockedSlotSize, Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 15f);
                    }
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
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotBorderPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotBorderPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
        }

        // draw out the background for the head slot.
        if (_cosmetics.TryGetOverlay(ProfileComponent.BlockedSlot, info.BlockedSlotOverlay, out var blockedSlotOverlay))
        {
            // obtain the start position, then start drawing all of the overlays at once.
            var blockedSlotOverlayPos = LockedSlotsGroupPos;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
            // scooch down and repeat.

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);

            blockedSlotOverlayPos.Y += LockedSlotSize.Y + LockedSlotSpacing.Y;
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos, LockedSlotSize, 10f);
            AddImageRounded(drawList, blockedSlotBG, blockedSlotOverlayPos + new Vector2(LockedSlotSize.X + LockedSlotSpacing.X, 0), LockedSlotSize, 10f);
        }

        // draw the icon list underneath that displays the hardcore traits and shit
        var hardcoreTraitsPos = HardcoreTraitsRowPos;
        var activeSetLight = Pair.LastLightStorage?.Restraints.FirstOrDefault(x => x.Identifier == Pair.LastWardrobeData?.ActiveSetId) ?? null;
        if (Pair.LastWardrobeData is not null && activeSetLight is not null && activeSetLight.HardcoreTraits.TryGetValue(Pair.LastWardrobeData.ActiveSetEnabledBy, out var traits))
        {
            if (traits.ArmsRestrained || traits.ArmsRestrained)
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.RestrainedArmsLegs], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Arms/Legs Restrained--SEP--Restricts Actions that require the use of arms/legs, whichever option is enabled.");
            else
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.RestrainedArmsLegs], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Arms/Legs Restrained");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.Gagged)
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Gagged], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Gagged--SEP--Restricts Actions that have your character shout/speak");
            else
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Gagged], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Gagged");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.Blindfolded)
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.SightLoss], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Blindfolded--SEP--Restricts Actions that require sight to be used.");
            else
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.SightLoss], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Blindfolded");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.Weighty)
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Weighty], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Weighty--SEP--With heavy bondage applied, this trait forces them to only walk.");
            else
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Weighty], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Weighty");

            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            if (traits.StimulationLevel is not StimulationLevel.None)
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Stimulated], hardcoreTraitsPos, HardcoreTraitIconSize, Gold, true, "Hardcore Trait: Stimulated--SEP--Distracted with stimulation, you care for combat less, increasing GCD Cooldown time in proportion to arousal level.");
            else
                AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Stimulated], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Stimulated");
        }
        else
        {
            AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.RestrainedArmsLegs], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Arms/Legs Restrained");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Gagged], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Gagged");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.SightLoss], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Blindfolded");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Weighty], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Weighty");
            hardcoreTraitsPos.X += HardcoreTraitIconSize.X + HardcoreTraitSpacing.X;
            AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Stimulated], hardcoreTraitsPos, HardcoreTraitIconSize, ImGuiColors.DalamudGrey3, true, "Hardcore Trait: Stimulated");
        }
    }

    private void CloseButton(ImDrawListPtr drawList)
    {
        var btnPos = CloseButtonPos;
        var btnSize = CloseButtonSize;

        var closeButtonColor = HoveringCloseButton ? ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)) : ImGui.GetColorU32(ImGuiColors.ParsedPink);

        drawList.AddLine(btnPos, btnPos + btnSize, closeButtonColor, 3);
        drawList.AddLine(new Vector2(btnPos.X + btnSize.X, btnPos.Y), new Vector2(btnPos.X, btnPos.Y + btnSize.Y), closeButtonColor, 3);


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
