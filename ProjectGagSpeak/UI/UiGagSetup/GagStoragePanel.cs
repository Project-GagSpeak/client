using Dalamud.Interface;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop;
using GagSpeak.Interop.Ipc;
using GagSpeak.Interop.IpcHelpers.Moodles;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Numerics;

namespace GagSpeak.UI.Tabs.WardrobeTab;
/// <summary> This class is used to handle the ConfigSettings Tab. </summary>
public class GagStoragePanel : DisposableMediatorSubscriberBase
{
    private const float ComboWidth = 225f;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientData _playerManager;
    private readonly MoodlesAssociations _relatedMoodles;
    private readonly GameItemStainHandler _itemStainHandler;
    private readonly UiSharedService _uiShared;
    private readonly TutorialService _guides;
    public GagStoragePanel(ILogger<GagStoragePanel> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientData playerManager,
        GameItemStainHandler itemStainHandler, MoodlesAssociations relatedMoodles, 
        UiSharedService uiShared, TutorialService guides) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerManager = playerManager;
        _relatedMoodles = relatedMoodles;
        _itemStainHandler = itemStainHandler;
        _uiShared = uiShared;
        _guides = guides;
        GameItemCombo = _itemStainHandler.ObtainItemCombos();
        StainCombo = _itemStainHandler.ObtainStainCombos(ComboWidth);

