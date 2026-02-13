using CkCommons.FileSystem;
using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GagSpeak.FileSystems;

public sealed class AliasesFileSystem : CkFileSystem<AliasTrigger>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<AliasesFileSystem> _logger;
    private readonly PuppeteerManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public AliasesFileSystem(ILogger<AliasesFileSystem> logger, GagspeakMediator mediator, PuppeteerManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigAliasItemChanged>(this, (msg) => OnAliasChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is GSModule.Puppeteer) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_Aliases), _manager.Storage.Items, AliasToIdentifier, AliasToName))
            _hybridSaver.Save(this);

        _logger.LogDebug($"Reloaded aliases filesystem with {_manager.Storage.Items.Count} alias triggers.");
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

    public bool FindLeaf(AliasTrigger loot, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<AliasTrigger>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == loot);
        return leaf != null;
    }

    private void OnAliasChange(StorageChangeType type, AliasTrigger loot, string? oldString)
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
    private static string AliasToIdentifier(AliasTrigger loot)
        => loot.Identifier.ToString();

    private static string AliasToName(AliasTrigger loot)
        => loot.Label.FixName();

    private static bool LootHasDefaultPath(AliasTrigger loot, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(AliasToName(loot))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveAlias(AliasTrigger loot, string fullPath)
        // Only save pairs with non-default paths.
        => LootHasDefaultPath(loot, fullPath)
            ? (string.Empty, false)
            : (AliasToIdentifier(loot), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_Aliases).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveAlias, true);
}

