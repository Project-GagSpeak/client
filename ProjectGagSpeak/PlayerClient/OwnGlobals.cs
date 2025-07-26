using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using System.Collections.ObjectModel;

namespace GagSpeak.PlayerClient;

// Possibly cleanup and merge with kinkster requests once we get stuff with the hub sorted out and the rest cleaned up.
public sealed class OwnGlobals : DisposableMediatorSubscriberBase
{
    private readonly PiShockProvider _shockies;
    private readonly KinksterManager _kinksters;
    private readonly HardcoreHandler _hcHandler;
    private readonly RemoteService _remoteService;
    private readonly NameplateService _nameplates;

    // Delegate helper for handling special global permission changes.
    private delegate void GlobalPermChangeHandler(object newVal, object? prevVal, string enactor, Kinkster? pair);
    private Dictionary<string, GlobalPermChangeHandler> _changeHandlers = new();
    private static GlobalPerms? _perms = null;

    public OwnGlobals(ILogger<OwnGlobals> logger, GagspeakMediator mediator,
        PiShockProvider shockies, KinksterManager pairs, HardcoreHandler hcHandler,
        RemoteService remoteService, NameplateService nameplates) 
        : base(logger, mediator)
    {
        _shockies = shockies;
        _kinksters = pairs;
        _hcHandler = hcHandler;
        _remoteService = remoteService;
        _nameplates = nameplates;

        Svc.ClientState.Logout += OnLogout;
        // Assign delegates for handling global permission changes.
        _changeHandlers[nameof(GlobalPerms.GaggedNameplate)] = OnGaggedNameplateChange;
        _changeHandlers[nameof(GlobalPerms.HypnosisCustomEffect)] = OnHypnoEffectChange;
        _changeHandlers[nameof(GlobalPerms.LockedFollowing)] = OnLockedFollowingChange;
        _changeHandlers[nameof(GlobalPerms.LockedEmoteState)] = OnLockedEmoteChange;
        _changeHandlers[nameof(GlobalPerms.IndoorConfinement)] = OnForcedStayChange;
        _changeHandlers[nameof(GlobalPerms.ChatBoxesHidden)] = OnHiddenChatBoxesChange;
        _changeHandlers[nameof(GlobalPerms.ChatInputHidden)] = OnHiddenChatInputChange;
        _changeHandlers[nameof(GlobalPerms.ChatInputBlocked)] = OnBlockedChatInputChange;
    }

    /// <summary>
    ///     Static readonly accessor for Global Perms to prevent circular dependency hell. <para/> 
    ///     Instance and setters are handled via the class.
    /// </summary>
    /// <remarks> Might be planting a bomb making an interface possibly nullable, we'll see. </remarks>
    public static IReadOnlyGlobalPerms? Perms => _perms;
    public static EmoteState LockedEmoteState => EmoteState.FromString(_perms?.LockedEmoteState ?? string.Empty);

    /// <summary> Create a mutable clone of the current globals, that is not readonly. </summary>
    /// <remarks> Since it's a record, <c>_perms with {}</c> makes a shallow copy, so original is unaffected. </remarks>
    public static GlobalPerms CurrentPermsWith(Action<GlobalPerms> configure)
    {
        // Shallow copy.
        var copy = (_perms ?? new GlobalPerms()) with { };
        // apply with changes.
        configure(copy);
        // return copy.
        return copy;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ClientState.Logout -= OnLogout;
    }

    private void OnLogout(int type, int code)
    {
        Logger.LogInformation("Clearing Global Permissions on Logout.");
        // this may likely cause some issues down the line, so see if we need to bulk set it off or something.
        ApplyBulkChange(null!, MainHub.UID);
        _perms = null;
    }

