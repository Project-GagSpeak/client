using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Gui.Components;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using CkCommons.Raii;
using CkCommons.Widgets;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.State.Managers;
using GagspeakAPI.Hub;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using CkCommons;
using CkCommons.Gui;
using System.Drawing;

namespace GagSpeak.Gui.Modules.Puppeteer;
public partial class ControllerUniquePanel : IDisposable
{
    private readonly ILogger<ControllerUniquePanel> _logger;
    private readonly MainHub _hub;
    private readonly PuppeteerHelper _helper;
    private readonly AliasItemDrawer _aliasDrawer;
    private readonly PuppeteerManager _manager;
    private readonly CosmeticService _cosmetics;

    private string _searchStr = string.Empty;
    private IReadOnlyList<AliasTrigger> _filteredItems = new List<AliasTrigger>();
    private static TagCollection PairTriggerTags = new();

    public ControllerUniquePanel(
        ILogger<ControllerUniquePanel> logger,
        MainHub hub,
        MainConfig config,
        PuppeteerHelper helper,
        AliasItemDrawer aliasDrawer,
        PuppeteerManager manager,
        FavoritesManager favorites,
        KinksterManager pairs,
        CosmeticService cosmetics)
    {
        _hub = hub;
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
            _filteredItems = _helper.PairAliasStorage?.Items ?? new List<AliasTrigger>();
        else
            _filteredItems = _helper.PairAliasStorage?.Items
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
        DrawPermissionsBox(region);
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

    private void DrawPermissionsBox(CkHeader.DrawRegion region)
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var triggerPhrasesH = CkStyle.GetFrameRowsHeight(3); // 3 lines of buttons.
        var permissionsH = CkStyle.GetFrameRowsHeight(4);
        var childH = triggerPhrasesH.AddWinPadY() + permissionsH + CkGui.GetSeparatorSpacedHeight(spacing.Y);
        var headerText = _helper.SelectedPair is { } p ? $"{p.GetNickAliasOrUid()}'s Settings for You" : "Select a Kinkster from the 2nd panel first!";

        using var c = CkRaii.ChildLabelTextFull(new Vector2(region.SizeX, childH), headerText, ImGui.GetFrameHeight(), DFlags.RoundCornersLeft, LabelFlags.AddPaddingToHeight);

        if (_helper.SelectedPair is not { } validPair)
            return;

        // extract the tabs by splitting the string by comma's
        using (CkRaii.FramedChildPaddedW("Triggers", c.InnerRegion.X, triggerPhrasesH, CkColor.FancyHeaderContrast.Uint(), CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll))
        {
            ImGui.TextUnformatted($"Listening to: {_helper.PairNamedStorage!.ExtractedListenerName}");
            if (ImGui.Button("Send Name"))
            {
                var name = PlayerData.NameWithWorld;
                _logger.LogInformation($"Sending name: {PlayerData.NameWithWorld}");
                try
                {
                    var task = Task.Run(
                        async () => await _hub.UserSendNameToKinkster(new(_helper.SelectedPair.UserData), name)
                    );
                    if (task.Result.ErrorCode is not GagSpeakApiEc.Success)
                    {
                        _logger.LogError($"Failed to perform UserSendNameToKinker, Task: {task.Result.ErrorCode}");
                    }
                    else
                    {
                        _logger.LogInformation($"Sending Username `{PlayerData.NameWithWorld}` to `{_helper.SelectedPair.UserData.UID}`");
                    }
                }
                catch (Bagagwa e)
                {
                    _logger.LogError($"{e}");
                }
            }
            PairTriggerTags.DrawTagsPreview("##OtherPairPhrases", validPair.PairPerms.TriggerPhrase);
        }

        CkGui.SeparatorSpacedColored(spacing.Y, c.InnerRegion.X, CkColor.FancyHeaderContrast.Uint());

        // Draw out the global puppeteer image.
        if (CosmeticService.CoreTextures.Cache[CoreTexture.PuppetMaster] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((c.InnerRegion.X / 2) - permissionsH) / 2));
            ImGui.Image(wrap.ImGuiHandle, new Vector2(permissionsH));
        }

        // Draw out the permission checkboxes
        ImGui.SameLine(c.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);

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
    }

    private void DrawExamplesBox(Vector2 region)
    {
        var size = new Vector2(region.X, ImGui.GetFrameHeightWithSpacing() * 3);
        using (var child = CkRaii.ChildLabelText(size, .7f, "Example Uses", ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersLeft))
        {
            ImGui.TextWrapped("Ex 1: /gag <trigger phrase> <message>");
            ImGui.TextWrapped("Ex 2: /gag <trigger phrase> <message> <image>");
        }
    }

}
