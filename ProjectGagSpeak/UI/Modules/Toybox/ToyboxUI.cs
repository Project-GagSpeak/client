using Dalamud.Interface.Utility;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.UiToybox;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using ImGuiNET;

namespace GagSpeak.Gui.Toybox;

public class ToyboxUI : WindowMediatorSubscriberBase
{
    private readonly ToysPanel _sexToysPanel;
    private readonly PatternsPanel _patternsPanel;
    private readonly AlarmsPanel _alarmsPanel;
    private readonly TriggersPanel _triggersPanel;
    private readonly PlaybackDrawer _playback;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;

    public ToyboxUI(
        ILogger<ToyboxUI> logger,
        GagspeakMediator mediator,
        ToysPanel sexToysPanel,
        PatternsPanel patternsPanel,
        AlarmsPanel alarmsPanel,
        TriggersPanel triggersPanel,
        PlaybackDrawer playback,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator, "Toybox UI")
    {
        _sexToysPanel = sexToysPanel;
        _patternsPanel = patternsPanel;
        _alarmsPanel = alarmsPanel;
        _triggersPanel = triggersPanel;
        _playback = playback;
        _cosmetics = cosmetics;
        _guides = guides;

        _tabMenu.AddDrawButton(CosmeticService.CoreTextures[CoreTexture.Vibrator], ToyboxTabs.SelectedTab.ToysAndLobbies,
            "Configure & use your Toys, or join lobbies to control others");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures[CoreTexture.Stimulated], ToyboxTabs.SelectedTab.Patterns,
            "Create, Edit, and playback patterns");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures[CoreTexture.Clock], ToyboxTabs.SelectedTab.Alarms,
            "Set various Alarms that play patterns when triggered");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures[CoreTexture.CircleDot], ToyboxTabs.SelectedTab.Triggers,
            "Create various kinds of Triggers");

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
                ShowTooltip = () => ImGui.SetTooltip("Migrate Old Toybox Data")
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
                        ToyboxTabs.SelectedTab.ToysAndLobbies => "Start/Stop Toys & Vibe Lobbies Tutorial",
                        ToyboxTabs.SelectedTab.Patterns => "Start/Stop Patterns Tutorial",
                        ToyboxTabs.SelectedTab.Alarms => "Start/Stop Alarms Tutorial",
                        ToyboxTabs.SelectedTab.Triggers => "Start/Stop Triggers Tutorial",
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
            MinimumSize = new Vector2(600, 530),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        RespectCloseHotkey = false;
    }

    private static ToyboxTabs _tabMenu = new ToyboxTabs();
    private bool ThemePushed = false;

    private static float RightLength() => 300 * ImGuiHelpers.GlobalScale;

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
        var frameH = ImGui.GetFrameHeight();
        // Toys and Vibe Lobbies are special <3
        if (_tabMenu.TabSelection is ToyboxTabs.SelectedTab.ToysAndLobbies)
        {
            var talDrawRegions = CkHeader.FlatWithBends(CkColor.FancyHeader.Uint(), frameH * 2, frameH);
            _sexToysPanel.DrawContents(talDrawRegions, RightLength(), frameH, _tabMenu);
            return;
        }

        // Handle other cases.
        var drawRegions = CkHeader.FancyCurve(CkColor.FancyHeader.Uint(), frameH, frameH, RightLength(), true);

        switch (_tabMenu.TabSelection)
        {
            case ToyboxTabs.SelectedTab.Patterns:
                _patternsPanel.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case ToyboxTabs.SelectedTab.Alarms:
                _alarmsPanel.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case ToyboxTabs.SelectedTab.Triggers:
                _triggersPanel.DrawContents(drawRegions, frameH, _tabMenu);
                break;
        }
    }

    private void TutorialClickedAction()
    {
        switch (_tabMenu.TabSelection)
        {
            case ToyboxTabs.SelectedTab.ToysAndLobbies:
            case ToyboxTabs.SelectedTab.Patterns:
            case ToyboxTabs.SelectedTab.Alarms:
            case ToyboxTabs.SelectedTab.Triggers:
                // DO LATER. I hate everything! :D
                break;
        }
    }
}
