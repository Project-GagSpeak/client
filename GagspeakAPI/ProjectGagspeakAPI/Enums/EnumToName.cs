using GagspeakAPI.Data.IPC;

namespace GagspeakAPI.Enums;
public static partial class EnumToName
{
    public static string ToName(this InteractionFilter filterKind)
    => filterKind switch
    {
        InteractionFilter.All => "All",
        InteractionFilter.Applier => "Nick/Alias/UID",
        InteractionFilter.Interaction => "Interaction Type",
        InteractionFilter.Content => "Content Details",
        _ => "UNK"
    };

    public static string ToName(this RevertStyle revertStyle)
    => revertStyle switch
    { 
        RevertStyle.RevertToGame => "to Game",
        RevertStyle.RevertToAutomation => "to Automation",
        RevertStyle.RevertEquipToGame => "to Game State",
        RevertStyle.RevertEquipToAutomation => "to Automation",
        _ => "UNK"
    };

    public static string ToHelpText(this RevertStyle revertStyle)
    => revertStyle switch
    {
        RevertStyle.RevertToGame => "Full Appearance is reset to Base Game State.",
        RevertStyle.RevertToAutomation => "Full Appearance is reset to current Job/Gearset Automation."
        + Environment.NewLine + "- If no Automation for Job/Gearset, reverts to Base Game.",
        RevertStyle.RevertEquipToGame => "Only Gear is reset to Base Game State, while Customization is Kept.",
        RevertStyle.RevertEquipToAutomation => "Only Gear is reset to current Job/Gearset Automation, while Customization is kept."
        + "--SEP--THIS DOES NOT CURRENTLY WORK AS GLAMOURER API DOES NOT PLAY NICE WITH THIS CONCEPT "
        + "--SEP--I did my best to make it possible but couldn't. Sorry. This Option will use Revert To Automation instead.",
        _ => "UNK"
    };


    public static string ToName(this Padlocks padlock)
    => padlock switch
    {
        Padlocks.None => "None",
        Padlocks.MetalPadlock => "Metal Padlock",
        Padlocks.CombinationPadlock => "Combination Padlock",
        Padlocks.PasswordPadlock => "Password Padlock",
        Padlocks.FiveMinutesPadlock => "5 Minutes Padlock",
        Padlocks.TimerPasswordPadlock => "Timer Password Padlock",
        Padlocks.OwnerPadlock => "Owner Padlock",
        Padlocks.OwnerTimerPadlock => "Owner Timer Padlock",
        Padlocks.DevotionalPadlock => "Devotional Padlock",
        Padlocks.DevotionalTimerPadlock => "Devotional Timer Padlock",
        Padlocks.MimicPadlock => "Mimic Padlock",
        _ => "UNK"
    };

    public static Padlocks ToPadlock(this string padlockAlias)
    => padlockAlias switch
    {
        "None" => Padlocks.None,
        "Metal Padlock" => Padlocks.MetalPadlock,
        "Combination Padlock" => Padlocks.CombinationPadlock,
        "Password Padlock" => Padlocks.PasswordPadlock,
        "5 Minutes Padlock" => Padlocks.FiveMinutesPadlock,
        "Timer Password Padlock" => Padlocks.TimerPasswordPadlock,
        "Owner Padlock" => Padlocks.OwnerPadlock,
        "Owner Timer Padlock" => Padlocks.OwnerTimerPadlock,
        "Devotional Padlock" => Padlocks.DevotionalPadlock,
        "Devotional Timer Padlock" => Padlocks.DevotionalTimerPadlock,
        "Mimic Padlock" => Padlocks.MimicPadlock,
        // handle outdated calls where they were translated by ToString()
        "MetalPadlock" => Padlocks.MetalPadlock,
        "CombinationPadlock" => Padlocks.CombinationPadlock,
        "PasswordPadlock" => Padlocks.PasswordPadlock,
        "FiveMinutesPadlock" => Padlocks.FiveMinutesPadlock,
        "TimerPasswordPadlock" => Padlocks.TimerPasswordPadlock,
        "OwnerPadlock" => Padlocks.OwnerPadlock,
        "OwnerTimerPadlock" => Padlocks.OwnerTimerPadlock,
        _ => Padlocks.None
    };

