namespace GagSpeak.PlayerState.Components;

/// <summary> Handles the modification of the stored data for the player state. </summary>
public interface IVisualManager
{
    void OnLogin();
    void OnLogout();
}

public interface IRestraintManager : IVisualManager
{
    /// <summary> Validates if the padlockable object can be applied. </summary>
    bool CanApply(Guid restraintId);

    /// <summary> Validates if the padlock can be locked. </summary>
    bool CanLock(Guid restraintId);

    /// <summary> Validates if the padlock can be unlocked. </summary>
    bool CanUnlock(Guid restraintId);

    /// <summary> Validates if the padlock can be removed. </summary>
    bool CanRemove(Guid restraintId);

    /// <summary> Applies the Restriction, and updates the active state. </summary>
    void ApplyRestriction();

    /// <summary> Locks the restriction's active state with a suitable padlock. </summary>
    void LockRestriction();

    /// <summary> Unlocks the restriction's active state, if permitted. </summary>
    void UnlockRestriction();

    /// <summary> Removes the restriction's active state. </summary>
    void RemoveRestriction();
}

public interface IGagManager : IVisualManager
{
    /// <summary> Validates if the padlockable object can be applied. </summary>
    bool CanApply(GagLayer layer, GagType newGag);

    /// <summary> Validates if the padlock can be locked. </summary>
    bool CanLock(GagLayer layer);

    /// <summary> Validates if the padlock can be unlocked. </summary>
    bool CanUnlock(GagLayer layer);

    /// <summary> Validates if the padlock can be removed. </summary>
    bool CanRemove(GagLayer layer);
}
