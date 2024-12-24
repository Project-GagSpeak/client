using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Extensions;

namespace GagSpeak.PlayerData.Services;

// A class to help with callbacks received from the server.
public class ClientCallbackService
{
    private readonly ILogger<ClientCallbackService> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ClientData _playerData;
    private readonly WardrobeHandler _wardrobeHandler;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;
    private readonly GagManager _gagManager;
    private readonly IpcManager _ipcManager;
    private readonly IpcFastUpdates _ipcFastUpdates;
    private readonly AppearanceManager _appearanceManager;
    private readonly ToyboxManager _toyboxManager;

    public ClientCallbackService(ILogger<ClientCallbackService> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientData playerData, WardrobeHandler wardrobeHandler,
        PairManager pairManager, GagManager gagManager, IpcManager ipcManager, IpcFastUpdates ipcFastUpdates,
        AppearanceManager appearanceManager, ToyboxManager toyboxManager)
    {
        _logger = logger;
        _mediator = mediator;
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _wardrobeHandler = wardrobeHandler;
        _pairManager = pairManager;
        _gagManager = gagManager;
        _ipcManager = ipcManager;
        _ipcFastUpdates = ipcFastUpdates;
        _appearanceManager = appearanceManager;
        _toyboxManager = toyboxManager;
    }

    public bool ShockCodePresent => _playerData.CoreDataNull && _playerData.GlobalPerms!.GlobalShockShareCode.IsNullOrEmpty();
    public string GlobalPiShockShareCode => _playerData.GlobalPerms!.GlobalShockShareCode;
    public void SetGlobalPerms(UserGlobalPermissions perms) => _playerData.GlobalPerms = perms;
    public void SetAppearanceData(CharaAppearanceData appearanceData) => _playerData.AppearanceData = appearanceData;
    public void ApplyGlobalPerm(UserGlobalPermChangeDto dto) => _playerData.ApplyGlobalPermChange(dto, _pairManager);
    private bool CanDoWardrobeInteract() => !_playerData.CoreDataNull && _playerData.GlobalPerms!.WardrobeEnabled && _playerData.GlobalPerms.RestraintSetAutoEquip;

