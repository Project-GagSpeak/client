using Dalamud.Plugin.Services;
using GagSpeak.CkCommons.FileSystem.Selector;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Mediator;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.FileSystems;

// Continue reworking this to integrate a combined approach if we can figure out a better file management system.
public sealed class PatternFileSelector : CkFileSystemSelector<Pattern, PatternFileSelector.PatternState>, IMediatorSubscriber, IDisposable
{
    private readonly PatternManager _manager;
    public GagspeakMediator Mediator { get; init; }

    /// <summary> 
    /// For now, use this 'state storage', it is a list of attributes linked to each leaf.
    /// To be honest im not sure why to not just access this from the path item directly during the draw, but whatever.
    /// We will find out later if anything.
    /// </summary>
    /// <remarks> This allows each item in here to be accessed efficiently at runtime during the draw loop. </remarks>
    public record struct PatternState(uint Color) { }

    /// <summary> This is the currently selected leaf in the file system. </summary>
    public new PatternFileSystem.Leaf? SelectedLeaf
    => base.SelectedLeaf;

    public PatternFileSelector(PatternManager manager, GagspeakMediator mediator, PatternFileSystem fileSystem, 
        ILogger<PatternFileSelector> log, IKeyState keys) : base(fileSystem, log, keys, "##PatternFileSelector")
    {
        Mediator = mediator;
        _manager = manager;

        Mediator.Subscribe<ConfigPatternChanged>(this, (msg) => OnPatternChange(msg.Type, msg.Item, msg.OldString));

        // we can add, or unsubscribe from buttons here. Remember this down the line, it will become useful.
    }

    private void RenameLeafPattern(PatternFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        RenameLeaf(leaf);
    }

    private void RenamePattern(PatternFileSystem.Leaf leaf)
    {
        ImGui.Separator();
        var currentName = leaf.Value.Label;
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Pattern:");
        if (ImGui.InputText("##RenamePattern", ref currentName, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _manager.Rename(leaf.Value, currentName);
            ImGui.CloseCurrentPopup();
        }
        ImGuiUtil.HoverTooltip("Enter a new name here to rename the changed pattern.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Mediator.Unsubscribe<ConfigPatternChanged>(this);
    }

    // can override the selector here to mark the last selected set in the config or something somewhere.

    // if desired, can override the DrawLeafName and DrawFolderNames

    // if desired, can override the colors for expanded, collapsed, and folder line colors.
    // Can also define if the folders are open by default or not.

    /// <summary> Just set the filter to dirty regardless of what happened. </summary>
    private void OnPatternChange(StorageItemChangeType type, Pattern pattern, string? oldString)
        => SetFilterDirty();


    // Any custom popups or buttons can be setup here.

    // any custom filters, if any, can be setup here, though they should likely be removed as
    // they should end up embedded within the custom filter applier inside the file system later on.

    // If you need help understanding more about this reference Glamourer and Penumbra again.
}

