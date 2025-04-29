using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using OtterGui.Widgets;

namespace GagSpeak.CkCommons.Gui.Modules.Puppeteer;
public sealed partial class PuppetVictimGlobalPanel
{
    private readonly ILogger<PuppetVictimGlobalPanel> _logger;
    private readonly MainHub _hub;
    private readonly GlobalData _globals;
    private readonly PuppeteerManager _manager;
    private readonly FavoritesManager _favorites;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;

    private LazyCSVCache _triggerPhraseCSV { get; init; }
    private TagButtons _phraseButtonList = new();

    public PuppetVictimGlobalPanel(
        ILogger<PuppetVictimGlobalPanel> logger,
        MainHub hub,
        GlobalData globals,
        PuppeteerManager manager,
        FavoritesManager favorites,
        CosmeticService cosmetics,
        TutorialService guides)
    {
        _logger = logger;
        _hub = hub;
        _globals = globals;
        _manager = manager;
        _favorites = favorites;
        _cosmetics = cosmetics;
        _guides = guides;

        _triggerPhraseCSV = new LazyCSVCache(() => _globals.GlobalPerms?.TriggerPhrase ?? string.Empty);
        _triggerPhraseCSV.StringChanged += UpdateTriggerPhrase;
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, PuppeteerTabs tabMenu)
    {
        // Icon TabBar
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("PuppeteerTopLeft", drawRegions.TopLeft.Size))
            tabMenu.Draw(drawRegions.TopLeft.Size, ImGuiHelpers.ScaledVector2(4));

        // Permission Handling & Examples.
        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        DrawPermsAndExamples(drawRegions.BotLeft);

        // Search Bar
        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PuppeteerTopRight", drawRegions.TopRight.Size))
            DrawAliasSearch(drawRegions.TopRight.SizeX);

        // Alias List
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

    private void DrawPermsAndExamples(CkHeader.DrawRegion region)
    {
        DrawPermissionsBox(region);

        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y);
        ImGui.SetCursorScreenPos(region.Pos + verticalShift);
        DrawExamplesBox(region.Size - verticalShift);

        // Draw the grey strip thingy here.
    }

    private void DrawPermissionsBox(CkHeader.DrawRegion drawRegion)
    {
        using var _ = ImRaii.Group();

        DrawPermissionsBoxHeader(drawRegion);
        ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(0, ImGui.GetItemRectSize().Y));
        DrawPermissionsBoxBody(drawRegion);
    }

    private void DrawPermissionsBoxHeader(CkHeader.DrawRegion drawRegion)
    {
        var pos = ImGui.GetCursorPos();
        var splitH = CkRaii.GetSplitHeight();
        using (CkRaii.Group(CkColor.VibrantPink.Uint(), ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersTopLeft))
        {
            // Ensure our width.
            ImGui.Dummy(new Vector2(drawRegion.SizeX, ImGui.GetFrameHeight() + splitH));
            ImGui.SetCursorPos(pos);

            // Ensure the Spacing, and draw the header.
            ImGui.SameLine(ImGui.GetFrameHeight());
            ImUtf8.TextFrameAligned("Global Puppeteer Settings");
        }
        var max = ImGui.GetItemRectMax();
        var linePos = ImGui.GetItemRectMin() with { Y = max.Y - splitH / 2 };
        ImGui.GetWindowDrawList().AddLine(linePos, linePos with { X = max.X }, CkColor.SideButton.Uint(), CkRaii.GetSplitHeight());
    }

    private void DrawPermissionsBoxBody(CkHeader.DrawRegion drawRegion)
    {
        using var bodyChild = CkRaii.FakeChild(drawRegion.SizeX, CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersBottomLeft);

        ImGui.Spacing();

        // extract the tabs by splitting the string by comma's
        var triggerPhraseSize = new Vector2(bodyChild.InnerRegion.X, ImGui.GetFrameHeightWithSpacing() * 4);
        using (CkRaii.FramedChildPadded("Trigger Phrases", triggerPhraseSize, CkColor.FancyHeaderContrast.Uint()))
        {
            _triggerPhraseCSV.Sync();

            if (_phraseButtonList.Draw("", "", _triggerPhraseCSV, out var editedTag) is { } idx && idx != -1)
            {
                if (idx < _triggerPhraseCSV.Count)
                {
                    if (editedTag.Length == 0) 
                        _triggerPhraseCSV.Remove(idx);
                    else
                        _triggerPhraseCSV.Rename(idx, editedTag);
                }
                else
                    _triggerPhraseCSV.Add(editedTag);
            }
        }

        ImGui.Spacing();

        ImGui.Dummy(new Vector2(bodyChild.InnerRegion.X, ImGui.GetStyle().ItemSpacing.Y));
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.FancyHeaderContrast.Uint(), CkRaii.GetSplitHeight());

        ImGui.Spacing();

        var sideLength = ImGui.GetFrameHeight() * 4 + ImGui.GetStyle().ItemSpacing.Y * 3;
        if (_cosmetics.CoreTextures[CoreTexture.PuppetVictimGlobal] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((bodyChild.InnerRegion.X / 2) - sideLength) / 2));
            ImGui.Image(wrap.ImGuiHandle, new Vector2(sideLength));
        }

        ImGui.SameLine(bodyChild.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);
        using (ImRaii.Group())
            {
                var categoryFilter = (uint)(_globals.GlobalPerms?.PuppetPerms ?? PuppetPerms.None);
                foreach (var category in Enum.GetValues<PuppetPerms>().Skip(1))
                    ImGui.CheckboxFlags($"Allow {category}", ref categoryFilter, (uint)category);

                if (_globals.GlobalPerms is { } globals && globals.PuppetPerms != (PuppetPerms)categoryFilter)
                {
                    _hub.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData,
                        new KeyValuePair<string, object>(nameof(UserGlobalPermissions.PuppetPerms), (PuppetPerms)categoryFilter), UpdateDir.Own)).ConfigureAwait(false);
                }
            }

        ImGui.Spacing();
    }

    private void UpdateTriggerPhrase(string newPhrase)
    {
        _logger.LogInformation($"Updated own pair permission: TriggerPhrase to {newPhrase} from {_triggerPhraseCSV.CurrentCache}");
        _hub.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(nameof(UserGlobalPermissions.TriggerPhrase), newPhrase), UpdateDir.Own)).ConfigureAwait(false);
    }


    private void DrawExamplesBox(Vector2 region)
    {

    }

}
