using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI;

// This section of the MainHub focuses on responses received by the Server.
// We use this to perform actions to our client's data.
public partial class MainHub
{
    #region Pairing & Messages
    /// <summary> 
    ///     Called when the server sends a message to the client.
    /// </summary>
    public Task Callback_ServerMessage(MessageSeverity messageSeverity, string message)
    {
        if (messageSeverity == MessageSeverity.Information && _suppressNextNotification)
        {
            _suppressNextNotification = false;
            return Task.CompletedTask;
        }

        var (title, type) = messageSeverity switch
        {
            MessageSeverity.Error => ($"Error from {MAIN_SERVER_NAME}", NotificationType.Error),
            MessageSeverity.Warning => ($"Warning from {MAIN_SERVER_NAME}", NotificationType.Warning),
            _ => ($"Info from {MAIN_SERVER_NAME}", NotificationType.Info),
        };

        Mediator.Publish(new NotificationMessage(title, message, type, TimeSpan.FromSeconds(7.5)));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Sometimes Corby just wants to do a little bullying.
    /// </summary>
    public Task Callback_HardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState newServerState)
    {
        if (messageSeverity == MessageSeverity.Information && _suppressNextNotification)
            _suppressNextNotification = false;
        else
        {
            var (title, type, duration) = messageSeverity switch
            {
                MessageSeverity.Error => ($"Error from {MAIN_SERVER_NAME}", NotificationType.Error, 7.5),
                MessageSeverity.Warning => ($"Warning from {MAIN_SERVER_NAME}", NotificationType.Warning, 7.5),
                _ => ($"Info from {MAIN_SERVER_NAME}", NotificationType.Info, 5.0),
            };
            Mediator.Publish(new NotificationMessage(title, message, type, TimeSpan.FromSeconds(duration)));
        }

        // we need to update the api server state to be stopped if connected
        if (IsConnected)
        {
            _ = Task.Run(async () =>
            {
                // pause the server state
                _config.SetPauseState(true);
                _suppressNextNotification = true;
                // If forcing a hard reconnect, fully unload the client & their kinksters.
                await Disconnect(ServerState.Disconnected, DisconnectIntent.Reload).ConfigureAwait(false);
                // Clear our token cache between, incase we were banned.
                _tokenProvider.ResetTokenCache();
                // Revert full pause status and create a new connection.
                _config.SetPauseState(false);
                _suppressNextNotification = true;

                await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

                await Connect().ConfigureAwait(false);
            });
        }
        // return completed
        return Task.CompletedTask;
    }

    public Task Callback_ServerInfo(ServerInfoResponse serverInfo)
    {
        _serverInfo = serverInfo;
        return Task.CompletedTask;
    }

