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
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;
    private readonly IpcManager _ipcManager;
    private readonly IpcFastUpdates _ipcFastUpdates;
    private readonly AppearanceManager _appearanceManager;
    private readonly ToyboxManager _toyboxManager;

    public ClientCallbackService(ILogger<ClientCallbackService> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientData playerData, PairManager pairManager, 
        IpcManager ipcManager, IpcFastUpdates ipcFastUpdates, AppearanceManager appearanceManager, 
        ToyboxManager toyboxManager)
    {
        _logger = logger;
        _mediator = mediator;
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _pairManager = pairManager;
        _ipcManager = ipcManager;
        _ipcFastUpdates = ipcFastUpdates;
        _appearanceManager = appearanceManager;
        _toyboxManager = toyboxManager;
    }

    public bool ShockCodePresent => _playerData.GlobalPerms is not null && _playerData.GlobalPerms.GlobalShockShareCode.IsNullOrEmpty();
    public string GlobalPiShockShareCode => _playerData.GlobalPerms!.GlobalShockShareCode;
    public void SetGlobalPerms(UserGlobalPermissions perms) => _playerData.GlobalPerms = perms;
    public void SetAppearanceData(CharaAppearanceData appearanceData) => _playerData.AppearanceData = appearanceData;
    public void ApplyGlobalPerm(UserGlobalPermChangeDto dto) => _playerData.ApplyGlobalPermChange(dto, _pairManager.DirectPairs.FirstOrDefault(x => x.UserData.UID == dto.Enactor.UID));

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


    public async void CallbackAppearanceUpdate(OnlineUserCharaAppearanceDataDto callbackDto)
    {
        if (_playerData.AppearanceData is null)
            return;

        var currentGagSlot = _playerData.AppearanceData.GagSlots[(int)callbackDto.UpdatedLayer];
        var cbSlot = callbackDto.AppearanceData.GagSlots[(int)callbackDto.UpdatedLayer];

        _logger.LogTrace("Callback State: " + callbackDto.Type + " | Callback Layer: " + callbackDto.UpdatedLayer + " | Callback GagType: " + cbSlot.GagType
            + " | Current GagType: " + currentGagSlot.GagType.ToGagType(), LoggerType.Callbacks);

        if (callbackDto.Enactor.UID == MainHub.UID)
        {
            // handle callbacks made from ourselves.
            if (callbackDto.Type is GagUpdateType.GagApplied)
                _logger.LogDebug("SelfApplied GAG APPLY Verified by Server Callback.", LoggerType.Callbacks);
            else if (callbackDto.Type is GagUpdateType.GagLocked)
                _logger.LogDebug("SelfApplied GAG LOCK Verified by Server Callback.", LoggerType.Callbacks);
            else if (callbackDto.Type is GagUpdateType.GagUnlocked)
            {
                _logger.LogDebug("SelfApplied GAG UNLOCK Verified by Server Callback.", LoggerType.Callbacks);
                // If the gagType is not none,
                if (callbackDto.AppearanceData.GagSlots[(int)callbackDto.UpdatedLayer].GagType.ToGagType() is not GagType.None)
                {
                    // This means the gag is still applied, so we should see if we want to auto remove it.
                    _logger.LogDebug("Gag is still applied. Previous Padlock was: [" + callbackDto.PreviousPadlock.ToName() + "]", LoggerType.Callbacks);
                    if ((_clientConfigs.GagspeakConfig.RemoveGagUponLockExpiration && callbackDto.PreviousPadlock.IsTimerLock()) || callbackDto.PreviousPadlock is Padlocks.MimicPadlock)
                        await _appearanceManager.GagRemoved(callbackDto.UpdatedLayer, callbackDto.Enactor.UID, true, false);
                }
            }
            else if (callbackDto.Type is GagUpdateType.GagRemoved)
                _logger.LogDebug("SelfApplied GAG DISABLED Verified by Server Callback.", LoggerType.Callbacks);

            return;
        }
        else
        {
            // handle callbacks made from other pairs.
            if (callbackDto.Type is GagUpdateType.GagApplied)
            {
                _logger.LogDebug("GAG APPLIED callback from server recieved. is already applied. Swapping Gag.", LoggerType.Callbacks);
                await _appearanceManager.SwapOrApplyGag(callbackDto.UpdatedLayer, cbSlot.GagType.ToGagType(), callbackDto.Enactor.UID, true);
                // log interaction event if it is from a pair.
                if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                    _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.ApplyGag, cbSlot.GagType + " swapped/applied to " + callbackDto.UpdatedLayer)));
            }
            else if (callbackDto.Type is GagUpdateType.GagLocked)
            {
                _logger.LogDebug("GAG LOCKED callback recieved from server. Expires in : " + (cbSlot.Timer - DateTime.UtcNow).TotalSeconds, LoggerType.Callbacks);
                _appearanceManager.GagLocked(callbackDto.UpdatedLayer, cbSlot.Padlock.ToPadlock(), cbSlot.Password, cbSlot.Timer.GetEndTimeOffsetString(), callbackDto.Enactor.UID, true, true);
                // Log the Interaction Event.
                if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                    _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.LockGag, cbSlot.GagType + " locked on " + callbackDto.UpdatedLayer)));
            }
            else if (callbackDto.Type is GagUpdateType.GagUnlocked)
            {
                _logger.LogDebug("GAG UNLOCKED callback from server recieved.", LoggerType.Callbacks);
                var currentPass = _playerData.AppearanceData?.GagSlots[(int)callbackDto.UpdatedLayer].Password ?? string.Empty;
                _appearanceManager.GagUnlocked(callbackDto.UpdatedLayer, currentPass, callbackDto.Enactor.UID, true, true);
                // Log the Interaction Event.
                if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                    _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.UnlockGag, cbSlot.GagType + " unlocked on " + callbackDto.UpdatedLayer)));
            }
            else if (callbackDto.Type is GagUpdateType.GagRemoved)
            {
                await _appearanceManager.GagRemoved(callbackDto.UpdatedLayer, callbackDto.Enactor.UID, true, true);
                // Log the Interaction Event.
                if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                    _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.RemoveGag, "Removed Gag from " + callbackDto.UpdatedLayer)));
            }
        }
    }

    public async void CallbackWardrobeUpdate(OnlineUserCharaWardrobeDataDto callbackDto)
    {
        var data = callbackDto.WardrobeData;

        if (callbackDto.Enactor.UID == MainHub.UID)
        {
            // handle callbacks made from ourselves.
            if (callbackDto.Type is WardrobeUpdateType.RestraintApplied)
                _logger.LogDebug("SelfApplied RESTRAINT APPLY Verified by Server Callback.", LoggerType.Callbacks);
            else if (callbackDto.Type is WardrobeUpdateType.RestraintLocked)
                _logger.LogDebug("SelfApplied RESTRAINT LOCK Verified by Server Callback.", LoggerType.Callbacks);
            else if (callbackDto.Type is WardrobeUpdateType.RestraintUnlocked)
            {
                _logger.LogDebug("SelfApplied RESTRAINT UNLOCK Verified by Server Callback.", LoggerType.Callbacks);
                if(_clientConfigs.TryGetActiveSet(out var activeSet))
                {
                    if ((_clientConfigs.GagspeakConfig.DisableSetUponUnlock && callbackDto.AffectedItem.ToPadlock().IsTimerLock()))
                        await _appearanceManager.DisableRestraintSet(activeSet.RestraintId, MainHub.UID, true, false);
                }
            }
            else if (callbackDto.Type is WardrobeUpdateType.RestraintDisabled)
                _logger.LogDebug("SelfApplied RESTRAINT DISABLED Verified by Server Callback.", LoggerType.Callbacks);
        }
        else
        {
            // handle callbacks made from other pairs.
            if (callbackDto.Type is WardrobeUpdateType.RestraintApplied)
            {
                _logger.LogDebug("RESTRAINT APPLY Verified by Server Callback.", LoggerType.Callbacks);
                await _appearanceManager.SwapOrApplyRestraint(data.ActiveSetId, callbackDto.Enactor.UID, false);
                // Log the Interaction Event.
                if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                    _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.SwappedRestraint, "Applied/Swapped Set to: " + _clientConfigs.GetSetNameByGuid(data.ActiveSetId))));
            }
            else if (callbackDto.Type is WardrobeUpdateType.RestraintLocked)
            {
                _logger.LogDebug("RESTRAINT LOCK Verified by Server Callback. With '" + data.Timer.GetEndTimeOffsetString() + "' remaining", LoggerType.Callbacks);
                _appearanceManager.LockRestraintSet(data.ActiveSetId, data.Padlock.ToPadlock(), data.Password, data.Timer.GetEndTimeOffsetString(), callbackDto.Enactor.UID, true, true);
                // Log the Interaction Event.
                if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                    _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.LockRestraint, "Locked Set: " + _clientConfigs.GetSetNameByGuid(data.ActiveSetId))));
            }
            else if (callbackDto.Type is WardrobeUpdateType.RestraintUnlocked)
            {
                // get the active item to obtain the password, as we already have valid access to unlock it.
                if (_clientConfigs.TryGetActiveSet(out var activeSet))
                {
                    _logger.LogDebug("RESTRAINT UNLOCK Verified by Server Callback.", LoggerType.Callbacks);
                    _appearanceManager.UnlockRestraintSet(data.ActiveSetId, activeSet.Password, callbackDto.Enactor.UID, true, true);
                    // Log the Interaction Event
                    if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                        _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.UnlockRestraint, _clientConfigs.GetSetNameByGuid(data.ActiveSetId) + " is now unlocked")));
                }
                else
                {
                    _logger.LogError("For some reason your set was not active when you received this... desync?");
                    return;
                }
            }
            else if (callbackDto.Type is WardrobeUpdateType.RestraintDisabled)
            {
                _logger.LogDebug($"{callbackDto.User.UID} has force disabled your restraint set!", LoggerType.Callbacks);
                if (_clientConfigs.TryGetActiveSet(out var activeSet))
                {
                    _logger.LogDebug($"{callbackDto.User.UID} has force disabled your restraint set!", LoggerType.Callbacks);
                    await _appearanceManager.DisableRestraintSet(activeSet.RestraintId, callbackDto.Enactor.UID, true, true);
                    // Log the Interaction Event.
                    if (_pairManager.TryGetNickAliasOrUid(callbackDto.Enactor.UID, out var nick))
                        _mediator.Publish(new EventMessage(new(nick, callbackDto.Enactor.UID, InteractionType.RemoveRestraint, _clientConfigs.GetSetNameByGuid(Guid.Parse(callbackDto.AffectedItem)) + " has been removed")));
                }
            }
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
