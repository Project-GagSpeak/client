using Dalamud.Interface.Utility.Raii;
using GagSpeak.State.Caches;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Permissions;

public partial class PairStickyUI
{
    // Maybe add later, not sure atm.
    private Guid _selectedOwnStatus = Guid.Empty;
    private Guid _selectedOwnPreset = Guid.Empty;
    private Guid _selectedPairStatus = Guid.Empty;
    private Guid _selectedPairPreset = Guid.Empty;
    private Guid _selectedRemoval = Guid.Empty;
    private void DrawMoodlesActions()
    {
        var lastIpcData = SPair.LastIpcData;
        var pairUniquePerms = SPair.PairPerms;

        var ApplyPairsMoodleToPairDisabled = !pairUniquePerms.MoodlePerms.HasFlag(MoodlePerms.PairCanApplyYourMoodlesToYou) || lastIpcData.Statuses.Count <= 0;
        var ApplyOwnMoodleToPairDisabled = !pairUniquePerms.MoodlePerms.HasFlag(MoodlePerms.PairCanApplyTheirMoodlesToYou) || MoodleCache.IpcData is null || MoodleCache.IpcData.Statuses.Count <= 0;
        var RemovePairsMoodlesDisabled = !pairUniquePerms.MoodlePerms.HasFlag(MoodlePerms.RemovingMoodles) || lastIpcData.Statuses.Count <= 0;

        ////////// APPLY MOODLES FROM PAIR's LIST //////////
        if (CkGui.IconTextButton(FAI.PersonCirclePlus, "Apply a Moodle from their list", WindowMenuWidth, true, ApplyPairsMoodleToPairDisabled))
            OpenOrClose(InteractionType.ApplyPairMoodle);
        CkGui.AttachToolTip("Applies a Moodle from " + SPair.UserData.AliasOrUID + "'s Moodles List to them.");

        if (OpenedInteraction is InteractionType.ApplyPairMoodle)
        {
            using (ImRaii.Child("ApplyPairMoodles", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairMoodleStatuses.DrawComboButton("##PairPermStatuses" + DisplayName, WindowMenuWidth, true, "Select a Status to Apply");
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM PAIR's LIST //////////
        if (CkGui.IconTextButton(FAI.FileCirclePlus, "Apply a Preset from their list", WindowMenuWidth, true, ApplyPairsMoodleToPairDisabled))
            OpenOrClose(InteractionType.ApplyPairMoodlePreset);
        CkGui.AttachToolTip("Applies a Preset from " + DisplayName + "'s Presets List to them.");
        
        if (OpenedInteraction is InteractionType.ApplyPairMoodlePreset)
        {
            using (ImRaii.Child("ApplyPairPresetsChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _pairMoodlePresets.DrawComboButton("##PairPermPresets" + DisplayName, WindowMenuWidth, true, "Select a Preset to Apply");
            ImGui.Separator();
        }

        ////////// APPLY MOODLES FROM OWN LIST //////////
        if (CkGui.IconTextButton(FAI.UserPlus, "Apply a Moodle from your list", WindowMenuWidth, true, ApplyOwnMoodleToPairDisabled))
            OpenOrClose(InteractionType.ApplyOwnMoodle);
        CkGui.AttachToolTip("Applies a Moodle from your Moodles List to " + DisplayName + ".");

        if (OpenedInteraction is InteractionType.ApplyOwnMoodle)
        {
            using (ImRaii.Child("ApplyOwnMoodlesChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _moodleStatuses.DrawComboButton("##OwnStatusesSticky" + DisplayName, WindowMenuWidth, true, "Select a Status to Apply");
            ImGui.Separator();
        }

        ////////// APPLY PRESETS FROM OWN LIST //////////
        if (CkGui.IconTextButton(FAI.FileCirclePlus, "Apply a Preset from your list", WindowMenuWidth, true, ApplyOwnMoodleToPairDisabled))
            OpenOrClose(InteractionType.ApplyOwnMoodlePreset);
        CkGui.AttachToolTip("Applies a Preset from your Presets List to " + DisplayName + ".");

        if (OpenedInteraction is InteractionType.ApplyOwnMoodlePreset)
        {
            using (ImRaii.Child("ApplyOwnPresetsChildWindow", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _moodlePresets.DrawComboButton("##OwnPresetsSticky" + DisplayName, WindowMenuWidth, true, "Select a Preset to Apply");
            ImGui.Separator();
        }


        ////////// REMOVE MOODLES //////////
        if (CkGui.IconTextButton(FAI.UserMinus, "Remove a Moodle from " + DisplayName, WindowMenuWidth, true, RemovePairsMoodlesDisabled))
            OpenOrClose(InteractionType.RemoveMoodle);
        CkGui.AttachToolTip("Removes a Moodle from " + DisplayName + "'s Statuses.");

        if (OpenedInteraction is InteractionType.RemoveMoodle)
        {
            using (ImRaii.Child("RemoveMoodles", new Vector2(WindowMenuWidth, ImGui.GetFrameHeight())))
                _activePairStatusCombo.DrawComboButton("##ActivePairStatuses" + DisplayName, WindowMenuWidth, false, "Select a Status to remove.");
            ImGui.Separator();
        }
    }
}
