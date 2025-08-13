using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;

namespace GagSpeak.State.Listeners;

/// <summary> Listeners for components that are not in the toybox compartment nor are visual components. </summary>
/// <remarks> May be catagorized later, but are filtered into here for now. </remarks>
public sealed class OwnGlobalsListener
{
    // holds the special permissions in a hashset for quick lookup.
    private static readonly Dictionary<string, Type> SpecialPermissions = new()
    {
        { nameof(GlobalPerms.GaggedNameplate), typeof(bool) },
        { nameof(GlobalPerms.HypnosisCustomEffect), typeof(string) },
        { nameof(GlobalPerms.LockedFollowing), typeof(string) },
        { nameof(GlobalPerms.LockedEmoteState), typeof(string) },
        { nameof(GlobalPerms.IndoorConfinement), typeof(string) },
        { nameof(GlobalPerms.ChatBoxesHidden), typeof(string) },
        { nameof(GlobalPerms.ChatInputHidden), typeof(string) },
        { nameof(GlobalPerms.ChatInputBlocked), typeof(string) }
    };

    private readonly ILogger<OwnGlobalsListener> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly OwnGlobals _perms;
    private readonly PiShockProvider _shockies;
    private readonly HardcoreHandler _hcHandler;
    private readonly KinksterManager _kinksters;
    private readonly RemoteService _remoteService;
    private readonly NameplateService _nameplates;
    public OwnGlobalsListener(ILogger<OwnGlobalsListener> logger, GagspeakMediator mediator, 
        OwnGlobals perms, PiShockProvider shockies, HardcoreHandler hcHandler, KinksterManager pairs, 
        RemoteService remoteService, NameplateService nameplates)
    {
        _logger = logger;
        _mediator = mediator;
        _perms = perms;
        _shockies = shockies;
        _hcHandler = hcHandler;
        _kinksters = pairs;
        _remoteService = remoteService;
        _nameplates = nameplates;
    }

    public bool IsSpecialPermission(string permName)
        => SpecialPermissions.ContainsKey(permName);

    public object? GetCurrentValue(string permName)
        => _perms.GetCurrentValue(permName);

    /// <summary>
    ///     Validates if it is ok for the requested global change to occur. <para />
    ///     Verification is done against internal client states via the hardcore 
    ///     handler, to ensure it is safe to do so.
    /// </summary>
    /// <returns> True if the permission can be set, false otherwise. </returns>
    public bool CanSetPermission(UserData enactor, string permName, object newValue)
    {
        if (OwnGlobals.Perms is null)
            return false;
        // if not a special permission, its always true.
        if (!IsSpecialPermission(permName))
            return true;
        // reject if the type does not match expected.
        if (!(SpecialPermissions.TryGetValue(permName, out var type) && type.IsInstanceOfType(newValue)))
            return false;
        // Validate special permissions.
        return permName switch
        {
            nameof(GlobalPerms.GaggedNameplate) => true,
            nameof(GlobalPerms.HypnosisCustomEffect) => CanChangeHypnosis(OwnGlobals.Perms, enactor, (string)newValue),
            nameof(GlobalPerms.LockedFollowing) => CanChangeLockedFollow(OwnGlobals.Perms, enactor, (string)newValue),
            nameof(GlobalPerms.LockedEmoteState) => CanChangeLockedEmoteState(OwnGlobals.Perms, enactor, (string)newValue),
            nameof(GlobalPerms.IndoorConfinement) => CanChangeIndoorConfinement(OwnGlobals.Perms, enactor, (string)newValue),
            nameof(GlobalPerms.Imprisonment) => CanChangeImprisonment(OwnGlobals.Perms, enactor, (string)newValue),
            nameof(GlobalPerms.ChatBoxesHidden) => CanChangeChatBoxesHidden(OwnGlobals.Perms, enactor, (string)newValue),
            nameof(GlobalPerms.ChatInputHidden) => CanChangeChatInputHidden(OwnGlobals.Perms, enactor, (string)newValue),
            nameof(GlobalPerms.ChatInputBlocked) => CanChangeChatInputBlocked(OwnGlobals.Perms, enactor, (string)newValue),
            _ => false
        };
    }

