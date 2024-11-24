using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.Interop.IpcHelpers.Penumbra;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Numerics;

namespace GagSpeak.UI.UiWardrobe;

public class RestraintSetEditor : IMediatorSubscriber
{
    private readonly ILogger<RestraintSetEditor> _logger;
    private readonly ModAssociations _relatedMods;
    private readonly MoodlesAssociations _relatedMoodles;
    private readonly PairManager _pairManager;
    private readonly GameItemStainHandler _itemStainHandler;
    private readonly UserPairListHandler _userPairListHandler;
    private readonly WardrobeHandler _handler;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;
    public GagspeakMediator Mediator { get; init; }

    public RestraintSetEditor(ILogger<RestraintSetEditor> logger, GagspeakMediator mediator,
        ModAssociations relatedMods, MoodlesAssociations relatedMoodles, PairManager pairManager,
        GameItemStainHandler stains, UserPairListHandler userPairList, WardrobeHandler handler,
        UiSharedService uiShared, TutorialService guides)
    {
        _logger = logger;
        Mediator = mediator;
        _relatedMods = relatedMods;
        _relatedMoodles = relatedMoodles;
        _pairManager = pairManager;
        _itemStainHandler = stains;
        _userPairListHandler = userPairList;
        _handler = handler;
        _uiShared = uiShared;
        _guides = guides;

        GameIconSize = new Vector2(2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y);
        // Assign Data to our combos.
        ItemCombos = _itemStainHandler.ObtainItemCombos();
        StainColorCombos = _itemStainHandler.ObtainStainCombos(ComboWidth);
        BonusItemCombos = _itemStainHandler.ObtainBonusItemCombos();

        Mediator.Subscribe<CharacterIpcDataCreatedMessage>(this, (msg) => LastCreatedCharacterData = msg.CharaIPCData);
    }

    // Info related to the person we are inspecting.
    private CharaIPCData LastCreatedCharacterData = null!;
    private readonly GameItemCombo[] ItemCombos;
    private readonly StainColorCombo StainColorCombos;
    private readonly BonusItemCombo[] BonusItemCombos;
    private string RefSearchString = string.Empty;
    private Vector2 GameIconSize;
    private const float ComboWidth = 200f;
    private float ItemComboLength;
    public string _setNextTab = "Info"; // Default selected tab

