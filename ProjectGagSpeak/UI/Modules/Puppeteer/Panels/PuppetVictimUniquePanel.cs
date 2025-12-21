using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using OtterGui.Text;

namespace GagSpeak.Gui.Modules.Puppeteer;

public class PuppetVictimUniquePanel : DisposableMediatorSubscriberBase
{
    private readonly ILogger<PuppetVictimUniquePanel> _logger;
    private readonly ControllerUniquePanel _controllerPanel; // for updating the selected kinkster.
    private readonly MainHub _hub;
    private readonly AliasItemDrawer _aliasDrawer;
    private readonly PuppeteerManager _manager;

    private PairCombo _pairCombo;

    private string _searchStr = string.Empty;
    private IReadOnlyList<AliasTrigger> _filteredItems = new List<AliasTrigger>();
    private IEnumerable<InvokableActionType> _actionTypes = Enum.GetValues<InvokableActionType>();
    private InvokableActionType _selectedType = InvokableActionType.Gag;
    private TagCollection _pairTriggerTags = new();

    public PuppetVictimUniquePanel(ILogger<PuppetVictimUniquePanel> logger, GagspeakMediator mediator,
        MainConfig config, MainHub hub, AliasItemDrawer aliasDrawer, FavoritesManager favorites,
        KinksterManager kinksters, PuppeteerManager manager, ControllerUniquePanel controllerPanel)
        : base(logger, mediator)
    {
        _logger = logger;
        _controllerPanel = controllerPanel;
        _hub = hub;
        _aliasDrawer = aliasDrawer;
        _manager = manager;
        _pairCombo = new PairCombo(logger, mediator, config, kinksters, favorites);
    }

    public Kinkster? Selected => _controllerPanel.SelectedKinkster;

