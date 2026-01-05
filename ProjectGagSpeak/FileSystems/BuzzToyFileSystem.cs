using CkCommons.FileSystem;
using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GagSpeak.FileSystems;

public sealed class BuzzToyFileSystem : CkFileSystem<BuzzToy>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<BuzzToyFileSystem> _logger;
    private readonly BuzzToyManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public BuzzToyFileSystem(ILogger<BuzzToyFileSystem> logger, GagspeakMediator mediator, 
        BuzzToyManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigSexToyChanged>(this, (msg) => OnSexToyChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is GSModule.SexToys) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_BuzzToys), _manager.Storage.Values, SexToyToIdentifier, SexToyToName))
            _hybridSaver.Save(this);

        _logger.LogDebug($"Reloaded BuzzToys filesystem with {_manager.Storage.Count} items.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigSexToyChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(BuzzToy device, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<BuzzToy>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == device);
        return leaf != null;
    }

    private void OnSexToyChange(StorageChangeType type, BuzzToy device, string? oldString)
    {
        switch (type)
        {
            case StorageChangeType.Created:
                var parent = Root;
                if(oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Bagagwa ex) { _logger.LogWarning(ex, $"Could not move SexToy because the folder could not be created."); }

                CreateDuplicateLeaf(parent, device.LabelName, device);
                return;
            case StorageChangeType.Deleted:
                if (FindLeaf(device, out var leaf1))
                    Delete(leaf1);
                return;

            case StorageChangeType.Modified:
                // need to run checks for type changes and modifications.
                if (!FindLeaf(device, out var existingLeaf))
                    return;
                // Detect potential renames.
                if (existingLeaf.Name != device.LabelName)
                    RenameWithDuplicates(existingLeaf, device.LabelName);
                return;

            case StorageChangeType.Renamed when oldString != null:
                if (!FindLeaf(device, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, device.LabelName);
                return;
        }
    }

    // Used for saving and loading.
    private static string SexToyToIdentifier(BuzzToy device)
        => device.Id.ToString();

    private static string SexToyToName(BuzzToy device)
        => device.LabelName.FixName();

    private static bool SexToyHasDefaultPath(BuzzToy device, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(SexToyToName(device))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveSexToy(BuzzToy device, string fullPath)
        // Only save pairs with non-default paths.
        => SexToyHasDefaultPath(device, fullPath)
            ? (string.Empty, false)
            : (SexToyToIdentifier(device), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_BuzzToys).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveSexToy, true);
}

