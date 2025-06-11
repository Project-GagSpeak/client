using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentNumericInput.Delegates;

namespace GagSpeak.PlayerClient;

/// <summary>
///     Sealed container holding the GlobalPermissions, that can be retrieved as a static object.
///     It's values can be updated with 
///     <see cref="ChangeGlobalPermission(SingleChangeGlobal)"/> 
///     or <see cref="ChangeGlobalPermission(SingleChangeGlobal, Pair)"/>.
/// </summary>
public sealed class GlobalPermissions : DisposableMediatorSubscriberBase
{
    public GlobalPermissions(ILogger<GlobalPermissions> logger, GagspeakMediator mediator) 
        : base(logger, mediator)
    {
        Mediator.Subscribe<DalamudLogoutMessage>(this, _ => _globalPerms = null);
    }

    // private fields.
    private static GlobalPerms? _globalPerms;

    // static exposed accessors.
    public static readonly GlobalPerms? Globals;
    public static bool ForcedToFollow   => !Globals?.ForcedFollow.IsNullOrEmpty() ?? false;
    public static bool ForcedToEmote    => !Globals?.ForcedEmoteState.IsNullOrEmpty() ?? false;
    public static bool ForcedToStay     => !Globals?.ForcedStay.IsNullOrEmpty() ?? false;
    public static bool ChatHidden       => !Globals?.ChatBoxesHidden.IsNullOrEmpty() ?? false;
    public static bool ChatInputHidden  => !Globals?.ChatInputHidden.IsNullOrEmpty() ?? false;
    public static bool ChatInputBlocked => !Globals?.ChatInputBlocked.IsNullOrEmpty() ?? false;

    public static EmoteState ForcedEmoteState => EmoteState.FromString(Globals?.ForcedEmoteState ?? string.Empty);

    /// <summary> For permission updates done by ourselves. </summary>
    public void ChangeGlobalPermission(SingleChangeGlobal dto)
    {
        // Attempt to change the property.
        if (!PropertyChanger.TrySetProperty(_globalPerms, dto.NewPerm.Key, dto.NewPerm.Value, out var newValueState))
        {
            Logger.LogError($"Failed to set GlobalPermission setting [{dto.NewPerm.Key}] to [{dto.NewPerm.Value}].");
            return;
        }

        bool wasChanged = !newValueState.Equals(dto.NewPerm.Value);

        // Get the correct interaction type.
        var changeType = dto.NewPerm.Key switch
        {
            nameof(GlobalPerms.ForcedFollow) => InteractionType.ForcedFollow,
            nameof(GlobalPerms.ForcedEmoteState) => InteractionType.ForcedEmoteState,
            nameof(GlobalPerms.ForcedStay) => InteractionType.ForcedStay,
            nameof(GlobalPerms.ChatBoxesHidden) => InteractionType.ForcedChatVisibility,
            nameof(GlobalPerms.ChatInputHidden) => InteractionType.ForcedChatInputVisibility,
            nameof(GlobalPerms.ChatInputBlocked) => InteractionType.ForcedChatInputBlock,
            _ => InteractionType.ForcedPermChange,
        };

        // if one did occur, we can log it.
        Mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, changeType, $"{dto.NewPerm.Key.ToString()} changed to [{dto.NewPerm.Value.ToString()}]")));

        // If the change was a hardcore action, log a warning.
        if(changeType is not InteractionType.None && changeType is not InteractionType.ForcedPermChange)
            Logger.LogWarning($"Hardcore action [{changeType.ToString()}] has changed, but should never happen!");
    }

    /// <summary> For permission updates done by another user. </summary>
    public void ChangeGlobalPermission(SingleChangeGlobal dto, Pair enactor)
    {
        // Attempt to change the property.
        if(!PropertyChanger.TrySetProperty(_globalPerms, dto.NewPerm.Key, dto.NewPerm.Value, out var newValueState))
        {
            Logger.LogError($"Failed to set GlobalPermission setting [{dto.NewPerm.Key}] to [{dto.NewPerm.Value}].");
            return;
        }

        bool wasChanged = !newValueState.Equals(dto.NewPerm.Value);

        // Get the correct interaction type.
        var changeType = dto.NewPerm.Key switch
        {
            nameof(GlobalPerms.ForcedFollow) => InteractionType.ForcedFollow,
            nameof(GlobalPerms.ForcedEmoteState) => InteractionType.ForcedEmoteState,
            nameof(GlobalPerms.ForcedStay) => InteractionType.ForcedStay,
            nameof(GlobalPerms.ChatBoxesHidden) => InteractionType.ForcedChatVisibility,
            nameof(GlobalPerms.ChatInputHidden) => InteractionType.ForcedChatInputVisibility,
            nameof(GlobalPerms.ChatInputBlocked) => InteractionType.ForcedChatInputBlock,
            _ => InteractionType.ForcedPermChange,
        };

        if (changeType is InteractionType.ForcedPermChange)
        {
            Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), dto.Enactor.UID, InteractionType.ForcedPermChange,
                $"{dto.NewPerm.Key.ToString()} changed to [{dto.NewPerm.Value.ToString()}]")));
        }
        else
        {
            // would be a hardcore permission change in this case.
            Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), dto.Enactor.UID, changeType, $"{changeType.ToString()} changed to [{dto.NewPerm.Value.ToString()}]")));
            var newState = string.IsNullOrEmpty((string)dto.NewPerm.Value) ? NewState.Disabled : NewState.Enabled;
            Mediator.Publish(new HardcoreActionMessage(changeType, newState));
            UnlocksEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, changeType, newState, dto.Enactor.UID, MainHub.UID);
        }
    }
}
