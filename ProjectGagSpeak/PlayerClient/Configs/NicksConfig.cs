using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.WebAPI;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerClient;

public class NicksStorage
{
    public Dictionary<string, string> Nicknames { get; set; } = new(StringComparer.Ordinal);
}

public class NicksConfig : IHybridSavable
{
    private readonly ILogger<NicksConfig> _logger;
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
            ["ServerNicknames"] = JObject.FromObject(Current),
        }.ToString(Formatting.Indented);
    }
    public NicksConfig(ILogger<NicksConfig> logger, HybridSaveService saver)
    {
        _logger = logger;
        _saver = saver;
        Load();
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

        switch (version)
        {
            case 0:
                LoadV0(jObject["ServerNicknames"]);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        Save();
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JObject serverNicknames)
            return;
        Current = serverNicknames.ToObject<NicksStorage>() ?? throw new Exception("Failed to load NicknamesStorage.");
        // clean out any kvp with null or whitespace values.
        foreach (var kvp in Current.Nicknames.Where(kvp => string.IsNullOrWhiteSpace(kvp.Value)).ToList())
            Current.Nicknames.Remove(kvp.Key);
    }

    public NicksStorage Current { get; set; } = new NicksStorage();

    /// <summary>
    ///     Try to get a nickname for the provided UID.
    /// </summary>
    public bool TryGetNickname(string uid, [NotNullWhen(true)] out string? nickname)
        => Current.Nicknames.TryGetValue(uid, out nickname);

    /// <returns>
    ///     Returns the nickname if found, null otherwise.
    /// </returns>
    public string? GetNicknameForUid(string uid)
        => Current.Nicknames.TryGetValue(uid, out var n) && n is { Length: > 0 } ? n : null;


    /// <summary> 
    ///     Set a nickname for a user identifier.
    /// </summary>
    /// <param name="uid">the user identifier</param>
    /// <param name="nickname">the nickname to add</param>
    public void SetNickname(string uid, string nickname)
    {
        if (string.IsNullOrEmpty(uid))
            return;

        Current.Nicknames[uid] = nickname;
        Save();
    }
}