    #region IPC Callbacks
    public async void ApplyStatusesByGuid(ApplyMoodlesByGuidDto dto)
    {
        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == dto.User.UID);
        if (matchedPair is null)
        {
            _logger.LogError("Received Update by player is no longer present.");
            return;
        }
        if (!matchedPair.IsVisible)
        {
            _logger.LogError("Received Update by player is no longer visible.");
            return;
        }
        await _ipcManager.Moodles.ApplyOwnStatusByGUID(dto.Statuses).ConfigureAwait(false);
        // Log the Interaction Event.
        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ApplyOwnMoodle, "Moodle Status(s) Applied")));
    }

    public async void ApplyStatusesToSelf(ApplyMoodlesByStatusDto dto, string clientPlayerNameWithWorld)
    {
        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == dto.User.UID);
        if (matchedPair is null)
        {
            _logger.LogError("Pair Not Found.");
            return;
        }
        if (matchedPair.PlayerNameWithWorld.IsNullOrEmpty())
        {
            _logger.LogError("Received Update by player is no longer present.");
            return;
        }
        await _ipcManager.Moodles.ApplyStatusesFromPairToSelf(matchedPair.PlayerNameWithWorld, clientPlayerNameWithWorld, dto.Statuses).ConfigureAwait(false);
        // Log the Interaction Event.
        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ApplyPairMoodle, "Pair's Moodle Status(s) Applied")));
    }

    public async void RemoveStatusesFromSelf(RemoveMoodlesDto dto)
    {
        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == dto.User.UID);
        if (matchedPair is null)
        {
            _logger.LogError("Pair not found.");
            return;
        }
        if (!matchedPair.IsVisible)
        {
            _logger.LogError("Received Update by player is no longer visible.");
            return;
        }
        await _ipcManager.Moodles.RemoveOwnStatusByGuid(dto.Statuses).ConfigureAwait(false);
        // Log the Interaction Event.
        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.RemoveMoodle, "Moodle Status Removed")));
    }

    public async void ClearStatusesFromSelf(UserDto dto)
    {
        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == dto.User.UID);
        if (matchedPair is null)
        {
            _logger.LogError("Pair not found.");
            return;
        }
        if (!matchedPair.IsVisible)
        {
            _logger.LogError("Received Update by player is no longer visible.");
            return;
        }
        if (!matchedPair.OwnPerms.AllowRemovingMoodles)
        {
            _logger.LogError("Kinkster " + dto.User.UID + " tried to clear your moodles but you haven't given them the right!");
            return;
        }
        await _ipcManager.Moodles.ClearStatusAsync().ConfigureAwait(false);
        // Log the Interaction Event.
        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ClearMoodle, "Moodles Cleared")));
    }
    #endregion IPC Callbacks


    public async void CallbackAppearanceUpdate(OnlineUserCharaAppearanceDataDto callbackDto, bool callbackWasFromSelf)
    {
        if (_playerData.CoreDataNull) return;

        var currentGagSlot = _playerData.AppearanceData!.GagSlots[(int)callbackDto.UpdatedLayer];
        var callbackGagSlot = callbackDto.AppearanceData.GagSlots[(int)callbackDto.UpdatedLayer];

        if (callbackWasFromSelf)
        {
            if (callbackDto.Type is GagUpdateType.GagApplied)
                _logger.LogDebug("SelfApplied GAG APPLY Verified by Server Callback.", LoggerType.Callbacks);

            if (callbackDto.Type is GagUpdateType.GagLocked)
            {
                _logger.LogDebug("SelfApplied GAG LOCK Verified by Server Callback.", LoggerType.Callbacks);
                // update the lock information after our callback.
                _gagManager.LockGag(callbackDto.UpdatedLayer, callbackGagSlot.Padlock.ToPadlock(), callbackGagSlot.Password, callbackGagSlot.Timer, callbackDto.User.UID);
            }

            if (callbackDto.Type is GagUpdateType.GagUnlocked)
            {
                _logger.LogDebug("SelfApplied GAG UNLOCK Verified by Server Callback.", LoggerType.Callbacks);
                // unlock the padlock.
                _gagManager.UnlockGag(callbackDto.UpdatedLayer);

                // If the gagType is not none, 
                if (callbackDto.AppearanceData.GagSlots[(int)callbackDto.UpdatedLayer].GagType.ToGagType() is not GagType.None)
                {
                    // This means the gag is still applied, so we should see if we want to auto remove it.
                    _logger.LogDebug("Gag is still applied. Previous Padlock was: [" + callbackDto.PreviousPadlock.ToName() + "]", LoggerType.Callbacks);
                    if (_clientConfigs.GagspeakConfig.RemoveGagUponLockExpiration && callbackDto.PreviousPadlock.IsTimerLock())
                        await _appearanceManager.GagRemoved(callbackDto.UpdatedLayer, currentGagSlot.GagType.ToGagType(), isSelfApplied: true);
                }
            }

            if (callbackDto.Type is GagUpdateType.GagRemoved)
                _logger.LogDebug("SelfApplied GAG DISABLED Verified by Server Callback.", LoggerType.Callbacks);

            return;
        }

        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == callbackDto.User.UID);
        if (matchedPair == null)
        {
            _logger.LogError("Received Update by player is no longer present.");
            return;
        }

        _logger.LogDebug("Callback State: " + callbackDto.Type + " | Callback Layer: " + callbackDto.UpdatedLayer + " | Callback GagType: " + callbackGagSlot.GagType
            + " | Current GagType: " + currentGagSlot.GagType.ToGagType(), LoggerType.Callbacks);

        // let's start handling the cases. For starters, if the NewState is apply..
        if (callbackDto.Type is GagUpdateType.GagApplied)
        {
            // handle the case where we need to reapply, then...
            if (_playerData.AppearanceData!.GagSlots[(int)callbackDto.UpdatedLayer].GagType.ToGagType() != GagType.None)
            {
                _logger.LogDebug("Gag is already applied. Swapping Gag.", LoggerType.Callbacks);
                await _appearanceManager.GagSwapped(callbackDto.UpdatedLayer, currentGagSlot.GagType.ToGagType(), callbackGagSlot.GagType.ToGagType(), isSelfApplied: false, publish: false);
                // Log the Interaction Event.
                _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.SwappedGag, "Gag Swapped on " + callbackDto.UpdatedLayer)));
                return;
            }
            else
            {
                // Apply Gag
                _logger.LogDebug("Applying Gag to Character Appearance.", LoggerType.Callbacks);
                await _appearanceManager.GagApplied(callbackDto.UpdatedLayer, callbackGagSlot.GagType.ToGagType(), isSelfApplied: false, publishApply: false);
                // Log the Interaction Event.
                _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ApplyGag, callbackGagSlot.GagType + " applied to " + callbackDto.UpdatedLayer)));
                return;
            }
        }
        else if (callbackDto.Type is GagUpdateType.GagLocked)
        {
            _logger.LogTrace("A Padlock has been applied that will expire in : " + (callbackGagSlot.Timer - DateTime.UtcNow).TotalSeconds, LoggerType.Callbacks);
            _gagManager.LockGag(callbackDto.UpdatedLayer, callbackGagSlot.Padlock.ToPadlock(), callbackGagSlot.Password, callbackGagSlot.Timer, callbackDto.User.UID);
            // Log the Interaction Event.
            _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.LockGag, callbackGagSlot.GagType + " locked on " + callbackDto.UpdatedLayer)));
            return;
        }
        else if (callbackDto.Type is GagUpdateType.GagUnlocked)
        {
            _gagManager.UnlockGag(callbackDto.UpdatedLayer);
            // Log the Interaction Event.
            _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.UnlockGag, callbackGagSlot.GagType + " unlocked on " + callbackDto.UpdatedLayer)));
            return;
        }
        else if (callbackDto.Type is GagUpdateType.GagRemoved)
        {
            await _appearanceManager.GagRemoved(callbackDto.UpdatedLayer, currentGagSlot.GagType.ToGagType(), isSelfApplied: false, publishRemoval: false);
            // Log the Interaction Event.
            _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.RemoveGag, "Removed Gag from " + callbackDto.UpdatedLayer)));
            return;
        }
    }

    public async void CallbackWardrobeUpdate(OnlineUserCharaWardrobeDataDto callbackDto, bool callbackWasFromSelf)
    {
        var data = callbackDto.WardrobeData;
        int callbackSetIdx = _clientConfigs.GetSetIdxByGuid(data.ActiveSetId);
        RestraintSet? callbackSet = null;
        if (callbackSetIdx is not -1) callbackSet = _clientConfigs.GetRestraintSet(callbackSetIdx);

        if (callbackWasFromSelf)
        {
            if (callbackDto.Type is WardrobeUpdateType.RestraintApplied)
                _logger.LogDebug("SelfApplied RESTRAINT APPLY Verified by Server Callback.", LoggerType.Callbacks);
            if (callbackDto.Type is WardrobeUpdateType.RestraintLocked)
                _logger.LogDebug("SelfApplied RESTRAINT LOCKED Verified by Server Callback.", LoggerType.Callbacks);
            if (callbackDto.Type is WardrobeUpdateType.RestraintUnlocked)
            {
                _logger.LogDebug("SelfApplied RESTRAINT UNLOCK Verified by Server Callback.", LoggerType.Callbacks);
                // fire trigger if valid
                Guid activeSetId = _clientConfigs.GetActiveSet()?.RestraintId ?? Guid.Empty;
                if (!activeSetId.IsEmptyGuid())
                {
                    if (callbackDto.PreviousLock.IsTimerLock() && _clientConfigs.GagspeakConfig.DisableSetUponUnlock)
                    {
                        await _wardrobeHandler.DisableRestraintSet(activeSetId, MainHub.UID, true);
                    }
                }
                _gagManager.UpdateRestraintLockSelections(false);
            }

            if (callbackDto.Type is WardrobeUpdateType.RestraintDisabled)
                _logger.LogDebug("SelfApplied RESTRAINT DISABLED Verified by Server Callback.", LoggerType.Callbacks);

            return;
        }

        ////////// Callback was not from self past this point.

        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == callbackDto.User.UID);
        if (matchedPair is null)
        {
            _logger.LogError("Received Update by player is no longer present.");
            return;
        }

        if (!CanDoWardrobeInteract())
        {
            _logger.LogError("Player does not have permission to update their own wardrobe.");
            return;
        }

        try
        {
            switch (callbackDto.Type)
            {
                case WardrobeUpdateType.RestraintApplied:
                    // Check to see if we need to reapply.
                    var activeSet = _clientConfigs.GetActiveSet();
                    if (activeSet is not null && callbackSet is not null)
                    {
                        await _appearanceManager.RestraintSwapped(callbackSet.RestraintId, callbackDto.User.UID, publish: false);
                        _logger.LogDebug($"{callbackDto.User.UID} has swapped your [{activeSet.Name}] restraint set to another set!", LoggerType.Callbacks);
                        // Log the Interaction Event
                        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.SwappedRestraint, "Swapped Set to: " + _clientConfigs.GetSetNameByGuid(data.ActiveSetId))));
                    }
                    else
                    {
                        if (callbackSet is not null)
                        {
                            _logger.LogDebug($"{callbackDto.User.UID} has forcibly applied one of your restraint sets!", LoggerType.Callbacks);
                            await _wardrobeHandler.EnableRestraintSet(callbackSet.RestraintId, callbackDto.User.UID, pushToServer: false);
                            // Log the Interaction Event
                            _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ApplyRestraint, "Applied Set: " + _clientConfigs.GetSetNameByGuid(data.ActiveSetId))));
                        }
                    }
                    break;

                case WardrobeUpdateType.RestraintLocked:
                    if (callbackSet is not null)
                    {
                        _logger.LogDebug($"{callbackDto.User.UID} has locked your active restraint set!", LoggerType.Callbacks);
                        await _appearanceManager.LockRestraintSet(callbackSet.RestraintId, data.Padlock.ToPadlock(), data.Password, data.Timer, callbackDto.User.UID, false);
                        // Log the Interaction Event
                        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.LockRestraint, _clientConfigs.GetSetNameByGuid(data.ActiveSetId) + " is now locked")));
                    }
                    break;

                case WardrobeUpdateType.RestraintUnlocked:
                    if (callbackSet != null)
                    {
                        _logger.LogDebug($"{callbackDto.User.UID} has unlocked your active restraint set!", LoggerType.Callbacks);
                        Padlocks previousPadlock = callbackSet.LockType.ToPadlock();
                        await _appearanceManager.UnlockRestraintSet(callbackSet.RestraintId, callbackDto.User.UID, false);
                        _gagManager.UpdateRestraintLockSelections(false);
                        // Log the Interaction Event
                        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.UnlockRestraint, _clientConfigs.GetSetNameByGuid(data.ActiveSetId) + " is now unlocked")));
                        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, callbackSet, previousPadlock, false, callbackDto.User.UID);
                    }
                    break;

                case WardrobeUpdateType.RestraintDisabled:
                    _logger.LogDebug($"{callbackDto.User.UID} has force disabled your restraint set!", LoggerType.Callbacks);
                    var currentlyActiveSet = _clientConfigs.GetActiveSet();
                    if (currentlyActiveSet is not null)
                    {
                        await _wardrobeHandler.DisableRestraintSet(currentlyActiveSet.RestraintId, callbackDto.User.UID, pushToServer: false);
                        // Log the Interaction Event
                        _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.RemoveRestraint, currentlyActiveSet.Name + " has been removed")));
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing Wardrobe Update.");
        }

    }

    public void CallbackAliasStorageUpdate(OnlineUserCharaAliasDataDto callbackDto)
    {
        // this call should only ever be used for updating the registered name of a pair. if used for any other purpose, log error.
        if (callbackDto.Type is PuppeteerUpdateType.PlayerNameRegistered)
        {
            _clientConfigs.UpdateAliasStoragePlayerInfo(callbackDto.User.UID, callbackDto.AliasData.ListenerName);
            _logger.LogDebug("Player Name Registered Successfully processed by Server!", LoggerType.Callbacks);
        }
        else if (callbackDto.Type is PuppeteerUpdateType.AliasListUpdated)
        {
            _logger.LogDebug("Alias List Update Success retrieved from Server for UID: " + callbackDto.User.UID, LoggerType.Callbacks);
        }
        else
        {
            _logger.LogError("Another Player should not be attempting to update your own alias list. Report this if you see it.", LoggerType.Callbacks);
            return;
        }
    }

    public void CallbackToyboxUpdate(OnlineUserCharaToyboxDataDto callbackDto, bool callbackFromSelf)
    {
        if (callbackFromSelf)
        {
            if (callbackDto.Type is ToyboxUpdateType.PatternExecuted)
                _logger.LogDebug("SelfApplied PATTERN EXECUTED Verified by Server Callback.", LoggerType.Callbacks);
            if (callbackDto.Type is ToyboxUpdateType.PatternStopped)
                _logger.LogDebug("SelfApplied PATTERN STOPPED Verified by Server Callback.", LoggerType.Callbacks);
            if (callbackDto.Type is ToyboxUpdateType.AlarmToggled)
                _logger.LogDebug("SelfApplied ALARM TOGGLED Verified by Server Callback.", LoggerType.Callbacks);
            if (callbackDto.Type is ToyboxUpdateType.TriggerToggled)
                _logger.LogDebug("SelfApplied TRIGGER TOGGLED Verified by Server Callback.", LoggerType.Callbacks);
            return;
        }

        // Verify who the pair was.
        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == callbackDto.User.UID);
        if (matchedPair is null || matchedPair.LastLightStorage is null)
        {
            _logger.LogError("Received Update by pair that you no longer have added.");
            return;
        }

        // Update Appearance without calling any events so we don't loop back to the server.
        Guid idAffected = callbackDto.ToyboxInfo.InteractionId;
        switch (callbackDto.Type)
        {
            case ToyboxUpdateType.PatternExecuted:
                // verify it actually exists in the list.
                var enableIdIsValid = _clientConfigs.PatternConfig.PatternStorage.Patterns.Any(x => x.UniqueIdentifier == idAffected);
                if (!enableIdIsValid)
                {
                    // Locate the pattern by the interactionGUID.
                    _logger.LogError("Tried to activate pattern but pattern does not exist? How is this even possible.");
                    return;
                }
                // if we are currently playing a pattern, stop it first.
                if (_clientConfigs.AnyPatternIsPlaying)
                {
                    _logger.LogDebug("Stopping currently playing pattern before executing new one.", LoggerType.Callbacks);
                    _toyboxManager.DisablePattern(_clientConfigs.ActivePatternGuid());
                }
                // execute the pattern.
                _toyboxManager.EnablePattern(idAffected, MainHub.UID);
                _logger.LogInformation("Pattern Executed by Server Callback.", LoggerType.Callbacks);
                _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ActivatePattern, "Pattern Enabled")));
                break;

            case ToyboxUpdateType.PatternStopped:
                // verify it actually exists in the list.
                var stopIdIsValid = _clientConfigs.PatternConfig.PatternStorage.Patterns.Any(x => x.UniqueIdentifier == idAffected);
                if (!stopIdIsValid)
                {
                    // Locate the pattern by the interactionGUID.
                    _logger.LogError("Tried to stop a pattern but pattern does not exist? How is this even possible.");
                    return;
                }
                // if no pattern is playing, log a warning and return.
                if (!_clientConfigs.AnyPatternIsPlaying)
                {
                    _logger.LogWarning("Tried to stop a pattern but no pattern is currently playing.", LoggerType.Callbacks);
                    return;
                }
                // stop the pattern.
                _toyboxManager.DisablePattern(idAffected);
                _logger.LogInformation("Pattern Stopped by Server Callback.", LoggerType.Callbacks);
                _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ActivatePattern, "Pattern Stopped")));
                break;

            case ToyboxUpdateType.AlarmToggled:
                // verify that this item actually exists.
                var alarm = _clientConfigs.AlarmConfig.AlarmStorage.Alarms.FirstOrDefault(x => x.Identifier == idAffected);
                if (alarm is null)
                {
                    // Locate the alarm by the interactionGUID.
                    _logger.LogError("Tried to toggle alarm but alarm does not exist? How is this even possible.");
                    return;
                }
                // grab the current state of the alarm.
                var curState = alarm.Enabled;
                // toggle the alarm.
                if (curState)
                {
                    _toyboxManager.DisableAlarm(idAffected);
                    _logger.LogInformation("Alarm Disabled by Server Callback.", LoggerType.Callbacks);
                    _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ToggleAlarm, "Alarm Disabled")));
                }
                else
                {
                    _toyboxManager.EnableAlarm(idAffected);
                    _logger.LogInformation("Alarm Enabled by Server Callback.", LoggerType.Callbacks);
                    _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ToggleAlarm, "Alarm Enabled")));
                }
                break;

            case ToyboxUpdateType.TriggerToggled:
                // verify that this item actually exists.
                var trigger = _clientConfigs.TriggerConfig.TriggerStorage.Triggers.FirstOrDefault(x => x.Identifier == idAffected);
                if (trigger is null)
                {
                    // Locate the trigger by the interactionGUID.
                    _logger.LogError("Tried to toggle trigger but trigger does not exist? How is this even possible.");
                    return;
                }
                // grab the current state of the trigger.
                var curTriggerState = trigger.Enabled;
                // toggle the trigger.
                if (curTriggerState)
                {
                    _toyboxManager.DisableTrigger(idAffected);
                    _logger.LogInformation("Trigger Disabled by Server Callback.", LoggerType.Callbacks);
                    _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ToggleTrigger, "Trigger Disabled")));
                }
                else
                {
                    _toyboxManager.EnableTrigger(idAffected, callbackDto.User.UID);
                    _logger.LogInformation("Trigger Enabled by Server Callback.", LoggerType.Callbacks);
                    _mediator.Publish(new EventMessage(new(matchedPair.GetNickAliasOrUid(), matchedPair.UserData.UID, InteractionType.ToggleTrigger, "Trigger Enabled")));
                }
                break;
        }
    }

    public void CallbackLightStorageUpdate(OnlineUserStorageUpdateDto update)
    {
        _logger.LogDebug("Light Storage Update received successfully from server!", LoggerType.Callbacks);
    }




}
