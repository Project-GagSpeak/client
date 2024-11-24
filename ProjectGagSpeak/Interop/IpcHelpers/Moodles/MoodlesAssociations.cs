using Dalamud.Interface;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.IPC;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using System.Numerics;

namespace GagSpeak.Interop.IpcHelpers.Moodles;
public class MoodlesAssociations : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerMoodles _moodles;
    private readonly MoodlesService _moodlesService;

    public MoodlesAssociations(ILogger<MoodlesAssociations> logger, GagspeakMediator mediator,
        IpcCallerMoodles moodles, MoodlesService moodlesService) : base(logger, mediator)
    {
        _moodles = moodles;
        _moodlesService = moodlesService;
    }

    private Guid SelectedStatusGuid = Guid.Empty;
    private Guid SelectedPresetGuid = Guid.Empty;

    // main draw function for the mod associations table
    public void DrawMoodlesStatusesListForItem(IMoodlesAssociable associable, CharaIPCData? lastPlayerIpcData, float paddingHeight, bool isPresets)
    {
        if (lastPlayerIpcData == null) return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.X * 0.3f, paddingHeight));
        using var table = ImRaii.Table("RestraintSetMoodlesTable-" + isPresets, 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table) { return; }

        string columnTitle = isPresets ? "Moodle Presets" : "Moodle Statuses";
        ImGui.TableSetupColumn("##Delete", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn(columnTitle + " to enable with set", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        Guid? removedMoodle = null;
        Guid? removedMoodlePreset = null;

        // before we fetch the list, first see if any moodles in the list are no longer present in our Moodles List. 
        if (IpcCallerMoodles.APIAvailable)
        {
            var moodlesStatusList = lastPlayerIpcData.MoodlesStatuses;
            associable.AssociatedMoodles.RemoveAll(x => !moodlesStatusList.Any(y => y.GUID == x));

            var moodlesPresetList = lastPlayerIpcData.MoodlesPresets;
            if (!lastPlayerIpcData.MoodlesPresets.Any(x => x.Item1 == associable.AssociatedMoodlePreset))
                associable.AssociatedMoodlePreset = Guid.Empty;
        }

        // Handle what we are drawing based on what the table is for.
        if (isPresets)
        {
            //////////////////// HANDLE PRESETS ////////////////////
            var associatedPreset = associable.AssociatedMoodlePreset; // Assuming this is now a single Guid
            if (associatedPreset != Guid.Empty)
            {
                using var id = ImRaii.PushId("Preset");

                DrawAssociatedMoodlePresetRow(lastPlayerIpcData, associatedPreset, 0, out var removedPresetTmp);
                if (removedPresetTmp.HasValue)
                {
                    removedMoodlePreset = removedPresetTmp;
                }
            }

            DrawSetNewMoodlePresetRow(associable, lastPlayerIpcData);

            if (removedMoodlePreset.HasValue)
            {
                associable.AssociatedMoodlePreset = Guid.Empty;
                // reset value
                removedMoodlePreset = null;
            }
        }
        else
        {
            //////////////////// HANDLE STATUSES ////////////////////
            foreach (var (associatedMoodle, idx) in associable.AssociatedMoodles.WithIndex())
            {
                using var id = ImRaii.PushId(idx + "Statuses");

                DrawAssociatedMoodleRow(lastPlayerIpcData, associatedMoodle, idx, out var removedMoodleTmp);
                if (removedMoodleTmp.HasValue)
                {
                    removedMoodle = removedMoodleTmp;
                }
            }

            DrawSetNewMoodleRow(associable, lastPlayerIpcData);

            if (removedMoodle.HasValue)
            {
                associable.AssociatedMoodles.Remove(removedMoodle.Value);
            }
        }
    }


    private void DrawAssociatedMoodleRow(CharaIPCData clientIpcData, Guid moodleGuid, int idx, out Guid? removedMoodleTmp)
    {
        removedMoodleTmp = null;
        ImGui.TableNextColumn();
        // delete icon
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
        "Delete this Moodle from associations (Hold Shift)", !ImGui.GetIO().KeyShift, true))
        {
            removedMoodleTmp = moodleGuid;
        }

        // the name of the appended mod
        ImGui.TableNextColumn();
        // locate the friendly name 
        string friendlyName = clientIpcData.MoodlesStatuses.FirstOrDefault(x => x.GUID == moodleGuid).Title ?? "No Title Set";
        ImGui.Selectable($" {friendlyName}##name");
    }

    private void DrawAssociatedMoodlePresetRow(CharaIPCData clientIpcData, Guid presetGuid, int idx, out Guid? removedPresetTmp)
    {
        removedPresetTmp = null;
        ImGui.TableNextColumn();
        // delete icon
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
        "Delete this Moodle Preset's Statuses from associations (Hold Shift)", !ImGui.GetIO().KeyShift, true))
        {
            removedPresetTmp = presetGuid;
        }

        // the name of the appended mod
        ImGui.TableNextColumn();
        // locate the friendly name 
        ImGui.Selectable($"{presetGuid} - (Hover To See Attached Statuses)##presetName");
        if (ImGui.IsItemHovered())
        {
            // get the list of friendly names for each guid in the preset
            var test = clientIpcData.MoodlesPresets
                .FirstOrDefault(x => x.Item1 == presetGuid).Item2
                .Select(x => clientIpcData.MoodlesStatuses
                .FirstOrDefault(y => y.GUID == x).Title ?? "No FriendlyName Set for this Moodle") ?? new List<string>();
            ImGui.SetTooltip($"This Preset Enables the Following Moodles:\n" + string.Join(Environment.NewLine, test));
        }
    }

    private void DrawSetNewMoodleRow(IMoodlesAssociable refSet, CharaIPCData ipcData)
    {
        var displayList = ipcData.MoodlesStatuses;

        ImGui.TableNextColumn();
        var tooltip = refSet.AssociatedMoodles.Any(x => x == SelectedStatusGuid)
                ? "The Restraint Set already includes this Moodle." : "Add Moodle Status to Status List";

        bool disableCondition = refSet.AssociatedMoodles.Any(x => x == SelectedStatusGuid);

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(),
            new Vector2(ImGui.GetFrameHeight()), tooltip, disableCondition, true))
        {
            refSet.AssociatedMoodles.Add(SelectedStatusGuid);
        }

        ImGui.TableNextColumn();
        var length = ImGui.GetContentRegionAvail().X;
        _moodlesService.DrawMoodleStatusCombo("##MoodlesAssociationStatusSelector", length, ipcData.MoodlesStatuses,
            (i) => SelectedStatusGuid = i ?? Guid.Empty, 1.25f);
    }

    private void DrawSetNewMoodlePresetRow(IMoodlesAssociable refSet, CharaIPCData ipcData)
    {
        var displayList = ipcData.MoodlesPresets;
        // if the size is 0, then make the default preview value "No Presets Available"
        ImGui.TableNextColumn();
        var tooltip = "Add Moodle Preset's Statuses to the status list";

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(),
            new Vector2(ImGui.GetFrameHeight()), tooltip, false, true))
        {
            // using the currently selected preset guid, locate it in the list where it matches the .Item1, and get .Item2 values.
            var presetGuids = displayList.FirstOrDefault(x => x.Item1 == SelectedPresetGuid).Item2;
            if (presetGuids != null)
            {
                refSet.AssociatedMoodles.AddRange(presetGuids.Where(x => !refSet.AssociatedMoodles.Contains(x)));
                refSet.AssociatedMoodlePreset = SelectedPresetGuid;
            }
        }

        ImGui.TableNextColumn();
        var length = ImGui.GetContentRegionAvail().X;
        _moodlesService.DrawMoodlesPresetCombo("RestraintSetPresetSelector", length, ipcData.MoodlesPresets,
            ipcData.MoodlesStatuses, (i) => SelectedPresetGuid = i ?? Guid.Empty);
    }

    public void MoodlesStatusSelectorForCursedItem(CursedItem cursedItem, CharaIPCData ipcData, float width)
    {
        if (cursedItem.MoodleType is IpcToggleType.MoodlesStatus)
        {
            _moodlesService.DrawMoodleStatusCombo("##CursedItemMoodleStatusSelector" + cursedItem.LootId, width, ipcData.MoodlesStatuses,
                (i) => cursedItem.MoodleIdentifier = i ?? Guid.Empty, 1.25f);
        }
        else
        {
            _moodlesService.DrawMoodlesPresetCombo("##CursedItemMoodlePresetSelector" + cursedItem.LootId, width, ipcData.MoodlesPresets,
                ipcData.MoodlesStatuses, (i) => cursedItem.MoodleIdentifier = i ?? Guid.Empty);
        }
    }
}
