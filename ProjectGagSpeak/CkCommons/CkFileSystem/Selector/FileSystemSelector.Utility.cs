using Dalamud.Plugin.Services;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage>
{
    private readonly IKeyState _keyState;

    /// <summary> Some actions should not be done during due to changed collections </summary>
    /// <remarks> Should also not be done on dependancy on ImGui ID's </remarks>
    protected void EnqueueFsAction(Action action)
        => _fsActions.Enqueue(action);

    /// <summary> The Queue of Actions to be executed. </summary>
    private readonly Queue<Action> _fsActions = new();

    /// <summary> Execute all collected actions in the queue </summary>
    /// <remarks> called after creating the selector, but before starting the draw iteration. </remarks>
    private void HandleActions()
    {
        while (_fsActions.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
            }
        }
    }

    /// <summary> Used for buttons and context menu entries. </summary>
    private static void RemovePrioritizedDelegate<TDelegate>(List<(TDelegate, int)> list, TDelegate action) where TDelegate : Delegate
    {
        var idxAction = list.FindIndex(p => p.Item1 == action);
        if (idxAction >= 0)
            list.RemoveAt(idxAction);
    }

    /// <summary> Used for buttons and context menu entries. </summary>
    private static void AddPrioritizedDelegate<TDelegate>(List<(TDelegate, int)> list, TDelegate action, int priority)
        where TDelegate : Delegate
    {
        var idxAction = list.FindIndex(p => p.Item1 == action);
        if (idxAction >= 0)
        {
            if (list[idxAction].Item2 == priority)
                return;

            list.RemoveAt(idxAction);
        }

        var idx = list.FindIndex(p => p.Item2 > priority);
        if (idx < 0)
            list.Add((action, priority));
        else
            list.Insert(idx, (action, priority));
    }

    /// <summary> Set the expansion state of a specific folder and all its descendant folders to the given value.
    /// <para> Can only be executed from the main selector window due to ID computation, so use this only in Enqueued actions. </para>
    /// </summary>
    /// <param name="folder"> The folder to toggle the descendants of. </param>
    /// <param name="stateIdx"> the current index in the statestruct cache list. </param>
    /// <param name="open"> if the folder is expanded or not. </param>
    /// <remarks> Handles ImGui-state as well as cache-state. </remarks>
    private void ToggleDescendants(FileSystem<T>.Folder folder, int stateIdx, bool open)
    {
        folder.UpdateState(open);
        RemoveDescendants(stateIdx);
        foreach (var child in folder.GetAllDescendants(ISortMode<T>.Lexicographical).OfType<FileSystem<T>.Folder>())
            child.UpdateState(open);

        if (open)
            AddDescendants(folder, stateIdx);
    }

    /// <summary> Expand all ancestors of a given path, used for when new objects are created. </summary>
    /// <param name="path"> The Path to expand all its ancestors from. </param>
    /// <returns> If any state was changed. </returns>
    /// <remarks> Can only be executed from the main selector window due to ID computation. Handles only ImGui-state. </remarks>
    private bool ExpandAncestors(FileSystem<T>.IPath path)
    {
        if (path.IsRoot || path.Parent.IsRoot)
            return false;

        var parent  = path.Parent;
        var changes = false;
        while (!parent.IsRoot)
        {
            changes |= !parent.State;
            parent.UpdateState(true);
            parent = parent.Parent;
        }

        return changes;
    }
}
