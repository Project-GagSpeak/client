using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagSpeak.Utils;
using GagspeakAPI.Data.IPC;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace GagSpeak.UI.UiWardrobe;

public class WardrobeUI : WindowMediatorSubscriberBase
{
    private readonly WardrobeTabMenu _tabMenu;
    private readonly WardrobeHandler _handler;
    private readonly RestraintSetManager _overviewPanel;
    private readonly StruggleSim _struggleSimPanel;
    private readonly CursedDungeonLoot _cursedLootPanel;
    private readonly MoodlesManager _moodlesPanel;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;
    public WardrobeUI(ILogger<WardrobeUI> logger, GagspeakMediator mediator, 
        WardrobeHandler handler, RestraintSetManager restraintOverview, 
        StruggleSim struggleSim, CursedDungeonLoot cursedLoot,
        MoodlesManager moodlesManager, CosmeticService cosmetics,
        UiSharedService uiSharedService, TutorialService guides) : base(logger, mediator, "Wardrobe UI")
    {
        _handler = handler;
        _overviewPanel = restraintOverview;
        _struggleSimPanel = struggleSim;
        _cursedLootPanel = cursedLoot;
        _moodlesPanel = moodlesManager;
        _cosmetics = cosmetics;
        _uiShared = uiSharedService;
        _guides = guides;

        _tabMenu = new WardrobeTabMenu(_uiShared);

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
                Click = (msg) =>
                {
                    if(_tabMenu.SelectedTab == WardrobeTabs.Tabs.ManageSets)
                    {
                        if(_guides.IsTutorialActive(TutorialType.Restraints))
                        {
                            _guides.SkipTutorial(TutorialType.Restraints);
                            _logger.LogInformation("Skipping Restraints Tutorial");
                        }
                        else
                        {
                            _guides.StartTutorial(TutorialType.Restraints);
                            _logger.LogInformation("Starting Restraints Tutorial");
                        }
                    }
                    else if(_tabMenu.SelectedTab == WardrobeTabs.Tabs.CursedLoot)
                    {
                        if(_guides.IsTutorialActive(TutorialType.CursedLoot))
                        {
                            _guides.SkipTutorial(TutorialType.CursedLoot);
                            _logger.LogInformation("Skipping CursedLoot Tutorial");
                        }
                        else
                        {
                            _guides.StartTutorial(TutorialType.CursedLoot);
                            _logger.LogInformation("Starting CursedLoot Tutorial");
                        }
                    }
                },
                IconOffset = new(2, 1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    var text = _tabMenu.SelectedTab switch
                    {
                        WardrobeTabs.Tabs.ManageSets => "Start/Stop Restraints Tutorial",
                        WardrobeTabs.Tabs.CursedLoot => "Start/Stop Cursed Loot Tutorial",
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

    protected override void DrawInternal()
    {
        //_logger.LogInformation(ImGui.GetWindowSize().ToString()); // <-- USE FOR DEBUGGING ONLY.
        // get information about the window region, its item spacing, and the topleftside height.
        var region = ImGui.GetContentRegionAvail();
        LastWinPos = ImGui.GetWindowPos();
        LastWinSize = ImGui.GetWindowSize();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;
        var cellPadding = ImGui.GetStyle().CellPadding;

        // create the draw-table for the selectable and viewport displays
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));
        try
        {
            using (var table = ImRaii.Table($"WardrobeUiWindowTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
            {
                if (!table) return;
                // setup the columns for the table
                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();
                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

                using (var leftChild = ImRaii.Child($"###WardrobeLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    var iconTexture = _cosmetics.CorePluginTextures[CorePluginTexture.Logo256];
                    if (iconTexture is { } wrap)
                    {
                        UtilsExtensions.ImGuiLineCentered("###WardrobeLogo", () =>
                        {
                            ImGui.Image(wrap.ImGuiHandle, new(125f * _uiShared.GetFontScalerFloat(), 125f * _uiShared.GetFontScalerFloat()));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text($"What's this? A tooltip hidden in plain sight?");
                                ImGui.EndTooltip();
                            }
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                UnlocksEventManager.AchievementEvent(UnlocksEvent.EasterEggFound, "Wardrobe");
                        });
                    }
                    // add separator
                    ImGui.Spacing();
                    ImGui.Separator();
                    // add the tab menu for the left side.
                    _tabMenu.DrawSelectableTabMenu();
                }
                // pop pushed style variables and draw next column.
                ImGui.PopStyleVar();
                ImGui.TableNextColumn();
                
                // display right half viewport based on the tab selection
                using (var rightChild = ImRaii.Child($"###WardrobeSetupRight", Vector2.Zero, false))
                {
                    switch (_tabMenu.SelectedTab)
                    {
                        case WardrobeTabs.Tabs.ManageSets:
                            _overviewPanel.DrawManageSets(cellPadding);
                            break;
                        case WardrobeTabs.Tabs.StruggleSim:
                            _struggleSimPanel.DrawStruggleSim();
                            break;
                        case WardrobeTabs.Tabs.CursedLoot:
                            _cursedLootPanel.DrawCursedLootPanel();
                            break;
                        case WardrobeTabs.Tabs.ManageMoodles:
                            _moodlesPanel.DrawMoodlesManager(cellPadding);
                            break;
                        default:
                            break;
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }
        finally
        {
            ImGui.PopStyleVar();
        }
    }
}
