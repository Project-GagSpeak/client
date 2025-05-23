/*using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.UiGagSetup;

public class GagSetupUI : WindowMediatorSubscriberBase
{
    private readonly GagSetupTabMenu _tabMenu;
    private readonly ActiveGagsPanel _activeGags;
    private readonly LockPickerSim _lockPickSim;
    private readonly GagRestrictionsPanel _gagStorage;
    private readonly GlobalData _playerManager;
    private readonly CosmeticService _cosmetics;
    private readonly CkGui CkGui.;
    private readonly TutorialService _guides;

    public GagSetupUI(ILogger<GagSetupUI> logger, GagspeakMediator mediator,
        ActiveGagsPanel activeGags, LockPickerSim lockPickSim, GagRestrictionsPanel gagStorage, 
        GlobalData playerManager, CosmeticService cosmetics,
        CkGui uiShared, TutorialService guides) : base(logger, mediator, "Gag Setup UI")
    {
        _playerManager = playerManager;
        _activeGags = activeGags;
        _lockPickSim = lockPickSim;
        _gagStorage = gagStorage;
        _cosmetics = cosmetics;
        CkGui. = uiShared;
        _guides = guides;

        _tabMenu = new GagSetupTabMenu(CkGui.);

        AllowPinning = false;
        AllowClickthrough = false;
        TitleBarButtons = new()
        {
            new TitleBarButton()
            {
                Icon = FAI.CloudDownloadAlt,
                Click = (msg) =>
                {
                    Mediator.Publish(new UiToggleMessage(typeof(MigrationsUI)));
                },
                IconOffset = new(2,1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Migrate Old GagStorage Files");
                    ImGui.EndTooltip();
                }
            },
            new TitleBarButton()
            {
                Icon = FAI.QuestionCircle,
                Click = (msg) =>
                {
                    if(_tabMenu.SelectedTab == GagSetupTabs.Tabs.ActiveGags)
                    {
                        if(_guides.IsTutorialActive(TutorialType.Gags))
                        {
                            _guides.SkipTutorial(TutorialType.Gags);
                            _logger.LogInformation("Skipping Gags Tutorial");
                        }
                        else
                        {
                            _guides.StartTutorial(TutorialType.Gags);
                            _logger.LogInformation("Starting Gags Tutorial");
                        }
                    }
                    else if(_tabMenu.SelectedTab == GagSetupTabs.Tabs.GagStorage)
                    {
                        if(_guides.IsTutorialActive(TutorialType.GagStorage))
                        {
                            _guides.SkipTutorial(TutorialType.GagStorage);
                            _logger.LogInformation("Skipping GagStorage Tutorial");
                        }
                        else
                        {
                            _guides.StartTutorial(TutorialType.GagStorage);
                            _logger.LogInformation("Starting GagStorage Tutorial");
                        }
                    }
                },
                IconOffset = new(2, 1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    var text = _tabMenu.SelectedTab switch
                    {
                        GagSetupTabs.Tabs.ActiveGags => "Start/Stop Gags Tutorial",
                        GagSetupTabs.Tabs.GagStorage => "Start/Stop GagStorage Tutorial",
                        _ => "No Tutorial Available"
                    };
                    ImGui.Text(text);
                    ImGui.EndTooltip();
                }
            }
        };

        // define initial size of window and to not respect the close hotkey.
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 430),
            MaximumSize = new Vector2(744, 430)
        };
        RespectCloseHotkey = false;
    }

    private bool ThemePushed = false;
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
        // get information about the window region, its item spacing, and the topleftside height.
        var region = ImGui.GetContentRegionAvail();
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;

        // create the draw-table for the selectable and viewport displays
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f * CkGui..GetFontScalerFloat(), 0));
        try
        {
            using (var table = ImRaii.Table($"GagSetupUiWindowTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
            {
                if (!table) return;
                // setup columns.
                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 200f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();
                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

                using (var leftChild = ImRaii.Child($"###GagSetupLeft", regionSize with { Y = topLeftSideHeight }, false, WFlags.NoDecoration))
                {
                    // get the gag setup logo image
                    //var iconTexture = CkGui..GetImageFromAssetsFolder("icon.png");
                    var iconTexture = _cosmetics.CoreTextures[CoreTexture.Logo256];
                    if (iconTexture is { } wrap)
                    {
                        // aligns the image in the center like we want.
                        UtilsExtensions.ImGuiLineCentered("###GagSetupLogo", () =>
                        {
                            ImGui.Image(wrap.ImGuiHandle, new(125f * CkGui..GetFontScalerFloat(), 125f * CkGui..GetFontScalerFloat()));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text($"What's this? A tooltip hidden in plain sight?");
                                ImGui.EndTooltip();
                            }
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                UnlocksEventManager.AchievementEvent(UnlocksEvent.EasterEggFound, "Gags");
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
                using (var rightChild = ImRaii.Child($"###GagSetupRight", Vector2.Zero, false))
                {
                    switch (_tabMenu.SelectedTab)
                    {
                        case GagSetupTabs.Tabs.ActiveGags: // shows the interface for inspecting or applying your own gags.
                            _activeGags.DrawActiveGagsPanel(winPos, winSize);
                            break;
                        case GagSetupTabs.Tabs.LockPicker: // shows off the gag storage configuration for the user's gags.
                            _lockPickSim.DrawLockPickingSim();
                            break;
                        case GagSetupTabs.Tabs.GagStorage: // fancy WIP thingy to give players access to features based on achievements or unlocks.
                            _gagStorage.DrawGagStoragePanel(winPos, winSize);
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
*/
