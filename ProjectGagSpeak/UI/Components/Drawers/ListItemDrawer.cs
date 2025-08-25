using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Managers;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

// Scoped, sealed class to draw the editor and display components of aliasItems.
public sealed class ListItemDrawer
{
    private readonly ILogger<AliasItemDrawer> _logger;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly PuppeteerManager _manager;
    private readonly AliasItemDrawer _AAAAAAAAAAA;
    private readonly MoodleDrawer _moodleDrawer;

    private static readonly string[] ThreeLayerNames = [ "Layer 1", "Layer 2", "Layer 3", "Any Layer" ];
    private static readonly string[] FiveLayerNames = [ "Layer 1", "Layer 2", "Layer 3", "Layer 4", "Layer 5", "Any Layer" ];
    private HashSet<Guid> ExpandedTriggers = new HashSet<Guid>();
    public ListItemDrawer(
        ILogger<AliasItemDrawer> logger,
        GagspeakMediator mediator,
        MoodleDrawer moodleDrawer,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        PuppeteerManager manager,
        AliasItemDrawer AAAAAAAAAAA,
        FavoritesManager favorites)
    {
        _logger = logger;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _manager = manager;
        _moodleDrawer = moodleDrawer;
        _AAAAAAAAAAA = AAAAAAAAAAA;
    }

    public void DrawAchievementList(AchievementModuleKind type, Vector2 region)
    {
        // Push styles for our inner child items.
        using var _ = CkRaii.Child("AchievementList", region, wFlags: WFlags.NoScrollbar);

        var unlocks = ClientAchievements.GetByModule(type);
        if (!unlocks.Any())
            return;

        // filter down the unlocks to searchable results.
        var filteredUnlocks = unlocks
            .Where(goal => goal.Title.Contains(string.Empty, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var size = new Vector2(ImGui.GetContentRegionAvail().X, 96f.AddWinPadY() + ImGui.GetStyle().CellPadding.Y * 2);
        foreach (var achievement in filteredUnlocks.ToList())
            DrawAchievementProgressBox(achievement, size);
    }

    public void DrawAchievementProgressBox(AchievementBase achievementItem, Vector2 size)
    {
        var imageTabWidth = 96 + ImGui.GetStyle().ItemSpacing.X * 2;
        using var _ = CkRaii.FramedChild($"Achievement-{achievementItem.Title}", size, new Vector4(0.25f, 0.2f, 0.2f, 0.4f).ToUint(), 
            CkColor.VibrantPink.Uint(), 5f, 1f, wFlags: WFlags.AlwaysUseWindowPadding);

        using var t = ImRaii.Table($"AchievementTable {achievementItem.Title}", 2, ImGuiTableFlags.RowBg);
        if (!t) return;

        ImGui.TableSetupColumn("##AchievementText", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##AchievementIcon", ImGuiTableColumnFlags.WidthFixed, 96);

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
                CkGui.AttachToolTip("Explore GagSpeak's Features or work together with others to uncover how you obtain this Achievement!)");
        }
        // underneath this, we should draw the current progress towards the goal.
        DrawProgressForAchievement(achievementItem);
        if (ImGui.IsItemHovered() && achievementItem is DurationAchievement)
            CkGui.AttachToolTip((achievementItem as DurationAchievement)?.GetActiveItemProgressString() ?? "NO PROGRESS");

        // draw the text in the second column.
        ImGui.TableNextColumn();
        // Ensure its a valid texture wrap
        if (CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg] is { } wrap)
            ImGui.Image(wrap.Handle, new(96, 96));
    }


    // Referenced draw-list structure for progress bar from DevUI Bar's and Mare's Progress bar.
    private const int Transparency = 100;
    private const int ProgressBarBorder = 1;
    private void DrawProgressForAchievement(AchievementBase achievement)
    {
        var region = ImGui.GetContentRegionAvail(); // content region
        var padding = ImGui.GetStyle().FramePadding; // padding

        // grab progress and milestone to help with drawing the progress bar.
        var completionPercentage = achievement.CurrentProgressPercentage();
        if (completionPercentage > 1f) completionPercentage = 1f;

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