    /// <summary>
    ///     Should only ever be called by the client. <para />
    ///     A bulk global change is determined as 'absolutele', and
    ///     will enable/disable any active special action without hesitation.
    /// </summary>
    public void SetAllGlobalPermissions(GlobalPerms newPermissions)
    {
        var prevGlobals = OwnGlobals.Perms;
        _perms.ApplyBulkChange(newPermissions);
        _hcHandler.ProcessBulkGlobalUpdate(prevGlobals, newPermissions);
    }

    /// <summary>
    ///     Runs a check to see if the permission can be set, and if so, sets it.
    ///     Internally calls <see cref="CanSetPermission" /> to validate the change.
    /// </summary>
    /// <returns> If the permission was valid and set, false otherwise. </returns>
    public bool SetPermissionSafe(UserData enactor, string permName, object newValue)
    {
        if (!CanSetPermission(enactor, permName, newValue))
            return false;
        SetPermissionUnsafe(enactor, permName, newValue);
        return true;
    }

    /// <summary>
    ///     Executes the set permission directly, without running <see cref="CanSetPermission" /> checks."/>.
    ///     Only run this if you are sure you have validated the change, otherwise desync may occur.
    /// </summary>
    public void SetPermissionUnsafe(UserData enactor, string permName, object newValue)
    {
        if (OwnGlobals.Perms is null)
            return;
        // Remember we can use unsafe methods since this should be validated!
        var currentValue = GetCurrentValue(permName)!;
        _logger.LogDebug($"Previous Value was: {currentValue}");
        // update it internally.
        _perms.UpdatePermissionValue(enactor, permName, newValue);
        _logger.LogDebug($"Updated {permName} to {newValue} for {enactor.AliasOrUID}");
        // if a special permission, process the change.
        if (IsSpecialPermission(permName))
        {
            _logger.LogDebug($"Processing Special Permission Change for {permName} to {newValue}");
            ProcessSpecialPermissionChange(enactor, permName, currentValue, newValue);
        }
        else
        {
            _logger.LogDebug("Not a special permission, no further processing required.");
        }
    }

    /// <summary>
    ///     In case we manually set the permission through a direct call, 
    ///     and doesn't from a callback, we want to process the change after. <para />
    ///     This can be done here.
    /// </summary>
    /// <exception cref="ArgumentException"> When the permission was not a special permission. </exception>
    public void ProcessSpecialPermissionChange(UserData enactor, string permName, object currentValue, object newValue)
    {
        var _ = permName switch
        {
            nameof(GlobalPerms.GaggedNameplate) => OnNamePlateStateChange(enactor, (bool)currentValue, (bool)newValue),
            nameof(GlobalPerms.HypnosisCustomEffect) => OnHypnoEffectChange(enactor, (string)currentValue, (string)newValue),
            nameof(GlobalPerms.LockedFollowing) => OnLockedFollowChange(enactor, (string)currentValue, (string)newValue),
            nameof(GlobalPerms.LockedEmoteState) => OnLockedEmoteStateChange(enactor, (string)currentValue, (string)newValue),
            nameof(GlobalPerms.IndoorConfinement) => OnIndoorConfinementChange(enactor, (string)currentValue, (string)newValue),
            nameof(GlobalPerms.Imprisonment) => OnImprisonmentChange(enactor, (string)currentValue, (string)newValue),
            nameof(GlobalPerms.ChatBoxesHidden) => OnChatBoxesHiddenChange(enactor, (string)currentValue, (string)newValue),
            nameof(GlobalPerms.ChatInputHidden) => OnChatInputHiddenChange(enactor, (string)currentValue, (string)newValue),
            nameof(GlobalPerms.ChatInputBlocked) => OnChatInputBlockedChange(enactor, (string)currentValue, (string)newValue),
            _ => throw new ArgumentException($"Unknown Special Permission: {permName}")
        };
    }

    public bool CanApplyHypnoEffect(UserData enactor, int time, HypnoticEffect effect, string? img = null)
        => OwnGlobals.Perms is not null && _hcHandler.CanApplyTimedHypnoEffect(enactor, OwnGlobals.Perms, effect, TimeSpan.FromSeconds(time), img);

    public bool ApplyHypnoEffect(UserData enactor, int time, HypnoticEffect effect, string? img = null)
    {
        if (!CanApplyHypnoEffect(enactor, time, effect, img))
            return false;
        ApplyHypnoEffectUnsafe(enactor, time, effect, img);
        return true;
    }

