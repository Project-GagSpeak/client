namespace Glamourer.Api.Enums;

/// <summary> Return codes for API functions. </summary>
public enum GlamourerApiEc
{
    /// <summary> The function succeeded. </summary>
    Success = 0,

    /// <summary> The function did not encounter a problem, but also did not do anything. </summary>
    NothingDone = 1,

    /// <summary> The requested actor was not found. </summary>
    ActorNotFound = 2,

    /// <summary> The requested actor was not human, but should have been. </summary>
    ActorNotHuman,

    /// <summary> The requested design was not found. </summary>
    DesignNotFound,

    /// <summary> The requested item was not found or could not be applied to the requested slot. </summary>
    ItemInvalid,

    /// <summary> The state of an actor could not be manipulated because it was locked and the provided key could not unlock it. </summary>
    InvalidKey,

    /// <summary> The provided object could not be converted into a valid Glamourer state to apply. </summary>
    InvalidState,
}
