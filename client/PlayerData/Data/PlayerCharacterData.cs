using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Dto.Permissions;
using System.Reflection;
using GagspeakAPI.Extensions;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Services.Events;

namespace GagSpeak.PlayerData.Data;

/// <summary>
/// Handles the player character data.
/// <para>
/// Applies callback updates to clientConfig data
/// Compiles client config data into API format for server transfer.
/// </para>
/// </summary>
public class PlayerCharacterData : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;

    public PlayerCharacterData(ILogger<PlayerCharacterData> logger, GagspeakMediator mediator, 
        ClientConfigurationManager clientConfigs, PairManager pairManager) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _pairManager = pairManager;

        Mediator.Subscribe<PlayerCharAppearanceChanged>(this, (msg) => PushAppearanceDataToAPI(msg));
        Mediator.Subscribe<PlayerCharWardrobeChanged>(this, (msg) => PushWardrobeDataToAPI(msg));
        Mediator.Subscribe<PlayerCharAliasChanged>(this, (msg) => PushAliasListDataToAPI(msg));
        Mediator.Subscribe<PlayerCharToyboxChanged>(this, (msg) => PushToyboxDataToAPI(msg));
        Mediator.Subscribe<PlayerCharStorageUpdated>(this, _ => PushLightStorageToAPI());

        Mediator.Subscribe<CharacterIpcDataCreatedMessage>(this, (msg) => LastIpcData = msg.CharaIPCData);

        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) =>
        {
            GlobalPerms = null;
            AppearanceData = null;
            LastIpcData = null;
            CustomizeProfiles = new();
        });
    }

    public UserGlobalPermissions? GlobalPerms { get; set; } = null;
    public CharaAppearanceData? AppearanceData { get; set; } = null;
    public CharaIPCData? LastIpcData { get; set; } = null;
    public List<CustomizeProfile> CustomizeProfiles { get; set; } = new();

    public bool CoreDataNull => GlobalPerms is null || AppearanceData is null;
    public bool IpcDataNull => LastIpcData is null;
    private bool CustomizeNull => CustomizeProfiles is null || CustomizeProfiles.Count == 0;
    public bool IsPlayerGagged => AppearanceData?.GagSlots.Any(x => x.GagType != GagType.None.GagName()) ?? false;
    public int TotalGagsEquipped => AppearanceData?.GagSlots.Count(x => x.GagType != GagType.None.GagName()) ?? 0;

    /// <summary> Updates the changed permission from server callback to global permissions </summary>
    public void ApplyGlobalPermChange(UserGlobalPermChangeDto changeDto)
    {
        if (CoreDataNull) 
            return;

        // establish the key-value pair from the Dto so we know what is changing.
        string propertyName = changeDto.ChangedPermission.Key;
        object newValue = changeDto.ChangedPermission.Value;
        PropertyInfo? propertyInfo = typeof(UserGlobalPermissions).GetProperty(propertyName);

        if (propertyInfo is null)
            return;

        // See if someone else did this.
        var changedPair = _pairManager.DirectPairs.FirstOrDefault(x => x.UserData.UID == changeDto.Enactor.UID);

        // Get the Hardcore Change Type before updating the property (if it is not valid it wont return anything but none anyways)
        HardcoreAction hardcoreChangeType = GlobalPerms!.GetHardcoreChange(propertyName, newValue);

        // If the property exists and is found, update its value
        if (newValue is UInt64 && propertyInfo.PropertyType == typeof(TimeSpan))
        {
            long ticks = (long)(ulong)newValue;
            propertyInfo.SetValue(GlobalPerms, TimeSpan.FromTicks(ticks));
        }
        // char recognition. (these are converted to byte for Dto's instead of char)
        else if (changeDto.ChangedPermission.Value.GetType() == typeof(byte) && propertyInfo.PropertyType == typeof(char))
        {
            propertyInfo.SetValue(GlobalPerms, Convert.ToChar(newValue));
        }
        else if (propertyInfo != null && propertyInfo.CanWrite)
        {
            // Convert the value to the appropriate type before setting
            var value = Convert.ChangeType(newValue, propertyInfo.PropertyType);
            propertyInfo.SetValue(GlobalPerms, value);
            Logger.LogDebug($"Updated global permission '{propertyName}' to '{newValue}'", LoggerType.ClientPlayerData);
        }
        else
        {
            Logger.LogError($"Property '{propertyName}' not found or cannot be updated.");
            return;
        }

        // If not a hardcore change but another perm change, publish that.
        if (changedPair is not null && hardcoreChangeType is HardcoreAction.None)
            Mediator.Publish(new EventMessage(new(changedPair.GetNickAliasOrUid(), changedPair.UserData.UID, InteractionType.ForcedPermChange, "Permission (" + changeDto + ") Changed")));

        // Handle hardcore changes here.
        if (hardcoreChangeType is HardcoreAction.None)
        {
            Logger.LogInformation("No Hardcore Change Detected. Returning.", LoggerType.PairManagement);
            return;
        }

        var newState = string.IsNullOrEmpty((string)newValue) ? NewState.Disabled : NewState.Enabled;
        Logger.LogInformation(hardcoreChangeType.ToString() + " has changed, and is now "+ newValue, LoggerType.PairManagement);
        Mediator.Publish(new HardcoreActionMessage(hardcoreChangeType, newState));
        // If the changed Pair is not null, we should map the type and log the interaction event.
        if (changedPair is not null)
        {
            var interactionType = hardcoreChangeType switch
            {
                HardcoreAction.ForcedFollow => InteractionType.ForcedFollow,
                HardcoreAction.ForcedEmoteState => InteractionType.ForcedEmoteState,
                HardcoreAction.ForcedStay => InteractionType.ForcedStay,
                HardcoreAction.ForcedBlindfold => InteractionType.ForcedBlindfold,
                HardcoreAction.ChatboxHiding => InteractionType.ForcedChatVisibility,
                HardcoreAction.ChatInputHiding => InteractionType.ForcedChatInputVisibility,
                HardcoreAction.ChatInputBlocking => InteractionType.ForcedChatInputBlock,
                _ => InteractionType.None
            };
            Mediator.Publish(new EventMessage(new(changedPair.GetNickAliasOrUid(), changedPair.UserData.UID, interactionType, "Hardcore Action (" + hardcoreChangeType + ") is now " + newState)));
        }
        UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreForcedPairAction, hardcoreChangeType, newState, changeDto.Enactor.UID, MainHub.UID);
    }


    private void OldPiShockDataGrab()
    {
/*        var userPairs = _pairManager.GetOnlineUserPairs();

        bool hasApiOn = !string.IsNullOrEmpty(_clientConfigs.GagspeakConfig.PiShockApiKey) && !string.IsNullOrEmpty(_clientConfigs.GagspeakConfig.PiShockUsername);

        List<Task<(string UID, PiShockPermissions)>> getPermissionsTasks = new();

        foreach (var user in userPairs)
        {
            aliasData[user.UserData.UID] = CompileAliasToAPI(user.UserData.UID);

            // Only fetch permissions if API credentials are available
            if (hasApiOn) getPermissionsTasks.Add(GetPairPiShockPerms(user).ContinueWith(task => (user.UserData.UID, task.Result)));
        }

        if (hasApiOn)
        {
            // Wait for all tasks to complete if API credentials are available
            var permissionsResults = await Task.WhenAll(getPermissionsTasks);

            // Populate pairShockData with the results
            foreach (var result in permissionsResults)
            {
                pairShockData[result.UID] = result.Item2 ?? new PiShockPermissions(); // Default or null handling
            }
        }
        else
        {
            // If no API credentials, populate with default permissions
            foreach (var user in userPairs)
            {
                pairShockData[user.UserData.UID] = new PiShockPermissions(); // Default or fallback
            }
        }

        PiShockPermissions globalShockPerms = hasApiOn ? await GetGlobalPiShockPerms() : new PiShockPermissions();
        */
    }


    // helper method to decompile a received composite data message
    public CharaCompositeData CompileCompositeDataToSend()
    {
        // make use of the various compiling methods to construct our composite data.
        CharaAppearanceData appearanceData = CompileAppearanceToAPI();
        CharaWardrobeData wardrobeData = CompileWardrobeToAPI();

        Dictionary<string, CharaAliasData> aliasData = _clientConfigs.GetCompiledAliasData();
        CharaToyboxData toyboxData = _clientConfigs.CompileToyboxToAPI();

        CharaStorageData lightStorageData = _clientConfigs.CompileLightStorageToAPI();

        return new CharaCompositeData
        {
            AppearanceData = appearanceData,
            WardrobeData = wardrobeData,
            AliasData = aliasData,
            ToyboxData = toyboxData,
            LightStorageData = lightStorageData
        };
    }

    public CharaAppearanceData CompileAppearanceToAPI()
    {
        if (AppearanceData == null)
        {
            Logger.LogError("Appearance data is null. This should not be possible.");
            return new CharaAppearanceData();
        }

        CharaAppearanceData dataToPush = new CharaAppearanceData
        {
            GagSlots = new GagSlot[3]
            {
                new GagSlot
                {
                    GagType = AppearanceData.GagSlots[0].GagType,
                    Padlock = AppearanceData.GagSlots[0].Padlock,
                    Password = AppearanceData.GagSlots[0].Password,
                    Timer = AppearanceData.GagSlots[0].Timer,
                    Assigner = AppearanceData.GagSlots[0].Assigner
                },
                new GagSlot
                {
                    GagType = AppearanceData.GagSlots[1].GagType,
                    Padlock = AppearanceData.GagSlots[1].Padlock,
                    Password = AppearanceData.GagSlots[1].Password,
                    Timer = AppearanceData.GagSlots[1].Timer,
                    Assigner = AppearanceData.GagSlots[1].Assigner
                },
                new GagSlot
                {
                    GagType = AppearanceData.GagSlots[2].GagType,
                    Padlock = AppearanceData.GagSlots[2].Padlock,
                    Password = AppearanceData.GagSlots[2].Password,
                    Timer = AppearanceData.GagSlots[2].Timer,
                    Assigner = AppearanceData.GagSlots[2].Assigner
                }
            }
        };

        return dataToPush;
    }

    public CharaWardrobeData CompileWardrobeToAPI()
    {
        // attempt to locate the active restraint set
        var activeSet = _clientConfigs.GetActiveSet();
        return new CharaWardrobeData
        {
            ActiveSetId = activeSet?.RestraintId ?? Guid.Empty,
            ActiveSetEnabledBy = activeSet?.EnabledBy ?? string.Empty,
            Padlock = activeSet?.LockType ?? Padlocks.None.ToName(),
            Password = activeSet?.LockPassword ?? "",
            Timer = activeSet?.LockedUntil ?? DateTimeOffset.MinValue,
            Assigner = activeSet?.LockedBy ?? "",
            ActiveCursedItems = _clientConfigs.ActiveCursedItems
        };
    }

    public CharaAliasData CompileAliasToAPI(string UserUID)
    {
        var AliasStorage = _clientConfigs.FetchAliasStorageForPair(UserUID);
        CharaAliasData dataToPush = new CharaAliasData
        {
            // don't include names here to secure privacy.
            AliasList = AliasStorage.AliasList
        };

        return dataToPush;
    }

    public CharaToyboxData CompileToyboxToAPI()
    {
        return _clientConfigs.CompileToyboxToAPI();
    }

    public void PushAppearanceDataToAPI(PlayerCharAppearanceChanged msg)
    {
        Mediator.Publish(new CharacterAppearanceDataCreatedMessage(msg.newGagData, msg.UpdateKind));
    }

    public void PushWardrobeDataToAPI(PlayerCharWardrobeChanged msg)
    {
        CharaWardrobeData dataToPush = CompileWardrobeToAPI();
        Mediator.Publish(new CharacterWardrobeDataCreatedMessage(dataToPush, msg.UpdateKind));
    }

    public void PushAliasListDataToAPI(PlayerCharAliasChanged msg)
    {
        UserData? userPair = _pairManager.GetUserDataFromUID(msg.UpdatedPairUID);
        if (userPair == null)
        {
            Logger.LogError("User pair not found for Alias update.");
            return;
        }

        var dataToPush = CompileAliasToAPI(userPair.UID);
        Mediator.Publish(new CharacterAliasDataCreatedMessage(dataToPush, userPair, DataUpdateKind.PuppeteerAliasListUpdated));
    }

    public void PushToyboxDataToAPI(PlayerCharToyboxChanged msg)
    {
        var dataToPush = _clientConfigs.CompileToyboxToAPI();
        Mediator.Publish(new CharacterToyboxDataCreatedMessage(dataToPush, msg.UpdateKind));
    }

    public void PushLightStorageToAPI()
    {
        var dataToPush = _clientConfigs.CompileLightStorageToAPI();
        Mediator.Publish(new CharacterStorageDataCreatedMessage(dataToPush));
    }
}
