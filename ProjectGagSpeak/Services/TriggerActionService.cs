using CkCommons.Helpers;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using OtterGui.Extensions;

namespace GagSpeak.Services;

/// <summary>
///     Handles how triggers are fired by any source that can use Triggers. (Alias, Triggers Module)
/// </summary>
/// <remarks>
///     Makes sure that actions are not fired too often, and awaits the DataDistribution actions directly,
///     preventing Multi-Action alias's from executing in parallel. (if this is even something to be worried about?)
/// </remarks>
public class TriggerActionService
{
    private readonly ILogger<TriggerActionService> _logger;
    private readonly PiShockProvider _shockies;
    private readonly KinksterManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly BuzzToyManager _toys;
    private readonly MoodleHandler _moodles;
    private readonly DataDistributionService _distributer; // Allows us to directly execute the calls and await for them to finish.

    // (This rate limiter is kind of busted at the moment, maybe find a better solution for this)
    private ActionRateLimiter _rateLimiter = new(TimeSpan.FromSeconds(3), 1, 3, 3, 3);

    public TriggerActionService(
        ILogger<TriggerActionService> logger,
        PiShockProvider shockies,
        KinksterManager pairs,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        BuzzToyManager toys,
        MoodleHandler moodles)
    {
        _logger = logger;
        _shockies = shockies;
        _pairs = pairs;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _toys = toys;
        _moodles = moodles;
    }

    public async Task<bool> HandleActionAsync(InvokableGsAction invokableAction, string enactor, ActionSource source)
    {
        try
        {
            // Rate-limit check before invoking action
            if (!CanExecuteAction(invokableAction))
            {
                _logger.LogDebug("Rate limit exceeded for action: " + invokableAction.GetType().Name, LoggerType.Triggers);
                return false;
            }

            // perform an action based on the type.
            return invokableAction switch
            {
                TextAction        ta  =>       DoTextAction(ta, enactor, source),
                GagAction         ga  => await DoGagAction(ga, enactor),
                RestrictionAction rsa => await DoRestrictionAction(rsa, enactor),
                RestraintAction   rta => await DoRestraintAction(rta, enactor),
                MoodleAction      ma  => await DoMoodleAction(ma, enactor),
                PiShockAction     ps  =>       DoPiShockAction(ps, enactor),
                SexToyAction      sta =>       DoSexToyAction(sta, enactor),
                _ => throw new InvalidOperationException($"Unhandled action type: {invokableAction.GetType().Name}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action: {Action}", invokableAction);
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
            _logger.LogTrace("Message is empty after alias conversion.", LoggerType.Puppeteer);
            return false;
        }

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();
        
        // Handle final checks based on the source type.
        switch (source)
        {
            case ActionSource.GlobalAlias:
                if(OwnGlobals.Perms is not { } globals || !globals.PuppetPerms.HasAny(PuppetPerms.Alias))
                    return false;

                break;
            case ActionSource.PairAlias:
                if (_pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == enactor) is not { } match)
                    return false;
                // If it was a match, return false if you have not given the pair alias permissions.
                if(!match.OwnPerms.PuppetPerms.HasAny(PuppetPerms.Alias))
                    return false;

                break;
            default:
                _logger.LogWarning("Unknown or disallowed type for Text Action.");
                return false;
        }

        _logger.LogInformation("Text Action is being executed.", LoggerType.Puppeteer);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
        ChatService.EnqueueMessage("/" + remainingMessage.TextValue);
        return true;
    }

