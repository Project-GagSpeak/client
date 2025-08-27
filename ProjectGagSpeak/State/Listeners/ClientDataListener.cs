using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Processes all changes to ClientData Globals and HardcoreState <para />
///     Helps process Handler updates in addition to this.
/// </summary>
public sealed class ClientDataListener : IDisposable
{
    private readonly ILogger<ClientDataListener> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ClientData _data;
    private readonly KinksterManager _kinksters;
    private readonly PlayerCtrlHandler _handler;
    private readonly NameplateService _nameplates;
    public ClientDataListener(ILogger<ClientDataListener> logger, GagspeakMediator mediator,
        ClientData data, KinksterManager kinksters, PlayerCtrlHandler handler,
        NameplateService nameplate)
    {
        _logger = logger;
        _mediator = mediator;
        _data = data;
        _handler = handler;
        _kinksters = kinksters;
        _nameplates = nameplate;
        Svc.ClientState.Logout += OnLogout;
    }

    public void Dispose()
    {
        Svc.ClientState.Logout -= OnLogout;
        OnLogout(69, 69);
    }
    private void OnLogout(int type, int code)
    {
        try
        {
            _logger.LogInformation("Disabling all ClientGlobals handlers on logout.");
            _handler.DisableLockedFollow(new("Logout/Disposal"), false);
            _handler.DisableLockedEmote(new("Logout/Disposal"), false);
            _handler.DisableConfinement(new("Logout/Disposal"), false);
            _handler.DisableImprisonment(new("Logout/Disposal"), false);
            _handler.DisableHiddenChatBoxes(new("Logout/Disposal"), false);
            _handler.RestoreChatInputVisibility(new("Logout/Disposal"), false);
            _handler.UnblockChatInput(new("Logout/Disposal"), false);
            _handler.RemoveHypnoEffect(new("Logout/Disposal"), false, true);
        }
        catch (Exception e)
        {
            _logger.LogError("Error while disabling ClientGlobals handlers on logout." + e);
        }
    }

    // Only ever self-invoked, all handlers should process their own strings.
    public void ChangeAllClientGlobals(UserData enactor, GlobalPerms globals, HardcoreState hardcore)
    {
        var prevGlobals = ClientData.GlobalPermClone();
        var prevHardcore = ClientData.HardcoreClone();
        _data.SetGlobals(globals, hardcore);
        HandleGlobalPermChanges(enactor, prevGlobals, globals);
        // Resync Hardcore State Changes. (Skip UNKNOWN and HYPNO-EFFECT)
        foreach (var attr in Enum.GetValues<HcAttribute>().Skip(1).SkipLast(1))
            HandleHardcoreStateChange(new(hardcore.Enactor(attr)), attr, prevHardcore.IsEnabled(attr), hardcore.IsEnabled(attr));
        // Inform that the hcStateCache changed so we can update our detours.
        _mediator.Publish(new HcStateCacheChanged());
    }

