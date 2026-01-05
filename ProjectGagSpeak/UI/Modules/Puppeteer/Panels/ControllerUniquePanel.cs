using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using OtterGui.Text;

namespace GagSpeak.Gui.Modules.Puppeteer;

public sealed class ControllerUniquePanel
{
    private readonly ILogger<ControllerUniquePanel> _logger;
    private readonly MainHub _hub;
    private readonly AliasItemDrawer _aliasDrawer;
    private readonly KinksterManager _kinksters;

    private string _searchStr = string.Empty;
    private IReadOnlyList<AliasTrigger> _filteredItems = new List<AliasTrigger>();
    private TagCollection _pairTriggerTags = new();

    public ControllerUniquePanel(ILogger<ControllerUniquePanel> logger, MainHub hub, 
        MainConfig config, AliasItemDrawer aliasDrawer, KinksterManager kinksters)
    {
        _logger = logger;
        _hub = hub;
        _aliasDrawer = aliasDrawer;
        _kinksters = kinksters;

        UpdateFilteredItems();
    }

    private Kinkster? selectedKinkster = null;
    public Kinkster? SelectedKinkster
    {
        get { return selectedKinkster; }
        set
        {
            selectedKinkster = value;
            UpdateFilteredItems();
        }
    }

    private void UpdateFilteredItems()
    {
        if (_searchStr.IsNullOrEmpty())
            _filteredItems = SelectedKinkster?.LastPairAliasData.Storage.Items ?? new List<AliasTrigger>();
        else
            _filteredItems = SelectedKinkster?.LastPairAliasData.Storage.Items
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
        if (FancySearchBar.Draw("##PairControllerSearch", width,  ref _searchStr, "Search for an Alias", 200))
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

        if (SelectedKinkster is null)
            return;

        foreach (var aliasItem in _filteredItems.ToList())
            _aliasDrawer.DrawAliasTrigger(aliasItem, SelectedKinkster.MoodleData, out bool _, false);
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
        var padding = ImGui.GetStyle().FramePadding;
        var spacing = ImGui.GetStyle().ItemInnerSpacing;
        var seperators = CkGui.GetSeparatorSpacedHeight(spacing.Y);
        var triggerPhrasesH = CkStyle.GetFrameRowsHeight(3);
        var permissionsH = CkStyle.GetFrameRowsHeight(4);
        var size = new Vector2(region.SizeX, triggerPhrasesH.AddWinPadY() + permissionsH + seperators + ImGui.GetFrameHeightWithSpacing());

        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());
        using var c = CkRaii.ChildLabelCustomFull("PairController", size, ImGui.GetFrameHeight(), CustomHeader, DFlags.RoundCornersLeft, LabelFlags.AddPaddingToHeight);

        if (SelectedKinkster is null)
            return;

        DrawListenerNameBracketsRow(c.InnerRegion.X, SelectedKinkster);

        DrawTriggerPhraseBox(c.InnerRegion.X, triggerPhrasesH, SelectedKinkster);

        CkGui.SeparatorSpaced(CkColor.FancyHeaderContrast.Uint(), spacing.Y, c.InnerRegion.X);

