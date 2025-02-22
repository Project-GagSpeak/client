using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Extensions;

namespace GagSpeak.PlayerData.Data;

/// <summary> Handles the Kinkster Requests, Global Permissions, and Global Data Updates. </summary>
public sealed class GlobalData : DisposableMediatorSubscriberBase
{
    public GlobalData(ILogger<GlobalData> logger, GagspeakMediator mediator) : base(logger, mediator)
    {
        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) => { GlobalPerms = null; });
    }

    public UserGlobalPermissions? GlobalPerms { get; set; } = null;
    public HashSet<UserPairRequestDto> CurrentRequests { get; set; } = new();
    public HashSet<UserPairRequestDto> OutgoingRequests => CurrentRequests.Where(x => x.User.UID == MainHub.UID).ToHashSet();
    public HashSet<UserPairRequestDto> IncomingRequests => CurrentRequests.Where(x => x.RecipientUser.UID == MainHub.UID).ToHashSet();

    public void AddPairRequest(UserPairRequestDto dto)
    {
        CurrentRequests.Add(dto);
        Logger.LogInformation("New pair request added!", LoggerType.PairManagement);
        Mediator.Publish(new RefreshUiMessage());
    }

    public void RemovePairRequest(UserPairRequestDto dto)
    {
        var res = CurrentRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.RecipientUser.UID == dto.RecipientUser.UID);
        Logger.LogInformation("Removed " + res + " pair requests.", LoggerType.PairManagement);
        Mediator.Publish(new RefreshUiMessage());
    }

    /// <summary> The function that applies a global permission change from an enactor. </summary>
    /// <param name="changeDto">the dto of the change.</param>
    /// <param name="enactorPair">Defines which pair made the change. If null, it came from the client themselves.</param>
    public void ApplyGlobalPermChange(UserGlobalPermChangeDto changeDto, Pair enactorPair)
    {
        if (GlobalPerms is null) 
            return;

        // establish the key-value pair from the Dto so we know what is changing.
        var propertyName = changeDto.ChangedPermission.Key;
        var newValue = changeDto.ChangedPermission.Value;
        var propertyInfo = typeof(UserGlobalPermissions).GetProperty(propertyName);

        if (propertyInfo is null) 
            return;

        // Get the Hardcore Change Type before updating the property.
        var hardcoreChangeType = GlobalPerms!.GetHardcoreChange(propertyName, newValue);

        // If the property exists and is found, update its value
        if (newValue is UInt64 && propertyInfo.PropertyType == typeof(TimeSpan))
        {
            var ticks = (long)(ulong)newValue;
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

        // Handle how we log and output the events / achievement sends.
        var newState = string.IsNullOrEmpty((string)newValue) ? NewState.Disabled : NewState.Enabled;
        var permName = hardcoreChangeType is InteractionType.None ? propertyName : hardcoreChangeType.ToString();
        HandleHardcorePermUpdate(hardcoreChangeType, enactorPair, changeDto.Enactor.UID, permName, newState);
    }

    private void HandleHardcorePermUpdate(InteractionType hardcoreChangeType, Pair enactor, string enactorUid, string permissionName, NewState newState)
    {
        Logger.LogInformation(hardcoreChangeType.ToString() + " has changed, and is now " + newState, LoggerType.PairManagement);
        // if the changeType is none, that means it was not a hardcore change, so we can log the generic event message and return.
        if (hardcoreChangeType is InteractionType.None)
        {
            Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactorUid, InteractionType.ForcedPermChange, "Permission (" + permissionName + ") Changed")));
            return;
        }

        // If the enactor is anything else, it is a hardcore permission change, and we should execute its operation.
        Logger.LogDebug("Change was a hardcore action. Publishing HardcoreActionMessage.", LoggerType.PairManagement);
        Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactorUid, hardcoreChangeType, "Hardcore Action (" + hardcoreChangeType + ") is now " + newState)));
        Mediator.Publish(new HardcoreActionMessage(hardcoreChangeType, newState));

        UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, hardcoreChangeType, newState, enactorUid, MainHub.UID);
    }
}
