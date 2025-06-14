using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;

namespace GagSpeak.FileSystems;

public sealed class CursedLootFileSystem : CkFileSystem<CursedItem>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<CursedLootFileSystem> _logger;
    private readonly CursedLootManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public CursedLootFileSystem(ILogger<CursedLootFileSystem> logger, GagspeakMediator mediator, 
        CursedLootManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigCursedItemChanged>(this, (msg) => OnRestrictionChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is GagspeakModule.CursedLoot) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_CursedLoot), _manager.Storage, RestrictionToIdentifier, RestrictionToName))
            _hybridSaver.Save(this);

        _logger.LogDebug("Reloaded cursed items filesystem with " + _manager.Storage.Count + " cursed items.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigRestrictionChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(CursedItem restriction, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<CursedItem>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == restriction);
        return leaf != null;
    }

    private void OnRestrictionChange(StorageChangeType type, CursedItem restriction, string? oldString)
    {
        switch (type)
        {
            case StorageChangeType.Created:
                var parent = Root;
                if(oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Could not move restriction because the folder could not be created."); }

                CreateDuplicateLeaf(parent, restriction.Label, restriction);
                return;
            case StorageChangeType.Deleted:
                if (FindLeaf(restriction, out var leaf1))
                    Delete(leaf1);
                return;
            case StorageChangeType.Modified:
                Reload();
                return;
            case StorageChangeType.Renamed when oldString != null:
                if (!FindLeaf(restriction, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, restriction.Label);
                return;
        }
    }

    // Used for saving and loading.
    private static string RestrictionToIdentifier(CursedItem restriction)
        => restriction.Identifier.ToString();

    private static string RestrictionToName(CursedItem restriction)
        => restriction.Label.FixName();

    private static bool RestrictionHasDefaultPath(CursedItem restriction, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(RestrictionToName(restriction))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveRestriction(CursedItem restriction, string fullPath)
        // Only save pairs with non-default paths.
        => RestrictionHasDefaultPath(restriction, fullPath)
            ? (string.Empty, false)
            : (RestrictionToIdentifier(restriction), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_CursedLoot).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveRestriction, true);
}