    private void UpdateFilteredItems()
    {
        var managerStorageForSel = _manager.PairAliasStorage.GetValueOrDefault(Selected?.UserData.UID ?? string.Empty)?.Storage.Items ?? new List<AliasTrigger>();
        if (_searchStr.IsNullOrEmpty())
            _filteredItems = managerStorageForSel;
        else
            _filteredItems = managerStorageForSel.Where(x => x.Label.Contains(_searchStr, StringComparison.OrdinalIgnoreCase)).ToList();
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
        if (FancySearchBar.Draw("##PairVictimSearch", width, ref _searchStr, "Search for an Alias", 200, ImGui.GetFrameHeight(), AddTriggerButton))
        {
            Logger.LogInformation($"Searching for Alias: {_searchStr}");
            UpdateFilteredItems();
        }

        void AddTriggerButton()
        {
            if (CkGui.IconButton(FAI.Plus, inPopup: true) && Selected is not null)
            {
                _manager.CreateNew(Selected.UserData.UID);
                UpdateFilteredItems();
                Logger.LogInformation("Added a new Alias Trigger.");
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
        if (_pairCombo.Current == null)
        {
            return;
        }
        foreach (var aliasItem in _filteredItems.ToList())
        {
            if (aliasItem.Identifier == _manager.ItemInEditor?.Identifier)
            {
                {
                    _aliasDrawer.DrawAliasTriggerEditor(_actionTypes, ref _selectedType, out var result);
                    switch (result)
                    {
                        case DrawAliasTriggerButtonAction.NoAction:
                            // No action at this time
                            break;
                        case DrawAliasTriggerButtonAction.Delete:
                            _manager.Delete(aliasItem);
                            _manager.StopEditing();
                            PushUpdateAliasTask(_pairCombo.Current.UserData, aliasItem.Identifier, null);
                            break;
                        case DrawAliasTriggerButtonAction.Revert:
                            _manager.StopEditing();
                            break;
                        case DrawAliasTriggerButtonAction.SaveChanges:
                            _manager.SaveChangesAndStopEditing();
                            PushUpdateAliasTask(_pairCombo.Current.UserData, aliasItem.Identifier, aliasItem);
                            break;
                    }
                }
            }
            else
            {
                _aliasDrawer.DrawAliasTrigger(aliasItem, MoodleCache.IpcData, out bool startEditing, true);
                if (startEditing)
                {
                    _manager.StartEditing(aliasItem, _pairCombo.Current.UserData.UID);
                }
            }
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
        var frameBgCol = ImGui.GetColorU32(ImGuiCol.FrameBg);
        var padding = ImGui.GetStyle().FramePadding;
        var spacing = ImGui.GetStyle().ItemInnerSpacing;
        var listenerRow = ImGui.GetFrameHeightWithSpacing();
        var seperator = CkGui.GetSeparatorSpacedHeight(spacing.Y);
        var triggerPhrasesH = CkStyle.GetFrameRowsHeight(3); // 3 lines of buttons.
        var permissionsH = CkStyle.GetFrameRowsHeight(4);
        var size = new Vector2(drawRegion.SizeX, listenerRow + triggerPhrasesH.AddWinPadY() + permissionsH + seperator);

        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());
        using var c = CkRaii.ChildLabelCustomFull("PupPairSelector", size, ImGui.GetFrameHeight(), PairSelector, DFlags.RoundCornersLeft, LabelFlags.AddPaddingToHeight);

        // Disable all if the selected pair is null, but also keep alpha normal!
        using var dis = ImRaii.Disabled(Selected is null);
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 1f, Selected is null);

        DrawListenerNameBracketsRow(c.InnerRegion.X, Selected);
        DrawTriggerPhraseBox(c.InnerRegion.X, triggerPhrasesH, Selected);
        CkGui.SeparatorSpaced(CkColor.FancyHeaderContrast.Uint(), spacing.Y, c.InnerRegion.X);

        // Draw out the global puppeteer image.
        if (CosmeticService.CoreTextures.Cache[CoreTexture.PuppetVictimUnique] is { } wrap)
        {
            var pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(pos.X + (((c.InnerRegion.X / 2) - permissionsH) / 2));
            ImGui.Image(wrap.Handle, new Vector2(permissionsH));
        }

        // Draw out the permission checkboxes
        ImGui.SameLine(c.InnerRegion.X / 2, ImGui.GetStyle().ItemInnerSpacing.X);

        DrawPuppetPermsGroup(Selected, Selected?.OwnPerms.PuppetPerms ?? PuppetPerms.None);

        void PairSelector()
        {
            using (var hChild = CkRaii.Child("##PairSelector", new Vector2(drawRegion.SizeX, ImGui.GetFrameHeight())))
            {
                ImGui.SameLine(ImGui.GetFrameHeight());
                ImUtf8.TextFrameAligned("Your Settings For");
                ImGui.SameLine();
                var selectorW = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X;
                // Shift down by frame padding.
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);
                using var s = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(ImGui.GetStyle().FramePadding.X, 0));
                // Split channels and draw in foreground.
                var wdl = ImGui.GetWindowDrawList();
                wdl.ChannelsSplit(2);
                wdl.ChannelsSetCurrent(1);
                if (_pairCombo.Draw(Selected, selectorW, 1.25f, frameBgCol))
                {
                    Logger.LogInformation($"Selected Pair: {_pairCombo.Current?.GetNickAliasOrUid() ?? "None"} ({_pairCombo.Current?.UserData.ToString()})");
                    _controllerPanel.SelectedKinkster = _pairCombo.Current;
                    UpdateFilteredItems();
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    _pairCombo.ClearSelected();
                    _controllerPanel.SelectedKinkster = _pairCombo.Current;
                    UpdateFilteredItems();
                }
                // Draw the background.
                wdl.ChannelsSetCurrent(0);
                wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.FancyHeaderContrast.Uint(), ImGui.GetStyle().FrameRounding, ImDrawFlags.RoundCornersAll);
                wdl.ChannelsMerge();
            }
        }
    }

    private void DrawListenerNameBracketsRow(float width, Kinkster? k)
    {
        var bracketsWidth = ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().ItemSpacing.X;
        var listenerName = k is null ? string.Empty : _manager.PairAliasStorage.GetValueOrDefault(k.UserData.UID)?.ExtractedListenerName ?? string.Empty;
        var tooltip = k is null
            ? "No Kinkster selected, cannot display Listener Name."
            : $"The Player who can puppeteer you with the below phrases." +
            $"--SEP--This should be {k.GetNickAliasOrUid()}'s IGN. If it's not, they need to send you theirs.";

        CkGui.FramedIconText(FAI.Eye, !listenerName.IsNullOrEmpty() ? CkColor.IconCheckOn.Uint() : uint.MaxValue);
        CkGui.AttachToolTip(tooltip);
        ImUtf8.SameLineInner();
        var listenerWidth = ImGui.GetContentRegionAvail().X - bracketsWidth;
        using (CkRaii.Child("ListenerName", new Vector2(listenerWidth, ImGui.GetFrameHeight()), CkColor.FancyHeaderContrast.Uint(), dFlags: DFlags.RoundCornersAll))
            CkGui.CenterTextAligned(listenerName ?? "No Name Stored!");
        CkGui.AttachToolTip(tooltip);

        ImUtf8.SameLineInner();
        var sChar = k?.OwnPerms.StartChar.ToString() ?? string.Empty;
        var eChar = k?.OwnPerms.EndChar.ToString() ?? string.Empty;
        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
        ImGui.InputText("##BracketBegin", ref sChar, 1);
        if (ImGui.IsItemDeactivated() && k is not null && sChar.Length == 1)
        {
            if (sChar != k.OwnPerms.StartChar.ToString())
            {
                Logger.LogTrace($"Updating Start Bracket as it changed to: {sChar}");
                UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.StartChar), sChar[0]));
            }
        }
        CkGui.AttachToolTip($"Optional Start Bracket to scope the text command in.");

        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight());
        ImGui.InputText("##BracketEnd", ref eChar, 1);
        if (ImGui.IsItemDeactivated() && k is not null && eChar.Length == 1)
        {
            if (eChar != k.OwnPerms.EndChar.ToString())
            {
                Logger.LogTrace($"Updating End Bracket as it changed to: {eChar}");
                UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, k.UserData, k.OwnPerms, nameof(PairPerms.EndChar), eChar[0]));
            }
        }
        CkGui.AttachToolTip($"Optional End Bracket to scope the text command in.");
    }

    private void DrawTriggerPhraseBox(float paddedWidth, float height, Kinkster? kinkster)
    {
        using var _ = CkRaii.FramedChildPaddedW("Triggers", paddedWidth, height, CkColor.FancyHeaderContrast.Uint(), CkColor.FancyHeaderContrast.Uint(), ImDrawFlags.RoundCornersAll);
        if (kinkster is null)
            return;

        var triggerPhrase = kinkster?.OwnPerms.TriggerPhrase ?? string.Empty;
        if (_pairTriggerTags.DrawTagsEditor("##OwnPairPhrases", triggerPhrase, out var updatedString) && kinkster is not null)
        {
            Logger.LogTrace("The Tag Editor had an update!");
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(_hub, kinkster.UserData, kinkster.OwnPerms, nameof(PairPerms.TriggerPhrase), updatedString));
        }
    }

    private void DrawPuppetPermsGroup(Kinkster? kinkster, PuppetPerms perms)
    {
        using var _ = ImRaii.Group();

        var categoryFilter = (uint)(perms);
        foreach (var category in Enum.GetValues<PuppetPerms>().Skip(1))
        {
            ImGui.CheckboxFlags($"Allow {category}", ref categoryFilter, (uint)category);

            CkGui.AttachToolTip(category switch
            {
                PuppetPerms.All => $"Allow {kinkster?.GetNickAliasOrUid() ?? "this Kinkster"} access to all commands.--SEP--(Take Care with this)",
                PuppetPerms.Alias => $"Allows {kinkster?.GetNickAliasOrUid() ?? "this Kinkster"} to make you execute alias triggers.",
                PuppetPerms.Emotes => $"Allows {kinkster?.GetNickAliasOrUid() ?? "this Kinkster"} to make you perform emotes.",
                PuppetPerms.Sit => $"Allows {kinkster?.GetNickAliasOrUid() ?? "this Kinkster"} to make you sit or cycle poses.",
                _ => $"NO PERMS ALLOWED."
            });
        }

        if (kinkster is not null && kinkster.OwnPerms.PuppetPerms != (PuppetPerms)categoryFilter)
        {
            UiService.SetUITask(async () => await PermissionHelper.ChangeOwnUnique(
                _hub, kinkster.UserData, kinkster.OwnPerms, nameof(PairPerms.PuppetPerms), (PuppetPerms)categoryFilter));
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

    private void PushUpdateAliasTask(UserData user, Guid id, AliasTrigger? aliasTrigger)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _hub.UserPushAliasUniqueUpdate(new(user, id, aliasTrigger));
            _logger.LogTrace($"Pushing latest alias {id} with value {aliasTrigger}");
            if (res.ErrorCode is not GagSpeakApiEc.Success)
            {
                _logger.LogError($"Failed to perform Update alias {user.UID} Alias: {res.ErrorCode}");
                return;
            }
        });
    }
}
