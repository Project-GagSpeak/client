using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Penumbra.String.Classes;
using System.Numerics;

namespace GagSpeak.UI;

public class SettingsHardcore
{
    private readonly ILogger<SettingsHardcore> _logger;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly GameItemStainHandler _itemStainHandler;
    private readonly WardrobeHandler _wardrobeHandler;
    private readonly HardcoreHandler _hardcoreHandler;
    private readonly UiSharedService _uiShared;

    private const float ComboWidth = 200;
    private Vector2 IconSize;
    private float ComboLength;
    private readonly GameItemCombo[] GameItemCombo;
    private readonly StainColorCombo StainCombo;

    public SettingsHardcore(ILogger<SettingsHardcore> logger,
        ClientConfigurationManager clientConfigs, GameItemStainHandler itemStainHandler,
        WardrobeHandler wardrobeHandler, HardcoreHandler hardcoreHandler, UiSharedService uiShared)
    {
        _logger = logger;
        _clientConfigs = clientConfigs;
        _itemStainHandler = itemStainHandler;
        _wardrobeHandler = wardrobeHandler;
        _hardcoreHandler = hardcoreHandler;
        _uiShared = uiShared;
        // create a new gameItemCombo for each equipment piece type, then store them into the array.
        GameItemCombo = _itemStainHandler.ObtainItemCombos();
        StainCombo = _itemStainHandler.ObtainStainCombos(ComboWidth);
    }

