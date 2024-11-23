using MessagePack;

namespace GagspeakAPI.Data.Permissions;

[MessagePackObject(keyAsPropertyName: true)]
public record UserEditAccessPermissions
{
    // unique permissions stored here:
    public bool LiveChatGarblerActiveAllowed { get; set; } = false; // Global
    public bool LiveChatGarblerLockedAllowed { get; set; } = false; // Global
    public bool GagFeaturesAllowed { get; set; } = false;
    public bool OwnerLocksAllowed { get; set; } = false;
    public bool DevotionalLocksAllowed { get; set; } = false;
    public bool ExtendedLockTimesAllowed { get; set; } = false;
    public bool MaxLockTimeAllowed { get; set; } = false;

    // unique permissions for the wardrobe
    public bool WardrobeEnabledAllowed { get; set; } = false; // Global
    public bool ItemAutoEquipAllowed { get; set; } = false; // Global
    public bool RestraintSetAutoEquipAllowed { get; set; } = false; // Global
    public bool ApplyRestraintSetsAllowed { get; set; } = false;
    public bool LockRestraintSetsAllowed { get; set; } = false;
    public bool MaxAllowedRestraintTimeAllowed { get; set; } = false;
    public bool UnlockRestraintSetsAllowed { get; set; } = false;
    public bool RemoveRestraintSetsAllowed { get; set; } = false;

    // unique permissions for the puppeteer
    public bool PuppeteerEnabledAllowed { get; set; } = false; // Global
    public bool AllowSitRequestsAllowed { get; set; } = false;
    public bool AllowMotionRequestsAllowed { get; set; } = false;
    public bool AllowAllRequestsAllowed { get; set; } = false;

    // unique Moodles permissions
    public bool MoodlesEnabledAllowed { get; set; } = false; // Global
    public bool AllowPositiveStatusTypesAllowed { get; set; } = false;
    public bool AllowNegativeStatusTypesAllowed { get; set; } = false;
    public bool AllowSpecialStatusTypesAllowed { get; set; } = false;
    public bool PairCanApplyOwnMoodlesToYouAllowed { get; set; } = false;
    public bool PairCanApplyYourMoodlesToYouAllowed { get; set; } = false;
    public bool MaxMoodleTimeAllowed { get; set; } = false;
    public bool AllowPermanentMoodlesAllowed { get; set; } = false;
    public bool AllowRemovingMoodlesAllowed { get; set; } = false;

    // unique permissions for the toybox
    public bool ToyboxEnabledAllowed { get; set; } = false; // Global
    public bool LockToyboxUIAllowed { get; set; } = false; // Global
    public bool SpatialVibratorAudioAllowed { get; set; } = false; // Global
    public bool CanToggleToyStateAllowed { get; set; } = false;
    public bool CanUseVibeRemoteAllowed { get; set; } = false;
    public bool CanExecutePatternsAllowed { get; set; } = false;
    public bool CanStopPatternsAllowed { get; set; } = false;
    public bool CanToggleAlarmsAllowed { get; set; } = false;
    public bool CanSendAlarmsAllowed { get; set; } = false;
    public bool CanToggleTriggersAllowed { get; set; } = false;
}
