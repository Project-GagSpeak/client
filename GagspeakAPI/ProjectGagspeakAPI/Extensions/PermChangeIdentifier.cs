using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;

namespace GagspeakAPI.Extensions;

public static class PermChangeIdentifier
{
    public static HardcoreAction GetHardcoreChange(this UserGlobalPermissions currentPermissions, string changedPermission, object changedValue)
    {
        switch (changedPermission)
        {
            case nameof(UserGlobalPermissions.ForcedFollow):
                return currentPermissions.ForcedFollow != (string)changedValue ?
                       HardcoreAction.ForcedFollow: HardcoreAction.None;
            case nameof(UserGlobalPermissions.ForcedEmoteState):
                return currentPermissions.ForcedEmoteState != (string)changedValue ?
                       HardcoreAction.ForcedEmoteState : HardcoreAction.None;
            case nameof(UserGlobalPermissions.ForcedStay):
                return currentPermissions.ForcedStay != (string)changedValue ?
                       HardcoreAction.ForcedStay : HardcoreAction.None;
            case nameof(UserGlobalPermissions.ForcedBlindfold):
                return currentPermissions.ForcedBlindfold != (string)changedValue ?
                       HardcoreAction.ForcedBlindfold : HardcoreAction.None;
            case nameof(UserGlobalPermissions.ChatBoxesHidden):
                return currentPermissions.ChatBoxesHidden != (string)changedValue ?
                       HardcoreAction.ChatboxHiding : HardcoreAction.None;
            case nameof(UserGlobalPermissions.ChatInputHidden):
                return currentPermissions.ChatInputHidden != (string)changedValue ?
                       HardcoreAction.ChatInputHiding : HardcoreAction.None;
            case nameof(UserGlobalPermissions.ChatInputBlocked):
                return currentPermissions.ChatInputBlocked != (string)changedValue ?
                       HardcoreAction.ChatInputBlocking : HardcoreAction.None;
            default:
                return HardcoreAction.None;
        }
    }


}
