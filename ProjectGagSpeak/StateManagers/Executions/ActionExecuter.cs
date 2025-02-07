using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Extensions;
using ImPlotNET;
using OtterGui;

namespace GagSpeak.StateManagers;

// remove sealed if causing any issues.
public sealed class ActionExecutor
{
    private readonly ILogger<ActionExecutor> _logger;
    private readonly ClientData _playerData;
    private readonly AppearanceManager _appearanceManager;
    private readonly PairManager _pairs;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly IpcCallerMoodles _moodlesIpc;
    private readonly VibratorService _vibeService;

    public ActionExecutor(ILogger<ActionExecutor> logger, ClientData playerData,
        AppearanceManager appearanceManager, PairManager pairs,
        ClientConfigurationManager clientConfigs, IpcCallerMoodles moodlesIpc,
        VibratorService vibeService)
    {
        _logger = logger;
        _playerData = playerData;
        _appearanceManager = appearanceManager;
        _pairs = pairs;
        _clientConfigs = clientConfigs;
        _moodlesIpc = moodlesIpc;
        _vibeService = vibeService;
    }

    public async Task<bool> ExecuteActionAsync(IActionGS actionExecutable, string performerUID)
    {
        if (actionExecutable == null)
            throw new ArgumentNullException(nameof(actionExecutable));

        return actionExecutable.ExecutionType switch
        {
            ActionExecutionType.TextOutput => HandleTextAction(actionExecutable as TextAction, performerUID),
            ActionExecutionType.Gag => await HandleGagAction(actionExecutable as GagAction, performerUID),
            ActionExecutionType.Restraint => await HandleRestraintAction(actionExecutable as RestraintAction, performerUID),
            ActionExecutionType.Moodle => await HandleMoodleAction(actionExecutable as MoodleAction, performerUID),
            ActionExecutionType.ShockCollar => await HandlePiShockAction(actionExecutable as PiShockAction, performerUID),
            ActionExecutionType.SexToy => await HandleSexToyAction(actionExecutable as SexToyAction, performerUID),
            _ => false // If no matching execution type, return false
        };
    }

    public async Task ExecuteMultiActionAsync(List<IActionGS> multiAction, string performerUID, Action? onSuccess = null)
    {
        bool anySuccess = false;
        foreach (var action in multiAction)
        {
            var result = await ExecuteActionAsync(action, performerUID);
            if (result && anySuccess) anySuccess = true;
        }
        if (anySuccess) onSuccess?.Invoke();
    }
    private bool HandleTextAction(TextAction? textAction, string performerUID)
    {
        if (textAction is null)
        {
            _logger.LogWarning("Executing Action is invalid, cannot execute Text Action.");
            return false;
        }

        // construct the new SeString to send.
        var remainingMessage = new SeString().Append(textAction.OutputCommand);
        if (remainingMessage.TextValue.IsNullOrEmpty())
        {
            _logger.LogTrace("Message is empty after alias conversion.", LoggerType.Puppeteer);
            return false;
        }

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();
        // verify permissions are satisfied.
        var executerUID = performerUID;
        
        var aliasAllowed = false;
        // if performer is self, use global perms, otherwise, use pair perms.
        if (performerUID == MainHub.UID)
        {
            // This method is _only called_ from an alias
            aliasAllowed = _playerData.GlobalPerms?.GlobalAllowAliasRequests ?? false;
        }
        else
        {
            var matchedPair = _pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == performerUID);
            if (matchedPair is null)
            {
                _logger.LogWarning("No pair found for the performer, cannot execute Text Action.");
                return false;
            }

            matchedPair.OwnPerms.PuppetPerms(out bool sits2, out bool motions2, out bool alias2, out bool all2, out char startChar, out char endChar);
            aliasAllowed = alias2;
        }

        // Only apply this if it either originated from a trigger (or alias) 
        // Or it it meets the various criteria for the sender.
        if (aliasAllowed)
        {
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatBoxMessage.EnqueueMessage("/" + remainingMessage.TextValue);
            return true;
        }

