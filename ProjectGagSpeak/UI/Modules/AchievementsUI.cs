using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using ImGuiNET;
using NAudio.Utils;
using OtterGui.Text;

namespace GagSpeak.Gui;

// this can easily become the "contact list" tab of the "main UI" window.
public class AchievementsUI : WindowMediatorSubscriberBase
{
    private readonly AchievementTabs _tabMenu;
    private readonly CosmeticService _textures;
    private readonly ListItemDrawer _drawer;
    public bool ThemePushed = false;

    public AchievementsUI(ILogger<AchievementsUI> logger, GagspeakMediator mediator,
        AchievementTabs tabMenu, CosmeticService textures, ListItemDrawer drawer)
        : base(logger, mediator, "Achievements###AchievementsUI")
    {
        _tabMenu = tabMenu;
        _textures = textures;
        _drawer = drawer;

        Flags |= WFlags.NoDocking;
        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(525, 400), new Vector2(525, 2000));
    }

    private string _searchStr = string.Empty;
    private static readonly Vector2 IconSize = new(96, 96);

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
        CenteredHeader();
        // draw the tab menu for the achievement types.
        _tabMenu.Draw(ImGui.GetContentRegionAvail().X);

        // draw the search filter.
        DrawSearchFilter(ImGui.GetContentRegionAvail().X, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.Separator();

        // obtain the type based on the selected tab, and the unlocks for that category.
        var type = GetTypeFromTab();

        // draw the resulting list, wrapped in a child to prevent push pop errors (black magic shit dont ask me)
        using (var _ = CkRaii.Child("ListGuardingChild", ImGui.GetContentRegionAvail()))
            _drawer.DrawAchievementList(type, _.InnerRegion);
    }

    private void CenteredHeader()
    {
        var text = $"GagSpeak Achievements ({ClientAchievements.Completed}/{ClientAchievements.Total})";
        using (UiFontService.UidFont.Push())
        {
            var uidTextSize = ImGui.CalcTextSize(text);
            ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) / 2 - uidTextSize.X / 2);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().WindowPadding.Y / 2);
            // display it, it should be green if connected and red when not.
            ImGui.TextColored(ImGuiColors.ParsedGold, text);
        }
    }

    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
        ImGui.SetNextItemWidth(availableWidth - CkGui.IconTextButtonSize(FAI.Ban, "Clear") - spacingX);
        ImGui.InputTextWithHint("##AchievementSearchStringFilter", "Search for an Achievement...", ref _searchStr, 255);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Ban, "Clear", disabled: string.IsNullOrEmpty(_searchStr)))
            _searchStr = string.Empty;
        CkGui.AttachToolTip("Clear the search filter.");
    }

    private AchievementModuleKind GetTypeFromTab()
        => _tabMenu.TabSelection switch
        {
            AchievementTabs.SelectedTab.Gags => AchievementModuleKind.Gags,
            AchievementTabs.SelectedTab.Wardrobe => AchievementModuleKind.Wardrobe,
            AchievementTabs.SelectedTab.Puppeteer => AchievementModuleKind.Puppeteer,
            AchievementTabs.SelectedTab.Toybox => AchievementModuleKind.Toybox,
            AchievementTabs.SelectedTab.Remotes => AchievementModuleKind.Remotes,
            AchievementTabs.SelectedTab.Arousal => AchievementModuleKind.Arousal,
            AchievementTabs.SelectedTab.Hardcore => AchievementModuleKind.Hardcore,
            AchievementTabs.SelectedTab.Secrets => AchievementModuleKind.Secrets,
            _ => AchievementModuleKind.Generic
        };
    
}
