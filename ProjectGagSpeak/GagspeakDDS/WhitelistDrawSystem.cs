using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.HybridSaver;
using GagSpeak.Kinksters;
using GagSpeak.Pairs;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.User;

namespace GagSpeak.DrawSystem;

public class WhitelistDrawSystem : DynamicDrawSystem<Kinkster>, IMediatorSubscriber, IDisposable, IHybridSavable
{
    private readonly ILogger<WhitelistDrawSystem> _logger;
    private readonly MainConfig _config;
    private readonly SorterHelpers _sortHelpers;
    private readonly KinksterManager _kinksters;
    private readonly OnlineKinksterManager _onlineUsers;
    private readonly HybridSaveService _hybridSaver;
    private readonly PairService _pairService;

    private readonly object _updateLock = new();


    public GagspeakMediator Mediator { get; init; }

    public WhitelistDrawSystem(ILogger<WhitelistDrawSystem> logger, GagspeakMediator mediator,
        MainConfig config, SorterHelpers sortHelpers, KinksterManager kinksters,
        OnlineKinksterManager onlineUsers, HybridSaveService saver, PairService pairService)
    {
        _logger = logger;
        Mediator = mediator;
        _config = config;
        _sortHelpers = sortHelpers;
        _kinksters = kinksters;
        _onlineUsers = onlineUsers;
        _hybridSaver = saver;
        _pairService = pairService;

        // Load the hierarchy and initialize the folders.
        LoadData();

        _onlineUsers.UserWentOnline += OnUserStatusChanged;
        _onlineUsers.UserWentOffline += OnUserStatusChanged;

        Mediator.Subscribe<DDSUpdateKinkster>(this, _ => RequestUpdateAll());
        Mediator.Subscribe<KinksterRendered>(this, _ => RequestUpdateVisible());
        Mediator.Subscribe<KinksterUnrendered>(this, _ => RequestUpdateVisible());
        Mediator.Subscribe<ConnectedMessage>(this, _ => RequestUpdateAll());

        DDSChanged += OnChange;
        CollectionUpdated += OnCollectionUpdate;
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        _onlineUsers.UserWentOnline -= OnUserStatusChanged;
        _onlineUsers.UserWentOffline -= OnUserStatusChanged;
        DDSChanged -= OnChange;
        CollectionUpdated -= OnCollectionUpdate;
    }

    #region Events
    private void OnUserStatusChanged(UserData _, string __)
        => RequestUpdateAll();

    internal void RequestUpdateAll()
    {
        lock (_updateLock)
            UpdateFolders();
    }

    internal void RequestUpdateVisible()
    {
        lock (_updateLock)
            UpdateFolder(Consts.DDS_Rendered);
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
    #endregion

    private void LoadData()
    {
        SetSortDirection(root, true); // Visible->Online->Offline

        if (LoadFile(new FileInfo(_hybridSaver.FileNames.DDS_Whitelist)))
        {
            _logger.LogInformation("WhitelistDrawSystem folder structure changed on load, saving updated structure.");
            _hybridSaver.Save(this);
        }
        else if (!File.Exists(_hybridSaver.FileNames.DDS_Whitelist))
        {
            _logger.LogInformation("Loading Defaults and saving.");
            EnsureAllFolders(new Dictionary<string, string>());
            _hybridSaver.Save(this);
        }
    }

    #region Folder Management
    protected override bool EnsureAllFolders(Dictionary<string, string> _)
    {
        var anyChanged = UpdateVisibleFolderState(_config.Data.VisibleFolder)
                       | UpdateOfflineFolderState(_config.Data.OfflineFolder);
        _logger.LogDebug($"Ensured all folders, total now {FolderMap.Count} folders.", LoggerType.UI);
        return anyChanged;
    }

    public bool UpdateVisibleFolderState(bool showFolder)
    {
        if (showFolder)
        {
            if (FolderMap.ContainsKey(Consts.DDS_Rendered)) return false;

            return AddFolder(CreateDefaultFolder(
                Consts.DDS_Rendered, FAI.Eye, CkCol.TriStateCheck.Uint(),
                GetVisible, () => _config.Data.WhitelistSortOrderVisible));
        }
        return Delete(Consts.DDS_Rendered);
    }

    public bool UpdateOfflineFolderState(bool showFolder)
    {
        var anyChanges = false;
        if (showFolder)
        {
            anyChanges |= Delete(Consts.DDS_All);

            anyChanges |= AddFolder(CreateDefaultFolder(
                Consts.DDS_Online, FAI.Link, CkCol.TriStateCheck.Uint(),
                GetOnline, () => _config.Data.WhitelistSortOrderOnline));

            anyChanges |= AddFolder(CreateDefaultFolder(
                Consts.DDS_Offline, FAI.Link, CkCol.TriStateCross.Uint(),
                GetOffline, () => _config.Data.WhitelistSortOrderOffline));
        }
        else
        {
            anyChanges |= Delete(Consts.DDS_Online);
            anyChanges |= Delete(Consts.DDS_Offline);

            anyChanges |= AddFolder(CreateDefaultFolder(
                Consts.DDS_All, FAI.Globe, uint.MaxValue,
                GetAllUsers, () => _config.Data.WhitelistSortOrderAll));
        }
        return anyChanges;
    }

    private PairFolder CreateDefaultFolder(string tag, FAI icon, uint color, Func<List<Kinkster>> fetcher, Func<List<FolderSortFilter>> sortOrder)
        => new(_sortHelpers, _config, root, idCounter + 1u, icon, tag, color, fetcher, sortOrder);

    public List<Kinkster> GetVisible()
        => _kinksters.DirectPairs.Where(u => u.IsRendered && u.IsOnline).ToList();

    public List<Kinkster> GetOnline()
        => _kinksters.DirectPairs.Where(u => u.IsOnline).ToList();

    public List<Kinkster> GetOffline()
        => _kinksters.DirectPairs.Where(u => !u.IsOnline).ToList();

    public List<Kinkster> GetAllUsers()
        => _kinksters.DirectPairs;
    #endregion

    // HybridSavable
    public int ConfigVersion => 0;
    public int MaxBackups => 2;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC => DateTime.MinValue;
    public string ToFilePath(GsFiles files) => files.DDS_Whitelist;
    public string JsonSerialize() => throw new NotImplementedException();
    public void WriteToStream(StreamWriter writer) => SaveToFile(writer);
}
