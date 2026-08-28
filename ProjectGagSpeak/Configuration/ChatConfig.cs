using CkCommons;
using CkCommons.HybridSaver;
using CkCommons.RichText;
using Dalamud.Game.Text;
using Dalamud.Interface.FontIdentifier;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Services.Configs;
using GagspeakAPI.Chat;
using NAudio.Wave;

namespace GagSpeak.PlayerClient;

public class ChatConfigData : IAudioConfigData
{
    // CHAT -> RULES //
    public bool ShowInUIHide { get; set; } = false;
    public bool ShowInCutscene { get; set; } = false;
    public bool ShowInGroupPose { get; set; } = false;
    public bool OpenUIOnStartup { get; set; } = false;

    // CHAT -> STYLE //
    public float WindowOpacity { get; set; } = 1f;
    public float UnfocusedWindowOpacity { get; set; } = 0.5f;
    public float OpacityShiftDelta { get; set; } = 0.02f;
    public bool UnreadBubble { get; set; } = true;

    // CHAT -> CUSTOMIZATIONS //
    public bool Timestamps { get; set; } = true;
    public bool ShowEmotes { get; set; } = true;
    public bool UseCustomChatFont { get; set; } = false;
    public IFontSpec? ChatFont { get; set; }

    // PREFS -> GLOBAL-CHAT //
    public ChatFlags ChatPerms { get; set; } = ChatFlags.AllowRequests | ChatFlags.UseDisplayName;
    public bool UseNativeChat { get; set; } = false;
    public XivChatType ChatType { get; set; } = XivChatType.Debug;
    public NativeUiColor ChatColor { get; set; } = GsDefaults.GlobalChatColor;


    // NATIVE UI -> CHAT COLORS //
    // CHAT -> CUSTOMIZATIONS / DMs //
    public bool ShowDMsInChatbox { get; set; } = true;
    public NativeUiColor DMPrefixColor { get; set; } = GsDefaults.DMColorPrefix;
    public NativeUiColor DMTextColor { get; set; } = GsDefaults.DMColorText;

    // NOTIFICATIONS -> CHAT MENTIONS //
    public bool PingOnDM { get; set; } = true;
    public bool MentionHighlights { get; set; } = true;
    public uint MentionColor { get; set; } = GsDefaults.DefaultMentionColor;
    public AlertKind AlertKind { get; set; } = AlertKind.Bubble;
    public string AlertCustomPath { get; set; } = string.Empty;
    public float AlertVolume { get; set; } = 0.5f;
    public Sounds AlertSoundbyte { get; set; } = Sounds.Sound15;
    public bool AlertIsCustom { get; set; } = false;
}

/// <summary>
///   Universal across all accounts.
/// </summary>
public class ChatConfig : IHybridSavable, IAudioConfig<ChatConfigData>, IDisposable
{
    private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        Formatting = Formatting.Indented,
    });

    private readonly ILogger<ChatConfig> _logger;
    private readonly HybridSaveService _saver;

    // Cached items for custom alert configuration.
    private AudioFileReader? _audioFile;
    private WaveOutEvent? _audioEvent;

    // Hybrid Savable stuff
    public int ConfigVersion => 0;
    public int MaxBackups => 2;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string ToFilePath(GsFiles files) => files.ChatConfig;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Config"] = JObject.FromObject(Data, Serializer),
        }.ToString(Formatting.Indented);
    }

    public ChatConfig(ILogger<ChatConfig> logger, HybridSaveService saver)
    {
        _logger = logger;
        _saver = saver;
        Load();
    }

    public void Dispose()
        => DisposeAudio();

    private void DisposeAudio()
    {
        _audioFile?.Dispose();
        _audioFile = null;
        _audioEvent?.Dispose();
        _audioEvent = null;
    }

    public void Save()
    {
        if (NewRichText.ShowEmojis != Data.ShowEmotes)
            NewRichText.ShowEmojis = Data.ShowEmotes;
        _saver.Save(this);
    }

    public void Load()
    {
        var file = _saver.FileNames.ChatConfig;
        _logger.LogInformation($"Loading in Chat Config: {file}");
        if (!File.Exists(file))
        {
            _logger.LogWarning($"ChatConfig file not found: {file}");
            _saver.Save(this);
            return;
        }

        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Load instance configuration
        Data = jObject["Config"]?.ToObject<ChatConfigData>(Serializer)?? new ChatConfigData();
        Save();
        // Re-init audio
        UpdateAudio();

    }

    public ChatConfigData Data { get; private set; } = new();
    
    // Audio Helpers
    public bool IsAudioReady()
        => !Data.AlertIsCustom
        || (_audioFile != null && _audioEvent != null);

    public unsafe bool PlaySound()
    {
        if (!Data.AlertKind.HasAny(AlertKind.Audio))
            return false;

        if (Data.AlertIsCustom)
        {
            if (_audioFile is null || _audioEvent is null)
                return false;

            _audioEvent.Stop();
            _audioFile.Position = 0;
            _audioEvent.Play();
            return true;
        }

        UIGlobals.PlaySoundEffect((uint)Data.AlertSoundbyte);
        return true;
    }

    public void UpdateAudio()
    {
        // Dispose the audio if no longer valid.
        if (!(Data.AlertKind.HasAny(AlertKind.Audio) && Data.AlertIsCustom))
        {
            DisposeAudio();
            Save();
            return;
        }

        try
        {
            // If the audio file name is no longer the chosen sound path, dispose of it.
            if (_audioFile?.FileName != Data.AlertCustomPath)
                DisposeAudio();
            // Recreate the audio with the requested path and volume.
            _audioFile = new AudioFileReader(Data.AlertCustomPath) { Volume = Data.AlertVolume };
            _audioEvent = new WaveOutEvent();
            _audioEvent.Init(_audioFile);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting up alert sound: {ex}");
            DisposeAudio();
        }
        finally
        {
            Save();
        }
    }
}
