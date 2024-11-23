using GagspeakAPI.Enums;

namespace GagspeakAPI.Extensions;
public static class UpdateKindExtensions
{
    public static NewState ToNewState(this DataUpdateKind kind)
    => kind switch
    {
        DataUpdateKind.AppearanceGagAppliedLayerOne => NewState.Enabled,
        DataUpdateKind.AppearanceGagAppliedLayerTwo => NewState.Enabled,
        DataUpdateKind.AppearanceGagAppliedLayerThree => NewState.Enabled,
        DataUpdateKind.WardrobeRestraintApplied => NewState.Enabled,

        DataUpdateKind.AppearanceGagLockedLayerOne => NewState.Locked,
        DataUpdateKind.AppearanceGagLockedLayerTwo => NewState.Locked,
        DataUpdateKind.AppearanceGagLockedLayerThree => NewState.Locked,
        DataUpdateKind.WardrobeRestraintLocked => NewState.Locked,


        DataUpdateKind.AppearanceGagUnlockedLayerOne => NewState.Unlocked,
        DataUpdateKind.AppearanceGagUnlockedLayerTwo => NewState.Unlocked,
        DataUpdateKind.AppearanceGagUnlockedLayerThree => NewState.Unlocked,
        DataUpdateKind.WardrobeRestraintUnlocked => NewState.Unlocked,

        DataUpdateKind.AppearanceGagRemovedLayerOne => NewState.Disabled,
        DataUpdateKind.AppearanceGagRemovedLayerTwo => NewState.Disabled,
        DataUpdateKind.AppearanceGagRemovedLayerThree => NewState.Disabled,
        DataUpdateKind.WardrobeRestraintDisabled => NewState.Disabled,

        _ => NewState.Disabled
    };

    public static GagLayer ToSlot(this DataUpdateKind kind)
    => kind switch
    {
        DataUpdateKind.AppearanceGagAppliedLayerOne => GagLayer.UnderLayer,
        DataUpdateKind.AppearanceGagAppliedLayerTwo => GagLayer.MiddleLayer,
        DataUpdateKind.AppearanceGagAppliedLayerThree => GagLayer.TopLayer,

        DataUpdateKind.AppearanceGagLockedLayerOne => GagLayer.UnderLayer,
        DataUpdateKind.AppearanceGagLockedLayerTwo => GagLayer.MiddleLayer,
        DataUpdateKind.AppearanceGagLockedLayerThree => GagLayer.TopLayer,

        DataUpdateKind.AppearanceGagUnlockedLayerOne => GagLayer.UnderLayer,
        DataUpdateKind.AppearanceGagUnlockedLayerTwo => GagLayer.MiddleLayer,
        DataUpdateKind.AppearanceGagUnlockedLayerThree => GagLayer.TopLayer,

        DataUpdateKind.AppearanceGagRemovedLayerOne => GagLayer.UnderLayer,
        DataUpdateKind.AppearanceGagRemovedLayerTwo => GagLayer.MiddleLayer,
        DataUpdateKind.AppearanceGagRemovedLayerThree => GagLayer.TopLayer,

        _ => GagLayer.UnderLayer
    };

    public static string ToName(this DataUpdateKind kind)
    => kind switch
    {
        DataUpdateKind.None => "None",
        DataUpdateKind.Safeword => "Safeword",
        DataUpdateKind.FullDataUpdate => "Full Data Update",
        DataUpdateKind.AppearanceGagAppliedLayerOne => "Gag Applied [Layer 1]",
        DataUpdateKind.AppearanceGagAppliedLayerTwo => "Gag Applied [Layer 2]",
        DataUpdateKind.AppearanceGagAppliedLayerThree => "Gag Applied [Layer 3]",
        DataUpdateKind.AppearanceGagLockedLayerOne => "Gag Locked [Layer 1]",
        DataUpdateKind.AppearanceGagLockedLayerTwo => "Gag Locked [Layer 2]",
        DataUpdateKind.AppearanceGagLockedLayerThree => "Gag Locked [Layer 3]",
        DataUpdateKind.AppearanceGagUnlockedLayerOne => "Gag Unlocked [Layer 1]",
        DataUpdateKind.AppearanceGagUnlockedLayerTwo => "Gag Unlocked [Layer 2]",
        DataUpdateKind.AppearanceGagUnlockedLayerThree => "Gag Unlocked [Layer 3]",
        DataUpdateKind.AppearanceGagRemovedLayerOne => "Gag Removed [Layer 1]",
        DataUpdateKind.AppearanceGagRemovedLayerTwo => "Gag Removed [Layer 2]",
        DataUpdateKind.AppearanceGagRemovedLayerThree => "Gag Removed [Layer 3]",
        DataUpdateKind.WardrobeRestraintApplied => "Restraint Applied",
        DataUpdateKind.WardrobeRestraintLocked => "Restraint Locked",
        DataUpdateKind.WardrobeRestraintUnlocked => "Restraint Unlocked",
        DataUpdateKind.WardrobeRestraintDisabled => "Restraint Disabled",
        DataUpdateKind.PuppeteerPlayerNameRegistered => "Puppeteer Player Name Registered",
        DataUpdateKind.PuppeteerAliasListUpdated => "Puppeteer Alias List Updated",
        DataUpdateKind.ToyboxPatternExecuted => "Toybox Pattern Executed",
        DataUpdateKind.ToyboxPatternStopped => "Toybox Pattern Stopped",
        DataUpdateKind.ToyboxAlarmToggled => "Toybox Alarm Toggled",
        DataUpdateKind.ToyboxTriggerToggled => "Toybox Trigger Toggled",
        DataUpdateKind.IpcUpdateVisible => "Ipc Update Visible",
        DataUpdateKind.IpcMoodlesStatusManagerChanged => "Ipc Moodles Status Manager Changed",
        DataUpdateKind.IpcMoodlesStatusesUpdated => "Ipc Moodles Statuses Updated",
        DataUpdateKind.IpcMoodlesPresetsUpdated => "Ipc Moodles Presets Updated",
        DataUpdateKind.IpcMoodlesCleared => "Ipc Moodles Cleared",
        _ => "Unknown"
    };

}
