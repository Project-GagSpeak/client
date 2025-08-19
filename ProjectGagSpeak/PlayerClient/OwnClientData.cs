using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;

namespace GagSpeak.PlayerClient;

/// <summary> 
///     Holds all personal information about the client's Kinkster information. <para />
///     This includes GlobalPerms, HardcoreState, and Pair Requests. <para />
///     GlobalPerms and HardcoreState can be accessed statically, as this is singleton, 
///     and makes readonly access less of a hassle considering how frequently they are accessed.
/// </summary>
public sealed class ClientData
{
    private readonly ILogger<ClientData> _logger;
    private readonly GagspeakMediator _mediator;
    public ClientData(ILogger<ClientData> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;

        Svc.ClientState.Logout += OnLogout;
    }

    private static GlobalPerms? _clientGlobals;
    private static HardcoreState? _clientHardcore;
    private HashSet<KinksterPairRequest> _pairingRequests = new();
    private HashSet<CollarOwnershipRequest> _collarRequests = new();

    public static bool IsNull = false;
    internal static IReadOnlyGlobalPerms? Globals => _clientGlobals;
    internal static IReadOnlyHardcoreState? Hardcore => _clientHardcore;
    public bool HasKinksterRequests => _pairingRequests.Count > 0;
    public bool HasCollarRequests => _collarRequests.Count > 0;
    public IEnumerable<KinksterPairRequest> ReqPairOutgoing => _pairingRequests.Where(x => x.User.UID == MainHub.UID);
    public IEnumerable<KinksterPairRequest> ReqPairIncoming => _pairingRequests.Where(x => x.Target.UID == MainHub.UID);
    public IEnumerable<CollarOwnershipRequest> ReqCollarOutgoing => _collarRequests.Where(x => x.User.UID == MainHub.UID);
    public IEnumerable<CollarOwnershipRequest> ReqCollarIncoming => _collarRequests.Where(x => x.Target.UID == MainHub.UID);

    public void Dispose()
    {
        Svc.ClientState.Logout -= OnLogout;
    }

    private void OnLogout(int type, int code)
    {
        _logger.LogInformation("Clearing Global Permissions on Logout.");
        _clientGlobals = null;
        _clientHardcore = null;
        IsNull = true;
        _pairingRequests.Clear();
        _collarRequests.Clear();
    }

    // might need some better way to update this i think.
    public void InitClientData(ConnectionResponse connectionDto)
    {
        var prevGlobals = _clientGlobals;
        var prevHardcore = _clientHardcore;
        _clientGlobals = connectionDto.GlobalPerms;
        _clientHardcore = connectionDto.HardcoreState;
        IsNull = false;
    }

    public void InitRequests(List<KinksterPairRequest> kinksterRequests, List<CollarOwnershipRequest> collarRequests)
    {
        _pairingRequests = kinksterRequests.ToHashSet();
        _collarRequests = collarRequests.ToHashSet();
        _logger.LogInformation("Initialized Kinkster and Collar Requests.");
        _mediator.Publish(new RefreshUiKinkstersMessage());
    }

    public void AddPairRequest(KinksterPairRequest dto)
    {
        _pairingRequests.Add(dto);
        _logger.LogInformation("New pair request added!", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiKinkstersMessage());
    }

