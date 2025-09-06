using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using OtterGui;

namespace GagSpeak.PlayerClient;

/// <summary> 
///     Holds all personal information about the client's Kinkster information. <para />
///     This includes GlobalPerms, HardcoreState, and Pair Requests. <para />
///     GlobalPerms and HardcoreState can be accessed statically, as this is singleton, 
///     and makes readonly access less of a hassle considering how frequently they are accessed.
/// </summary>
public sealed class ClientData : IDisposable
{
    private readonly ILogger<ClientData> _logger;
    public ClientData(ILogger<ClientData> logger)
    {
        _logger = logger;
        Svc.ClientState.Logout += OnLogout;
    }

    public void Dispose()
    {
        OnLogout(0, 0);
        Svc.ClientState.Logout -= OnLogout;
    }

    // Clear data contents on logout to ensure consistency between profiles.
    private void OnLogout(int type, int code)
    {
        _logger.LogInformation("Clearing Global Permissions on Logout.");
        SetGlobals(null, null);
        _pairingRequests.Clear();
        _collarRequests.Clear();
    }

    private static GlobalPerms? _clientGlobals;
    private static HardcoreState? _clientHardcore;
    private HashSet<KinksterPairRequest> _pairingRequests = new();
    private HashSet<CollarOwnershipRequest> _collarRequests = new();

    /// <summary>
    ///     When true, <see cref="GlobalPerms"/> or <see cref="HardcoreState"/> are not initialized.
    /// </summary>
    public static bool IsNull { get; private set; } = false;
    internal static IReadOnlyGlobalPerms? Globals => _clientGlobals;
    internal static IReadOnlyHardcoreState? Hardcore => _clientHardcore;
    public IEnumerable<KinksterPairRequest> ReqPairOutgoing => _pairingRequests.Where(x => x.User.UID == MainHub.UID);
    public IEnumerable<KinksterPairRequest> ReqPairIncoming => _pairingRequests.Where(x => x.Target.UID == MainHub.UID);
    // make these independant maybe. Idk, would be more convient for the UI.
    public List<CollarOwnershipRequest> ReqCollarOutgoing => _collarRequests.Where(x => x.User.UID == MainHub.UID).ToList();
    public List<CollarOwnershipRequest> ReqCollarIncoming => _collarRequests.Where(x => x.Target.UID == MainHub.UID).ToList();

    public static Vector3 GetImprisonmentPos()
        => _clientHardcore?.ImprisonedPos ?? Vector3.Zero;

    public static GlobalPerms? GlobalPermClone()
        => _clientGlobals != null ? _clientGlobals with { } : null;
    public static HardcoreState? HardcoreClone()
        => _clientHardcore != null ? _clientHardcore with { } : null;

    public static GlobalPerms GlobalsWithNewShockPermissions(PiShockPermissions newPerms)
        => (_clientGlobals ?? new GlobalPerms()) with
        {
            AllowShocks = newPerms.AllowShocks,
            AllowVibrations = newPerms.AllowVibrations,
            AllowBeeps = newPerms.AllowBeeps,
            MaxDuration = newPerms.MaxDuration,
            MaxIntensity = newPerms.MaxIntensity
        };

    public static GlobalPerms GlobalsWithSafewordApplied()
        => (_clientGlobals ?? new GlobalPerms()) with
        {
            ChatGarblerActive = false,          // Disable Chat Garbler.
            ChatGarblerLocked = false,          // Don't keep garbler locked.
            GaggedNameplate = false,            // Disable Gagged Nameplate.
            WardrobeEnabled = false,            // prevent any generic wardrobe calls.
            GagVisuals = false,                 // prevent gag visuals.
            RestrictionVisuals = false,         // prevent restriction visuals.
            RestraintSetVisuals = false,        // prevent restraint visuals.
            PuppeteerEnabled = false,           // prevent puppeteer from all sources.
            ToyboxEnabled = false,              // prevent toybox from all sources.
            SpatialAudio = false,               // prevent sounds.
            GlobalShockShareCode = string.Empty,// Prevent shockies.
            AllowShocks = false,
            AllowVibrations = false,
            AllowBeeps = false,
            MaxIntensity = -1,
            MaxDuration = -1
        };

