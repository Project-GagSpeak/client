using CkCommons;
using CkCommons.Widgets;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
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

        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained], SelectedTab.MyRestraints,
            "Restraints--SEP--Apply, Lock, Unlock, Remove, or Configure your various Restraints");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.RestrainedArmsLegs], SelectedTab.MyRestrictions,
            "Restrictions--SEP--Apply, Lock, Unlock, Remove, or Configure your various Restrictions");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged], SelectedTab.MyGags,
            "Gags--SEP--Apply, Lock, Unlock, Remove, or Configure your various Gags");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.CursedLoot], SelectedTab.MyCursedLoot,
            "Cursed Loot--SEP--Configure your Cursed Items, or manage the active Loot Pool.");

        AllowPinning = false;
        AllowClickthrough = false;
        TitleBarButtons = new()
        {
            new TitleBarButton()
            {
                Icon = FAI.CloudDownloadAlt,
                Click = (msg) => Mediator.Publish(new UiToggleMessage(typeof(MigrationsUI))),
                IconOffset = new(2,1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Migrate Old Restriction Sets");
                    ImGui.EndTooltip();
                }
            },
            new TitleBarButton()
            {
                Icon = FAI.QuestionCircle,
                Click = (msg) => TutorialClickedAction(),
                IconOffset = new (2, 1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    var text = _tabMenu.TabSelection switch
                    {
                        SelectedTab.MyRestraints => "Start/Stop Restraints Tutorial",
                        SelectedTab.MyRestrictions => "Start/Stop Restrictions Tutorial",
                        SelectedTab.MyGags => "Start/Stop Gags Tutorial",
                        SelectedTab.MyCursedLoot => "Start/Stop Cursed Loot Tutorial",
                        _ => "No Tutorial Available"
                    };
                    ImGui.Text(text);
                    ImGui.EndTooltip();
                }
            }
        };

        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 490),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        RespectCloseHotkey = false;
    }

    private static WardrobeTabs _tabMenu = new WardrobeTabs();
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

    private void TutorialClickedAction()
    {
        switch (_tabMenu.TabSelection)
        {
            case SelectedTab.MyRestraints:
                if (_guides.IsTutorialActive(TutorialType.Restraints))
                {
                    _guides.SkipTutorial(TutorialType.Restraints);
                    _logger.LogInformation("Skipping Restrictions Tutorial");
                }
                else
                {
                    _guides.StartTutorial(TutorialType.Restraints);
                    _logger.LogInformation("Starting Restrictions Tutorial");
                }
                return;
            case SelectedTab.MyRestrictions:
                return;
            // DO LATER
            case SelectedTab.MyGags:
                return;
            // DO LATER
            case SelectedTab.MyCursedLoot:
                if (_guides.IsTutorialActive(TutorialType.CursedLoot))
                {
                    _guides.SkipTutorial(TutorialType.CursedLoot);
                    _logger.LogInformation("Skipping CursedLoot Tutorial");
                }
                else
                {
                    _guides.StartTutorial(TutorialType.CursedLoot);
                    _logger.LogInformation("Starting CursedLoot Tutorial");
                }
                return;
        }
    }
}
