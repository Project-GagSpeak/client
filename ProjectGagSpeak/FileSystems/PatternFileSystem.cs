using GagSpeak.CkCommons.FileSystem;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GagSpeak.FileSystems;

public sealed class PatternFileSystem : CkFileSystem<Pattern>, IMediatorSubscriber, IHybridSavable, IDisposable
{
    private readonly ILogger<PatternFileSystem> _logger;
    private readonly PatternManager _manager;
    private readonly HybridSaveService _hybridSaver;
    public GagspeakMediator Mediator { get; init; }
    public PatternFileSystem(ILogger<PatternFileSystem> logger, GagspeakMediator mediator, 
        PatternManager manager, HybridSaveService saver)
    {
        _logger = logger;
        Mediator = mediator;
        _manager = manager;
        _hybridSaver = saver;

        Mediator.Subscribe<ConfigPatternChanged>(this, (msg) => OnPatternChange(msg.Type, msg.Item, msg.OldString));
        Mediator.Subscribe<ReloadFileSystem>(this, (msg) => { if (msg.Module is Module.Pattern) Reload(); });
        Changed += OnChange;
        Reload();
    }

    private void Reload()
    {
        if (Load(new FileInfo(_hybridSaver.FileNames.CKFS_Patterns), _manager.Storage, PatternToIdentifier, PatternToName))
            _hybridSaver.Save(this);

        _logger.LogDebug("Reloaded patterns filesystem with " + _manager.Storage.Count + " patterns.");
    }

    public void Dispose()
    {
        Mediator.Unsubscribe<ConfigPatternChanged>(this);
        Changed -= OnChange;
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _hybridSaver.Save(this);
    }

    public bool FindLeaf(Pattern pattern, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Pattern>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == pattern);
        return leaf != null;
    }

    private void OnPatternChange(StorageItemChangeType type, Pattern pattern, string? oldString)
    {
        switch (type)
        {
            case StorageItemChangeType.Created:
                var parent = Root;
                if(oldString != null)
                    try { parent = FindOrCreateAllFolders(oldString); }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Could not move pattern because the folder could not be created."); }

                CreateDuplicateLeaf(parent, pattern.Label, pattern);
                return;
            case StorageItemChangeType.Deleted:
                if (FindLeaf(pattern, out var leaf1))
                    Delete(leaf1);
                return;
            case StorageItemChangeType.Modified:
                Reload();
                return;
            case StorageItemChangeType.Renamed when oldString != null:
                if (!FindLeaf(pattern, out var leaf2))
                    return;

                var old = oldString.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, pattern.Label);
                return;
        }
    }

    // Used for saving and loading.
    private static string PatternToIdentifier(Pattern pattern)
        => pattern.Identifier.ToString();

    private static string PatternToName(Pattern pattern)
        => pattern.Label.FixName();

    private static bool PatternHasDefaultPath(Pattern pattern, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(PatternToName(pattern))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SavePattern(Pattern pattern, string fullPath)
        // Only save pairs with non-default paths.
        => PatternHasDefaultPath(pattern, fullPath)
            ? (string.Empty, false)
            : (PatternToIdentifier(pattern), true);

    // HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CKFS_Patterns).Item2;

    public string JsonSerialize() 
        => throw new NotImplementedException();

    public void WriteToStream(StreamWriter writer) 
        => SaveToFile(writer, SavePattern, true);
}

