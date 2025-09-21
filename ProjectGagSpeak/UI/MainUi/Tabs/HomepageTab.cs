using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Gui.Modules.Puppeteer;
using GagSpeak.Gui.Publications;
using GagSpeak.Gui.Remote;
using GagSpeak.Gui.Toybox;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.Interop;
using GagSpeak.MufflerCore.Handler;
using GagSpeak.PlayerControl;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;

namespace GagSpeak.Gui.MainWindow;

/// <summary> The homepage will provide the player with links to open up other windows in the plugin via components </summary>
public class HomepageTab
{
    private readonly ILogger<HomepageTab> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly IpcCallerPenumbra _ipc;
    private readonly HcTaskManager _temp;
    private readonly PlayerControlCache _cache;
    private readonly MovementDetours _detours;

    private int HoveredItemIndex = -1;
    private readonly List<(string Label, FontAwesomeIcon Icon, Action OnClick)> Modules;

    public HomepageTab(ILogger<HomepageTab> logger, GagspeakMediator mediator, IpcCallerPenumbra ipc, HcTaskManager temp, 
        PlayerControlCache cache, MovementDetours detours)
    {
        _logger = logger;
        _mediator = mediator;
        _ipc = ipc;
        _temp = temp;
        _cache = cache;
        _detours = detours;

        // Define all module information in a single place
        Modules = new List<(string, FontAwesomeIcon, Action)>
        {
            ("Sex Toy Remote", FAI.WaveSquare, () => _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI)))),
            ("Wardrobe", FAI.ToiletPortable, () => _mediator.Publish(new UiToggleMessage(typeof(WardrobeUI)))),
            ("Cursed Loot", FAI.BoxOpen, () => _mediator.Publish(new UiToggleMessage(typeof(CursedLootUI)))),
            ("Puppeteer", FAI.PersonHarassing, () => _mediator.Publish(new UiToggleMessage(typeof(PuppeteerUI)))),
            ("Toybox", FAI.BoxOpen, () => _mediator.Publish(new UiToggleMessage(typeof(ToyboxUI)))),
            ("Mod Presets", FAI.FileAlt, () => _mediator.Publish(new UiToggleMessage(typeof(ModPresetsUI)))),
            ("Trait Allowances", FAI.UserShield, () => _mediator.Publish(new UiToggleMessage(typeof(TraitAllowanceUI)))),
            ("Publications", FAI.CloudUploadAlt, () => _mediator.Publish(new UiToggleMessage(typeof(PublicationsUI)))),
            ("Achievements", FAI.Trophy, () => _mediator.Publish(new UiToggleMessage(typeof(AchievementsUI))))
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
                module.OnClick?.Invoke();

            if (ImGui.IsItemHovered())
            {
                itemGotHovered = true;
                HoveredItemIndex = i;
            }
        }
        // if itemGotHovered is false, reset the index.
        if (!itemGotHovered)
            HoveredItemIndex = -1;


        // Logger for Penumbra debugging.
        if (ImGui.Button("PenumbraDebugger"))
            PrintPenumbraDebugger();
        // Use for further debugging if nessisary.
        //unsafe
        //{
        //    if (ImGui.Button("Home Locator"))
        //    {
        //        var node = HcStayHousingEntrance.GetNearestHousingEntrance(out var distance);
        //        Svc.Logger.Warning($"Distance to node: {distance}");
        //        if (node is not null)
        //            Svc.Logger.Warning($"Node DataID: {node.DataId} Name: {node.Name}");
        //    }


        //    if (HcTaskUtils.IsOutside())
        //        return;

        //    if (HousingManager.Instance()->GetCurrentHousingTerritoryType() is HousingTerritoryType.None)
        //        return;

        //    var hausInfo = HousingManager.Instance()->IndoorTerritory->HouseId;
        //    ImGui.Text($"HouseId: {hausInfo.Id}");
        //    ImGui.Text($"Territory: {hausInfo.TerritoryTypeId}");
        //    ImGui.Text($"WorldId: {hausInfo.WorldId}");
        //    ImGui.Text($"WardIdx: {hausInfo.WardIndex}");
        //    ImGui.Text($"PlotIdx: {hausInfo.PlotIndex}");
        //    ImGui.Text($"RoomNumber: {hausInfo.RoomNumber}");
        //    ImGui.Text($"IsApartment: {hausInfo.IsApartment}");
        //    if (hausInfo.IsApartment)
        //    {
        //        ImGui.Text($"ApartmentDivision: {hausInfo.ApartmentDivision}");
        //    }
        //    ImGui.Text($"IsWorkshop: {hausInfo.IsWorkshop}");

        //}
    }

    private async void PrintPenumbraDebugger()
    {
        var paths = await _ipc.GetKinksterModData(0);
        if (paths is null)
            return;

        _logger.LogInformation("---- Penumbra Debugger ----");
        foreach (var (local, replacements) in paths)
            _logger.LogInformation($"Game: [{local}] => Replacements: [{string.Join(',', replacements)}]");
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
            return true && !UiService.DisableUI;

        return false;
    }
}
