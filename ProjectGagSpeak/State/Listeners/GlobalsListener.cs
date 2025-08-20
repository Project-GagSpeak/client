using Dalamud.Bindings.ImGui;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Processes all changes to ClientData Globals and HardcoreState <para />
///     Helps process Handler updates in addition to this.
/// </summary>
public sealed class ClientDataListener
{
    private readonly ILogger<ClientDataListener> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ClientData _data;
    private readonly KinksterManager _kinksters;
    private readonly PlayerControlHandler _handler;
    private readonly NameplateService _nameplates;
    public ClientDataListener(ILogger<ClientDataListener> logger, GagspeakMediator mediator,
        ClientData data, KinksterManager kinksters, PlayerControlHandler handler,
        NameplateService nameplate)
    {
        _logger = logger;
        _mediator = mediator;
        _data = data;
        _handler = handler;
        _kinksters = kinksters;
        _nameplates = nameplate;
    }

    public void ChangeAllClientGlobals(UserData enactor, GlobalPerms globals, HardcoreState hardcore)
    {
        var prevGlobals = ClientData.GlobalPermClone();
        var prevHardcore = ClientData.HardcoreClone();
        _data.SetGlobals(globals, hardcore);
        HandleGlobalPermChanges(enactor, prevGlobals, globals);
        HandleHardcoreStateFullChange(enactor, prevHardcore, hardcore);
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

    // Bulk Change Handler.
    private void HandleHardcoreStateFullChange(UserData enactor, IReadOnlyHardcoreState? prev, IReadOnlyHardcoreState? current)
    {
        HandleHardcoreStateChange(enactor, HcAttribute.Follow, prev.IsEnabled(HcAttribute.Follow), current.IsEnabled(HcAttribute.Follow));
        HandleHardcoreStateChange(enactor, HcAttribute.EmoteState, prev.IsEnabled(HcAttribute.EmoteState), current.IsEnabled(HcAttribute.EmoteState));
        HandleHardcoreStateChange(enactor, HcAttribute.Confinement, prev.IsEnabled(HcAttribute.Confinement), current.IsEnabled(HcAttribute.Confinement));
        HandleHardcoreStateChange(enactor, HcAttribute.Imprisonment, prev.IsEnabled(HcAttribute.Imprisonment), current.IsEnabled(HcAttribute.Imprisonment));
        HandleHardcoreStateChange(enactor, HcAttribute.HiddenChatBox, prev.IsEnabled(HcAttribute.HiddenChatBox), current.IsEnabled(HcAttribute.HiddenChatBox));
        HandleHardcoreStateChange(enactor, HcAttribute.HiddenChatInput, prev.IsEnabled(HcAttribute.HiddenChatInput), current.IsEnabled(HcAttribute.HiddenChatInput));
        HandleHardcoreStateChange(enactor, HcAttribute.BlockedChatInput, prev.IsEnabled(HcAttribute.BlockedChatInput), current.IsEnabled(HcAttribute.BlockedChatInput));
        // only handle hypno if being disabled.
        if (prev.IsEnabled(HcAttribute.HypnoticEffect) && !current.IsEnabled(HcAttribute.HypnoticEffect))
            HandleHardcoreStateChange(enactor, HcAttribute.HypnoticEffect, true, false);
    }

    // All Single Change handles for GlobalPermissions so far are just bools, which makes this easier for us.
    public void HandleGlobalPermChanges(UserData enactor, IReadOnlyGlobalPerms? prev, IReadOnlyGlobalPerms? current)
    {
        bool garblerChanged = prev?.ChatGarblerActive != current?.ChatGarblerActive;
        bool nameplateChanged = prev?.GaggedNameplate != current?.GaggedNameplate;
        if (!garblerChanged && !nameplateChanged)
            return;
        // a change occurred, so update the nameplates.
        _nameplates.RefreshClientGagState();
    }
    // Single Change Handler.
    private void HandleHardcoreStateChange(UserData enactor, HcAttribute changed, bool prevState, bool newState)
    {
        // if from false to false nothing happened so just return.
        if (!prevState && !newState)
            return;

        switch (changed)
        {
            case HcAttribute.Follow when (!prevState && newState):
                _handler.EnableLockedFollow(enactor);
                break;
            
            case HcAttribute.Follow when (prevState && !newState):
                _handler.DisableLockedFollow(enactor, true);
                break;
                
            case HcAttribute.EmoteState when (!prevState && newState):
                _handler.EnableLockedEmote(enactor);
                break;
            // Remember we can go from true to true here, and switch the emote being performed.
            case HcAttribute.EmoteState when (prevState && newState):
                _handler.UpdateLockedEmote(enactor);
                break;

            case HcAttribute.EmoteState when (prevState && !newState):
                _handler.DisableLockedEmote(enactor, true);
                break;

            case HcAttribute.Confinement when (!prevState && newState):
                _handler.EnableConfinement(enactor, AddressBookEntry.FromHardcoreState(ClientData.Hardcore!));
                break;

            case HcAttribute.Confinement when (prevState && !newState):
                _handler.DisableConfinement(enactor, true);
                break;

            case HcAttribute.Imprisonment when (!prevState && newState):
                _handler.EnableImprisonment(enactor, ClientData.GetImprisonmentPos(), ClientData.Hardcore!.ImprisonedRadius);
                break;
            // Remember we can go from true to true here, and switch the imprisonment position.
            case HcAttribute.Imprisonment when (prevState && newState):
                _handler.UpdateImprisonment(enactor, ClientData.GetImprisonmentPos(), ClientData.Hardcore!.ImprisonedRadius);
                break;
            
            case HcAttribute.Imprisonment when (prevState && !newState):
                _handler.DisableImprisonment(enactor, true);
                break;

            case HcAttribute.HiddenChatBox when (!prevState && newState):
                _handler.EnableHiddenChatBoxes(enactor);
                break;

            case HcAttribute.HiddenChatBox when (prevState && !newState):
                _handler.DisableHiddenChatBoxes(enactor, true);
                break;

            case HcAttribute.HiddenChatInput when (!prevState && newState):
                _handler.HideChatInputVisibility(enactor);
                break;

            case HcAttribute.HiddenChatInput when (prevState && !newState):
                _handler.RestoreChatInputVisibility(enactor, true);
                break;

            case HcAttribute.BlockedChatInput when (!prevState && newState):
                _handler.BlockChatInput(enactor);
                break;

            case HcAttribute.BlockedChatInput when (prevState && !newState):
                _handler.UnblockChatInput(enactor, true);
                break;

            case HcAttribute.HypnoticEffect when (prevState && !newState):
                _handler.RemoveHypnoEffect(enactor, true);
                break;
            // Throw an exception in ALL other cases.
            default:
                throw new NotImplementedException($"HcStateChange for [{changed}] from ({prevState}) to ({newState}) is not implemented!");
        }
    }
}
