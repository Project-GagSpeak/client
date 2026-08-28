using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerClient;

public enum ConnectionKind
{
    /// <summary>
    ///   You are connected normally, All data is sent and received.
    /// </summary>
    Normal,

    /// <summary>
    ///   No data is sent or received. (Avoid Connection / Disconnect)
    /// </summary>
    FullPause
}
public class ServerHubInfo
{
    public string HubURI { get; set; } = string.Empty;
    public string HubName { get; set; } = string.Empty;
}

public class ConnectionsConfig : IHybridSavable
{
    public const string MAIN_SERVER_NAME = "GagSpeak Main";
    public const string MAIN_SERVER_URI = "wss://gagspeak.kinkporium.studio";
     
    private static readonly List<ServerHubInfo> OfficialHubs =
    [
        new ServerHubInfo
        {
            HubName = MAIN_SERVER_NAME,
            HubURI  = MAIN_SERVER_URI
        },
    ];

    private readonly ILogger<ConnectionsConfig> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public int ConfigVersion => 1;
    public int MaxBackups => 2;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string ToFilePath(GsFiles files) => files.ConnectionsConfig;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["ConnectionKind"] = (int)_connectionState,
            ["LastKnownProfileUID"] = _lastKnownProfileUID,
            ["CurrentHubIdx"] = _currentHubIdx,
            ["ServerHubs"] = JArray.FromObject(_cachedHubs),
        }.ToString(Formatting.Indented);
    }

    // Privated fields for saving / loading.
    private ConnectionKind _connectionState = ConnectionKind.Normal;
    private static List<ServerHubInfo> _cachedHubs = OfficialHubs;
    private static int _currentHubIdx = 0;
    private string _lastKnownProfileUID = string.Empty;

    public ConnectionsConfig(ILogger<ConnectionsConfig> logger, GagspeakMediator mediator, HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _saver = saver;
        Load();
    }

    /// <summary>
    ///   How we should behave when connected, or if we should at all. (Maybe bind to event call)
    /// </summary>
    public ConnectionKind ConnectionState
    {
        get => _connectionState;
        set
        {
            var prevState = _connectionState;
            _connectionState = value;
            _mediator.Publish(new ConnectionKindChanged(prevState, value));
            _saver.Save(this);
        }
    }
    // READ-ONLY Access
    public static int CurrentHubIndex => _currentHubIdx;
    public static IReadOnlyList<ServerHubInfo> ServerHubs => _cachedHubs;

    public static ServerHubInfo CurrentHub => _cachedHubs[CurrentHubIndex];
    public static string CurrentHubName => _cachedHubs[CurrentHubIndex].HubName;
    public static string CurrentHubURI => _cachedHubs[CurrentHubIndex].HubURI;
    
    public string CurrentProfileUID => _lastKnownProfileUID;

    public void Save()
        => _saver.Save(this);

    public void Load()
    {
        var file = _saver.FileNames.ConnectionsConfig;
        _logger.LogInformation($"Loading in ServerConfig: {file}");
        if (!File.Exists(file))
        {
            _logger.LogWarning($"ServerConfig file not found: {file}");
            _saver.Save(this);
            return;
        }

        // Do not try-catch these, invalid loads of these should not allow the plugin to load.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Load additional fields safely.
        _connectionState = (ConnectionKind)(jObject["ConnectionKind"]?.Value<int>() ?? 0);
        _lastKnownProfileUID = jObject["LastKnownProfileUID"]?.Value<string>() ?? string.Empty;
        _currentHubIdx = jObject["CurrentHubIdx"]?.Value<int>() ?? 0;
        _cachedHubs = jObject["ServerHubs"]?.ToObject<List<ServerHubInfo>>() ?? OfficialHubs;

        // Validate hub index.
        if (_currentHubIdx < 0 || _currentHubIdx >= ServerHubs.Count)
        {
            // Move back to the last selected index. However, if 1, and not in devmode, move to 0.
            var newIdx = Math.Clamp(_currentHubIdx, 0, ServerHubs.Count - 1);
#if !DEBUG
            if (newIdx is 1) newIdx = 0;
#endif
            _logger.LogWarning($"ChosenHubIndex {_currentHubIdx} is out of range. Resetting to {newIdx}.");
            _currentHubIdx = newIdx;
        }

        Save();
    }

    public void SetHubIndex(int newIdx)
    {
        if (newIdx == _currentHubIdx)
            return;

        if (newIdx < 0 || newIdx >= ServerHubs.Count)
        {
            newIdx = Math.Clamp(newIdx, 0, ServerHubs.Count - 1);
#if !DEBUG
                if (newIdx is 1) newIdx = 0;
#endif
        }
        _currentHubIdx = newIdx;
        _lastKnownProfileUID = string.Empty; // Reset last logged in UID when changing hubs.
        _saver.Save(this);
    }

    public void SetCurrentProfile(string userUID)
    {
        _lastKnownProfileUID = userUID;
        _mediator.Publish(new ConnectedHubProfileChanged());
        _saver.Save(this);
    }

    public bool AddServerHub(ServerHubInfo hubInfo)
    {
        // Ensure no URI duplication
        if (ServerHubs.Any(h => string.Equals(h.HubURI, hubInfo.HubURI, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning($"Attempted to add duplicate hub URI: {hubInfo.HubURI}");
            return false;
        }
        // Otherwise, add it.
        _cachedHubs.Add(hubInfo);
        _saver.Save(this);
        return true;
    }

    public bool RemoveHub(ServerHubInfo hubInfo)
    {
        if (_cachedHubs.Remove(hubInfo))
        {
            _logger.LogWarning($"Failed to remove hub: {hubInfo.HubName} with URI {hubInfo.HubURI}");
            return false;
        }
        // Ensure the index is within bounds.
        if (_currentHubIdx >= _cachedHubs.Count)
        {
            _currentHubIdx = Math.Clamp(_currentHubIdx, 0, _cachedHubs.Count - 1);
#if !DEBUG
            if (_currentHubIdx is 1) _currentHubIdx = 0;
#endif
        }
        _saver.Save(this);
        return true;
    }
}
