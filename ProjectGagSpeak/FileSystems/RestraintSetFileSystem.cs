using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GagSpeak.FileSystems;

public sealed class RestraintSetFileSystem : CkFileSystem<RestraintSet>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<RestraintSetFileSystem> _logger;
    private readonly RestraintManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public RestraintSetFileSystem(ILogger<RestraintSetFileSystem> logger, GagspeakMediator mediator,
        RestraintManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigRestraintSetChanged>(this, (msg) => OnRestraintSetChange(msg.Type, msg.Item, msg.OldString));
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.SortFilers), _manager.Storage, RestraintSetToIdentifier, RestraintSetToName))
            _hybridSaver.Save(this);

        _logger.LogDebug("Reloaded restraintSets filesystem.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigRestraintSetChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(RestraintSet restraintSet, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<RestraintSet>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == restraintSet);
        return leaf != null;
    }

    private void OnRestraintSetChange(StorageItemChangeType type, RestraintSet restraintSet, string? oldString)
    {
        switch (type)
        {
            case StorageItemChangeType.Created:
                var parent = Root;
                if(oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Could not move restraintSet because the folder could not be created."); }

                CreateDuplicateLeaf(parent, restraintSet.Label, restraintSet);
                return;
            case StorageItemChangeType.Deleted:
                if (FindLeaf(restraintSet, out var leaf1))
                    Delete(leaf1);
                return;
            case StorageItemChangeType.Modified:
                Reload();
                return;
            case StorageItemChangeType.Renamed when oldString != null:
                if (!FindLeaf(restraintSet, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, restraintSet.Label);
                return;
        }
    }

    // Used for saving and loading.
    private static string RestraintSetToIdentifier(RestraintSet restraintSet)
        => restraintSet.Identifier.ToString();

    private static string RestraintSetToName(RestraintSet restraintSet)
        => restraintSet.Label.FixName();

    private static bool RestraintSetHasDefaultPath(RestraintSet restraintSet, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(RestraintSetToName(restraintSet))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveRestraintSet(RestraintSet restraintSet, string fullPath)
        // Only save pairs with non-default paths.
        => RestraintSetHasDefaultPath(restraintSet, fullPath)
            ? (string.Empty, false)
            : (RestraintSetToIdentifier(restraintSet), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.SortFilers).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveRestraintSet, true);
}

