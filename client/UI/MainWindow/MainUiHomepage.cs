using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.UiGagSetup;
using GagSpeak.UI.UiOrders;
using GagSpeak.UI.UiPuppeteer;
using GagSpeak.UI.UiRemote;
using GagSpeak.UI.UiToybox;
using GagSpeak.UI.UiWardrobe;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.MainWindow;

/// <summary> 
/// Partial class responsible for drawing the homepage element of the main UI.
/// The homepage will provide the player with links to open up other windows in the plugin via components.
/// </summary>
public class MainUiHomepage
{
    private readonly GagspeakMediator _mediator;
    private readonly UiSharedService _uiShared;

    private int HoveredItemIndex = -1;
    private readonly List<(string Label, FontAwesomeIcon Icon, Type ToggleType)> Modules;

    public MainUiHomepage(GagspeakMediator mediator, UiSharedService uiSharedService)
    {
        _mediator = mediator;
        _uiShared = uiSharedService;

        // Define all module information in a single place
        Modules = new List<(string, FontAwesomeIcon, Type)>
        {
            ("Sextoy Remote", FontAwesomeIcon.WaveSquare, typeof(RemotePersonal)),
            ("Orders Module", FontAwesomeIcon.ClipboardList, typeof(OrdersUI)),
            ("Gags Module", FontAwesomeIcon.CommentSlash, typeof(GagSetupUI)),
            ("Wardrobe Module", FontAwesomeIcon.ToiletPortable, typeof(WardrobeUI)),
            ("Puppeteer Module", FontAwesomeIcon.PersonHarassing, typeof(PuppeteerUI)),
            ("Toybox Module", FontAwesomeIcon.BoxOpen, typeof(ToyboxUI)),
            ("Achievements Module", FontAwesomeIcon.Trophy, typeof(AchievementsUI)),
        };
    }

    public void DrawHomepageSection()
    {
        using var borderSize = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 4f);
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(6, 1));
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var homepageChild = ImRaii.Child("##Homepage", new Vector2(UiSharedService.GetWindowContentRegionWidth(), 0), false, ImGuiWindowFlags.NoScrollbar);

        var sizeFont = _uiShared.CalcFontTextSize("Achievements Module", _uiShared.GagspeakLabelFont);
        var selectableSize = new Vector2(UiSharedService.GetWindowContentRegionWidth(), sizeFont.Y + ImGui.GetStyle().WindowPadding.Y * 2);
        bool itemGotHovered = false;

        for (int i = 0; i < Modules.Count; i++)
        {
            var module = Modules[i];
            bool isHovered = HoveredItemIndex == i;

            if (HomepageSelectable(module.Label, module.Icon, selectableSize, isHovered))
            {
                _mediator.Publish(new UiToggleMessage(module.ToggleType));
                if (module.ToggleType == typeof(RemotePersonal))
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.RemoteOpened);
            }

            if (ImGui.IsItemHovered())
            {
                itemGotHovered = true;
                HoveredItemIndex = i;
            }
        }

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
        using (ImRaii.Child($"##HomepageItem{label}", region, true, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
        {
            using var group = ImRaii.Group();
            var height = ImGui.GetContentRegionAvail().Y;

            _uiShared.GagspeakBigText(label);
            ImGui.SetWindowFontScale(1.5f);

            var size = _uiShared.GetIconData(FontAwesomeIcon.WaveSquare);
            var color = hovered ? ImGuiColors.ParsedGold : ImGuiColors.DalamudWhite;
            ImGui.SameLine(UiSharedService.GetWindowContentRegionWidth() - size.X - ImGui.GetStyle().ItemInnerSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (height - size.Y) / 2);
            _uiShared.IconText(icon, color);

            ImGui.SetWindowFontScale(1.0f);
        }
        // draw the button over the child.
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.InvisibleButton("##Button-" + label, region))
            return true;

        return false;
    }
}
