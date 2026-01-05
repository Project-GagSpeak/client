using CkCommons.FileSystem;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.FileSystems;

public sealed class ModPresetFileSystem : CkFileSystem<ModPresetContainer>, IMediatorSubscriber, IDisposable
{
    private readonly ILogger<ModPresetFileSystem> _logger;
    private readonly ModPresetManager _manager;
    public GagspeakMediator Mediator { get; init; }

    // internally cache the dictionary for the folder paths, since they only exist during the plugins lifetime, and are not saved to file. 
    private Dictionary<string, string> hierarchyData = new Dictionary<string, string>();
    private string[] emptyFolders = Array.Empty<string>();

    public ModPresetFileSystem(ILogger<ModPresetFileSystem> logger, GagspeakMediator mediator, ModPresetManager manager)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;

        Mediator.Subscribe<ConfigModPresetChanged>(this, (msg) => OnModPresetChange(msg.Type, msg.Item, msg.OldDirString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => OnFileSystemReload(msg.Module));
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        // update the hierarchy to the latest state.
        hierarchyData = _manager.ModPresetStorage.ToDictionary(
            mp => mp.DirectoryPath, mp => mp.FileSystemPath);

        Load(hierarchyData, emptyFolders, _manager.ModPresetStorage, ModPresetToIdentifier, ModPresetToName);
        _logger.LogDebug($"Reloaded ModPresets FS with {_manager.ModPresetStorage.Count} ModPresets.");
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        Changed -= OnChange;
    }

    private void OnFileSystemReload(GSModule module)
    {
        if (module is GSModule.ModPreset) 
            Reload();
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        // no point in this really.
        //if (type != FileSystemChangeType.Reload)
        //    _hybridSaver.Save(this);
    }

    public bool FindLeaf(ModPresetContainer modPreset, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<ModPresetContainer>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == modPreset);
        return leaf != null;
    }

    private void OnModPresetChange(StorageChangeType type, ModPresetContainer modPreset, string? oldString)
    {
        switch (type)
        {
            case StorageChangeType.Created:
                var parent = Root;
                if (oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Bagagwa ex) { _logger.LogWarning(ex, $"Could not move modPreset because the folder could not be created."); }

                CreateDuplicateLeaf(parent, modPreset.ModName, modPreset);
                return;
            case StorageChangeType.Deleted:
                if (FindLeaf(modPreset, out var leaf1))
                    Delete(leaf1);
                return;

            case StorageChangeType.Modified:
                // need to run checks for type changes and modifications.
                if (!FindLeaf(modPreset, out var existingLeaf))
                    return;
                // Detect potential renames.
                if (existingLeaf.Name != modPreset.ModName)
                    RenameWithDuplicates(existingLeaf, modPreset.ModName);
                return;

            case StorageChangeType.Renamed when oldString != null:
                if (!FindLeaf(modPreset, out var leaf2))
                    return;
                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, modPreset.ModName);
                return;
        }
    }

    // Used for saving and loading.
    private static string ModPresetToIdentifier(ModPresetContainer modPreset)
        => modPreset.DirectoryPath.ToString();

    private static string ModPresetToName(ModPresetContainer modPreset)
        => modPreset.ModName.FixName();
}

