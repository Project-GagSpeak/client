using Dalamud.Interface;
using Dalamud.Interface.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagSpeak.UI.UiToybox;
using ImGuiNET;

namespace GagSpeak.UI.Toybox;

public class ToyboxUI : WindowMediatorSubscriberBase
{
    private readonly ToyboxTabs _tabMenu = new ToyboxTabs();
    private readonly SexToysPanel _sexToysPanel;
    private readonly VibeLobbiesPanel _vibeLobbyPanel;
    private readonly PatternsPanel _patternsPanel;
    private readonly AlarmsPanel _alarmsPanel;
    private readonly TriggersPanel _triggersPanel;
    private readonly PlaybackDrawer _playback;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public ToyboxUI(
        ILogger<ToyboxUI> logger,
        GagspeakMediator mediator,
        SexToysPanel sexToysPanel,
        VibeLobbiesPanel vibeLobbyPanel,
        PatternsPanel patternsPanel,
        AlarmsPanel alarmsPanel,
        TriggersPanel triggersPanel,
        PlaybackDrawer playback,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator, "Toybox UI")
    {
        _sexToysPanel = sexToysPanel;
        _vibeLobbyPanel = vibeLobbyPanel;
        _patternsPanel = patternsPanel;
        _alarmsPanel = alarmsPanel;
        _triggersPanel = triggersPanel;
        _playback = playback;
        _cosmetics = cosmetics;
        _guides = guides;

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
                ShowTooltip = () => ImGui.SetTooltip("Migrate Old Toybox Data")
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
                        ToyboxTabs.SelectedTab.ToyOverview => "Start/Stop Toy Manager Tutorial",
                        ToyboxTabs.SelectedTab.VibeServer => "Start/Stop Vibe Lobby Tutorial",
                        ToyboxTabs.SelectedTab.PatternManager => "Start/Stop Patterns Tutorial",
                        ToyboxTabs.SelectedTab.AlarmManager => "Start/Stop Alarms Tutorial",
                        ToyboxTabs.SelectedTab.TriggerManager => "Start/Stop Triggers Tutorial",
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
            MaximumSize = new Vector2(760 * 1.5f, 1000f)
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

    // THE FOLLOWING IS A TEMPORARY PLACEHOLDER UI DESIGN MADE TO SIMPLY VERIFY THINGS ACTUALLY CAN BUILD. DESIGN LATER.
    protected override void DrawInternal()
    {
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var cellPadding = ImGui.GetStyle().CellPadding;

        _tabMenu.Draw(region.X);

        ImGui.Separator();
        // Now we should draw out the contents of the respective tab. Each tab having their own set of rules.
        switch (_tabMenu.TabSelection)
        {
            case ToyboxTabs.SelectedTab.ToyOverview:
                _sexToysPanel.DrawPanel(region, GetSelectorSize());
                break;
            case ToyboxTabs.SelectedTab.VibeServer:
                _vibeLobbyPanel.DrawPanel(region, GetSelectorSize());
                break;
            case ToyboxTabs.SelectedTab.PatternManager:
                _patternsPanel.DrawPanel(region, GetSelectorSize());
                break;
            case ToyboxTabs.SelectedTab.AlarmManager:
                _alarmsPanel.DrawPanel(region, GetSelectorSize());
                break;
            case ToyboxTabs.SelectedTab.TriggerManager:
                _triggersPanel.DrawPanel(region, GetSelectorSize());
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
            case ToyboxTabs.SelectedTab.ToyOverview:
            case ToyboxTabs.SelectedTab.VibeServer:
            case ToyboxTabs.SelectedTab.PatternManager:
            case ToyboxTabs.SelectedTab.AlarmManager:
            case ToyboxTabs.SelectedTab.TriggerManager:
                // DO LATER. I hate everything! :D
                break;
        }
    }
}