    public void RemovePairRequest(KinksterPairRequest dto)
    {
        var res = _pairingRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.Target.UID == dto.Target.UID);
        _logger.LogInformation($"Removed [{res}] pair request.", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiKinkstersMessage());
    }

    public void AddCollarRequest(CollarOwnershipRequest dto)
    {
        _collarRequests.Add(dto);
        _logger.LogInformation("New collar request added!", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiKinkstersMessage());
    }

    public void RemoveCollarRequest(CollarOwnershipRequest dto)
    {
        var res = _collarRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.Target.UID == dto.Target.UID);
        _logger.LogInformation($"Removed [{res}] collar request.", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiKinkstersMessage());
    }

    public void ChangeGlobalsBulk(GlobalPerms newGlobals)
    {
        var prevGlobals = _clientGlobals;
        _clientGlobals = newGlobals;
        _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.BulkUpdate, "Global Permissions Updated in Bulk")));
    }

    public void ChangeGlobalPerm(UserData enactor, string permName, object newValue, Kinkster? pair = null)
    {
        if (pair is null && !string.Equals(enactor.UID, MainHub.UID))
            throw new InvalidOperationException($"Change not from self, and [{enactor.AliasOrUID}] is not a Kinkster Pair. Invalid change for [{permName}]!");

        // Attempt to set the property. if this fails, which it never should if validated previously, throw an exception.
        if (!PropertyChanger.TrySetProperty(_clientGlobals, permName, newValue, out var _))
            throw new InvalidOperationException($"Failed to set property [{permName}] to [{newValue}] on Global Permissions.");
        // Then perform the log.
        _mediator.Publish(new EventMessage(new(pair?.GetNickAliasOrUid() ?? "Self-Update", enactor.UID, InteractionType.ForcedPermChange, $"[{permName}] changed to [{newValue}]")));
    }

    public void EnableHardcoreState(UserData enactor, HcAttribute attribute, HardcoreState newData, Kinkster? pair = null)
    {
        if (_clientHardcore is not { } hcState)
            throw new InvalidOperationException("Hardcore State is not initialized. Cannot change Hardcore State.");

        if (pair is null && !string.Equals(enactor.UID, MainHub.UID))
            throw new InvalidOperationException($"Change not from self, and [{MainHub.UID}] is not a Kinkster Pair. Invalid change for Hardcore State!");

        // Warn that this is a self-invoked auto-timeout change if pair is null and it was from ourselves.
        if (pair is null && enactor.UID.Equals(MainHub.UID))
            _logger.LogInformation($"HardcoreStateChange for attribute [{attribute}] was self-invoked due to natural timer expiration!");

        // Update the values based on the attribute.
        switch (attribute)
        {
            case HcAttribute.Follow:
                hcState.LockedFollowing = newData.LockedFollowing;
                break;

            case HcAttribute.EmoteState:
                hcState.LockedEmoteState = newData.LockedEmoteState;
                hcState.EmoteExpireTime = newData.EmoteExpireTime;
                hcState.EmoteId = newData.EmoteId;
                hcState.EmoteCyclePose = newData.EmoteCyclePose;
                break;

            case HcAttribute.Confinement:
                hcState.IndoorConfinement = newData.IndoorConfinement;
                hcState.ConfinementTimer = newData.ConfinementTimer;
                hcState.ConfinedWorld = newData.ConfinedWorld;
                hcState.ConfinedCity = newData.ConfinedCity;
                hcState.ConfinedWard = newData.ConfinedWard;
                hcState.ConfinedPlaceId = newData.ConfinedPlaceId;
                hcState.ConfinedInApartment = newData.ConfinedInApartment;
                hcState.ConfinedInSubdivision = newData.ConfinedInSubdivision;
                break;

            case HcAttribute.Imprisonment:
                hcState.Imprisonment = newData.Imprisonment;
                hcState.ImprisonmentTimer = newData.ImprisonmentTimer;
                hcState.ImprisonedTerritory = newData.ImprisonedTerritory;
                hcState.ImprisonedPos = newData.ImprisonedPos;
                hcState.ImprisonedRadius = newData.ImprisonedRadius;
                break;

            case HcAttribute.HiddenChatBox:
                hcState.ChatBoxesHidden = newData.ChatBoxesHidden;
                hcState.ChatBoxesHiddenTimer = newData.ChatBoxesHiddenTimer;
                break;

            case HcAttribute.HiddenChatInput:
                hcState.ChatInputHidden = newData.ChatInputHidden;
                hcState.ChatInputHiddenTimer = newData.ChatInputHiddenTimer;
                break;

            case HcAttribute.BlockedChatInput:
                hcState.ChatInputBlocked = newData.ChatInputBlocked;
                hcState.ChatInputBlockedTimer = newData.ChatInputBlockedTimer;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Invalid Hardcore State attribute to change.");
        }
        
        // log the change.
        _mediator.Publish(new EventMessage(new(pair?.GetNickAliasOrUid() ?? "Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Hardcore Attribute [{attribute}] was Enabled!")));
    }

    /// <summary>
    ///     Assumes server has already validated this operation. If called locally, implies a natural falloff has occurred. <para />
    ///     Not connected to achievement flagging or Hardcore Handlers, so be sure those are handled appropriately before calling this.
    /// </summary>
    public void DisableHardcoreState(UserData enactor, HcAttribute attribute, Kinkster? pair = null)
    {
        if (_clientHardcore is not { } hcState)
            throw new InvalidOperationException("Hardcore State is not initialized. Cannot change Hardcore State.");

        if (pair is null && !string.Equals(enactor.UID, MainHub.UID))
            throw new InvalidOperationException($"Change not from self, and [{MainHub.UID}] is not a Kinkster Pair. Invalid change for Hardcore State!");

        // Warn that this is a self-invoked auto-timeout change if pair is null and it was from ourselves.
        if (pair is null && enactor.UID.Equals(MainHub.UID))
            _logger.LogInformation($"HardcoreStateChange for attribute [{attribute}] was self-invoked due to natural timer expiration!");

        // No harm in turning something off twice, since nothing would happen regardless, so we can be ok with that.
        switch (attribute)
        {
            case HcAttribute.Follow:
                hcState.LockedFollowing = string.Empty;
                break;

            case HcAttribute.EmoteState:
                hcState.LockedEmoteState = string.Empty;
                hcState.EmoteExpireTime = DateTimeOffset.MinValue;
                hcState.EmoteId = 0;
                hcState.EmoteCyclePose = 0;
                break;

            case HcAttribute.Confinement:
                hcState.IndoorConfinement = string.Empty;
                hcState.ConfinementTimer = DateTimeOffset.MinValue;
                hcState.ConfinedWorld = 0;
                hcState.ConfinedCity = 0;
                hcState.ConfinedWard = 0;
                hcState.ConfinedPlaceId = 0;
                hcState.ConfinedInApartment = false;
                hcState.ConfinedInSubdivision = false;
                break;

            case HcAttribute.Imprisonment:
                hcState.Imprisonment = string.Empty;
                hcState.ImprisonmentTimer = DateTimeOffset.MinValue;
                hcState.ImprisonedTerritory = 0;
                hcState.ImprisonedPos = Vector3.Zero;
                hcState.ImprisonedRadius = 0;
                break;

            case HcAttribute.HiddenChatBox:
                hcState.ChatBoxesHidden = string.Empty;
                hcState.ChatBoxesHiddenTimer = DateTimeOffset.MinValue;
                break;

            case HcAttribute.HiddenChatInput:
                hcState.ChatInputHidden = string.Empty;
                hcState.ChatInputHiddenTimer = DateTimeOffset.MinValue;
                break;

            case HcAttribute.BlockedChatInput:
                hcState.ChatInputBlocked = string.Empty;
                hcState.ChatInputBlockedTimer = DateTimeOffset.MinValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Invalid Hardcore State attribute to Disable.");
        }

        // log the change.
        _mediator.Publish(new EventMessage(new(pair?.GetNickAliasOrUid() ?? "Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Hardcore Attribute [{attribute}] was Disabled!")));
    }
}
