using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Utility;
using GagSpeak.Services.Configs;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Extensions;
using System.Reflection;

namespace GagSpeak.Services;

/// <summary> Snagged from penumbra since Otter knows about this more than me! </summary>
public sealed class UiFileDialogService : IDisposable
{
    private readonly FileDialogManager _manager;
    private readonly ConcurrentDictionary<string, string> _startPaths = new();
    private bool _isOpen;
    public UiFileDialogService(FileDialogManager manager)
    {
        _manager = new FileDialogManager { AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking };

        // We can remove the useless folders by appending a -1 to them.
        _manager.CustomSideBarItems.Add(("Videos", string.Empty, 0, -1));
        _manager.CustomSideBarItems.Add(("Music", string.Empty, 0, -1));
        _manager.CustomSideBarItems.Add(("Documents", string.Empty, 0, -1));
        _manager.CustomSideBarItems.Add(("Favorites", string.Empty, 0, -1));
        _manager.CustomSideBarItems.Add(("Pictures", string.Empty, 0, -1));

        // Retrieve our personal Downloads Folder. (F*ck you OneDrive)
        if (Functions.GetDownloadsFolder(out var downloadsFolder))
            _manager.CustomSideBarItems.Add(("Downloads", downloadsFolder, FontAwesomeIcon.Download, -1));
        // Get all Quick Access Folders pinned by us in the file explorer.
        if (Functions.GetQuickAccessFolders(out var folders))
            foreach (var ((name, path), idx) in folders.WithIndex())
                _manager.CustomSideBarItems.Add(($"{name}##{idx}", path, FontAwesomeIcon.Folder, -1));

        // Add The GagSpeakConfig Root.
        _manager.CustomSideBarItems.Add(("GagSpeak", ConfigFileProvider.GagSpeakDirectory, FontAwesomeIcon.Handcuffs, 0));
    }

    public void OpenMultiFilePicker(string title, string filters, Action<bool, List<string>> callback, int selectionCountMax, string? startPath,
    bool forceStartPath)
    {
        _isOpen = true;
        _manager.OpenFileDialog(title, filters, CreateCallback(title, callback), selectionCountMax, GetStartPath(title, startPath, forceStartPath));
    }

    public void OpenSingleFilePicker(string title, string filters, Action<bool, string> callback)
    {
        _isOpen = true;
        _manager.OpenFileDialog(title, filters, CreateCallback(title, callback));
    }

    public void OpenFolderPicker(string title, Action<bool, string> callback, string? startPath, bool forceStartPath)
    {
        _isOpen = true;
        _manager.OpenFolderDialog(title, CreateCallback(title, callback), GetStartPath(title, startPath, forceStartPath));
    }

    public void Reset()
    {
        _isOpen = false;
        _manager.Reset();
    }

    public void Draw()
    {
        if (_isOpen)
            _manager.Draw();
    }

    public void Dispose()
    {
        _startPaths.Clear();
        _manager.Reset();
    }

    // Retrieves the StartPath for the file dialog, if it exists and is valid.
    private string? GetStartPath(string title, string? startPath, bool forceStartPath)
    {
        var path = !forceStartPath && _startPaths.TryGetValue(title, out var p) ? p : startPath;
        if (!path.IsNullOrEmpty() && !Directory.Exists(path))
            path = null;
        return path;
    }

    // unsure why this is needed atm.
    private Action<bool, List<string>> CreateCallback(string title, Action<bool, List<string>> callback)
    {
        return (valid, list) =>
        {
            _isOpen = false;
            var loc = HandleRoot(GetCurrentLocation());
            _startPaths[title] = loc;
            callback(valid, list.Select(HandleRoot).ToList());
        };
    }

    // unsure why this is needed atm.
    private Action<bool, string> CreateCallback(string title, Action<bool, string> callback)
    {
        return (valid, list) =>
        {
            _isOpen = false;
            var loc = HandleRoot(GetCurrentLocation());
            _startPaths[title] = loc;
            callback(valid, HandleRoot(list));
        };
    }

    // unsure why this is needed atm.
    private static string HandleRoot(string path)
    {
        if (path is [_, ':'])
            return path + '\\';

        return path;
    }

    private string GetCurrentLocation()
    => (_manager.GetType().GetField("dialog", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_manager) as FileDialog)
        ?.GetCurrentPath()
     ?? ".";
}
