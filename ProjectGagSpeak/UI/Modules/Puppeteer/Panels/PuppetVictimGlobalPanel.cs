using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
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
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using ImGuiNET;
using OtterGui.Text;
using OtterGui.Widgets;

namespace GagSpeak.CkCommons.Gui.Modules.Puppeteer;
public sealed partial class PuppetVictimGlobalPanel
{
    private readonly ILogger<PuppetVictimGlobalPanel> _logger;
    private readonly MainHub _hub;
    private readonly GlobalData _globals;
    private readonly AliasItemDrawer _aliasDrawer;
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
        AliasItemDrawer aliasDrawer,
        PuppeteerManager manager,
        FavoritesManager favorites,
        CosmeticService cosmetics,
        TutorialService guides)
    {
        _logger = logger;
        _hub = hub;
        _globals = globals;
        _manager = manager;
        _aliasDrawer = aliasDrawer;
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
    private IReadOnlyList<AliasTrigger> FilteredItems = new List<AliasTrigger>();

    private void DrawAliasSearch(float width)
    {
        var change = FancySearchBar.Draw("##GlobalAliasSearch", width, "Search for an Alias", ref _searchStr, 200);
        if(change)
        {
            _logger.LogInformation($"Searching for Alias: {_searchStr}");
            FilteredItems = _manager.GlobalAliasStorage
                .Where(x => x.Label.Contains(_searchStr, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void DrawAliasItems(Vector2 region)
    {
        using var _ = CkRaii.ChildPadded("GlobalAliasList", region);

        // Push styles for our inner child items.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12)
            .Push(ImGuiStyleVar.ChildRounding, 5f)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(8, 5))
            .Push(ImGuiStyleVar.FramePadding, new Vector2(4, 2))
            .Push(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.FrameBorderSize, 1f);
        using var color = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink)
            .Push(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));


        // Place these into a list, so that when we finish changing an item, the list does not throw an error.
        foreach (var aliasItem in _manager.GlobalAliasStorage.ToList())
        {
            if (aliasItem.Identifier == _manager.ItemInEditor?.Identifier)
                DrawAliasTriggerEditor();
            else
                DrawAliasTrigger(aliasItem);
        }
    }

    private void DrawPermsAndExamples(CkHeader.DrawRegion region)
    {
        using (ImRaii.Group())
        {
            DrawPermissionsBoxHeader(region);
            ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(0, ImGui.GetItemRectSize().Y));
            DrawPermissionsBoxBody(region);
        }
        var lineTopLeft = ImGui.GetItemRectMin() with { X = ImGui.GetItemRectMax().X };
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));

        // Shift down and draw the lower.
        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y * 3);
        ImGui.SetCursorScreenPos(region.Pos + verticalShift);
        DrawExamplesBox(region.Size - verticalShift);
        var botLineTopLeft = ImGui.GetItemRectMin() with { X = ImGui.GetItemRectMax().X };
        var botLineBotRight = botLineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(botLineTopLeft, botLineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawPermissionsBoxHeader(CkHeader.DrawRegion drawRegion)
    {
        var pos = ImGui.GetCursorPos();
        var splitH = ImGui.GetStyle().ItemSpacing.Y;
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
        ImGui.GetWindowDrawList().AddLine(linePos, linePos with { X = max.X }, CkColor.SideButton.Uint(), splitH);
    }

    private void DrawPermissionsBoxBody(CkHeader.DrawRegion drawRegion)
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var triggerPhrasesH = ImGui.GetFrameHeightWithSpacing() * 3; // 3 lines of buttons.
        var spacingsH = spacing.Y * 2;
        var permissionsH = ImGui.GetFrameHeight() * 4 + spacing.Y * 3;
        var childH = triggerPhrasesH.AddWinPadY() + spacingsH + permissionsH + CkGui.GetSeparatorHeight(spacing.Y);

        // Create the inner child box.
        using var child = CkRaii.ChildPaddedW("PermBoxBody", drawRegion.SizeX, childH, CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersBottomLeft);

        var cursorPos = ImGui.GetCursorPosY();
        ImGui.Spacing();

        // extract the tabs by splitting the string by comma's
        using (CkRaii.FramedChildPaddedW("Triggers", child.InnerRegion.X, triggerPhrasesH, CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll))
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

        CkGui.Separator(spacing.Y, child.InnerRegion.X);
        
        // Draw out the global puppeteer image.
        if (_cosmetics.CoreTextures[CoreTexture.PuppetVictimGlobal] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((child.InnerRegion.X / 2) - permissionsH) / 2));
            ImGui.Image(wrap.ImGuiHandle, new Vector2(permissionsH));
        }

        // Draw out the permission checkboxes
        ImGui.SameLine(child.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);
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
        var examplesRegion = new Vector2(region.X, ImGui.GetFrameHeightWithSpacing() * 3);
        var colorTheme = new CkRaii.HeaderChildColors(CkColor.VibrantPink.Uint(), CkColor.ElementSplit.Uint(), CkColor.FancyHeader.Uint());
        var labelWidth = region.X * .7f;

        using (var child = CkRaii.LabelHeaderChild(examplesRegion, "Example Uses", labelWidth, ImGui.GetFrameHeight(), ImGui.GetFrameHeight(),
            colorTheme, ImDrawFlags.RoundCornersLeft, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersBottomRight))
        {
            ImGui.TextWrapped("Ex 1: /gag <trigger phrase> <message>");
            ImGui.TextWrapped("Ex 2: /gag <trigger phrase> <message> <image>");
        }
    }

}
