using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.InterfaceConverters;
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
        var sits = false;
        var motions = false;
        var all = false;
        // if performer is self, use global perms, otherwise, use pair perms.
        if(performerUID == MainHub.UID)
        {
            sits = _playerData.GlobalPerms?.GlobalAllowSitRequests ?? false;
            motions = _playerData.GlobalPerms?.GlobalAllowMotionRequests ?? false;
            all = _playerData.GlobalPerms?.GlobalAllowAllRequests ?? false;
        }
        else
        {
            var matchedPair = _pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == performerUID);
            if (matchedPair is null)
            {
                _logger.LogWarning("No pair found for the performer, cannot execute Text Action.");
                return false;
            }

            matchedPair.OwnPerms.PuppetPerms(out bool sits2, out bool motions2, out bool all2, out char startChar, out char endChar);
            sits = sits2;
            motions = motions2;
            all = all2;
        }

        // only apply it if the message meets the criteria for the sender.
        if (MeetsSettingCriteria(sits, motions, all, remainingMessage))
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
            await _appearanceManager.GagApplied((GagLayer)availableSlot, gagAction.GagType, isSelfApplied: true);
            return true;
        }
        else if (gagAction.NewState is NewState.Disabled)
        {
            // allow wildcard.
            if (gagAction.GagType is GagType.None)
            {
                // remove the outermost gag (target the end of the list and move to the start)
                var outermostGagLayer = _playerData.AppearanceData.GagSlots.ToList().IndexOf(x => x.GagType.ToGagType() is GagType.None);
                if (outermostGagLayer is not -1)
                {
                    // dont allow removing locked gags.
                    if (_playerData.AppearanceData.GagSlots[outermostGagLayer].Padlock.ToPadlock() is not Padlocks.None)
                        return false;

                    _logger.LogInformation("ActionExecutorGS is removing Gag Type " + gagAction.GagType + " from layer " + (GagLayer)outermostGagLayer, LoggerType.GagHandling);
                    await _appearanceManager.GagRemoved((GagLayer)outermostGagLayer, _playerData.AppearanceData.GagSlots[outermostGagLayer].GagType.ToGagType(), isSelfApplied: true);
                    return true;
                }
            }
            else
            {
                // if the gagtype is not GagType.None, disable the first gagtype matching it.
                if (_playerData.AppearanceData.GagSlots.Any(x => x.GagType.ToGagType() == gagAction.GagType))
                {
                    var slotIndex = _playerData.AppearanceData.GagSlots.IndexOf(x => x.GagType.ToGagType() == gagAction.GagType);
                    _logger.LogInformation("ActionExecutorGS is removing Gag Type " + gagAction.GagType + " from layer " + (GagLayer)slotIndex, LoggerType.GagHandling);
                    await _appearanceManager.GagRemoved((GagLayer)slotIndex, _playerData.AppearanceData.GagSlots[slotIndex].GagType.ToGagType(), isSelfApplied: true);
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

        // if a set is active and already locked, do not execute, and log error.                
        var activeSet = _clientConfigs.GetActiveSet();
        if (activeSet is not null)
        {
            if (activeSet.Locked)
            {
                _logger.LogError("Cannot apply/remove a restraint set while current is locked!");
                return false;
            }
            // This set is already enabled.
            if (activeSet.RestraintId == restraintAction.OutputIdentifier && restraintAction.NewState is NewState.Enabled)
            {
                _logger.LogWarning("Set is already enabled, no need to re-enable.");
                return false;
            }
        }

        // if enabling.
        if (restraintAction.NewState is NewState.Enabled)
        {
            // swap if one is active, or apply if not.
            if (activeSet is not null)
            {
                _logger.LogInformation("HandleRestraint ActionExecution performing set SWAP.", LoggerType.Restraints);
                await _appearanceManager.RestraintSwapped(restraintAction.OutputIdentifier, MainHub.UID);
                return true;
            }
            else
            {
                _logger.LogInformation("HandleRestraint ActionExecution performing set APPLY.", LoggerType.Restraints);
                await _appearanceManager.EnableRestraintSet(restraintAction.OutputIdentifier, MainHub.UID);
                return true;
            }
        }

        // if disabling.
        if (restraintAction.NewState is NewState.Disabled)
        {
            // if the set is not active, return.
            if (activeSet is null)
                return false;

            // if the set is active, and the set is the one we want to disable, disable it.
            _logger.LogInformation("HandleRestraint ActionExecution performing set DISABLE.");
            await _appearanceManager.DisableRestraintSet(activeSet.RestraintId, MainHub.UID);
            return true;
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
        _logger.LogInformation("HandlePiShock Action is executing instruction!", LoggerType.PiShock);
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





