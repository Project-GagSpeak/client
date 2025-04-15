namespace GagSpeak.PlayerState.Models;

/*public class ModSettingsPreset : IComparable<ModSettingsPreset>
{
    public Guid Identifier { get; set; } = Guid.NewGuid();

    public string Label { get; set; } = string.Empty;
    public Dictionary<string, List<string>> SettingSelections { get; set; } = new();

    public ModSettingsPreset() { }

    public ModSettingsPreset(ModSettingsPreset other)
    {
        Identifier = other.Identifier;
        Label      = other.Label;
        SettingSelections = new Dictionary<string, List<string>>(other.SettingSelections);
    }

    public int CompareTo(ModSettingsPreset? other)
    {
        if (other == null)
            return 1;
        return string.Compare(Label, other.Label, StringComparison.Ordinal);
    }

    public JObject Serialize()
    {
        return new JObject
        {
            ["Identifier"] = Identifier.ToString(),
            ["Label"] = Label,
            ["SettingSelections"] = new JObject(SettingSelections.Select(kvp => new JProperty(kvp.Key, new JArray(kvp.Value)))),
        };
    }

    public void LoadModPreset(JToken? modPreset)
    {
        if (modPreset is not JObject jsonObject)
            return;

        Identifier = Guid.TryParse(jsonObject["Identifier"]?.Value<string>(), out var guid) ? guid : throw new Exception("Invalid GUID Data!");
        Label = jsonObject["Label"]?.Value<string>() ?? string.Empty;
        SettingSelections = jsonObject["SettingSelections"]?.ToObject<Dictionary<string, List<string>>>() ?? new Dictionary<string, List<string>>();
    }
}*/
