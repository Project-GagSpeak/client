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
using System.Diagnostics.CodeAnalysis;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.Localization;
using GagspeakAPI.Dto.UserPair;

namespace GagSpeak.PlayerData.Data;

/// <summary>
/// Managed by ClientData, while storing the data so it can be accessed without conflicts with ClientData.
/// </summary>
public class ClientData : DisposableMediatorSubscriberBase
{
    public ClientData(ILogger<ClientData> logger, GagspeakMediator mediator) : base(logger, mediator)
    {
        Mediator.Subscribe<CharacterIpcDataCreatedMessage>(this, (msg) =>
        {
            LastIpcData = msg.CharaIPCData;
            Logger.LogDebug("New Moodles Data Contains " + msg.CharaIPCData.MoodlesStatuses.Count + " Statuses" +
                " and " + msg.CharaIPCData.MoodlesPresets.Count + " Presets.", LoggerType.IpcMoodles);
        });

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
    public HashSet<UserPairRequestDto> CurrentRequests { get; set; } = new();
    public CharaIPCData? LastIpcData { get; set; } = null;
    public List<CustomizeProfile> CustomizeProfiles { get; set; } = new();

    public HashSet<UserPairRequestDto> OutgoingRequests => CurrentRequests.Where(x => x.User.UID == MainHub.UID).ToHashSet();
    public HashSet<UserPairRequestDto> IncomingRequests => CurrentRequests.Where(x => x.RecipientUser.UID == MainHub.UID).ToHashSet();


    public bool CoreDataNull => GlobalPerms is null || AppearanceData is null;
    public bool IpcDataNull => LastIpcData is null;
    private bool CustomizeNull => CustomizeProfiles is null || CustomizeProfiles.Count == 0;
    public bool IsPlayerGagged => AppearanceData?.GagSlots.Any(x => x.GagType != GagType.None.GagName()) ?? false;
    public int TotalGagsEquipped => AppearanceData?.GagSlots.Count(x => x.GagType != GagType.None.GagName()) ?? 0;

    public bool AnyGagActive => AppearanceData?.GagSlots.Any(x => x.GagType != GagType.None.GagName()) ?? false;
    public bool AnyGagLocked => AppearanceData?.GagSlots.Any(x => x.Padlock != Padlocks.None.ToName()) ?? false;
    public List<string> CurrentGagNames => Enumerable.Range(0, 3)
        .Select(i => AppearanceData?.GagSlots[i].GagType ?? GagType.None.GagName())
        .ToList();

    public void AddPairRequest(UserPairRequestDto dto)
    {
        // remove the request where the dto's userUID matches the userUID and the recipientUID matches the recipientUID
        CurrentRequests.Add(dto);
        // log the number of elements removed.
        Logger.LogInformation("New pair request added!", LoggerType.PairManagement);
        // publish a refresh ui message.
        Mediator.Publish(new RefreshUiMessage());
    }

    public void RemovePairRequest(UserPairRequestDto dto)
    {
        // remove the request where the dto's userUID matches the userUID and the recipientUID matches the recipientUID
        var res = CurrentRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.RecipientUser.UID == dto.RecipientUser.UID);
        // log the number of elements removed.
        Logger.LogInformation("Removed " + res + " pair requests.", LoggerType.PairManagement);
        // publish a refresh ui message.
        Mediator.Publish(new RefreshUiMessage());
    }


    public CharaAppearanceData CompileAppearanceToAPI() => AppearanceData?.DeepCloneData() ?? new CharaAppearanceData();

    public void ApplyGlobalPermChange(UserGlobalPermChangeDto changeDto, PairManager pairs)
    {
        if (CoreDataNull) return;

        // establish the key-value pair from the Dto so we know what is changing.
        string propertyName = changeDto.ChangedPermission.Key;
        object newValue = changeDto.ChangedPermission.Value;
        PropertyInfo? propertyInfo = typeof(UserGlobalPermissions).GetProperty(propertyName);

        if (propertyInfo is null) return;

        // See if someone else did this.
        var changedPair = pairs.DirectPairs.FirstOrDefault(x => x.UserData.UID == changeDto.Enactor.UID);

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
        Logger.LogInformation(hardcoreChangeType.ToString() + " has changed, and is now " + newValue, LoggerType.PairManagement);
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



}
