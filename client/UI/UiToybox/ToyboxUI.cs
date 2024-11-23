using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagSpeak.Utils;
using GagspeakAPI.Data.IPC;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace GagSpeak.UI.UiToybox;

public class ToyboxUI : WindowMediatorSubscriberBase
{
    private readonly ToyboxTabMenu _tabMenu;
    private readonly ToyboxOverview _toysOverview;
    private readonly ToyboxPrivateRooms _vibeServer;
    private readonly ToyboxPatterns _patterns;
    private readonly ToyboxTriggerManager _triggerManager;
    private readonly ToyboxAlarmManager _alarmManager;
    private readonly PatternPlayback _patternPlayback;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;

    // mapping tutorials.
    private static readonly Dictionary<object, (TutorialType Type, string StartLog, string SkipLog)> TutorialMap = new Dictionary<object, (TutorialType Type, string StartLog, string SkipLog)>()
    {
        { ToyboxTabs.Tabs.ToyOverview, (TutorialType.Toybox, "Starting Toybox Tutorial", "Skipping Toybox Tutorial") },
        { ToyboxTabs.Tabs.PatternManager, (TutorialType.Patterns, "Starting Patterns Tutorial", "Skipping Patterns Tutorial") },
        { ToyboxTabs.Tabs.TriggerManager, (TutorialType.Triggers, "Starting Triggers Tutorial", "Skipping Triggers Tutorial") },
        { ToyboxTabs.Tabs.AlarmManager, (TutorialType.Alarms, "Starting Alarms Tutorial", "Skipping Alarms Tutorial") }
    };

    public ToyboxUI(ILogger<ToyboxUI> logger, GagspeakMediator mediator,
        ToyboxOverview toysOverview, ToyboxPrivateRooms vibeServer, ToyboxPatterns patterns,
        ToyboxTriggerManager triggerManager, ToyboxAlarmManager alarmManager,
        PatternPlayback playback, CosmeticService cosmetics, UiSharedService uiShared,
        TutorialService guides) : base(logger, mediator, "Toybox UI")
    {
        _toysOverview = toysOverview;
        _vibeServer = vibeServer;
        _patterns = patterns;
        _triggerManager = triggerManager;
        _alarmManager = alarmManager;
        _patternPlayback = playback;
        _cosmetics = cosmetics;
        _uiShared = uiShared;
        _guides = guides;

        _tabMenu = new ToyboxTabMenu(_uiShared);

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
                    ImGui.Text("Migrate Old Pattern Data");
                    ImGui.EndTooltip();
                }
            },
            new TitleBarButton()
            {
                Icon = FontAwesomeIcon.QuestionCircle,
                Click = (msg) =>
                {
                    // Check if the current tab has an associated tutorial
                    if (TutorialMap.TryGetValue(_tabMenu.SelectedTab, out var tutorialInfo))
                    {
                        // Perform tutorial actions
                        if (_guides.IsTutorialActive(tutorialInfo.Type))
                        {
                            _guides.SkipTutorial(tutorialInfo.Type);
                            _logger.LogInformation(tutorialInfo.SkipLog);
                        }
                        else
                        {
                            _guides.StartTutorial(tutorialInfo.Type);
                            _logger.LogInformation(tutorialInfo.StartLog);
                        }
                    }
                },
                IconOffset = new(2, 1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    var text = _tabMenu.SelectedTab switch
                    {
                        ToyboxTabs.Tabs.ToyOverview => "Start/Stop Toybox Tutorial",
                        ToyboxTabs.Tabs.PatternManager => "Start/Stop Patterns Tutorial",
                        ToyboxTabs.Tabs.TriggerManager => "Start/Stop Triggers Tutorial",
                        ToyboxTabs.Tabs.AlarmManager => "Start/Stop Alarms Tutorial",
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
            MinimumSize = new Vector2(525, 500),
            MaximumSize = new Vector2(575, 500)
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
        // get information about the window region, its item spacing, and the topleftside height.
        var region = ImGui.GetContentRegionAvail();
        LastWinPos = ImGui.GetWindowPos();
        LastWinSize = ImGui.GetWindowSize();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var cellPadding = ImGui.GetStyle().CellPadding;
        var topLeftSideHeight = region.Y;

        // create the draw-table for the selectable and viewport displays
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));
        try
        {
            using (var table = ImRaii.Table($"ToyboxUiWindowTable", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
            {
                if (!table) return;

                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 200 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();

                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

                using (var leftChild = ImRaii.Child($"###ToyboxLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    // attempt to obtain an image wrap for it
                    var iconTexture = _cosmetics.CorePluginTextures[CorePluginTexture.Logo256];
                    if (iconTexture is { } wrap)
                    {
                        // aligns the image in the center like we want.
                        UtilsExtensions.ImGuiLineCentered("###ToyboxLogo", () =>
                        {
                            ImGui.Image(wrap.ImGuiHandle, new(125f * _uiShared.GetFontScalerFloat(), 125f * _uiShared.GetFontScalerFloat()));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text($"What's this? A tooltip hidden in plain sight?");
                                ImGui.EndTooltip();
                            }
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                                UnlocksEventManager.AchievementEvent(UnlocksEvent.EasterEggFound, "Toybox");
                        });
                    }
                    // add separator
                    ImGui.Spacing();
                    ImGui.Separator();
                    // add the tab menu for the left side.
                    _tabMenu.DrawSelectableTabMenu();

                    ImGui.SetCursorPosY(region.Y - 80f);
                    _patternPlayback.DrawPlaybackDisplay();
                }
                // pop pushed style variables and draw next column.
                ImGui.PopStyleVar();

                ImGui.TableNextColumn();

                // display right half viewport based on the tab selection
                using (var rightChild = ImRaii.Child($"###ToyboxRight", Vector2.Zero, false))
                {
                    switch (_tabMenu.SelectedTab)
                    {
                        case ToyboxTabs.Tabs.ToyOverview:
                            _toysOverview.DrawOverviewPanel();
                            break;
                        case ToyboxTabs.Tabs.VibeServer:
                            _vibeServer.DrawVibeServerPanel();
                            break;
                        case ToyboxTabs.Tabs.PatternManager:
                            _patterns.DrawPatternManagerPanel();
                            break;
                        case ToyboxTabs.Tabs.TriggerManager:
                            _triggerManager.DrawTriggersPanel();
                            break;
                        case ToyboxTabs.Tabs.AlarmManager:
                            _alarmManager.DrawAlarmManagerPanel();
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
