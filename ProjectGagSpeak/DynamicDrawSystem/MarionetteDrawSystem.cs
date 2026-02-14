using CkCommons.DrawSystem;
using CkCommons.HybridSaver;
using Dalamud.Bindings.ImGui;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;

namespace GagSpeak.DrawSystem;

// Draws out the alias items for a Marionette
public class MarionetteDrawSystem : DynamicDrawSystem<AliasTrigger>, IMediatorSubscriber, IDisposable, IHybridSavable
{
    private readonly ILogger<MarionetteDrawSystem> _logger;
    private readonly MainConfig _config;
    private readonly PuppeteerManager _manager;
    private readonly KinksterManager _kinksters;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }

    private PairCombo _marionetteCombo;

    public MarionetteDrawSystem(ILogger<MarionetteDrawSystem> logger, GagspeakMediator mediator,
        MainConfig config, FavoritesConfig favorites, KinksterManager kinksters, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _config = config;
        _kinksters = kinksters;
        _hybridSaver = saver;

        _marionetteCombo = new PairCombo(logger, mediator, favorites, () => [
            ..kinksters.DirectPairs
                .Where(k => k.PairPerms.IsMarionette())
                .OrderByDescending(p => FavoritesConfig.Kinksters.Contains(p.UserData.UID))
                .ThenBy(pair => pair.GetDisplayName(), StringComparer.OrdinalIgnoreCase)
        ]);

        // Load the hierarchy and initialize the folders.
        LoadData();

        // Composite updates dont call these, do its helpful to have.
        Mediator.Subscribe<PlayerLatestActiveItems>(this, _ => UpdateFolders());
        // Whenever someone goes online/offline
        Mediator.Subscribe<FolderUpdateKinkster>(this, _ => UpdateFolders());
        // A permission change occured that changes if someone could be a marionette or not.
        Mediator.Subscribe<FolderUpdateMarionettes>(this, _ => _marionetteCombo.Refresh());
        Mediator.Subscribe<FolderUpdateKinksterAliases>(this, _ =>
        {
            // Only update if matching
            if (_.Kinkster == SelectedMarionette)
                UpdateFolders();
        });

        DDSChanged += OnChange;
        CollectionUpdated += OnCollectionUpdate;
    }

    public Kinkster? SelectedMarionette { get; private set; } = null;

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        DDSChanged -= OnChange;
        CollectionUpdated -= OnCollectionUpdate;
    }

    /// <summary>
    ///     Properly draws out the Marionette selector for the given width.
    /// </summary>
    public bool DrawMarionetteCombo(float width, float scalar = 1.15f)
    {
        if (_marionetteCombo.Draw(SelectedMarionette, width, scalar))
        {
            _logger.LogInformation($"Selected Marionette: {_marionetteCombo.Current?.GetNickAliasOrUid() ?? "None"}");
            SelectedMarionette = _marionetteCombo.Current;
            UpdateFolders();
            return true;
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogInformation("Clearing selected Marionette.");
            SelectedMarionette = null;
            UpdateFolders(); 
            return true;
        }

        return false;
    }


    private void OnChange(DDSChange type, IDynamicNode<AliasTrigger> obj, IDynamicCollection<AliasTrigger>? _, IDynamicCollection<AliasTrigger>? __)
    {
        if (type is not (DDSChange.FullReloadStarting or DDSChange.FullReloadFinished))
        {
            _logger.LogInformation($"DDS Change [{type}] for node [{obj.Name} ({obj.FullPath})] occured. Saving Config.");
            _hybridSaver.Save(this);
        }
    }

    private void OnCollectionUpdate(CollectionUpdate kind, IDynamicCollection<AliasTrigger> collection, IEnumerable<DynamicLeaf<AliasTrigger>>? _)
    {
        if (kind is CollectionUpdate.OpenStateChange)
            _hybridSaver.Save(this);
    }

    private void LoadData()
    {
        // If any changes occured, re-save the file.
        if (LoadFile(new FileInfo(_hybridSaver.FileNames.DDS_Marionettes)))
        {
            _logger.LogInformation("MarionetteDrawSystem folder structure changed on load, saving updated structure.");
            _hybridSaver.Save(this);
        }
        // See if the file doesnt exist, if it does not, load defaults.
        else if (!File.Exists(_hybridSaver.FileNames.DDS_Marionettes))
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
        if (!FolderMap.ContainsKey(Constants.FolderTagAliasesActive))
            anyChanged |= AddFolder(new AliasFolder(root, idCounter + 1u, Constants.FolderTagAliasesActive, uint.MaxValue,
                () => [.. SelectedMarionette?.SharedAliases.Where(a => a.Enabled) ?? [] ]));
        // Ensure Other Folder.
        if (!FolderMap.ContainsKey(Constants.FolderTagAliasesInactive))
            anyChanged |= AddFolder(new AliasFolder(root, idCounter + 1u, Constants.FolderTagAliasesInactive, uint.MaxValue,
                () => [.. SelectedMarionette?.SharedAliases.Where(a => !a.Enabled) ?? []]));

        return anyChanged;
    }

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.DDS_Marionettes).Item2;

    public string JsonSerialize()
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer)
        => SaveToFile(writer);
}