    public void ApplyBulkChange(GlobalPerms newGlobals, string enactor)
    {
        var prevGlobals = _perms;
        _perms = newGlobals;
        Mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.BulkUpdate, "Global Permissions Updated in Bulk")));

        // process the special changes that require immidiate action.
        foreach (var kvp in _changeHandlers)
        {
            var prop = typeof(GlobalPerms).GetProperty(kvp.Key);
            if (prop is null)
                continue;

            var newVal = prop.GetValue(newGlobals);
            var prevVal = prevGlobals is not null ? prop.GetValue(prevGlobals) : null;
            Logger.LogInformation($"[OwnGlobalsManager] Processing change for {kvp.Key}: New Value: {newVal}, Previous Value: {prevVal}, Enactor: {enactor}");
            kvp.Value.Invoke(newVal!, prevVal, enactor, null);
        }
    }

    public void DoubleGlobalPermChange(DoubleChangeGlobal dto)
    {
        SingleGlobalPermChange(dto.Enactor, dto.NewPerm1);
        SingleGlobalPermChange(dto.Enactor, dto.NewPerm2);
    }

    public void SingleGlobalPermChange(UserData enactor, KeyValuePair<string, object> newPerm)
    {
        if (string.Equals(enactor.UID, MainHub.UID))
            PerformPermissionChange(enactor, newPerm);
        else if (_kinksters.TryGetKinkster(enactor, out var kinkster))
            PerformPermissionChange(enactor, newPerm, kinkster);
        else
            Logger.LogWarning("Change was not from self or a pair, not setting!");
    }

    private void PerformPermissionChange(UserData enactor, KeyValuePair<string, object> newPerm, Kinkster? pair = null)
    {
        var prevValue = typeof(GlobalPerms).GetProperty(newPerm.Key)?.GetValue(_perms);

        if (!PropertyChanger.TrySetProperty(_perms, newPerm.Key, newPerm.Value, out var _))
        {
            Logger.LogError($"Failed to apply Global Permission change for [{newPerm.Key}] to [{newPerm.Value}].");
            return;
        }

        if (_changeHandlers.TryGetValue(newPerm.Key, out var handler))
            handler(newPerm.Value, prevValue, enactor.UID, pair);

        // Then perform the log.
        SendActionEventMessage(pair?.GetNickAliasOrUid() ?? "Self-Update", enactor.UID, $"[{newPerm.Key}] changed to [{newPerm.Value}]");
    }

    private void SendActionEventMessage(string applierNick, string applierUid, string message)
        => Mediator.Publish(new EventMessage(new(applierNick, applierUid, InteractionType.ForcedPermChange, message)));


    private void OnGaggedNameplateChange(object newVal, object? prevVal, string _, Kinkster? __ = null)
    {
        var newBool = (bool)newVal;
        var prevBool = prevVal as bool?;
        if (!newBool.Equals(prevBool))
            _nameplates.RefreshClientGagState();
    }

    private void OnHypnoEffectChange(object newVal, object? prevVal, string enactor, Kinkster? _)
    {
        var newEffect = newVal as string ?? string.Empty;
        Logger.LogInformation($"Hypnosis Custom Effect changed by {enactor}: {newEffect}");
    }

    private void OnLockedFollowingChange(object newVal, object? prevVal, string enactor, Kinkster? pair = null)
    {
        bool prevState = !string.IsNullOrEmpty(prevVal as string);
        bool newState = !string.IsNullOrEmpty(newVal as string);

        if (!prevState.Equals(newState))
        {
            if (newState) _hcHandler.EnableLockedFollowing(pair);
            else _hcHandler.DisableLockedFollowing(enactor);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.LockedFollowing, newState, enactor, MainHub.UID);
        }
    }

    private void OnLockedEmoteChange(object newVal, object? prevVal, string enactor, Kinkster? _)
    {
        // We convert to bools to prevent switching between certain active states from causing issues.
        var prevState = string.IsNullOrEmpty(prevVal as string);
        var newState = string.IsNullOrEmpty(newVal as string);
        if (!prevState.Equals(newState))
        {
            if (newState) _hcHandler.EnableLockedEmote(enactor);
            else _hcHandler.DisableLockedEmote(enactor);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.LockedEmote, newState, enactor, MainHub.UID);
        }
    }

    private void OnForcedStayChange(object newVal, object? prevVal, string enactor, Kinkster? _ = null)
    {
        var prevState = string.IsNullOrEmpty(prevVal as string);
        var newState = string.IsNullOrEmpty(newVal as string);
        if (!prevState.Equals(newState))
        {
            if (newState) _hcHandler.EnableForcedStay(enactor);
            else _hcHandler.DisableForcedStay(enactor);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.IndoorConfinement, newState, enactor, MainHub.UID);
        }
    }

    private void OnHiddenChatBoxesChange(object newVal, object? prevVal, string enactor, Kinkster? _ = null)
    {
        var prevState = string.IsNullOrEmpty(prevVal as string);
        var newState = string.IsNullOrEmpty(newVal as string);
        if (!prevState.Equals(newState))
        {
            if (newState) _hcHandler.EnableHiddenChatBoxes(enactor);
            else _hcHandler.DisableHiddenChatBoxes(enactor);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatBoxesHidden, newState, enactor, MainHub.UID);
        }
    }

    private void OnHiddenChatInputChange(object newVal, object? prevVal, string enactor, Kinkster? _ = null)
    {
        var prevState = string.IsNullOrEmpty(prevVal as string);
        var newState = string.IsNullOrEmpty(newVal as string);
        if (!prevState.Equals(newState))
        {
            if (newState) _hcHandler.EnableHiddenChatInput(enactor);
            else _hcHandler.DisableHiddenChatInput(enactor);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputHidden, newState, enactor, MainHub.UID);
        }
    }

    private void OnBlockedChatInputChange(object newVal, object? prevVal, string enactor, Kinkster? _ = null)
    {
        var prevState = string.IsNullOrEmpty(prevVal as string);
        var newState = string.IsNullOrEmpty(newVal as string);
        if (!prevState.Equals(newState))
        {
            if (newState) _hcHandler.EnableBlockedChatInput(enactor);
            else _hcHandler.DisableBlockedChatInput(enactor);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputBlocked, newState, enactor, MainHub.UID);
        }
    }

    public void OnPiShockInstruction(ShockCollarAction dto)
    {
        // figure out who sent the command, and see if we have a unique sharecode setup for them.
        if (_kinksters.TryGetKinkster(dto.User, out var enactor))
        {
            var interactionType = dto.OpCode switch { 0 => "shocked", 1 => "vibrated", 2 => "beeped", _ => "unknown" };
            var eventLogMessage = $"Pishock {interactionType}, intensity: {dto.Intensity}, duration: {dto.Duration}";
            Logger.LogInformation($"Received Instruction for {eventLogMessage}", LoggerType.Callbacks);

            if (!enactor.OwnPerms.PiShockShareCode.IsNullOrEmpty())
            {
                Logger.LogDebug("Executing Shock Instruction to UniquePair ShareCode", LoggerType.Callbacks);
                Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
                _shockies.ExecuteOperation(enactor.OwnPerms.PiShockShareCode, dto.OpCode, dto.Intensity, dto.Duration);
                if (dto.OpCode is 0)
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
            }
            else if (_perms is { } g && !g.GlobalShockShareCode.IsNullOrEmpty())
            {
                Logger.LogDebug("Executing Shock Instruction to Global ShareCode", LoggerType.Callbacks);
                Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
                _shockies.ExecuteOperation(g.GlobalShockShareCode, dto.OpCode, dto.Intensity, dto.Duration);
                if (dto.OpCode is 0)
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
            }
            else
            {
                Logger.LogWarning("Someone Attempted to execute an instruction to you, but you don't have any share codes enabled!");
            }
        }
    }

    public void OnHypnosisApplication(HypnoticAction dto)
    {
        if (_perms is not { } g)
            throw new NullReferenceException($"Globals is Null.");

        if (!_kinksters.TryGetKinkster(dto.User, out var enactingKinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        // prevent change is already under hypnosis.
        if (_hcHandler.IsHypnotized || !string.IsNullOrEmpty(g.HypnosisCustomEffect))
        {
            Logger.LogWarning($"Kinkster [{enactingKinkster.GetNickAliasOrUid()}] attempted to hypnotize while already hypnotized, discarding!");
            return;
        }

        // handle the hypnosis effect.
    }

    public void OnConfineByAddress(ConfineByAddress dto)
    {
        if (_perms is not { } g)
            throw new NullReferenceException($"Globals is Null.");

        if (!_kinksters.TryGetKinkster(dto.User, out var enactingKinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");

        // an override for forced stay that can inject spesific addresses to go to.
    }

    public void OnImprisonByLocation(ImprisonAtPosition dto)
    {
        if (_perms is not { } g)
            throw new NullReferenceException($"Globals is Null.");

        if (!_kinksters.TryGetKinkster(dto.User, out var enactingKinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");

        // kind of like forced sit, but with a little wiggle room.

    }
}
