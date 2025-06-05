using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GagSpeak.FileSystems;

public sealed class GagFileSystem : CkFileSystem<GarblerRestriction>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<GagFileSystem> _logger;
    private readonly GagRestrictionManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public GagFileSystem(ILogger<GagFileSystem> logger, GagspeakMediator mediator,
        GagRestrictionManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigGagRestrictionChanged>(this, (msg) => OnGagChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is Module.Gag) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_GagRestrictions), _manager.Storage.Values, GagToIdentifier, GagToName))
            _hybridSaver.Save(this);

        _logger.LogDebug("Reloaded gags filesystem with " + _manager.Storage.Count + " gags.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigGagRestrictionChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(GarblerRestriction gag, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<GarblerRestriction>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == gag);
        return leaf != null;
    }

    private void OnGagChange(StorageChangeType type, GarblerRestriction gag, string? oldString)
    {
        if (type is StorageChangeType.Modified)
            Reload();
    }

    // Used for saving and loading.
    private static string GagToIdentifier(GarblerRestriction gag)
        => gag.GagType.GagName();

    private static string GagToName(GarblerRestriction gag)
        => gag.GagType.GagName().FixName();

    private static bool GagHasDefaultPath(GarblerRestriction gag, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(GagToName(gag))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveGag(GarblerRestriction gag, string fullPath)
        // Only save pairs with non-default paths.
        => GagHasDefaultPath(gag, fullPath)
            ? (string.Empty, false)
            : (GagToIdentifier(gag), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_GagRestrictions).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveGag, true);
}

