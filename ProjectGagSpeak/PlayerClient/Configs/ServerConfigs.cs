using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.WebAPI;

namespace GagSpeak.PlayerClient;

public class NicknamesConfigService : IHybridSavable
{
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string GetFileName(ConfigFileProvider files, out bool upa) => (upa = false, files.Nicknames).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["ServerNicknames"] = JObject.FromObject(Storage),
        }.ToString(Formatting.Indented);
    }
    public NicknamesConfigService(HybridSaveService saver)
    {
        _saver = saver;
        Load(); // This doesnt seem to enjoy being called for some reason.
    }

    public void Save() => _saver.Save(this);
    public void Load()
    {
        var file = _saver.FileNames.Nicknames;
        Svc.Logger.Information("Loading in Config for file: " + file);
        if (!File.Exists(file))
        {
            Svc.Logger.Warning("Config file not found for: " + file);
            return;
        }

        // Do not try-catch these, invalid loads of these should not allow the plugin to load.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // if migrations needed, do logic for that here.
        if (jObject["ServerNicknames"]?["UidServerComments"] is JToken)
        {
            // Contains old config, migrate it.
            jObject = ConfigMigrator.MigrateNicknamesConfig(jObject);
        }

        switch (version)
        {
            case 0:
                LoadV0(jObject["ServerNicknames"]);
                break;
            default:
                Svc.Logger.Error("Invalid Version!");
                return;
        }
        Svc.Logger.Information("Config loaded.");
        Save();
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JObject serverNicknames)
            return;
        Storage = serverNicknames.ToObject<ServerNicknamesStorage>() ?? throw new Exception("Failed to load ServerNicknamesStorage.");
        // clean out any kvp with null or whitespace values.
        foreach (var kvp in Storage.Nicknames.Where(kvp => string.IsNullOrWhiteSpace(kvp.Value)).ToList())
            Storage.Nicknames.Remove(kvp.Key);
    }

    public ServerNicknamesStorage Storage { get; set; } = new ServerNicknamesStorage();
}



public class ServerNicknamesStorage
{
    public HashSet<string> OpenPairListFolders { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Nicknames { get; set; } = new(StringComparer.Ordinal);
}


public class ServerConfigService : IHybridSavable
{
    private readonly ILogger<ServerConfigService> _logger;
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string GetFileName(ConfigFileProvider files, out bool upa) => (upa = false, files.ServerConfig).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["ServerStorage"] = JObject.FromObject(Storage),
        }.ToString(Formatting.Indented);
    }
    public ServerConfigService(ILogger<ServerConfigService> logger, HybridSaveService saver)
    {
        _logger = logger;
        _saver = saver;
        Load();
    }

    public void Save() => _saver.Save(this);
    public void Load()
    {
        var file = _saver.FileNames.ServerConfig;
        Svc.Logger.Information("Loading in Config for file: " + file);
        if (!File.Exists(file))
        {
            Svc.Logger.Warning("Config file not found for: " + file);
            return;
        }

        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // if migrations needed, do logic for that here.
        if (jObject["ServerStorage"]?["ToyboxFullPause"] is not null)
        {
            // Contains old config, migrate it.
            jObject = ConfigMigrator.MigrateServerConfig(jObject, _saver.FileNames);
        }

        // execute based on version.
        switch (version)
        {
            case 0:
                LoadV0(jObject["ServerStorage"]);
                break;
            default:
                Svc.Logger.Error("Invalid Version!");
                return;
        }
        Svc.Logger.Information("Config loaded.");
        Save();
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JObject storage)
            return;
        Storage = storage.ToObject<ServerStorage>() ?? throw new Exception("Failed to load ServerStorage.");
    }

    public ServerStorage Storage { get; set; } = new ServerStorage();
}

public class ServerStorage
{
    public List<Authentication> Authentications { get; set; } = [];  // the authentications we have for this client
    public bool FullPause { get; set; } = false;                     // if client is disconnected from the server (not integrated yet)
    public string ServerName { get; set; } = MainHub.MAIN_SERVER_NAME;     // name of the server client is connected to
    public string ServiceUri { get; set; } = MainHub.MAIN_SERVER_URI; // address of the server the client is connected to
}


public record Authentication
{
    public ulong CharacterPlayerContentId { get; set; } = 0;
    public string CharacterName { get; set; } = string.Empty;
    public ushort WorldId { get; set; } = 0;
    public bool IsPrimary { get; set; } = false;
    public SecretKey SecretKey { get; set; } = new();
}


public class SecretKey
{
    public string Label { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool HasHadSuccessfulConnection { get; set; } = false;
    public string LinkedProfileUID { get; set; } = string.Empty;
}
