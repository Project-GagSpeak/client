using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Controllers;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Microsoft.IdentityModel.Tokens;
using OtterGui;

namespace GagSpeak.PlayerState.Toybox;

/// <summary> This is technically an applier for other trigger sources like puppeteer as well. </summary>
/// </summary>
public sealed class TriggerApplier : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly GagspeakConfigService _config;
    private readonly GlobalData _playerData;
    private readonly PairManager _pairs;
    private readonly RestraintManager _restraints;
    private readonly RestrictionManager _restrictions;
    private readonly GagRestrictionManager _gags;
    private readonly VisualApplierMoodles _moodles;
    private readonly SexToyManager _vibeService;

    // Our RateLimiter.
    private ActionRateLimiter _rateLimiter { get; init; }

    public TriggerApplier(ILogger<TriggerApplier> logger, GagspeakMediator mediator, MainHub hub,
        GagspeakConfigService config, GlobalData playerData, PairManager pairs,
        RestraintManager restraints, RestrictionManager restrictions, GagRestrictionManager gags,
        VisualApplierMoodles moodles, SexToyManager vibeService) : base(logger, mediator)
    {
        _hub = hub;
        _config = config;
        _playerData = playerData;
        _pairs = pairs;
        _restraints = restraints;
        _restrictions = restrictions;
        _gags = gags;
        _moodles = moodles;
        _vibeService = vibeService;

        _rateLimiter = new ActionRateLimiter(TimeSpan.FromSeconds(3), 1, 3, 3, 3);
    }

    public async Task<bool> HandleActionAsync(InvokableGsAction invokableAction, string enactor, ActionSource source)
    {
        try
        {
            // Rate-limit check before invoking action
            if (!CanExecuteAction(invokableAction))
            {
                Logger.LogDebug("Rate limit exceeded for action: " + invokableAction.GetType().Name, LoggerType.ToyboxTriggers);
                return false;
            }

            // perform an action based on the type.
            return invokableAction switch
            {
                TextAction        ta  =>       DoTextAction(ta, enactor, source),
                GagAction         ga  => await DoGagAction(ga, enactor),
                RestrictionAction rsa => await DoRestrictionAction(rsa, enactor),
                RestraintAction   rta => await DoRestraintAction(rta, enactor),
                MoodleAction      ma  =>       DoMoodleAction(ma, enactor),
                PiShockAction     ps  =>       DoPiShockAction(ps, enactor),
                SexToyAction      sta =>       DoSexToyAction(sta, enactor),
                _ => throw new InvalidOperationException($"Unhandled action type: {invokableAction.GetType().Name}")
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing action: {Action}", invokableAction);
            return false;
        }
    }

    public async Task<bool> HandleMultiActionAsync(IEnumerable<InvokableGsAction> multiAction, string enactor, ActionSource source)
    {
        var anySuccess = false;
        foreach (var action in multiAction)
        {
            var result = await HandleActionAsync(action, enactor, source);
            if (result && !anySuccess)
                anySuccess = true;
        }
        return anySuccess;
    }


    private bool CanExecuteAction(InvokableGsAction invokableAction)
    {
        switch (invokableAction)
        {
            case GagAction         _: return _rateLimiter.CanExecute(InvokableActionType.Gag);
            case RestraintAction   _: return _rateLimiter.CanExecute(InvokableActionType.Restraint);
            case RestrictionAction _: return _rateLimiter.CanExecute(InvokableActionType.Restriction);
            case MoodleAction      _: return _rateLimiter.CanExecute(InvokableActionType.Moodle);
            default:                  return true; // No rate limit for other actions
        }
    }

    private bool DoTextAction(TextAction act, string enactor, ActionSource source)
    {
        if(enactor == MainHub.UID)
            return false;

        // construct the new SeString to send.
        var remainingMessage = new SeString().Append(act.OutputCommand);
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        if (remainingMessage.TextValue.IsNullOrEmpty())
        {
            Logger.LogTrace("Message is empty after alias conversion.", LoggerType.Puppeteer);
            return false;
        }

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();
        
        // Handle final checks based on the source type.
        switch (source)
        {
            case ActionSource.GlobalAlias:
                if(_playerData.GlobalPerms is null || !_playerData.GlobalPerms.PuppetPerms.HasFlag(PuppetPerms.Alias))
                    return false;

                break;
            case ActionSource.PairAlias:
                if (_pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == enactor) is not { } match)
                    return false;
                // If it was a match, return false if you have not given the pair alias permissions.
                if(match.OwnPerms.PuppetPerms.HasFlag(PuppetPerms.Alias) is false)
                    return false;

                break;
            default:
                Logger.LogWarning("Unknown or disallowed type for Text Action.");
                return false;
        }

        Logger.LogInformation("Text Action is being executed.", LoggerType.Puppeteer);
        UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
        ChatMonitor.EnqueueMessage("/" + remainingMessage.TextValue);
        return true;
    }

    private async Task<bool> DoGagAction(GagAction act, string enactor)
    {
        if(_gags.ServerGagData is not { } gagData)
            return false;

        GagSpeakApiEc retCode;
        switch (act.NewState)
        {
            case NewState.Enabled:
                var availableIdx = gagData.FindFirstUnused();
                if (availableIdx is -1)
                    return false;

                Logger.LogInformation("Applying a " + act.GagType + " to layer " + (int)availableIdx, LoggerType.GagHandling);
                PushClientGagSlotUpdate newInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Applied)
                {
                    Layer = availableIdx,
                    Gag = act.GagType,
                    Enabler = MainHub.UID
                };

                retCode = (await _hub.UserPushDataGags(newInfo)).ErrorCode;
                break;
            case NewState.Locked:
                // Idk why you would do this lol but account for it.
                if (act.Padlock is Padlocks.None)
                    return false;

                // If we have defined a layer idx, look for a gag on that index and return false if none are present or it is locked.
                if (act.LayerIdx != -1)
                {
                    if (gagData.GagSlots[act.LayerIdx].IsLocked() || gagData.GagSlots[act.LayerIdx].GagItem is GagType.None)
                        return false;
                }

                // If we have selected a specific gag to lock, look for it, and if none are found, return false.
                if (act.GagType is not GagType.None && gagData.FindOutermostActive(act.GagType) is -1)
                    return false;

                // Otherwise, attempt to locate the first lockable gagslot.
                var idx = gagData.FindFirstUnlocked();
                if (idx is -1)
                    return false;

                // We have found one to lock. Check what lock we chose, and define accordingly.
                var password = act.Padlock switch
                {
                    // Generate a random 6 digit string of characters.
                    Padlocks.PasswordPadlock => Generators.GetRandomCharaString(10),
                    Padlocks.CombinationPadlock => Generators.GetRandomIntString(4),
                    Padlocks.TimerPasswordPadlock => Generators.GetRandomCharaString(10),
                    _ => string.Empty
                };
                // define a random time between 2 timespan bounds.
                var timer = act.Padlock.IsTimerLock() ? Generators.GetRandomTimeSpan(act.LowerBound, act.UpperBound) : TimeSpan.Zero;
                Logger.LogInformation("Locking a " + act.GagType + " with " + act.Padlock + " to layer " + (int)idx, LoggerType.ToyboxTriggers);
                PushClientGagSlotUpdate newLockInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Locked)
                {
                    Layer = idx,
                    Padlock = act.Padlock,
                    Password = password,
                    Timer = new DateTimeOffset(DateTime.UtcNow + timer),
                    Assigner = MainHub.UID
                };
                retCode = (await _hub.UserPushDataGags(newLockInfo)).ErrorCode;
                break;

            case NewState.Disabled:
                var match = act.GagType is GagType.None
                    ? gagData.FindOutermostActive()
                    : gagData.FindOutermostActive(act.GagType);

                if (match is -1)
                    return false;

                // We can remove it.
                Logger.LogDebug("Removing a " + act.GagType + " from layer " + (int)match, LoggerType.ToyboxTriggers);
                PushClientGagSlotUpdate removeInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Removed)
                {
                    Layer = match
                };
                retCode = (await _hub.UserPushDataGags(removeInfo)).ErrorCode;
                break;

            default:
                return false;
        }

        if (retCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError("Gag action failed with error code: " + retCode);
            return false;
        }

        return true;
    }

    private async Task<bool> DoRestrictionAction(RestrictionAction act, string enactor)
    {
        if (_restrictions.ServerRestrictionData is not { } restrictions)
            return false;

        GagSpeakApiEc retCode;
        switch (act.NewState)
        {
            case NewState.Enabled:
                // grab the right restriction first.
                var availableIdx = act.LayerIdx is -1
                    ? restrictions.Restrictions.IndexOf(x => x.Identifier.IsEmptyGuid())
                    : act.LayerIdx;

                if (availableIdx is -1 || restrictions.Restrictions[availableIdx].IsLocked() || !restrictions.Restrictions[availableIdx].CanApply())
                    return false;

                Logger.LogInformation("Applying a restriction item to layer " + availableIdx, LoggerType.ToyboxTriggers);
                PushClientRestrictionUpdate newInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Applied)
                {
                    Layer = availableIdx,
                    Identifier = act.RestrictionId,
                    Enabler = MainHub.UID
                };
                retCode = (await _hub.UserPushDataRestrictions(newInfo)).ErrorCode;
                break;

            case NewState.Locked:
                // Locate the first available unlocked restriction.
                var idx = act.LayerIdx is -1
                    ? restrictions.Restrictions.IndexOf(x => !x.Identifier.IsEmptyGuid() && x.CanLock())
                    : act.LayerIdx;

                if (idx is -1 || !restrictions.Restrictions[idx].CanLock() || act.Padlock is Padlocks.None)
                    return false;

                var password = act.Padlock switch
                {
                    Padlocks.PasswordPadlock => Generators.GetRandomCharaString(10),
                    Padlocks.CombinationPadlock => Generators.GetRandomIntString(4),
                    Padlocks.TimerPasswordPadlock => Generators.GetRandomCharaString(10),
                    _ => string.Empty
                };
                // define a random time between 2 timespan bounds.
                var timer = act.Padlock.IsTimerLock() ? Generators.GetRandomTimeSpan(act.LowerBound, act.UpperBound) : TimeSpan.Zero;
                Logger.LogDebug("Locking a restriction item with " + act.Padlock + " to layer " + idx, LoggerType.ToyboxTriggers);
                PushClientRestrictionUpdate newLockInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Locked)
                {
                    Layer = idx,
                    Padlock = act.Padlock,
                    Password = password,
                    Timer = new DateTimeOffset(DateTime.UtcNow + timer),
                    Assigner = MainHub.UID
                };
                retCode = (await _hub.UserPushDataRestrictions(newLockInfo)).ErrorCode;
                break;

            case NewState.Disabled:
                var match = !act.RestrictionId.IsEmptyGuid()
                    ? restrictions.FindOutermostActiveUnlocked()
                    : restrictions.Restrictions.IndexOf(x => x.Identifier == act.RestrictionId);

                if (match is -1 || restrictions.Restrictions[match].CanRemove() is false)
                    return false;

                Logger.LogDebug("Removing a restriction item from layer " + match, LoggerType.ToyboxTriggers);
                PushClientRestrictionUpdate removeInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Removed)
                {
                    Layer = match
                };
                retCode = (await _hub.UserPushDataRestrictions(removeInfo)).ErrorCode;
                break;

            default:
                return false;
        }

        if (retCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError("Restriction action failed with error code: " + retCode);
            return false;
        }

        return true;
    }

    private async Task<bool> DoRestraintAction(RestraintAction act, string enactor)
    {
        if(_restraints.ServerRestraintData is not { } restraint)
            return false;

        GagSpeakApiEc retCode;
        switch (act.NewState)
        {
            case NewState.Enabled:
                if (!restraint.CanApply() || !_restraints.Storage.Contains(act.RestrictionId))
                    return false;

                Logger.LogDebug("Applying a restraint item to layer " + act.RestrictionId, LoggerType.ToyboxTriggers);
                PushClientRestraintUpdate newInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Applied)
                {
                    ActiveSetId = act.RestrictionId,
                    Enabler = MainHub.UID
                };
                retCode = (await _hub.UserPushDataRestraint(newInfo)).ErrorCode;
                break;

            case NewState.Locked:
                if (!restraint.CanLock() || act.Padlock is Padlocks.None)
                    return false;

                var password = act.Padlock switch
                {
                    Padlocks.PasswordPadlock => Generators.GetRandomCharaString(10),
                    Padlocks.CombinationPadlock => Generators.GetRandomIntString(4),
                    Padlocks.TimerPasswordPadlock => Generators.GetRandomCharaString(10),
                    _ => string.Empty
                };
                // define a random time between 2 timespan bounds.
                var timer = act.Padlock.IsTimerLock() ? Generators.GetRandomTimeSpan(act.LowerBound, act.UpperBound) : TimeSpan.Zero;
                Logger.LogDebug("Locking a restraint item with " + act.Padlock, LoggerType.ToyboxTriggers);
                PushClientRestraintUpdate newLockInfo = new(_pairs.GetOnlineUserDatas(), DataUpdateType.Locked)
                {
                    Padlock = act.Padlock,
                    Password = password,
                    Timer = new DateTimeOffset(DateTime.UtcNow + timer),
                    Assigner = MainHub.UID
                };
                retCode = (await _hub.UserPushDataRestraint(newLockInfo)).ErrorCode;
                break;

            case NewState.Disabled:
                if (!restraint.CanRemove() || !_restraints.Storage.Contains(act.RestrictionId))
                    return false;

                Logger.LogDebug("Removing a restraint item from layer " + act.RestrictionId, LoggerType.ToyboxTriggers);
                retCode = (await _hub.UserPushDataRestraint(new(_pairs.GetOnlineUserDatas(), DataUpdateType.Removed))).ErrorCode;
                break;

            default:
                return false;
        }

        if (retCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError("Restraint action failed with error code: " + retCode);
            return false;
        }

        return true;
    }

    private bool DoMoodleAction(MoodleAction act, string enactor)
    {
        if(!IpcCallerMoodles.APIAvailable || act.MoodleItem.Id.IsEmptyGuid())
        {
            Logger.LogWarning("Moodles not available, cannot execute moodle trigger.");
            return false;
        }

        Logger.LogDebug("Applying a Moodle action to the player.", LoggerType.IpcMoodles);
        _moodles.AddRestrictedMoodle(act.MoodleItem);
        return true;
    }

    private bool DoPiShockAction(PiShockAction act, string enactor)
    {
        if (_playerData.GlobalPerms is not { } perms)
            return false;

        if(perms.GlobalShockShareCode.IsNullOrWhitespace() || !perms.HasValidShareCode())
        {
            Logger.LogWarning("Can't execute Shock Instruction if none are currently connected!");
            return false;
        }

        // execute the instruction with our global share code.
        Logger.LogInformation("DoPiShock Action is executing instruction based on global sharecode settings!", LoggerType.PiShock);
        var shareCode = perms.GlobalShockShareCode;
        Mediator.Publish(new PiShockExecuteOperation(shareCode, (int)act.ShockInstruction.OpCode, act.ShockInstruction.Intensity, act.ShockInstruction.Duration));
        return true;
    }

    private bool DoSexToyAction(SexToyAction act, string enactor)
    {
        // Nothing atm.
        return true;
    }
}





