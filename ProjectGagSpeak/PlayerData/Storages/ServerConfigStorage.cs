using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.WebAPI;

namespace GagSpeak.PlayerData.Storage;

public class NicknamesConfigService : IHybridSavable
{
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string GetFileName(ConfigFileProvider files, out bool upa) => (upa = false, files.Nicknames).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize() => JsonConvert.SerializeObject(Storage, Formatting.Indented);
    public NicknamesConfigService(HybridSaveService saver)
    {
        _saver = saver;
        Load();
    }

    public void Save() => _saver.Save(this);
    public void Load()
    {
        var file = _saver.FileNames.Nicknames;
        GagSpeak.StaticLog.Warning("Loading in Config for file: " + file);

        if (!File.Exists(file)) return;
        try
        {
            var load = JsonConvert.DeserializeObject<ServerNicknamesStorage>(File.ReadAllText(file));
            if (load is null) throw new Exception("Failed to load Config.");

            Storage = load;
        }
        catch (Exception e) { GagSpeak.StaticLog.Error(e, "Failed to load Config."); }
    }

    public ServerNicknamesStorage Storage { get; set; } = new ServerNicknamesStorage();
}



public class ServerNicknamesStorage
{
    public HashSet<string> OpenPairListFolders { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Nicknames { get; set; } = new(StringComparer.Ordinal);
}

////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////

public class ServerConfigService : IHybridSavable
{
    private readonly ILogger<ServerConfigService> _logger;
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string GetFileName(ConfigFileProvider files, out bool upa) => (upa = false, files.ServerConfig).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize() => JsonConvert.SerializeObject(Storage, Formatting.Indented);
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
        GagSpeak.StaticLog.Warning("Loading in Config for file: " + file);
        if (!File.Exists(file))
        {
            GagSpeak.StaticLog.Warning("Config file not found for: " + file);
            return;
        }

        try
        {
            string fileContent = File.ReadAllText(file);
            GagSpeak.StaticLog.Warning($"Raw Config File Content: {fileContent}");
            GagSpeak.StaticLog.Warning("ProposedConfigToMatch: " + JsonConvert.SerializeObject(Storage, Formatting.Indented));

            var load = JsonConvert.DeserializeObject<ServerStorage>(fileContent);
            if (load is null) throw new Exception("Failed to load Config.");

            Storage = load;
            GagSpeak.StaticLog.Warning("Loaded Server Config.");
        }
        catch (Exception e) { GagSpeak.StaticLog.Error(e, "Failed to load Config."); }
    }

    public ServerStorage Storage { get; set; } = new ServerStorage();
}

public class ServerStorage
{
    public List<Authentication> Authentications { get; set; } = [];  // the authentications we have for this client
    public bool FullPause { get; set; } = false;                     // if client is disconnected from the server (not integrated yet)
    public string ServerName { get; set; } = MainHub.MainServer;     // name of the server client is connected to
    public string ServiceUri { get; set; } = MainHub.MainServiceUri; // address of the server the client is connected to
}


public record Authentication
{
    public ulong CharacterPlayerContentId { get; set; } = 0;
    public string CharacterName { get; set; } = string.Empty;
    public uint WorldId { get; set; } = 0;
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
