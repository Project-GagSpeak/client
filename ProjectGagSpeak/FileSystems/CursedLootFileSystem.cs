using CkCommons.FileSystem;
using CkCommons.HybridSaver;
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

        Mediator.Subscribe<ConfigCursedItemChanged>(this, (msg) => OnLootChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is GSModule.CursedLoot) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_CursedLoot), _manager.Storage, LootToIdentifier, LootToName))
            _hybridSaver.Save(this);

        _logger.LogDebug("Reloaded cursed items filesystem with " + _manager.Storage.Count + " cursed items.");
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
        {
            // _logger.LogDebug($"CursedLootFileSystem changed [{type}], saving...");
            _hybridSaver.Save(this);
        }
    }

    public bool FindLeaf(CursedItem loot, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<CursedItem>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == loot);
        return leaf != null;
    }

    private void OnLootChange(StorageChangeType type, CursedItem loot, string? oldString)
    {
        switch (type)
        {
            case StorageChangeType.Created:
                var parent = Root;
                if (oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Bagagwa ex) { _logger.LogWarning(ex, $"Could not move loot because the folder could not be created."); }

                CreateDuplicateLeaf(parent, loot.Label, loot);
                return;
            case StorageChangeType.Deleted:
                if (FindLeaf(loot, out var leaf1))
                    Delete(leaf1);
                return;

            case StorageChangeType.Modified:
                // need to run checks for type changes and modifications.
                if (!FindLeaf(loot, out var existingLeaf))
                    return;
                // Check for type changes.
                if (existingLeaf.Value.GetType() != loot.GetType())
                    UpdateLeafValue(existingLeaf, loot);
                // Detect potential renames.
                if (existingLeaf.Name != loot.Label)
                    RenameWithDuplicates(existingLeaf, loot.Label);
                return;

            case StorageChangeType.Renamed when oldString != null:
                if (!FindLeaf(loot, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, loot.Label);
                return;
        }
    }

    // Used for saving and loading.
    private static string LootToIdentifier(CursedItem loot)
        => loot.Identifier.ToString();

    private static string LootToName(CursedItem loot)
        => loot.Label.FixName();

    private static bool LootHasDefaultPath(CursedItem loot, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(LootToName(loot))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveLoot(CursedItem loot, string fullPath)
        // Only save pairs with non-default paths.
        => LootHasDefaultPath(loot, fullPath)
            ? (string.Empty, false)
            : (LootToIdentifier(loot), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_CursedLoot).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveLoot, true);
}

