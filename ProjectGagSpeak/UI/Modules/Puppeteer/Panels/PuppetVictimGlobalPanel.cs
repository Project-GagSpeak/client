using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using ImGuiNET;

namespace GagSpeak.Gui.Modules.Puppeteer;
public sealed partial class PuppetVictimGlobalPanel
{
    private readonly ILogger<PuppetVictimGlobalPanel> _logger;
    private readonly MainHub _hub;
    private readonly AliasItemDrawer _aliasDrawer;
    private readonly PuppeteerManager _manager;

    private string _searchStr = string.Empty;
    private IReadOnlyList<AliasTrigger> _filteredItems = new List<AliasTrigger>();
    private IEnumerable<InvokableActionType> _actionTypes = Enum.GetValues<InvokableActionType>();
    private InvokableActionType _selectedType = InvokableActionType.Gag;

    private static TagCollection GlobalTriggerTags = new();

    public PuppetVictimGlobalPanel(ILogger<PuppetVictimGlobalPanel> logger, MainHub hub,
        AliasItemDrawer aliasDrawer, PuppeteerManager manager, FavoritesManager favorites)
    {
        _logger = logger;
        _hub = hub;
        _manager = manager;
        _aliasDrawer = aliasDrawer;

        _filteredItems = _manager.GlobalAliasStorage.Items;
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
        if(FancySearchBar.Draw("##GlobalSearch", width, "Search for an Alias", ref _searchStr, 200, ImGui.GetFrameHeight(), AddTriggerButton))
        {
            _logger.LogInformation($"Searching for Alias: {_searchStr}");
            _filteredItems = _searchStr.IsNullOrEmpty()
                ? _manager.GlobalAliasStorage.Items
                : _manager.GlobalAliasStorage.Items
                .Where(x => x.Label.Contains(_searchStr, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        void AddTriggerButton()
        {
            if (CkGui.IconButton(FAI.Plus, inPopup: true))
            {
                _manager.CreateNew();
                _logger.LogInformation("Added a new Alias Trigger.");
            }
        }
    }

    public void DrawAliasItems(Vector2 region)
    {
        // Push styles for our inner child items.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(4))
            .Push(ImGuiStyleVar.FramePadding, new Vector2(2));
        // Place these into a list, so that when we finish changing an item, the list does not throw an error.

        _aliasDrawer.DrawAchievementList(AchievementModuleKind.Generic, region);

        //foreach (var aliasItem in _filteredItems.ToList())
        //{
        //    if (aliasItem.Identifier == _manager.ItemInEditor?.Identifier)
        //        _aliasDrawer.DrawAliasTriggerEditor(_actionTypes, ref _selectedType);
        //    else
        //        _aliasDrawer.DrawAliasTrigger(aliasItem, MoodleCache.IpcData);
        //}
    }
    private void DrawPermsAndExamples(CkHeader.DrawRegion region)
    {
        DrawPermissions(region);
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

    private void DrawPermissions(CkHeader.DrawRegion region)
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var triggerPhrasesH = CkStyle.GetFrameRowsHeight(3); // 3 lines of buttons.
        var permissionsH = CkStyle.GetFrameRowsHeight(4);
        var childH = triggerPhrasesH.AddWinPadY() + permissionsH + CkGui.GetSeparatorSpacedHeight(spacing.Y);
        using var c = CkRaii.ChildLabelTextFull(new Vector2(region.SizeX, childH), "Global Puppeteer Settings", ImGui.GetFrameHeight(), DFlags.RoundCornersLeft, LabelFlags.AddPaddingToHeight);

        // extract the tabs by splitting the string by comma's
        using (CkRaii.FramedChildPaddedW("Triggers", c.InnerRegion.X, triggerPhrasesH, CkColor.FancyHeaderContrast.Uint(), DFlags.RoundCornersAll))
        {
            var globalPhrase = OwnGlobals.Perms?.TriggerPhrase ?? string.Empty;
            if (GlobalTriggerTags.DrawTagsEditor("##GlobalPhrases", globalPhrase, out var updatedString) && OwnGlobals.Perms is { } globals)
            {
                _logger.LogTrace("The Tag Editor had an update!");
                UiService.SetUITask(async () =>
                {
                    var res = await _hub.UserChangeOwnGlobalPerm(new(MainHub.PlayerUserData, new KeyValuePair<string, object>(
                        nameof(GlobalPerms.TriggerPhrase), updatedString), UpdateDir.Own, MainHub.PlayerUserData)).ConfigureAwait(false);
                    if (res.ErrorCode is not GagSpeakApiEc.Success)
                        _logger.LogError($"Failed to update global trigger phrase: {res.ErrorCode}");
                });
            }
        }

        CkGui.SeparatorSpacedColored(spacing.Y, c.InnerRegion.X, CkColor.FancyHeaderContrast.Uint());

        // Draw out the global puppeteer image.
        if (CosmeticService.CoreTextures.Cache[CoreTexture.PuppetVictimGlobal] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((c.InnerRegion.X / 2) - permissionsH) / 2));
            ImGui.Image(wrap.ImGuiHandle, new Vector2(permissionsH));
        }

        // Draw out the permission checkboxes
        ImGui.SameLine(c.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);
        using (ImRaii.Group())
        {
            var categoryFilter = (uint)(OwnGlobals.Perms?.PuppetPerms ?? PuppetPerms.None);
            foreach (var category in Enum.GetValues<PuppetPerms>().Skip(1))
                ImGui.CheckboxFlags($"Allow {category}", ref categoryFilter, (uint)category);

            if (OwnGlobals.Perms is { } globals && globals.PuppetPerms != (PuppetPerms)categoryFilter)
                UiService.SetUITask(async () =>
                {
                    var res = await _hub.UserChangeOwnGlobalPerm(new(MainHub.PlayerUserData, new KeyValuePair<string, object>(
                        nameof(GlobalPerms.PuppetPerms), (PuppetPerms)categoryFilter), UpdateDir.Own, MainHub.PlayerUserData)).ConfigureAwait(false);
                    if (res.ErrorCode is not GagSpeakApiEc.Success)
                        _logger.LogError($"Failed to update global puppet perms: {res.ErrorCode}");
                });
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
