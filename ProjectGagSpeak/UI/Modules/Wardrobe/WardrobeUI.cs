using Dalamud.Interface;
using Dalamud.Interface.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;

public class WardrobeUI : WindowMediatorSubscriberBase
{
    private readonly WardrobeTabs _tabMenu;
    private readonly RestraintsPanel _restraintPanel;
    private readonly RestrictionsPanel _restrictionsPanel;
    private readonly GagRestrictionsPanel _gagRestrictionsPanel;
    private readonly CursedLootPanel _cursedLootPanel;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;
    public WardrobeUI(
        ILogger<WardrobeUI> logger,
        GagspeakMediator mediator,
        RestraintsPanel restraintPanel,
        RestrictionsPanel restrictionsPanel,
        GagRestrictionsPanel gagRestrictionsPanel,
        CursedLootPanel cursedLootPanel,
        CosmeticService cosmetics,
        UiSharedService uiShared,
        TutorialService guides) : base(logger, mediator, "Wardrobe UI")
    {
        _restraintPanel = restraintPanel;
        _restrictionsPanel = restrictionsPanel;
        _gagRestrictionsPanel = gagRestrictionsPanel;
        _cursedLootPanel = cursedLootPanel;
        _cosmetics = cosmetics;
        _uiShared = uiShared;
        _guides = guides;

        _tabMenu = new WardrobeTabs(_uiShared);

        AllowPinning = false;
        AllowClickthrough = false;
        TitleBarButtons = new()
        {
            new TitleBarButton()
            {
                Icon = FontAwesomeIcon.CloudDownloadAlt,
                Click = (msg) =>
                {
                    Mediator.Publish(new UiToggleMessage(typeof(MigrationsUI)));
                },
                IconOffset = new(2,1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Migrate Old Restraint Sets");
                    ImGui.EndTooltip();
                }
            },
            new TitleBarButton()
            {
                Icon = FontAwesomeIcon.QuestionCircle,
                Click = (msg) => TutorialClickedAction(),
                IconOffset = new (2, 1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    var text = _tabMenu.TabSelection switch
                    {
                        WardrobeTabs.SelectedTab.MyRestraints => "Start/Stop Restraints Tutorial",
                        WardrobeTabs.SelectedTab.MyRestrictions => "Start/Stop Restrictions Tutorial",
                        WardrobeTabs.SelectedTab.MyGags => "Start/Stop Gags Tutorial",
                        WardrobeTabs.SelectedTab.MyCursedLoot => "Start/Stop Cursed Loot Tutorial",
                        WardrobeTabs.SelectedTab.MyModPresets => "Start/Stop Mod Presets Tutorial",
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
            MinimumSize = new Vector2(760, 470),
            MaximumSize = new Vector2(760*1.5f, 1000f)
        };
        RespectCloseHotkey = false;
    }

    private bool ThemePushed = false;
    public static Vector2 LastWinPos = Vector2.Zero;
    public static Vector2 LastWinSize = Vector2.Zero;

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

    // THE FOLLOWING IS A TEMPORARY PLACEHOLDER UI DESIGN MADE TO SIMPLY VERIFY THINGS ACTUALLY CAN BUILD. DESIGN LATER.
    protected override void DrawInternal()
    {
        LastWinPos = ImGui.GetWindowPos();
        LastWinSize = ImGui.GetWindowSize();
        //_logger.LogInformation(LastWinSize.ToString()); // <-- USE FOR DEBUGGING ONLY.
        var region = ImGui.GetContentRegionAvail();
        // Store styles to restore later.
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var cellPadding = ImGui.GetStyle().CellPadding;

        // We should first begin by drawing the search bar and the selector on the left, as in the final design the buttons will be to the right.

        // For now, instead, simply display the horizontal region for the tab selection.
        _tabMenu.Draw(GetSelectorSize());

        ImGui.Separator();
        // Now we should draw out the contents of the respective tab. Each tab having their own set of rules.
        switch (_tabMenu.TabSelection)
        {
            case WardrobeTabs.SelectedTab.MyRestraints:
                _restraintPanel.DrawPanel(region, GetSelectorSize());
                break;
            case WardrobeTabs.SelectedTab.MyRestrictions:
                _restrictionsPanel.DrawPanel(region, GetSelectorSize());
                break;
            case WardrobeTabs.SelectedTab.MyGags:
                _gagRestrictionsPanel.DrawPanel(region, GetSelectorSize());
                break;
            case WardrobeTabs.SelectedTab.MyCursedLoot:
                _cursedLootPanel.DrawPanel(region, GetSelectorSize());
                break;
            case WardrobeTabs.SelectedTab.MyModPresets:
                ImGui.Text("Mod Presets Content");
                break;
        }

        // All content should be drawn by this point.
        // if we want to move the tutorial down to the bottom right we can draw that here.
    }

    private float GetSelectorSize() => 300f * ImGuiHelpers.GlobalScale;


    private void TutorialClickedAction()
    {
        switch (_tabMenu.TabSelection)
        {
            case WardrobeTabs.SelectedTab.MyRestraints:
                if (_guides.IsTutorialActive(TutorialType.Restraints))
                {
                    _guides.SkipTutorial(TutorialType.Restraints);
                    _logger.LogInformation("Skipping Restraints Tutorial");
                }
                else
                {
                    _guides.StartTutorial(TutorialType.Restraints);
                    _logger.LogInformation("Starting Restraints Tutorial");
                }
                return;
            case WardrobeTabs.SelectedTab.MyRestrictions:
                return;
            // DO LATER
            case WardrobeTabs.SelectedTab.MyGags:
                return;
            // DO LATER
            case WardrobeTabs.SelectedTab.MyCursedLoot:
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
            case WardrobeTabs.SelectedTab.MyModPresets:
                return;
        }
    }
}
