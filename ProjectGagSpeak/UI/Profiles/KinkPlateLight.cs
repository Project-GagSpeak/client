using Dalamud.Interface.Colors;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GagSpeak.Achievements;
using Dalamud.Interface;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using Dalamud.Interface.Utility.Raii;
using System.Threading;
using GagSpeak.WebAPI;
using Dalamud.Interface.Utility;

namespace GagSpeak.UI.Profile;

/// <summary>
/// The UI Design for the KinkPlates.
/// </summary>
public class KinkPlateLight
{
    private readonly ILogger<KinkPlateLight> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly PairManager _pairManager;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly KinkPlateService _profileService;
    private readonly CosmeticService _cosmetics;
    private readonly TextureService _textures;
    private readonly UiSharedService _uiShared;
    public KinkPlateLight(ILogger<KinkPlateLight> logger, GagspeakMediator mediator,
        PairManager pairManager, ServerConfigurationManager serverConfigs,
        KinkPlateService profileService, CosmeticService cosmetics,
        TextureService textureService, UiSharedService uiShared)
    {
        _logger = logger;
        _mediator = mediator;
        _pairManager = pairManager;
        _serverConfigs = serverConfigs;
        _profileService = profileService;
        _cosmetics = cosmetics;
        _textures = textureService;
        _uiShared = uiShared;
    }

    public Vector2 RectMin { get; set; } = Vector2.Zero;
    public Vector2 RectMax { get; set; } = Vector2.Zero;
    private Vector2 PlateSize => RectMax - RectMin;
    public Vector2 CloseButtonPos => RectMin + ImGuiHelpers.ScaledVector2(21.25f);
    public Vector2 CloseButtonSize => ImGuiHelpers.ScaledVector2(27f);
    public Vector2 ReportButtonPos => RectMin + ImGuiHelpers.ScaledVector2(18.0625f, 223.125f);
    private Vector2 ProfilePictureBorderPos => RectMin + new Vector2(PlateSize.X - ProfilePictureBorderSize.X) / 2;
    private Vector2 ProfilePictureBorderSize => ImGuiHelpers.ScaledVector2(254.25f);
    private Vector2 ProfilePicturePos => RectMin + new Vector2(6.75f * ImGuiHelpers.GlobalScale + (PlateSize.X - ProfilePictureBorderSize.X) / 2);
    public Vector2 ProfilePictureSize => ImGuiHelpers.ScaledVector2(240.75f);
    private Vector2 SupporterIconBorderPos => RectMin + ImGuiHelpers.ScaledVector2(211.5f, 18f);
    private Vector2 SupporterIconBorderSize => ImGuiHelpers.ScaledVector2(58.5f);
    private Vector2 SupporterIconPos => RectMin + ImGuiHelpers.ScaledVector2(213.75f, 20.25f);
    private Vector2 SupporterIconSize => ImGuiHelpers.ScaledVector2(54f);
    private Vector2 DescriptionBorderPos => RectMin + ImGuiHelpers.ScaledVector2(13.5f, 385f);
    private Vector2 DescriptionBorderSize => ImGuiHelpers.ScaledVector2(261f, 177f);
    private Vector2 TitleLineStartPos => RectMin + ImGuiHelpers.ScaledVector2(13.5f, 345f);
    private Vector2 TitleLineSize => ImGuiHelpers.ScaledVector2(261f, 5.625f);
    private Vector2 StatsPos => RectMin + ImGuiHelpers.ScaledVector2(0, 358f);
    private Vector2 StatIconSize => ImGuiHelpers.ScaledVector2(22.5f);
    private static Vector4 Gold = new Vector4(1f, 0.851f, 0.299f, 1f);