    // Could only ever be performed by the client.
    public void ChangeAllGlobalPerms(GlobalPerms newGlobals)
    {
        var prevGlobals = ClientData.GlobalPermClone();
        _data.ChangeGlobalsBulkInternal(newGlobals);
        HandleGlobalPermChanges(MainHub.PlayerUserData, prevGlobals, ClientData.Globals);

        _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.BulkUpdate, "BULK UPDATE -> Global Permissions")));
    }

    public void ChangeGlobalPerm(UserData enactor, string permName, object newValue)
    {
        var prevGlobals = ClientData.GlobalPermClone();
        // Find the nickname of the person enacting this change.
        var kinkster = _kinksters.TryGetKinkster(enactor, out var k) ? k : null;
        _data.ChangeGlobalPermInternal(enactor, permName, newValue, kinkster);
        // Process global permission updates.
        HandleGlobalPermChanges(enactor, prevGlobals, ClientData.Globals);

        _mediator.Publish(new EventMessage(new(kinkster?.GetNickAliasOrUid() ?? "Self-Update", enactor.UID, InteractionType.ForcedPermChange, $"[{permName}] changed to [{newValue}]")));
    }

    /// <summary>
    ///     Either enables or disables a hardcore attribute within the hardcore state, making use of the newData object. <para />
    ///     If enabling a hardcore state, <paramref name="newData"/> <b> MUST BE NON-NULL.</b>
    /// </summary>
    public void ChangeHardcoreState(UserData enactor, HcAttribute attribute, HardcoreState newData)
    {
        // if the attribute was hypno with a new state of active, fail. This MUST be handled separately.
        if (attribute is HcAttribute.HypnoticEffect && newData.HypnoticEffect.Length > 0)
            throw new InvalidOperationException("Cannot Enable Hypno effects via this method, must use HypnotizeKinkster!");

        var prevState = ClientData.Hardcore.IsEnabled(attribute);
        // Find the kinkster for this change.
        if (_kinksters.GetKinksterOrDefault(enactor) is not { } kinkster)
            throw new InvalidOperationException($"Kinkster [{enactor.AliasOrUID}] not found.");
        // Make the change.
        _data.SetHardcoreState(enactor, attribute, newData, kinkster);
        HandleHardcoreStateChange(enactor, attribute, prevState, ClientData.Hardcore.IsEnabled(attribute));

        _mediator.Publish(new EventMessage(new(kinkster.GetNickAliasOrUid(), enactor.UID, InteractionType.HardcoreStateChange, $"[{attribute}] changed to [{newData.IsEnabled(attribute)}]")));
        _mediator.Publish(new HcStateCacheChanged());
    }

    public void InitRequests(ActiveRequests requests)
    {
        _data.InitRequests(requests.KinksterRequests, requests.CollarRequests);
        _logger.LogInformation($"Init: [{requests.KinksterRequests.Count} PairRequests][{requests.CollarRequests.Count} CollarRequests]", LoggerType.ApiCore);
        _mediator.Publish(new RefreshUiRequestsMessage());
    }

    public void AddPairRequest(KinksterPairRequest dto)
    {
        _data.AddPairRequest(dto);
        _logger.LogInformation("New pair request added!", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiRequestsMessage());
    }

    public void RemovePairRequest(KinksterPairRequest dto)
    {
        var res = _data.RemovePairRequest(dto);
        _logger.LogInformation($"Removed [{res}] pair request.", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiRequestsMessage());
    }

    public void AddCollarRequest(CollarOwnershipRequest dto)
    {
        _data.AddCollarRequest(dto);
        _logger.LogInformation("New collar request added!", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiRequestsMessage());
    }

    public void RemoveCollarRequest(CollarOwnershipRequest dto)
    {
        var res = _data.RemoveCollarRequest(dto);
        _logger.LogInformation($"Removed [{res}] collar request.", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiRequestsMessage());
    }


    // All Single Change handles for GlobalPermissions so far are just bools, which makes this easier for us.
    public void HandleGlobalPermChanges(UserData enactor, IReadOnlyGlobalPerms? prev, IReadOnlyGlobalPerms? current)
    {
        var garblerChanged = prev?.ChatGarblerActive != current?.ChatGarblerActive;
        var nameplateChanged = prev?.GaggedNameplate != current?.GaggedNameplate;
        if (!garblerChanged && !nameplateChanged)
            return;
        // a change occurred, so update the nameplates.
        _nameplates.RefreshClientGagState();
    }
    // Single Change Handler.
    private void HandleHardcoreStateChange(UserData enactor, HcAttribute changed, bool prevState, bool newState)
    {
        // If both states are false, nothing happened.
        if (!prevState && !newState)
            return;

        // If states are different, handle them.
        switch (changed)
        {
            case HcAttribute.Follow:
                if (!prevState && newState) _handler.EnableLockedFollow(enactor);
                else if (prevState && !newState) _handler.DisableLockedFollow(enactor, true);
                break;

            case HcAttribute.EmoteState:
                if (!prevState && newState) _handler.EnableLockedEmote(enactor);
                else if (prevState && !newState) _handler.DisableLockedEmote(enactor, true);
                else if (prevState && newState) _handler.UpdateLockedEmote(enactor);
                break;

            case HcAttribute.Confinement:
                if (!prevState && newState) _handler.EnableConfinement(enactor, AddressBookEntry.FromHardcoreState(ClientData.Hardcore!));
                else if (prevState && !newState) _handler.DisableConfinement(enactor, true);
                break;

            case HcAttribute.Imprisonment:
                if (!prevState && newState) _handler.EnableImprisonment(enactor);
                else if (prevState && !newState) _handler.DisableImprisonment(enactor, true);
                else if (prevState && newState) _handler.UpdateImprisonment(enactor);
                break;

            case HcAttribute.HiddenChatBox:
                if (!prevState && newState) _handler.EnableHiddenChatBoxes(enactor);
                else if (prevState && !newState) _handler.DisableHiddenChatBoxes(enactor, true);
                break;

            case HcAttribute.HiddenChatInput:
                if (!prevState && newState) _handler.HideChatInputVisibility(enactor);
                else if (prevState && !newState) _handler.RestoreChatInputVisibility(enactor, true);
                break;

            case HcAttribute.BlockedChatInput:
                if (!prevState && newState) _handler.BlockChatInput(enactor);
                else if (prevState && !newState) _handler.UnblockChatInput(enactor, true);
                break;

            case HcAttribute.HypnoticEffect:
                if (prevState && !newState) _handler.RemoveHypnoEffect(enactor, true);
                // enable and reapply intentionally ignored, handled in bulk
                break;

            // Throw an exception in ALL other cases.
            default:
                throw new NotImplementedException($"HcStateChange for [{changed}] from ({prevState}) to ({newState}) is not implemented!");
        }
    }
}
