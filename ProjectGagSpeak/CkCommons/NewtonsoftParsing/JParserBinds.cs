using GagSpeak.Kinksters.Storage;
using GagSpeak.State;
using GagSpeak.State.Listeners;
using GagSpeak.Services;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.CkCommons.Newtonsoft;

public static class JParserBinds
{
    public static RestrictionItem FromNormalToken(JToken token, ItemService items, ModSettingPresetManager modPresets)
    {
        if (token is not JObject jsonObject)
            throw new ArgumentException("Invalid JObjectToken!");

        var newItem = new RestrictionItem();
        LoadBindTokenCommon(newItem, jsonObject, items, modPresets);

        return newItem;
    }

    public static BlindfoldRestriction FromBlindfoldToken(JToken token, ItemService items, ModSettingPresetManager modPresets)
    {
        if (token is not JObject json)
            throw new ArgumentException("Invalid JObjectToken!");

        var newItem = new BlindfoldRestriction();
        LoadBindTokenCommon(newItem, json, items, modPresets);

        newItem.HeadgearState = JParser.FromJObject(json["HeadgearState"]);
        newItem.VisorState = JParser.FromJObject(json["VisorState"]);
        newItem.ForceFirstPerson = json["ForceFirstPerson"]?.ToObject<bool>() ?? false;
        newItem.BlindfoldPath = json["BlindfoldPath"]?.ToObject<string>() ?? string.Empty;
        return newItem;
    }

    public static HypnoticRestriction LoadHypnoticToken(JToken token, ItemService items, ModSettingPresetManager modPresets)
    {
        if (token is not JObject json)
            throw new ArgumentException("Invalid JObjectToken!");

        var newItem = new HypnoticRestriction();
        LoadBindTokenCommon(newItem, json, items, modPresets);

        newItem.HeadgearState = JParser.FromJObject(json["HeadgearState"]);
        newItem.VisorState = JParser.FromJObject(json["VisorState"]);
        newItem.ForceFirstPerson = json["ForceFirstPerson"]?.ToObject<bool>() ?? false;
        newItem.HypnotizePath = json["HypnoticPath"]?.ToObject<string>() ?? string.Empty;
        newItem.Effect = json["Effect"]?.ToObject<HypnoticEffect>() ?? new HypnoticEffect();
        return newItem;
    }

    public static CollarRestriction FromCollarToken(JToken token, ItemService items, ModSettingPresetManager modPresets)
    {
        if (token is not JObject json)
            throw new ArgumentException("Invalid JObjectToken!");

        var newItem = new CollarRestriction();
        LoadBindTokenCommon(newItem, json, items, modPresets);

        newItem.OwnerUID = json["OwnerUID"]?.ToObject<string>() ?? string.Empty;
        newItem.CollarWriting = json["CollarWriting"]?.ToObject<string>() ?? string.Empty;
        return newItem;
    }

    // Load in the base restriction information.
    private static void LoadBindTokenCommon(RestrictionItem item, JObject json, ItemService items, ModSettingPresetManager modPresets)
    {
        var modAttachment = ModSettingsPreset.FromRefToken(json["Mod"], modPresets);
        var moodleType = Enum.TryParse<MoodleType>(json["Moodle"]?["Type"]?.ToObject<string>(), out var moodle) ? moodle : MoodleType.Status;
        var moodles = JParser.LoadMoodle(json["Moodle"]);

        item.Identifier = json["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier");
        item.Label = json["Label"]?.ToObject<string>() ?? string.Empty;
        item.ThumbnailPath = json["ThumbnailPath"]?.ToObject<string>() ?? string.Empty;
        item.Glamour = items.ParseGlamourSlot(json["Glamour"]);
        item.Mod = modAttachment;
        item.Moodle = moodles;
        item.Traits = Enum.TryParse<Traits>(json["Traits"]?.ToObject<string>(), out var traits) ? traits : Traits.None;
        item.Stimulation = Enum.TryParse<Stimulation>(json["Stimulation"]?.ToObject<string>(), out var stim) ? stim : Stimulation.None;
        item.DoRedraw = json["Redraw"]?.ToObject<bool>() ?? false;
    }
}