    public bool DrawKinkPlateLight(ImDrawListPtr drawList, KinkPlate profile, string displayName, UserData userData, bool isPair, bool hoveringReport)
    {
        DrawPlate(drawList, profile.KinkPlateInfo, displayName);

        DrawProfilePic(drawList, profile, displayName, userData, isPair);

        DrawDescription(drawList, profile, userData, isPair);

        // Now let's draw out the chosen achievement Name..
        using (_uiShared.GagspeakLabelFont.Push())
        {
            var titleName = AchievementManager.GetTitleById(profile.KinkPlateInfo.ChosenTitleId);
            var chosenTitleSize = ImGui.CalcTextSize(titleName);
            ImGui.SetCursorScreenPos(new Vector2(TitleLineStartPos.X + TitleLineSize.X / 2 - chosenTitleSize.X / 2, TitleLineStartPos.Y - chosenTitleSize.Y));
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedGold, titleName);
        }
        // move over to the top area to draw out the achievement title line wrap.
        KinkPlateUI.AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.AchievementLineSplit], TitleLineStartPos, TitleLineSize);

        var ret = DrawStats(drawList, profile.KinkPlateInfo, displayName, userData, hoveringReport);
        return ret;
    }

    private void DrawPlate(ImDrawListPtr drawList, KinkPlateContent info, string displayName)
    {
        // draw out the background for the window.
        if (_cosmetics.TryGetBackground(ProfileComponent.PlateLight, info.PlateBackground, out var plateBG))
            KinkPlateUI.AddImageRounded(drawList, plateBG, RectMin, PlateSize, 30f);

        // draw out the border on top of that.
        if (_cosmetics.TryGetBorder(ProfileComponent.PlateLight, info.PlateBorder, out var plateBorder))
            KinkPlateUI.AddImageRounded(drawList, plateBorder, RectMin, PlateSize, 20f);
    }

    private void DrawProfilePic(ImDrawListPtr drawList, KinkPlate profile, string displayName, UserData userData, bool isPair)
    {
        if (userData.UID == MainHub.UID)
        {
            // The user is us, and we are under review, show our picture.
            var pfpWrap = profile.GetCurrentProfileOrDefault();
            KinkPlateUI.AddImageRounded(drawList, pfpWrap, ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
        }
        else if(profile.TempDisabled)
        {
            // profile is pending report review.
            KinkPlateUI.AddImageRounded(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Logo256], ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
            KinkPlateUI.AddRelativeTooltip(ProfilePictureBorderPos + ProfilePictureBorderSize / 4, ProfilePictureBorderSize / 2, "Profile Image is reset to default, currently under report submission.");
        }
        else if ((!profile.KinkPlateInfo.PublicPlate && !isPair))
        {
            // profile is not public.
            KinkPlateUI.AddImageRounded(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Logo256], ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
            KinkPlateUI.AddRelativeTooltip(ProfilePictureBorderPos + ProfilePictureBorderSize / 4, ProfilePictureBorderSize / 2, "Profile Pic is hidden as they have not allowed public plates!");
        }
        else
        {
            // Viewing a direct pair, draw the profile picture.
            var pfpWrap = profile.GetCurrentProfileOrDefault();
            KinkPlateUI.AddImageRounded(drawList, pfpWrap, ProfilePicturePos, ProfilePictureSize, ProfilePictureSize.Y / 2);
        }

        // draw out the border for the profile picture
        if (_cosmetics.TryGetBorder(ProfileComponent.ProfilePicture, profile.KinkPlateInfo.ProfilePictureBorder, out var pfpBorder))
            KinkPlateUI.AddImageRounded(drawList, pfpBorder, ProfilePictureBorderPos, ProfilePictureBorderSize, ProfilePictureSize.Y / 2);

        // Draw out Supporter Icon Black BG base.
        drawList.AddCircleFilled(SupporterIconBorderPos + SupporterIconBorderSize / 2,
            SupporterIconBorderSize.X / 2, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)));

        // Draw out Supporter Icon.
        var supporterInfo = _cosmetics.GetSupporterInfo(userData);
        if (supporterInfo.SupporterWrap is { } wrap)
        {
            KinkPlateUI.AddImageRounded(drawList, wrap, SupporterIconPos, SupporterIconSize, SupporterIconSize.Y / 2, true, displayName + " Is Supporting CK!");
        }
        // Draw out the border for the icon.
        drawList.AddCircle(SupporterIconBorderPos + SupporterIconBorderSize / 2, SupporterIconBorderSize.X / 2,
            ImGui.GetColorU32(ImGuiColors.ParsedPink), 0, 4f);


        // draw out the UID here. We must make it centered. To do this, we must fist calculate how to center it.
        var widthToCenterOn = ProfilePictureBorderSize.X;
        using (_uiShared.UidFont.Push())
        {
            var aliasOrUidSize = ImGui.CalcTextSize(displayName);
            ImGui.SetCursorScreenPos(new Vector2(ProfilePictureBorderPos.X + widthToCenterOn / 2 - aliasOrUidSize.X / 2, ProfilePictureBorderPos.Y + ProfilePictureBorderSize.Y + 5));
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedPink, displayName);
        }
