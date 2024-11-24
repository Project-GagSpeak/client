using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.Interop.IpcHelpers.Penumbra;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagSpeak.Utils;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using System.Numerics;

namespace GagSpeak.UI.UiWardrobe;

public class CursedDungeonLoot : DisposableMediatorSubscriberBase
{
    private readonly PlayerCharacterData _clientPlayerData;
    private readonly SetPreviewComponent _drawDataHelper;
    private readonly ModAssociations _relatedMods;
    private readonly MoodlesAssociations _relatedMoodles;
    private readonly CursedLootHandler _handler;
    private readonly GagspeakConfigService _mainConfig;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;

    public CursedDungeonLoot(ILogger<CursedDungeonLoot> logger,
        GagspeakMediator mediator, PlayerCharacterData clientPlayerData,
        SetPreviewComponent drawDataHelper, ModAssociations relatedMods,
        MoodlesAssociations relatedMoodles, CursedLootHandler handler,
        GagspeakConfigService mainConfig, UiSharedService uiShared,
        TutorialService guides) : base(logger, mediator)
    {
        _clientPlayerData = clientPlayerData;
        _drawDataHelper = drawDataHelper;
        _relatedMods = relatedMods;
        _relatedMoodles = relatedMoodles;
        _handler = handler;
        _mainConfig = mainConfig;
        _uiShared = uiShared;
        _guides = guides;

        Mediator.Subscribe<TooltipSetItemToCursedItemMessage>(this, (msg) =>
        {
            // Identify what window is expanded and add the item to that.
            if (NewItem is not null)
            {
                NewItem.AppliedItem.Slot = msg.Slot;
                NewItem.AppliedItem.GameItem = msg.Item;
                Logger.LogDebug($"Set [" + msg.Slot + "] to [" + msg.Item.Name + "] on new cursed item", LoggerType.CursedLoot);

            }
            else
            {
                // apply it to the expanded window.
                if (ExpandedItemIndex != -1)
                {
                    FilteredItemList[ExpandedItemIndex].AppliedItem.Slot = msg.Slot;
                    FilteredItemList[ExpandedItemIndex].AppliedItem.GameItem = msg.Item;
                    Logger.LogDebug($"Set [" + msg.Slot + "] to [" + msg.Item.Name + "] on expanded item [" + FilteredItemList[ExpandedItemIndex].Name + "]", LoggerType.CursedLoot);
                }
            }
        });
    }