    private async Task<bool> DoGagAction(GagAction act, string enactor)
    {
        if(_gags.ServerGagData is not { } gagData)
            return false;

        var layerIdx = -1;
        var gagSlot = new ActiveGagSlot();
        DataUpdateType updateType;

        switch (act.NewState)
        {
            case NewState.Enabled:
                layerIdx = gagData.FindFirstUnused();
                if (layerIdx == -1)
                    return false;

                _logger.LogInformation($"Applying [{act.GagType}] to layer {layerIdx}", LoggerType.Triggers | LoggerType.Gags);
                gagSlot.GagItem = act.GagType;
                gagSlot.Enabler = MainHub.UID;
                updateType = DataUpdateType.Applied;
                break;

            case NewState.Locked:
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
                layerIdx = gagData.FindFirstUnlocked();
                if (layerIdx == -1)
                    return false;

                // We have found one to lock. Check what lock we chose, and define accordingly.
                var password = act.Padlock switch
                {
                    Padlocks.PasswordPadlock => Generators.GetRandomCharaString(10),
                    Padlocks.CombinationPadlock => Generators.GetRandomIntString(4),
                    Padlocks.TimerPasswordPadlock => Generators.GetRandomCharaString(10),
                    _ => string.Empty
                };
                // define a random time between 2 timespan bounds.
                var timer = act.Padlock.IsTimerLock() ? Generators.GetRandomTimeSpan(act.LowerBound, act.UpperBound) : TimeSpan.Zero;
                _logger.LogInformation($"Locking [{act.GagType}] with [{act.Padlock}] on layer {layerIdx}", LoggerType.Triggers);
                gagSlot.Padlock = act.Padlock;
                gagSlot.Password = password;
                gagSlot.Timer = new DateTimeOffset(DateTime.UtcNow + timer);
                gagSlot.PadlockAssigner = MainHub.UID;
                updateType = DataUpdateType.Locked;
                break;

            case NewState.Disabled:
                layerIdx = act.GagType is GagType.None ? gagData.FindOutermostActive() : gagData.FindOutermostActive(act.GagType);
                if (layerIdx == -1)
                    return false;

                _logger.LogDebug($"Removing [{act.GagType}] from layer {layerIdx}", LoggerType.Triggers);
                updateType = DataUpdateType.Removed;
                break;

            default:
                return false;
        }

        // Call the new distribution method
        if (!await _distributer.PushGagTriggerAction(layerIdx, gagSlot, updateType))
        {
            _logger.LogWarning("The GagTriggerAction was not processed sucessfully by the server!");
            return false;
        }

        return true;
    }

    private async Task<bool> DoRestrictionAction(RestrictionAction act, string enactor)
    {
        if (_restrictions.ServerRestrictionData is not { } restrictions)
            return false;

        var layerIdx = -1;
        var restriction = new ActiveRestriction();
        DataUpdateType updateType;

        switch (act.NewState)
        {
            case NewState.Enabled:
                // grab the right restriction first.
                layerIdx = act.LayerIdx == -1
                    ? restrictions.Restrictions.IndexOf(x => x.Identifier== Guid.Empty)
                    : act.LayerIdx;

                if (layerIdx == -1 || restrictions.Restrictions[layerIdx].IsLocked() || !restrictions.Restrictions[layerIdx].CanApply())
                    return false;

                _logger.LogInformation($"Applying restriction [{act.RestrictionId}] to layer {layerIdx}", LoggerType.Triggers);
                restriction.Identifier = act.RestrictionId;
                restriction.Enabler = MainHub.UID;
                updateType = DataUpdateType.Applied;
                break;

            case NewState.Locked:
                layerIdx = act.LayerIdx == -1
                    ? restrictions.Restrictions.IndexOf(x => x.Identifier != Guid.Empty && x.CanLock())
                    : act.LayerIdx;

                if (layerIdx == -1 || !restrictions.Restrictions[layerIdx].CanLock() || act.Padlock is Padlocks.None)
                    return false;

                _logger.LogInformation($"Locking restriction [{act.RestrictionId}] with [{act.Padlock}] on layer {layerIdx}", LoggerType.Triggers);
                restriction.Padlock = act.Padlock;
                restriction.Password = act.Padlock switch
                {
                    Padlocks.PasswordPadlock => Generators.GetRandomCharaString(10),
                    Padlocks.CombinationPadlock => Generators.GetRandomIntString(4),
                    Padlocks.TimerPasswordPadlock => Generators.GetRandomCharaString(10),
                    _ => string.Empty
                };
                restriction.Timer = new DateTimeOffset(DateTime.UtcNow + (act.Padlock.IsTimerLock() ? Generators.GetRandomTimeSpan(act.LowerBound, act.UpperBound) : TimeSpan.Zero));
                restriction.PadlockAssigner = MainHub.UID;
                updateType = DataUpdateType.Locked;
                break;

            case NewState.Disabled:
                layerIdx = act.RestrictionId != Guid.Empty
                    ? restrictions.FindOutermostActiveUnlocked()
                    : restrictions.Restrictions.IndexOf(x => x.Identifier == act.RestrictionId);

                if (layerIdx == -1 || !restrictions.Restrictions[layerIdx].CanRemove())
                    return false;

                _logger.LogDebug($"Removing restriction [{act.RestrictionId}] from layer {layerIdx}", LoggerType.Triggers);
                updateType = DataUpdateType.Removed;
                break;

            default:
                return false;
        }

        if (!await _distributer.PushRestrictionTriggerAction(layerIdx, restriction, updateType))
        {
            _logger.LogWarning("The RestrictionTriggerAction was not processed successfully by the server!");
            return false;
        }

        return true;
    }

