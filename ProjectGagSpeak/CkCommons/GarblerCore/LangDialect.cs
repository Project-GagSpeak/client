namespace GagSpeak.CkCommons.GarblerCore;

public enum GarbleCoreLang
{
    English = 0,
    Spanish = 1,
    French = 2,
    Japanese = 3,
}

public enum GarbleCoreDialect
{
    US = 0,
    UK = 1,
    Spain = 2,
    Mexico = 3,
    France = 4,
    Quebec = 5,
    Japan = 6,
}

public static class LangDialectEx
{
    public static IEnumerable<GarbleCoreDialect> GetDialects(this GarbleCoreLang lang)
        => lang switch
        {
            GarbleCoreLang.English => new[] { GarbleCoreDialect.US, GarbleCoreDialect.UK },
            GarbleCoreLang.Spanish => new[] { GarbleCoreDialect.Spain, GarbleCoreDialect.Mexico },
            GarbleCoreLang.French => new[] { GarbleCoreDialect.France, GarbleCoreDialect.Quebec },
            GarbleCoreLang.Japanese => new[] { GarbleCoreDialect.Japan },
            _ => throw new ArgumentOutOfRangeException(nameof(lang)),
        };

    public static string ToName(this GarbleCoreLang lang)
        => lang switch
        {
            GarbleCoreLang.English => "English",
            GarbleCoreLang.Spanish => "Spanish",
            GarbleCoreLang.French => "French",
            GarbleCoreLang.Japanese => "Japanese",
            _ => throw new ArgumentOutOfRangeException(nameof(lang)),
        };

    public static string ToName(this GarbleCoreDialect dialect)
        => dialect switch
        {
            GarbleCoreDialect.US => "US",
            GarbleCoreDialect.UK => "UK",
            GarbleCoreDialect.Spain => "Spain",
            GarbleCoreDialect.Mexico => "Mexico",
            GarbleCoreDialect.France => "France",
            GarbleCoreDialect.Quebec => "Quebec",
            GarbleCoreDialect.Japan => "Japan",
            _ => throw new ArgumentOutOfRangeException(nameof(dialect)),
        };

    public static string ToGarbleCoreId(this GarbleCoreDialect dialect)
    => dialect switch
    {
        GarbleCoreDialect.US => "IPA_US",
        GarbleCoreDialect.UK => "IPA_UK",
        GarbleCoreDialect.Spain => "IPA_SPAIN",
        GarbleCoreDialect.Mexico => "IPA_MEXICO",
        GarbleCoreDialect.France => "IPA_FRENCH",
        GarbleCoreDialect.Quebec => "IPA_QUEBEC",
        GarbleCoreDialect.Japan => "IPA_JAPAN",
        _ => "IPA_US",
    };

    public static GarbleCoreDialect ToDialect(this string dialect)
        => dialect switch
        {
            "IPA_US" => GarbleCoreDialect.US,
            "IPA_UK" => GarbleCoreDialect.UK,
            "IPA_SPAIN" => GarbleCoreDialect.Spain,
            "IPA_MEXICO" => GarbleCoreDialect.Mexico,
            "IPA_FRENCH" => GarbleCoreDialect.France,
            "IPA_QUEBEC" => GarbleCoreDialect.Quebec,
            "IPA_JAPAN" => GarbleCoreDialect.Japan,
            _ => throw new ArgumentOutOfRangeException(nameof(dialect)),
        };
}