    public void ApplyHypnoEffectUnsafe(UserData enactor, int time, HypnoticEffect effect, string? img = null)
    {
        _hcHandler.ApplyTimedHypnoEffectUnsafe(enactor, effect, TimeSpan.FromSeconds(time), img);
        // This was already updated for everyone else, so dont worry about it.
        _perms.UpdatePermissionValue(enactor, nameof(GlobalPerms.HypnosisCustomEffect), enactor.UID);
    }

    public bool CanConfineIndoors(UserData enactor)
        => OwnGlobals.Perms is not null && _hcHandler.CanEnableIndoorConfinement(enactor, OwnGlobals.Perms);

    public bool ConfineIndoors(UserData enactor, AddressBookEntry? address = null)
    {
        if (!CanConfineIndoors(enactor))
            return false;
        ConfineIndoorsUnsafe(enactor, address);
        return true;
    }

    public void ConfineIndoorsUnsafe(UserData enactor, AddressBookEntry? address = null)
    {
        _hcHandler.EnableConfinementUnsafe(enactor, address);
        // This was already updated for everyone else, so dont worry about it.
        _perms.UpdatePermissionValue(enactor, nameof(GlobalPerms.IndoorConfinement), enactor.UID);
    }

    public bool CanImprison(UserData enactor, Vector3 position, float maxRadius)
        => OwnGlobals.Perms is not null && _hcHandler.CanEnableImprisonment(enactor, OwnGlobals.Perms, position);

    public bool Imprison(UserData enactor, Vector3 position, float maxRadius)
    {
        if (!CanImprison(enactor, position, maxRadius))
            return false;
        ImprisonUnsafe(enactor, position, maxRadius);
        return true;
    }

    public void ImprisonUnsafe(UserData enactor, Vector3 position, float maxRadius)
    {
        _hcHandler.EnableImprisonmentUnsafe(enactor, position, maxRadius);
        // This was already updated for everyone else, so dont worry about it.
        _perms.UpdatePermissionValue(enactor, nameof(GlobalPerms.Imprisonment), enactor.UID);
    }


    // Every function in this region is assumed to be already valid! 
    #region Permission Changes
    private bool OnNamePlateStateChange(UserData enactor, bool prevValue, bool newValue)
    {
        if (prevValue != newValue) // Change if different!
            _nameplates.RefreshClientGagState();
        return true;
    }
    private bool OnHypnoEffectChange(UserData enactor, string prevValue, string newValue)
    {
        // Applying is handled via other callbacks, instead, only handle removal.
        if (!string.IsNullOrEmpty(prevValue) && string.IsNullOrEmpty(newValue))
            _hcHandler.RemoveHypnoEffectUnsafe(enactor);
        return true;
    }

    private bool OnLockedFollowChange(UserData enactor, string prevValue, string newValue)
    {
        return true;

    }

    private bool OnLockedEmoteStateChange(UserData enactor, string prevValue, string newValue)
    {
        return true;

    }

    private bool OnIndoorConfinementChange(UserData enactor, string prevValue, string newValue)
    {
        return true;

    }

    private bool OnImprisonmentChange(UserData enactor, string prevValue, string newValue)
    {
        return true;

    }

    private bool OnChatBoxesHiddenChange(UserData enactor, string prevValue, string newValue)
    {
        return true;

    }

    private bool OnChatInputHiddenChange(UserData enactor, string prevValue, string newValue)
    {
        return true;

    }

    private bool OnChatInputBlockedChange(UserData enactor, string prevValue, string newValue)
    {
        return true;

    }

    public void OnPiShockInstruction(ShockCollarAction dto)
    {
        // figure out who sent the command, and see if we have a unique sharecode setup for them.
        if (!_kinksters.TryGetKinkster(dto.User, out var enactor))
            return;

        var interactionType = dto.OpCode switch { 0 => "shocked", 1 => "vibrated", 2 => "beeped", _ => "unknown" };
        var eventLogMessage = $"Pishock {interactionType}, intensity: {dto.Intensity}, duration: {dto.Duration}";
        _logger.LogInformation($"Received Instruction for {eventLogMessage}", LoggerType.Callbacks);

        if (!enactor.OwnPerms.PiShockShareCode.IsNullOrEmpty())
        {
            _logger.LogDebug("Executing Shock Instruction to UniquePair ShareCode", LoggerType.Callbacks);
            _mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
            _shockies.ExecuteOperation(enactor.OwnPerms.PiShockShareCode, dto.OpCode, dto.Intensity, dto.Duration);
            if (dto.OpCode is 0)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
        }
        else if (OwnGlobals.Perms is { } g && !g.GlobalShockShareCode.IsNullOrEmpty())
        {
            _logger.LogDebug("Executing Shock Instruction to Global ShareCode", LoggerType.Callbacks);
            _mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
            _shockies.ExecuteOperation(g.GlobalShockShareCode, dto.OpCode, dto.Intensity, dto.Duration);
            if (dto.OpCode is 0)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
        }
        else
        {
            _logger.LogDebug("Someone Attempted to execute an instruction to you, but you don't have any share codes enabled!");
        }
    }
    #endregion Permission Changes



