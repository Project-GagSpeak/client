using GagSpeak.PlayerState.Visual;

namespace GagSpeak.CkCommons.Gui.Permissions;

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
        var lastIpcData = SPair.LastIpcData;
        var pairUniquePerms = SPair.PairPerms;

        var ApplyPairsMoodleToPairDisabled = !pairUniquePerms.MoodlePerms.HasFlag(MoodlePerms.PairCanApplyYourMoodlesToYou) || lastIpcData.MoodlesStatuses.Count <= 0;
        var ApplyOwnMoodleToPairDisabled = !pairUniquePerms.MoodlePerms.HasFlag(MoodlePerms.PairCanApplyTheirMoodlesToYou) || VisualApplierMoodles.LatestIpcData is null || VisualApplierMoodles.LatestIpcData.MoodlesStatuses.Count <= 0;
        var RemovePairsMoodlesDisabled = !pairUniquePerms.MoodlePerms.HasFlag(MoodlePerms.RemovingMoodles) || lastIpcData.MoodlesDataStatuses.Count <= 0;
/*
        ////////// APPLY MOODLES FROM PAIR's LIST //////////
        if (CkGui.IconTextButton(FAI.PersonCirclePlus, "Apply a Moodle from their list", WindowMenuWidth, true, ApplyPairsMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyPairMoodle ? InteractionType.None : InteractionType.ApplyPairMoodle;
        }
        CkGui.AttachToolTip("Applies a Moodle from " + SPair.UserData.AliasOrUID + "'s Moodles List to them.");
        if (PairCombos.Opened is InteractionType.ApplyPairMoodle)
        {
            using (var child = ImRaii.Child("ApplyPairMoodlesChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsPairStatusList" + PermissionData.DispName) ?? Guid.Empty;

                _moodlesService.DrawMoodleStatusComboButton("##PermissionActionsPairStatusList" + PermissionData.DispName, "Apply",
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
                    if (!_moodlesService.ValidatePermissionForApplication(SPair.PairPerms, statusInfo)) return;

                    _ = _hub.UserApplyMoodlesByGuid(new(SPair.UserData, new List<Guid> { onButtonPress.Value }, IpcToggleType.MoodlesStatus));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM PAIR's LIST //////////
        if (CkGui.IconTextButton(FAI.FileCirclePlus, "Apply a Preset from their list", WindowMenuWidth, true, ApplyPairsMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyPairMoodlePreset ? InteractionType.None : InteractionType.ApplyPairMoodlePreset;
        }
        CkGui.AttachToolTip("Applies a Preset from " + PermissionData.DispName + "'s Presets List to them.");
        if (PairCombos.Opened is InteractionType.ApplyPairMoodlePreset)
        {
            using (var child = ImRaii.Child("ApplyPairPresetsChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsPairPresetList" + PermissionData.DispName) ?? Guid.Empty;

                _moodlesService.DrawMoodlesPresetComboButton("##PermissionActionsPairPresetList" + PermissionData.DispName, "Apply",
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

                    _ = _hub.UserApplyMoodlesByGuid(new(SPair.UserData, statusesToApply.Select(s => s.GUID).ToList(), IpcToggleType.MoodlesPreset));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }

        ////////// APPLY MOODLES FROM OWN LIST //////////
        if (CkGui.IconTextButton(FAI.UserPlus, "Apply a Moodle from your list", WindowMenuWidth, true, ApplyOwnMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyOwnMoodle ? InteractionType.None : InteractionType.ApplyOwnMoodle;
        }
        CkGui.AttachToolTip("Applies a Moodle from your Moodles List to " + PermissionData.DispName + ".");
        if (PairCombos.Opened is InteractionType.ApplyOwnMoodle)
        {
            using (var child = ImRaii.Child("ApplyOwnMoodlesChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                if (!child) return;

                if (LastCreatedCharacterData is null) return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsOwnStatusList" + PermissionData.DispName) ?? Guid.Empty;

                _moodlesService.DrawMoodleStatusComboButton("##PermissionActionsOwnStatusList" + PermissionData.DispName, "Apply",
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

                    _logger.LogInformation("Applying Moodle: " + onButtonPress + " to " + PermissionData.DispName);
                    _ = _hub.UserApplyMoodlesByStatus(new(SPair.UserData, statusInfo, IpcToggleType.MoodlesStatus));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM OWN LIST //////////
        if (CkGui.IconTextButton(FAI.FileCirclePlus, "Apply a Preset from your list", WindowMenuWidth, true, ApplyOwnMoodleToPairDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.ApplyOwnMoodlePreset ? InteractionType.None : InteractionType.ApplyOwnMoodlePreset;
        }
        CkGui.AttachToolTip("Applies a Preset from your Presets List to " + PermissionData.DispName + ".");

        if (PairCombos.Opened is InteractionType.ApplyOwnMoodlePreset)
        {
            using (ImRaii.Child("ApplyOwnPresetsChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {

                if (VisualApplierMoodles.LatestIpcData is null)
                    return;

                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsOwnPresetList" + PermissionData.DispName) ?? Guid.Empty;

                _moodlesService.DrawMoodlesPresetComboButton("##PermissionActionsOwnPresetList" + PermissionData.DispName, "Apply",
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

                    _ = _hub.UserApplyMoodlesByStatus(new(SPair.UserData, statusesToApply, IpcToggleType.MoodlesPreset));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }


        ////////// REMOVE MOODLES //////////
        if (CkGui.IconTextButton(FAI.UserMinus, "Remove a Moodle from " + PermissionData.DispName, WindowMenuWidth, true, RemovePairsMoodlesDisabled))
        {
            PairCombos.Opened = PairCombos.Opened == InteractionType.RemoveMoodle ? InteractionType.None : InteractionType.RemoveMoodle;
        }
        CkGui.AttachToolTip("Removes a Moodle from " + PermissionData.DispName + "'s Statuses.");
        if (PairCombos.Opened is InteractionType.RemoveMoodle)
        {
            using (ImRaii.Child("RemoveMoodles", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight()), false))
            {
                Guid selectedItem = _moodlesService.GetSelectedItem("##PermissionActionsRemoveMoodle" + PermissionData.DispName) ?? Guid.Empty;

                _moodlesService.DrawMoodleStatusComboButton("##PermissionActionsRemoveMoodle" + PermissionData.DispName, "Remove",
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

                    _logger.LogInformation("Removing Moodle: " + onButtonPress + " from " + PermissionData.DispName);
                    _ = _hub.UserRemoveMoodles(new(SPair.UserData, new List<Guid> { onButtonPress.Value }));
                    PairCombos.Opened = InteractionType.None;
                });
            }
            ImGui.Separator();
        }*/
    }
}
