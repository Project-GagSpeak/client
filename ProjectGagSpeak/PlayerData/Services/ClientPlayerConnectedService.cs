using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.PlayerData.Services;

// A class intended to help execute any actions that should be performed by the client upon initial connection.
public sealed class OnConnectedService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly ClientData _playerData;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;
    private readonly GagGarbler _garbler;
    private readonly IpcManager _ipcManager;
    private readonly WardrobeHandler _wardrobeHandler;
    private readonly HardcoreHandler _hardcoreHandler;
    private readonly AppearanceManager _appearanceHandler;
    private readonly ClientMonitorService _clientService;

    public OnConnectedService(ILogger<OnConnectedService> logger, GagspeakMediator mediator, 
        ClientData playerData, ClientConfigurationManager clientConfigs, PairManager pairManager,
        GagGarbler garbler, IpcManager ipcManager, WardrobeHandler wardrobeHandler, HardcoreHandler blindfold, 
        AppearanceManager appearanceHandler, ClientMonitorService clientService) : base(logger, mediator)
    {
        _playerData = playerData;
        _clientConfigs = clientConfigs;
        _pairManager = pairManager;
        _garbler = garbler;
        _ipcManager = ipcManager;
        _wardrobeHandler = wardrobeHandler;
        _hardcoreHandler = blindfold;
        _appearanceHandler = appearanceHandler;
        _clientService = clientService;

        // Potentially move this until after all checks for validation are made to prevent invalid startups.
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ => OnConnected());

        Mediator.Subscribe<OnlinePairsLoadedMessage>(this, _ => CheckHardcore());

        Mediator.Subscribe<CustomizeReady>(this, _ => _playerData.CustomizeProfiles = _ipcManager.CustomizePlus.GetProfileList());

        Mediator.Subscribe<CustomizeDispose>(this, _ => _playerData.CustomizeProfiles = new List<CustomizeProfile>());
        _clientService = clientService;
    }

    private async void OnConnected()
    {
        if (MainHub.ConnectionDto is null)
        {
            Logger.LogError("Connection DTO is null. Cannot proceed with OnConnected Service. (This shouldnt even be possible)", LoggerType.ClientPlayerData);
            return;
        }

        Logger.LogInformation("------- Connected Message Received. Processing State Synchronization -------");

        // Update Global Permissions and Appearance Data.
        Logger.LogDebug("Setting Global Perms & Appearance from Server.", LoggerType.ClientPlayerData);
        _playerData.GlobalPerms = MainHub.ConnectionDto.UserGlobalPermissions;
        _playerData.AppearanceData = MainHub.ConnectionDto.GagData;
        Logger.LogDebug("Data Set", LoggerType.ClientPlayerData); 
        _garbler.UpdateGarblerLogic();


        Logger.LogInformation("Syncing Data with Connection DTO", LoggerType.ClientPlayerData);
        // Obtain Server-Side Active State Data.
        var serverData = MainHub.ConnectionDto.ActiveRestraintData;
        var serverExpectsActiveSet = serverData.ActiveSetId != Guid.Empty;
        // Upon connection all mods should be resynchronized with the acctive restraints/gags.
        // This is primarily to ensure that the mods can be accurately synchronized between multiple characters.
        _ipcManager.Penumbra.ClearAllTemporaryMods();
        // Handle it accordingly.
        if (serverExpectsActiveSet)
        {
            await HandleActiveServerSet(serverData);
        }
        else
        {
            await ClearActiveSet();
        }

        // Apply Hardcore Traits to the active set.
        ApplyTraitsIfAny();

        // Send the latest active items off for the achievement manager to run a update check on for duration achievements.
        PublishLatestActiveItems();

        // recalc if we have any alterations.
        if (_playerData.IsPlayerGagged || _playerData.IsPlayerBlindfolded || _clientConfigs.HasGlamourerAlterations)
            await _appearanceHandler.RecalcAndReload(true);
    }

    private void PublishLatestActiveItems()
    {
        var gagInfo = _playerData.AppearanceData ?? new CharaAppearanceData();
        var activeSetId = _clientConfigs.GetActiveSet()?.RestraintId ?? Guid.Empty;
        Mediator.Publish(new PlayerLatestActiveItems(MainHub.PlayerUserData, gagInfo, activeSetId));
    }

    private async Task HandleActiveServerSet(CharaActiveSetData serverData)
    {
        // grab the index in our list of sets matching the expected set ID.
        int setIdx = _clientConfigs.GetSetIdxByGuid(serverData.ActiveSetId);
        if (setIdx < 0)
        {
            // if it doesnt exist, clear the active set if any and publish that change and return.
            Logger.LogError("The Active Set ID from the server does not match any stored sets. Resyncing Data.", LoggerType.Restraints);
            await ClearActiveSet();
            return;
        }

        var hasExpiredTimer = serverData.Padlock.ToPadlock().IsTimerLock() && serverData.Timer < DateTimeOffset.UtcNow;
        if(_clientConfigs.TryGetActiveSet(out var activeClientSet))
            await HandleClientSet(serverData, hasExpiredTimer, activeClientSet);
    }

    private async Task HandleClientSet(CharaActiveSetData serverData, bool hasExpiredTimer, RestraintSet activeClientSet)
    {
        if (activeClientSet.RestraintId == serverData.ActiveSetId && hasExpiredTimer)
        {
            await UnlockAndDisableSet(serverData);
            // publish a full data wardrobe update after this.
            Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.FullDataUpdate, string.Empty));
        }
        else if (activeClientSet.RestraintId != serverData.ActiveSetId)
        {
            await EnableAndRelockSet(serverData);
            // publish a full data wardrobe update after this.
            Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.FullDataUpdate, string.Empty));
        }
        // In every other case, we have an active set that matches the server set, so we want to be sure to re-enable the temporary mods.
        else {
            await _appearanceHandler.PenumbraModsToggle(NewState.Enabled, activeClientSet.AssociatedMods);
        }
    }

    private async Task UnlockAndDisableSet(CharaActiveSetData serverData)
    {
        Logger.LogInformation("The stored active Restraint Set is locked with a Timer Padlock. Unlocking Set.", LoggerType.Restraints);
        _appearanceHandler.UnlockRestraintSet(serverData.ActiveSetId, serverData.Password, serverData.Assigner, true, true);

        if (_clientConfigs.GagspeakConfig.DisableSetUponUnlock)
        {
            Logger.LogInformation("Disabling Unlocked Set due to Config Setting.", LoggerType.Restraints);
            await _appearanceHandler.DisableRestraintSet(serverData.ActiveSetId, serverData.ActiveSetEnabler, true, true);
        }
    }

    private async Task EnableAndRelockSet(CharaActiveSetData serverData)
    {
        Logger.LogInformation("Re-Enabling the stored active Restraint Set", LoggerType.Restraints);
        await _appearanceHandler.EnableRestraintSet(serverData.ActiveSetId, serverData.ActiveSetEnabler, true, true);

        if (serverData.Padlock != Padlocks.None.ToName())
        {
            Logger.LogInformation("Re-Locking the stored active Restraint Set", LoggerType.Restraints);
            _appearanceHandler.LockRestraintSet(serverData.ActiveSetId, serverData.Padlock.ToPadlock(),
                serverData.Password, serverData.Timer.GetEndTimeOffsetString(), serverData.Assigner, true, true);
        }
    }

    private async Task ClearActiveSet()
    {
        // Obtain Client-Side active Restraint Set for this UID.
        // If any are active, unlock and remove them. (time expired / force removal)
        if (_clientConfigs.TryGetActiveSet(out var activeSet))
        {
            Logger.LogWarning("The Stored Restraint Set was Empty, yet you have one equipped. Unlocking and unequipping.");
            // Unlock Active Set
            if (activeSet.IsLocked())
                _appearanceHandler.UnlockRestraintSet(activeSet.RestraintId, activeSet.Password, activeSet.Assigner, true, true);

            // Disable Active Set.
            await _appearanceHandler.DisableRestraintSet(activeSet.RestraintId, activeSet.Assigner, true, true);
            // publish the changes.
            Mediator.Publish(new PlayerCharWardrobeChanged(WardrobeUpdateType.FullDataUpdate, string.Empty));
        }
    }

    private void ApplyTraitsIfAny()
    {
        if (_clientConfigs.TryGetActiveSet(out var set))
        {
            // Enable the Hardcore Properties by invoking the ipc call.
            if (set.HasPropertiesForUser(set.EnabledBy))
            {
                Logger.LogDebug("Set Contains HardcoreProperties for " + set.EnabledBy, LoggerType.Restraints);
                if (set.PropertiesEnabledForUser(set.EnabledBy))
                {
                    Logger.LogDebug("Hardcore properties are enabled for this set!");
                    IpcFastUpdates.InvokeHardcoreTraits(NewState.Enabled, set);
                }
            }
        }
    }

    private async void CheckHardcore()
    {
        // make 30s timeout token. and wait for player to load.
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;
        while (!await _clientService.IsPresentAsync().ConfigureAwait(false) && !token.IsCancellationRequested)
        {
            Logger.LogDebug("Player not loaded in yet, waiting", LoggerType.ApiCore);
            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        }

        if (_hardcoreHandler.IsForcedToFollow) _hardcoreHandler.UpdateForcedFollow(NewState.Disabled);

        // Re-Enable forced Sit if it is disabled.
        if (_hardcoreHandler.IsForcedToEmote && !string.IsNullOrEmpty(_playerData.GlobalPerms?.ForcedEmoteState)) _hardcoreHandler.UpdateForcedEmoteState(NewState.Enabled);

        // Re-Enable Forcd Stay if it was enabled.
        if (_hardcoreHandler.IsForcedToStay) _hardcoreHandler.UpdateForcedStayState(NewState.Enabled);

        // Re-Enable Blindfold if it was enabled.
        if (_hardcoreHandler.IsBlindfolded) await _hardcoreHandler.HandleBlindfoldLogic(NewState.Enabled);

        // Re-Enable the chat related hardcore things.
        if (_hardcoreHandler.IsHidingChat) _hardcoreHandler.UpdateHideChatboxState(NewState.Enabled);
        if (_hardcoreHandler.IsHidingChatInput) _hardcoreHandler.UpdateHideChatInputState(NewState.Enabled);
        if (_hardcoreHandler.IsBlockingChatInput) _hardcoreHandler.UpdateChatInputBlocking(NewState.Enabled);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting OnConnectedService");

        // grab the latest CustomizePlus Profile List.
        _playerData.CustomizeProfiles = _ipcManager.CustomizePlus.GetProfileList();

        Logger.LogInformation("Started OnConnectedService");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping IpcProvider Service");

        return Task.CompletedTask;
    }


}
