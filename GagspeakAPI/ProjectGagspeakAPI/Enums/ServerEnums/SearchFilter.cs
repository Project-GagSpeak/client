namespace GagspeakAPI.Enums;

public enum SearchFilter
{
    Downloads,
    Likes,
    MostRecent,
    Author,
    DurationTiny, // under a minute
    DurationShort, // 5 minutes or less
    DurationMedium, // 5 to 20 minutes
    DurationLong, // under an hour
    DurationExtraLong, // over an hour
    // TODO: Move this to supported Types enum later.
    UsesVibration,
    UsesRotation,
}

public enum SearchSort
{
    Ascending,
    Descending,
}

public enum SupportedTypes
{
    Vibration,
    Rotation,
}
