using CkCommons;
using CkCommons.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using ImGuiNET;

namespace GagSpeak.Gui.Wardrobe;

public class ModPresetsUI : WindowMediatorSubscriberBase
{
    private readonly ModPresetSelector _selector;
    private readonly ModPresetsPanel _panel;
    private readonly TutorialService _guides;
    public ModPresetsUI(
        ILogger<WardrobeUI> logger,
        GagspeakMediator mediator,
        TutorialService guides,
        ModPresetSelector selector,
        ModPresetsPanel panel) : base(logger, mediator, "Mod Presets UI")
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
                    ImGui.Text("Start/Stop Mod Presets Tutorial");
                    ImGui.EndTooltip();
                }
            }
        };

        // define initial size of window and to not respect the close hotkey.
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(LeftLength + ModPresetsPanel.PresetSelectorWidth + 225f, 300),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };
        RespectCloseHotkey = false;
    }

    private bool ThemePushed = false;
    private static float LeftLength = 225f * ImGuiHelpers.GlobalScale;

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

    protected override void DrawInternal()
    {
        var winPadding = ImGui.GetStyle().WindowPadding;
        var headerInnder = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
        var splitterSize = ImGui.GetFrameHeight() / 4;

        // Draw a flat header.
        var drawRegions = CkHeader.Flat(CkColor.FancyHeader.Uint(), headerInnder, LeftLength, splitterSize);

        // Create a child for each region, drawn to the size.
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("PresetTL", drawRegions.TopLeft.Size))
            _selector.DrawSearch();

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("PresetBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawModSelector();

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PresetTR", drawRegions.TopRight.Size))
            _panel.DrawModuleTitle();

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("PresetBR", drawRegions.BotRight.Size))
            _panel.DrawPresetEditor();
    }
}
