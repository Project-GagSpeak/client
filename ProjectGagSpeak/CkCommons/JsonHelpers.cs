using OtterGui.Classes;
using Penumbra.GameData.Structs;

namespace GagSpeak.CkCommons;

public static class JsonHelp
{
    /// <summary> Helps parse out compacted StainId's to make them only take up one row instead of 2. </summary>
    public static StainIds ParseCompactStainIds(JObject stainJson)
    {
        var result = StainIds.None;
        var gameStainString = (stainJson["Stains"]?.Value<string>() ?? "0,0").Split(',');
        return gameStainString.Length == 2
               && int.TryParse(gameStainString[0], out int stain1)
               && int.TryParse(gameStainString[1], out int stain2)
            ? new StainIds((StainId)stain1, (StainId)stain2)
            : StainIds.None;
    }
    public static OptionalBool FromJObject(JToken? tokenValue)
    {
        if (tokenValue is null)
            return OptionalBool.Null;

        var value = tokenValue.Value<string>() ?? string.Empty;
        return value.ToLowerInvariant() switch
        {
            "true" => OptionalBool.True,
            "false" => OptionalBool.False,
            "null" => OptionalBool.Null,
            _ => throw new ArgumentException("Invalid string value for OptionalBool")
        };
    }
}

