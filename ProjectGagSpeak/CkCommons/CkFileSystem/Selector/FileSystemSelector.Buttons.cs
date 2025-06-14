using Dalamud.Interface;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Text;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class CkFileSystemSelector<T, TStateStorage>
{
    /// <summary> Draw necessary popups from buttons outside of pushed styles. </summary>
    protected virtual void DrawPopups() { }

    /// <summary> Protected so it can be removed. </summary>
    protected void FolderAddButton()
    {
        const string newFolderName = "folderName";

        if (CkGui.IconButton(FAI.FolderPlus))
            ImGui.OpenPopup(newFolderName);
        ImUtf8.HoverTooltip("Create a new, empty folder. Can contain '/' to create a directory structure."u8);

        // Does not need to be delayed since it is not in the iteration itself.
        CkFileSystem<T>.Folder? folder = null;
        if (ImGuiUtil.OpenNameField(newFolderName, ref _newName) && _newName.Length > 0)
            try
            {
                folder = CkFileSystem.FindOrCreateAllFolders(_newName);
                _newName = string.Empty;
            }
            catch { /* Consume */ }

        if (folder != null)
            _filterDirty |= ExpandAncestors(folder);
    }

    // remove later maybe? Might be useful for multi- delete later idk.
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

        if (ImGuiUtil.DrawDisabledButton(FAI.Trash.ToIconString(), size, tt, !anySelected || !keys, true))
        {
            if (Selected != null)
                delete(Selected);
            else
                foreach (var leaf in _selectedPaths.OfType<CkFileSystem<T>.Leaf>())
                    delete(leaf.Value);
        }
    }
}
