using Dalamud.Interface.Utility;
using GagSpeak.CkCommons;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;
using static GagSpeak.UI.Components.WardrobeTabs;

namespace GagSpeak.UI.Wardrobe;

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

        _tabMenu = new WardrobeTabs();
        _tabMenu.AddDrawButton(_cosmetics.CoreTextures[CoreTexture.Restrained], SelectedTab.MyRestraints,
            "Restraints--SEP--Apply, Lock, Unlock, Remove, or Configure your various Restraints");
        _tabMenu.AddDrawButton(_cosmetics.CoreTextures[CoreTexture.RestrainedArmsLegs], SelectedTab.MyRestrictions,
            "Restrictions--SEP--Apply, Lock, Unlock, Remove, or Configure your various Restrictions");
        _tabMenu.AddDrawButton(_cosmetics.CoreTextures[CoreTexture.Gagged], SelectedTab.MyGags,
            "Gags--SEP--Apply, Lock, Unlock, Remove, or Configure your various Gags");
        _tabMenu.AddDrawButton(_cosmetics.CoreTextures[CoreTexture.CursedLoot], SelectedTab.MyCursedLoot,
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
            MinimumSize = new Vector2(600, 470),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        RespectCloseHotkey = false;
    }
    private WardrobeTabs _tabMenu { get; init; }
    private bool ThemePushed = false;
    private static float LeftLength = 275f * ImGuiHelpers.GlobalScale;

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

    // THE FOLLOWING IS A TEMPORARY PLACEHOLDER UI DESIGN MADE TO SIMPLY VERIFY THINGS ACTUALLY CAN BUILD. DESIGN LATER.
    protected override void DrawInternal()
    {

        var wdl = ImGui.GetWindowDrawList();
        var winMinPos = wdl.GetClipRectMin();
        var winMaxPos = wdl.GetClipRectMax();

        var winPadding = ImGui.GetStyle().WindowPadding;
        var headerLeftInner = new Vector2(LeftLength, ImGui.GetFrameHeight());
        var splitterSize = headerLeftInner.Y;
        var isEditing = IsEditing(_tabMenu.TabSelection);


        // Restraints Module is Special <3
        if (_tabMenu.TabSelection is SelectedTab.MyRestraints)
        {
            // if we are editing draw the editor header, otherwise, draw the normal header.
            if (isEditing)
            {
                var rsEditorRegions = DrawerHelpers.FlatHeaderWithCurve(CkColor.FancyHeader.Uint(), headerLeftInner.Y, headerLeftInner.Y);
                _restraintPanel.DrawEditorContents(rsEditorRegions.Top, rsEditorRegions.Bottom);
                return;
            }
        }

        // Otherwise, perform the normal logic for these.
        var drawRegions = DrawerHelpers.CurvedHeader(isEditing, CkColor.FancyHeader.Uint(), headerLeftInner, headerLeftInner.Y);

        switch (_tabMenu.TabSelection)
        {
            case SelectedTab.MyRestraints:
                _restraintPanel.DrawContents(drawRegions, splitterSize, _tabMenu);
                break;

            case SelectedTab.MyRestrictions:
                if (isEditing) _restrictionsPanel.DrawEditorContents(drawRegions, splitterSize);
                else _restrictionsPanel.DrawContents(drawRegions, splitterSize, _tabMenu);
                break;

            case SelectedTab.MyGags:
                if (isEditing) _gagRestrictionsPanel.DrawEditorContents(drawRegions, splitterSize);
                else _gagRestrictionsPanel.DrawContents(drawRegions, splitterSize, _tabMenu);
                break;

            case SelectedTab.MyCursedLoot:
                _cursedLootPanel.DrawContents(drawRegions, splitterSize, _tabMenu);
                break;
        }
    }

    private bool IsEditing(SelectedTab tab)
    => tab switch
    {
        SelectedTab.MyRestraints => _restraintPanel.IsEditing,
        SelectedTab.MyRestrictions => _restrictionsPanel.IsEditing,
        SelectedTab.MyGags => _gagRestrictionsPanel.IsEditing,
        SelectedTab.MyCursedLoot => true,
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
