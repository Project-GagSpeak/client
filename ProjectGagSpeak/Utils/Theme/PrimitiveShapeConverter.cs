using GagspeakAPI.Profiles;

namespace GagSpeak.Utils.Themes;
#pragma warning disable CS8765

public class PrimitiveShapeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(IPrimativeShape));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        // Read the "Type" property from the JSON
        var shapeType = jsonObject["Type"]?.ToObject<PrimShapeType>()?? throw new JsonSerializationException("Missing or invalid 'Type' property on shape.");

        // Instantiate the correct concrete class based on the Type string/enum
        IPrimativeShape shape = shapeType switch
        {
            PrimShapeType.Circle => new PrimativeCircle(),
            PrimShapeType.Rect => new PrimativeRect(),
            PrimShapeType.Gradient => new PrimativeGradient(),
            PrimShapeType.Quad => new PrimativeQuad(),
            PrimShapeType.Icon => new PrimativeIcon(),
            PrimShapeType.Line => new PrimativeLine(),
            PrimShapeType.Path => new PrimativePath(),
            _ => throw new NotImplementedException($"Unknown shape type: {shapeType}")
        };

        // Populate the rest of the properties onto the new concrete object
        serializer.Populate(jsonObject.CreateReader(), shape);

        return shape;
    }

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        => throw new NotImplementedException();
}
#pragma warning restore CS8765