        // Draw out the global puppeteer image.
        if (CosmeticService.CoreTextures.Cache[CoreTexture.PuppetMaster] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((c.InnerRegion.X / 2) - permissionsH) / 2));
            ImGui.Image(wrap.Handle, new Vector2(permissionsH));
        }

        // Draw out the permission checkboxes
        ImGui.SameLine(c.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);

        DrawPuppetPermsGroup(SelectedKinkster.PairPerms.PuppetPerms, SelectedKinkster);

        void CustomHeader()
        {
            var headerText = SelectedKinkster is not null ? $"{SelectedKinkster.GetNickAliasOrUid()}'s Settings" : "Select a Kinkster from the 2nd panel first!";
            var SendNameWidth = CkGui.IconTextButtonSize(FAI.CloudUploadAlt, "Send Name");
            using (CkRaii.Child("##PairSelector", new Vector2(region.SizeX, ImGui.GetFrameHeight())))
            {
                using var col = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint());
                ImGui.SameLine(ImGui.GetFrameHeight());
                ImUtf8.TextFrameAligned(headerText);
                if (SelectedKinkster is not null)
                {
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - SendNameWidth - spacing.X);
                    var wdl = ImGui.GetWindowDrawList();
                    wdl.ChannelsSplit(2);
                    wdl.ChannelsSetCurrent(1);
                    if (CkGui.SmallIconTextButton(FAI.CloudUploadAlt, "Send Name"))
                        SendNameTask(SelectedKinkster);
                    CkGui.AttachToolTip($"Update {SelectedKinkster.GetNickAliasOrUid()} with your IGN." +
                        $"--SEP--This allows them to react to you using their trigger phrases.");
                    wdl.ChannelsSetCurrent(0);
                    wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.FancyHeaderContrast.Uint(), ImGui.GetStyle().FrameRounding, ImDrawFlags.RoundCornersAll);
                    wdl.ChannelsMerge();
                }
            }
        }
    }

    private void DrawListenerNameBracketsRow(float width, Kinkster kinkster)
    {
        var bracketsWidth = ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.X;
        var playerStrRef = kinkster.LastPairAliasData.ExtractedListenerName;
        var label = playerStrRef.IsNullOrEmpty() ? "<No Name@World Set!>" : playerStrRef;
        var tooltip = $"The Character that can Puppeteer {kinkster.GetNickAliasOrUid()} using the below phrases." +
            $"--SEP--This should be your Player's name. If it isn't, send them yours.";

        CkGui.FramedIconText(FAI.Eye, !playerStrRef.IsNullOrEmpty() ? CkColor.IconCheckOn.Uint() : uint.MaxValue);
        CkGui.AttachToolTip(tooltip);
        ImUtf8.SameLineInner();
        var listenerWidth = ImGui.GetContentRegionAvail().X - bracketsWidth;
        using (CkRaii.Child("ListenerName", new Vector2(listenerWidth, ImGui.GetFrameHeight()), CkColor.FancyHeaderContrast.Uint(), dFlags: DFlags.RoundCornersAll))
            CkGui.CenterTextAligned(label);
        CkGui.AttachToolTip(tooltip);

        ImUtf8.SameLineInner();
        var sChar = kinkster.PairPerms.StartChar.ToString();
        var eChar = kinkster.PairPerms.EndChar.ToString();
        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
        ImGui.InputText("##BracketBegin", ref sChar, 1, ITFlags.ReadOnly);
        CkGui.AttachToolTip($"Optional Start Bracket to scope the command after the trigger phrase in.");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
        ImGui.InputText("##BracketEnd", ref eChar, 1, ITFlags.ReadOnly);
        CkGui.AttachToolTip($"Optional End Bracket to scope the command after the trigger phrase in.");
    }

    private void DrawTriggerPhraseBox(float paddedWidth, float height, Kinkster kinkster)
    {
        using var _ = CkRaii.FramedChildPaddedW("Triggers", paddedWidth, height, CkColor.FancyHeaderContrast.Uint(), CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll);
        _pairTriggerTags.DrawTagsPreview("##OtherPairPhrases", kinkster.PairPerms.TriggerPhrase);
    }

    private void DrawPuppetPermsGroup(PuppetPerms permissions, Kinkster kinkster)
    {
        using var _ = ImRaii.Group();

        var categoryFilter = (uint)permissions;
        foreach (var category in Enum.GetValues<PuppetPerms>().Skip(1))
        {
            using (ImRaii.Disabled())
                ImGui.CheckboxFlags($"Allows {category}", ref categoryFilter, (uint)category);
            CkGui.AttachToolTip(category switch
            {
                PuppetPerms.All => $"{kinkster?.GetNickAliasOrUid() ?? "This Kinkster"} has granted you full control.--SEP--(Take Care with this)",
                PuppetPerms.Alias => $"{kinkster?.GetNickAliasOrUid() ?? "This Kinkster"} allows you to execute their alias triggers.",
                PuppetPerms.Emotes => $"{kinkster?.GetNickAliasOrUid() ?? "This Kinkster"} allows you to make them perform emotes.",
                PuppetPerms.Sit => $"{kinkster?.GetNickAliasOrUid() ?? "This Kinkster"} allows you to make them sit or cycle poses.",
                _ => $"NO PERMS ALLOWED."
            });
        }
    }

    private void DrawExamplesBox(Vector2 region)
    {
        var size = new Vector2(region.X, ImGui.GetFrameHeightWithSpacing() * 3);
        using (var child = CkRaii.ChildLabelText(size, .7f, "Example Uses", ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersLeft, LabelFlags.ResizeHeightToAvailable))
        {
            ImGui.TextWrapped("Ex 1: /gag <trigger phrase> <message>");
            ImGui.TextWrapped("Ex 2: /gag <trigger phrase> <message> <image>");
        }
    }

    private void SendNameTask(Kinkster kinkster)
    {
        var nameInThread = PlayerData.NameWithWorld;
        UiService.SetUITask(async () =>
        {
            var res = await _hub.UserSendNameToKinkster(new(kinkster.UserData), nameInThread);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                _logger.LogError($"Failed to perform UserSendNameToKinker: {res.ErrorCode}");
                return;
            }
            // Success, update listener name on pair end (since we never get the callback).
            _kinksters.NewListenerName(kinkster.UserData, nameInThread);
        });
    }

}
