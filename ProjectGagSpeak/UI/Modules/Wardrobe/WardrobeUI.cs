using CkCommons;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using static GagSpeak.Gui.Components.WardrobeTabs;

namespace GagSpeak.Gui.Wardrobe;

public class WardrobeUI : WindowMediatorSubscriberBase
{
    private readonly RestraintsPanel _restraintPanel;
    private readonly RestrictionsPanel _restrictionsPanel;
    private readonly GagRestrictionsPanel _gagRestrictionsPanel;
    private readonly CollarPanel _collarPanel;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public WardrobeUI(
        ILogger<WardrobeUI> logger,
        GagspeakMediator mediator,
        RestraintsPanel restraintPanel,
        RestrictionsPanel restrictionsPanel,
        GagRestrictionsPanel gagRestrictionsPanel,
        CollarPanel collarPanel,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator, "Wardrobe UI")
    {
        _restraintPanel = restraintPanel;
        _restrictionsPanel = restrictionsPanel;
        _gagRestrictionsPanel = gagRestrictionsPanel;
        _collarPanel = collarPanel;
        _cosmetics = cosmetics;
        _guides = guides;

        // recompile the tab menu, along with its buttons.
        _tabMenu = new WardrobeTabs();

        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(600, 490), ImGui.GetIO().DisplaySize);
        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.CloudDownloadAlt, "Wardrobe Migrations", () => Mediator.Publish(new UiToggleMessage(typeof(MigrationsUI))))
            .AddTutorial(_guides, TutorialFromTab)
            .Build();
    }

    // Accessed by Tutorial System
    public static Vector2 LastPos { get; private set; } = Vector2.Zero;
    public static Vector2 LastSize { get; private set; } = Vector2.Zero;

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
        // Collar layout follows different rules in that it does not have different headers or editing modes.
        // If we are on the collar tab, avoid unessisary calculations by drawing it directly.
        if (_tabMenu.TabSelection is SelectedTab.MyCollar)
            DrawCollarLayout();
        else
            DrawNormalLayout();

        LastPos = ImGui.GetWindowPos();
        LastSize = ImGui.GetWindowSize();
    }

    private void DrawCollarLayout()
    {
        var frameH = ImGui.GetFrameHeight();
        var drawSpaces = CkHeader.FlatWithBends(CkCol.CurvedHeader.Uint(), 2 * frameH, frameH * 0.5f, frameH);
        _collarPanel.DrawContents(drawSpaces, RightLength(), _tabMenu);
    }

    private void DrawNormalLayout()
    {
        var frameH = ImGui.GetFrameHeight();
        var isEditing = IsEditing();
        // If we are editing, draw out the flat with bends, otherwise, draw the fancy curve.
        var drawRegions = isEditing
            ? CkHeader.FlatWithBends(CkCol.CurvedHeader.Uint(), frameH, frameH, frameH)
            : CkHeader.FancyCurve(CkCol.CurvedHeader.Uint(), frameH, frameH * .5f, RightLength(), frameH);

        // Then draw out the contents.
        switch (_tabMenu.TabSelection)
        {
            case SelectedTab.MyRestraints:
                if (isEditing) _restraintPanel.DrawEditorContents(drawRegions);
                else _restraintPanel.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case SelectedTab.MyRestrictions:
                if (isEditing) _restrictionsPanel.DrawEditorContents(drawRegions, frameH);
                else _restrictionsPanel.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case SelectedTab.MyGags:
                if (isEditing) _gagRestrictionsPanel.DrawEditorContents(drawRegions, frameH);
                else _gagRestrictionsPanel.DrawContents(drawRegions, frameH, _tabMenu);
                break;
        }
    }

    private bool IsEditing()
        => _tabMenu.TabSelection switch
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
            SelectedTab.MyRestrictions => TutorialType.Restrictions,
            SelectedTab.MyGags => TutorialType.Gags,
            _ => TutorialType.Collar,
        };
}
