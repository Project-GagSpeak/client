using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using OtterGui.Text;

namespace GagSpeak.Services;

public class PresetLogicDrawer
{
    private readonly ILogger<PresetLogicDrawer> _logger;
    private readonly MainHub _hub;
    public PresetLogicDrawer(ILogger<PresetLogicDrawer> logger, MainHub hub)
    {
        _logger = logger;
        _hub = hub;
    }

    public DateTime LastApplyTime { get; private set; } = DateTime.MinValue;
    public PresetName SelectedPreset { get; private set; } = PresetName.NoneSelected;

    public void DrawPresetList(Pair pairToDrawListFor, float width)
    {
        // before drawing, we need to know if we should disable it or not.
        // It's OK if things are active for the player, since it doesn't actually trigger everything at once.
        var disabledCondition = DateTime.UtcNow - LastApplyTime < TimeSpan.FromSeconds(10) || pairToDrawListFor.OwnPerms.InHardcore;

        var comboW = width - CkGui.IconTextButtonSize(FAI.Sync, "Apply Preset");
        using (var disabled = ImRaii.Disabled(disabledCondition))
        {
            if(CkGuiUtils.EnumCombo("##Presets", comboW, SelectedPreset, out var newVal))
                SelectedPreset = newVal;

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Sync, "Apply Preset", disabled: SelectedPreset is PresetName.NoneSelected))
            {
                ApplySelectedPreset(pairToDrawListFor);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PresetApplied);
            }
        }
        CkGui.AttachToolTip(pairToDrawListFor.OwnPerms.InHardcore
            ? "Cannot execute presets while in Hardcore mode."
            : disabledCondition
                ? "You must wait 10 seconds between applying presets."
                : "Select a permission preset to apply for this pair." + Environment.NewLine + "Will update your permissions in bulk. (10s Cooldown)");
    }

    private void ApplySelectedPreset(Pair pairToDrawListFor)
    {
        // get the correct preset we are applying, and execute the action. Afterwards, set the last executed time.
        try
        {
            Tuple<PairPerms, PairPermAccess> permissionTuple;
            switch (SelectedPreset)
            {
                case PresetName.Dominant:
                    permissionTuple = PresetDominantSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "Dominant");
                    break;

                case PresetName.Brat:
                    permissionTuple = PresetBratSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "Brat");
                    break;

                case PresetName.RopeBunny:
                    permissionTuple = PresetRopeBunnySetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "RopeBunny");
                    break;

                case PresetName.Submissive:
                    permissionTuple = PresetSubmissiveSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "Submissive");
                    break;

                case PresetName.Slut:
                    permissionTuple = PresetSlutSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "Slut");
                    break;

                case PresetName.Pet:
                    permissionTuple = PresetPetSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "Pet");
                    break;

                case PresetName.Slave:
                    permissionTuple = PresetSlaveSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "Slave");
                    break;

                case PresetName.OwnersSlut:
                    permissionTuple = PresetOwnersSlutSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "OwnersSlut");
                    break;

                case PresetName.OwnersPet:
                    permissionTuple = PresetOwnersPetSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "OwnersPet");
                    break;

                case PresetName.OwnersSlave:
                    permissionTuple = PresetOwnersSlaveSetup();
                    PushCmdToServer(pairToDrawListFor, permissionTuple, "OwnersSlave");
                    break;

                default:
                    _logger.LogWarning("No preset selected for pair {pair}", pairToDrawListFor.UserData.UID);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error applying preset {preset} to pair {pair}", SelectedPreset, pairToDrawListFor.UserData.UID);
        }
    }

    private void PushCmdToServer(Pair pairToDrawListFor, Tuple<PairPerms, PairPermAccess> permTuple, string presetName)
    {
        _ = _hub.UserBulkChangeUnique(new(pairToDrawListFor.UserData, permTuple.Item1, permTuple.Item2));
        _logger.LogInformation("Applied {preset} preset to pair {pair}", presetName, pairToDrawListFor.UserData.UID);
        LastApplyTime = DateTime.UtcNow;
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
            ApplyGags = true,
            LockGags = false,
            MaxGagTime = new TimeSpan(0, 30, 0),
            UnlockGags = false,
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
            LockRestrictions = false,
            MaxRestrictionTime = new TimeSpan(1, 0, 0),
            UnlockRestrictions = false,
            RemoveRestrictions = true,

            ApplyRestraintSets = true,
            LockRestraintSets = false,
            MaxRestraintTime = new TimeSpan(1, 0, 0),
            UnlockRestraintSets = false,
            RemoveRestraintSets = true,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes,
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
            OwnerLocks = false,
            DevotionalLocks = false,

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
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(1, 30, 0),
            UnlockRestraintSets = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes,
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
            IsPaused = false,

            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(2, 30, 0),
            UnlockGags = true,
            RemoveGags = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.PairCanApplyYourMoodlesToYou,
            MaxMoodleTime = new TimeSpan(1, 30, 0),

            ToggleToyState = true,
            RemoteControlAccess = true,
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
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.SpecialStatusTypes | MoodlePerms.PairCanApplyYourMoodlesToYou,
            MaxMoodleTime = new TimeSpan(3, 0, 0),

            ToggleToyState = true,
            RemoteControlAccess = true,
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
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(12, 0, 0),
            UnlockRestraintSets = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.SpecialStatusTypes 
            | MoodlePerms.PairCanApplyYourMoodlesToYou | MoodlePerms.PermanentMoodles | MoodlePerms.RemovingMoodles,
            MaxMoodleTime = new TimeSpan(12, 0, 0),

            ToggleToyState = true,
            RemoteControlAccess = true,
            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,
            ToggleTriggers = true,

            InHardcore = false,
            AllowForcedFollow = true,
            AllowForcedSit = true,
            AllowForcedStay = true,
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
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = false,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.PairCanApplyYourMoodlesToYou,
            MaxMoodleTime = new TimeSpan(1, 30, 0),

            ToggleToyState = true,
            RemoteControlAccess = true,
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
            ApplyRestraintLayersAllowed = false,
            LockRestrictionsAllowed = true,
            MaxRestrictionTimeAllowed = false,
            UnlockRestrictionsAllowed = true,
            RemoveRestrictionsAllowed = true,

            ApplyRestraintSetsAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxRestraintTimeAllowed = false,
            UnlockRestraintSetsAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            PuppetPermsAllowed = PuppetPerms.Sit,

            MoodlesEnabledAllowed = false,
            MoodlePermsAllowed = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.SpecialStatusTypes
            | MoodlePerms.PairCanApplyTheirMoodlesToYou | MoodlePerms.PairCanApplyYourMoodlesToYou | MoodlePerms.PermanentMoodles,

            LockToyboxUIAllowed = true,
            SpatialAudioAllowed = true,
            ToggleToyStateAllowed = true,
            RemoteControlAccessAllowed = true,
            ExecutePatternsAllowed = true,
            StopPatternsAllowed = true,
        };
        return new(pairPerms, pairAccess);
    }

    private Tuple<PairPerms, PairPermAccess> PresetOwnersPetSetup()
    {
        var pairPerms = new PairPerms()
        {
            IsPaused = false,

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
            ApplyRestraintLayers = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.SpecialStatusTypes
            | MoodlePerms.PairCanApplyYourMoodlesToYou,
            MaxMoodleTime = new TimeSpan(3, 0, 0),

            ToggleToyState = true,
            RemoteControlAccess = true,
            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,

            InHardcore = false,
            PairLockedStates = false,
            AllowForcedFollow = false,
            AllowForcedSit = true,
            AllowForcedStay = false,
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
            ApplyRestraintLayersAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxRestraintTimeAllowed = false,
            UnlockRestraintSetsAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            PuppetPermsAllowed = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlesEnabledAllowed = false,
            MoodlePermsAllowed = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.SpecialStatusTypes
            | MoodlePerms.PairCanApplyTheirMoodlesToYou | MoodlePerms.PairCanApplyYourMoodlesToYou | MoodlePerms.PermanentMoodles,
            MaxMoodleTimeAllowed = false,

            ToyboxEnabledAllowed = true,
            LockToyboxUIAllowed = true,
            SpatialAudioAllowed = true,
            ToggleToyStateAllowed = true,
            RemoteControlAccessAllowed = true,
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
            IsPaused = false,

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
            ApplyRestraintLayers = true,
            LockRestraintSets = true,
            MaxRestraintTime = new TimeSpan(12, 0, 0),
            UnlockRestraintSets = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            PuppetPerms = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlePerms = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.SpecialStatusTypes
            | MoodlePerms.PairCanApplyYourMoodlesToYou | MoodlePerms.PermanentMoodles | MoodlePerms.RemovingMoodles,
            MaxMoodleTime = new TimeSpan(12, 0, 0),

            ToggleToyState = true,
            RemoteControlAccess = true,
            ExecutePatterns = true,
            StopPatterns = true,
            ToggleAlarms = true,
            ToggleTriggers = true,

            InHardcore = false,
            PairLockedStates = false,
            AllowForcedFollow = true,
            AllowForcedSit = true,
            AllowForcedStay = true,
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
            ApplyRestraintLayersAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxRestraintTimeAllowed = true,
            UnlockRestraintSetsAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            PuppetPermsAllowed = PuppetPerms.Sit | PuppetPerms.Emotes | PuppetPerms.Alias,

            MoodlesEnabledAllowed = false,
            MoodlePermsAllowed = MoodlePerms.PositiveStatusTypes | MoodlePerms.NegativeStatusTypes | MoodlePerms.SpecialStatusTypes
            | MoodlePerms.PairCanApplyTheirMoodlesToYou | MoodlePerms.PairCanApplyYourMoodlesToYou | MoodlePerms.PermanentMoodles,
            MaxMoodleTimeAllowed = true,

            ToyboxEnabledAllowed = true,
            LockToyboxUIAllowed = true,
            SpatialAudioAllowed = true,
            ToggleToyStateAllowed = true,
            RemoteControlAccessAllowed = true,
            ExecutePatternsAllowed = true,
            StopPatternsAllowed = true,
            ToggleAlarmsAllowed = true,
            ToggleTriggersAllowed = true,
        };
        return new(pairPerms, pairAccess);
    }
}
