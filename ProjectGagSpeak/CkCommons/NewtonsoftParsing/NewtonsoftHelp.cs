using GagSpeak.PlayerState.Models;
using OtterGui.Classes;
using Penumbra.GameData.Structs;

namespace GagSpeak.CkCommons.Newtonsoft;
public static class JParser
{
    public static JObject Serialize(this Moodle moodle)
    {
        var type = moodle is MoodlePreset ? MoodleType.Preset : MoodleType.Status;

        var json = new JObject
        {
            ["Type"] = type.ToString(),
            ["Id"] = moodle.Id.ToString(),
        };

        if (moodle is MoodlePreset moodlePreset)
        {
            json["StatusIds"] = new JArray(moodlePreset.StatusIds.Select(x => x.ToString()));
        }

        return json;
    }

    public static void LoadMoodle(this Moodle moodle, JToken? token)
    {
        if (token is not JObject jsonObject)
            return;

        var type = Enum.TryParse<MoodleType>(jsonObject["Type"]?.Value<string>(), out var moodleType) ? moodleType : MoodleType.Status;
        moodle.Id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");

        if (moodle is MoodlePreset moodlePreset)
            moodlePreset.StatusIds = jsonObject["StatusIds"]?.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>();
    }

    public static Moodle LoadMoodle(JToken? token)
    {
        if (token is not JObject jsonObject)
            throw new ArgumentException("Invalid JObjectToken!");

        var type = Enum.TryParse<MoodleType>(jsonObject["Type"]?.Value<string>(), out var moodleType) ? moodleType : MoodleType.Status;
        var id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");

        if (type == MoodleType.Preset)
        {
            var statusIds = jsonObject["StatusIds"]?.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>();
            return new MoodlePreset { Id = id, StatusIds = statusIds };
        }

        return new Moodle { Id = id };
    }

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
