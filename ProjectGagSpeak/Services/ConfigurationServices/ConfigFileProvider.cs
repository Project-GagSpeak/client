using Dalamud.Plugin;
using GagSpeak.CkCommons.HybridSaver;

namespace GagSpeak.Services.Configs;

/// <summary> Any file type that we want to let the HybridSaveService handle </summary>
public interface IHybridSavable : IHybridConfig<ConfigFileProvider> { }

/// <summary> Helps encapsulate all the configuration file names into a single place. </summary>
public class ConfigFileProvider : IConfigFileProvider
{
    // Shared Config Directories
    public readonly string GagSpeakDirectory;
    public readonly string EventDirectory;

    // Shared Client Configs
    public readonly string MainConfig;
    public readonly string Patterns;
    public readonly string RecentGlobalChat;
    public readonly string CustomModSettings;
    public readonly string TraitAllowances;
    public readonly string SortFilers;
    public readonly string Favorites;

    // Shared Server Configs
    public readonly string Nicknames;
    public readonly string ServerConfig;
    public readonly string ServerTags;

    // Unique Client Configs Per Account.
    public string GagRestrictions => Path.Combine(CurrentPlayerDirectory, "gag-restrictions.json");
    public string Restrictions => Path.Combine(CurrentPlayerDirectory, "restrictions.json");
    public string RestraintSets => Path.Combine(CurrentPlayerDirectory, "restraint-sets.json");
    public string CursedLoot => Path.Combine(CurrentPlayerDirectory, "cursed-loot.json");
    public string Puppeteer => Path.Combine(CurrentPlayerDirectory, "puppeteer.json");
    public string Alarms => Path.Combine(CurrentPlayerDirectory, "alarms.json");
    public string Triggers => Path.Combine(CurrentPlayerDirectory, "triggers.json");
    public string CurrentPlayerDirectory => Path.Combine(GagSpeakDirectory, CurrentUserUID ?? "InvalidFiles");
    public string? CurrentUserUID { get; private set; } = null;

    public ConfigFileProvider(IDalamudPluginInterface pi)
    {
        GagSpeakDirectory = pi.ConfigDirectory.FullName;
        EventDirectory = Path.Combine(GagSpeakDirectory, "eventlog");
        MainConfig = Path.Combine(GagSpeakDirectory, "config.json");
        Patterns = Path.Combine(GagSpeakDirectory, "patterns.json");
        RecentGlobalChat = Path.Combine(GagSpeakDirectory, "global-chat-recent.json");
        CustomModSettings = Path.Combine(GagSpeakDirectory, "custom-mod-settings.json");
        TraitAllowances = Path.Combine(GagSpeakDirectory, "trait-allowances.json");
        SortFilers = Path.Combine(GagSpeakDirectory, "file-system-arrangements.json");
        Favorites = Path.Combine(GagSpeakDirectory, "favorites.json");
        Nicknames = Path.Combine(GagSpeakDirectory, "nicknames.json");
        ServerConfig = Path.Combine(GagSpeakDirectory, "server.json");
        ServerTags = Path.Combine(GagSpeakDirectory, "servertags.json"); // this is depricated.

        // attempt to load in the UID if the config.json exists.
        if (File.Exists(MainConfig))
        {
            var json = File.ReadAllText(MainConfig);
            var configJson = JObject.Parse(json);
            CurrentUserUID = configJson["Config"]!["LastUidLoggedIn"]?.Value<string>() ?? "UNKNOWN_VOID";
        }
    }

    // If this is not true, we should not be saving our configs anyways.
    public bool HasValidProfileConfigs { get; private set; } = false;

    public void ClearUidConfigs()
    {
        HasValidProfileConfigs = false;
        UpdateUserUID(null);
    }

    public void UpdateConfigs(string uid)
    {
        GagSpeak.StaticLog.Information("Updating Configs for UID: " + uid);
        UpdateUserUID(uid);

        if (!Directory.Exists(CurrentPlayerDirectory))
            Directory.CreateDirectory(CurrentPlayerDirectory);

        GagSpeak.StaticLog.Information("Configs Updated.");
        HasValidProfileConfigs = true;
    }

    private void UpdateUserUID(string? uid)
    {
        if (CurrentUserUID != uid)
        {
            CurrentUserUID = uid;
            UpdateUidInConfig(uid);
        }
    }

    private void UpdateUidInConfig(string? uid)
    {
        var uidFilePath = Path.Combine(GagSpeakDirectory, "config.json");
        if (!File.Exists(uidFilePath))
            return;

        var tempFilePath = uidFilePath + ".tmp";
        using (var reader = new StreamReader(uidFilePath))
        using (var writer = new StreamWriter(tempFilePath))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().StartsWith("\"LastUidLoggedIn\""))
                {
                    writer.WriteLine($"    \"LastUidLoggedIn\": \"{uid ?? ""}\",");
                }
                else
                {
                    writer.WriteLine(line);
                }
            }
        }
        File.Move(tempFilePath, uidFilePath, true);
    }
}
