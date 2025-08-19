using CkCommons.FileSystem;
using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;

namespace GagSpeak.FileSystems;

public sealed class CollarFileSystem : CkFileSystem<GagSpeakCollar>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<CollarFileSystem> _logger;
    private readonly CollarManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public CollarFileSystem(ILogger<CollarFileSystem> logger, GagspeakMediator mediator, 
        CollarManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigCollarChanged>(this, (msg) => OnCollarChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is GagspeakModule.Collar) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_Collars), _manager.Storage, CollarToIdentifier, CollarToName))
            _hybridSaver.Save(this);

        _logger.LogDebug("Reloaded restrictions filesystem with " + _manager.Storage.Count + " collar items.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigCollarChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(GagSpeakCollar collar, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<GagSpeakCollar>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == collar);
        return leaf != null;
    }

    private void OnCollarChange(StorageChangeType type, GagSpeakCollar collar, string? oldString)
    {
        switch (type)
        {
            case StorageChangeType.Created:
                var parent = Root;
                if(oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Bagagwa ex) { _logger.LogWarning(ex, $"Could not move collar because the folder could not be created."); }

                CreateDuplicateLeaf(parent, collar.Label, collar);
                return;

            case StorageChangeType.Deleted:
                if (FindLeaf(collar, out var leaf1))
                    Delete(leaf1);
                return;

            case StorageChangeType.Modified:
                Reload();
                return;

            case StorageChangeType.Renamed when oldString != null:
                if (!FindLeaf(collar, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, collar.Label);
                return;
        }
    }

    // Used for saving and loading.
    private static string CollarToIdentifier(GagSpeakCollar collar)
        => collar.Identifier.ToString();

    private static string CollarToName(GagSpeakCollar collar)
        => collar.Label.FixName();

    private static bool CollarHasDefaultPath(GagSpeakCollar collar, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(CollarToName(collar))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveCollar(GagSpeakCollar collar, string fullPath)
        // Only save pairs with non-default paths.
        => CollarHasDefaultPath(collar, fullPath)
            ? (string.Empty, false)
            : (CollarToIdentifier(collar), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_Collars).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveCollar, true);
}

