// AttackType from ReceiveActionEffect calls.
// https://github.com/NightmareXIV/ECommons/blob/master/ECommons/Hooks/ActionEffectTypes/AttackType.cs

namespace GagspeakAPI.Enums;

public enum InteractionType
{
    None,
    SwappedGag, //
    ApplyGag, //
    LockGag, //
    UnlockGag, //
    RemoveGag, //
    SwappedRestraint, //
    ApplyRestraint, //
    LockRestraint, //
    UnlockRestraint, //
    RemoveRestraint, //
    ApplyPairMoodle, //
    ApplyPairMoodlePreset, //
    ApplyOwnMoodle, //
    ApplyOwnMoodlePreset, //
    RemoveMoodle, //
    ClearMoodle, //
    ToggleAlarm, //
    ActivatePattern, //
    StopPattern, //
    ToggleTrigger, //
    ShockAction, // ???? Forgot where this happens
    VibrateAction, // ???? Forgot where this happens
    BeepAction, // ???? Forgot where this happens
    BulkUpdate, //
    ForcedFollow, //
    ForcedEmoteState, //
    ForcedStay, //
    ForcedBlindfold, //
    ForcedChatVisibility, //
    ForcedChatInputVisibility, //
    ForcedChatInputBlock, //
    ForcedPermChange, //
    VibeControl,
    PiShockUpdate,
}

public enum InteractionFilter
{
    All,
    Applier,
    Interaction,
    Content,
}
