using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Gui.Modules.Puppeteer;
using GagSpeak.Gui.Publications;
using GagSpeak.Gui.Toybox;
using GagSpeak.Gui.UiRemote;
using GagSpeak.Gui.Wardrobe;
using ImGuiNET;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Structs;
using OtterGui.Text;
using GagSpeak.Services.Textures;
using CkCommons.Gui;

namespace GagSpeak.Gui.MainWindow;

/// <summary> The homepage will provide the player with links to open up other windows in the plugin via components </summary>
public class HomepageTab
{
    private readonly GagspeakMediator _mediator;
    private readonly OnFrameworkService _framework;

    private int HoveredItemIndex = -1;
    private readonly List<(string Label, FontAwesomeIcon Icon, Type ToggleType)> Modules;

    public HomepageTab(GagspeakMediator mediator, OnFrameworkService framework)
    {
        _mediator = mediator;
        _framework = framework;

        // Define all module information in a single place
        Modules = new List<(string, FontAwesomeIcon, Type)>
        {
            ("Sex Toy Remote", FAI.WaveSquare, typeof(RemotePersonal)),
            ("Wardrobe", FAI.ToiletPortable, typeof(WardrobeUI)),
            ("Puppeteer", FAI.PersonHarassing, typeof(PuppeteerUI)),
            ("Toybox", FAI.BoxOpen, typeof(ToyboxUI)),
            ("Mod Presets", FAI.FileAlt, typeof(ModPresetsUI)),
            ("Trait Allowances", FAI.UserShield, typeof(TraitAllowanceUI)),
            ("Publications", FAI.CloudUploadAlt, typeof(PublicationsUI)),
            ("Achievements", FAI.Trophy, typeof(AchievementsUI)),
        };
    }

    public void DrawHomepageSection()
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.ChildRounding, 4f)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(6, 1));
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

        using var _ = ImRaii.Child("##Homepage", new Vector2(CkGui.GetWindowContentRegionWidth(), 0), false, WFlags.NoScrollbar);

        var sizeFont = CkGui.CalcFontTextSize("Achievements Module", UiFontService.GagspeakLabelFont);
        var selectableSize = new Vector2(CkGui.GetWindowContentRegionWidth(), sizeFont.Y + ImGui.GetStyle().WindowPadding.Y * 2);
        var itemGotHovered = false;

        for (var i = 0; i < Modules.Count; i++)
        {
            var module = Modules[i];
            var isHovered = HoveredItemIndex == i;

            if (HomepageSelectable(module.Label, module.Icon, selectableSize, isHovered))
            {
                _mediator.Publish(new UiToggleMessage(module.ToggleType));
                if (module.ToggleType == typeof(RemotePersonal))
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteOpened);
            }

            if (ImGui.IsItemHovered())
            {
                itemGotHovered = true;
                HoveredItemIndex = i;
            }
        }
        // if itemGotHovered is false, reset the index.
        if (!itemGotHovered)
            HoveredItemIndex = -1;
    }

    private bool HomepageSelectable(string label, FontAwesomeIcon icon, Vector2 region, bool hovered = false)
    {
        using var bgColor = hovered
            ? ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered))
            : ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        // store the screen position before drawing the child.
        var buttonPos = ImGui.GetCursorScreenPos();
        using (ImRaii.Child($"##HomepageItem{label}", region, true, WFlags.NoInputs | WFlags.NoScrollbar))
        {
            using var group = ImRaii.Group();
            var height = ImGui.GetContentRegionAvail().Y;

            CkGui.FontText(label, UiFontService.GagspeakLabelFont);
            ImGui.SetWindowFontScale(1.5f);

            var size = CkGui.IconSize(FAI.WaveSquare);
            var color = hovered ? ImGuiColors.ParsedGold : ImGuiColors.DalamudWhite;
            ImGui.SameLine(CkGui.GetWindowContentRegionWidth() - size.X - ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (height - size.Y) / 2);
            CkGui.IconText(icon, color);

            ImGui.SetWindowFontScale(1.0f);
        }
        // draw the button over the child.
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.InvisibleButton("##Button-" + label, region))
            return true;

        return false;
    }
}
