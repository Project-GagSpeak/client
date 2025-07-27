using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagspeakAPI.Data;

namespace GagSpeak.PlayerClient;
public class HypnoEffectManager : IHybridSavable
{
    private readonly HybridSaveService _saver;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider ser, out bool upa) => (upa = false, ser.HypnoEffects).Item2;
    public string JsonSerialize() => throw new NotImplementedException();
    public HypnoEffectManager(HybridSaveService saver)
    {
        _saver = saver;
        Load();
    }

    private Dictionary<string, HypnoticEffect> _presets = new();
    public IReadOnlyDictionary<string, HypnoticEffect> Presets => _presets;

    public bool TryRenamePreset(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || _presets.ContainsKey(newName))
            return false;

        if (_presets.TryGetValue(oldName, out var effect))
        {
            if(_presets.Remove(oldName, out var removedItem))
                _presets[newName] = new(removedItem);
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public bool TryAddPreset(string name, HypnoticEffect effect)
    {
        if (_presets.ContainsKey(name))
            return false;

        _presets[name] = new(effect);
        _saver.Save(this);
        return true;
    }

    public bool RemovePreset(string name)
    {
        if (_presets.Remove(name))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public void Save()
        => _saver.Save(this);

    public void Load()
    {
        var file = _saver.FileNames.HypnoEffects;
        Svc.Logger.Information($"Loading in HypnoEffect Presets: {file}");
        if (!File.Exists(file))
        {
            Svc.Logger.Warning($"Creating new HypnoEffectPreset file, as none were located at: {file}");
            _saver.Save(this);
            return;
        }

        try
        {
            var text = File.ReadAllText(file);
            var loaded = JsonConvert.DeserializeObject<LoadIntermediary>(text);
            if (loaded is null)
                throw new Exception("Failed to load hypno effect presets.");
            if (loaded.Presets is null)
            {
                Svc.Logger.Warning("No valid HypnoEffect Presets found in file. Initializing empty state.");
                _presets = new Dictionary<string, HypnoticEffect>();
                _saver.Save(this);
                return;
            }
            // otherwise valid so set.
            _presets.Clear();
            // Here you can add version-based migration logic if needed in the future
            foreach (var kvp in loaded.Presets)
                _presets.TryAdd(kvp.Key, kvp.Value);

            _saver.Save(this);
        }
        catch (Bagagwa e)
        {
            Svc.Logger.Error(e, "Failed to load Effect Presets.");
        }
    }
    public void WriteToStream(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
        var obj = new LoadIntermediary
        {
            Version = ConfigVersion,
            Presets = _presets
        };
        JsonSerializer.CreateDefault().Serialize(j, obj);
    }
    // Used to help with object based deserialization from the json loader.
    public class LoadIntermediary
    {
        public int Version { get; set; } = 0;
        public Dictionary<string, HypnoticEffect> Presets { get; set; } = new();
    }
}