    #region Permission Validation
    private bool CanChangeHypnosis(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.HypnosisCustomEffect), string.IsNullOrEmpty(newValue)) switch
        {
            // Only allow applying (setting it to a non-empty string) if the enactor is the client.
            //
            // This means it was set by one of our restrictions.
            //
            // This will instead be manually validated and set by a seperate method all together as it has
            // attached metadata that must be stored.
            //
            // Because everyone else already has the value updated as is, we need to reset it to the original value
            // if this fails.
            (true, false) => newValue == MainHub.UID,
            (false, true) => _hcHandler.CanRemoveHypnosis(enactor, p),
            _ => false
        };

    private bool CanChangeLockedFollow(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.LockedFollowing), string.IsNullOrEmpty(newValue)) switch
        {
            (true, false) => _hcHandler.CanEnableLockedFollow(enactor, p),
            (false, true) => _hcHandler.CanDisableLockedFollow(enactor, p),
            _ => false
        };

    private bool CanChangeLockedEmoteState(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.LockedEmoteState), string.IsNullOrEmpty(newValue)) switch
        {
            (true, false) => _hcHandler.CanEnableLockedEmote(enactor, p),
            (false, true) => _hcHandler.CanDisableLockedEmote(enactor, p),
            _ => false
        };

    private bool CanChangeIndoorConfinement(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.IndoorConfinement), string.IsNullOrEmpty(newValue)) switch
        {
            // Only allow applying (setting it to a non-empty string) if the enactor is the client.
            //
            // This means it was set by one of our restrictions.
            //
            // This will instead be manually validated and set by a seperate method all together as it has
            // attached metadata that must be stored.
            //
            // Because everyone else already has the value updated as is, we need to reset it to the original value
            // if this fails.
            (true, false) => newValue == MainHub.UID,
            (false, true) => _hcHandler.CanDisableIndoorConfinement(enactor, p),
            _ => false
        };

    private bool CanChangeImprisonment(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.Imprisonment), string.IsNullOrEmpty(newValue)) switch
        {
            // Only allow applying (setting it to a non-empty string) if the enactor is the client.
            //
            // This means it was set by one of our restrictions.
            //
            // This will instead be manually validated and set by a seperate method all together as it has
            // attached metadata that must be stored.
            //
            // Because everyone else already has the value updated as is, we need to reset it to the original value
            // if this fails.
            (true, false) => newValue == MainHub.UID,
            (false, true) => _hcHandler.CanDisableImprisonment(enactor, p),
            _ => false
        };

    private bool CanChangeChatBoxesHidden(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.ChatBoxesHidden), string.IsNullOrEmpty(newValue)) switch
        {
            (true, false) => _hcHandler.CanEnableHiddenChatBoxes(enactor, p),
            (false, true) => _hcHandler.CanDisableHiddenChatBoxes(enactor, p),
            _ => false
        };

    private bool CanChangeChatInputHidden(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.ChatInputHidden), string.IsNullOrEmpty(newValue)) switch
        {
            (true, false) => _hcHandler.CanHideChatInputVis(enactor, p),
            (false, true) => _hcHandler.CanRestoreChatInputVis(enactor, p),
            _ => false
        };

    private bool CanChangeChatInputBlocked(IReadOnlyGlobalPerms p, UserData enactor, string newValue)
        => (string.IsNullOrEmpty(p.ChatInputBlocked), string.IsNullOrEmpty(newValue)) switch
        {
            (true, false) => _hcHandler.CanBlockChatInput(enactor, p),
            (false, true) => _hcHandler.CanUnblockChatInput(enactor, p),
            _ => false
        };
    #endregion Permission Validation
}
