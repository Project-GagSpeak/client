using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.HybridSaver;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.DrawSystem;

public class WhitelistDrawSystem : DynamicDrawSystem<Kinkster>, IMediatorSubscriber, IDisposable, IHybridSavable
{
    private readonly ILogger<WhitelistDrawSystem> _logger;
    private readonly MainConfig _config;
    private readonly KinksterManager _kinksters;
    private readonly HybridSaveService _hybridSaver;

    public GagspeakMediator Mediator { get; init; }

    public WhitelistDrawSystem(ILogger<WhitelistDrawSystem> logger, GagspeakMediator mediator,
        MainConfig config, KinksterManager kinksters, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _config = config;
        _kinksters = kinksters;
        _hybridSaver = saver;

        // Load the hierarchy and initialize the folders.
        LoadData();

        Mediator.Subscribe<FolderUpdateKinkster>(this, _ => UpdateFolders());
        Mediator.Subscribe<KinksterPlayerRendered>(this, _ => UpdateFolder(Constants.FolderTagVisible));
        Mediator.Subscribe<KinksterPlayerUnrendered>(this, _ => UpdateFolder(Constants.FolderTagVisible));

        // Subscribe to the changes (which is to change very, very soon, with overrides.
        DDSChanged += OnChange;
        CollectionUpdated += OnCollectionUpdate;
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        DDSChanged -= OnChange;
        CollectionUpdated -= OnCollectionUpdate;
    }

    // Note that this will change very soon, as saves should only occur for certain changes.
    private void OnChange(DDSChange type, IDynamicNode<Kinkster> obj, IDynamicCollection<Kinkster>? _, IDynamicCollection<Kinkster>? __)
    {
        if (type is not (DDSChange.FullReloadStarting or DDSChange.FullReloadFinished))
        {
            _logger.LogInformation($"DDS Change [{type}] for node [{obj.Name} ({obj.FullPath})] occured. Saving Config.");
            _hybridSaver.Save(this);
        }
    }

    private void OnCollectionUpdate(CollectionUpdate kind, IDynamicCollection<Kinkster> collection, IEnumerable<DynamicLeaf<Kinkster>>? _)
    {
        if (kind is CollectionUpdate.OpenStateChange)
            _hybridSaver.Save(this);
    }

    private void LoadData()
    {
        // Before we load anything, inverse the sort direction of root.
        SetSortDirection(root, true);
        // If any changes occured, re-save the file.
        if (LoadFile(new FileInfo(_hybridSaver.FileNames.DDS_Whitelist)))
        {
            _logger.LogInformation("WhitelistDrawSystem folder structure changed on load, saving updated structure.");
            _hybridSaver.Save(this);
        }
    }

    protected override bool EnsureAllFolders(Dictionary<string, string> _)
    {
        // Load in the folders, they are all descendants of root.
        bool anyChanged = false;
        anyChanged |= UpdateVisibleFolderState(_config.Current.VisibleFolder);
        anyChanged |= UpdateOfflineFolderState(_config.Current.OfflineFolder);
        _logger.LogInformation($"Ensured all folders, total now {FolderMap.Count} folders.");
        return anyChanged;
    }

    // Update the FolderSystem folders based on if it should be included or not.
    public bool UpdateVisibleFolderState(bool showFolder)
    {
        // If we want to show the folder and it already exists then change nothing.
        if (showFolder)
        {
            if (FolderMap.ContainsKey(Constants.FolderTagVisible))
                return false;
            // Try to add it.
            return TryAdd(FAI.Eye, Constants.FolderTagVisible, CkCol.TriStateCheck.Uint(), () => [.. _kinksters.DirectPairs.Where(u => u.IsRendered && u.IsOnline)]);
        }
        // Otherwise attempt to remove it.
        return Delete(Constants.FolderTagVisible);
    }

    // Not too worried about additional work here since it only happens on recalculations.
    public bool UpdateOfflineFolderState(bool showFolder)
    {
        // Assume no changes.
        bool anyChanges = false;
        // If we wanted to show offline/online..
        if (showFolder)
        {
            anyChanges |= Delete(Constants.FolderTagAll);
            anyChanges |= TryAdd(FAI.Link, Constants.FolderTagOnline, CkCol.TriStateCheck.Uint(), () => [.. _kinksters.DirectPairs.Where(s => s.IsOnline)]);
            anyChanges |= TryAdd(FAI.Link, Constants.FolderTagOffline, CkCol.TriStateCross.Uint(), () => [.. _kinksters.DirectPairs.Where(s => !s.IsOnline)]);
        }
        // Otherwise we wanted to only show ALL.
        else
        {
            anyChanges |= Delete(Constants.FolderTagOnline);
            anyChanges |= Delete(Constants.FolderTagOffline);
            anyChanges |= AddFolder(new PairFolder(root, idCounter + 1u, FAI.Globe, Constants.FolderTagAll,
                                        uint.MaxValue, () => _kinksters.DirectPairs, SorterEx.AllFolderSorter));
        }
        // Return if anything was modified.
        return anyChanges;
    }

    private bool TryAdd(FAI icon, string name, uint iconColor, Func<IReadOnlyList<Kinkster>> generator)
        => AddFolder(new PairFolder(root, idCounter + 1u, icon, name, iconColor, generator, [SorterEx.ByPairName]));

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.DDS_Whitelist).Item2;

    public string JsonSerialize()
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer)
        => SaveToFile(writer);
}
