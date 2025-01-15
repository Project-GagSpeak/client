using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using OtterGui.Text;

namespace GagSpeak.Services;

public class PermissionPresetLogic
{
    private readonly ILogger<PermissionPresetLogic> _logger;
    private readonly MainHub _apiHubMain;
    private readonly ClientData _playerManager;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager; // might not need if we use a pair to pass in for this.
    private readonly UiSharedService _uiShared;

    public PermissionPresetLogic(ILogger<PermissionPresetLogic> logger,
        MainHub apiHubMain, ClientData playerManager,
        ClientConfigurationManager clientConfigs, PairManager pairManager,
        UiSharedService uiShared)
    {
        _logger = logger;
        _apiHubMain = apiHubMain;
        _playerManager = playerManager;
        _clientConfigs = clientConfigs;
        _pairManager = pairManager;
        _uiShared = uiShared;
    }

    public DateTime LastApplyTime { get; private set; } = DateTime.MinValue;
    public PresetName SelectedPreset { get; private set; } = PresetName.NoneSelected;

    public void DrawPresetList(Pair pairToDrawListFor, float width)
    {
        // before drawing, we need to know if we should disable it or not.
        // It's OK if things are active for the player, since it doesn't actually trigger everything at once.
        bool disabledCondition = DateTime.UtcNow - LastApplyTime < TimeSpan.FromSeconds(10) || pairToDrawListFor.OwnPerms.InHardcore;

        float comboWidth = width - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Sync, "Apply Preset");
        using (var disabled = ImRaii.Disabled(disabledCondition))
        {
            _uiShared.DrawCombo("Permission Preset Selector", comboWidth, Enum.GetValues<PresetName>(),
            (preset) => preset.ToName(), (i) => SelectedPreset = (PresetName)i, SelectedPreset, false);
            ImUtf8.SameLineInner();
            if (_uiShared.IconTextButton(FontAwesomeIcon.Sync, "Apply Preset", disabled: SelectedPreset is PresetName.NoneSelected))
            {
                ApplySelectedPreset(pairToDrawListFor);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PresetApplied);
            }
        }
        UiSharedService.AttachToolTip(pairToDrawListFor.OwnPerms.InHardcore
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
            Tuple<UserPairPermissions, UserEditAccessPermissions> permissionTuple;
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

    private void PushCmdToServer(Pair pairToDrawListFor, Tuple<UserPairPermissions, UserEditAccessPermissions> permissionTuple, string presetName)
    {
        _ = _apiHubMain.UserPushAllUniquePerms(new(pairToDrawListFor.UserData, MainHub.PlayerUserData, permissionTuple.Item1, permissionTuple.Item2, UpdateDir.Own));
        _logger.LogInformation("Applied {preset} preset to pair {pair}", presetName, pairToDrawListFor.UserData.UID);
        LastApplyTime = DateTime.UtcNow;
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetDominantSetup()
    {
        var pairPerms = new UserPairPermissions();
        var pairAccess = new UserEditAccessPermissions();
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetBratSetup()
    {
        var pairPerms = new UserPairPermissions()
        {
            ApplyGags = true,
            LockGags = false,
            MaxGagTime = new TimeSpan(0, 30, 0),
            UnlockGags = false,
            RemoveGags = true,
        };
        var pairAccess = new UserEditAccessPermissions();
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetRopeBunnySetup()
    {
        var pairPerms = new UserPairPermissions()
        {
            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(1, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestraintSets = true,
            LockRestraintSets = false,
            MaxAllowedRestraintTime = new TimeSpan(1, 0, 0),
            UnlockRestraintSets = false,
            RemoveRestraintSets = true,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = false,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = false,
            MaxMoodleTime = new TimeSpan(1, 0, 0),
            AllowPermanentMoodles = false,
            AllowRemovingMoodles = false,
        };
        // all is false by default.
        var pairAccess = new UserEditAccessPermissions();
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetSubmissiveSetup()
    {
        var pairPerms = new UserPairPermissions()
        {
            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxAllowedRestraintTime = new TimeSpan(1, 30, 0),
            UnlockGags = true,
            RemoveGags = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            SitRequests = true,
            MotionRequests = false,
            AllRequests = false,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = false,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = false,
            MaxMoodleTime = new TimeSpan(1, 30, 0),
            AllowPermanentMoodles = false,
            AllowRemovingMoodles = false,
        };
        // all is false by default.
        var pairAccess = new UserEditAccessPermissions();
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetSlutSetup()
    {
        var pairPerms = new UserPairPermissions()
        {
            IsPaused = false,

            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxAllowedRestraintTime = new TimeSpan(2, 30, 0),
            UnlockGags = true,
            RemoveGags = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            SitRequests = true,
            MotionRequests = true,
            AllRequests = false,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = false,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = true,
            MaxMoodleTime = new TimeSpan(1, 30, 0),
            AllowPermanentMoodles = false,
            AllowRemovingMoodles = false,

            CanToggleToyState = true,
            CanUseVibeRemote = true,
            CanToggleAlarms = false,
            CanExecutePatterns = true,
            CanStopPatterns = true,
            CanToggleTriggers = false,
        };
        // all is false by default.
        var pairAccess = new UserEditAccessPermissions();
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetPetSetup()
    {
        var pairPerms = new UserPairPermissions()
        {
            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxAllowedRestraintTime = new TimeSpan(3, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            SitRequests = true,
            MotionRequests = true,
            AllRequests = false,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = true,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = true,
            MaxMoodleTime = new TimeSpan(3, 0, 0),
            AllowPermanentMoodles = false,
            AllowRemovingMoodles = true,

            CanToggleToyState = true,
            CanUseVibeRemote = true,
            CanToggleAlarms = true,
            CanSendAlarms = true,
            CanExecutePatterns = true,
            CanStopPatterns = true,
            CanToggleTriggers = false,
        };
        // all is false by default.
        var pairAccess = new UserEditAccessPermissions();
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetSlaveSetup()
    {
        var pairPerms = new UserPairPermissions()
        {
            PermanentLocks = true,
            OwnerLocks = false,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxAllowedRestraintTime = new TimeSpan(12, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            SitRequests = true,
            MotionRequests = true,
            AllRequests = false,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = true,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = true,
            MaxMoodleTime = new TimeSpan(12, 0, 0),
            AllowPermanentMoodles = true,
            AllowRemovingMoodles = true,

            CanToggleToyState = true,
            CanUseVibeRemote = true,
            CanToggleAlarms = true,
            CanSendAlarms = true,
            CanExecutePatterns = true,
            CanStopPatterns = true,
            CanToggleTriggers = true,

            InHardcore = false,
            DevotionalStatesForPair = false,
            AllowForcedFollow = true,
            AllowForcedSit = true,
            AllowForcedToStay = true,
            AllowBlindfold = true,
            AllowHidingChatBoxes = false,
            AllowHidingChatInput = false,
            AllowChatInputBlocking = false
        };
        // all is false by default.
        var pairAccess = new UserEditAccessPermissions();
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetOwnersSlutSetup()
    {
        var pairPerms = new UserPairPermissions()
        {
            PermanentLocks = true,
            OwnerLocks = true,
            DevotionalLocks = false,

            ApplyGags = true,
            LockGags = true,
            MaxGagTime = new TimeSpan(3, 0, 0),
            UnlockGags = true,
            RemoveGags = true,

            ApplyRestraintSets = true,
            LockRestraintSets = true,
            MaxAllowedRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = false,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            SitRequests = true,
            MotionRequests = true,
            AllRequests = false,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = false,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = true,
            MaxMoodleTime = new TimeSpan(1, 30, 0),
            AllowPermanentMoodles = false,
            AllowRemovingMoodles = false,

            CanToggleToyState = true,
            CanUseVibeRemote = true,
            CanToggleAlarms = false,
            CanSendAlarms = true,
            CanExecutePatterns = true,
            CanStopPatterns = true,
            CanToggleTriggers = false,

            InHardcore = false,
            DevotionalStatesForPair = false,
            AllowForcedFollow = false,
            AllowForcedSit = false,
            AllowForcedToStay = false,
            AllowBlindfold = true,
            AllowHidingChatBoxes = false,
            AllowHidingChatInput = false,
            AllowChatInputBlocking = false
        };
        var pairAccess = new UserEditAccessPermissions()
        {
            LiveChatGarblerActiveAllowed = true,
            LiveChatGarblerLockedAllowed = false,

            PermanentLocksAllowed = false,
            OwnerLocksAllowed = true,
            DevotionalLocksAllowed = false,

            ApplyGagsAllowed = true,
            LockGagsAllowed = false,
            MaxGagTimeAllowed = false,
            UnlockGagsAllowed = true,
            RemoveGagsAllowed = true,

            WardrobeEnabledAllowed = false,
            ItemAutoEquipAllowed = false,
            RestraintSetAutoEquipAllowed = false,
            ApplyRestraintSetsAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxAllowedRestraintTimeAllowed = false,
            UnlockRestraintSetsAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            SitRequestsAllowed = true,
            MotionRequestsAllowed = false,
            AliasRequestsAllowed = false,
            AllRequestsAllowed = false,

            MoodlesEnabledAllowed = false,
            AllowPositiveStatusTypesAllowed = true,
            AllowNegativeStatusTypesAllowed = true,
            AllowSpecialStatusTypesAllowed = true,
            PairCanApplyOwnMoodlesToYouAllowed = true,
            PairCanApplyYourMoodlesToYouAllowed = true,
            MaxMoodleTimeAllowed = false,
            AllowPermanentMoodlesAllowed = true,
            AllowRemovingMoodlesAllowed = false,

            ToyboxEnabledAllowed = false,
            LockToyboxUIAllowed = true,
            SpatialVibratorAudioAllowed = true,
            CanToggleToyStateAllowed = true,
            CanUseVibeRemoteAllowed = true,
            CanToggleAlarmsAllowed = false,
            CanSendAlarmsAllowed = false,
            CanExecutePatternsAllowed = true,
            CanStopPatternsAllowed = false,
            CanToggleTriggersAllowed = false,
        };
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetOwnersPetSetup()
    {
        var pairPerms = new UserPairPermissions()
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

            ApplyRestraintSets = true,
            LockRestraintSets = true,
            MaxAllowedRestraintTime = new TimeSpan(3, 0, 0),
            UnlockRestraintSets = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            SitRequests = true,
            MotionRequests = true,
            AllRequests = false,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = true,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = true,
            MaxMoodleTime = new TimeSpan(3, 0, 0),
            AllowPermanentMoodles = false,
            AllowRemovingMoodles = true,

            CanToggleToyState = true,
            CanUseVibeRemote = true,
            CanToggleAlarms = true,
            CanSendAlarms = true,
            CanExecutePatterns = true,
            CanStopPatterns = true,
            CanToggleTriggers = false,

            InHardcore = false,
            DevotionalStatesForPair = false,
            AllowForcedFollow = false,
            AllowForcedSit = true,
            AllowForcedToStay = false,
            AllowBlindfold = true,
            AllowHidingChatBoxes = false,
            AllowHidingChatInput = false,
            AllowChatInputBlocking = false
        };
        var pairAccess = new UserEditAccessPermissions()
        {
            LiveChatGarblerActiveAllowed = true,
            LiveChatGarblerLockedAllowed = true,

            PermanentLocksAllowed = true,
            OwnerLocksAllowed = true,
            DevotionalLocksAllowed = false,

            ApplyGagsAllowed = true,
            LockGagsAllowed = true,
            MaxGagTimeAllowed = false,
            UnlockGagsAllowed = true,
            RemoveGagsAllowed = true,

            WardrobeEnabledAllowed = false,
            ItemAutoEquipAllowed = false,
            RestraintSetAutoEquipAllowed = false,
            ApplyRestraintSetsAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxAllowedRestraintTimeAllowed = false,
            UnlockRestraintSetsAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            SitRequestsAllowed = true,
            MotionRequestsAllowed = true,
            AliasRequestsAllowed = false,
            AllRequestsAllowed = false,

            MoodlesEnabledAllowed = false,
            AllowPositiveStatusTypesAllowed = true,
            AllowNegativeStatusTypesAllowed = true,
            AllowSpecialStatusTypesAllowed = true,
            PairCanApplyOwnMoodlesToYouAllowed = true,
            PairCanApplyYourMoodlesToYouAllowed = true,
            MaxMoodleTimeAllowed = false,
            AllowPermanentMoodlesAllowed = true,
            AllowRemovingMoodlesAllowed = false,

            ToyboxEnabledAllowed = true,
            LockToyboxUIAllowed = true,
            SpatialVibratorAudioAllowed = true,
            CanToggleToyStateAllowed = true,
            CanUseVibeRemoteAllowed = true,
            CanToggleAlarmsAllowed = true,
            CanSendAlarmsAllowed = true,
            CanExecutePatternsAllowed = true,
            CanStopPatternsAllowed = true,
            CanToggleTriggersAllowed = true,
        };
        return new(pairPerms, pairAccess);
    }

    private Tuple<UserPairPermissions, UserEditAccessPermissions> PresetOwnersSlaveSetup()
    {
        var pairPerms = new UserPairPermissions()
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

            ApplyRestraintSets = true,
            LockRestraintSets = true,
            MaxAllowedRestraintTime = new TimeSpan(12, 0, 0),
            UnlockRestraintSets = true,
            RemoveRestraintSets = true,

            TriggerPhrase = "",
            StartChar = '(',
            EndChar = ')',
            SitRequests = true,
            MotionRequests = true,
            AllRequests = false,

            AllowPositiveStatusTypes = true,
            AllowNegativeStatusTypes = true,
            AllowSpecialStatusTypes = true,
            PairCanApplyOwnMoodlesToYou = false,
            PairCanApplyYourMoodlesToYou = true,
            MaxMoodleTime = new TimeSpan(12, 0, 0),
            AllowPermanentMoodles = true,
            AllowRemovingMoodles = true,

            CanToggleToyState = true,
            CanUseVibeRemote = true,
            CanToggleAlarms = true,
            CanSendAlarms = true,
            CanExecutePatterns = true,
            CanStopPatterns = true,
            CanToggleTriggers = true,

            InHardcore = false,
            DevotionalStatesForPair = false,
            AllowForcedFollow = true,
            AllowForcedSit = true,
            AllowForcedToStay = true,
            AllowBlindfold = true,
            AllowHidingChatBoxes = false,
            AllowHidingChatInput = false,
            AllowChatInputBlocking = false
        };
        var pairAccess = new UserEditAccessPermissions()
        {
            LiveChatGarblerActiveAllowed = true,
            LiveChatGarblerLockedAllowed = true,

            PermanentLocksAllowed = true,
            OwnerLocksAllowed = true,
            DevotionalLocksAllowed = true,

            ApplyGagsAllowed = true,
            LockGagsAllowed = true,
            MaxGagTimeAllowed = true,
            UnlockGagsAllowed = true,
            RemoveGagsAllowed = true,

            WardrobeEnabledAllowed = false,
            ItemAutoEquipAllowed = true,
            RestraintSetAutoEquipAllowed = true,
            ApplyRestraintSetsAllowed = true,
            LockRestraintSetsAllowed = true,
            MaxAllowedRestraintTimeAllowed = true,
            UnlockRestraintSetsAllowed = true,
            RemoveRestraintSetsAllowed = true,

            PuppeteerEnabledAllowed = false,
            SitRequestsAllowed = true,
            MotionRequestsAllowed = true,
            AllRequestsAllowed = true,

            MoodlesEnabledAllowed = false,
            AllowPositiveStatusTypesAllowed = true,
            AllowNegativeStatusTypesAllowed = true,
            AllowSpecialStatusTypesAllowed = true,
            PairCanApplyOwnMoodlesToYouAllowed = true,
            PairCanApplyYourMoodlesToYouAllowed = true,
            MaxMoodleTimeAllowed = true,
            AllowPermanentMoodlesAllowed = true,
            AllowRemovingMoodlesAllowed = true,

            ToyboxEnabledAllowed = true,
            LockToyboxUIAllowed = true,
            SpatialVibratorAudioAllowed = true,
            CanToggleToyStateAllowed = true,
            CanUseVibeRemoteAllowed = true,
            CanToggleAlarmsAllowed = true,
            CanSendAlarmsAllowed = true,
            CanExecutePatternsAllowed = true,
            CanStopPatternsAllowed = true,
            CanToggleTriggersAllowed = true,
        };
        return new(pairPerms, pairAccess);
    }
}
