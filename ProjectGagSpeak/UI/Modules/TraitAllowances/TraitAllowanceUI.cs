using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.CkCommons.Gui.Components;
using ImGuiNET;
using OtterGuiInternal.Structs;
using GagSpeak.CkCommons.Widgets;

namespace GagSpeak.CkCommons.Gui.Wardrobe;

public class TraitAllowanceUI : WindowMediatorSubscriberBase
{
    private readonly TraitAllowanceSelector _selector;
    private readonly TraitAllowancePanel _panel;
    private readonly TutorialService _guides;
    public TraitAllowanceUI(
        ILogger<TraitAllowanceUI> logger,
        GagspeakMediator mediator,
        TutorialService guides,
        TraitAllowanceSelector selector,
        TraitAllowancePanel panel) : base(logger, mediator, "Trait Allowances")
    {
        _selector = selector;
        _panel = panel;
        _guides = guides;

        AllowPinning = false;
        AllowClickthrough = false;
        TitleBarButtons = new()
        {
            new TitleBarButton()
            {
                Icon = FAI.QuestionCircle,
                Click = (msg) => { },
                IconOffset = new (2, 1),
                ShowTooltip = () =>
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Start/Stop Trait Allowances Tutorial");
                    ImGui.EndTooltip();
                }
            }
        };

        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(550, 470),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        RespectCloseHotkey = false;
    }

    private bool ThemePushed = false;
    private static float LeftLength = 175f * ImGuiHelpers.GlobalScale;

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6));
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
        var winPadding = ImGui.GetStyle().WindowPadding;
        var headerInner = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        var splitterSize = ImGui.GetFrameHeight() / 4;

        // Draw a flat header.
        var drawRegions = CkHeader.Flat(CkColor.FancyHeader.Uint(), headerInner, LeftLength, splitterSize);

        // Create a child for each region, drawn to the size.
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("TraitsTL", drawRegions.TopLeft.Size))
            _selector.DrawSearch();

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("TraitsBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawResultList();

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("TraitsTR", drawRegions.TopRight.Size))
            _panel.DrawModuleTitle();

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("TraitsBR", drawRegions.BotRight.Size))
            _panel.DrawAllowancesEditor();
    }
}
