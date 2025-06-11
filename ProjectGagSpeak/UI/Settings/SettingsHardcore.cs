using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Configs;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui;

public class SettingsHardcore
{
    private readonly ILogger<SettingsHardcore> _logger;
    private readonly MainConfigService _clientConfigs;
    private readonly KinksterRequests _globals;

    public SettingsHardcore(ILogger<SettingsHardcore> logger, MainConfigService config, KinksterRequests globals)
    {
        _logger = logger;
        _clientConfigs = config;
        _globals = globals;
    }

    public void DrawHardcoreSettings()
    {
        DisplayTextButtons();
        ImGui.Spacing();
        foreach (var node in _clientConfigs.Config.ForcedStayPromptList.Children.ToArray())
            DisplayTextEntryNode(node);
    }
/*
    private void DrawBlindfoldItem()
    {
        // define icon size and combo length
        IconSize = new Vector2(3 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y * 2);
        ComboLength = ComboWidth * ImGuiHelpers.GlobalScale;

        // on the new line, lets draw out a group, containing the image, and the slot, item, and stain listings.
        var BlindfoldDrawData = _wardrobeHandler.GetBlindfoldDrawData();

        // go to first column.
        CkGui.GagspeakBigText("Blindfold Item");
        using (ImRaii.Group())
        {
            BlindfoldDrawData.GameItem.DrawIcon(_itemStainHandler.IconData, IconSize, BlindfoldDrawData.Slot);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace($"Blindfold changed to {ItemService.NothingItem(BlindfoldDrawData.Slot)} [{ItemService.NothingItem(BlindfoldDrawData.Slot).ItemId}] " +
                    $"from {BlindfoldDrawData.GameItem} [{BlindfoldDrawData.GameItem.ItemId}]");
                BlindfoldDrawData.GameItem = ItemService.NothingItem(BlindfoldDrawData.Slot);
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
                    BlindfoldDrawData.GameItem = ItemService.NothingItem(BlindfoldDrawData.Slot);
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
            var forceLockFirstPerson = _clientConfigs.Config.ForceLockFirstPerson;
            var blindfoldOpacityPercentage = (int)(_clientConfigs.Config.BlindfoldMaxOpacity * 100);

            // Draw the first person selection.
            if (ImGui.Checkbox(GSLoc.Settings.Hardcore.BlindfoldFirstPerson, ref forceLockFirstPerson))
            {
                _clientConfigs.Config.ForceLockFirstPerson = forceLockFirstPerson;
                _clientConfigs.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Hardcore.BlindfoldFirstPersonTT);

            using (ImRaii.Disabled(_hardcoreHandler.IsBlindfolded))
            {
                // draw the lace type selection
                var selectedBlindfoldType = _clientConfigs.Config.BlindfoldStyle;
                CkGui.DrawCombo(GSLoc.Settings.Hardcore.BlindfoldType, 150f, Enum.GetValues<BlindfoldType>(), (type) => type.ToString(),
                (i) =>
                {
                    _clientConfigs.Config.BlindfoldStyle = i;
                    _clientConfigs.Save();
                    _logger.LogTrace($"Blindfold Style changed to {i}");
                }, selectedBlindfoldType);
            }
            CkGui.HelpText(GSLoc.Settings.Hardcore.BlindfoldTypeTT);

            using (ImRaii.Disabled(_hardcoreHandler.IsBlindfolded))
            {
                // draw the transparency slider, this displays on the slider a % symbol and translates to a float between 0 and 1 for the opacity.
                ImGui.SetNextItemWidth(150f);
                if (ImGui.SliderInt(GSLoc.Settings.Hardcore.BlindfoldMaxOpacity, ref blindfoldOpacityPercentage, 50, 100, "%d%% Opacity", ImGuiSliderFlags.None))
                {
                    _clientConfigs.Config.BlindfoldMaxOpacity = blindfoldOpacityPercentage / 100.0f;
                    _clientConfigs.Save();
                }
            }
            CkGui.HelpText(GSLoc.Settings.Hardcore.BlindfoldMaxOpacityTT);
        }
        ImGui.Separator();
        var filePath = _clientConfigs.Config.BlindfoldStyle switch
        {
            BlindfoldType.Light => "RequiredImages\\Blindfold_Light.png",
            BlindfoldType.Sensual => "RequiredImages\\Blindfold_Sensual.png",
            _ => "INVALID_FILE",
        };

        var previewImage = CkGui.GetImageFromAssetsFolder(filePath);
        if (previewImage is { } wrap)
        {
            // calculate the height of the available region and compare it to the ImGuiHandles Y height, to get how long we should display the X.
            // we need to do this to scale down the imagesize in the imguihandle to fit within the content region.
            var scale = Math.Min(ImGui.GetContentRegionAvail().X / wrap.Width, ImGui.GetContentRegionAvail().Y / wrap.Height);
            var finalSize = new Vector2(wrap.Width * scale, wrap.Height * scale);
            // display the image.
            ImGui.Image(wrap.ImGuiHandle, finalSize, Vector2.Zero, Vector2.One, new(1.0f, 1.0f, 1.0f, _clientConfigs.Config.BlindfoldMaxOpacity));
            CkGui.AttachToolTip("Preview of the Blindfold Style");
        }
    }
*/
    private void DisplayTextButtons()
    {
        if (_globals.GlobalPerms is not { } globals)
            return;

        // replace disabled with ForcedStay == true
        if (CkGui.IconTextButton(FAI.SearchPlus, "Last Seen TextNode", disabled: globals.ForcedStay.IsNullOrEmpty()))
        {
            _clientConfigs.AddLastSeenNode();
        }
        CkGui.AttachToolTip(GSLoc.Settings.Hardcore.AddNodeLastSeenTT);

        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.PlusCircle, "New TextNode", disabled: globals.ForcedStay.IsNullOrEmpty()))
        {
            _clientConfigs.CreateTextNode();
        }
        CkGui.AttachToolTip(GSLoc.Settings.Hardcore.AddNodeNewTT);

        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.PlusCircle, "New ChamberNode", disabled: globals.ForcedStay.IsNullOrEmpty()))
        {
            _clientConfigs.CreateChamberNode();
        }
        CkGui.AttachToolTip(GSLoc.Settings.Hardcore.AddNodeNewChamberTT);

        ImGui.SameLine();
        using (ImRaii.Disabled(globals.ForcedStay.IsNullOrEmpty()))
        {
            var enterChambersRef = _clientConfigs.Config.MoveToChambersInEstates;
            if (ImGui.Checkbox("Auto-Move to Chambers", ref enterChambersRef))
            {
                _clientConfigs.Config.MoveToChambersInEstates = enterChambersRef;
                _clientConfigs.Save();
            }
        }
        CkGui.AttachToolTip(GSLoc.Settings.Hardcore.ChamberAutoMoveTT);

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
        var disableElement = _clientConfigs.Config.ForcedStayPromptList.Children.Take(10).Contains(node);
        TextNodePopup(node, disableElement);
    }

    private void TextNodePopup(ITextNode node, bool disableElements = false)
    {
        var style = ImGui.GetStyle();
        using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(style.ItemSpacing.X / 2, style.ItemSpacing.Y));

        if (ImGui.BeginPopup($"{node.GetHashCode()}-popup"))
        {
            if (CkGui.IconButton(FAI.TrashAlt, disabled: disableElements || !KeyMonitor.ShiftPressed()))
            {
                if (_clientConfigs.TryFindParent(node, out var parentNode))
                {
                    parentNode!.Children.Remove(node);
                    // if the new size is now just 2 contents
                    if (parentNode.Children.Count == 0)
                        _clientConfigs.CreateTextNode();
                }
            }
            CkGui.AttachToolTip("Delete Custom Addition");

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
                CkGui.AttachToolTip("The Friendly name that will display in the ForcedStay Prompt List.");

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
                CkGui.AttachToolTip("The name of the node to look for when interacting with it.");

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
        CkGui.AttachToolTip("The text that is displayed in the prompt menu for this node.");

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
        CkGui.AttachToolTip("The option within the prompt that we should automatically select.");
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
        CkGui.AttachToolTip("This is the index to select from the (001-015) RoomSet list. Leave blank for first.");

        // Display the room index to automatically join into.
        var roomListIdxRef = node.ChamberListIdx;
        ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 225);
        if (ImGui.InputInt($"EnterRoom Index##{node.FriendlyName}-matchRoomIndexLabel", ref roomListIdxRef, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            node.ChamberListIdx = roomListIdxRef;
            _clientConfigs.Save();
        }
        CkGui.AttachToolTip("This is NOT the room number, it is the index from\ntop to bottom in the room listings, starting at 0.");

    }


    private LowerString PairSearchString = LowerString.Empty;
    public void DrawUidSearchFilter(float availableWidth)
    {
        var buttonSize = CkGui.IconTextButtonSize(FAI.Ban, "Clear");
        ImGui.SetNextItemWidth(availableWidth - buttonSize - ImGui.GetStyle().ItemInnerSpacing.X);
        string filter = PairSearchString;
        if (ImGui.InputTextWithHint("##filter", "Filter for UID/notes", ref filter, 255))
        {
            PairSearchString = filter;
        }
        ImUtf8.SameLineInner();
        using var disabled = ImRaii.Disabled(string.IsNullOrEmpty(PairSearchString));
        if (CkGui.IconTextButton(FAI.Ban, "Clear"))
        {
            PairSearchString = string.Empty;
        }
    }
}