    public void SetGlobals(GlobalPerms? globals, HardcoreState? hardcore)
    {
        _clientGlobals = globals;
        _clientHardcore = hardcore;
        IsNull = globals is null || hardcore is null;
    }

    public void InitRequests(List<KinksterPairRequest> kinksterRequests, List<CollarOwnershipRequest> collarRequests)
    {
        _pairingRequests = kinksterRequests.ToHashSet();
        _collarRequests = collarRequests.ToHashSet();
    }

    public void AddPairRequest(KinksterPairRequest dto)
        => _pairingRequests.Add(dto);

    public int RemovePairRequest(KinksterPairRequest dto)
        => _pairingRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.Target.UID == dto.Target.UID);

    public void AddCollarRequest(CollarOwnershipRequest dto)
        => _collarRequests.Add(dto);
        
    public int RemoveCollarRequest(CollarOwnershipRequest dto)
        => _collarRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.Target.UID == dto.Target.UID);

    public void ChangeGlobalsBulkInternal(GlobalPerms newGlobals)
        => _clientGlobals = newGlobals;

    public void ChangeGlobalPermInternal(UserData enactor, string permName, object newValue, Kinkster? pair = null)
    {
        if (pair is null && !string.Equals(enactor.UID, MainHub.UID))
            throw new InvalidOperationException($"Change not from self, and [{enactor.AliasOrUID}] is not a Kinkster Pair. Invalid change for [{permName}]!");
        // Attempt to set the property.
        if (!PropertyChanger.TrySetProperty(_clientGlobals, permName, newValue, out var _))
            throw new InvalidOperationException($"Failed to set property [{permName}] to [{newValue}] on Global Permissions.");
    }

    /// <summary>
    ///     Can either enable or disable a hardcore state via <paramref name="attribute"/>, and the values 
    ///     within <paramref name="newData"/>. <para />
    ///     This method cannot, and should be enabled by the client, and must only be enacted by a kinkster pair.
    ///     <b> THIS WILL NOT HANDLE ANY PLAYER CONTROL LOGIC AND MUST BE HANDLED SEPERATELY. </b>
    /// </summary>
    public void SetHardcoreState(UserData enactor, HcAttribute attribute, HardcoreState newData, Kinkster pair)
    {
        if (_clientHardcore is not { } hcState)
            throw new InvalidOperationException("Hardcore State is not initialized. Cannot change Hardcore State.");

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
                // this is set to the client position if anything but 0 (enabling/disabling)
                hcState.ImprisonedPos = newData.Imprisonment.Length > 0
                    ? (newData.ImprisonedPos == Vector3.Zero ? PlayerData.PositionInstanced : newData.ImprisonedPos)
                    : Vector3.Zero;
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

            case HcAttribute.HypnoticEffect:
                hcState.HypnoticEffect = newData.HypnoticEffect;
                hcState.HypnoticEffectTimer = newData.HypnoticEffectTimer;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Invalid Hardcore State attribute to change.");
        }
    }

    /// <summary>
    ///     Assumes server has already validated this operation. If called locally, implies a natural falloff has occurred. <para />
    ///     <b> THIS WILL NOT HANDLE ANY PLAYER CONTROL LOGIC OR ACHIEVEMENTS AND MUST BE HANDLED SEPERATELY. </b>
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
                hcState.ImprisonedRadius = 1;
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

            case HcAttribute.HypnoticEffect:
                hcState.HypnoticEffect = string.Empty;
                hcState.HypnoticEffectTimer = DateTimeOffset.MinValue;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Invalid Hardcore State attribute to Disable.");
        }
    }

    public void DrawHardcoreState()
        => PermissionHelper.DrawHardcoreState(_clientHardcore);
}
