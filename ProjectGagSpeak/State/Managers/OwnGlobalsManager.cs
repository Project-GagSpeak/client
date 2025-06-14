using GagSpeak.PlayerClient;
using GagSpeak.Kinksters.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;

namespace GagSpeak.State.Managers;

public sealed class OwnGlobalsManager
{
    private readonly ILogger<OwnGlobalsManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly GlobalPermissions _globals;
    private readonly PairManager _pairs;
    private readonly HardcoreHandler _hcHandler;
    public OwnGlobalsManager(
        ILogger<OwnGlobalsManager> logger,
        GagspeakMediator mediator,
        GlobalPermissions globals,
        PairManager pairs,
        HardcoreHandler hcHandler)
    {
        _logger = logger;
        _mediator = mediator;
        _globals = globals;
        _pairs = pairs;
        _hcHandler = hcHandler;
    }

    public void ApplyBulkChange(GlobalPerms newGlobals, string enactor)
    {
        var prevGlobals = _globals.Current;
        _globals.ApplyFullDataChange(newGlobals);
        _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.BulkUpdate, "Global Permissions Updated in Bulk")));

        // Check for changes in each hardcore global permission
        OnForcedFollowChange(newGlobals.ForcedFollow, prevGlobals?.ForcedFollow, enactor);
        OnForcedEmoteChange(newGlobals.ForcedEmoteState, prevGlobals?.ForcedEmoteState, enactor);
        OnForcedStayChange(newGlobals.ForcedStay, prevGlobals?.ForcedStay, enactor);
        OnHiddenChatBoxesChange(newGlobals.ChatBoxesHidden, prevGlobals?.ChatBoxesHidden, enactor);
        OnHiddenChatInputChange(newGlobals.ChatInputHidden, prevGlobals?.ChatInputHidden, enactor);
        OnBlockedChatInputChange(newGlobals.ChatInputBlocked, prevGlobals?.ChatInputBlocked, enactor);
    }

    public void SingleGlobalPermissionChange(SingleChangeGlobal dto)
    {
        // Handle based on direction.
        if (string.Equals(dto.Enactor.UID, MainHub.UID))
        {
            _logger.LogDebug("OWN SingleChangeGlobal (From Self): " + dto, LoggerType.Callbacks);
            PerformPermissionChange(dto);
        }
        else if (_pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == dto.User.UID) is { } pair)
        {
            _logger.LogDebug("OWN SingleChangeGlobal (From Other): " + dto, LoggerType.Callbacks);
            PerformPermissionChange(dto, pair);
        }
        else
        {
            _logger.LogWarning("Change was not from self or a pair, not setting!");
        }
    }

    private void PerformPermissionChange(SingleChangeGlobal dto, Pair? pair = null)
    {
        // retrieve the previous value, in the case it is a hardcore change.
        string? prevValue = dto.NewPerm.Key switch
        {
            nameof(GlobalPerms.ForcedFollow) => _globals.Current?.ForcedFollow,
            nameof(GlobalPerms.ForcedEmoteState) => _globals.Current?.ForcedEmoteState,
            nameof(GlobalPerms.ForcedStay) => _globals.Current?.ForcedStay,
            nameof(GlobalPerms.ChatBoxesHidden) => _globals.Current?.ChatBoxesHidden,
            nameof(GlobalPerms.ChatInputHidden) => _globals.Current?.ChatInputHidden,
            nameof(GlobalPerms.ChatInputBlocked) => _globals.Current?.ChatInputBlocked,
            _ => null
        };

        // Attempt to make the change.
        if (!_globals.TryApplyChange(dto.NewPerm.Key, dto.NewPerm.Value))
        {
            _logger.LogError($"Failed to apply Global Permission change for [{dto.NewPerm.Key}] to [{dto.NewPerm.Value}].");
            return;
        }

        // Handle the hardcore permissions.
        switch (dto.NewPerm.Key)
        {
            case nameof(GlobalPerms.ForcedFollow):
                OnForcedFollowChange((string)dto.NewPerm.Value, prevValue, dto.Enactor.UID, pair);
                break;
            case nameof(GlobalPerms.ForcedEmoteState):
                OnForcedEmoteChange((string)dto.NewPerm.Value, prevValue, dto.Enactor.UID);
                break;
            case nameof(GlobalPerms.ForcedStay):
                OnForcedStayChange((string)dto.NewPerm.Value, prevValue, dto.Enactor.UID);
                break;
            case nameof(GlobalPerms.ChatBoxesHidden):
                OnHiddenChatBoxesChange((string)dto.NewPerm.Value, prevValue, dto.Enactor.UID);
                break;
            case nameof(GlobalPerms.ChatInputHidden):
                OnHiddenChatInputChange((string)dto.NewPerm.Value, prevValue, dto.Enactor.UID);
                break;
            case nameof(GlobalPerms.ChatInputBlocked):
                OnBlockedChatInputChange((string)dto.NewPerm.Value, prevValue, dto.Enactor.UID);
                break;
        } 

        // Then perform the log.
        if (pair is null)
            SendActionEventMessage("Self-Update", MainHub.UID, $"[{dto.NewPerm.Key}] changed to [{dto.NewPerm.Value}]");
        else
            SendActionEventMessage(pair.GetNickAliasOrUid(), dto.Enactor.UID, $"[{dto.NewPerm.Key}] changed to [{dto.NewPerm.Value}]");
    }

    private void SendActionEventMessage(string applierNick, string applierUid, string message)
        => _mediator.Publish(new EventMessage(new(applierNick, applierUid, InteractionType.ForcedPermChange, message)));

    private void OnForcedFollowChange(string newPermVal, string? prevVal, string enactor, Pair? pair = null)
    {
        // We convert to bools to prevent switching between certain active states from causing issues.
        bool prevState = string.IsNullOrEmpty(prevVal);
        bool newState = string.IsNullOrEmpty(newPermVal);
        // Attempt to apply the change, enabling/disabling the respective handler if the states are different.
        if (_globals.TryApplyChange(nameof(GlobalPerms.ForcedFollow), newPermVal) && (prevState != newState))
        {
            if (newState) _hcHandler.EnableForcedFollow(pair);
            else _hcHandler.DisableForcedFollow(enactor);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ForcedFollow, newState, enactor, MainHub.UID);
        }
    }

    private void OnForcedEmoteChange(string newPermVal, string? prevVal, string enactor)
    {
        // We convert to bools to prevent switching between certain active states from causing issues.
        bool prevState = string.IsNullOrEmpty(prevVal);
        bool newState = string.IsNullOrEmpty(newPermVal);
        // Attempt to apply the change, enabling/disabling the respective handler if the states are different.
        if (_globals.TryApplyChange(nameof(GlobalPerms.ForcedEmoteState), newPermVal) && (prevState != newState))
        {
            if (newState) _hcHandler.EnableForcedEmote(enactor);
            else _hcHandler.DisableForcedEmote(enactor);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ForcedEmote, newState, enactor, MainHub.UID);
        }
    }

    private void OnForcedStayChange(string newPermVal, string? prevVal, string enactor)
    {
        // We convert to bools to prevent switching between certain active states from causing issues.
        bool prevState = string.IsNullOrEmpty(prevVal);
        bool newState = string.IsNullOrEmpty(newPermVal);
        // Attempt to apply the change, enabling/disabling the respective handler if the states are different.
        if (_globals.TryApplyChange(nameof(GlobalPerms.ForcedStay), newPermVal) && (prevState != newState))
        {
            if (newState) _hcHandler.EnableForcedStay(enactor);
            else _hcHandler.DisableForcedStay(enactor);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ForcedStay, newState, enactor, MainHub.UID);
        }
    }

    private void OnHiddenChatBoxesChange(string newPermVal, string? prevVal, string enactor)
    {
        // We convert to bools to prevent switching between certain active states from causing issues.
        bool prevState = string.IsNullOrEmpty(prevVal);
        bool newState = string.IsNullOrEmpty(newPermVal);
        // Attempt to apply the change, enabling/disabling the respective handler if the states are different.
        if (_globals.TryApplyChange(nameof(GlobalPerms.ChatBoxesHidden), newPermVal) && (prevState != newState))
        {
            if (newState) _hcHandler.EnableHiddenChatBoxes(enactor);
            else _hcHandler.DisableHiddenChatBoxes(enactor);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatBoxesHidden, newState, enactor, MainHub.UID);
        }
    }

    private void OnHiddenChatInputChange(string newPermVal, string? prevVal, string enactor)
    {
        // We convert to bools to prevent switching between certain active states from causing issues.
        bool prevState = string.IsNullOrEmpty(prevVal);
        bool newState = string.IsNullOrEmpty(newPermVal);
        // Attempt to apply the change, enabling/disabling the respective handler if the states are different.
        if (_globals.TryApplyChange(nameof(GlobalPerms.ChatInputHidden), newPermVal) && (prevState != newState))
        {
            if (newState) _hcHandler.EnableHiddenChatInput(enactor);
            else _hcHandler.DisableHiddenChatInput(enactor);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputHidden, newState, enactor, MainHub.UID);
        }
    }

    private void OnBlockedChatInputChange(string newPermVal, string? prevVal, string enactor)
    {
        // We convert to bools to prevent switching between certain active states from causing issues.
        bool prevState = string.IsNullOrEmpty(prevVal);
        bool newState = string.IsNullOrEmpty(newPermVal);
        // Attempt to apply the change, enabling/disabling the respective handler if the states are different.
        if (_globals.TryApplyChange(nameof(GlobalPerms.ChatInputBlocked), newPermVal) && (prevState != newState))
        {
            if (newState) _hcHandler.EnableBlockedChatInput(enactor);
            else _hcHandler.DisableBlockedChatInput(enactor);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputBlocked, newState, enactor, MainHub.UID);
        }
    }
}