    public static string ToName(this InteractionType interactionType)
    => interactionType switch
    {
        InteractionType.None => "None",
        InteractionType.SwappedGag => "Gag Swapped",
        InteractionType.ApplyGag => "Gag Applied",
        InteractionType.LockGag => "Gag Locked",
        InteractionType.UnlockGag => "Gag Unlocked",
        InteractionType.RemoveGag => "Gag Removed",
        InteractionType.SwappedRestraint=> "Restraint Swapped",
        InteractionType.ApplyRestraint => "Restraint Applied",
        InteractionType.LockRestraint => "Restraint Locked",
        InteractionType.UnlockRestraint => "Restraint Unlocked",
        InteractionType.RemoveRestraint => "Restraint Removed",
        InteractionType.ApplyPairMoodle => "Moodle Applied",
        InteractionType.ApplyPairMoodlePreset => "Moodle Preset Applied",
        InteractionType.ApplyOwnMoodle => "Moodle Applied",
        InteractionType.ApplyOwnMoodlePreset => "Moodle Preset Applied",
        InteractionType.RemoveMoodle => "Moodle Removed",
        InteractionType.ClearMoodle => "Moodle Cleared",
        InteractionType.ToggleAlarm => "Alarm Toggled",
        InteractionType.ActivatePattern => "Pattern Activated",
        InteractionType.StopPattern => "Pattern Stopped",
        InteractionType.ToggleTrigger => "Trigger Toggled",
        InteractionType.ShockAction => "Shock Action",
        InteractionType.VibrateAction => "Vibrate Action",
        InteractionType.BeepAction => "Beep Action",
        InteractionType.BulkUpdate => "Bulk Update",
        InteractionType.ForcedFollow => "Forced Follow",
        InteractionType.ForcedEmoteState => "Forced Emote State",
        InteractionType.ForcedStay => "Forced Stay",
        InteractionType.ForcedBlindfold => "Forced Blindfold",
        InteractionType.ForcedChatVisibility => "Chat Visibility",
        InteractionType.ForcedChatInputVisibility => "Chat Input Visibility",
        InteractionType.ForcedChatInputBlock => "Chat Input Block",        
        InteractionType.ForcedPermChange => "Forced Perm Change",
        InteractionType.VibeControl => "Vibe Control",
        _ => "UNK"
    };

    public static string ToName(this IpcToggleType toggleType)
    => toggleType switch
    {
        IpcToggleType.MoodlesStatus => "Moodle Status",
        IpcToggleType.MoodlesPreset => "Moodle Preset",
        _ => "UNK"
    };

    public static string ToName(this Precedence precedence)
    => precedence switch
    {
        Precedence.VeryLow => "Very Low",
        Precedence.Low => "Low",
        Precedence.Default => "Default",
        Precedence.High => "High",
        Precedence.VeryHigh => "Very High",
        Precedence.Highest => "Highest",
        _ => "UNK"
    };

    public static string ToName(this TriggerActionType triggerActionType)
    {
        return triggerActionType switch
        {
            TriggerActionType.Vibration => "Vibration",
            TriggerActionType.PatternPlayback => "Pattern",
            _ => "UNK"
        };
    }
    public static string ToName(this TriggerActionKind triggerActionKind)
    {
        return triggerActionKind switch
        {
            TriggerActionKind.SexToy => "Sex Toy (Lovense Device)",
            TriggerActionKind.ShockCollar => "Shock Collar",
            TriggerActionKind.Restraint => "Apply Restraint",
            TriggerActionKind.Gag => "Apply Gag",
            TriggerActionKind.Moodle => "Apply Moodle",
            TriggerActionKind.MoodlePreset => "Apply Moodle Preset",
            _ => "UNK"
        };
    }

    public static string ToName(this PresetName preset)
    {
        return preset switch
        {
            PresetName.NoneSelected => "None",
            PresetName.Dominant => "Dominant",
            PresetName.Brat => "Brat",
            PresetName.RopeBunny => "Rope Bunny",
            PresetName.Submissive => "Submissive",
            PresetName.Slut => "Slut",
            PresetName.Pet => "Pet",
            PresetName.Slave => "Slave",
            PresetName.OwnersSlut => "Owner's Slut",
            PresetName.OwnersPet => "Owner's Pet",
            PresetName.OwnersSlave => "Owner's Slave",
            _ => "UNK"
        };
    }

    public static string TriggerKindToString(this TriggerKind type)
    {
        return type switch
        {
            TriggerKind.Chat => "Chat Trigger",
            TriggerKind.SpellAction => "Action Trigger",
            TriggerKind.HealthPercent => "Health% Trigger",
            TriggerKind.RestraintSet => "Restraint Trigger",
            TriggerKind.GagState => "GagState Trigger",
            TriggerKind.SocialAction => "Social Action",
            _ => "UNK"
        };
    }