#if DEBUG
        UiSharedService.CopyableDisplayText(userData.UID);
#endif
    }

    private void DrawDescription(ImDrawListPtr drawList, KinkPlate profile, UserData userData, bool isPair)
    {
        // draw out the description background.
        if (_cosmetics.TryGetBackground(ProfileComponent.DescriptionLight, profile.KinkPlateInfo.DescriptionBackground, out var descBG))
            KinkPlateUI.AddImageRounded(drawList, descBG, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description border
        if (_cosmetics.TryGetBorder(ProfileComponent.DescriptionLight, profile.KinkPlateInfo.DescriptionBorder, out var descBorder))
            KinkPlateUI.AddImageRounded(drawList, descBorder, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // description overlay.
        if (_cosmetics.TryGetOverlay(ProfileComponent.DescriptionLight, profile.KinkPlateInfo.DescriptionOverlay, out var descOverlay))
            KinkPlateUI.AddImageRounded(drawList, descOverlay, DescriptionBorderPos, DescriptionBorderSize, 2f);

        // draw out the description text here.
        ImGui.SetCursorScreenPos(DescriptionBorderPos + ImGuiHelpers.ScaledVector2(12f, 8f));
        if (userData.UID == MainHub.UID)
        {
            // The user is us, and we are under review, show our picture.
            var description = profile.KinkPlateInfo.Description.IsNullOrEmpty() ? "No Description Was Set.." : profile.KinkPlateInfo.Description;
            var color = profile.KinkPlateInfo.Description.IsNullOrEmpty() ? ImGuiColors.DalamudGrey2 : ImGuiColors.DalamudWhite;
            DrawLimitedDescription(description, color, DescriptionBorderSize - new Vector2(15, 0));
        }
        else if (profile.TempDisabled)
        {
            // profile is pending report review.
            DrawLimitedDescription("Profile is pending review from the CK Team after being reported.", ImGuiColors.DalamudRed, DescriptionBorderSize - new Vector2(15, 0));

        }
        else if ((!profile.KinkPlateInfo.PublicPlate && !isPair))
        {
            DrawLimitedDescription("This Kinkster hasn't made their plate public!", ImGuiColors.DalamudRed, DescriptionBorderSize - new Vector2(15, 0));
        }
        else
        {
            // Draw the pairs description.
            var description = profile.KinkPlateInfo.Description.IsNullOrEmpty() ? "No Description Was Set.." : profile.KinkPlateInfo.Description;
            var color = profile.KinkPlateInfo.Description.IsNullOrEmpty() ? ImGuiColors.DalamudGrey2 : ImGuiColors.DalamudWhite;
            DrawLimitedDescription(description, color, DescriptionBorderSize - new Vector2(15, 0));
        }
    }

    private void DrawLimitedDescription(string desc, Vector4 color, Vector2 size)
    {
        // Calculate the line height and determine the max lines based on available height
        float lineHeight = ImGui.CalcTextSize("A").Y;
        int maxLines = (int)(size.Y / lineHeight);

        int currentLines = 1;
        float lineWidth = size.X; // Max width for each line
        string[] words = desc.Split(' '); // Split text by words
        string newDescText = "";
        string currentLine = "";

        foreach (var word in words)
        {
            // Try adding the current word to the line
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            float testLineWidth = ImGui.CalcTextSize(testLine).X;

            if (testLineWidth > lineWidth)
            {
                // Current word exceeds line width; finalize the current line
                newDescText += currentLine + "\n";
                currentLine = word;
                currentLines++;

                // Check if maxLines is reached and break if so
                if (currentLines >= maxLines)
                    break;
            }
            else
            {
                // Word fits in the current line; accumulate it
                currentLine = testLine;
            }
        }

        // Add any remaining text if we haven’t hit max lines
        if (currentLines < maxLines && !string.IsNullOrEmpty(currentLine))
        {
            newDescText += currentLine;
            currentLines++; // Increment the line count for the final line
        }

        UiSharedService.ColorTextWrapped(newDescText.TrimEnd(), color);
    }

    private bool DrawStats(ImDrawListPtr drawList, KinkPlateContent info, string displayName, UserData userData, bool hoveringReport)
    {
        // jump down to where we should draw out the stats, and draw out the achievement icon.
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var statsPos = StatsPos;
        var formattedDate = userData.createdOn ?? DateTime.MinValue;
        string createdDate = formattedDate != DateTime.MinValue ? formattedDate.ToString("d", CultureInfo.CurrentCulture) : "MM-DD-YYYY";
        float dateWidth = ImGui.CalcTextSize(createdDate).X;
        float achievementWidth = ImGui.CalcTextSize(info.CompletedAchievementsTotal + "/" + AchievementManager.Total).X;
        float totalWidth = dateWidth + achievementWidth + StatIconSize.X * 3 + spacing * 3;

        statsPos.X += (PlateSize.X - totalWidth) / 2;
        KinkPlateUI.AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Clock], statsPos, StatIconSize, ImGuiColors.ParsedGold);

        // set the cursor screen pos to the right of the clock, and draw out the joined date.
        statsPos.X += StatIconSize.X + 2f;
        ImGui.SetCursorScreenPos(statsPos);
        UiSharedService.ColorText(createdDate, ImGuiColors.ParsedGold);
        UiSharedService.AttachToolTip("The date " + displayName + " first joined GagSpeak.");

        statsPos.X += dateWidth + spacing;
        KinkPlateUI.AddImage(drawList, _cosmetics.CorePluginTextures[CorePluginTexture.Achievement], statsPos, StatIconSize, ImGuiColors.ParsedGold);

        statsPos.X += StatIconSize.X + 2f;
        ImGui.SetCursorScreenPos(statsPos);
        UiSharedService.ColorText(info.CompletedAchievementsTotal + "/" + AchievementManager.Total, ImGuiColors.ParsedGold);
        UiSharedService.AttachToolTip("The total achievements " + displayName + " has earned.");

        statsPos.X += achievementWidth + spacing;
        statsPos.Y += 2f;
        ImGui.SetCursorScreenPos(statsPos);
        var color = hoveringReport && (KeyMonitor.CtrlPressed() && KeyMonitor.ShiftPressed())
            ? ImGui.GetColorU32(ImGuiColors.DalamudRed) 
            : hoveringReport ? ImGui.GetColorU32(ImGuiColors.DalamudGrey)
                             : ImGui.GetColorU32(ImGuiColors.DalamudGrey3);
        using (_uiShared.IconFont.Push())
        {
            drawList.AddText(statsPos, color, FontAwesomeIcon.Flag.ToIconString());
        }
        ImGui.SetWindowFontScale(1.0f);
        ImGui.SetCursorScreenPos(statsPos);
        using (ImRaii.Disabled(!KeyMonitor.CtrlPressed() || !KeyMonitor.ShiftPressed()))
        {
            if (ImGui.InvisibleButton($"ReportKinkPlate##ReportKinkPlate" + userData.UID, CloseButtonSize))
                _mediator.Publish(new ReportKinkPlateMessage(userData));

        }
        UiSharedService.AttachToolTip("Report " + displayName + "'s KinkPlate™" +
            "--SEP--Press CTRL+SHIFT to report.\n" +
            "(Opens Report Submission Window)");

        return ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
    }
}
