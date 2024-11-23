namespace GagspeakAPI.Enums;

/// <summary>
/// Enum outlining the type of hardcore property that can be on a restraint set.
/// </summary
public enum HardcoreAction
{
    None,
    ForcedFollow,
    ForcedEmoteState,
    ForcedStay,
    ForcedBlindfold,
    ChatboxHiding,
    ChatInputHiding,
    ChatInputBlocking,
}

public enum HardcoreChangeType
{
    LegsRestraint,
    ArmsRestraint,
    Gagged,
    Blindfolded,
    Immobile,
    Weighty,
    LightStimulation,
    MildStimulation,
    HeavyStimulation,
    Safeword,
}

