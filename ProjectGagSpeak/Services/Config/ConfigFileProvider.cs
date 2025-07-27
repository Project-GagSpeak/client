using Dalamud.Plugin;
using CkCommons.HybridSaver;

namespace GagSpeak.Services.Configs;

/// <summary> Any file type that we want to let the HybridSaveService handle </summary>
public interface IHybridSavable : IHybridConfig<ConfigFileProvider> { }

/// <summary> Helps encapsulate all the configuration file names into a single place. </summary>
public class ConfigFileProvider : IConfigFileProvider
{
    // Shared Config Directories
    public static string AssemblyLocation       => Svc.PluginInterface.AssemblyLocation.FullName;
    public static string AssemblyDirectoryName  => Svc.PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;
    public static string AssemblyDirectory      => Svc.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty;
    public static string GagSpeakDirectory      => Svc.PluginInterface.ConfigDirectory.FullName;
    public static string EventDirectory     { get; private set; } = string.Empty;
    public static string FileSysDirectory   { get; private set; } = string.Empty;
    public static string ThumbnailDirectory { get; private set; } = string.Empty;

    // Shared Client Configs
    public readonly string GagDataJson;

    public readonly string MainConfig;
    public readonly string Patterns;
    public readonly string RecentGlobalChat;
    public readonly string CustomModSettings;
    public readonly string TraitAllowances;
    public readonly string Favorites;
    public readonly string HypnoEffects;
    public readonly string BuzzToys;
    public string CKFS_GagRestrictions => Path.Combine(FileSysDirectory, "fs-gagrestrictions.json");
    public string CKFS_Restrictions => Path.Combine(FileSysDirectory, "fs-restrictions.json");
    public string CKFS_RestraintSets => Path.Combine(FileSysDirectory, "fs-restraintsets.json");
    public string CKFS_CursedLoot => Path.Combine(FileSysDirectory, "fs-cursedloot.json");
    public string CKFS_BuzzToys => Path.Combine(FileSysDirectory, "fs-buzzdevices.json");
    public string CKFS_Patterns => Path.Combine(FileSysDirectory, "fs-patterns.json");
    public string CKFS_Alarms => Path.Combine(FileSysDirectory, "fs-alarms.json");
    public string CKFS_Triggers => Path.Combine(FileSysDirectory, "fs-triggers.json");

    // Shared Server Configs
    public readonly string Nicknames;
    public readonly string ServerConfig;

    // Unique Client Configs Per Account.
    public string GagRestrictions => Path.Combine(CurrentPlayerDirectory, "gag-restrictions.json");
    public string Restrictions => Path.Combine(CurrentPlayerDirectory, "restrictions.json");
    public string RestraintSets => Path.Combine(CurrentPlayerDirectory, "restraint-sets.json");
    public string CursedLoot => Path.Combine(CurrentPlayerDirectory, "cursed-loot.json");
    public string Puppeteer => Path.Combine(CurrentPlayerDirectory, "puppeteer.json");
    public string Alarms => Path.Combine(CurrentPlayerDirectory, "alarms.json");
    public string Triggers => Path.Combine(CurrentPlayerDirectory, "triggers.json");
    public string MetaData => Path.Combine(CurrentPlayerDirectory, "metadata.json");

    public string CurrentPlayerDirectory => Path.Combine(GagSpeakDirectory, CurrentUserUID ?? "InvalidFiles");
    public string? CurrentUserUID { get; private set; } = null;

    public ConfigFileProvider()
    {
        GagDataJson = Path.Combine(AssemblyDirectory, "MufflerCore", "GagData", "gag_data.json");

        EventDirectory = Path.Combine(GagSpeakDirectory, "eventlog");
        FileSysDirectory = Path.Combine(GagSpeakDirectory, "filesystem");
        ThumbnailDirectory = Path.Combine(GagSpeakDirectory, "thumbnails");

        MainConfig = Path.Combine(GagSpeakDirectory, "config.json");
        Patterns = Path.Combine(GagSpeakDirectory, "patterns.json");
        RecentGlobalChat = Path.Combine(GagSpeakDirectory, "global-chat-recent.json");
        CustomModSettings = Path.Combine(GagSpeakDirectory, "custom-mod-settings.json");
        TraitAllowances = Path.Combine(GagSpeakDirectory, "trait-allowances.json");
        Favorites = Path.Combine(GagSpeakDirectory, "favorites.json");
        HypnoEffects = Path.Combine(GagSpeakDirectory, "hypno-effect-presets.json");
        BuzzToys = Path.Combine(GagSpeakDirectory, "buzz-devices.json");

        Nicknames = Path.Combine(GagSpeakDirectory, "nicknames.json");
        ServerConfig = Path.Combine(GagSpeakDirectory, "server.json");

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
        Svc.Logger.Information("Updating Configs for UID: " + uid);
        UpdateUserUID(uid);

        if (!Directory.Exists(CurrentPlayerDirectory))
            Directory.CreateDirectory(CurrentPlayerDirectory);

        Svc.Logger.Information("Configs Updated.");
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
