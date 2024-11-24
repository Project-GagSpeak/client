using Penumbra.GameData.Structs;

namespace GagSpeak.Interop.IpcHelpers.GameData;
// had to make this to get a workaround for the fact that the json read-write doesn't read from private readonly structs, and equipItem is a private readonly struct
public class GameStainConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(StainIds);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null) throw new ArgumentNullException(nameof(value), "Value cannot be null");

        var gameStain = (StainIds)value;
        // Assuming StainIds is a struct that contains two StainId values
        var stainsArray = new JArray
        {
            // Serialize each StainId to its respective representation
            JToken.FromObject(gameStain.Stain1, serializer),
            JToken.FromObject(gameStain.Stain2, serializer)
        };
        stainsArray.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        // Deserialize the JSON array back into StainIds
        var stainsArray = JArray.Load(reader);
        if (stainsArray.Count >= 2)
        {
            var stain1 = stainsArray[0].ToObject<StainId>(serializer);
            var stain2 = stainsArray[1].ToObject<StainId>(serializer);
            return new StainIds(stain1, stain2);
        }
        else
        {
            return StainIds.None; // Or however you represent a default/empty StainIds
        }
    }
}