        // Implement text logic here
        return false;
    }

    private async Task<bool> HandleGagAction(GagAction? gagAction, string performerUID)
    {
        if (_playerData.AppearanceData is null || gagAction is null)
        {
            _logger.LogWarning("Executing Action is invalid, cannot execute Gag Action.");
            return false;
        }

        // if the state is to enable the ballgag.
        if (gagAction.NewState is NewState.Enabled)
        {
            // don't process if all gags equipped already.
            if (!_playerData.AppearanceData.GagSlots.Any(x => x.GagType.ToGagType() is GagType.None))
                return false;

            // if a slot is available, find the first index that is empty.
            var availableSlot = _playerData.AppearanceData.GagSlots.IndexOf(x => x.GagType.ToGagType() is GagType.None);
            // apply the gag to that slot.
            _logger.LogInformation("ActionExecutorGS is applying Gag Type " + gagAction.GagType + " to layer " + (GagLayer)availableSlot, LoggerType.GagHandling);
            await _appearanceManager.GagApplied((GagLayer)availableSlot, gagAction.GagType, MainHub.UID, true, false);
            return true;
        }
        else if (gagAction.NewState is NewState.Disabled)
        {
            // allow wildcard.
            if (gagAction.GagType is GagType.None)
            {
                // remove the outermost gag (target the end of the list and move to the start)
                var idx = _playerData.AppearanceData.FindOutermostActive();
                if (idx is not -1)
                {
                    _logger.LogInformation("ActionExecutorGS is attempting removing Gag Type " + gagAction.GagType + " from layer " + (GagLayer)idx, LoggerType.GagHandling);
                    await _appearanceManager.GagRemoved((GagLayer)idx, performerUID, true, false);
                    return true;
                }
            }
            else
            {
                // if the gagtype is not GagType.None, disable the first gagtype matching it.
                var idx = _playerData.AppearanceData.FindOutermostActive(gagAction.GagType);
                if (idx is not -1)
                {
                    _logger.LogInformation("ActionExecutorGS is attempting removing Gag Type " + gagAction.GagType + " from layer " + (GagLayer)idx, LoggerType.GagHandling);
                    await _appearanceManager.GagRemoved((GagLayer)idx, performerUID, true, false);
                    return true;
                }
            }
        }
        return false;
    }

    private async Task<bool> HandleRestraintAction(RestraintAction? restraintAction, string performerUID)
    {
        if (restraintAction is null)
        {
            _logger.LogWarning("Executing Action is invalid, cannot execute Restraint Action.");
            return false;
        }

        // if the set does not exist in our list of sets, log error and return.
        if (!_clientConfigs.StoredRestraintSets.Any(x => x.RestraintId == restraintAction.OutputIdentifier))
        {
            _logger.LogError("ActionExecution Set no longer exists in your wardrobe!");
            return false;
        }

        // if enabling.
        if (restraintAction.NewState is NewState.Enabled)
        {
            if(_appearanceManager.CanEnableSet(restraintAction.OutputIdentifier))
            {
                // swap if one is active, or apply if not.
                _logger.LogInformation("HandleRestraint ActionExecution performing set SWAP/APPLY.", LoggerType.Restraints);
                await _appearanceManager.SwapOrApplyRestraint(restraintAction.OutputIdentifier, MainHub.UID, true);
                return true;
            }
        }
        else if (restraintAction.NewState is NewState.Disabled)
        {
            if (_clientConfigs.TryGetActiveSet(out var activeSet))
            {
                if (_appearanceManager.CanDisableSet(activeSet.RestraintId))
                {
                    // if the set is active, and the set is the one we want to disable, disable it.
                    _logger.LogInformation("HandleRestraint ActionExecution performing set DISABLE.");
                    await _appearanceManager.DisableRestraintSet(activeSet.RestraintId, MainHub.UID, true, false);
                    return true;
                }
            }
        }

        return false; // Failure.
    }

    private async Task<bool> HandleMoodleAction(MoodleAction? moodleAction, string performerUID)
    {
        if (moodleAction is null)
        {
            _logger.LogWarning("Executing Action is invalid, cannot execute Moodle Action.");
            return false;
        }

        if (!IpcCallerMoodles.APIAvailable || _playerData.LastIpcData is null)
        {
            _logger.LogError("Moodles IPC is not available, cannot execute moodle trigger.");
            return false;
        }

        // check if the action is available in our lists.
        if (moodleAction.MoodleType is IpcToggleType.MoodlesStatus)
        {
            if (!_playerData.LastIpcData.MoodlesStatuses.Any(x => x.GUID == moodleAction.Identifier))
                return false;

            if (_playerData.LastIpcData.MoodlesDataStatuses.Any(x => x.GUID == moodleAction.Identifier))
                return false;

            await _moodlesIpc.ApplyOwnStatusByGUID(new List<Guid>() { moodleAction.Identifier });
            return true;
        }
        else
        {
            if (_playerData.LastIpcData.MoodlesPresets.Any(x => x.Item1 == moodleAction.Identifier))
            {
                // we have a valid Moodle to set, so go ahead and try to apply it!
                _logger.LogInformation("HandleMoodlePreset Action Execution performing a MOODLE PRESET APPLY", LoggerType.IpcMoodles);
                await _moodlesIpc.ApplyOwnPresetByGUID(moodleAction.Identifier);
                return true;
            }
        }

        return false; // Failure.
    }

    private Task<bool> HandlePiShockAction(PiShockAction? piShockAction, string performerUID)
    {
        if (_playerData.GlobalPerms is null || piShockAction is null)
        {
            _logger.LogWarning("Executing Action is invalid, cannot execute Gag Action.");
            return Task.FromResult(false);
        }

        if (_playerData.GlobalPerms.GlobalShockShareCode.IsNullOrEmpty() || _playerData.GlobalPerms.HasValidShareCode() is false)
        {
            _logger.LogWarning("Can't execute Shock Instruction if none are currently connected!");
            return Task.FromResult(false);
        }

        // execute the instruction with our global share code.
        _logger.LogInformation("HandlePiShock Action is executing instruction based on global sharecode settings!", LoggerType.PiShock);
        _vibeService.ExecuteShockAction(_playerData.GlobalPerms.GlobalShockShareCode, piShockAction.ShockInstruction);
        return Task.FromResult(true);
    }

    private Task<bool> HandleSexToyAction(SexToyAction? sexToyAction, string performerUID)
    {
        if (sexToyAction is null)
            return Task.FromResult(false);

        _vibeService.DeviceHandler.ExecuteVibeTrigger(sexToyAction);
        return Task.FromResult(true);
    }

    public bool MeetsSettingCriteria(bool canSit, bool canEmote, bool canAll, SeString message)
    {
        if (canAll)
        {
            _logger.LogTrace("Accepting Message as you allow All Commands", LoggerType.Puppeteer);
            return true;
        }

        if (canEmote)
        {
            var emote = EmoteMonitor.EmoteCommandsWithId
                .FirstOrDefault(e => string.Equals(message.TextValue, e.Key.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(emote.Key))
            {
                _logger.LogTrace("Valid Emote name: " + emote.Key.Replace(" ", "").ToLower() + ", RowID: " + emote.Value, LoggerType.Puppeteer);
                _logger.LogTrace("Accepting Message as you allow Motion Commands", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)emote.Value);
                return true;
            }
        }

        // 50 == Sit, 52 == Sit (Ground), 90 == Change Pose
        if (canSit)
        {
            _logger.LogTrace("Checking if message is a sit command", LoggerType.Puppeteer);
            var sitEmote = EmoteMonitor.SitEmoteComboList.FirstOrDefault(e => message.TextValue.Contains(e.Name.ToString().Replace(" ", "").ToLower()));
            if (sitEmote.RowId is 50 or 52)
            {
                _logger.LogTrace("Message is a sit command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)sitEmote.RowId);
                return true;
            }
            if (EmoteMonitor.EmoteCommandsWithId.Where(e => e.Value is 90).Any(e => message.TextValue.Contains(e.Key.Replace(" ", "").ToLower())))
            {
                _logger.LogTrace("Message is a change pose command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)90);
                return true;
            }
        }

        // Failure
        return false;
    }
}