    public static string DirectionToString(this TriggerDirection type)
    {
        return type switch
        {
            TriggerDirection.Self => "From Self",
            TriggerDirection.SelfToOther => "From you to others",
            TriggerDirection.Other => "From Others",
            TriggerDirection.OtherToSelf => "From others to You",
            TriggerDirection.Any => "Any Filter",
            _ => "UNK"
        };
    }

    public static string EffectTypeToString(this LimitedActionEffectType type)
    {
        return type switch
        {
            LimitedActionEffectType.Miss => "Action Missed",
            LimitedActionEffectType.Damage => "Damage Related",
            LimitedActionEffectType.Heal => "Heal Related",
            LimitedActionEffectType.BlockedDamage => "Damage Blocked",
            LimitedActionEffectType.ParriedDamage => "Damage Parried",
            LimitedActionEffectType.Attract1 => "Rescue Used",
            LimitedActionEffectType.Knockback => "Pushed Back",
            _ => "UNK"
        };
    }

    public static string ToName(this LoggerType type)
    {
        return type switch
        {
            LoggerType.None => "None",
            LoggerType.Achievements => "Achievements",
            LoggerType.AchievementEvents => "Events",
            LoggerType.AchievementInfo => "Achievement Info",

            LoggerType.IpcGagSpeak => "Ipc Provider",
            LoggerType.IpcCustomize => "Customize+",
            LoggerType.IpcGlamourer => "Glamourer",
            LoggerType.IpcMare => "Mare",
            LoggerType.IpcMoodles => "Moodles",
            LoggerType.IpcPenumbra => "Penumbra",

            LoggerType.AppearanceState => "Appearance",
            LoggerType.ToyboxState => "Toybox",
            LoggerType.Mediator => "Mediator",
            LoggerType.GarblerCore => "GarblerCore",

            LoggerType.ToyboxAlarms => "Alarms",
            LoggerType.ActionsNotifier => "Actions Notifier",
            LoggerType.KinkPlateMonitor => "KinkPlates™",
            LoggerType.EmoteMonitor => "Emote Monitor",
            LoggerType.ChatDetours => "Chat Detours",
            LoggerType.ActionEffects => "Action Effects",
            LoggerType.SpatialAudioLogger => "Spatial Logger",

            LoggerType.HardcoreActions => "Actions",
            LoggerType.HardcoreMovement => "Movements",
            LoggerType.HardcorePrompt => "Prompts",

            LoggerType.ClientPlayerData => "Client Data",
            LoggerType.GagHandling => "Gag Handling",
            LoggerType.PadlockHandling => "Padlock Handling",
            LoggerType.Restraints => "Restraints",
            LoggerType.Puppeteer => "Puppeteer",
            LoggerType.CursedLoot => "Cursed Loot",
            LoggerType.ToyboxDevices => "Devices",
            LoggerType.ToyboxPatterns => "Patterns",
            LoggerType.ToyboxTriggers => "Triggers",
            LoggerType.VibeControl => "Vibe Control",

            LoggerType.PairManagement => "Pair Management",
            LoggerType.PairInfo => "Pair Info",
            LoggerType.PairDataTransfer => "Pair Data",
            LoggerType.PairHandlers => "Pair Handlers",
            LoggerType.OnlinePairs => "Online Pairs",
            LoggerType.VisiblePairs => "Visible Pairs",
            LoggerType.PrivateRooms => "Private Rooms",
            LoggerType.GameObjects => "Game Objects",

            LoggerType.Cosmetics => "Cosmetics",
            LoggerType.Textures => "Textures",
            LoggerType.GlobalChat => "Global Chat",
            LoggerType.ContextDtr => "Context DTR",
            LoggerType.PatternHub => "Pattern Hub",
            LoggerType.Safeword => "Safeword",

            LoggerType.UiCore => "UI Core",
            LoggerType.UserPairDrawer => "User Pair Drawer",
            LoggerType.Permissions => "Permissions",
            LoggerType.Simulation => "Simulation",

            LoggerType.PiShock => "PiShock",
            LoggerType.ApiCore => "API Core",
            LoggerType.Callbacks => "Callbacks",
            LoggerType.Health => "Health",
            LoggerType.HubFactory => "Hub Factory",
            LoggerType.JwtTokens => "JWT Tokens",
            _ => "UNK"
        };
    }
}
