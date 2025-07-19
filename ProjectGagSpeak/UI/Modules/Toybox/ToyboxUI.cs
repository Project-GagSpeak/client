using CkCommons;
using CkCommons.Widgets;
using Dalamud.Interface.Utility;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.UiToybox;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using ImGuiNET;

namespace GagSpeak.Gui.Toybox;

public class ToyboxUI : WindowMediatorSubscriberBase
{
    private readonly ToysPanel _sexToys;
    private readonly VibeLobbiesPanel _vibeLobbies;
    private readonly PatternsPanel _patterns;
    private readonly AlarmsPanel _alarms;
    private readonly TriggersPanel _triggers;
    private readonly TutorialService _guides;

    public ToyboxUI(
        ILogger<ToyboxUI> logger,
        GagspeakMediator mediator,
        ToysPanel sexToys,
        VibeLobbiesPanel vibeLobbies,
        PatternsPanel patterns,
        AlarmsPanel alarms,
        TriggersPanel triggers,
        TutorialService guides) : base(logger, mediator, "Toybox UI")
    {
        _sexToys = sexToys;
        _vibeLobbies = vibeLobbies;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
        _guides = guides;

        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Vibrator], ToyboxTabs.SelectedTab.BuzzToys,
            "Configure your interactable Sex Toy Devices");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.VibeLobby], ToyboxTabs.SelectedTab.VibeLobbies,
            "Invite, Join, or create Vibe Rooms to play with others");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Stimulated], ToyboxTabs.SelectedTab.Patterns,
            "Create, Edit, and playback patterns");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.Clock], ToyboxTabs.SelectedTab.Alarms,
            "Set various Alarms that play patterns when triggered");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures.Cache[CoreTexture.CircleDot], ToyboxTabs.SelectedTab.Triggers,
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
                        ToyboxTabs.SelectedTab.BuzzToys => "Start/Stop Personal Toys Tutorial",
                        ToyboxTabs.SelectedTab.VibeLobbies => "Start/Stop Vibe Lobbies Tutorial",
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
        var drawRegions = CkHeader.FancyCurve(CkColor.FancyHeader.Uint(), frameH, frameH, RightLength(), true);

        switch (_tabMenu.TabSelection)
        {
            case ToyboxTabs.SelectedTab.BuzzToys:
                _sexToys.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case ToyboxTabs.SelectedTab.VibeLobbies:
                _vibeLobbies.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case ToyboxTabs.SelectedTab.Patterns:
                _patterns.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case ToyboxTabs.SelectedTab.Alarms:
                _alarms.DrawContents(drawRegions, frameH, _tabMenu);
                break;

            case ToyboxTabs.SelectedTab.Triggers:
                _triggers.DrawContents(drawRegions, frameH, _tabMenu);
                break;
        }
    }

    private void TutorialClickedAction()
    {
        switch (_tabMenu.TabSelection)
        {
            case ToyboxTabs.SelectedTab.BuzzToys:
            case ToyboxTabs.SelectedTab.VibeLobbies:
            case ToyboxTabs.SelectedTab.Patterns:
            case ToyboxTabs.SelectedTab.Alarms:
            case ToyboxTabs.SelectedTab.Triggers:
                // DO LATER. I hate everything! :D
                break;
        }
    }
}
