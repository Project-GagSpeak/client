using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using OtterGui.Filesystem;
using ProjectGagSpeak.CkCommons.HybridSaver;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GagSpeak.FileSystems;

public sealed class AlarmFileSystem : FileSystem<Alarm>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<AlarmFileSystem> _logger;
    private readonly AlarmManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public AlarmFileSystem(ILogger<AlarmFileSystem> logger, GagspeakMediator mediator, 
        AlarmManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigAlarmChanged>(this, (msg) => OnAlarmChange(msg.Type, msg.Item, msg.OldString));
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.SortFilers), _manager.Storage, AlarmToIdentifier, AlarmToName))
            _hybridSaver.Save(this);

        _logger.LogDebug("Reloaded alarms filesystem.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigAlarmChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(Alarm alarm, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Alarm>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == alarm);
        return leaf != null;
    }

    private void OnAlarmChange(StorageItemChangeType type, Alarm alarm, string? oldString)
    {
        switch (type)
        {
            case StorageItemChangeType.Created:
                var parent = Root;
                if(oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Could not move alarm because the folder could not be created."); }

                CreateDuplicateLeaf(parent, alarm.Label, alarm);
                return;
            case StorageItemChangeType.Deleted:
                if (FindLeaf(alarm, out var leaf1))
                    Delete(leaf1);
                return;
            case StorageItemChangeType.Modified:
                Reload();
                return;
            case StorageItemChangeType.Renamed when oldString != null:
                if (!FindLeaf(alarm, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, alarm.Label);
                return;
        }
    }

    // Used for saving and loading.
    private static string AlarmToIdentifier(Alarm alarm)
        => alarm.Identifier.ToString();

    private static string AlarmToName(Alarm alarm)
        => alarm.Label.FixName();

    private static bool AlarmHasDefaultPath(Alarm alarm, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(AlarmToName(alarm))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveAlarm(Alarm alarm, string fullPath)
        // Only save pairs with non-default paths.
        => AlarmHasDefaultPath(alarm, fullPath)
            ? (string.Empty, false)
            : (AlarmToIdentifier(alarm), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.SortFilers).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SaveAlarm, true);
}

