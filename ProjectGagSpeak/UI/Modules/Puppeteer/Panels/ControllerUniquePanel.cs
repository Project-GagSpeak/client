using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Modules.Puppeteer;
public partial class ControllerUniquePanel : IDisposable
{
    private readonly ILogger<ControllerUniquePanel> _logger;
    private readonly PuppeteerHelper _helper;
    private readonly AliasItemDrawer _aliasDrawer;
    private readonly PuppeteerManager _manager;
    private readonly CosmeticService _cosmetics;

    private string _searchStr = string.Empty;
    private IReadOnlyList<AliasTrigger> _filteredItems = new List<AliasTrigger>();
    private static TagCollection PairTriggerTags = new();

    public ControllerUniquePanel(
        ILogger<ControllerUniquePanel> logger,
        GagspeakConfigService config,
        PuppeteerHelper helper,
        AliasItemDrawer aliasDrawer,
        PuppeteerManager manager,
        FavoritesManager favorites,
        PairManager pairs,
        CosmeticService cosmetics)
    {
        _logger = logger;
        _helper = helper;
        _manager = manager;
        _aliasDrawer = aliasDrawer;
        _cosmetics = cosmetics;

        UpdateFilteredItems();
        _helper.OnPairUpdated += UpdateFilteredItems;
    }

    public void Dispose()
    {
        _helper.OnPairUpdated -= UpdateFilteredItems;
    }

    private void UpdateFilteredItems()
    {
        if (_searchStr.IsNullOrEmpty())
            _filteredItems = _helper.PairAliasStorage ?? new AliasStorage();
        else
            _filteredItems = _helper.PairAliasStorage?
                .Where(x => x.Label.Contains(_searchStr, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<AliasTrigger>();
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

    private void DrawAliasSearch(float width)
    {
        if (FancySearchBar.Draw("##PairControllerSearch", width, "Search for an Alias", ref _searchStr, 200))
        {
            _logger.LogInformation($"Searching for Alias: {_searchStr}");
            UpdateFilteredItems();
        }
    }

    private void DrawAliasItems(Vector2 region)
    {
        // Push styles for our inner child items.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(4))
            .Push(ImGuiStyleVar.FramePadding, new Vector2(2));
        using var _ = CkRaii.Child("##PairControlAliasList", region);
        // Place these into a list, so that when we finish changing an item, the list does not throw an error.

        if (_helper.SelectedPair is not { } validPair)
            return;

        foreach (var aliasItem in _filteredItems.ToList())
            _aliasDrawer.DrawAliasTrigger(aliasItem, validPair.LastIpcData, false);
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
        var spacing = ImGui.GetStyle().ItemSpacing;
        using (CkRaii.Child("##PermBoxHeader", new Vector2(drawRegion.SizeX, ImGui.GetFrameHeightWithSpacing()), 
            CkColor.VibrantPink.Uint(), ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersTopLeft))
        {
            // Ensure the Spacing, and draw the header.
            ImGui.SameLine(ImGui.GetFrameHeight());
            var headerText = _helper.SelectedPair is { } pair 
                ? $"{pair.GetNickAliasOrUid()}'s Settings for You" 
                : "Select a Kinkster from the 2nd panel first!";
            ImUtf8.TextFrameAligned(headerText);
        }
        var max = ImGui.GetItemRectMax();
        var linePos = ImGui.GetItemRectMin() with { Y = max.Y - spacing.Y / 2 };
        ImGui.GetWindowDrawList().AddLine(linePos, linePos with { X = max.X }, CkColor.SideButton.Uint(), spacing.Y);
    }

    private void DrawPermissionsBoxBody(CkHeader.DrawRegion drawRegion)
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var triggerPhrasesH = ImGui.GetFrameHeightWithSpacing() * 3; // 3 lines of buttons.
        var spacingsH = spacing.Y * 2;
        var permissionsH = ImGui.GetFrameHeight() * 4 + spacing.Y * 3;
        var childH = triggerPhrasesH.AddWinPadY() + spacingsH + permissionsH + CkGui.GetSeparatorHeight(spacing.Y);

        // Create the inner child box.
        using var child = CkRaii.ChildPaddedW("PermBoxBody", drawRegion.SizeX, childH, CkColor.FancyHeader.Uint(),
            ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersBottomLeft);

        if(_helper.SelectedPair is not { } validPair)
        {
            ImGuiUtil.Center("No Kinkster Selected!");
            return;
        }

        var cursorPos = ImGui.GetCursorPosY();
        ImGui.Spacing();

        // extract the tabs by splitting the string by comma's
        using (CkRaii.FramedChildPaddedW("Triggers", child.InnerRegion.X, triggerPhrasesH, CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll))
            PairTriggerTags.DrawTagsPreview("##OtherPairPhrases", validPair.PairPerms.TriggerPhrase);

        CkGui.Separator(spacing.Y, child.InnerRegion.X);

        // Draw out the global puppeteer image.
        if (_cosmetics.CoreTextures[CoreTexture.PuppetMaster] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((child.InnerRegion.X / 2) - permissionsH) / 2));
            ImGui.Image(wrap.ImGuiHandle, new Vector2(permissionsH));
        }

        // Draw out the permission checkboxes
        ImGui.SameLine(child.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);

        using (ImRaii.Group())
        {
            var categoryFilter = (uint)(validPair.PairPerms.PuppetPerms);
            foreach (var category in Enum.GetValues<PuppetPerms>().Skip(1))
            {
                using (ImRaii.Disabled())
                    ImGui.CheckboxFlags($"Allows {category}", ref categoryFilter, (uint)category);
                CkGui.AttachToolTip(category switch
                {
                    PuppetPerms.All => $"{validPair.GetNickAliasOrUid() ?? "This Kinkster"} has granted you full control.--SEP--(Take Care with this)",
                    PuppetPerms.Alias => $"{validPair.GetNickAliasOrUid() ?? "This Kinkster"} allows you to execute their alias triggers.",
                    PuppetPerms.Emotes => $"{validPair.GetNickAliasOrUid() ?? "This Kinkster"} allows you to make them perform emotes.",
                    PuppetPerms.Sit => $"{validPair.GetNickAliasOrUid() ?? "This Kinkster"} allows you to make them sit or cycle poses.",
                    _ => $"NO PERMS ALLOWED."
                });
            }
        }

        ImGui.Spacing();
    }

    private void DrawExamplesBox(Vector2 region)
    {
        var size = new Vector2(region.X, ImGui.GetFrameHeightWithSpacing() * 3);
        var labelSize = new Vector2(region.X * .7f, ImGui.GetTextLineHeightWithSpacing());

        using (var child = CkRaii.LabelChildText(size, labelSize, "Example Uses", ImGui.GetFrameHeight(), ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersLeft))
        {
            ImGui.TextWrapped("Ex 1: /gag <trigger phrase> <message>");
            ImGui.TextWrapped("Ex 2: /gag <trigger phrase> <message> <image>");
        }
    }

}
