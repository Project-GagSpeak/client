using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.UI.Components.Combos;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.UI.Permissions;

/// <summary>
/// Contains functions relative to the paired users permissions for the client user.
/// 
/// Yes its messy, yet it's long, but i functionalized it best i could for the insane 
/// amount of logic being performed without adding too much overhead.
/// </summary>
public partial class PairStickyUI
{
    private void DrawMoodlesActions()
    {
        var lastIpcData = StickyPair.LastIpcData;
        var pairUniquePerms = StickyPair.PairPerms;

        var ApplyPairsMoodleToPairDisabled = !pairUniquePerms.MoodlePerms.HasFlag.PairCanApplyYourMoodlesToYou || lastIpcData.MoodlesStatuses.Count <= 0;
        var ApplyOwnMoodleToPairDisabled = !pairUniquePerms.PairCanApplyOwnMoodlesToYou || LastCreatedCharacterData == null || LastCreatedCharacterData.MoodlesStatuses.Count <= 0;
        var RemovePairsMoodlesDisabled = !pairUniquePerms.AllowRemovingMoodles || lastIpcData.MoodlesDataStatuses.Count <= 0;
        var ClearPairsMoodlesDisabled = !pairUniquePerms.AllowRemovingMoodles || lastIpcData.MoodlesData == string.Empty;

        ////////// APPLY MOODLES FROM PAIR's LIST //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.PersonCirclePlus, "Apply a Moodle from their list", WindowMenuWidth, true, ApplyPairsMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyPairMoodle ? InteractionType.None : InteractionType.ApplyPairMoodle;
        }
        UiSharedService.AttachToolTip("Applies a Moodle from " + StickyPair.UserData.AliasOrUID + "'s Moodles List to them.");
        if (PairCombos.Opened is InteractionType.ApplyPairMoodle)
        {
            using (var child = ImRaii.Child("ApplyPairMoodlesChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsPairStatusList" + PairUID) ?? Guid.Empty;

                _moodlesService.DrawMoodleStatusComboButton("##PermissionActionsPairStatusList" + PairUID, "Apply",
                ImGui.GetContentRegionAvail().X,
                lastIpcData.MoodlesStatuses,
                selectedItem == Guid.Empty,
                (onSelected) => { _logger.LogDebug("Selected Moodle: " + onSelected, LoggerType.Permissions); },
                (onButtonPress) =>
                {
                    if (onButtonPress is null) return;
                    // make sure that the moodles statuses contains the selected guid.
                    if (!lastIpcData.MoodlesStatuses.Any(x => x.GUID == onButtonPress)) return;

                    var statusInfo = new List<MoodlesStatusInfo> { lastIpcData.MoodlesStatuses.First(x => x.GUID == onButtonPress) };
                    if (!_moodlesService.ValidatePermissionForApplication(StickyPair.PairPerms, statusInfo)) return;

                    _ = _hub.UserApplyMoodlesByGuid(new(StickyPair.UserData, new List<Guid> { onButtonPress.Value }, IpcToggleType.MoodlesStatus));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM PAIR's LIST //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.FileCirclePlus, "Apply a Preset from their list", WindowMenuWidth, true, ApplyPairsMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyPairMoodlePreset ? InteractionType.None : InteractionType.ApplyPairMoodlePreset;
        }
        UiSharedService.AttachToolTip("Applies a Preset from " + PairUID + "'s Presets List to them.");
        if (PairCombos.Opened is InteractionType.ApplyPairMoodlePreset)
        {
            using (var child = ImRaii.Child("ApplyPairPresetsChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsPairPresetList" + PairUID) ?? Guid.Empty;

                _moodlesService.DrawMoodlesPresetComboButton("##PermissionActionsPairPresetList" + PairUID, "Apply",
                ImGui.GetContentRegionAvail().X,
                lastIpcData.MoodlesPresets, lastIpcData.MoodlesStatuses, selectedItem == Guid.Empty,
                (onSelected) => { _logger.LogDebug("Selected Preset: " + onSelected, LoggerType.Permissions); },
                (onButtonPress) =>
                {
                    // ensure its a valid status
                    var presetSelected = lastIpcData.MoodlesPresets.FirstOrDefault(x => x.GUID == onButtonPress);
                    if (presetSelected.Item1 != onButtonPress) return;

                    var statusesToApply = lastIpcData.MoodlesStatuses.Where(x => presetSelected.Item2.Contains(x.GUID)).ToList();

                    if (!_moodlesService.ValidatePermissionForApplication(pairUniquePerms, statusesToApply)) return;

                    _ = _hub.UserApplyMoodlesByGuid(new(StickyPair.UserData, statusesToApply.Select(s => s.GUID).ToList(), IpcToggleType.MoodlesPreset));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }

        ////////// APPLY MOODLES FROM OWN LIST //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.UserPlus, "Apply a Moodle from your list", WindowMenuWidth, true, ApplyOwnMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyOwnMoodle ? InteractionType.None : InteractionType.ApplyOwnMoodle;
        }
        UiSharedService.AttachToolTip("Applies a Moodle from your Moodles List to " + PairUID + ".");
        if (PairCombos.Opened is InteractionType.ApplyOwnMoodle)
        {
            using (var child = ImRaii.Child("ApplyOwnMoodlesChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                if (LastCreatedCharacterData is null) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsOwnStatusList" + PairUID) ?? Guid.Empty;

                _moodlesService.DrawMoodleStatusComboButton("##PermissionActionsOwnStatusList" + PairUID, "Apply",
                ImGui.GetContentRegionAvail().X,
                LastCreatedCharacterData.MoodlesStatuses,
                selectedItem == Guid.Empty,
                (onSelected) => { _logger.LogDebug("Selected Moodle: " + onSelected, LoggerType.Permissions); },
                (onButtonPress) =>
                {
                    if (onButtonPress is null) return;
                    // make sure that the moodles statuses contains the selected guid.
                    if (!LastCreatedCharacterData.MoodlesStatuses.Any(x => x.GUID == onButtonPress)) return;

                    var statusInfo = new List<MoodlesStatusInfo> { LastCreatedCharacterData.MoodlesStatuses.First(x => x.GUID == onButtonPress) };
                    if (!_moodlesService.ValidatePermissionForApplication(pairUniquePerms, statusInfo)) return;

                    _logger.LogInformation("Applying Moodle: " + onButtonPress + " to " + PairNickOrAliasOrUID);
                    _ = _hub.UserApplyMoodlesByStatus(new(StickyPair.UserData, statusInfo, IpcToggleType.MoodlesStatus));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM OWN LIST //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.FileCirclePlus, "Apply a Preset from your list", WindowMenuWidth, true, ApplyOwnMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyOwnMoodlePreset ? InteractionType.None : InteractionType.ApplyOwnMoodlePreset;
        }
        UiSharedService.AttachToolTip("Applies a Preset from your Presets List to " + PairUID + ".");

        if (PairCombos.Opened is InteractionType.ApplyOwnMoodlePreset)
        {
            using (var child = ImRaii.Child("ApplyOwnPresetsChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                if (LastCreatedCharacterData is null) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsOwnPresetList" + PairUID) ?? Guid.Empty;

                _moodlesService.DrawMoodlesPresetComboButton("##PermissionActionsOwnPresetList" + PairUID, "Apply",
                ImGui.GetContentRegionAvail().X,
                LastCreatedCharacterData.MoodlesPresets, LastCreatedCharacterData.MoodlesStatuses, selectedItem == Guid.Empty,
                (onSelected) => { _logger.LogDebug("Selected Preset: " + onSelected, LoggerType.Permissions); },
                (onButtonPress) =>
                {
                    // ensure its a valid status
                    if (!LastCreatedCharacterData.MoodlesPresets.Any(x => x.Item1 == onButtonPress)) return;

                    var selectedPreset = LastCreatedCharacterData.MoodlesPresets.First(x => x.Item1 == onButtonPress);
                    var statusesToApply = LastCreatedCharacterData.MoodlesStatuses
                        .Where(x => selectedPreset.Item2.Contains(x.GUID))
                        .ToList();

                    if (!_moodlesService.ValidatePermissionForApplication(pairUniquePerms, statusesToApply)) return;

                    _ = _hub.UserApplyMoodlesByStatus(new(StickyPair.UserData, statusesToApply, IpcToggleType.MoodlesPreset));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }


        ////////// REMOVE MOODLES //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.UserMinus, "Remove a Moodle from " + PairNickOrAliasOrUID, WindowMenuWidth, true, RemovePairsMoodlesDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.RemoveMoodle ? InteractionType.None : InteractionType.RemoveMoodle;
        }
        UiSharedService.AttachToolTip("Removes a Moodle from " + PairNickOrAliasOrUID + "'s Statuses.");
        if (PairCombos.Opened is InteractionType.RemoveMoodle)
        {
            using (var child = ImRaii.Child("RemoveMoodles", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsRemoveMoodle" + PairUID) ?? Guid.Empty;

                _moodlesService.DrawMoodleStatusComboButton("##PermissionActionsRemoveMoodle" + PairUID, "Remove",
                ImGui.GetContentRegionAvail().X,
                lastIpcData.MoodlesDataStatuses, selectedItem == Guid.Empty,
                (onSelected) => { _logger.LogDebug("Selected Moodle to remove: " + onSelected); },
                (onButtonPress) =>
                {
                    if (onButtonPress is null)
                        return;
                    // ensure its a valid status
                    if (!lastIpcData.MoodlesDataStatuses.Any(x => x.GUID == onButtonPress))
                        return;
                    var statusInfo = new List<MoodlesStatusInfo> { lastIpcData.MoodlesDataStatuses.First(x => x.GUID == onButtonPress) };
                    if (!_moodlesService.ValidatePermissionForApplication(pairUniquePerms, statusInfo))
                        return;

                    _logger.LogInformation("Removing Moodle: " + onButtonPress + " from " + PairNickOrAliasOrUID);
                    _ = _hub.UserRemoveMoodles(new(StickyPair.UserData, new List<Guid> { onButtonPress.Value }));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }

        ////////// CLEAR MOODLES //////////
        if (_uiShared.IconTextButton(FontAwesomeIcon.UserSlash, "Clear all Moodles from " + PairNickOrAliasOrUID, WindowMenuWidth, true, ClearPairsMoodlesDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ClearMoodle ? InteractionType.None : InteractionType.ClearMoodle;
        }
        UiSharedService.AttachToolTip("Clears all Moodles from " + PairNickOrAliasOrUID + "'s Statuses.");

        if (PairCombos.Opened is InteractionType.ClearMoodle)
        {
            using (var child = ImRaii.Child("ClearMoodles", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                ImGui.SetNextItemWidth(WindowMenuWidth);
                if (ImGuiUtil.DrawDisabledButton("Clear All Active Moodles##ClearStatus" + PairUID, new Vector2(), string.Empty, !(KeyMonitor.ShiftPressed() && KeyMonitor.CtrlPressed())))
                {
                    _logger.LogInformation("Clearing all Moodles from " + PairNickOrAliasOrUID);
                    _ = _hub.UserClearMoodles(new(StickyPair.UserData));
                    PairCombos.Opened = InteractionType.None;
                }
                UiSharedService.AttachToolTip("Clear all statuses from " + PairNickOrAliasOrUID
                    + "--SEP--Must be holding SHIFT & CTRL to fire!");
            }
        }
        ImGui.Separator();
    }
}