        Mediator.Subscribe<IpcDataCreatedMessage>(this, (msg) => LastCreatedCharacterData = msg.CharaIPCData);
    }

    // Info related to the person we are inspecting.
    private CharaIPCData LastCreatedCharacterData = null!;
    
    private Vector2 IconSize;
    private float ComboLength;
    private readonly GameItemCombo[] GameItemCombo;
    private readonly StainColorCombo StainCombo;

    private GagDrawData UnsavedDrawData = null!;
    private GagType SelectedGag = GagType.BallGag;

    private void DrawGagStorageHeader(Vector2 winPos, Vector2 winSize)
    {
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f);
        var startYpos = ImGui.GetCursorPosY();
        Vector2 textSize;
        using (_uiShared.UidFont.Push()) { textSize = ImGui.CalcTextSize("Select Gag Type"); }
        var saveSize = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Save, "Save");
        var centerYpos = (textSize.Y - ImGui.GetFrameHeight());

        using (ImRaii.Child("MoodlesManagerHeader", new Vector2(UiSharedService.GetWindowContentRegionWidth(), ImGui.GetFrameHeight() + (centerYpos - startYpos) * 2)))
        {
            // now next to it we need to draw the header text
            ImGui.SameLine(ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(startYpos);
            using (_uiShared.UidFont.Push())
            {
                UiSharedService.ColorText("Select Gag Type", ImGuiColors.ParsedPink);
            }

            // now calculate it so that the cursors Yposition centers the button in the middle height of the text
            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - saveSize - 175f - ImGui.GetStyle().ItemSpacing.X * 2);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            _uiShared.DrawComboSearchable("GagStorage Gag Type", 175f, Enum.GetValues<GagType>().Where(gag => gag != GagType.None), 
                (gag) => gag.GagName(), false, (i) =>
            {
                // grab the new gag info.
                SelectedGag = i;
                UnsavedDrawData = _clientConfigs.GetDrawData(SelectedGag).DeepCloneData();
            }, SelectedGag);
            _guides.OpenTutorial(TutorialType.GagStorage, StepsGagStorage.GagStorageSelection, winPos, winSize);

            ImGui.SameLine(ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - saveSize - ImGui.GetStyle().ItemSpacing.X);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerYpos);
            var currentYpos = ImGui.GetCursorPosY();
            // draw revert button at the same location but right below that button
            if (_uiShared.IconTextButton(FontAwesomeIcon.Save, "Save"))
            {
                _clientConfigs.UpdateGagItem(SelectedGag, UnsavedDrawData!);
                _lastSaveTime = DateTime.UtcNow;
            }
            UiSharedService.AttachToolTip("View the list of Moodles Statuses");
            _guides.OpenTutorial(TutorialType.GagStorage, StepsGagStorage.SavingCustomizations, winPos, winSize);
        }
    }

    public void DrawGagStoragePanel(Vector2 winPos, Vector2 winSize)
    {
        DrawGagStorageHeader(winPos, winSize);
        ImGui.Separator();
        var cellPadding = ImGui.GetStyle().CellPadding;

        if (UnsavedDrawData == null)
        {
            SelectedGag = GagType.BallGag;
            UnsavedDrawData = _clientConfigs.GetDrawData(SelectedGag);
        }

        if (ImGui.BeginTabBar("GagStorageEditor"))
        {
            if (ImGui.BeginTabItem("Gag Glamour"))
            {
                DrawGagGlamour(winPos, winSize);
                ImGui.EndTabItem();
            }
            using (ImRaii.Disabled(!IpcCallerMoodles.APIAvailable))
            {
                if (ImGui.BeginTabItem("Moodles"))
                {
                    DrawGagMoodles(cellPadding.Y);
                    ImGui.EndTabItem();
                }
            }
            _guides.OpenTutorial(TutorialType.GagStorage, StepsGagStorage.Moodles, winPos, winSize);

            if (ImGui.BeginTabItem("Audio"))
            {
                _uiShared.BigText("Audio WIP");
                ImGui.EndTabItem();
            }
            _guides.OpenTutorial(TutorialType.GagStorage, StepsGagStorage.Audio, winPos, winSize);

            if (DateTime.UtcNow - _lastSaveTime < TimeSpan.FromSeconds(3))
            {
                using (ImRaii.Disabled())
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0, 1, 0, 1)))
                    {
                        if (ImGui.BeginTabItem("GagData Saved Successfully!")) { ImGui.EndTabItem(); }
                    }
                }
            }

            ImGui.EndTabBar();
        }
    }

    DateTime _lastSaveTime = DateTime.MinValue;
    private void DrawSavedNotification()
    {
        using var tabBar = ImRaii.TabBar("GagStorageEditor");
        if (tabBar)
        {
            var gagSaved = ImRaii.TabItem("Saved");
            if (gagSaved)
            {
                ImGui.Text("Data Saved");
            }
            gagSaved.Dispose();
        }
    }


    private void DrawGagMoodles(float cellPaddingY)
    {
        if (!IpcCallerMoodles.APIAvailable)
        {
            UiSharedService.ColorText("Moodles not Enabled. Please enable to view Moodles options.", ImGuiColors.DalamudRed);
            return;
        }
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
                _relatedMoodles.DrawMoodlesStatusesListForItem(UnsavedDrawData, LastCreatedCharacterData, cellPaddingY, false);
            }
            ImGui.Separator();
            using (var child2 = ImRaii.Child("##RestraintMoodlePresetSelection", -Vector2.One, false))
            {
                if (!child2) return;
                _relatedMoodles.DrawMoodlesStatusesListForItem(UnsavedDrawData, LastCreatedCharacterData, cellPaddingY, true);
            }


            ImGui.TableNextColumn();
            // Filter the MoodlesStatuses list to get only the moodles that are in AssociatedMoodles
            var associatedMoodles = LastCreatedCharacterData.MoodlesStatuses
                .Where(moodle => UnsavedDrawData.AssociatedMoodles.Contains(moodle.GUID))
                .ToList();
            // draw out all the active associated moodles in the restraint set with thier icon beside them.
            UiSharedService.ColorText("Moodles Applied with Set:", ImGuiColors.ParsedPink);
            ImGui.Separator();
            foreach (var moodle in associatedMoodles)
            {
                using (var group = ImRaii.Group())
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
        catch (Exception e)
        {
            Logger.LogError(e, "Error Drawing Moodles Options for Selected Gag.");
        }
    }

    private void DrawGagAudio()
    {

    }

    private void DrawGagGlamour(Vector2 winPos, Vector2 winSize)
    {
        // define icon size and combo length
        IconSize = new Vector2(3 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y * 2);
        ComboLength = ComboWidth * ImGuiHelpers.GlobalScale;

        // on the new line, lets draw out a group, containing the image, and the slot, item, and stain listings.
        using (var gagStorage = ImRaii.Group())
        {
            try
            {
                UnsavedDrawData.GameItem.DrawIcon(_itemStainHandler.IconData, IconSize, UnsavedDrawData.Slot);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    Logger.LogTrace($"Item changed to {ItemIdVars.NothingItem(UnsavedDrawData.Slot)} [{ItemIdVars.NothingItem(UnsavedDrawData.Slot).ItemId}] " +
                        $"from {UnsavedDrawData.GameItem} [{UnsavedDrawData.GameItem.ItemId}]");
                    UnsavedDrawData.GameItem = ItemIdVars.NothingItem(UnsavedDrawData.Slot);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to draw gag icon.");
            }
            // right beside it, draw a secondary group of 3
            ImGui.SameLine(0, 6);
            using (var group = ImRaii.Group())
            {
                // display the wardrobe slot for this gag
                var refValue = Array.IndexOf(EquipSlotExtensions.EqdpSlots.ToArray(), UnsavedDrawData.Slot);
                ImGui.SetNextItemWidth(ComboLength);
                if (ImGui.Combo(" Equipment Slot##WardrobeEquipSlot", ref refValue,
                    EquipSlotExtensions.EqdpSlots.Select(slot => slot.ToName()).ToArray(), EquipSlotExtensions.EqdpSlots.Count))
                {
                    // Update the selected slot when the combo box selection changes
                    UnsavedDrawData.Slot = EquipSlotExtensions.EqdpSlots[refValue];
                    // reset display and/or selected item to none.
                    UnsavedDrawData.GameItem = ItemIdVars.NothingItem(UnsavedDrawData.Slot);
                }

                DrawEquip(GameItemCombo, StainCombo, ComboLength);
            }
        }
        _guides.OpenTutorial(TutorialType.GagStorage, StepsGagStorage.GagGlamours, winPos, winSize);

        ImGui.Separator();
        _uiShared.BigText("Adjustments");

        using (ImRaii.Group())
        {
            var refEnabled = UnsavedDrawData!.IsEnabled;
            if (ImGui.Checkbox("Enable " + SelectedGag.GagName(), ref refEnabled))
            {
                UnsavedDrawData.IsEnabled = refEnabled;
                Logger.LogTrace($"Gag {SelectedGag.GagName()} is now {(UnsavedDrawData.IsEnabled ? "enabled" : "disabled")}");
            }
            _uiShared.DrawHelpText("When enabled, allows Item-AutoEquip to function with this Gag." + Environment.NewLine
                + "When disabled, this Gag Glamour will not be auto equipped, even with Item Auto-Equip on.");

            var refHelmetForced = UnsavedDrawData.ForceHeadgear;
            if (ImGui.Checkbox($"Force Headgear", ref refHelmetForced))
            {
                UnsavedDrawData.ForceHeadgear = refHelmetForced;
                Logger.LogTrace($"Gag {SelectedGag.GagName()} will now {(UnsavedDrawData.ForceHeadgear ? "force headgear on" : "not force headgear on")} when enabled");
            }
            _uiShared.DrawHelpText("When enabled, your [Hat Visible] property in Glamourer will be set to enabled. Making headgear visible.");

            var refVisorForced = UnsavedDrawData.ForceVisor;
            if (ImGui.Checkbox($"Force Visor", ref refVisorForced))
            {
                UnsavedDrawData.ForceVisor = refVisorForced;
                Logger.LogTrace($"Gag {SelectedGag.GagName()} will now {(UnsavedDrawData.ForceVisor ? "force visor on" : "not force visor on")} when enabled");
            }
            _uiShared.DrawHelpText("When enabled, your [Visor Visible] property in Glamourer will be set to enabled. Making visor visible.");

            if (IpcCallerCustomize.APIAvailable)
            {
                ImGui.Spacing();
                // collect the list of profiles from our last received IPC data.
                var profiles = _playerManager.CustomizeProfiles;

                _uiShared.DrawComboSearchable("C+ Profile##GagStorageCP_Profile" + SelectedGag, 150f, profiles, (profile) => profile.ProfileName, true, 
                    (i) =>
                    {
                        UnsavedDrawData.CustomizeGuid = i.ProfileGuid;
                        Logger.LogTrace($"Gag {SelectedGag.GagName()} will now use the Customize+ Profile {i.ProfileName}");
                    }, profiles.FirstOrDefault(p => p.ProfileGuid == UnsavedDrawData.CustomizeGuid), "No Profiles Selected");
                _uiShared.DrawHelpText("Select a Customize+ Profile to apply while this Gag is equipped.");

                int priorityRef = (int)UnsavedDrawData.CustomizePriority;
                ImGui.SetNextItemWidth(150f);
                if (ImGui.InputInt("C+ Priority##GagStorageCP_Priority" + SelectedGag, ref priorityRef))
                {
                    UnsavedDrawData.CustomizePriority = (byte)priorityRef;
                }
                _uiShared.DrawHelpText("Set the priority of this profile. Higher numbers will override lower numbers.");
            }
        }
        _guides.OpenTutorial(TutorialType.GagStorage, StepsGagStorage.Adjustements, winPos, winSize);
    }

    public void DrawEquip(GameItemCombo[] _gameItemCombo, StainColorCombo _stainCombo, float _comboLength)
    {
        using var id = ImRaii.PushId((int)UnsavedDrawData.Slot);
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        using var group = ImRaii.Group();
        DrawItem(out var label, right, left, _comboLength, _gameItemCombo);
        DrawStain(_comboLength, _stainCombo);
    }

    private void DrawItem(out string label, bool clear, bool open, float width,
    GameItemCombo[] _gameItemCombo)
    {
        // draw the item combo.
        var combo = _gameItemCombo[UnsavedDrawData.Slot.ToIndex()];
        label = combo.Label;
        if (open)
        {
            GenericHelpers.OpenCombo($"##GagShelfItem{SelectedGag}{combo.Label}");
            Logger.LogTrace($"{combo.Label} Toggled");
        }
        // draw the combo
        var change = combo.Draw(UnsavedDrawData.GameItem.Name, UnsavedDrawData.GameItem.ItemId, width, ComboWidth, " Gag Glamour Item");

        // conditionals to detect for changes in the combo's
        if (change && !UnsavedDrawData.GameItem.Equals(combo.CurrentSelection))
        {
            // log full details.
            Logger.LogTrace($"Item changed from {combo.CurrentSelection} [{combo.CurrentSelection.ItemId}] " +
                $"to {UnsavedDrawData.GameItem} [{UnsavedDrawData.GameItem.ItemId}]");
            UnsavedDrawData.GameItem = combo.CurrentSelection;
        }

        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            Logger.LogTrace($"Item changed to {ItemIdVars.NothingItem(UnsavedDrawData.Slot)} [{ItemIdVars.NothingItem(UnsavedDrawData.Slot).ItemId}] " +
                $"from {UnsavedDrawData.GameItem} [{UnsavedDrawData.GameItem.ItemId}]");
            UnsavedDrawData.GameItem = ItemIdVars.NothingItem(UnsavedDrawData.Slot);
        }
    }

    private void DrawStain(float width, StainColorCombo _stainCombo)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (UnsavedDrawData.GameStain.Count - 1)) / UnsavedDrawData.GameStain.Count;

        foreach (var (stainId, index) in UnsavedDrawData.GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = _itemStainHandler.TryGetStain(stainId, out var stain);
            // draw the stain combo.
            var change = _stainCombo.Draw($"##stain{UnsavedDrawData.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, widthStains);
            if (index < UnsavedDrawData.GameStain.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one.

            // if we had a change made, update the stain data.
            if (change)
            {
                if (_itemStainHandler.TryGetStain(_stainCombo.CurrentSelection.Key, out stain))
                {
                    // if changed, change it.
                    UnsavedDrawData.GameStain = UnsavedDrawData.GameStain.With(index, stain.RowIndex);
                }
                else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
                {
                    // if set to none, reset it to default
                    UnsavedDrawData.GameStain = UnsavedDrawData.GameStain.With(index, Stain.None.RowIndex);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // reset the stain to default
                UnsavedDrawData.GameStain = UnsavedDrawData.GameStain.With(index, Stain.None.RowIndex);
            }
        }
        ImUtf8.SameLineInner();
        ImGui.Text(" Applied Dyes");
    }
}