    private async Task<bool> DoRestraintAction(RestraintAction act, string enactor)
    {
        if(_restraints.ServerData is not { } restraint)
            return false;

        CharaActiveRestraint restraintData = new() { Identifier = act.RestrictionId };
        DataUpdateType updateType;

        switch (act.NewState)
        {
            case NewState.Enabled:
                if (!restraint.CanApply() || !_restraints.Storage.Contains(act.RestrictionId))
                    return false;

                _logger.LogDebug($"Applying restraint [{act.RestrictionId}]", LoggerType.Triggers);
                restraintData.Enabler = MainHub.UID;
                updateType = DataUpdateType.Applied;
                break;

            case NewState.Locked:
                if (!restraint.CanLock() || act.Padlock is Padlocks.None)
                    return false;

                _logger.LogDebug($"Locking restraint [{act.RestrictionId}] with [{act.Padlock}]", LoggerType.Triggers);
                restraintData.Padlock = act.Padlock;
                restraintData.Password = act.Padlock switch
                {
                    Padlocks.PasswordPadlock => Generators.GetRandomCharaString(10),
                    Padlocks.CombinationPadlock => Generators.GetRandomIntString(4),
                    Padlocks.TimerPasswordPadlock => Generators.GetRandomCharaString(10),
                    _ => string.Empty
                };
                restraintData.Timer = new DateTimeOffset(DateTime.UtcNow + (act.Padlock.IsTimerLock() ? Generators.GetRandomTimeSpan(act.LowerBound, act.UpperBound) : TimeSpan.Zero));
                restraintData.PadlockAssigner = MainHub.UID;
                updateType = DataUpdateType.Locked;
                break;

            case NewState.Disabled:
                if (!restraint.CanRemove() || !_restraints.Storage.Contains(act.RestrictionId))
                    return false;

                _logger.LogDebug($"Removing restraint [{act.RestrictionId}]", LoggerType.Triggers);
                updateType = DataUpdateType.Removed;
                break;

            default:
                return false;
        }

        if (!await _distributer.PushRestraintTriggerAction(restraintData, updateType))
        {
            _logger.LogWarning("The RestraintTriggerAction was not processed successfully by the server!");
            return false;
        }

        return true;
    }

    private async Task<bool> DoMoodleAction(MoodleAction act, string enactor)
    {
        if(!IpcCallerMoodles.APIAvailable || act.MoodleItem.Id== Guid.Empty)
        {
            _logger.LogWarning("Moodles not available, cannot execute moodle trigger.");
            return false;
        }

        _logger.LogDebug("Applying a Moodle action to the player.", LoggerType.IpcMoodles);
        await _moodles.ApplyMoodle(act.MoodleItem);
        return true;
    }

    private bool DoPiShockAction(PiShockAction act, string enactor)
    {
        if (OwnGlobals.Perms is not { } perms)
            return false;

        if(string.IsNullOrWhiteSpace(perms.GlobalShockShareCode) || !perms.HasValidShareCode())
        {
            _logger.LogWarning("Can't execute Shock Instruction if none are currently connected!");
            return false;
        }

        // execute the instruction with our global share code.
        _logger.LogInformation("DoPiShock Action is executing instruction based on global sharecode settings!", LoggerType.PiShock);
        var shareCode = perms.GlobalShockShareCode;
        _shockies.ExecuteOperation(shareCode, (int)act.ShockInstruction.OpCode, act.ShockInstruction.Intensity, act.ShockInstruction.Duration);
        return true;
    }

    private bool DoSexToyAction(SexToyAction act, string enactor)
    {
        // Nothing atm.
        return true;
    }
}
