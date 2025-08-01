using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;

namespace GagSpeak.Gui.Components;

/// <summary>
/// Helps with copying over details stored on other GagSpeak accounts.
/// </summary>
public class AccountInfoExchanger
{
    public string ConfigDirectory => ConfigFileProvider.GagSpeakDirectory;
    public string CurrentUID => MainHub.UID;

    // the config directory files for each account
    private const string GagRestrictionStorageFile = "gag-storage.json";
    private const string WardrobeFile = "wardrobe.json";
    private const string CursedLootFile = "cursedloot.json";
    private const string TriggersFile = "triggers.json";
    private const string AlarmsFile = "alarms.json";

    public AccountInfoExchanger()
    { }


    // a helper function that can fetch all the folder names in our config directory
    // to fetch the list of UID's from our accounts.
    public HashSet<string> GetUIDs()
    {
        var uids = new HashSet<string>();
        var directories = Directory
            .GetDirectories(ConfigDirectory)
            .Where(c => !c.Contains("eventlog") && !c.Contains("audiofiles"))
            .ToList();
        foreach (var dir in directories)
            uids.Add(Path.GetFileName(dir));
        return uids;
    }

    // obtain the gagstorage dictionary from the specified UID
    public GagRestrictionStorage GetGagRestrictionStorageFromUID(string uid)
    {
        var ret = new GagRestrictionStorage();

/*        var path = Path.Combine(ConfigDirectory, uid, GagRestrictionStorageFile);
        if (!File.Exists(path)) return ret;
        var json = File.ReadAllText(path);
        var configJson = JObject.Parse(json);
        var gagEquipDataObject = configJson["GagRestrictionStorage"]!["GagEquipData"] as JObject ?? new JObject();
        if (gagEquipDataObject is null) return ret;

        foreach (var gagData in gagEquipDataObject)
        {
            // Try to parse GagType directly from the key
            if (gagData.Key.IsValidGagName() && gagData.Value is JObject itemObject)
            {
                var gagType = Enum.GetValues(typeof(GagType)).Cast<GagType>().FirstOrDefault(gt => gt.GagName() == gagData.Key);
                var slotString = itemObject["Slot"]?.Value<string>() ?? "Head";
                var slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), slotString);
                var gagDrawData = new GagDrawData(ItemSvc.NothingItem(slot));
                gagDrawData.Deserialize(itemObject);
                if (ret.GagEquipData.ContainsKey(gagType))
                {
                    ret.GagEquipData[gagType] = gagDrawData;
                }
                else
                {
                    ret.GagEquipData.Add(gagType, gagDrawData);
                }
            }
        }*/
        return ret;
    }

    // obtain the restraint set list from the specified UID
    public List<RestraintSet> GetRestraintSetsFromUID(string uid)
    {
        var ret = new List<RestraintSet>();

/*        var path = Path.Combine(ConfigDirectory, uid, WardrobeFile);
        if (!File.Exists(path)) return ret;
        var json = File.ReadAllText(path);
        var configJson = JObject.Parse(json);

        var wardrobeStorageToken = configJson["WardrobeStorage"];
        if (wardrobeStorageToken is null) return ret;

        var restraintSetsArray = wardrobeStorageToken["RestraintSets"]?.Value<JArray>();
        if (restraintSetsArray is null) return ret;

        foreach (var item in restraintSetsArray)
        {
            var restraintSet = new RestraintSet();
            var ItemValue = item.Value<JObject>();
            if (ItemValue != null)
            {
                restraintSet.Deserialize(ItemValue);
                ret.Add(restraintSet);
            }
        }*/
        return ret;
    }

    // obtain the list of cursed items from the specified UID
    public List<CursedItem> GetCursedItemsFromUID(string uid)
    {
        var ret = new List<CursedItem>();
/*
        var path = Path.Combine(ConfigDirectory, uid, CursedLootFile);
        if (!File.Exists(path)) return ret;
        var json = File.ReadAllText(path);
        var configJson = JObject.Parse(json);

        var cursedItemsArray = configJson["CursedLootStorage"]!["CursedItems"]?.Value<JArray>();
        if (cursedItemsArray is null) return ret;

        foreach (var cursedItem in cursedItemsArray)
        {
            var readCursedItem = new CursedItem();
            var cursedItemValue = cursedItem.Value<JObject>();
            if (cursedItemValue != null)
            {
                readCursedItem.Deserialize(cursedItemValue);
                ret.Add(readCursedItem);
            }
        }*/
        return ret;
    }

    // obtain the pattern list from the specified UID
    public List<Trigger> GetTriggersFromUID(string uid)
    {
        var ret = new List<Trigger>();
/*
        var path = Path.Combine(ConfigDirectory, uid, TriggersFile);
        if (!File.Exists(path)) return ret;
        var json = File.ReadAllText(path);
        var configJson = JObject.Parse(json);


        var triggerArray = configJson["TriggerStorage"]!["Triggers"]?.Value<JArray>();
        if (triggerArray is null) return ret;

        // we are in version 1, so we should deserialize appropriately
        foreach (var triggerToken in triggerArray)
        {
            // we need to obtain the information from the triggers "Type" property to know which kind of trigger to create, as we cant create an abstract "Trigger".
            if (Enum.TryParse(triggerToken["Type"]?.ToString(), out TriggerKind triggerType))
            {
                Trigger? triggerAbstract = null;
                switch (triggerType)
                {
                    case TriggerKind.SpellAction:
                        triggerAbstract = triggerToken.ToObject<SpellActionTrigger>()!;
                        break;
                    case TriggerKind.HealthPercent:
                        triggerAbstract = triggerToken.ToObject<HealthPercentTrigger>()!;
                        break;
                    case TriggerKind.RestraintSet:
                        triggerAbstract = triggerToken.ToObject<RestraintTrigger>()!;
                        break;
                    case TriggerKind.GagState:
                        triggerAbstract = triggerToken.ToObject<GagTrigger>()!;
                        break;
                    case TriggerKind.SocialAction:
                        triggerAbstract = triggerToken.ToObject<SocialTrigger>()!;
                        break;
                }
                if (triggerAbstract != null)
                {
                    ret.Add(triggerAbstract);
                }
            }
        }*/
        return ret;
    }

    // obtain the alarm list from the specified UID
    public List<Alarm> GetAlarmsFromUID(string uid)
    {
        var ret = new List<Alarm>();
/*
        var path = Path.Combine(ConfigDirectory, uid, AlarmsFile);
        if (!File.Exists(path)) return ret;
        var json = File.ReadAllText(path);
        var configJson = JObject.Parse(json);
        var result = JsonConvert.DeserializeObject<AlarmConfig>(configJson.ToString());*/
        return ret;
    }


}

