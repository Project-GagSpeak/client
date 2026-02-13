using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.HybridSaver;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;

namespace GagSpeak.DrawSystem;

// Seperates Puppeteers from Non-Puppeteers
public class PuppeteersDrawSystem : DynamicDrawSystem<Kinkster>, IMediatorSubscriber, IDisposable, IHybridSavable
{
    private readonly ILogger<PuppeteersDrawSystem> _logger;
    private readonly PuppeteerManager _puppeteer;
    private readonly KinksterManager _kinksters;
    private readonly HybridSaveService _hybridSaver;

    public GagspeakMediator Mediator { get; init; }

    public PuppeteersDrawSystem(ILogger<PuppeteersDrawSystem> logger, GagspeakMediator mediator,
        PuppeteerManager puppeteer, KinksterManager kinksters, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _puppeteer = puppeteer;
        _kinksters = kinksters;
        _hybridSaver = saver;

        // Load the hierarchy and initialize the folders.
        LoadData();

        Mediator.Subscribe<FolderUpdateKinkster>(this, _ => UpdateFolders());

        DDSChanged += OnChange;
        CollectionUpdated += OnCollectionUpdate;
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        DDSChanged -= OnChange;
        CollectionUpdated -= OnCollectionUpdate;
    }

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
        if (LoadFile(new FileInfo(_hybridSaver.FileNames.DDS_Puppeteers)))
        {
            _logger.LogInformation("PuppeteersDrawSystem folder structure changed on load, saving updated structure.");
            _hybridSaver.Save(this);
        }
        // See if the file doesnt exist, if it does not, load defaults.
        else if (!File.Exists(_hybridSaver.FileNames.DDS_Puppeteers))
        {
            _logger.LogInformation("Loading Defaults and saving.");
            EnsureAllFolders(new Dictionary<string, string>());
            _hybridSaver.Save(this);
        }
    }

    protected override bool EnsureAllFolders(Dictionary<string, string> _)
    {
        bool anyChanged = false;
        // Ensure Puppeteers Folder
        if (!FolderMap.ContainsKey(Constants.FolderTagPuppeteers))
            anyChanged |= AddFolder(new PairFolder(root, idCounter + 1u, FAI.None, Constants.FolderTagPuppeteers, uint.MaxValue,
                () => [ .. _kinksters.DirectPairs.Where(p => _puppeteer.Puppeteers.ContainsKey(p.UserData.UID)).ToList()], [SorterEx.ByFavorite, SorterEx.ByPairName]));
        // Ensure Other Folder.
        if (!FolderMap.ContainsKey(Constants.FolderTagNonPuppeteers))
            anyChanged |= AddFolder(new PairFolder(root, idCounter + 1u, FAI.None, Constants.FolderTagNonPuppeteers, uint.MaxValue,
                () => [.. _kinksters.DirectPairs.Where(p => !_puppeteer.Puppeteers.ContainsKey(p.UserData.UID)).ToList()], [SorterEx.ByFavorite, SorterEx.ByPairName]));

        return anyChanged;
    }

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.DDS_Puppeteers).Item2;

    public string JsonSerialize()
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer)
        => SaveToFile(writer);
}