    // private vars used for searches or temp storage for handling hovers and value changes.
    private LowerString ItemSearchStr = LowerString.Empty;
    private int HoveredItemIndex = -1;
    private int ExpandedItemIndex = -1;
    private int HoveredCursedPoolIdx = -1;
    private bool CreatorExpanded = false;
    private string? TempLowerTimerRange = null;
    private string? TempUpperTimerRange = null;
    private List<CursedItem> FilteredItemList
    {
        get => _handler.CursedItems
            .Where(set => set.Name.Contains(ItemSearchStr, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private CursedItem? NewItem = null; // is null when not creating?

    public void DrawCursedLootPanel()
    {
        // If the expended item index is not -1, set creator expanded to false.
        if (ExpandedItemIndex != -1)
            CreatorExpanded = false;

        // inform user they dont have the settings enabled for it, and return.
        if (!_mainConfig.Current.CursedDungeonLoot)
        {
            _uiShared.BigText("Must Enable Cursed Dungeon Loot");
            UiSharedService.ColorText("This can be found in the Global GagSpeak Settings", ImGuiColors.ParsedGold);
            return;
        }

        // split thye UI veritcally in half. The right side will display the sets in the pool and the 
        var region = ImGui.GetContentRegionAvail();
        var topLeftSideHeight = region.Y;
        var width = ImGui.GetContentRegionAvail().X * .55f;

        // setup the usings for the styles in the following selectables that are shared.
        using var borderSize = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1f);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 4f);
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(4, 4));

        // draw out the selectables.
        using (ImRaii.Table("CursedLootItems", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("CursedItemList", ImGuiTableColumnFlags.WidthFixed, width);
            ImGui.TableSetupColumn("ActiveAndEnabledCursedItems", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow(); ImGui.TableNextColumn();
            var regionSize = ImGui.GetContentRegionAvail();

            using (ImRaii.Child($"###CursedItemsList", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
            {
                CursedLootHeader("Your Cursed Items");
                ImGui.Separator();
                DrawSearchFilter(regionSize.X, ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.Separator();
                NewItemWindow();
                DrawCursedItemList();
            }
            ImGui.TableNextColumn();

            regionSize = ImGui.GetContentRegionAvail();
            using (ImRaii.Child($"###ActiveCursedSets", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
            {
                CursedLootHeader("Enabled Pool");
                ImGui.Separator();
                DrawLockRangesAndChance(regionSize.X);
                ImGui.Separator();
                // Draw all items in the pool that are active, in order of their application, (Longest timer is top)
                DrawActiveItemsInPool();
                // Draw out all the inactive items still in the pool.
                DrawInactiveItemsInPool();
            }
            _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.TheEnabledPool, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
        }
    }

    private void DrawCursedItemList()
    {
        if (_handler.CursedItems.Count <= 0) return;

        var region = ImGui.GetContentRegionAvail();
        var topLeftSideHeight = region.Y;
        bool itemGotHovered = false;
        using (ImRaii.Child($"###CursedItemList", region with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
        {
            // print out the items in the list.
            for (int i = 0; i < FilteredItemList.Count; i++)
            {
                var item = FilteredItemList[i];
                bool isHovered = i == HoveredItemIndex;

                // draw the selectable item.
                CursedItemSelectable(item, i, hovered: i == HoveredItemIndex, expanded: i == ExpandedItemIndex,
                    onCaretPressed: (idx) => ExpandedItemIndex = (idx ? i : -1),
                    onItemEnabled: (enabled) => { item.InPool = enabled; _handler.Save(); });

                // if its not expanded and we are hovering, set the hovered index.
                if (ExpandedItemIndex != i && ImGui.IsItemHovered())
                {
                    itemGotHovered = true;
                    HoveredItemIndex = i;
                }
            }
        }
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.CursedItemList, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        // if the item was not hovered, reset hover index.
        if (!itemGotHovered)
            HoveredItemIndex = -1;
    }

    private void DrawActiveItemsInPool()
    {
        if (_handler.ActiveItemsDecending.Count <= 0) return;

        for (int i = 0; i < _handler.ActiveItemsDecending.Count; i++)
            EnabledItemSelectable(_handler.ActiveItemsDecending[i], i);
    }

    private void DrawInactiveItemsInPool()
    {
        if (_handler.InactiveItemsInPool.Count <= 0) return;

        bool activeCursedItemGotHovered = false;
        for (int i = 0; i < _handler.InactiveItemsInPool.Count; i++)
        {
            EnabledItemSelectable(_handler.InactiveItemsInPool[i], i, hovered: i == HoveredCursedPoolIdx, ShowButton: true,
                onButton: (disabled) => { _handler.InactiveItemsInPool[i].InPool = disabled; _handler.Save(); });

            // update the hovered item.
            if (ImGui.IsItemHovered())
            {
                activeCursedItemGotHovered = true;
                HoveredCursedPoolIdx = i;
            }
        }
        // if the item was not hovered, reset hover index.
        if (!activeCursedItemGotHovered)
            HoveredCursedPoolIdx = -1;
    }

    private void NewItemWindow()
    {
        // handle case where it is not yet made.
        if (NewItem is null)
        {
            NewItem = new CursedItem();
            return;
        }

        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudGrey);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var toggleButtonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.SyncAlt, "Equip");
        var caretButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.CaretDown);
        var selectableSize = CreatorExpanded
            ? new Vector2(UiSharedService.GetWindowContentRegionWidth(),
                (ImGui.GetFrameHeight() * (NewItem.IsGag ? 2 : 9) + ImGui.GetStyle().ItemSpacing.Y * (NewItem.IsGag ? 1 : 11) + ImGui.GetStyle().WindowPadding.Y * 2))
            : new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeight() + ImGui.GetStyle().WindowPadding.Y * 2);

        using var child = ImRaii.Child($"##NewItemWindow" + NewItem.LootId, selectableSize, true);
        using var group = ImRaii.Group();
        var yPos = ImGui.GetCursorPosY();
        var width = ImGui.GetContentRegionAvail().X;

        // draw out the item name. if we are editing it will be displayed differently.
        if (CreatorExpanded)
        {
            ImGui.SetCursorPosY(yPos + ((ImGui.GetFrameHeight() - 23) / 2) + 0.5f); // 23 is the input text box height
            ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
            var itemName = NewItem.Name;
            if (ImGui.InputTextWithHint("##ItemName" + NewItem.LootId, "Item Name...", ref itemName, 36))
                NewItem.Name = itemName;
            _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.NamingCursedItems, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(NewItem.Name);
        }

        // shift over to draw the buttons.
        ImGui.SameLine(width - (CreatorExpanded
            ? (toggleButtonSize + caretButtonSize.X * 2 + ImGui.GetStyle().ItemInnerSpacing.X * 2)
            : caretButtonSize.X));

        using (ImRaii.PushColor(ImGuiCol.Text, CreatorExpanded ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey))
        {
            // if we expanded, we should draw the save button.
            if (CreatorExpanded)
            {
                if (_uiShared.IconTextButton(FontAwesomeIcon.SyncAlt, NewItem.IsGag ? "Gag" : "Equip", width: toggleButtonSize, isInPopup: true))
                    NewItem.IsGag = !NewItem.IsGag;
                UiSharedService.AttachToolTip("Switch between Gag and Equip Cursed Item Types!");
                _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.SettingCursedItemType, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

                ImUtf8.SameLineInner();

                if (_uiShared.IconButton(FontAwesomeIcon.Plus, inPopup: true))
                {
                    _handler.AddItem(NewItem);
                    NewItem = null;
                    CreatorExpanded = false;
                    Logger.LogDebug("Adding new Item to Cursed Item List!");
                }
                UiSharedService.AttachToolTip("Add this Cursed Item!");
                _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.AddingCursedItem, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

                ImUtf8.SameLineInner();
            }

            if (_uiShared.IconButton(CreatorExpanded ? FontAwesomeIcon.CaretUp : FontAwesomeIcon.CaretDown, inPopup: true))
                CreatorExpanded = !CreatorExpanded;
        }
        // tutorial stuff
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.CreatingCursedItems, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () => CreatorExpanded = true);

        // if we are expanded, draw the details.
        if (CreatorExpanded && NewItem is not null)
        {
            if (NewItem.IsGag)
            {
                _uiShared.DrawCombo("##AttachGagtoCursedItem" + NewItem.LootId, width, Enum.GetValues<GagType>(),
                (gag) => gag.GagName(), (i) => NewItem.GagType = i, initialSelectedItem: NewItem.GagType);
                UiSharedService.AttachToolTip("Select the type of Gag this Cursed Item will apply!");
                return;
            }
            else DrawItemWindowExpanded(NewItem);
        }
    }

    private void CursedItemSelectable(CursedItem item, int idx, bool hovered = false, bool expanded = false, Action<bool>? onCaretPressed = null, Action<bool>? onItemEnabled = null)
    {
        using var borderCol = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        using var bgColor = hovered
            ? ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered))
            : ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var caretButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.CaretDown);
        var selectableSize = expanded
            ? new Vector2(UiSharedService.GetWindowContentRegionWidth(),
                (ImGui.GetFrameHeight() * (item.IsGag ? 2 : 9) + ImGui.GetStyle().ItemSpacing.Y * (item.IsGag ? 1 : 11) + ImGui.GetStyle().WindowPadding.Y * 2))
            : new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeight() + ImGui.GetStyle().WindowPadding.Y * 2);

        using var child = ImRaii.Child($"##CursedItemListing" + item.LootId, selectableSize, true);
        using var group = ImRaii.Group();
        var yPos = ImGui.GetCursorPosY();

        // obtain the width to skip for the buttons
        var width = caretButtonSize.X;
        if (expanded) width += caretButtonSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
        if (!item.InPool) width += caretButtonSize.X + ImGui.GetStyle().ItemInnerSpacing.X;

        // draw out the item name. if we are editing it will be displayed differently.
        if (expanded)
        {
            ImGui.SetCursorPosY(yPos + ((ImGui.GetFrameHeight() - 23) / 2) + 0.5f); // 23 is the input text box height
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - width - ImGui.GetStyle().ItemSpacing.X);
            var itemName = item.Name;
            if (ImGui.InputTextWithHint("##ItemName" + item.LootId, "Item Name...", ref itemName, 36, ImGuiInputTextFlags.EnterReturnsTrue))
                item.Name = itemName;
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(" " + item.Name);
        }

