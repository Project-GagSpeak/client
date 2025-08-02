using CkCommons;
using CkCommons.Widgets;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using ImGuiNET;
using static GagSpeak.Gui.Components.WardrobeTabs;

namespace GagSpeak.Gui.Wardrobe;

public class WardrobeUI : WindowMediatorSubscriberBase
{
    private readonly RestraintsPanel _restraintPanel;
    private readonly RestrictionsPanel _restrictionsPanel;
    private readonly GagRestrictionsPanel _gagRestrictionsPanel;
    private readonly CursedLootPanel _cursedLootPanel;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public WardrobeUI(
        ILogger<WardrobeUI> logger,
        GagspeakMediator mediator,
        RestraintsPanel restraintPanel,
        RestrictionsPanel restrictionsPanel,
        GagRestrictionsPanel gagRestrictionsPanel,
        CursedLootPanel cursedLootPanel,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator, "Wardrobe UI")
    {
        _restraintPanel = restraintPanel;
        _restrictionsPanel = restrictionsPanel;
        _gagRestrictionsPanel = gagRestrictionsPanel;
        _cursedLootPanel = cursedLootPanel;
        _cosmetics = cosmetics;
        _guides = guides;

        // recompile the tab menu, along with its buttons.
        _tabMenu = new WardrobeTabs();

        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(600, 490), ImGui.GetIO().DisplaySize);
        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.CloudDownloadAlt, "Wardrobe Migrations", () => Mediator.Publish(new UiToggleMessage(typeof(MigrationsUI))))
            .AddTutorial(_guides, TutorialFromTab())
            .Build();

        RespectCloseHotkey = false;
    }

    private WardrobeTabs _tabMenu { get; init; }
    private bool ThemePushed = false;

    public static float SelectedRestrictionH() => ImGui.GetFrameHeight() * 2 + MoodleDrawer.IconSize.Y + ImGui.GetStyle().ItemSpacing.Y * 2;
    public static float SelectedRestraintH() => ImGui.GetFrameHeight() * 3 + MoodleDrawer.IconSize.Y + ImGui.GetStyle().ItemSpacing.Y * 3;
    public static float SelectedOtherH() => ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2;
    private static float RightLength() => 7 * ImGui.GetFrameHeightWithSpacing() + (SelectedRestraintH() / 1.2f);

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .403f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.428f));
            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }

    protected override void DrawInternal()
    {
        var isEditing = IsEditing(_tabMenu.TabSelection);

        // Restraints Module is Special <3
        if (_tabMenu.TabSelection is SelectedTab.MyRestraints && isEditing)
        {
            // if we are editing draw the editor header, otherwise, draw the normal header.
            var rsEditorRegions = CkHeader.FlatWithBends(CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), ImGui.GetFrameHeight());
            _restraintPanel.DrawEditorContents(rsEditorRegions.Top, rsEditorRegions.Bottom);
            return;
        }

        // Otherwise, perform the normal logic for these.
        var drawRegions = CkHeader.FancyCurve(CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), ImGui.GetFrameHeight(), RightLength(), !isEditing);
        

        switch (_tabMenu.TabSelection)
        {
            case SelectedTab.MyRestraints:
                _restraintPanel.DrawContents(drawRegions, ImGui.GetFrameHeight(), _tabMenu);
                break;

            case SelectedTab.MyRestrictions:
                if (isEditing) _restrictionsPanel.DrawEditorContents(drawRegions, ImGui.GetFrameHeight());
                else _restrictionsPanel.DrawContents(drawRegions, ImGui.GetFrameHeight(), _tabMenu);
                break;

            case SelectedTab.MyGags:
                if (isEditing) _gagRestrictionsPanel.DrawEditorContents(drawRegions, ImGui.GetFrameHeight());
                else _gagRestrictionsPanel.DrawContents(drawRegions, ImGui.GetFrameHeight(), _tabMenu);
                break;

            case SelectedTab.MyCursedLoot:
                _cursedLootPanel.DrawContents(drawRegions, ImGui.GetFrameHeight(), _tabMenu);
                break;
        }
    }

    private bool IsEditing(SelectedTab tab)
    => tab switch
    {
        SelectedTab.MyRestraints => _restraintPanel.IsEditing,
        SelectedTab.MyRestrictions => _restrictionsPanel.IsEditing,
        SelectedTab.MyGags => _gagRestrictionsPanel.IsEditing,
        _ => false,
    };

    private TutorialType TutorialFromTab()
        => _tabMenu.TabSelection switch
        {
            SelectedTab.MyRestraints => TutorialType.Restraints,
            SelectedTab.MyRestrictions => TutorialType.Restraints,
            SelectedTab.MyGags => TutorialType.Gags,
            _ => TutorialType.CursedLoot,
        };
}
