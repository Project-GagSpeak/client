using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Hardcore.ForcedStay;
using GagspeakAPI.Data.Interfaces;
using Newtonsoft.Json.Converters;
using Penumbra.GameData.Data;

namespace GagSpeak.InterfaceConverters;

/*public class TriggerGSConverter : CustomCreationConverter<IActionGS>
{
    public override IActionGS Create(Type objectType) { throw new NotImplementedException(); }
    public IActionGS Create(Type objectType, JObject jObject)
    {
        StaticLogger.Logger.LogWarning("Full Parsed Json JObject:\n" + jObject.ToString());

        var executionType = jObject.Property("ExecutionType")?.ToObject<ActionExecutionType>();
     
        StaticLogger.Logger.LogWarning("ExecutionValue: {0}", executionType);

        return executionType switch
        {
            ActionExecutionType.TextOutput => new TextAction(),
            ActionExecutionType.Gag => new GagAction(),
            ActionExecutionType.Restraint => new RestraintAction(),
            ActionExecutionType.Moodle => new MoodleAction(),
            ActionExecutionType.ShockCollar => new PiShockAction(),
            ActionExecutionType.SexToy => new SexToyAction(),
            _ => throw new JsonSerializationException($"Unknown ExecutionType: {executionType}")
        };
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var target = Create(objectType, jObject);
        serializer.Populate(jObject.CreateReader(), target);
        return target;
    }
}
*/
/*public class TriggerTypeConverter : JsonConverter
{
    public override bool CanRead => true;

    public override bool CanWrite => true;

    public override bool CanConvert(Type objectType) => typeof(Trigger).IsAssignableFrom(objectType);

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var jType = jObject["$type"]!.Value<string>();

        if (jType is null) throw new JsonSerializationException("Missing $type property in JSON.");

        if (jType == SimpleName(typeof(ChatTrigger))) return CreateObject<ChatTrigger>(jObject, serializer);
        else if (jType == SimpleName(typeof(SpellActionTrigger))) return CreateObject<SpellActionTrigger>(jObject, serializer);
        else if (jType == SimpleName(typeof(HealthPercentTrigger))) return CreateObject<HealthPercentTrigger>(jObject, serializer);
        else if (jType == SimpleName(typeof(GagTrigger))) return CreateObject<GagTrigger>(jObject, serializer);
        else if (jType == SimpleName(typeof(RestraintTrigger))) return CreateObject<RestraintTrigger>(jObject, serializer);
        else if (jType == SimpleName(typeof(SocialTrigger))) return CreateObject<SocialTrigger>(jObject, serializer);
        else throw new JsonSerializationException($"Unknown TriggerType: {jType}");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        try
        {
            var jObject = JObject.FromObject(value!, serializer);
            jObject.AddFirst(new JProperty("$type", SimpleName(value!.GetType())));
            jObject.WriteTo(writer);
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogWarning("Error!" + e + "\n\n" + value?.GetType().Name);
            throw;
        }
    }
    private static T CreateObject<T>(JObject jObject, JsonSerializer serializer) where T : new()
    {
        var obj = new T();
        try
        {
            serializer.Populate(jObject.CreateReader(), obj);
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogWarning(e, "Failed to create object of type {0}", typeof(T).Name);
            throw;
        }
        return obj;
    }

    private static string SimpleName(Type type)
    {
        return $"{type.Name}, {type.Assembly.GetName().Name}";
    }
}*/

/*public class GSActionConverter : JsonConverter<IActionGS>
{

    public override IActionGS ReadJson(JsonReader reader, Type objectType, IActionGS? existingValue, bool hasValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        StaticLogger.Logger.LogWarning("READING GSCONVERTER Full Parsed Json JObject:\n" + jObject.ToString());

        var jType = jObject["$type"]!.Value<string>();

        if (jType == SimpleName(typeof(TextAction)))
        {
            return CreateObject<TextAction>(jObject, serializer);
        }
        else if (jType == SimpleName(typeof(GagAction)))
        {
            return CreateObject<GagAction>(jObject, serializer);
        }
        else if (jType == SimpleName(typeof(RestraintAction)))
        {
            return CreateObject<RestraintAction>(jObject, serializer);
        }
        else if (jType == SimpleName(typeof(MoodleAction)))
        {
            return CreateObject<MoodleAction>(jObject, serializer);
        }
        else if (jType == SimpleName(typeof(PiShockAction)))
        {
            return CreateObject<PiShockAction>(jObject, serializer);
        }
        else if (jType == SimpleName(typeof(SexToyAction)))
        {
            return CreateObject<SexToyAction>(jObject, serializer);
        }
        else
        {
            throw new NotSupportedException($"Unknown IActionGSType: {jType}");
        }
    }

    public override void WriteJson(JsonWriter writer, IActionGS? value, JsonSerializer serializer)
    {
        try
        {
            // Log the type of the object
            StaticLogger.Logger.LogWarning("Type: {0}", value!.GetType().Name);
            string serializedValue = JsonConvert.SerializeObject(value, Formatting.Indented);
            StaticLogger.Logger.LogWarning("WRITING Full Parsed Json JObject:\n" + serializedValue);
            var jObject = JObject.FromObject(value, serializer);
            jObject.AddFirst(new JProperty("$type", SimpleName(value!.GetType())));
            jObject.WriteTo(writer);
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogWarning(e, "Failed to write object of type {0}", value!.GetType().Name);
            throw;
        }
    }
    private static T CreateObject<T>(JObject jObject, JsonSerializer serializer) where T : new()
    {
        var obj = new T();
        try
        {
            serializer.Populate(jObject.CreateReader(), obj);
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogWarning(e, "Failed to create object of type {0}", typeof(T).Name);
            throw;
        }
        return obj;
    }

    private static string SimpleName(Type type)
    {
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}*/
