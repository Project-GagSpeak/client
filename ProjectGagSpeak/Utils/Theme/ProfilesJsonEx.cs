using GagspeakAPI.Profiles;

namespace GagSpeak.Utils.Themes;

/// <summary>
///   Extensions for default templates and theme styles.
/// </summary>
public static class ProfilesJsonEx
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        Converters = { new PrimitiveShapeConverter() },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>
    ///   Retrieves the UserProfileV1 from a JSON string.
    /// </summary>
    public static UserProfileV1 ReadUserV1(string? jsonData)
    {
        jsonData = (jsonData ?? string.Empty).Trim();
        if (jsonData.StartsWith("{", StringComparison.Ordinal))
            return JsonConvert.DeserializeObject<UserProfileV1>(jsonData, Settings) ?? new UserProfileV1() { };
        return new UserProfileV1 { Version = 1, Description = jsonData };
    }

    /// <summary>
    ///   Serializes the UserProfileV1 to a JSON string.
    /// </summary>
    public static string WriteToJson(this UserProfileV1 profileV1)
        => JsonConvert.SerializeObject(profileV1 ?? new UserProfileV1(), Formatting.None, Settings);

    // Revise the below later.
    public static UserProfileTheme ReadUserTheme(JToken? themeToken)
    {
        if (themeToken == null)
            return new UserProfileTheme();
        // Use the centralized JsonSettings that include the PrimitiveShapeConverter
        return themeToken.ToObject<UserProfileTheme>(JsonSerializer.Create(Settings)) ?? new UserProfileTheme();
    }

    public static string WriteThemeToJson(UserProfileTheme theme)
    {
        try
        {
            return JsonConvert.SerializeObject(theme, Formatting.None, Settings);
        }
        catch (Exception ex)
        {
            Svc.Logger.Error(ex, "Failed to serialize UserProfileTheme to JSON.");
            return "{}"; // Return an empty JSON object on failure
        }
    }
}
