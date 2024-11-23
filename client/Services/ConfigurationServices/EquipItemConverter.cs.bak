using Newtonsoft.Json;
using Penumbra.GameData.Structs;

namespace GagSpeak;
public class EquipItemConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(EquipItem))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (objectType == typeof(EquipItem))
        {
            var surrogate = serializer.Deserialize<EquipItemSurrogate>(reader);
            return (EquipItem)surrogate;
        }
        return null!;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var surrogate = (EquipItemSurrogate)(EquipItem)value;
        serializer.Serialize(writer, surrogate);
    }
}
