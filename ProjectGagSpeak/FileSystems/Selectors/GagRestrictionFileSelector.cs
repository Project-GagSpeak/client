using Dalamud.Plugin.Services;
using GagSpeak.CkCommons.FileSystem.Selector;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class GagRestrictionFileSelector : CkFileSystemSelector<GarblerRestriction, GagRestrictionFileSelector.GagRestrictionState>, IMediatorSubscriber, IDisposable
{
    private readonly GagRestrictionManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct GagRestrictionState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new GagFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public GagRestrictionFileSelector(ILogger<GagRestrictionFileSelector> log, GagspeakMediator mediator, GagRestrictionManager manager,
        GagFileSystem fileSystem, IKeyState keys) : base(fileSystem, log, keys, "##GarblerFileSelector")
    {
        Mediator = mediator;
        _manager = manager;

        Mediator.Subscribe<ConfigGagRestrictionChanged>(this, (msg) => OnGagRestrictionChange(msg.Type, msg.Item, msg.OldString));

        // we can add, or unsubscribe from buttons here. Remember this down the line, it will become useful.
        UnsubscribeRightClickLeaf(RenameLeaf);
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.Unsubscribe<ConfigGagRestrictionChanged>(this);
    }

    // can override the selector here to mark the last selected set in the config or something somewhere.

    // if desired, can override the DrawLeafName and DrawFolderNames

    // if desired, can override the colors for expanded, collapsed, and folder line colors.
    // Can also define if the folders are open by default or not.

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnGagRestrictionChange(StorageItemChangeType type, GarblerRestriction gagRestriction, string? oldString)
        => SetFilterDirty();


    // Any custom popups or buttons can be setup here.

    // any custom filters, if any, can be setup here, though they should likely be removed as
    // they should end up embedded within the custom filter applier inside the file system later on.

    // If you need help understanding more about this reference Glamourer and Penumbra again.
}

