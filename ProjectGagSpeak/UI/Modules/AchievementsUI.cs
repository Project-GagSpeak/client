using CkCommons.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui;

// this can easily become the "contact list" tab of the "main UI" window.
public class AchievementsUI : WindowMediatorSubscriberBase
{
    private readonly AchievementTabs _tabMenu;
    private readonly CosmeticService _textures;
    public bool ThemePushed = false;

    public AchievementsUI(ILogger<AchievementsUI> logger, GagspeakMediator mediator,
        AchievementTabs tabMenu, CosmeticService textures)
        : base(logger, mediator, "Achievements###AchievementsUI")
    {
        _tabMenu = tabMenu;
        _textures = textures;

        AllowPinning = false;
        AllowClickthrough = false;

        Flags |= WFlags.NoDocking;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(525, 400),
            MaximumSize = new Vector2(525, 2000)
        };
    }

    private string AchievementSearchString = string.Empty;

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));
            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        // get the width of the window content region we set earlier
        var contentRegion = CkGui.GetWindowContentRegionWidth();

        using (ImRaii.PushId("AchievementsHeader")) CenteredHeader();

        ImGui.Separator();
        // now we need to draw the tab bar below this.
        using (ImRaii.PushId("MainMenuTabBar")) _tabMenu.Draw(contentRegion);

        // Draw out the achievements in a child window we can scroll, but do not display the scroll bar.
        using (ImRaii.Child("AchievementsSection", new Vector2(contentRegion, 0), false, WFlags.NoScrollbar))
        {

            // display content based on the tab selected
            switch (_tabMenu.TabSelection)
            {
                case AchievementTabs.SelectedTab.Generic:
                    using (ImRaii.PushId("UnlocksComponentGeneric")) DrawAchievementList(AchievementModuleKind.Generic);
                    break;
                case AchievementTabs.SelectedTab.Gags:
                    using (ImRaii.PushId("UnlocksComponentGags")) DrawAchievementList(AchievementModuleKind.Gags);
                    break;
                case AchievementTabs.SelectedTab.Wardrobe:
                    using (ImRaii.PushId("UnlocksComponentWardrobe")) DrawAchievementList(AchievementModuleKind.Wardrobe);
                    break;
                case AchievementTabs.SelectedTab.Puppeteer:
                    using (ImRaii.PushId("UnlocksComponentPuppeteer")) DrawAchievementList(AchievementModuleKind.Puppeteer);
                    break;
                case AchievementTabs.SelectedTab.Toybox:
                    using (ImRaii.PushId("UnlocksComponentToybox")) DrawAchievementList(AchievementModuleKind.Toybox);
                    break;
                case AchievementTabs.SelectedTab.Remotes:
                    using (ImRaii.PushId("UnlocksComponentRemotes")) DrawAchievementList(AchievementModuleKind.Remotes);
                    break;
                case AchievementTabs.SelectedTab.Arousal:
                    using (ImRaii.PushId("UnlocksComponentArousal")) DrawAchievementList(AchievementModuleKind.Arousal);
                    break;
                case AchievementTabs.SelectedTab.Hardcore:
                    using (ImRaii.PushId("UnlocksComponentHardcore")) DrawAchievementList(AchievementModuleKind.Hardcore);
                    break;
                case AchievementTabs.SelectedTab.Secrets:
                    using (ImRaii.PushId("UnlocksComponentSecrets")) DrawAchievementList(AchievementModuleKind.Secrets);
                    break;
            }
        }
    }

    private void CenteredHeader()
    {
        var text = "GagSpeak Achievements (" + ClientAchievements.Completed + "/" + ClientAchievements.Total + ")";
        using (UiFontService.UidFont.Push())
        {
            var uidTextSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - uidTextSize.X / 2);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().WindowPadding.Y / 2);
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedGold, text);
        }
    }

    private void DrawAchievementList(AchievementModuleKind type)
    {
        // We likely want to avoid pushing the style theme here if we are swapping the colors based on the state of an achievement.
        // If that is not the case. move them here.
        var unlocks = ClientAchievements.GetByModule(type);
        if (!unlocks.Any())
            return;

        // filter down the unlocks to searchable results.
        var filteredUnlocks = unlocks
            .Where(goal => goal.Title.Contains(AchievementSearchString, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // reference the same achievement for every module.
        // draw the search filter.
        DrawSearchFilter(ImGui.GetContentRegionAvail().X, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.Separator();

        // create a window for scrolling through the available achievements.
        using var achievementListChild = ImRaii.Child("##AchievementListings" + type.ToString(), ImGui.GetContentRegionAvail(), false, WFlags.NoScrollbar);

        // draw the achievements in the first column.
        foreach (var achievement in filteredUnlocks)
            DrawAchievementProgressBox(achievement);
    }

    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
        var clearButtonSize = CkGui.IconTextButtonSize(FAI.Ban, "Clear");
        var resetButtonSize = CkGui.IconTextButtonSize(FAI.SyncAlt, "Reset");

        //if (CkGui.IconTextButton(FAI.SyncAlt, "Reset", disabled: !(KeyMonitor.ShiftPressed() && KeyMonitor.CtrlPressed())))
        //{
        //    // fire and forget.
        //    _ = _ClientAchievements.ResetAchievementData();
        //}
        ImGui.SetNextItemWidth(availableWidth - clearButtonSize - spacingX);
        var filter = AchievementSearchString;
        if (ImGui.InputTextWithHint("##AchievementSearchStringFilter", "Search for an Achievement...", ref filter, 255))
            AchievementSearchString = filter;

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Ban, "Clear", disabled: string.IsNullOrEmpty(AchievementSearchString)))
            AchievementSearchString = string.Empty;
        CkGui.AttachToolTip("Clear the search filter.");
    }

    private static Vector2 AchievementIconSize = new(96, 96);

    private void DrawAchievementProgressBox(AchievementBase achievementItem)
    {
        // set up the style theme for the box.
        //using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(ImGui.GetStyle().WindowPadding.X, 3f));
        //using var itemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 2f));
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f);
        using var col = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var imageTabWidth = AchievementIconSize.X + ImGui.GetStyle().ItemSpacing.X * 2;

        var size = new Vector2(ImGui.GetContentRegionAvail().X, AchievementIconSize.Y + ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetStyle().CellPadding.Y * 2);
        using (ImRaii.Child("##Achievement-" + achievementItem.Title, size, true, WFlags.ChildWindow))
        {
            try
            {
                // draw out a table that is 2 columns, and display the subsections in each column
                using (var table = ImRaii.Table("##AchievementTable" + achievementItem.Title, 2, ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("##AchievementText", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("##AchievementIcon", ImGuiTableColumnFlags.WidthFixed, AchievementIconSize.X);

                    // draw the information about the achievement and its progress bar within the first section.
                    // maybe the progress bar could span the bottom if icon image size is too much of a concern idk.
                    ImGui.TableNextColumn();
                    using (ImRaii.Group())
                    {
                        var progress = achievementItem.CurrentProgress();
                        var icon = achievementItem.IsCompleted ? FAI.Trophy : (progress != 0 ? FAI.Stopwatch : FAI.Trophy);
                        var color = achievementItem.IsCompleted ? ImGuiColors.ParsedGold : (progress != 0 ? ImGuiColors.DalamudGrey : ImGuiColors.DalamudGrey3);
                        var tooltip = achievementItem.IsCompleted ? "Achievement Completed!" : (progress != 0 ? "Achievement in Progress" : "Achievement Not Started");
                        ImGui.AlignTextToFramePadding();
                        CkGui.IconText(icon, color);
                        CkGui.AttachToolTip(tooltip);

                        // beside it, draw out the achievement's Title in white text.
                        ImUtf8.SameLineInner();
                        ImGui.AlignTextToFramePadding();
                        using (ImRaii.PushFont(UiBuilder.MonoFont)) CkGui.ColorText(achievementItem.Title, ImGuiColors.ParsedGold);
                        // Split between the title and description
                        ImGui.Separator();

                        ImGui.AlignTextToFramePadding();
                        CkGui.IconText(FAI.InfoCircle, ImGuiColors.TankBlue);

                        ImUtf8.SameLineInner();
                        ImGui.AlignTextToFramePadding();
                        var descText = achievementItem.IsSecretAchievement ? "????" : achievementItem.Description;
                        CkGui.TextWrapped(descText);
                        if (achievementItem.IsSecretAchievement)
                        {
                            CkGui.AttachToolTip("Explore GagSpeak's Features or work together with others to uncover how you obtain this Achievement!)");
                        }
                    }
                    // underneath this, we should draw the current progress towards the goal.
                    DrawProgressForAchievement(achievementItem);
                    if(ImGui.IsItemHovered() && achievementItem is DurationAchievement)
                        CkGui.AttachToolTip((achievementItem as DurationAchievement)?.GetActiveItemProgressString() ?? "NO PROGRESS");

                    // draw the text in the second column.
                    ImGui.TableNextColumn();
                    // Ensure its a valid texture wrap
                    if (CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg] is { } wrap)
                        ImGui.Image(wrap.ImGuiHandle, AchievementIconSize);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to draw achievement progress box.");
            }

        }
    }


    // Referenced draw-list structure for progress bar from DevUI Bar's and Mare's Progress bar.
    // https://github.com/Penumbra-Sync/client/blob/e35ed1b5297437cbcaa3dca5f5a089033c996020/MareSynchronos/UI/DownloadUi.cs#L138

    private const int Transparency = 100;
    private const int ProgressBarBorder = 1;
    private void DrawProgressForAchievement(AchievementBase achievement)
    {
        var region = ImGui.GetContentRegionAvail(); // content region
        var padding = ImGui.GetStyle().FramePadding; // padding

        // grab progress and milestone to help with drawing the progress bar.
        var completionPercentage = achievement.CurrentProgressPercentage();
        if(completionPercentage > 1f) completionPercentage = 1f;

        // Grab the displaytext for the progress bar.
        var progressBarString = achievement.ProgressString();
        var progressBarStringTextSize = ImGui.CalcTextSize(progressBarString);

        // move the cursor screen pos to the bottom of the content region - the progress bar height.
        ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X + ImGuiHelpers.GlobalScale, ImGui.GetCursorScreenPos().Y + region.Y - ((int)progressBarStringTextSize.Y + 5)));

        // grab the current cursor screen pos.
        var pos = ImGui.GetCursorScreenPos();

        // define the progress bar height and width for the windows drawlist.
        var progressHeight = (int)progressBarStringTextSize.Y + 2;
        var progressWidth = (int)(region.X - padding.X);

        // mark the starting position of our progress bar in the drawlist.
        var progressBarDrawStart = pos;

        // mark the ending position of the progress bar in the drawlist.
        var progressBarDrawEnd = new Vector2(pos.X + progressWidth, pos.Y + progressHeight);

        // grab the WINDOW draw list
        var drawList = ImGui.GetWindowDrawList();

        // Parsed Pink == (225,104,168,255)


        drawList.AddRectFilled( // The Outer Border of the progress bar
            progressBarDrawStart with { X = progressBarDrawStart.X - ProgressBarBorder - 1, Y = progressBarDrawStart.Y - ProgressBarBorder - 1 },
            progressBarDrawEnd with { X = progressBarDrawEnd.X + ProgressBarBorder + 1, Y = progressBarDrawEnd.Y + ProgressBarBorder + 1 },
            CkGui.Color(0, 0, 0, Transparency),
            25f,
            ImDrawFlags.RoundCornersAll);

        drawList.AddRectFilled( // The inner Border of the progress bar
            progressBarDrawStart with { X = progressBarDrawStart.X - ProgressBarBorder, Y = progressBarDrawStart.Y - ProgressBarBorder },
            progressBarDrawEnd with { X = progressBarDrawEnd.X + ProgressBarBorder, Y = progressBarDrawEnd.Y + ProgressBarBorder },
            CkGui.Color(220, 220, 220, Transparency),
            25f,
            ImDrawFlags.RoundCornersAll);

        drawList.AddRectFilled( // The progress bar background
            progressBarDrawStart,
            progressBarDrawEnd,
            CkGui.Color(0, 0, 0, Transparency),
            25f,
            ImDrawFlags.RoundCornersAll);

        // Do not draw the progress bar fill if it is less than .02% of the progress bar width.
        if (completionPercentage >= 0.025)
        {
            drawList.AddRectFilled( // The progress bar fill
                progressBarDrawStart,
                progressBarDrawEnd with { X = progressBarDrawStart.X + (float)(completionPercentage * (float)progressWidth) },
                CkGui.Color(225, 104, 168, 255),
                45f,
                ImDrawFlags.RoundCornersAll);
        }

        drawList.OutlinedFont(progressBarString,
            pos with { X = pos.X + ((progressWidth - progressBarStringTextSize.X) / 2f) - 1, Y = pos.Y + ((progressHeight - progressBarStringTextSize.Y) / 2f) - 1 },
            CkGui.Color(255, 255, 255, 255),
            CkGui.Color(53, 24, 39, 255),
            1);
    }
}
