using Dalamud.Interface.Utility;
using GagSpeak.Gui.Components;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using static GagSpeak.Gui.Components.PuppeteerTabs;

namespace GagSpeak.Gui.Modules.Puppeteer;

public class PuppeteerUI : WindowMediatorSubscriberBase
{
    private readonly PuppetVictimGlobalPanel _victimGlobalPanel;
    private readonly PuppetVictimUniquePanel _victimUniquePanel;
    private readonly ControllerUniquePanel _controllerUniquePanel;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;

    private static PuppeteerTabs _tabMenu = new PuppeteerTabs();

    public PuppeteerUI(
        ILogger<PuppeteerUI> logger,
        GagspeakMediator mediator,
        PuppetVictimGlobalPanel globalPanel,
        PuppetVictimUniquePanel victimUniquePanel,
        ControllerUniquePanel controllerUniquePanel,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator, "Puppeteer UI")
    {
        _victimGlobalPanel = globalPanel;
        _victimUniquePanel = victimUniquePanel;
        _controllerUniquePanel = controllerUniquePanel;
        _cosmetics = cosmetics;
        _guides = guides;

        _tabMenu.AddDrawButton(CosmeticService.CoreTextures[CoreTexture.PuppetVictimGlobal], SelectedTab.VictimGlobal,
            "Configure how others can control you like a puppet, Globally!");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures[CoreTexture.PuppetVictimUnique], SelectedTab.VictimUnique,
            "Configure how others can control you, with unique configurations for every kinkster!");
        _tabMenu.AddDrawButton(CosmeticService.CoreTextures[CoreTexture.PuppetMaster], SelectedTab.ControllerUnique,
            "View what another Kinkster allows you control over, and any aliases they made.");

        AllowPinning = false;
        AllowClickthrough = false;
        TitleBarButtons = new() { };

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 490),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        RespectCloseHotkey = false;
    }

    private bool ThemePushed = false;
    private static float LeftLength() => 275f * ImGuiHelpers.GlobalScale;

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
        var res = CkHeader.FancyCurveFlipped(CkColor.FancyHeader.Uint(), LeftLength(), ImGui.GetFrameHeight(), ImGui.GetFrameHeight());

        switch (_tabMenu.TabSelection)
        {
            case SelectedTab.VictimGlobal:
                _victimGlobalPanel.DrawContents(res, ImGui.GetFrameHeight(), _tabMenu);
                break;

            case SelectedTab.VictimUnique:
                _victimUniquePanel.DrawContents(res, ImGui.GetFrameHeight(), _tabMenu);
                break;

            case SelectedTab.ControllerUnique:
                _controllerUniquePanel.DrawContents(res, ImGui.GetFrameHeight(), _tabMenu);
                break;
        }
    }
}