    public Task Callback_AddClientPair(KinksterPair dto)
    {
        Logger.LogDebug($"Callback_AddClientPair: {dto}", LoggerType.Callbacks);
        Generic.Safe(() =>
        {
            _kinksters.AddKinkster(dto);
            // we just added a pair, so ping the achievement manager that a pair was added!
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PairAdded);
        });
        return Task.CompletedTask;
    }

    public Task Callback_RemoveClientPair(KinksterBase dto)
    {
        Logger.LogDebug($"Callback_AddClientPair: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.RemoveKinkster(dto));
        return Task.CompletedTask;
    }

    public Task Callback_AddPairRequest(KinksterRequest dto)
    {
        Logger.LogDebug($"Callback_AddPairRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _requests.AddNewRequest(dto));
        return Task.CompletedTask;
    }

    public Task Callback_RemovePairRequest(KinksterRequest dto)
    {
        Logger.LogDebug($"Callback_RemovePairRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _requests.RemoveRequest(dto));
        return Task.CompletedTask;
    }

    public Task Callback_AddCollarRequest(CollarRequest dto)
    {
        Logger.LogDebug($"Callback_AddCollarRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _collarManager.AddRequest(dto));
        return Task.CompletedTask;
    }

    public Task Callback_RemoveCollarRequest(CollarRequest dto)
    {
        Logger.LogDebug($"Callback_RemoveCollarRequest: {dto}", LoggerType.Callbacks);
        Generic.Safe(() => _collarManager.RemoveRequest(dto));
        return Task.CompletedTask;
    }

    #endregion Pairing & Messages

    #region Moodles
    public Task Callback_MoodleDataUpdated(MoodlesDataUpdate dto)
    {
        Logger.LogDebug($"Callback_MoodleDataUpdated: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.ReceiveMoodleData(dto.User, dto.NewData));
        return Task.CompletedTask;
    }
    public Task Callback_MoodleSMUpdated(MoodlesSMUpdate dto)
    {
        Logger.LogDebug($"Callback_MoodleSMUpdated: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.ReceiveSMUpdate(dto.User, dto.DataString, dto.DataInfo));
        return Task.CompletedTask;
    }
    public Task Callback_MoodleStatusesUpdate(MoodlesStatusesUpdate dto)
    {
        Logger.LogDebug($"Callback_MoodleStatusesUpdate: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.ReceiveMoodleStatuses(dto.User, dto.Statuses));
        return Task.CompletedTask;
    }
    public Task Callback_MoodlePresetsUpdate(MoodlesPresetsUpdate dto)
    {
        Logger.LogDebug($"Callback_SetKinksterMoodlesPresets: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.ReceiveMoodlePresets(dto.User, dto.Presets));
        return Task.CompletedTask;
    }

    public Task Callback_MoodleStatusModified(MoodlesStatusModified dto)
    {
        Logger.LogDebug($"Callback_MoodleStatusesUpdate: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.ReceiveMoodleStatusUpdate(dto.User, dto.Status, dto.Deleted));
        return Task.CompletedTask;
    }
    public Task Callback_MoodlePresetModified(MoodlesPresetModified dto)
    {
        Logger.LogDebug($"Callback_SetKinksterMoodlesPresets: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.ReceiveMoodlePresetUpdate(dto.User, dto.Preset, dto.Deleted));
        return Task.CompletedTask;
    }

    public async Task Callback_ApplyMoodlesByGuid(ApplyMoodleId dto)
    {
        Logger.LogDebug($"Callback_ApplyMoodlesById: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        // Fail if not a valid pair or not rendered.
        if (_kinksters.GetUserOrDefault(dto.User) is not { } pair)
            Logger.LogWarning($"Received ApplyMoodlesById from an unpaired user: {dto.User.AliasOrUID}");
        else if (!pair.IsRendered)
            Logger.LogWarning($"Received ApplyMoodlesById from an unrendered kinkster: {dto.User.AliasOrUID}");
        else
        {
            // Could maybe make the dto send if they want them to be locked or not? Idk, but its possible if we want.
            await _moodles.ApplyOwnStatus(dto.Ids).ConfigureAwait(false);
        }
    }

    public async Task Callback_ApplyMoodlesByStatus(ApplyMoodleStatus dto)
    {
        Logger.LogDebug("Callback_ApplyMoodlesByStatus: " + dto, LoggerType.Callbacks);
        // Fail if not a valid pair.
        if (_kinksters.GetUserOrDefault(dto.User) is not { } pair)
            Logger.LogWarning($"Received ApplyMoodleTuples from an unpaired user: {dto.User.AliasOrUID}");
        else if (!pair.IsRendered)
            Logger.LogWarning($"Received ApplyMoodleTuples from an unrendered kinkster: {dto.User.AliasOrUID}");
        else
        {
            Mediator.Publish(new EventMessage(new(pair.GetNickAliasOrUid(), pair.UserData.UID, InteractionType.ApplyOtherStatus, "Applied by Pair.")));
            // Could maybe make the dto send if they want them to be locked or not? Idk, but its possible if we want.
            // Can't do this anymore
            // IpcProvider.ApplyStatusTuples(dto.Statuses);
        }
    }

    public async Task Callback_RemoveMoodles(RemoveMoodleId dto)
    {
        Logger.LogDebug($"Callback_RemoveMoodles: {dto.User.AliasOrUID}", LoggerType.Callbacks);
        // Fail if not a valid pair or not rendered.
        if (_kinksters.GetUserOrDefault(dto.User) is not { } pair)
            Logger.LogWarning($"Received RemoveMoodles from an unpaired user: {dto.User.AliasOrUID}");
        else if (!pair.IsRendered)
            Logger.LogWarning($"Received RemoveMoodles from an unrendered kinkster: {pair.GetNickAliasOrUid()}");
        else
        {
            // Could maybe make the dto send if they want them to be locked or not? Idk, but its possible if we want.
            await _moodles.RemoveOwnStatuses(dto.Ids).ConfigureAwait(false);
        }
    }

    public async Task Callback_ClearMoodles(KinksterBase dto)
    {
        Logger.LogInformation($"Callback_ClearMoodles: {dto.User.AliasOrUID}");
        // Fail if not a valid pair or not rendered.
        if (_kinksters.GetUserOrDefault(dto.User) is not { } pair)
            Logger.LogWarning($"Received ClearMoodles from an unpaired user: {dto.User.AliasOrUID}");
        else if (!pair.IsRendered)
            Logger.LogWarning($"Received ClearMoodles from an unrendered kinkster: {pair.GetNickAliasOrUid()}");
        else
        {
            // Could maybe make the dto send if they want them to be locked or not? Idk, but its possible if we want.
            await _moodles.RemoveOwnStatuses(MoodleCache.IpcData.DataInfo.Keys).ConfigureAwait(false);
        }
    }
    #endregion Moodles

    #region Pair Permission Exchange
    public Task Callback_BulkChangeGlobal(BulkChangeGlobal dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _clientDatListener.ChangeAllClientGlobals(dto.User, dto.NewPerms, dto.NewState));
            // handle soon.
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.PermBulkChangeGlobal(dto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_BulkChangeUnique(BulkChangeUnique dto)
    {
        Generic.Safe(() =>
        {
            if (dto.User.UID == UID)
                throw new Exception("Should never be calling a permission update for yourself in bulk, use BulkChangeAll for these!");

            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            if (dto.Direction is UpdateDir.Own)
                _kinksters.PermBulkChangeUniqueOwn(dto.User, dto.NewPerms, dto.NewAccess);
            else
                _kinksters.PermBulkChangeUniqueOther(dto.User, dto.NewPerms, dto.NewAccess);
        });
        return Task.CompletedTask;
    }

    public Task Callback_SingleChangeGlobal(SingleChangeGlobal dto)
    {
        if (dto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _clientDatListener.ChangeGlobalPerm(dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.PermChangeGlobal(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
        }
        return Task.CompletedTask;
    }

    public Task Callback_SingleChangeUnique(SingleChangeUnique dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.PermChangeUnique(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.PermChangeUniqueOther(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
    }

    public Task Callback_SingleChangeAccess(SingleChangeAccess dto)
    {
        if (dto.Direction is UpdateDir.Own)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.PermChangeAccess(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.PermChangeAccessOther(dto.Target, dto.Enactor, dto.NewPerm.Key, dto.NewPerm.Value));
            return Task.CompletedTask;
        }
    }

    public Task Callback_StateChangeHardcore(HardcoreStateChange dto)
    {
        if (dto.Target.UID == UID)
        {
            Logger.LogDebug($"[OWN-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _clientDatListener.ChangeHardcoreStatus(dto.Enactor, dto.Changed, dto.NewData));
        }
        else
        {
            Logger.LogDebug($"[OTHER-PERM-CHANGE]: {dto}", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.StateChangeHardcore(dto.Target, dto.Enactor, dto.Changed, dto.NewData));
        }
        return Task.CompletedTask;

    }
    #endregion Pair Permission Exchange

    /// <summary> Should only ever get the other pairs. If getting self, something is up. </summary>
    public Task Callback_KinksterUpdateComposite(KinksterUpdateComposite dto)
    {
        if (dto.User.UID != UID)
            Generic.Safe(() => _kinksters.NewActiveComposite(dto.User, dto.Data, dto.WasSafeword));

        return Task.CompletedTask;
    }

    // Invoked by other kinksters.
    public Task Callback_KinksterUpdateActiveGag(KinksterUpdateActiveGag dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-GAGS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    if (dataDto.PreviousGag is GagType.None)
                        _callbackHandler.ApplyGag(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    else
                        _callbackHandler.SwapGag(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _callbackHandler.LockGag(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor);
                    break;
                case DataUpdateType.Unlocked:
                    _callbackHandler.UnlockGag(dataDto.AffectedLayer, dataDto.Enactor);
                    break;
                case DataUpdateType.Removed:
                    _callbackHandler.RemoveGag(dataDto.AffectedLayer, dataDto.Enactor).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-GAGS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.NewActiveGags(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveRestriction(KinksterUpdateActiveRestriction dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-RESTRICTIONS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    if (dataDto.PreviousRestriction == Guid.Empty)
                        _callbackHandler.ApplyRestriction(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    else
                        _callbackHandler.SwapRestriction(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _callbackHandler.LockRestriction(dataDto.AffectedLayer, dataDto.NewData, dataDto.Enactor);
                    break;
                case DataUpdateType.Unlocked:
                    _callbackHandler.UnlockRestriction(dataDto.AffectedLayer, dataDto.Enactor);
                    break;
                case DataUpdateType.Removed:
                    _callbackHandler.RemoveRestriction(dataDto.AffectedLayer, dataDto.Enactor).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-RESTRICTIONS-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.NewActiveRestriction(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveRestraint(KinksterUpdateActiveRestraint dataDto)
    {
        // If the update is for us, handle it.
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-RESTRAINT-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            switch (dataDto.Type)
            {
                case DataUpdateType.Swapped:
                case DataUpdateType.Applied:
                    if (dataDto.PreviousRestraint == Guid.Empty)
                        _callbackHandler.ApplyRestraint(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    else
                        _callbackHandler.SwapRestraint(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.LayersChanged:
                    _callbackHandler.SwapRestraintLayers(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.LayersApplied:
                    _callbackHandler.ApplyRestraintLayers(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Locked:
                    _callbackHandler.LockRestraint(dataDto.NewData, dataDto.Enactor);
                    break;
                case DataUpdateType.Unlocked:
                    _callbackHandler.UnlockRestraint(dataDto.Enactor);
                    break;
                case DataUpdateType.LayersRemoved:
                    _callbackHandler.RemoveRestraintLayers(dataDto.NewData, dataDto.Enactor).ConfigureAwait(false);
                    break;
                case DataUpdateType.Removed:
                    _callbackHandler.RemoveRestraint(dataDto.Enactor).ConfigureAwait(false);
                    break;
            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-RESTRAINT-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.NewActiveRestraint(dataDto));
            return Task.CompletedTask;
        }
    }

    public Task Callback_KinksterUpdateActiveCollar(KinksterUpdateActiveCollar dataDto)
    {
        if (dataDto.User.UID == UID)
        {
            Logger.LogDebug($"[OWN-COLLAR-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);

            switch (dataDto.Type)
            {
                case DataUpdateType.RequestAccepted:
                    // handle an accepted request here.
                    break;
                case DataUpdateType.OwnersUpdated:
                    // update owners and things here.
                    break;
                case DataUpdateType.VisibilityChange:
                    // process a toggle to visibility. Change always will inflict a toggle.
                    break;
                case DataUpdateType.DyesChange:
                    // process a change to the active collar's dyes.
                    break;
                case DataUpdateType.CollarMoodleChange:
                    // process a change to the active collar's Moodles.
                    break;
                case DataUpdateType.CollarWritingChange:
                    // process a change to the collar's writing,
                    // and perhaps an enforced profile refresh?
                    break;
                case DataUpdateType.CollarRemoved:
                    // process collar removal.
                    break;

            }
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"[OTHER-COLLAR-ACTIVE]: {dataDto.User} ({dataDto.Type})", LoggerType.Callbacks);
            Generic.Safe(() => _kinksters.NewActiveCollar(dataDto));
            return Task.CompletedTask;
        }
    }

    public async Task Callback_KinksterChangeEnabledItem(KinksterChangeEnabledItem dto)
    {
        Logger.LogDebug($"KinksterChangeEnabledItem for {dto.User.AliasOrUID} (from {dto.Enactor.AliasOrUID})", LoggerType.Callbacks);
        if (dto.User.UID != UID)
        {
            Generic.Safe(() => _kinksters.UpdateItemState(dto.User, dto.Enactor, dto.Module, dto.ItemId, dto.NewState));
        }
        else
        {
            // For Patterns
            if (dto.Module is GSModule.Pattern)
            {
                await Generic.Safe(async () =>
                {
                    //var success = dto.Type switch
                    //{
                    //    DataUpdateType.PatternSwitched => _toyboxListener.PatternSwitched(dto.ItemId, dto.Enactor.UID),
                    //    DataUpdateType.PatternExecuted => _toyboxListener.PatternStarted(dto.ItemId, dto.Enactor.UID),
                    //    DataUpdateType.PatternStopped => _toyboxListener.PatternStopped(dto.ItemId, dto.Enactor.UID),
                    //    _ => false
                    //};
                    //if (!success)
                    //{
                    //    Logger.LogError($"Failed to handle KinksterUpdateActivePattern for {dto.User.AliasOrUID} with type {dto.Type}");
                    //    Logger.LogError($"Attempt to find out why this is even allowed to happen, and fix it, as it should never occur!");
                    //    var recallType = _toyboxListener.ActivePattern == Guid.Empty ? DataUpdateType.PatternStopped : DataUpdateType.PatternSwitched;
                    //    await UserPushActivePattern(new PushItemEnabledState(_kinksters.GetOnlineUserDatas(), GSModule.Pattern, _toyboxListener.ActivePattern, false)));
                    //}
                });
            }
            else if (dto.Module is GSModule.Alarm)
            {
                _toyboxListener.AlarmStateChanged(dto.ItemId, dto.Enactor.UID);
            }
            else if (dto.Module is GSModule.Trigger)
            {
                _toyboxListener.TriggerToggled(dto.ItemId, dto.Enactor.UID);
            }
            else
            {
                Logger.LogWarning("No support is added for this outcome!");
            }
            // Was for player. Ensure to properly handle these changes accordingly for patterns, alarms, and triggers.
        }
    }

    public Task Callback_KinksterChangeEnabledGag(KinksterChangeEnabledGag dto)
    {
        Logger.LogDebug($"KinksterChangeEnabledGag for {dto.User.AliasOrUID})", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.UpdateGagState(dto.User, dto.Gag, dto.NewState));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterChangeEnabledToy(KinksterChangeEnabledToy dto)
    {
        Logger.LogDebug($"KinksterChangeEnabledToy for {dto.User.AliasOrUID})", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.UpdateToyState(dto.User, dto.Toy, dto.NewState));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterChangeEnabledItems(KinksterChangeEnabledItems dto)
    {
        Logger.LogDebug($"KinksterChangeEnabledItems for {dto.User.AliasOrUID} (from {dto.Enactor.AliasOrUID})", LoggerType.Callbacks);
        if (dto.User.UID != UID)
        {
            Generic.Safe(() => _kinksters.UpdateItemStates(dto.User, dto.Enactor, dto.Module, dto.ActiveItems, dto.NewState));
        }
        else
        {
            Logger.LogWarning("No support is added for this outcome!");
            // Was for player. Ensure to properly handle these changes accordingly for patterns, alarms, and triggers.
        }
        return Task.CompletedTask;

    }
    
    public Task Callback_KinksterChangeEnabledGags(KinksterChangeEnabledGags dto)
    {
        Logger.LogDebug($"KinksterChangeEnabledGags for {dto.User.AliasOrUID})", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.UpdateGagStates(dto.User, dto.ActiveGags, dto.NewState));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterChangeEnabledToys(KinksterChangeEnabledToys dto)
    {
        Logger.LogDebug($"KinksterChangeEnabledToys for {dto.User.AliasOrUID})", LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.UpdateToyStates(dto.User, dto.ActiveToys, dto.NewState));
        return Task.CompletedTask;
    }

    public Task Callback_ListenerName(SendNameAction dto)
    {
        Logger.LogDebug($"Kinkster {dto.User.AliasOrUID}'s updated their Listener Name", LoggerType.Callbacks);
        Generic.Safe(() => _callbackHandler.UpdateListener(dto.User.UID, dto.Name));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Shock Instruction from another Pair. </summary>
    public Task Callback_ShockInstruction(ShockCollarAction dto)
    {
        Generic.Safe(() => _shockies.PerformShockCollarAct(dto));
        return Task.CompletedTask;
    }

    // Expected to update their global permission with this new state. If it fails, should reset.
    public Task Callback_HypnoticEffect(HypnoticAction dto)
    {
        Logger.LogDebug("Callback_HypnoticEffect: " + dto, LoggerType.Callbacks);
        Generic.Safe(() => _clientDatListener.Hypnotize(dto.User, dto.Effect, dto.ExpireTime, dto.base64Image));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewGagData(KinksterNewGagData dto)
    {
        Generic.Safe(() => _kinksters.CachedGagDataChange(dto.User, dto.GagType, dto.NewData));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewRestrictionData(KinksterNewRestrictionData dto)
    {
        Generic.Safe(() => _kinksters.CachedRestrictionDataChange(dto.User, dto.ItemId, dto.NewData));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewRestraintData(KinksterNewRestraintData dto)
    {
        Generic.Safe(() => _kinksters.CachedRestraintDataChange(dto.User, dto.ItemId, dto.NewData));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewCollarData(KinksterNewCollarData dto)
    {
        Generic.Safe(() => _kinksters.CachedCollarDataChange(dto.User, dto.NewData));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewLootData(KinksterNewLootData dto)
    {
        Generic.Safe(() => _kinksters.CachedCursedLootDataChange(dto.User, dto.ItemId, dto.NewData));
        return Task.CompletedTask;
    }

    public Task Callback_KinksterNewAliasData(KinksterNewAliasData dto)
    {
        Generic.Safe(() => _kinksters.CachedAliasDataChange(dto.User, dto.ItemId, dto.NewData));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Kinkster's updated PatternData change. </summary>
    public Task Callback_KinksterNewPatternData(KinksterNewPatternData dto)
    {
        Generic.Safe(() => _kinksters.CachedPatternDataChange(dto.User, dto.ItemId, dto.NewData));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Kinkster's updated AlarmData change. </summary>
    public Task Callback_KinksterNewAlarmData(KinksterNewAlarmData dto)
    {
        Generic.Safe(() => _kinksters.CachedAlarmDataChange(dto.User, dto.ItemId, dto.NewData));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Kinkster's updated TriggerData change. </summary>
    public Task Callback_KinksterNewTriggerData(KinksterNewTriggerData dto)
    {
        Generic.Safe(() => _kinksters.CachedTriggerDataChange(dto.User, dto.ItemId, dto.NewData));
        return Task.CompletedTask;
    }

    /// <summary> Receive a Global Chat Message. </summary>
    public Task Callback_ChatMessageGlobal(ChatMessageGlobal dto)
    {
        Mediator.Publish(new GlobalChatMessage(dto, dto.Sender.UID.Equals(UID)));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs disconnects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the KinksterBase in our pair manager so they are marked as offline. </remarks>
    public Task Callback_KinksterOffline(KinksterBase dto)
    {
        Logger.LogDebug("Callback_SendOffline: " + dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.MarkKinksterOffline(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever any of our client pairs connects from GagSpeak Servers. </summary>
    /// <remarks> Use this info to update the KinksterBase in our pair manager so they are marked as online. </remarks>
    public Task Callback_KinksterOnline(OnlineKinkster dto)
    {
        Logger.LogDebug("Callback_SendOnline: " + dto, LoggerType.Callbacks);
        Generic.Safe(() => _kinksters.MarkKinksterOnline(dto));
        return Task.CompletedTask;
    }

    /// <summary> Received whenever we need to update the profile data of anyone, including ourselves. </summary>
    public Task Callback_ProfileUpdated(KinksterBase dto)
    {
        Logger.LogDebug("Callback_UpdateProfile: " + dto, LoggerType.Callbacks);
        Mediator.Publish(new ClearKinkPlateDataMessage(dto.User));
        return Task.CompletedTask;
    }

    /// <summary> The callback responsible for displaying verification codes to the clients monitor. </summary>
    /// <remarks> This is currently experiencing issues for some reason with the discord bot. Look into more? </remarks>
    public Task Callback_ShowVerification(VerificationCode dto)
    {
        Logger.LogDebug("Callback_ShowVerification: " + dto, LoggerType.Callbacks);
        Mediator.Publish(new VerificationPopupMessage(dto));
        return Task.CompletedTask;
    }

    public Task Callback_RoomJoin(RoomParticipant dto)
    {
        Logger.LogDebug("Callback_RoomJoin: " + dto, LoggerType.Callbacks);
        _toyboxListener.KinksterJoinedRoom(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomLeave(UserData dto)
    {
        Logger.LogDebug("Callback_RoomLeave: " + dto, LoggerType.Callbacks);
        _toyboxListener.KinksterLeftRoom(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomAddInvite(RoomInvite dto)
    {
        Logger.LogDebug("Callback_RoomAddInvite: " + dto, LoggerType.Callbacks);
        _toyboxListener.VibeRoomInviteReceived(dto);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Room Leave from a Room. </summary>
    public Task Callback_RoomHostChanged(UserData dto)
    {
        Logger.LogDebug("Callback_RoomHostChanged: " + dto, LoggerType.Callbacks);
        _toyboxListener.VibeRoomHostChanged(dto);
        return Task.CompletedTask;
    }



    /// <summary> Receive a Device Update from a Room. </summary>
    public Task Callback_RoomDeviceUpdate(UserData user, ToyInfo device)
    {
        Logger.LogDebug("Callback_RoomDeviceUpdate: " + user, LoggerType.Callbacks);
        _toyboxListener.KinksterUpdatedDevice(user, device);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Data Stream from a Room. </summary>
    public Task Callback_RoomIncDataStream(ToyDataStreamResponse dto)
    {
        Logger.LogDebug("Callback_RoomIncDataStream: " + dto, LoggerType.Callbacks);
        _toyboxListener.ReceivedBuzzToyDataStream(dto);
        return Task.CompletedTask;
    }

    /// <summary> A User granted us access to control their sex toys. </summary>
    public Task Callback_RoomAccessGranted(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessGranted: " + user, LoggerType.Callbacks);
        _toyboxListener.KinksterGrantedAccess(user);
        return Task.CompletedTask;
    }

    /// <summary> A User revoked access to their sextoys. </summary>
    public Task Callback_RoomAccessRevoked(UserData user)
    {
        Logger.LogDebug("Callback_RoomAccessRevoked: " + user, LoggerType.Callbacks);
        _toyboxListener.KinksterRevokedAccess(user);
        return Task.CompletedTask;
    }

    /// <summary> Receive a Chat Message from a Room. </summary>
    public Task Callback_RoomChatMessage(UserData user, string message)
    {
        Logger.LogDebug("Callback_RoomChatMessage: " + user + " - " + message, LoggerType.Callbacks);
        Mediator.Publish(new VibeRoomChatMessage(user, message));
        return Task.CompletedTask;
    }

    /* --------------------------------- void methods from the API to call the hooks --------------------------------- */
    public void OnServerMessage(Action<MessageSeverity, string> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ServerMessage), act);
    }

    public void OnHardReconnectMessage(Action<MessageSeverity, string, ServerState> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_HardReconnectMessage), act);
    }

    public void OnServerInfo(Action<ServerInfoResponse> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ServerInfo), act);
    }

    public void OnAddClientPair(Action<KinksterPair> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_AddClientPair), act);
    }

    public void OnRemoveClientPair(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemoveClientPair), act);
    }

    public void OnAddPairRequest(Action<KinksterRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_AddPairRequest), act);
    }

    public void OnRemovePairRequest(Action<KinksterRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemovePairRequest), act);
    }

    public void OnAddCollarRequest(Action<CollarRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_AddCollarRequest), act);
    }

    public void OnRemoveCollarRequest(Action<CollarRequest> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemoveCollarRequest), act);
    }

    public void OnMoodleDataUpdated(Action<MoodlesDataUpdate> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_MoodleDataUpdated), act);
    }

    public void OnMoodleSMUpdated(Action<MoodlesSMUpdate> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_MoodleSMUpdated), act);
    }

    public void OnMoodleStatusesUpdate(Action<MoodlesStatusesUpdate> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_MoodleStatusesUpdate), act);
    }

    public void OnMoodlePresetsUpdate(Action<MoodlesPresetsUpdate> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_MoodlePresetsUpdate), act);
    }

    public void OnMoodleStatusModified(Action<MoodlesStatusModified> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_MoodleStatusModified), act);
    }

    public void OnMoodlePresetModified(Action<MoodlesPresetModified> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_MoodlePresetModified), act);
    }

    public void OnApplyMoodlesByGuid(Action<ApplyMoodleId> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ApplyMoodlesByGuid), act);
    }

    public void OnApplyMoodlesByStatus(Action<ApplyMoodleStatus> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ApplyMoodlesByStatus), act);
    }

    public void OnRemoveMoodles(Action<RemoveMoodleId> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RemoveMoodles), act);
    }

    public void OnClearMoodles(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ClearMoodles), act);
    }
    public void OnBulkChangeGlobal(Action<BulkChangeGlobal> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_BulkChangeGlobal), act);
    }

    public void OnBulkChangeUnique(Action<BulkChangeUnique> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_BulkChangeUnique), act);
    }

    public void OnSingleChangeGlobal(Action<SingleChangeGlobal> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SingleChangeGlobal), act);
    }

    public void OnSingleChangeUnique(Action<SingleChangeUnique> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SingleChangeUnique), act);
    }

    public void OnSingleChangeAccess(Action<SingleChangeAccess> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_SingleChangeAccess), act);
    }

    public void OnStateChangeHardcore(Action<HardcoreStateChange> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_StateChangeHardcore), act);
    }

    public void OnKinksterUpdateComposite(Action<KinksterUpdateComposite> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateComposite), act);
    }

    public void OnKinksterUpdateActiveGag(Action<KinksterUpdateActiveGag> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveGag), act);
    }

    public void OnKinksterUpdateActiveRestriction(Action<KinksterUpdateActiveRestriction> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveRestriction), act);
    }

    public void OnKinksterUpdateActiveRestraint(Action<KinksterUpdateActiveRestraint> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveRestraint), act);
    }

    public void OnKinksterUpdateActiveCollar(Action<KinksterUpdateActiveCollar> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterUpdateActiveCollar), act);
    }

    public void OnKinksterChangeEnabledItem(Action<KinksterChangeEnabledItem> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterChangeEnabledItem), act);
    }

    public void OnKinksterChangeEnabledGag(Action<KinksterChangeEnabledGag> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterChangeEnabledGag), act);
    }

    public void OnKinksterChangeEnabledToy(Action<KinksterChangeEnabledToy> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterChangeEnabledToy), act);
    }

    public void OnKinksterChangeEnabledItems(Action<KinksterChangeEnabledItems> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterChangeEnabledItems), act);
    }

    public void OnKinksterChangeEnabledGags(Action<KinksterChangeEnabledGags> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterChangeEnabledGags), act);
    }

    public void OnKinksterChangeEnabledToys(Action<KinksterChangeEnabledToys> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterChangeEnabledToys), act);
    }

    public void OnListenerName(Action<SendNameAction> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ListenerName), act);
    }

    public void OnShockInstruction(Action<ShockCollarAction> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ShockInstruction), act);
    }

    public void OnHypnoticEffect(Action<HypnoticAction> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_HypnoticEffect), act);
    }

    public void OnKinksterNewGagData(Action<KinksterNewGagData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewGagData), act);
    }

    public void OnKinksterNewRestrictionData(Action<KinksterNewRestrictionData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewRestrictionData), act);
    }

    public void OnKinksterNewRestraintData(Action<KinksterNewRestraintData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewRestraintData), act);
    }

    public void OnKinksterNewCollarData(Action<KinksterNewCollarData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewCollarData), act);
    }

    public void OnKinksterNewLootData(Action<KinksterNewLootData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewLootData), act);
    }

    public void OnKinksterNewAliasData(Action<KinksterNewAliasData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewAliasData), act);
    }

    public void OnKinksterNewPatternData(Action<KinksterNewPatternData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewPatternData), act);
    }

    public void OnKinksterNewAlarmData(Action<KinksterNewAlarmData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewAlarmData), act);
    }

    public void OnKinksterNewTriggerData(Action<KinksterNewTriggerData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterNewTriggerData), act);
    }

    public void OnChatMessageGlobal(Action<ChatMessageGlobal> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ChatMessageGlobal), act);
    }

    public void OnKinksterOffline(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterOffline), act);
    }

    public void OnKinksterOnline(Action<OnlineKinkster> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_KinksterOnline), act);
    }

    public void OnProfileUpdated(Action<KinksterBase> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ProfileUpdated), act);
    }

    public void OnShowVerification(Action<VerificationCode> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_ShowVerification), act);
    }


    public void OnRoomJoin(Action<RoomParticipant> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomJoin), act);
    }

    public void OnRoomLeave(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomLeave), act);
    }

    public void OnRoomAddInvite(Action<RoomInvite> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomAddInvite), act);
    }

    public void OnRoomHostChanged(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomHostChanged), act);
    }

    public void OnRoomDeviceUpdate(Action<UserData, ToyInfo> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomDeviceUpdate), act);
    }

    public void OnRoomIncDataStream(Action<ToyDataStreamResponse> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomIncDataStream), act);
    }

    public void OnRoomAccessGranted(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomAccessGranted), act);
    }

    public void OnRoomAccessRevoked(Action<UserData> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomAccessRevoked), act);
    }

    public void OnRoomChatMessage(Action<UserData, string> act)
    {
        if (_apiHooksInitialized) return;
        _hubConnection!.On(nameof(Callback_RoomChatMessage), act);
    }
}
