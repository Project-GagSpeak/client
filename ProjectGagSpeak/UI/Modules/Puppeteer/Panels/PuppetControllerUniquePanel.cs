using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Tutorial;
using GagSpeak.CkCommons.Gui.Components;
using ImGuiNET;
using Dalamud.Interface.Utility;

namespace GagSpeak.CkCommons.Gui.Modules.Puppeteer;
public partial class PuppetControllerUniquePanel
{
    private readonly ILogger<PuppetControllerUniquePanel> _logger;
    private readonly PuppeteerManager _manager;
    private readonly FavoritesManager _favorites;
    private readonly TutorialService _guides;
    public PuppetControllerUniquePanel(
        ILogger<PuppetControllerUniquePanel> logger,
        PuppeteerManager manager,
        FavoritesManager favorites,
        TutorialService guides)
    {
        _logger = logger;
        _manager = manager;
        _favorites = favorites;
        _guides = guides;
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, PuppeteerTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("PuppeteerTopLeft", drawRegions.TopLeft.Size))
            tabMenu.Draw(drawRegions.TopLeft.Size, ImGuiHelpers.ScaledVector2(4));

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        DrawPermissionsAndExample(drawRegions.BotLeft);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PuppeteerTopRight", drawRegions.TopRight.Size))
            DrawAliasSearch(drawRegions.TopRight.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("PuppeteerBotRight", drawRegions.BotRight.Size, false, WFlags.NoScrollbar))
            DrawAliasItems(drawRegions.BotRight.Size);
    }

    private string _searchStr = string.Empty;
    private void DrawAliasSearch(float width)
    {
        var change = FancySearchBar.Draw("##GlobalAliasSearch", width, "Search for an Alias", ref _searchStr, 200);
        if(change)
            _logger.LogInformation($"Searching for Alias: {_searchStr}");
    }

    private void DrawAliasItems(Vector2 region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12);
        using var _ = ImRaii.Child("GlobalAliasList", region, false, WFlags.AlwaysUseWindowPadding);
    }

    private void DrawPermissionsAndExample(CkHeader.DrawRegion contentRegion)
    {
        var wdl = ImGui.GetWindowDrawList();
        
    }

    private void DrawPermissionsBox()
    {

    }

    private void DrawExamplesBox()
    {

    }

}
