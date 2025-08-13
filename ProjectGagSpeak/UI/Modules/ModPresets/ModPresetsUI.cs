using CkCommons;
using CkCommons.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.Utils;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.Gui.Wardrobe;

public class ModPresetsUI : WindowMediatorSubscriberBase
{
    private readonly ModPresetFileSelector _selector;
    private readonly ModPresetsPanel _panel;
    private readonly TutorialService _guides;
    public ModPresetsUI(ILogger<ModPresetsUI> logger, GagspeakMediator mediator, TutorialService guides,
        ModPresetFileSelector selector, ModPresetsPanel panel) 
        : base(logger, mediator, "Mod Presets UI")
    {
        _selector = selector;
        _panel = panel;
        _guides = guides;

        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(ModListLength * 2, 300), ImGui.GetIO().DisplaySize);
        TitleBarButtons = new TitleBarButtonBuilder().AddTutorial(_guides, TutorialType.ModPresets).Build();
        RespectCloseHotkey = false;
    }

    private bool ThemePushed = false;
    private static float ModListLength = 275f * ImGuiHelpers.GlobalScale;

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
        var drawRegions = CkHeader.Flat(CkColor.FancyHeader.Uint(), headerInnder, ModListLength, splitterSize);

        // Create a child for each region, drawn to the size.
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("PresetTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(ModListLength);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("PresetBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(ModListLength);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PresetTR", drawRegions.TopRight.Size))
            _panel.DrawModuleTitle();

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("PresetBR", drawRegions.BotRight.Size))
            _panel.DrawPresetEditor(drawRegions.BotRight.Size);
    }
}
