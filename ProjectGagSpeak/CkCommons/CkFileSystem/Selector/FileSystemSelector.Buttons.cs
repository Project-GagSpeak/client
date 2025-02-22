using Dalamud.Interface;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class CkFileSystemSelector<T, TStateStorage>
{
    /// <summary> Add a button to the bottom-list. Should be an object that does not exceed the size parameter. </summary>
    /// <remarks> Buttons are sorted from left to right on priority, then subscription order. </remarks>
    public void AddButton(Action<Vector2> action, int priority)
        => AddPrioritizedDelegate(_buttons, action, priority);

    /// <summary> Remove a button from the bottom-list by reference equality. </summary>
    public void RemoveButton(Action<Vector2> action)
        => RemovePrioritizedDelegate(_buttons, action);

    /// <summary> List sorted on priority, then subscription order. </summary>
    private readonly List<(Action<Vector2>, int)> _buttons = new(1);


    /// <summary> Draw all subscribed buttons. </summary>
    private void DrawButtons(float width)
    {
        var buttonWidth = new Vector2(width / Math.Max(_buttons.Count, 1), ImGui.GetFrameHeight());
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0f)
            .Push(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        foreach (var button in _buttons)
        {
            button.Item1.Invoke(buttonWidth);
            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    /// <summary> Draw necessary popups from buttons outside of pushed styles. </summary>
    protected virtual void DrawPopups() { }

    /// <summary> Protected so it can be removed. </summary>
    protected void FolderAddButton(Vector2 size)
    {
        const string newFolderName = "folderName";

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.FolderPlus.ToIconString(), size,
                "Create a new, empty folder. Can contain '/' to create a directory structure.", false, true))
            ImGui.OpenPopup(newFolderName);

        // Does not need to be delayed since it is not in the iteration itself.
        CkFileSystem<T>.Folder? folder = null;
        if (ImGuiUtil.OpenNameField(newFolderName, ref _newName) && _newName.Length > 0)
            try
            {
                folder   = CkFileSystem.FindOrCreateAllFolders(_newName);
                _newName = string.Empty;
            }
            catch
            {
                // Ignored
            }

        if (folder != null)
            _filterDirty |= ExpandAncestors(folder);
    }

    protected void DeleteSelectionButton(Vector2 size, DoubleModifier modifier, string singular, string plural, Action<T> delete)
    {
        var keys        = modifier.IsActive();
        var anySelected = _selectedPaths.Count > 1 || SelectedLeaf != null;
        var name        = _selectedPaths.Count > 1 ? plural : singular;
        var tt = !anySelected
            ? $"No {plural} selected."
            : $"Delete the currently selected {name} entirely from your drive.\n"
          + "This can not be undone.";
        if (!keys)
            tt += $"\nHold {modifier} while clicking to delete the {name}.";

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), size, tt, !anySelected || !keys, true))
        {
            if (Selected != null)
                delete(Selected);
            else
                foreach (var leaf in _selectedPaths.OfType<CkFileSystem<T>.Leaf>())
                    delete(leaf.Value);
        }
    }

    private void InitDefaultButtons()
    {
        AddButton(FolderAddButton, 50);
    }
}
