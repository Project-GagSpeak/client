namespace GagSpeak.State;

/// <summary>
///     Dictates what a player has access to.
/// </summary>
[Flags]
public enum HcTaskControl
{
    None = 0 << 0, // no enforced HC actions.
    NoChatInputAccess = 1 << 0, // prevents chat input.
    NoChatInputView = 1 << 1, // hides the chat input box.
    NoChatBoxView = 1 << 2, // hides the chat box

    MustFollow = 1 << 3, // required to follow current target.
    Weighted = 1 << 4, // required to walk.

    LockFirstPerson = 1 << 5, // locks camera to first person. (both cannot be set at the same time)
    LockThirdPerson = 1 << 6, // locks camera to third person. (both cannot be set at the same time)

    BlockMovementKeys = 1 << 7, // prevents movement key input. (WSAD / space)
    BlockAllKeys = 1 << 8, // prevents all key input. (except for CTRL+ALT+BACKSPACE for safeword)

    FreezePlayer = 1 << 9, // fully block movement, via pointer. Cannot follow in this state.

    DoConfinementPrompts = 1 << 10, // enforces scripted prompt answers for confinement blocking.

    InLifestreamTask = 1 << 11, // if processing a lifestream task, other paramaters are temporarily altared.

    InRequiredTurnTask = 1 << 12,

    NoActions = 1 << 13, // blocks all action usage.
    NoTeleport = 1 << 14, // blocks all teleportation actions.
}
