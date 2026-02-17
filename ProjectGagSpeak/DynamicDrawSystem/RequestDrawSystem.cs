using CkCommons.DrawSystem;
using CkCommons.HybridSaver;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.DrawSystem;

public sealed class RequestsDrawSystem : DynamicDrawSystem<RequestEntry>, IMediatorSubscriber, IDisposable, IHybridSavable
{
    private readonly ILogger<RequestsDrawSystem> _logger;
    private readonly RequestsManager _requests;
    private readonly HybridSaveService _hybridSaver;

    public GagspeakMediator Mediator { get; }

    public RequestsDrawSystem(ILogger<RequestsDrawSystem> logger, GagspeakMediator mediator,
        RequestsManager requests, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _requests = requests;
        _hybridSaver = saver;

        // Load the hierarchy and initialize the folders.
        LoadData();

        Mediator.Subscribe<FolderUpdateRequests>(this, _ => UpdateFolders());

        DDSChanged += OnChange;
        CollectionUpdated += OnCollectionUpdate;
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        DDSChanged -= OnChange;
        CollectionUpdated -= OnCollectionUpdate;
    }

    private void OnChange(DDSChange type, IDynamicNode<RequestEntry> obj, IDynamicCollection<RequestEntry>? _, IDynamicCollection<RequestEntry>? __)
    {
        if (type is not (DDSChange.FullReloadStarting or DDSChange.FullReloadFinished))
        {
            _logger.LogInformation($"DDS Change [{type}] for node [{obj.Name} ({obj.FullPath})] occured. Saving Config.");
            _hybridSaver.Save(this);
        }
    }

    private void OnCollectionUpdate(CollectionUpdate kind, IDynamicCollection<RequestEntry> collection, IEnumerable<DynamicLeaf<RequestEntry>>? _)
    {
        if (kind is CollectionUpdate.OpenStateChange)
            _hybridSaver.Save(this);
    }

    private void LoadData()
    {
        // If any changes occurred, re-save the file.
        if (LoadFile(new FileInfo(_hybridSaver.FileNames.DDS_Requests)))
        {
            _logger.LogInformation("RequestsDrawSystem folder structure changed on load, saving updated structure.");
            _hybridSaver.Save(this);
        }
        // See if the file doesnt exist, if it does not, load defaults.
        else if (!File.Exists(_hybridSaver.FileNames.DDS_Requests))
        {
            _logger.LogInformation("Loading Defaults and saving.");
            EnsureAllFolders(new Dictionary<string, string>());
            _hybridSaver.Save(this);
        }
    }

    // We dont care about the icons since we won't be showing them.
    protected override bool EnsureAllFolders(Dictionary<string, string> _)
    {
        bool anyAdded = false;
        anyAdded |= AddFolder(new RequestFolder(root, idCounter + 1u, FAI.Inbox, Constants.FolderTagRequestInc, () => _requests.Incoming, [ByTime]));
        anyAdded |= AddFolder(new RequestFolder(root, idCounter + 1u, FAI.Inbox, Constants.FolderTagRequestPending, () => _requests.Outgoing, [ByTime]));
        return anyAdded;
    }

    private static readonly ISortMethod<DynamicLeaf<RequestEntry>> ByTime = new SorterEx.ByRequestTime();

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.DDS_Requests).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer);
}