        // shift over to draw the buttons.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - width);

        // if we are not creating a new item, we should draw the add to pool button.
        if (!item.InPool)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGold))
            {
                if (_uiShared.IconButton(FontAwesomeIcon.ArrowRight, inPopup: true))
                    onItemEnabled?.Invoke(!item.InPool);
                UiSharedService.AttachToolTip("Add this Item to the Cursed Loot Pool.");
            }
            if (idx is 0) _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.AddingToEnabledPool, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

            ImUtf8.SameLineInner();
        }

        if (expanded)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            {
                if (_uiShared.IconButton(FontAwesomeIcon.Trash, disabled: item.InPool || !KeyMonitor.ShiftPressed(), inPopup: true))
                {
                    _handler.RemoveItem(item.LootId);
                    Logger.LogInformation("Removing " + item.Name + " from cursed item list.");
                }
                UiSharedService.AttachToolTip("Remove this Cursed Item from your storage! (Hold Shift)");
            }
            ImUtf8.SameLineInner();
        }

        // add the caret button.
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
        {
            if (_uiShared.IconButton(expanded ? FontAwesomeIcon.CaretUp : FontAwesomeIcon.CaretDown, inPopup: true))
                onCaretPressed?.Invoke(!expanded);
            UiSharedService.AttachToolTip((item.AppliedTime != DateTimeOffset.MinValue)
                ? "Cannot edit item while active!"
                : "Expand to edit item details!");
        }

        // if we are expanded, draw the details.
        if (expanded)
        {
            if (item.IsGag)
            {
                _uiShared.DrawCombo("##AttachGagtoCursedItem" + item.LootId, ImGui.GetContentRegionAvail().X, Enum.GetValues<GagType>(),
                (gag) => gag.GagName(), (i) => item.GagType = i, initialSelectedItem: item.GagType);
                UiSharedService.AttachToolTip("Select the type of Gag this Cursed Item will apply!");
                return;
            }
            else DrawItemWindowExpanded(item);
        }
    }

    private void EnabledItemSelectable(CursedItem item, int idx, bool hovered = false, bool ShowButton = false, Action<bool>? onButton = null)
    {
        using var borderCol = item.AppliedTime != DateTimeOffset.MinValue
            ? ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.HealerGreen)
            : ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // push a less transparent very dark grey background color.
        using var bgColor = hovered
            ? ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered))
            : ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.25f, 0.2f, 0.2f, 0.4f));

        var caretButtonSize = _uiShared.GetIconButtonSize(FontAwesomeIcon.ArrowLeft);
        var selectableSize = new Vector2(UiSharedService.GetWindowContentRegionWidth(), (ShowButton
            ? ImGui.GetFrameHeight() + ImGui.GetStyle().WindowPadding.Y * 2
            : ImGui.GetFrameHeight() + ImGui.GetStyle().WindowPadding.Y * 2));// ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2));

        using var child = ImRaii.Child($"##EnabledSelectable" + item.LootId, selectableSize, true);
        using var group = ImRaii.Group();

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(item.Name);

        // if we are showing the button, then we are drawing the enabled item. Otherwise, we are drawing the time remaining.
        if (ShowButton)
        {
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - caretButtonSize.X);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGold))
            {
                if (_uiShared.IconButton(FontAwesomeIcon.ArrowLeft, inPopup: true))
                    onButton?.Invoke(!item.InPool);
                UiSharedService.AttachToolTip("Remove this Item to the Cursed Loot Pool.");
            }
            if (idx is 0) _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.RemovingFromEnabledPool, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
        }
        // otherwise, we should display the remaining time since it is active.
        if (item.AppliedTime != DateTimeOffset.MinValue)
        {
            ImGui.SameLine();
            UiSharedService.ColorText(UiSharedService.TimeLeftFancy(item.ReleaseTime), ImGuiColors.HealerGreen);
        }
    }

    private void CursedLootHeader(string text)
    {
        var startYpos = ImGui.GetCursorPosY();
        Vector2 textSize;
        using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize(text); }
        using (ImRaii.Child("CursedSetsHeader" + text, new Vector2(UiSharedService.GetWindowContentRegionWidth(), 40)))
        {
            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X / 2 - textSize.X / 2));
            ImGui.SetCursorPosY(startYpos + 3f);
            _uiShared.BigText(text);
        }
    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter(float availableWidth, float spacingX)
    {
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - spacingX);
        string filter = ItemSearchStr;
        if (ImGui.InputTextWithHint("##CursedItemFilter", "Search your Cursed Items", ref filter, 255))
            ItemSearchStr = filter;

        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(ItemSearchStr));
        if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
            ItemSearchStr = string.Empty;
    }

    public void DrawLockRangesAndChance(float availableWidth)
    {
        // Define the widths for input fields and the slider
        float inputWidth = (availableWidth - _uiShared.GetIconData(FontAwesomeIcon.HourglassHalf).X - ImGui.GetStyle().ItemInnerSpacing.X * 2 - ImGui.CalcTextSize("100.9%  ").X) / 2;

        // Input Field for the first range
        ImGui.SetNextItemWidth(inputWidth);
        var spanLow = _handler.LowerLockLimit;
        TempLowerTimerRange = spanLow == TimeSpan.Zero ? string.Empty : _uiShared.TimeSpanToString(spanLow);
        if (ImGui.InputTextWithHint("##Timer_Input_Lower", "Ex: 0h2m7s", ref TempLowerTimerRange, 12, ImGuiInputTextFlags.EnterReturnsTrue))
            if (_uiShared.TryParseTimeSpan(TempLowerTimerRange, out var timeSpan))
                _handler.SetLowerLimit(timeSpan);
        UiSharedService.AttachToolTip("Min Cursed Lock Time.");
        // tutorial stuff
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.LowerLockTimer, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        ImUtf8.SameLineInner();
        _uiShared.IconText(FontAwesomeIcon.HourglassHalf, ImGuiColors.ParsedGold);

        ImUtf8.SameLineInner();
        // Input Field for the second range
        ImGui.SetNextItemWidth(inputWidth);
        var spanHigh = _handler.UpperLockLimit;
        TempUpperTimerRange = spanHigh == TimeSpan.Zero ? string.Empty : _uiShared.TimeSpanToString(spanHigh);
        if (ImGui.InputTextWithHint("##Timer_Input_Upper", "Ex: 0h2m7s", ref TempUpperTimerRange, 12, ImGuiInputTextFlags.EnterReturnsTrue))
            if (_uiShared.TryParseTimeSpan(TempUpperTimerRange, out var timeSpan))
                _handler.SetUpperLimit(timeSpan);
        UiSharedService.AttachToolTip("Max Cursed lock Time.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.UpperLockTimer, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        ImUtf8.SameLineInner();
        // Slider for percentage adjustment
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var percentage = _handler.LockChance;
        if (ImGui.DragInt("##Percentage", ref percentage, 0.1f, 0, 100, "%d%%"))
        {
            _handler.SetLockChance(percentage);
        }
        UiSharedService.AttachToolTip("The % Chance that opening Dungeon Loot will contain Cursed Bondage Loot.");
        _guides.OpenTutorial(TutorialType.CursedLoot, StepsCursedLoot.RollChance, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
    }

    public void DrawItemWindowExpanded(CursedItem item)
    {
        using var disabled = ImRaii.Disabled((item.AppliedTime != DateTimeOffset.MinValue));
        // define some of the basic options.
        var canOverride = item.CanOverride;
        if (ImGui.Checkbox("Overridable", ref canOverride))
        {
            item.CanOverride = canOverride;
            _handler.Save();
        }
        UiSharedService.AttachToolTip("If this item can be overridden by another cursed item in the pool."
            + Environment.NewLine + "(Must have a higher Precedence to do so)");

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Very Highmmm").X);
        var precedence = item.OverridePrecedence;
        _uiShared.DrawCombo("##ItemPrecedence" + item.LootId, ImGui.CalcTextSize("Very Highmmm").X, Enum.GetValues<Precedence>(),
            (clicked) => clicked.ToName(),
            onSelected: (i) =>
            {
                item.OverridePrecedence = i;
                _handler.Save();
            },
            initialSelectedItem: item.OverridePrecedence);
        UiSharedService.AttachToolTip("The Precedence of this item when comparing to other items in the pool."
            + Environment.NewLine + "Items with higher Precedence will be layered ontop of items in the same slot with lower Precedence.");

        // Define the related Glamour Item
        ImGui.Separator();
        _drawDataHelper.DrawEquipDataDetailedSlot(item.AppliedItem, ImGui.GetContentRegionAvail().X);
        UiSharedService.AttachToolTip("The Item that will be applied to the player when this Cursed Item is active.");

        // Define the related Mod
        ImGui.Separator();
        SelectableModSelection(item, ImGui.GetContentRegionAvail().X);

        // define the selectable Moodle
        ImGui.Separator();
        SelectableMoodleSelection(item, ImGui.GetContentRegionAvail().X);
    }

    private void SelectableModSelection(CursedItem item, float availableWidth)
    {
        using var group = ImRaii.Group();

        var buttonWidth = _uiShared.GetIconButtonSize(FontAwesomeIcon.Redo).X + _uiShared.GetIconButtonSize(FontAwesomeIcon.Times).X;
        _relatedMods.DrawCursedItemSelection(item, availableWidth - ImGui.GetStyle().ItemInnerSpacing.Y - _uiShared.GetIconButtonSize(FontAwesomeIcon.VoteYea).X);
        ImUtf8.SameLineInner();
        if (_uiShared.IconButton(FontAwesomeIcon.VoteYea, disabled: _relatedMods.CurrentSelection.Mod.Name.IsNullOrEmpty()))
        {
            item.AssociatedMod.Mod = _relatedMods.CurrentSelection.Mod;
            item.AssociatedMod.ModSettings = _relatedMods.CurrentSelection.Settings;
            _handler.Save();
        }
        UiSharedService.AttachToolTip("Make this mod bound to the cursed Item.");

        string truncatedText = item.AssociatedMod.Mod.Name.IsNullOrEmpty()
            ? "No Mod Selected"
            : (item.AssociatedMod.Mod.Name.Length > 33)
                ? item.AssociatedMod.Mod.Name.Substring(0, 33) + "..."
                : item.AssociatedMod.Mod.Name;

        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText(" " + truncatedText, ImGuiColors.ParsedGold);
        UiSharedService.AttachToolTip(item.AssociatedMod.Mod.Name.IsNullOrEmpty()
            ? "Select a Mod from the list above first."
            : "The Selected Mod bound to this cursed Item.\nFull Name: " + item.AssociatedMod.Mod.Name);

        // use sameline to jump to the end minus two button width.
        ImGui.SameLine(availableWidth - ImGui.GetStyle().ItemInnerSpacing.X - buttonWidth);

        // set icon and help text
        var icon = item.AssociatedMod.DisableWhenInactive ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;
        var helpText = item.AssociatedMod.DisableWhenInactive ? "Mod will be disabled once the cursed item is removed." : "Mod will stay enabled once the cursed item is removed.";
        if (_uiShared.IconButton(icon, disabled: item.AssociatedMod.Mod.Name.IsNullOrEmpty()))
        {
            item.AssociatedMod.DisableWhenInactive = !item.AssociatedMod.DisableWhenInactive;
            _handler.Save();
        }
        UiSharedService.AttachToolTip(helpText);

        ImUtf8.SameLineInner();

        var icon2 = item.AssociatedMod.RedrawAfterToggle ? FontAwesomeIcon.Redo : FontAwesomeIcon.Slash;
        var helpText2 = item.AssociatedMod.RedrawAfterToggle ? "Perform a redraw after Cursed Item is Applied/Removed (nessisary for VFX/Animation Mods)" : "Perform no redraw on item apply/remove";
        if (_uiShared.IconButton(icon2, disabled: item.AssociatedMod.Mod.Name.IsNullOrEmpty()))
        {
            item.AssociatedMod.RedrawAfterToggle = !item.AssociatedMod.RedrawAfterToggle;
            _handler.Save();
        }
        UiSharedService.AttachToolTip(helpText2);
    }

    private void SelectableMoodleSelection(CursedItem item, float width)
    {
        if (_clientPlayerData.IpcDataNull)
            return;

        // Define the related Moodle
        _uiShared.DrawCombo("##CursedItemMoodleType" + item.LootId, 90f, Enum.GetValues<IpcToggleType>(),
            (clicked) => clicked.ToName(),
            onSelected: (i) =>
            {
                item.MoodleType = i;
                item.MoodleIdentifier = Guid.Empty;
                _handler.Save();
            },
            initialSelectedItem: item.MoodleType,
            flags: ImGuiComboFlags.NoArrowButton);
        UiSharedService.AttachToolTip("The type of Moodle to apply with this item.");

        ImUtf8.SameLineInner();
        _relatedMoodles.MoodlesStatusSelectorForCursedItem(item, _clientPlayerData.LastIpcData!, ImGui.GetContentRegionAvail().X);

        // in new line, display the current moodle status or preset.
        var moodleText = string.Empty;
        if (item.MoodleType is IpcToggleType.MoodlesStatus)
        {
            var title = _clientPlayerData.LastIpcData!.MoodlesStatuses.FirstOrDefault(x => x.GUID == item.MoodleIdentifier).Title;

            moodleText = title.IsNullOrEmpty()
                ? (item.MoodleIdentifier == Guid.Empty ? "None Selected" : "ERROR")
                : "Status: " + title.StripColorTags();
        }
        else
            moodleText = "Preset: " + (item.MoodleIdentifier == Guid.Empty ? "None Selected" : item.MoodleIdentifier);

        ImGui.AlignTextToFramePadding();
        UiSharedService.ColorText(moodleText, ImGuiColors.ParsedGold);
        // if the identifier is valid and we are hovering it.
        if (item.MoodleIdentifier != Guid.Empty && ImGui.IsItemHovered())
        {
            if (item.MoodleType is IpcToggleType.MoodlesStatus)
                ImGui.SetTooltip("This Moodle Status will be applied with the item.");
            else
            {
                // get the list of friendly names for each guid in the preset
                var test = _clientPlayerData.LastIpcData!.MoodlesPresets
                    .FirstOrDefault(x => x.Item1 == item.MoodleIdentifier).Item2
                    .Select(x => _clientPlayerData.LastIpcData.MoodlesStatuses
                    .FirstOrDefault(y => y.GUID == x).Title ?? "No FriendlyName Set for this Moodle") ?? new List<string>();
                ImGui.SetTooltip($"This Preset Enables the Following Moodles:\n" + string.Join(Environment.NewLine, test));
            }
        }
    }
}