    public void DrawHardcoreSettings()
    {
        if (ImGui.BeginTabBar("hardcoreSettingsTabBar"))
        {
            if (ImGui.BeginTabItem("Blindfold Item"))
            {
                DrawBlindfoldItem();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Forced To Stay Filters"))
            {
                DisplayTextButtons();
                ImGui.Spacing();
                foreach (var node in _clientConfigs.GagspeakConfig.ForcedStayPromptList.Children.ToArray())
                    DisplayTextEntryNode(node);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawBlindfoldItem()
    {
        // define icon size and combo length
        IconSize = new Vector2(3 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y * 2);
        ComboLength = ComboWidth * ImGuiHelpers.GlobalScale;

        // on the new line, lets draw out a group, containing the image, and the slot, item, and stain listings.
        var BlindfoldDrawData = _wardrobeHandler.GetBlindfoldDrawData();

        // go to first column.
        _uiShared.GagspeakBigText("Blindfold Item");
        using (ImRaii.Group())
        {
            BlindfoldDrawData.GameItem.DrawIcon(_itemStainHandler.IconData, IconSize, BlindfoldDrawData.Slot);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace($"Blindfold changed to {ItemIdVars.NothingItem(BlindfoldDrawData.Slot)} [{ItemIdVars.NothingItem(BlindfoldDrawData.Slot).ItemId}] " +
                    $"from {BlindfoldDrawData.GameItem} [{BlindfoldDrawData.GameItem.ItemId}]");
                BlindfoldDrawData.GameItem = ItemIdVars.NothingItem(BlindfoldDrawData.Slot);
                // update the draw data.
                _wardrobeHandler.SetBlindfoldDrawData(BlindfoldDrawData);
            }

            // right beside it, draw a secondary group of 3
            ImGui.SameLine(0, 6);
            using (var group = ImRaii.Group())
            {
                // display the wardrobe slot for this gag
                var refValue = Array.IndexOf(EquipSlotExtensions.EqdpSlots.ToArray(), BlindfoldDrawData.Slot);
                ImGui.SetNextItemWidth(ComboLength);
                if (ImGui.Combo(' ' + GSLoc.Settings.Hardcore.BlindfoldSlot + "##WardrobeEquipSlot", ref refValue, EquipSlotExtensions.EqdpSlots.Select(slot => slot.ToName()).ToArray(), EquipSlotExtensions.EqdpSlots.Count))
                {
                    // Update the selected slot when the combo box selection changes
                    BlindfoldDrawData.Slot = EquipSlotExtensions.EqdpSlots[refValue];
                    BlindfoldDrawData.GameItem = ItemIdVars.NothingItem(BlindfoldDrawData.Slot);
                    // update it.
                    _wardrobeHandler.SetBlindfoldDrawData(BlindfoldDrawData);
                }

                // if data changed, update it.
                if (DrawEquip(BlindfoldDrawData, GameItemCombo, StainCombo, ComboLength))
                    _wardrobeHandler.SetBlindfoldDrawData(BlindfoldDrawData);
            }
        }
        ImGui.SameLine(0,50);
        using (ImRaii.Group())
        { 
            var forceLockFirstPerson = _clientConfigs.GagspeakConfig.ForceLockFirstPerson;
            int blindfoldOpacityPercentage = (int)(_clientConfigs.GagspeakConfig.BlindfoldOpacity * 100);

            // Draw the first person selection.
            if (ImGui.Checkbox(GSLoc.Settings.Hardcore.BlindfoldFirstPerson, ref forceLockFirstPerson))
            {
                _clientConfigs.GagspeakConfig.ForceLockFirstPerson = forceLockFirstPerson;
                _clientConfigs.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Hardcore.BlindfoldFirstPersonTT);

            using (ImRaii.Disabled(_hardcoreHandler.IsBlindfolded))
            {
                // draw the lace type selection
                var selectedBlindfoldType = _clientConfigs.GagspeakConfig.BlindfoldStyle;
                _uiShared.DrawCombo(GSLoc.Settings.Hardcore.BlindfoldType, 150f, Enum.GetValues<BlindfoldType>(), (type) => type.ToString(),
                (i) =>
                {
                    _clientConfigs.GagspeakConfig.BlindfoldStyle = i;
                    _clientConfigs.Save();
                    _logger.LogTrace($"Blindfold Style changed to {i}");
                }, selectedBlindfoldType);
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Hardcore.BlindfoldTypeTT);

            using (ImRaii.Disabled(_hardcoreHandler.IsBlindfolded))
            {
                // draw the transparency slider, this displays on the slider a % symbol and translates to a float between 0 and 1 for the opacity.
                ImGui.SetNextItemWidth(150f);
                if (ImGui.SliderInt(GSLoc.Settings.Hardcore.BlindfoldOpacity, ref blindfoldOpacityPercentage, 50, 100, "%d%% Opacity", ImGuiSliderFlags.None))
                {
                    _clientConfigs.GagspeakConfig.BlindfoldOpacity = blindfoldOpacityPercentage / 100.0f;
                    _clientConfigs.Save();
                }
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Hardcore.BlindfoldOpacityTT);
        }
        ImGui.Separator();
        string filePath = _clientConfigs.GagspeakConfig.BlindfoldStyle switch
        {
            BlindfoldType.Light => "RequiredImages\\Blindfold_Light.png",
            BlindfoldType.Sensual => "RequiredImages\\Blindfold_Sensual.png",
            _ => "INVALID_FILE",
        };

        var previewImage = _uiShared.GetImageFromDirectoryFile(filePath);
        if (previewImage is { } wrap)
        {
            // calculate the height of the available region and compare it to the ImGuiHandles Y height, to get how long we should display the X.
            // we need to do this to scale down the imagesize in the imguihandle to fit within the content region.
            float scale = Math.Min(ImGui.GetContentRegionAvail().X / wrap.Width, ImGui.GetContentRegionAvail().Y / wrap.Height);
            Vector2 finalSize = new Vector2(wrap.Width * scale, wrap.Height * scale);
            // display the image.
            ImGui.Image(wrap.ImGuiHandle, finalSize, Vector2.Zero, Vector2.One, new(1.0f, 1.0f, 1.0f, _clientConfigs.GagspeakConfig.BlindfoldOpacity));
            UiSharedService.AttachToolTip("Preview of the Blindfold Style");
        }
    }

    private void DisplayTextButtons()
    {
        // replace disabled with ForcedStay == true
        if (_uiShared.IconTextButton(FontAwesomeIcon.SearchPlus, "Last Seen TextNode", disabled: _hardcoreHandler.IsForcedToStay))
        {
            _clientConfigs.AddLastSeenNode();
        }
        UiSharedService.AttachToolTip(GSLoc.Settings.Hardcore.AddNodeLastSeenTT);

        ImGui.SameLine();
        if (_uiShared.IconTextButton(FontAwesomeIcon.PlusCircle, "New TextNode", disabled: _hardcoreHandler.IsForcedToStay))
        {
            _clientConfigs.CreateTextNode();
        }
        UiSharedService.AttachToolTip(GSLoc.Settings.Hardcore.AddNodeNewTT);

        ImGui.SameLine();
        if (_uiShared.IconTextButton(FontAwesomeIcon.PlusCircle, "New ChamberNode", disabled: _hardcoreHandler.IsForcedToStay))
        {
            _clientConfigs.CreateChamberNode();
        }
        UiSharedService.AttachToolTip(GSLoc.Settings.Hardcore.AddNodeNewChamberTT);
        ImGui.SameLine();
        using (ImRaii.Disabled(_hardcoreHandler.IsForcedToStay))
        {
            bool enterChambersRef = _clientConfigs.GagspeakConfig.MoveToChambersInEstates;
            if (ImGui.Checkbox("Auto-Move to Chambers", ref enterChambersRef))
            {
                _clientConfigs.GagspeakConfig.MoveToChambersInEstates = enterChambersRef;
                _clientConfigs.Save();
            }
        }
        UiSharedService.AttachToolTip(GSLoc.Settings.Hardcore.ChamberAutoMoveTT);

        ImGui.Separator();
    }

    private void DisplayTextEntryNode(ITextNode node)
    {
        if (node.Enabled)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f));
        if (!node.Enabled)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(.5f, .5f, .5f, 1));

        ImGui.TreeNodeEx(node.FriendlyName + "##" + node.FriendlyName + "-tree", ImGuiTreeNodeFlags.Leaf);
        ImGui.TreePop();

        ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                node.Enabled = !node.Enabled;
                _clientConfigs.Save();
                return;
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup($"{node.GetHashCode()}-popup");
            }
        }

        // If the node is one we should disable
        var disableElement = _clientConfigs.GagspeakConfig.ForcedStayPromptList.Children.Take(10).Contains(node);
        TextNodePopup(node, disableElement);
    }

    private void TextNodePopup(ITextNode node, bool disableElements = false)
    {
        var style = ImGui.GetStyle();
        using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(style.ItemSpacing.X / 2, style.ItemSpacing.Y));

        if (ImGui.BeginPopup($"{node.GetHashCode()}-popup"))
        {
            if (_uiShared.IconButton(FontAwesomeIcon.TrashAlt, disabled: disableElements || !KeyMonitor.ShiftPressed()))
            {
                if (_clientConfigs.TryFindParent(node, out var parentNode))
                {
                    parentNode!.Children.Remove(node);
                    // if the new size is now just 2 contents
                    if (parentNode.Children.Count == 0)
                        _clientConfigs.CreateTextNode();
                }
            }
            UiSharedService.AttachToolTip("Delete Custom Addition");

            ImGui.SameLine();
            var nodeEnabled = node.Enabled;

            using (var disabled = ImRaii.Disabled(disableElements))
            {
                if (ImGui.Checkbox("Enabled", ref nodeEnabled))
                {
                    node.Enabled = nodeEnabled;
                    _clientConfigs.Save();
                }
                ImGui.SameLine();
                var targetRequired = node.TargetRestricted;
                if (ImGui.Checkbox("Target Restricted", ref targetRequired))
                {
                    node.TargetRestricted = targetRequired;
                    _clientConfigs.Save();
                }

                // Display the friendly name
                ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
                var friendlyName = node.FriendlyName;
                if (ImGui.InputTextWithHint($"Friendly Name##{node.FriendlyName}-matchFriendlyName",
                    hint: "Provide a friendly name to display in the list",
                    input: ref friendlyName,
                    maxLength: 60,
                    flags: ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    node.FriendlyName = friendlyName;
                    _clientConfigs.Save();
                }
                UiSharedService.AttachToolTip("The Friendly name that will display in the ForcedStay Prompt List.");

                // Display the label
                var nodeName = node.TargetNodeName;
                ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
                if (ImGui.InputTextWithHint($"Node Name##{node.TargetNodeName}-matchTextName",
                    hint: "The Name Above the Node you interact with",
                    input: ref nodeName,
                    maxLength: 100,
                    flags: ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    node.TargetNodeName = nodeName;
                    _clientConfigs.Save();
                }
                UiSharedService.AttachToolTip("The name of the node to look for when interacting with it.");

                // Draw unique fields if text node
                if (node is TextEntryNode textNode)
                    DrawTextEntryUniqueFields(textNode);
            }
            // Draw editable fields for the chamber node, but disable them if we are in ForcedStay mode.
            if (node is ChambersTextNode chambersNode)
                DrawChambersUniqueFields(chambersNode);

            ImGui.EndPopup();
        }
    }

    private void DrawTextEntryUniqueFields(TextEntryNode node)
    {
        // Display the label of the node to listen to.
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        var nodeLabel = node.TargetNodeLabel;
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        if (ImGui.InputTextWithHint($"Node Label##{node.TargetNodeLabel}-matchTextLebel",
            hint: "The Label given to the prompt menu the node provides",
            input: ref nodeLabel,
            maxLength: 1000,
            flags: ImGuiInputTextFlags.EnterReturnsTrue))
        {
            node.TargetNodeLabel = nodeLabel;
            _clientConfigs.Save();
        }
        UiSharedService.AttachToolTip("The text that is displayed in the prompt menu for this node.");

        // Display the target text to select from the list of options.
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        var selectedOption = node.SelectedOptionText;
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        if (ImGui.InputTextWithHint($"Select This##{node.SelectedOptionText}-matchTextOption",
            hint: "The Option from the prompt menu to select",
            input: ref selectedOption,
            maxLength: 200,
            flags: ImGuiInputTextFlags.EnterReturnsTrue))
        {
            node.SelectedOptionText = selectedOption;
            _clientConfigs.Save();
        }
        UiSharedService.AttachToolTip("The option within the prompt that we should automatically select.");
    }

    private void DrawChambersUniqueFields(ChambersTextNode node)
    {
        // Change this to be the forced stay conditional.
        using var disableWhileActive = ImRaii.Disabled(false);

        // Input Int field to select which room set index they want to pick.
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        var roomSetIdxRef = node.ChamberRoomSet;
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        if (ImGui.InputInt($"RoomSet Index##{node.FriendlyName}-matchSetIndexLabel", ref roomSetIdxRef, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            node.ChamberRoomSet = roomSetIdxRef;
            _clientConfigs.Save();
        }
        UiSharedService.AttachToolTip("This is the index to select from the (001-015) RoomSet list. Leave blank for first.");

        // Display the room index to automatically join into.
        var roomListIdxRef = node.ChamberListIdx;
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        if (ImGui.InputInt($"EnterRoom Index##{node.FriendlyName}-matchRoomIndexLabel", ref roomListIdxRef, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            node.ChamberListIdx = roomListIdxRef;
            _clientConfigs.Save();
        }
        UiSharedService.AttachToolTip("This is NOT the room number, it is the index from\ntop to bottom in the room listings, starting at 0.");

    }


    private LowerString PairSearchString = LowerString.Empty;
    public void DrawUidSearchFilter(float availableWidth)
    {
        var buttonSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - ImGui.GetStyle().ItemInnerSpacing.X);
        string filter = PairSearchString;
        if (ImGui.InputTextWithHint("##filter", "Filter for UID/notes", ref filter, 255))
        {
            PairSearchString = filter;
        }
        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(PairSearchString));
        if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear"))
        {
            PairSearchString = string.Empty;
        }
    }

    public bool DrawEquip(EquipDrawData blindfold, GameItemCombo[] _gameItemCombo, StainColorCombo _stainCombo, float _comboLength)
    {
        using var id = ImRaii.PushId((int)blindfold.Slot);
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        bool itemChange = false;
        bool stainChange = false;

        using var group = ImRaii.Group();
        itemChange = DrawItem(out var label, right, left, _comboLength, _gameItemCombo, blindfold);
        stainChange = DrawStain(_comboLength, _stainCombo, blindfold);

        return itemChange || stainChange;
    }

    private bool DrawItem(out string label, bool clear, bool open, float width,
    GameItemCombo[] _gameItemCombo, EquipDrawData blindfold)
    {
        // draw the item combo.
        var combo = _gameItemCombo[blindfold.Slot.ToIndex()];
        label = combo.Label;
        if (open)
        {
            GenericHelpers.OpenCombo($"##BlindfoldItem{blindfold.GameItem.Name}{combo.Label}");
            _logger.LogTrace($"{combo.Label} Toggled");
        }
        // draw the combo
        var change = combo.Draw(blindfold.GameItem.Name, blindfold.GameItem.ItemId, width, ComboWidth, ' ' + GSLoc.Settings.Hardcore.BlindfoldItem);

        // conditionals to detect for changes in the combo's
        if (change && !blindfold.GameItem.Equals(combo.CurrentSelection))
        {
            // log full details.
            _logger.LogTrace($"Item changed from {combo.CurrentSelection} [{combo.CurrentSelection.ItemId}] " +
                $"to {blindfold.GameItem} [{blindfold.GameItem.ItemId}]");
            blindfold.GameItem = combo.CurrentSelection;
            change = true;
        }

        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace($"Item changed to {ItemIdVars.NothingItem(blindfold.Slot)} [{ItemIdVars.NothingItem(blindfold.Slot).ItemId}] " +
                $"from {blindfold.GameItem} [{blindfold.GameItem.ItemId}]");
            blindfold.GameItem = ItemIdVars.NothingItem(blindfold.Slot);
            change = true;
        }

        return change;
    }

    private bool DrawStain(float width, StainColorCombo _stainCombo, EquipDrawData blindfold)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (blindfold.GameStain.Count - 1)) / blindfold.GameStain.Count;

        bool dyeChanged = false;

        foreach (var (stainId, index) in blindfold.GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = _itemStainHandler.TryGetStain(stainId, out var stain);
            // draw the stain combo.
            var change = _stainCombo.Draw($"##stain{blindfold.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, widthStains);
            if (index < blindfold.GameStain.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one.

            // if we had a change made, update the stain data.
            if (change)
            {
                if (_itemStainHandler.TryGetStain(_stainCombo.CurrentSelection.Key, out stain))
                {
                    // if changed, change it.
                    blindfold.GameStain = blindfold.GameStain.With(index, stain.RowIndex);
                    change = true;
                }
                else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                {
                    // if set to none, reset it to default
                    blindfold.GameStain = blindfold.GameStain.With(index, Stain.None.RowIndex);
                    change = true;
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // reset the stain to default
                blindfold.GameStain = blindfold.GameStain.With(index, Stain.None.RowIndex);
                change = true;
            }

            dyeChanged |= change;

        }
        ImGui.SameLine();
        ImGui.Text(' ' + GSLoc.Settings.Hardcore.BlindfoldDye);
        return dyeChanged;
    }
}
