using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Gui.Components;
using CkCommons.Raii;
using CkCommons.Widgets;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using ImGuiNET;
using OtterGui.Text;
using CkCommons.Gui;
using CkCommons;

namespace GagSpeak.Gui.Modules.Puppeteer;

public sealed partial class PuppetVictimUniquePanel : IDisposable
{
    private readonly ILogger<PuppetVictimUniquePanel> _logger;
    private readonly MainHub _hub;
    private readonly PuppeteerHelper _helper;
    private readonly AliasItemDrawer _aliasDrawer;
    private readonly PuppeteerManager _manager;
    private readonly CosmeticService _cosmetics;

    private string _searchStr = string.Empty;
    private IReadOnlyList<AliasTrigger> _filteredItems = new List<AliasTrigger>();
    private IEnumerable<InvokableActionType> _actionTypes = Enum.GetValues<InvokableActionType>();
    private InvokableActionType _selectedType = InvokableActionType.Gag;
    private static TagCollection PairTriggerTags = new();

    public PuppetVictimUniquePanel(
        ILogger<PuppetVictimUniquePanel> logger,
        MainConfig config,
        MainHub hub,
        PuppeteerHelper helper,
        AliasItemDrawer aliasDrawer,
        PuppeteerManager manager,
        FavoritesManager favorites,
        KinksterManager pairs,
        CosmeticService cosmetics)
    {
        _logger = logger;
        _hub = hub;
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
            _filteredItems = _helper.Storage?.Items ?? new List<AliasTrigger>();
        else
            _filteredItems = _helper.Storage?.Items
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
        if (FancySearchBar.Draw("##PairVictimSearch", width, "Search for an Alias", ref _searchStr, 200, ImGui.GetFrameHeight(), AddTriggerButton))
        {
            _logger.LogInformation($"Searching for Alias: {_searchStr}");
            UpdateFilteredItems();
        }

        void AddTriggerButton()
        {
            if (CkGui.IconButton(FAI.Plus, inPopup: true) && _helper.SelectedPair is { } pair)
            {
                _manager.CreateNew(pair.UserData.UID);
                UpdateFilteredItems();
                _logger.LogInformation("Added a new Alias Trigger.");
            }
        }
    }

    private void DrawAliasItems(Vector2 region)
    {
        // Push styles for our inner child items.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(4))
            .Push(ImGuiStyleVar.FramePadding, new Vector2(2));
        using var _ = CkRaii.Child("##PairVictimAliasList", region);
        // Place these into a list, so that when we finish changing an item, the list does not throw an error.
        foreach (var aliasItem in _filteredItems.ToList())
        {
            if (aliasItem.Identifier == _manager.ItemInEditor?.Identifier)
                _aliasDrawer.DrawAliasTriggerEditor(_actionTypes, ref _selectedType);
            else
                _aliasDrawer.DrawAliasTrigger(aliasItem, MoodleCache.IpcData);
        }
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

    private void DrawPermissionsBox(CkHeader.DrawRegion drawRegion)
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var triggerPhrasesH = CkStyle.GetFrameRowsHeight(3); // 3 lines of buttons.
        var permissionsH = CkStyle.GetFrameRowsHeight(4);
        var childH = triggerPhrasesH.AddWinPadY() + permissionsH + CkGui.GetSeparatorSpacedHeight(spacing.Y);
        using var c = CkRaii.ChildLabelCustomFull("PupPairSelector", new Vector2(drawRegion.SizeX, childH), ImGui.GetFrameHeight(), PairSelector, DFlags.RoundCornersLeft, LabelFlags.AddPaddingToHeight);

        // extract the tabs by splitting the string by comma's
        using (ImRaii.Disabled(_helper.SelectedPair is null))
            DrawTriggerPhraseBox(c.InnerRegion.X, triggerPhrasesH);

        CkGui.SeparatorSpacedColored(spacing.Y, c.InnerRegion.X, CkColor.FancyHeaderContrast.Uint());

        // Draw out the global puppeteer image.
        if (CosmeticService.CoreTextures.Cache[CoreTexture.PuppetVictimUnique] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((c.InnerRegion.X / 2) - permissionsH) / 2));
            ImGui.Image(wrap.ImGuiHandle, new Vector2(permissionsH));
        }

        // Draw out the permission checkboxes
        ImGui.SameLine(c.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);
        
        DrawPuppetPermsGroup(_helper.SelectedPair?.OwnPerms.PuppetPerms ?? PuppetPerms.None);

        void PairSelector()
        {
            using (CkRaii.Child("##PairSelector", new Vector2(drawRegion.SizeX, ImGui.GetFrameHeight())))
            {
                ImGui.SameLine(ImGui.GetFrameHeight());
                ImUtf8.TextFrameAligned("Your Settings For");
                ImGui.SameLine();
                _helper.DrawPairSelector("VictimUnique", ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X);
            }
        }
    }

    private void DrawTriggerPhraseBox(float paddedWidth, float height)
    {
        using (CkRaii.FramedChildPaddedW("Triggers", paddedWidth, height, CkColor.FancyHeaderContrast.Uint(), CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll))
        {
            var triggerPhrase = _helper.SelectedPair?.OwnPerms.TriggerPhrase ?? string.Empty;
            if (PairTriggerTags.DrawTagsEditor("##OwnPairPhrases", triggerPhrase, out string updatedString) && _helper.SelectedPair is { } validPair)
            {
                _logger.LogTrace("The Tag Editor had an update!");
                PermissionHelper.ChangeOwnUnique(_hub, validPair.UserData, validPair.OwnPerms, nameof(PairPerms.TriggerPhrase), updatedString).ConfigureAwait(false);
            }
        }
    }

    private void DrawPuppetPermsGroup(PuppetPerms permissions)
    {
        using var _ = ImRaii.Group();

        var categoryFilter = (uint)(permissions);
        foreach (var category in Enum.GetValues<PuppetPerms>().Skip(1))
        {
            using (ImRaii.Disabled(_helper.SelectedPair is null))
                ImGui.CheckboxFlags($"Allow {category}", ref categoryFilter, (uint)category);
            
            CkGui.AttachToolTip(category switch
            {
                PuppetPerms.All => $"Allow {_helper.SelectedPair?.GetNickAliasOrUid() ?? "this Kinkster"} access to all commands.--SEP--(Take Care with this)",
                PuppetPerms.Alias => $"Allows {_helper.SelectedPair?.GetNickAliasOrUid() ?? "this Kinkster"} to make you execute alias triggers.",
                PuppetPerms.Emotes => $"Allows {_helper.SelectedPair?.GetNickAliasOrUid() ?? "this Kinkster"} to make you perform emotes.",
                PuppetPerms.Sit => $"Allows {_helper.SelectedPair?.GetNickAliasOrUid() ?? "this Kinkster"} to make you sit or cycle poses.",
                _ => $"NO PERMS ALLOWED."
            });
        }

        if (_helper.SelectedPair is { } validPair && validPair.OwnPerms.PuppetPerms != (PuppetPerms)categoryFilter)
            PermissionHelper.ChangeOwnUnique(_hub, validPair.UserData, validPair.OwnPerms, nameof(PairPerms.PuppetPerms), (PuppetPerms)categoryFilter).ConfigureAwait(false);
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
