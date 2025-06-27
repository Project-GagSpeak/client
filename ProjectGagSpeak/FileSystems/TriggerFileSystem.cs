using CkCommons.FileSystem;
using CkCommons.HybridSaver;
using GagSpeak.State.Models;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GagSpeak.State.Managers;

namespace GagSpeak.FileSystems;

public sealed class TriggerFileSystem : CkFileSystem<Trigger>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<TriggerFileSystem> _logger;
    private readonly TriggerManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public TriggerFileSystem(ILogger<TriggerFileSystem> logger, GagspeakMediator mediator,
        TriggerManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigTriggerChanged>(this, (msg) => OnTriggerChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is GagspeakModule.Trigger) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_Triggers), _manager.Storage, TriggerToIdentifier, TriggerToName))
            _hybridSaver.Save(this);


        _logger.LogDebug("Reloaded triggers filesystem with " + _manager.Storage.Count + " triggers.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigTriggerChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(Trigger trigger, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Trigger>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == trigger);
        return leaf != null;
    }

    private void OnTriggerChange(StorageChangeType type, Trigger trigger, string? oldString)
    {
        switch (type)
        {
            case StorageChangeType.Created:
                var parent = Root;
                if(oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Could not move trigger because the folder could not be created."); }

                CreateDuplicateLeaf(parent, trigger.Label, trigger);
                return;
            case StorageChangeType.Deleted:
                if (FindLeaf(trigger, out var leaf1))
                    Delete(leaf1);
                return;
            case StorageChangeType.Modified:
                Reload();
                return;
            case StorageChangeType.Renamed when oldString != null:
                if (!FindLeaf(trigger, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, trigger.Label);
                return;
        }
    }

    // Used for saving and loading.
    private static string TriggerToIdentifier(Trigger trigger)
        => trigger.Identifier.ToString();

    private static string TriggerToName(Trigger trigger)
        => trigger.Label.FixName();

    private static bool TriggerHasDefaultPath(Trigger trigger, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(TriggerToName(trigger))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveTrigger(Trigger trigger, string fullPath)
        // Only save pairs with non-default paths.
        => TriggerHasDefaultPath(trigger, fullPath)
            ? (string.Empty, false)
            : (TriggerToIdentifier(trigger), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_Triggers).Item2;

    public string JsonSerialize()
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer)
        => SaveToFile(writer, SaveTrigger, true);
}

