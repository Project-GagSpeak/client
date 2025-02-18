using GagSpeak.PlayerState.Models;

namespace GagSpeak.CkCommons.NewtonsoftHelp;
public static class NewtonsoftHelp
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

        var type = (MoodleType)Enum.Parse(typeof(MoodleType), jsonObject["Type"]?.Value<string>() ?? string.Empty);
        moodle.Id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");

        if (moodle is MoodlePreset moodlePreset)
            moodlePreset.StatusIds = jsonObject["StatusIds"]?.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>();
    }

    public static Moodle LoadMoodle(JToken? token)
    {
        if (token is not JObject jsonObject)
            throw new ArgumentException("Invalid JObjectToken!");

        var type = (MoodleType)Enum.Parse(typeof(MoodleType), jsonObject["Type"]?.Value<string>() ?? string.Empty);
        var id = jsonObject["Id"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");

        if (type == MoodleType.Preset)
        {
            var statusIds = jsonObject["StatusIds"]?.Select(x => x.ToObject<Guid>()) ?? Enumerable.Empty<Guid>();
            return new MoodlePreset { Id = id, StatusIds = statusIds };
        }

        return new Moodle { Id = id };
    }
}
