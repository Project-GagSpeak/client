using CkCommons;
using CkCommons.Widgets;
using Dalamud.Interface.Utility;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.UiToybox;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
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

        _tabMenu = new ToyboxTabs();

        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(600, 530), ImGui.GetIO().DisplaySize);
        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.CloudDownloadAlt, "Migrate Old Toybox Data", () => Mediator.Publish(new UiToggleMessage(typeof(MigrationsUI))))
            .AddTutorial(_guides, TutorialFromTab())
            .Build();
        RespectCloseHotkey = false;
    }

    private ToyboxTabs _tabMenu { get; init; }
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
    private TutorialType TutorialFromTab()
        => _tabMenu.TabSelection switch
        {
            ToyboxTabs.SelectedTab.BuzzToys => TutorialType.Toys,
            ToyboxTabs.SelectedTab.VibeLobbies => TutorialType.VibeLobby,
            ToyboxTabs.SelectedTab.Patterns => TutorialType.Patterns,
            ToyboxTabs.SelectedTab.Alarms => TutorialType.Alarms,
            _ => TutorialType.Triggers
        };
}
