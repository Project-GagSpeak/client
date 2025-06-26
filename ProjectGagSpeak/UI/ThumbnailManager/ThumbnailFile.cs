using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.Services.Configs;

namespace GagSpeak.Gui;

public sealed class ThumbnailFile : IDisposable
{
    public ThumbnailFolder ParentFolder { get; init; }
    public string FileName { get; private set; }
    public IDalamudTextureWrap? Icon { get; init; }

    /// <summary> The FileName without '.png' extension. </summary>
    public string FileNameNoExtension => FileName.Contains('.') ? FileName[..FileName.LastIndexOf('.')] : FileName;

    public ThumbnailFile(ThumbnailFolder parent, string name, IDalamudTextureWrap? icon)
    {
        ParentFolder = parent;
        FileName = name;
        Icon = icon;
    }

    /// <summary> Attempts to rename a file. </summary>
    /// <param name="newName"> the requested new name for the file. </param>
    /// <returns> True if the rename was successful, false otherwise. </returns>
    /// <remarks> Will fail if a file of the same name already exists or the name was empty.  </remarks>
    public bool TryRename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return false;

        if (newName == FileNameNoExtension)
            return false;

        var dir = Path.Combine(ConfigFileProvider.ThumbnailDirectory, ParentFolder.FolderName.ToString());
        var oldPath = Path.Combine(dir, FileName);
        var newPath = Path.Combine(dir, newName + ".png");

        try
        {
            if (File.Exists(newPath))
            {
                ParentFolder.Log.LogWarning($"Cannot rename: {newPath} already exists.");
                return false;
            }

            File.Move(oldPath, newPath, true);
            FileName = newName + ".png";
            ParentFolder.Log.LogDebug($"Renamed thumbnail from {oldPath} to {FileName}");
            return true;
        }
        catch (Exception ex)
        {
            ParentFolder.Log.LogError($"Failed to rename file {FileName} to {newName}: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        Icon?.Dispose();
    }
}
