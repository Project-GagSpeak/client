using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using OtterGui.Text;

namespace GagSpeak.Gui.MainWindow;

public partial class SidePanelPair
{
    public void DrawPresetList(KinksterInfoCache cache, Kinkster kinkster, float width)
    {
        var padding = ImGui.GetFrameHeight();
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var buttonW =CkGui.IconTextButtonSize(FAI.Sync, "Apply Preset");
        var comboW = width - buttonW - padding * 2 - spacing * 3;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding);
        using (ImRaii.Disabled(UiService.DisableUI || kinkster.OwnPerms.InHardcore))
        {
            if(CkGuiUtils.EnumCombo("##Presets", comboW, cache.CurPreset, out var newVal, flags: CFlags.None))
                cache.CurPreset = newVal;

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Sync, "Apply Preset", disabled: cache.CurPreset is PresetName.NoneSelected))
            {
                ApplySelectedPreset(cache, kinkster);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PresetApplied);
            }
        }
        CkGui.AttachToolTip(kinkster.OwnPerms.InHardcore
            ? "Can't use while in Hardcore mode!" 
            : "Select a preset to apply for this Kinkster.--NL--This will update your permissions in bulk.");
    }

    private void ApplySelectedPreset(KinksterInfoCache cache, Kinkster kinkster)
    {
        // get the correct preset we are applying, and execute the action. Afterwards, set the last executed time.
        Tuple<PairPerms, PairPermAccess> permissionTuple;
        switch (cache.CurPreset)
        {
            case PresetName.Dominant:
                permissionTuple = PresetDominantSetup();
                PushCmdToServer(kinkster, permissionTuple, "Dominant");
                break;

            case PresetName.Brat:
                permissionTuple = PresetBratSetup();
                PushCmdToServer(kinkster, permissionTuple, "Brat");
                break;

            case PresetName.RopeBunny:
                permissionTuple = PresetRopeBunnySetup();
                PushCmdToServer(kinkster, permissionTuple, "RopeBunny");
                break;

            case PresetName.Submissive:
                permissionTuple = PresetSubmissiveSetup();
                PushCmdToServer(kinkster, permissionTuple, "Submissive");
                break;

            case PresetName.Slut:
                permissionTuple = PresetSlutSetup();
                PushCmdToServer(kinkster, permissionTuple, "Slut");
                break;

            case PresetName.Pet:
                permissionTuple = PresetPetSetup();
                PushCmdToServer(kinkster, permissionTuple, "Pet");
                break;

            case PresetName.Slave:
                permissionTuple = PresetSlaveSetup();
                PushCmdToServer(kinkster, permissionTuple, "Slave");
                break;

            case PresetName.OwnersSlut:
                permissionTuple = PresetOwnersSlutSetup();
                PushCmdToServer(kinkster, permissionTuple, "OwnersSlut");
                break;

            case PresetName.OwnersPet:
                permissionTuple = PresetOwnersPetSetup();
                PushCmdToServer(kinkster, permissionTuple, "OwnersPet");
                break;

            case PresetName.OwnersSlave:
                permissionTuple = PresetOwnersSlaveSetup();
                PushCmdToServer(kinkster, permissionTuple, "OwnersSlave");
                break;

            default:
                Svc.Logger.Warning("No preset selected for pair {pair}", kinkster.UserData.UID);
                break;
        }
    }

    private void PushCmdToServer(Kinkster kinkster, Tuple<PairPerms, PairPermAccess> perms, string presetName)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _hub.UserBulkChangeUnique(new(kinkster.UserData, perms.Item1, perms.Item2, UpdateDir.Own, MainHub.OwnUserData));
            if (res.ErrorCode != GagSpeakApiEc.Success)
            {
                Svc.Logger.Error($"Failed preset application for [{kinkster.GetNickAliasOrUid()}]. Error: {res.ErrorCode}");
                return;
            }
            else
            {
                Svc.Logger.Information($"Applied Preset [{presetName}] to Kinkster [{kinkster.GetNickAliasOrUid()}]");
            }
        });
    }

    private Tuple<PairPerms, PairPermAccess> PresetDominantSetup()
    {
        var pairPerms = new PairPerms();
        var pairAccess = new PairPermAccess();
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetBratSetup()
    {
        var pairPerms = new PairPerms()
        {
            ApplyRestraintSets = true,
            RemoveRestraintSets = true,

            ApplyRestrictions = true,
            RemoveRestrictions = true,

            ApplyGags = true,
            RemoveGags = true,
        };
        var pairAccess = new PairPermAccess();
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetRopeBunnySetup()
    {
        var pairPerms = new PairPerms()
        {
            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(1, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestrictions = true,
            RemoveRestrictions = true,

            ApplyRestraintSets = true,
            ApplyLayers = true,
            RemoveLayers = true,
            RemoveRestraintSets = true,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative,
            MaxMoodleTime = new TimeSpan(1, 0, 0),
        };
        // all is false by default.
        var pairAccess = new PairPermAccess();
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetSubmissiveSetup()
    {
        var pairPerms = new PairPerms()
        {
            PermanentLocks = true,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(1, 30, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestrictions = true,
            LockRestrictions = true,
            MaxRestrictionTime = new TimeSpan(1, 30, 0),
            UnlockRestrictions = true,
            RemoveRestrictions = true,

            ApplyRestraintSets = true,
            ApplyLayers = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(1, 30, 0),
            UnlockRestraintSets = true,
            RemoveLayers = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative,
            MaxMoodleTime = new TimeSpan(1, 30, 0),
        };
        // all is false by default.
        var pairAccess = new PairPermAccess();
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetSlutSetup()
    {
        var pairPerms = new PairPerms()
        {
            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyRestraintSets = true,
            RemoveRestraintSets = true,

            ApplyRestrictions = true,
            LockRestrictions = true,
            MaxRestrictionTime = new TimeSpan(1, 0, 0),
            RemoveRestrictions = true,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(2, 30, 0),
            UnlockGags = true,
            RemoveGags = true,


            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.AllowOwn,
            MaxMoodleTime = new TimeSpan(1, 30, 0),

            ExecutePatterns = true,
            StopPatterns = true,
        };
        // all is false by default.
        var pairAccess = new PairPermAccess();
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetPetSetup()
    {
        var pairPerms = new PairPerms()
        {
            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyRestraintSets = true,
            ApplyLayers = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = true,
            RemoveLayers = true,
            RemoveRestraintSets = true,

            ApplyRestrictions = true,
            LockRestrictions = true,
            MaxRestrictionTime = new TimeSpan(3, 0, 0),
            UnlockRestrictions = true,
            RemoveRestrictions = true,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(3, 0, 0),
            UnlockGags = true,
            RemoveGags = true,



            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.Special | MoodleAccess.AllowOwn,
            MaxMoodleTime = new TimeSpan(3, 0, 0),

            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,
            ToggleTriggers = false,
        };
        // all is false by default.
        var pairAccess = new PairPermAccess();
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetSlaveSetup()
    {
        var pairPerms = new PairPerms()
        {
            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(12, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestrictions = true,
            LockRestrictions = true,
            MaxRestrictionTime = new TimeSpan(12, 0, 0),
            UnlockRestrictions = true,
            RemoveRestrictions = true,

            ApplyRestraintSets = true,
            ApplyLayers = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(12, 0, 0),
            UnlockRestraintSets = true,
            RemoveLayers = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.Special 
            | MoodleAccess.AllowOwn | MoodleAccess.Permanent | MoodleAccess.RemoveApplied,
            MaxMoodleTime = new TimeSpan(12, 0, 0),

            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,
            ToggleTriggers = true,

            InHardcore = false,
            AllowLockedFollowing = true,
            AllowLockedSitting = true,
            AllowIndoorConfinement = true,
        };
        // all is false by default.
        var pairAccess = new PairPermAccess();
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetOwnersSlutSetup()
    {
        var pairPerms = new PairPerms()
        {
            PermanentLocks = true,
            OwnerLocks = true,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(3, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestrictions = true,
            LockRestrictions = true,
            MaxRestrictionTime = new TimeSpan(3, 0, 0),
            UnlockRestrictions = true,
            RemoveRestrictions = true,

            ApplyRestraintSets = true,
            ApplyLayers = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = false,
            RemoveLayers = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.AllowOwn,
            MaxMoodleTime = new TimeSpan(1, 30, 0),

            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,
            ToggleTriggers = false,
        };
        var pairAccess = new PairPermAccess()
        {
            ChatGarblerActiveAllowed = true,

            PermanentLocksAllowed = false,
            OwnerLocksAllowed = true,
            DevotionalLocksAllowed = false,

            ApplyGagsAllowed = true,
            LockGagsAllowed = false,
            MaxGagTimeAllowed = false,
            UnlockGagsAllowed = true,
            RemoveGagsAllowed = true,

            ApplyRestrictionsAllowed = true,
            ApplyLayersAllowed = true,
            ApplyLayersWhileLockedAllowed = false,
            LockRestrictionsAllowed = true,
            MaxRestrictionTimeAllowed = false,
            UnlockRestrictionsAllowed = true,
            RemoveLayersAllowed = true,
            RemoveLayersWhileLockedAllowed = false,
            RemoveRestrictionsAllowed = true,

            ApplyRestraintSetsAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxRestraintTimeAllowed = false,
            UnlockRestraintSetsAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            PuppetPermsAllowed = PuppetPerms.Sit,

            MoodlesEnabledAllowed = false,
            MoodleAccessAllowed = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.Special
            | MoodleAccess.AllowOther | MoodleAccess.AllowOwn | MoodleAccess.Permanent,

            SpatialAudioAllowed = true,
            ExecutePatternsAllowed = true,
            StopPatternsAllowed = true,
        };
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetOwnersPetSetup()
    {
        var pairPerms = new PairPerms()
        {
            PermanentLocks = true,
            OwnerLocks = true,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(3, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestrictions = true,
            LockRestrictions = true,
            MaxRestrictionTime = new TimeSpan(3, 0, 0),
            UnlockRestrictions = true,
            RemoveRestrictions = true,

            ApplyRestraintSets = true,
            ApplyLayers = true,
            ApplyLayersWhileLocked = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = true,
            RemoveLayers = true,
            RemoveLayersWhileLocked = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.Special
            | MoodleAccess.AllowOwn,
            MaxMoodleTime = new TimeSpan(3, 0, 0),

            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,

            InHardcore = false,
            PairLockedStates = false,
            AllowLockedFollowing = false,
            AllowLockedSitting = true,
            AllowIndoorConfinement = false,
            AllowHidingChatBoxes = false,
            AllowHidingChatInput = false,
            AllowChatInputBlocking = false
        };
        var pairAccess = new PairPermAccess()
        {
            ChatGarblerActiveAllowed = true,
            ChatGarblerLockedAllowed = true,

            PermanentLocksAllowed = true,
            OwnerLocksAllowed = true,
            DevotionalLocksAllowed = false,

            ApplyGagsAllowed = true,
            LockGagsAllowed = true,
            MaxGagTimeAllowed = false,
            UnlockGagsAllowed = true,
            RemoveGagsAllowed = true,

            ApplyRestrictionsAllowed = true,
            LockRestrictionsAllowed = true,
            MaxRestrictionTimeAllowed = false,
            UnlockRestrictionsAllowed = true,
            RemoveRestrictionsAllowed = true,

            ApplyRestraintSetsAllowed = true,
            ApplyLayersAllowed = true,
            ApplyLayersWhileLockedAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxRestraintTimeAllowed = false,
            UnlockRestraintSetsAllowed = true,
            RemoveLayersAllowed = true,
            RemoveLayersWhileLockedAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            PuppetPermsAllowed = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlesEnabledAllowed = false,
            MoodleAccessAllowed = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.Special
            | MoodleAccess.AllowOther | MoodleAccess.AllowOwn | MoodleAccess.Permanent,
            MaxMoodleTimeAllowed = false,

            SpatialAudioAllowed = true,
            ExecutePatternsAllowed = true,
            StopPatternsAllowed = true,
            ToggleAlarmsAllowed = true,
            ToggleTriggersAllowed = true,
        };
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetOwnersSlaveSetup()
    {
        var pairPerms = new PairPerms()
        {
            PermanentLocks = true,
            OwnerLocks = true,
            DevotionalLocks = true,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(12, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestrictions = true,
            LockRestrictions = true,
            MaxRestrictionTime = new TimeSpan(12, 0, 0),
            UnlockRestrictions = true,
            RemoveRestrictions = true,

            ApplyRestraintSets = true,
            ApplyLayers = true,
            ApplyLayersWhileLocked = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(12, 0, 0),
            UnlockRestraintSets = true,
            RemoveLayers = true,
            RemoveLayersWhileLocked = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodleAccess = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.Special
            | MoodleAccess.AllowOwn | MoodleAccess.Permanent | MoodleAccess.RemoveApplied,
            MaxMoodleTime = new TimeSpan(12, 0, 0),

            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,
            ToggleTriggers = true,

            InHardcore = false,
            PairLockedStates = false,
            AllowLockedFollowing = true,
            AllowLockedSitting = true,
            AllowIndoorConfinement = true,
            AllowImprisonment = true,
            AllowHidingChatBoxes = false,
            AllowHidingChatInput = false,
            AllowChatInputBlocking = false
        };
        var pairAccess = new PairPermAccess()
        {
            ChatGarblerActiveAllowed = true,
            ChatGarblerLockedAllowed = true,

            PermanentLocksAllowed = true,
            OwnerLocksAllowed = true,
            DevotionalLocksAllowed = true,

            ApplyGagsAllowed = true,
            LockGagsAllowed = true,
            MaxGagTimeAllowed = true,
            UnlockGagsAllowed = true,
            RemoveGagsAllowed = true,

            ApplyRestrictionsAllowed = true,
            LockRestrictionsAllowed = true,
            MaxRestrictionTimeAllowed = true,
            UnlockRestrictionsAllowed = true,
            RemoveRestrictionsAllowed = true,
            
            ApplyRestraintSetsAllowed = true,
            ApplyLayersAllowed = true,
            ApplyLayersWhileLockedAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxRestraintTimeAllowed = true,
            UnlockRestraintSetsAllowed = true,
            RemoveLayersAllowed = true,
            RemoveLayersWhileLockedAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            PuppetPermsAllowed = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlesEnabledAllowed = false,
            MoodleAccessAllowed = MoodleAccess.Positive | MoodleAccess.Negative | MoodleAccess.Special
            | MoodleAccess.AllowOther | MoodleAccess.AllowOwn | MoodleAccess.Permanent,
            MaxMoodleTimeAllowed = true,

            SpatialAudioAllowed = true,
            ExecutePatternsAllowed = true,
            StopPatternsAllowed = true,
            ToggleAlarmsAllowed = true,
            ToggleTriggersAllowed = true,
        };
        return new(pairPerms, pairAccess);
    }
}
