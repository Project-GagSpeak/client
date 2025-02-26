using Dalamud.Plugin;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;

namespace GagSpeak.Services.Configs;

/// <summary> Any file type that we want to let the HybridSaveService handle </summary>
public interface IHybridSavable : IHybridConfig<ConfigFileProvider> { }

/// <summary> Helps encapsulate all the configuration file names into a single place. </summary>
public class ConfigFileProvider : IMediatorSubscriber, IDisposable, IConfigFileProvider
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

    // If anything gets written here, then there are serious issues going on...
    public string CurrentPlayerDirectory => Path.Combine(GagSpeakDirectory, CurrentUserUID ?? "InvalidFiles");

    private Task? _accountConfigLoadTask = null;
    public GagspeakMediator Mediator { get; }
    public string? CurrentUserUID { get; private set; } = null;

    public ConfigFileProvider(GagspeakMediator mediator, IDalamudPluginInterface pi)
    {
        StaticLogger.Logger.LogCritical("IM BEING INITIALIZED!");

        Mediator = mediator;

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
        ServerTags = Path.Combine(GagSpeakDirectory, "servertags.json");

        // attempt to load in the UID if the config.json exists.
/*        if(File.Exists(MainConfig))
        {
            var json = File.ReadAllText(MainConfig);
            var configJson = JObject.Parse(json);
            CurrentUserUID = configJson["LastUidLoggedIn"]?.Value<string>() ?? string.Empty;
        }*/

        Mediator.Subscribe<DalamudLogoutMessage>(this, (msg) =>
        {
            ClearUidConfigs();
        });

        Mediator.Subscribe<MainHubConnectedMessage>(this, _ =>
        {
            PerPlayerConfigsInitialized = false;
            if (MainHub.ConnectionDto is null)
                return;

            if (MainHub.UID != CurrentUserUID)
            {
                if (_accountConfigLoadTask is not null)
                    _accountConfigLoadTask.Wait();
                // assign the task.
                _accountConfigLoadTask = Task.Run(() => UpdateConfigs(MainHub.UID));
            }
            else
            {
                PerPlayerConfigsInitialized = true;
            }
        });
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
    }

    // If this is not true, we should not be saving our configs anyways.
    public bool PerPlayerConfigsInitialized { get; private set; } = false;

    public void ClearUidConfigs()
    {
        PerPlayerConfigsInitialized = false;
        if (_accountConfigLoadTask is not null)
            _accountConfigLoadTask.Wait();
        // assign the task.
        _accountConfigLoadTask = Task.Run(() => UpdateUserUID(null));
    }

    private void UpdateUserUID(string? uid)
    {
        if (CurrentUserUID != uid)
        {
            CurrentUserUID = uid;
            PerPlayerConfigsInitialized = false;
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
                    writer.WriteLine($"  \"LastUidLoggedIn\": \"{uid ?? ""}\",");
                }
                else
                {
                    writer.WriteLine(line);
                }
            }
        }
        File.Move(tempFilePath, uidFilePath, true);
    }

    private void UpdateConfigs(string uid)
    {
        UpdateUserUID(uid);
        // create a directory for the current player if none is set.
        if (!Directory.Exists(CurrentPlayerDirectory))
            Directory.CreateDirectory(CurrentPlayerDirectory);
        // set the flag to true.
        PerPlayerConfigsInitialized = true;
    }
}
