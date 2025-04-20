using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;

namespace GagSpeak.CkCommons.Newtonsoft;

public static class JParserBinds
{
    public static GarblerRestriction FromGagToken(JToken token, GagType gagType, ItemService items, ModSettingPresetManager modPresets)
    {
        if (token is not JObject json)
            throw new ArgumentException("Invalid JObjectToken!");

        var modAttachment = ModSettingsPreset.FromReferenceJToken(json["Mod"], modPresets);
        var moodleType = Enum.TryParse<MoodleType>(json["Moodle"]?["Type"]?.ToObject<string>(), out var moodle) ? moodle : MoodleType.Status;
        var moodles = JParser.LoadMoodle(json["Moodle"]);

        return new GarblerRestriction(gagType)
        {
            IsEnabled = json["IsEnabled"]?.ToObject<bool>() ?? false,
            Glamour = items.ParseGlamourSlot(json["Glamour"]),
            Mod = modAttachment,
            Moodle = moodles,
            Traits = Enum.TryParse<Traits>(json["Traits"]?.ToObject<string>(), out var traits) ? traits : Traits.None,
            Stimulation = Enum.TryParse<Stimulation>(json["Stimulation"]?.ToObject<string>(), out var stim) ? stim : Stimulation.None,
            HeadgearState = JParser.FromJObject(json["HeadgearState"]),
            VisorState = JParser.FromJObject(json["VisorState"]),
            ProfileGuid = json["ProfileGuid"]?.ToObject<Guid>() ?? throw new ArgumentNullException("ProfileGuid"),
            ProfilePriority = json["ProfilePriority"]?.ToObject<uint>() ?? throw new ArgumentNullException("ProfilePriority"),
            DoRedraw = json["DoRedraw"]?.ToObject<bool>() ?? false,
        };
    }

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
        newItem.Kind = Enum.TryParse<BlindfoldType>(json["Kind"]?.ToObject<string>(), out var kind) ? kind : BlindfoldType.Light;
        newItem.ForceFirstPerson = json["ForceFirstPerson"]?.ToObject<bool>() ?? false;
        newItem.CustomPath = json["CustomPath"]?.ToObject<string>() ?? string.Empty;
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
        var modAttachment = ModSettingsPreset.FromReferenceJToken(json["Mod"], modPresets);
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