    // Can pass in the set to create, or the set to edit. Either will result in appropriate action.
    public void DrawRestraintSetEditor(RestraintSet refRestraint, Vector2 cellPadding)
    {
        // Create a tab bar for the display
        if (ImGui.BeginTabBar("Outfit_Editor"))
        {
            // Define tabs and their corresponding actions
            var tabs = new Dictionary<string, Action>
            {
                { "Info", () => DrawInfo(refRestraint) },
                { "Appearance", () => DrawAppearance(refRestraint) },
                { "Mods", () => _relatedMods.DrawUnstoredSetTable(refRestraint, cellPadding.Y) },
                { "Moodles", () => DrawMoodlesOptions(refRestraint, cellPadding.Y) },
                { "Sounds", () => DrawSpatialAudioOptions(refRestraint, cellPadding.Y) },
                { "Hardcore Traits", () => DrawVisibilityAndProperties(refRestraint) }
            };

            // Loop through tabs
            foreach (var tab in tabs)
            {
                // Open the specified tab programmatically
                using (var open = ImRaii.TabItem(tab.Key, _setNextTab == tab.Key ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                {
                    if(_setNextTab == tab.Key) _setNextTab = string.Empty;

                    switch(tab.Key)
                    {
                        case "Info":
                            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.InfoTab, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () => refRestraint.Name = "Tutorial Set");
                            break;
                        case "Appearance":
                            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ToAppearanceTab, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () => _setNextTab = "Appearance");
                            break;
                        case "Mods":
                            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ToModsTab, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () => _setNextTab = "Mods");
                            break;
                        case "Moodles":
                            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ToMoodlesTab, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () => _setNextTab = "Moodles");
                            break;
                        case "Sounds":
                            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ToSoundsTab, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () => _setNextTab = "Sounds");
                            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.Sounds, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
                            break;
                        case "Hardcore Traits":
                            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ToHardcoreTraitsTab, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize, () => _setNextTab = "Hardcore Traits");
                            break;
                    }                   
                    if (open) tab.Value.Invoke();
                }
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawInfo(RestraintSet refRestraintSet)
    {
        string refName = refRestraintSet.Name;
        var width = ImGui.GetContentRegionAvail().X * 0.7f;

        ImGui.Text("Restraint Set Name:");
        ImGui.SetNextItemWidth(width);
        if (ImGui.InputTextWithHint($"##NameText", "Restraint Set Name...", ref refName, 48, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            refRestraintSet.Name = refName;
        }
        UiSharedService.AttachToolTip($"Gives the Restraint Set a name!");

        ImGui.Text("Restraint Set Description:");
        string descriptiontext = refRestraintSet.Description;
        using (var descColor = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey2))
        {
            if (UiSharedService.InputTextWrapMultiline("##InputSetDesc", ref descriptiontext, 150, 4, width))
            {
                refRestraintSet.Description = descriptiontext;
            }
        }
    }

    private void DrawAppearance(RestraintSet refRestraintSet)
    {
        ItemComboLength = ComboWidth * ImGuiHelpers.GlobalScale;
        var itemSpacing = ImGui.GetStyle().ItemSpacing;

        using (var table2 = ImRaii.Table("RestraintEquipSelection", 2, ImGuiTableFlags.RowBg))
        {
            if (!table2) return;
            // Create the headers for the table
            var width = ItemComboLength + GameIconSize.X + itemSpacing.X;
            // setup the columns
            ImGui.TableSetupColumn("EquipmentSlots", ImGuiTableColumnFlags.WidthFixed, width);
            ImGui.TableSetupColumn("AccessorySlots", ImGuiTableColumnFlags.WidthStretch);

            // draw out the equipment slots
            ImGui.TableNextRow(); ImGui.TableNextColumn();
            foreach (var slot in EquipSlotExtensions.EquipmentSlots)
            {
                refRestraintSet.DrawData[slot].GameItem.DrawIcon(_itemStainHandler.IconData, GameIconSize, slot);
                // if we right click the icon, clear it
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    refRestraintSet.DrawData[slot].GameItem = ItemIdVars.NothingItem(refRestraintSet.DrawData[slot].Slot);
                }
                ImGui.SameLine(0, 6);
                using (var groupDraw = ImRaii.Group())
                {
                    DrawEquip(ref refRestraintSet, refRestraintSet.DrawData[slot].Slot, ItemComboLength);
                }
            }
            // i am dumb and dont know how to place adjustable divider lengths
            ImGui.TableNextColumn();
            //draw out the accessory slots
            foreach (var slot in EquipSlotExtensions.AccessorySlots)
            {
                using (var groupIcon = ImRaii.Group())
                {
                    refRestraintSet.DrawData[slot].GameItem.DrawIcon(_itemStainHandler.IconData, GameIconSize, slot);
                    // if we right click the icon, clear it
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        refRestraintSet.DrawData[slot].GameItem = ItemIdVars.NothingItem(refRestraintSet.DrawData[slot].Slot);
                    }
                }

                ImGui.SameLine(0, 6);
                using (var groupDraw = ImRaii.Group())
                {
                    DrawEquip(ref refRestraintSet, refRestraintSet.DrawData[slot].Slot, ItemComboLength);
                }
            }
        }
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SettingGear, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

        using (var lowerTable = ImRaii.Table("BonusItemDescription", 2, ImGuiTableFlags.None))
        {
            // setup the columns
            var width = ItemComboLength + GameIconSize.X + itemSpacing.X;
            ImGui.TableSetupColumn("BonusItem", ImGuiTableColumnFlags.WidthFixed, width);
            ImGui.TableSetupColumn("DescriptionField", ImGuiTableColumnFlags.WidthStretch);

            // draw out the equipment slots
            ImGui.TableNextRow(); ImGui.TableNextColumn();

            var newWidth = ItemComboLength - _uiShared.GetIconButtonSize(FontAwesomeIcon.EyeSlash).X - ImUtf8.ItemInnerSpacing.X;
            // end of table, now draw the bonus items
            foreach (var slot in BonusExtensions.AllFlags)
            {
                refRestraintSet.BonusDrawData[slot].GameItem.DrawIcon(_itemStainHandler.IconData, GameIconSize, slot);
                ImGui.SameLine(0, 6);
                DrawBonusItem(ref refRestraintSet, refRestraintSet.BonusDrawData[slot].Slot, newWidth);
                ImUtf8.SameLineInner();
                FontAwesomeIcon icon = refRestraintSet.BonusDrawData[slot].IsEnabled ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
                if (_uiShared.IconButton(icon))
                {
                    refRestraintSet.BonusDrawData[slot].IsEnabled = !refRestraintSet.BonusDrawData[slot].IsEnabled;
                }
                UiSharedService.AttachToolTip("Toggles Apply Style of Item." +
                    Environment.NewLine + "EYE Icon (Apply Mode) applies regardless of selected item. (nothing slots make the slot nothing)" +
                    Environment.NewLine + "EYE SLASH Icon (Overlay Mode) means that it only will apply the item if it is NOT an nothing slot.");
            }
            // move to the next column.
            ImGui.TableNextColumn();
            // draw the checkbox options.
            // preset some variables to grab from our config service.
            bool forceHelmet= refRestraintSet.ForceHeadgear;
            bool forceVisor = refRestraintSet.ForceVisor;
            bool applyCustomizations = refRestraintSet.ApplyCustomizations;

            using(ImRaii.Group())
            {
                if (ImGui.Checkbox("Headgear", ref forceHelmet))
                {
                    refRestraintSet.ForceHeadgear = forceHelmet;
                }
                _uiShared.DrawHelpText("Will force your headgear to become visible when the set is applied. (Via Glamourer State)");
                ImGui.SameLine();
                if (ImGui.Checkbox("Visor", ref forceVisor))
                {
                    refRestraintSet.ForceVisor = forceVisor;
                }
                _uiShared.DrawHelpText("Will force your visor to become visible when the set is applied. (Via Glamourer State)");
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.Metadata, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
            
            if(ImGui.Checkbox("Apply Customize", ref applyCustomizations))
            {
                refRestraintSet.ApplyCustomizations = applyCustomizations;
            }
            _uiShared.DrawHelpText("Will apply any stored Customizations from import.");
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ApplyingCustomizations, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);


            ImGui.SameLine();

            _uiShared.BooleanToColoredIcon(!JToken.DeepEquals(refRestraintSet.CustomizeObject, new JObject()));
            UiSharedService.AttachToolTip(refRestraintSet.CustomizeObject != new JObject()
                ? "Customizations are stored for this set."
                : "No Customizations are stored for this set.");

            ImGui.SameLine();
            if (_uiShared.IconButton(FontAwesomeIcon.Ban, disabled: refRestraintSet.CustomizeObject == new JObject()))
            {
                refRestraintSet.CustomizeObject = new JObject();
                refRestraintSet.ParametersObject = new JObject();
                _logger.LogTrace("Customizations Cleared.", LoggerType.Restraints);
            }
            UiSharedService.AttachToolTip("Clears any stored Customizations for this set.");
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ClearingCustomizations, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
        }
    }

    private void DrawMoodlesOptions(RestraintSet refRestraintSet, float cellPaddingY)
    {
        if (LastCreatedCharacterData == null)
        {
            ImGui.TextWrapped("No Character Data Found. Please select a character to edit.");
            return;
        }

        try
        {
            using var table = ImRaii.Table("MoodlesSelections", 2, ImGuiTableFlags.BordersInnerV);
            if (!table) return;

            ImGui.TableSetupColumn("MoodleSelection", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("FinalizedPreviewList", ImGuiTableColumnFlags.WidthFixed, 200f);

            ImGui.TableNextRow(); ImGui.TableNextColumn();

            using (var child = ImRaii.Child("##RestraintMoodleStatusSelection", new(ImGui.GetContentRegionAvail().X - 1f, ImGui.GetContentRegionAvail().Y / 2), false))
            {
                if (!child) return;
                _relatedMoodles.DrawMoodlesStatusesListForItem(refRestraintSet, LastCreatedCharacterData, cellPaddingY, false);
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.MoodlesStatuses, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);


            ImGui.Separator();
            using (var child2 = ImRaii.Child("##RestraintMoodlePresetSelection", -Vector2.One, false))
            {
                if (!child2) return;
                _relatedMoodles.DrawMoodlesStatusesListForItem(refRestraintSet, LastCreatedCharacterData, cellPaddingY, true);
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.MoodlesPresets, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

            ImGui.TableNextColumn();
            // Filter the MoodlesStatuses list to get only the moodles that are in AssociatedMoodles
            var associatedMoodles = LastCreatedCharacterData.MoodlesStatuses
                .Where(moodle => refRestraintSet.AssociatedMoodles.Contains(moodle.GUID))
                .ToList();
            // draw out all the active associated moodles in the restraint set with thier icon beside them.
            using (ImRaii.Group())
            {            
                UiSharedService.ColorText("Moodles Applied with Set:", ImGuiColors.ParsedPink);
                ImGui.Separator();

                foreach (var moodle in associatedMoodles)
                {
                    using (ImRaii.Group())
                    {

                        var currentPos = ImGui.GetCursorPos();
                        if (moodle.IconID != 0 && currentPos != Vector2.Zero)
                        {
                            var statusIcon = _uiShared.GetGameStatusIcon((uint)((uint)moodle.IconID + moodle.Stacks - 1));

                            if (statusIcon is { } wrap)
                            {
                                ImGui.SetCursorPos(currentPos);
                                ImGui.Image(statusIcon.ImGuiHandle, MoodlesService.StatusSize);
                            }
                        }
                        ImGui.SameLine();
                        float shiftAmmount = (MoodlesService.StatusSize.Y - ImGui.GetTextLineHeight()) / 2;
                        ImGui.SetCursorPosY(currentPos.Y + shiftAmmount);
                        ImGui.Text(moodle.Title);
                    }
                }
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AppendedMoodles, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error Drawing Moodles Options for Restraint Set.");
        }
    }

    private void DrawSpatialAudioOptions(RestraintSet refRestraintSet, float cellPaddingY)
    {
        _uiShared.BigText("Select if Restraint Set Uses:\nRopes, Chains, Leather, Latex, ext* here.");
        ImGui.Text("They will then play immersive spatial audio on queue.");
    }


    public enum StimulationDegree { No, Light, Mild, Heavy }

    private void DrawVisibilityAndProperties(RestraintSet refRestraintSet)
    {
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;
        var cellPadding = ImGui.GetStyle().CellPadding;

        // create the draw-table for the selectable and viewport displays
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(5f * _uiShared.GetFontScalerFloat(), 0));
        using (ImRaii.Table($"RestraintHardcoreTraitsVisibility", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV))
        {
            // setup the columns for the table
            ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, 175f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##RightColumn", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextColumn();

            var regionSize = ImGui.GetContentRegionAvail();
            using (var leftChild = ImRaii.Child($"##RestraintHardcoreTraitsVisibilityLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
            {
                float width = ImGui.GetContentRegionAvail().X;
                using (var textAlign = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f)))
                {
                    // show the search filter just above the contacts list to form a nice separation.
                    _userPairListHandler.DrawSearchFilter(width, ImGui.GetStyle().ItemInnerSpacing.X, showButton: false);
                    ImGui.Separator();
                    using (var listChild = ImRaii.Child($"##RestraintHardcoreTraitsPairList", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoScrollbar))
                    {
                        _userPairListHandler.DrawPairListSelectable(width, false, 1);
                    }
                }
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SelectingPair, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);

            ImGui.TableNextColumn();
            // display right half viewport based on the tab selection
            using (var rightChild = ImRaii.Child($"##RestraintHardcoreTraitsVisibilityRight", Vector2.Zero, false))
            {
                DrawTraits(refRestraintSet, cellPadding);
            }
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.GrantingTraits, WardrobeUI.LastWinPos, WardrobeUI.LastWinSize);
        }
    }

    private void DrawTraits(RestraintSet refRestraintSet, Vector2 cellPadding)
    {
        var selectedPairRef = _userPairListHandler.SelectedPair;
        if (selectedPairRef is null)
            return;

        // draw a singular checkbox to grant a view access or remove them.
        bool pairHasAccess = refRestraintSet.SetTraits.ContainsKey(selectedPairRef.UserData.UID);
        FontAwesomeIcon icon = pairHasAccess ? FontAwesomeIcon.UserMinus : FontAwesomeIcon.UserPlus;
        string text = pairHasAccess
            ? "Prevent " + (selectedPairRef.GetNickname() ?? selectedPairRef.UserData.AliasOrUID) + " from interacting with this set."
            : "Allow " + (selectedPairRef.GetNickname() ?? selectedPairRef.UserData.AliasOrUID) + " to interact with this set.";

        if (_uiShared.IconTextButton(icon, text, ImGui.GetContentRegionAvail().X))
        {
            if (pairHasAccess)
            {
                refRestraintSet.SetTraits.Remove(selectedPairRef.UserData.UID);
            }
            else
            {
                refRestraintSet.SetTraits[selectedPairRef.UserData.UID] = new HardcoreTraits();
            }
        }

        ImGui.Separator();

        // if the pair is not in the list, return.
        if (!refRestraintSet.SetTraits.ContainsKey(selectedPairRef.UserData.UID))
            return;

        bool legsBound = refRestraintSet.SetTraits[selectedPairRef.UserData.UID].LegsRestrained;
        bool armsBound = refRestraintSet.SetTraits[selectedPairRef.UserData.UID].ArmsRestrained;
        bool gagged = refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Gagged;
        bool blindfolded = refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Blindfolded;
        bool immobile = refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Immobile;
        bool weighty = refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Weighty;

        if (ImGui.Checkbox("Legs will be restrainted", ref legsBound))
            refRestraintSet.SetTraits[selectedPairRef.UserData.UID].LegsRestrained = legsBound;
        _uiShared.DrawHelpText("Any action which typically involves fast leg movement is restricted");

        if (ImGui.Checkbox("Arms will be restrainted", ref armsBound))
            refRestraintSet.SetTraits[selectedPairRef.UserData.UID].ArmsRestrained = armsBound;
        _uiShared.DrawHelpText("Any action which typically involves fast arm movement is restricted");

        if (ImGui.Checkbox("Gagged", ref gagged))
            refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Gagged = gagged;
        _uiShared.DrawHelpText("Any action requiring speech is restricted");

        if (ImGui.Checkbox("Blindfolded", ref blindfolded))
            refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Blindfolded = blindfolded;
        _uiShared.DrawHelpText("Any actions requiring awareness or sight is restricted");

        if (ImGui.Checkbox("Immobile", ref immobile))
            refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Immobile = immobile;
        _uiShared.DrawHelpText("Player becomes unable to move in this set");

        if (ImGui.Checkbox("Weighty", ref weighty))
            refRestraintSet.SetTraits[selectedPairRef.UserData.UID].Weighty = weighty;
        _uiShared.DrawHelpText("Player is forced to only walk while wearing this restraint");

        _uiShared.DrawCombo("Stimulation Level##" + refRestraintSet.RestraintId + "stimulationLevel", 125f, Enum.GetValues<StimulationLevel>(),
            (name) => name.ToString(), (i) => refRestraintSet.SetTraits[selectedPairRef.UserData.UID].StimulationLevel = i,
            refRestraintSet.SetTraits[selectedPairRef.UserData.UID].StimulationLevel);
        _uiShared.DrawHelpText("Any action requiring focus or concentration has its recast time slower and slower~");
    }

    // space for helper functions below
    public void DrawEquip(ref RestraintSet refRestraintSet, EquipSlot slot, float _comboLength)
    {
        using var id = ImRaii.PushId((int)refRestraintSet.DrawData[slot].Slot);
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        var ItemWidth = _comboLength - _uiShared.GetIconButtonSize(FontAwesomeIcon.EyeSlash).X - ImUtf8.ItemInnerSpacing.X;

        using var group = ImRaii.Group();
        DrawItem(ref refRestraintSet, out var label, right, left, slot, ItemWidth);
        ImUtf8.SameLineInner();
        FontAwesomeIcon icon = refRestraintSet.DrawData[slot].IsEnabled ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
        if (_uiShared.IconButton(icon))
        {
            refRestraintSet.DrawData[slot].IsEnabled = !refRestraintSet.DrawData[slot].IsEnabled;
        }
        UiSharedService.AttachToolTip("Toggles Apply Style of Item." +
            Environment.NewLine + "EYE Icon (Apply Mode) applies regardless of selected item. (nothing slots make the slot nothing)" +
            Environment.NewLine + "EYE SLASH Icon (Overlay Mode) means that it only will apply the item if it is NOT an nothing slot.");
        DrawStain(ref refRestraintSet, slot, _comboLength);
    }

    private void DrawItem(ref RestraintSet refRestraintSet, out string label, bool clear, bool open, EquipSlot slot, float width)
    {
        // draw the item combo.
        var combo = ItemCombos[refRestraintSet.DrawData[slot].Slot.ToIndex()];
        label = combo.Label;
        if (open)
        {
            GenericHelpers.OpenCombo($"##WardrobeCreateNewSetItem-{slot}");
            _logger.LogTrace($"{combo.Label} Toggled");
        }
        // draw the combo
        var change = combo.Draw(refRestraintSet.DrawData[slot].GameItem.Name,
            refRestraintSet.DrawData[slot].GameItem.ItemId, width, ComboWidth * 1.3f);

        // if we changed something
        if (change && !refRestraintSet.DrawData[slot].GameItem.Equals(combo.CurrentSelection))
        {
            // log full details.
            _logger.LogTrace($"Item changed from {combo.CurrentSelection} [{combo.CurrentSelection.ItemId}] " +
                $"to {refRestraintSet.DrawData[slot].GameItem} [{refRestraintSet.DrawData[slot].GameItem.ItemId}]");
            // update the item to the new selection.
            refRestraintSet.DrawData[slot].GameItem = combo.CurrentSelection;
        }

        // if we right clicked
        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            // if we right click the item, clear it.
            _logger.LogTrace($"Item changed to {ItemIdVars.NothingItem(refRestraintSet.DrawData[slot].Slot)} " +
                $"[{ItemIdVars.NothingItem(refRestraintSet.DrawData[slot].Slot).ItemId}] " +
                $"from {refRestraintSet.DrawData[slot].GameItem} [{refRestraintSet.DrawData[slot].GameItem.ItemId}]");
            // clear the item.
            refRestraintSet.DrawData[slot].GameItem = ItemIdVars.NothingItem(refRestraintSet.DrawData[slot].Slot);
        }
    }

    private void DrawBonusItem(ref RestraintSet refRestraintSet, BonusItemFlag flag, float width)
    {
        using var id = ImRaii.PushId((int)refRestraintSet.BonusDrawData[flag].Slot);
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        bool clear = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        bool open = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        // Assuming _bonusItemCombo is similar to ItemCombos but for bonus items
        var combo = BonusItemCombos[refRestraintSet.BonusDrawData[flag].Slot.ToIndex()];

        if (open)
            ImGui.OpenPopup($"##{combo.Label}");

        var change = combo.Draw(refRestraintSet.BonusDrawData[flag].GameItem.Name,
            refRestraintSet.BonusDrawData[flag].GameItem.Id.BonusItem,
            width, ComboWidth * 1.3f);

        if (change && !refRestraintSet.BonusDrawData[flag].GameItem.Equals(combo.CurrentSelection))
        {
            // log full details.
            _logger.LogTrace($"Item changed from {combo.CurrentSelection} [{combo.CurrentSelection.PrimaryId}] " +
                $"to {refRestraintSet.BonusDrawData[flag].GameItem} [{refRestraintSet.BonusDrawData[flag].GameItem.PrimaryId}]");
            // change
            refRestraintSet.BonusDrawData[flag].GameItem = combo.CurrentSelection;
        }

        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            // Assuming a method to handle item reset or clear, similar to your DrawItem method
            _logger.LogTrace($"Item reset to default for slot {flag}");
            refRestraintSet.BonusDrawData[flag].GameItem = EquipItem.BonusItemNothing(flag);
        }
    }

    private void DrawStain(ref RestraintSet refRestraintSet, EquipSlot slot, float width)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X *
            (refRestraintSet.DrawData[slot].GameStain.Count - 1)) / refRestraintSet.DrawData[slot].GameStain.Count;

        // draw the stain combo for each of the 2 dyes (or just one)
        foreach (var (stainId, index) in refRestraintSet.DrawData[slot].GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = _itemStainHandler.TryGetStain(stainId, out var stain);
            // draw the stain combo.
            var change = StainColorCombos.Draw($"##stain{refRestraintSet.DrawData[slot].Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, widthStains);
            if (index < refRestraintSet.DrawData[slot].GameStain.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one if there are two stains

            // if we had a change made, update the stain data.
            if (change)
            {
                if (_itemStainHandler.TryGetStain(StainColorCombos.CurrentSelection.Key, out stain))
                {
                    // if changed, change it.
                    refRestraintSet.DrawData[slot].GameStain = refRestraintSet.DrawData[slot].GameStain.With(index, stain.RowIndex);
                }
                else if (StainColorCombos.CurrentSelection.Key == Stain.None.RowIndex)
                {
                    // if set to none, reset it to default
                    refRestraintSet.DrawData[slot].GameStain = refRestraintSet.DrawData[slot].GameStain.With(index, Stain.None.RowIndex);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // reset the stain to default
                refRestraintSet.DrawData[slot].GameStain = refRestraintSet.DrawData[slot].GameStain.With(index, Stain.None.RowIndex);
            }
        }
    }
}
