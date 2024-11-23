using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using System.Reflection.Metadata;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace GagSpeak.PlayerData.Services;

// A class intended to help execute any actions that should be performed by the client upon initial connection.
public sealed class OnConnectedService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly PlayerCharacterData _playerData;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;
    private readonly GagManager _gagManager;
    private readonly IpcManager _ipcManager;
    private readonly WardrobeHandler _wardrobeHandler;
    private readonly HardcoreHandler _hardcoreHandler;
    private readonly AppearanceManager _appearanceHandler;

    public OnConnectedService(ILogger<OnConnectedService> logger,
        GagspeakMediator mediator, PlayerCharacterData playerData,
        ClientConfigurationManager clientConfigs, PairManager pairManager,
        GagManager gagManager, IpcManager ipcManager, WardrobeHandler wardrobeHandler,
        HardcoreHandler blindfold, AppearanceManager appearanceHandler) : base(logger, mediator)
    {
        _playerData = playerData;
        _clientConfigs = clientConfigs;
        _pairManager = pairManager;
        _gagManager = gagManager;
        _ipcManager = ipcManager;
        _wardrobeHandler = wardrobeHandler;
        _hardcoreHandler = blindfold;
        _appearanceHandler = appearanceHandler;

        // Potentially move this until after all checks for validation are made to prevent invalid startups.
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ => OnConnected());

        Mediator.Subscribe<OnlinePairsLoadedMessage>(this, _ => CheckHardcore());

        Mediator.Subscribe<CustomizeReady>(this, _ => _playerData.CustomizeProfiles = _ipcManager.CustomizePlus.GetProfileList());

        Mediator.Subscribe<CustomizeDispose>(this, _ => _playerData.CustomizeProfiles = new List<CustomizeProfile>());
    }

    private async void OnConnected()
    {
        if(MainHub.ConnectionDto is null)
        {
            Logger.LogError("Connection DTO is null. Cannot proceed with OnConnected Service. (This shouldnt even be possible)", LoggerType.ClientPlayerData);
            return;
        }

        Logger.LogInformation("------- Connected Message Received. Processing State Synchronization -------");

        Logger.LogDebug("Setting Global Perms & Appearance from Server.", LoggerType.ClientPlayerData);
        _playerData.GlobalPerms = MainHub.ConnectionDto.UserGlobalPermissions;
        _playerData.AppearanceData = MainHub.ConnectionDto.CharaAppearanceData;
        Logger.LogDebug("Data Set", LoggerType.ClientPlayerData);

        // See why this is necessary later.
        //await _playerData.GetGlobalPiShockPerms();

        Logger.LogDebug("Setting up Update Tasks from GagSpeak Modules.", LoggerType.ClientPlayerData);
        // update the active gags
        _gagManager.UpdateGagLockComboSelections();
        _gagManager.UpdateGagGarblerLogic();

        Logger.LogInformation("Syncing Data with Connection DTO", LoggerType.ClientPlayerData);
        var serverData = MainHub.ConnectionDto.CharacterActiveStateData;
        var activeSet = _clientConfigs.GetActiveSet();
        // We should sync the active Restraint Set with the server's Active Set Name.
        if (!serverData.ActiveSetId.IsEmptyGuid())
        {
            int setIdx = _clientConfigs.GetSetIdxByGuid(serverData.ActiveSetId);
            var restraintId = _clientConfigs.WardrobeConfig.WardrobeStorage.RestraintSets[setIdx].RestraintId;

            // check to see if the active set stored on the server is locked with a timer padlock.
            if (GenericHelpers.TimerPadlocks.Contains(serverData.Padlock) && serverData.Timer < DateTimeOffset.UtcNow)
            {
                Logger.LogInformation("The stored active Restraint Set is locked with a Timer Padlock. Unlocking Set.", LoggerType.Restraints);
                // while this doesn't do anything client side, it will bump an update to the server, updating the active state data.
                await _appearanceHandler.UnlockRestraintSet(restraintId, serverData.Assigner, false, true); // Fire the achievement if we need to unlock it.

                // if we have it set to remove sets that are unlocked automatically, do so.
                if (_clientConfigs.GagspeakConfig.DisableSetUponUnlock)
                {
                    Logger.LogInformation("Disabling Unlocked Set due to Config Setting.", LoggerType.Restraints);
                    // we should also update the active state data to disable the set.
                    await _appearanceHandler.DisableRestraintSet(restraintId, serverData.ActiveSetEnabler, false, false);
                }
            }
            // if it is not a set that had its time expired, then we should re-enable it, and re-lock it if it was locked.
            else
            {
                if(restraintId != _clientConfigs.GetActiveSet()?.RestraintId)
                {
                    Logger.LogInformation("Re-Enabling the stored active Restraint Set", LoggerType.Restraints);
                    await _appearanceHandler.EnableRestraintSet(restraintId, serverData.ActiveSetEnabler, false, false);
                }
                // relock it if it had a timer.
                if (serverData.Padlock != Padlocks.None.ToName())
                {
                    Logger.LogInformation("Re-Locking the stored active Restraint Set", LoggerType.Restraints);
                    await _appearanceHandler.LockRestraintSet(restraintId, serverData.Padlock.ToPadlock(), serverData.Password, serverData.Timer, serverData.Assigner, false, true);
                }
            }
            // update the data. (Note, setting these to false may trigger a loophole by skipping over the monitored achievements,
            Mediator.Publish(new PlayerCharWardrobeChanged(DataUpdateKind.FullDataUpdate));
        }
        else
        {
            // if the active set is empty, but we have a set that is on or locked, we should unlock and remove it.
            activeSet = _clientConfigs.GetActiveSet();
            if (activeSet is not null)
            {
                Logger.LogInformation("The Stored Restraint Set was Empty, yet you have one equipped. Unlocking and unequipping.", LoggerType.Restraints);
                if (activeSet.LockType.ToPadlock() is not Padlocks.None)
                    await _appearanceHandler.UnlockRestraintSet(activeSet.RestraintId, activeSet.LockedBy, false, true);
                // disable it.
                await _appearanceHandler.DisableRestraintSet(activeSet.RestraintId, activeSet.LockedBy, false, true);
                // update the data. (Note, setting these to false may trigger a loophole by skipping over the monitored achievements,
                Mediator.Publish(new PlayerCharWardrobeChanged(DataUpdateKind.FullDataUpdate));
            }
        }
        _gagManager.UpdateGagLockComboSelections();
        _gagManager.UpdateRestraintLockSelections(false);
        // send our updated data.
        var activeGags = _playerData.AppearanceData.GagSlots.Where(x => x.GagType.ToGagType() is not GagType.None).Select(x => x.GagType).ToList();
        
        // now that everything is updated, get the active set.
        var activeSetFinal = _clientConfigs.GetActiveSet();
        if(activeSetFinal is not null)
        {
            // Enable the Hardcore Properties by invoking the ipc call.
            if (activeSetFinal.HasPropertiesForUser(activeSetFinal.EnabledBy))
            {
                Logger.LogDebug("Set Contains HardcoreProperties for " + activeSetFinal.EnabledBy, LoggerType.Restraints);
                if (activeSetFinal.PropertiesEnabledForUser(activeSetFinal.EnabledBy))
                {
                    Logger.LogDebug("Hardcore properties are enabled for this set!");
                    IpcFastUpdates.InvokeHardcoreTraits(NewState.Enabled, activeSetFinal);
                }
            }
        }
        var activeSetId = activeSetFinal?.RestraintId ?? Guid.Empty;
        Mediator.Publish(new PlayerLatestActiveItems(MainHub.PlayerUserData, activeGags, activeSetId));

        // Run a refresh on appearance data.
        await _appearanceHandler.RecalcAndReload(true);
    }

    private async void CheckHardcore()
    {
        // Stop this if it is true.
        if(_hardcoreHandler.IsForcedToFollow)
            _hardcoreHandler.UpdateForcedFollow(NewState.Disabled);

        // Re-Enable forced Sit if it is disabled.
        if(_hardcoreHandler.IsForcedToEmote)
            if(!string.IsNullOrEmpty(_playerData.GlobalPerms?.ForcedEmoteState))
                _hardcoreHandler.UpdateForcedEmoteState(NewState.Enabled);

        // Re-Enable Forcd Stay if it was enabled.
        if (_hardcoreHandler.IsForcedToStay)
            _hardcoreHandler.UpdateForcedStayState(NewState.Enabled);

        // Re-Enable Blindfold if it was enabled.
        if (_hardcoreHandler.IsBlindfolded)
            await _hardcoreHandler.HandleBlindfoldLogic(NewState.Enabled);

        // Re-Enable the chat related hardcore things.
        if(_hardcoreHandler.IsHidingChat)
            _hardcoreHandler.UpdateHideChatboxState(NewState.Enabled);
        if (_hardcoreHandler.IsHidingChatInput)
            _hardcoreHandler.UpdateHideChatInputState(NewState.Enabled);
        if(_hardcoreHandler.IsBlockingChatInput)
            _hardcoreHandler.UpdateChatInputBlocking(NewState.Enabled);
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
