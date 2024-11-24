using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.UpdateMonitoring.SpatialAudio.Managers;
using Penumbra.String;
using System.Runtime.InteropServices;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Loaders;
public unsafe partial class ResourceLoader
{
    private const int INVIS_FLAG = (1 << 1) | (1 << 11);

    public delegate IntPtr GetMatrixSingletonDelegate();

    public GetMatrixSingletonDelegate GetMatrixSingleton { get; private set; }

    public delegate IntPtr GetFileManagerDelegate();

    private readonly GetFileManagerDelegate GetFileManager;

    private readonly GetFileManagerDelegate GetFileManager2;

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate byte DecRefDelegate(IntPtr resource);

    private readonly DecRefDelegate DecRef;

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void* RequestFileDelegate(IntPtr a1, IntPtr a2, IntPtr a3, byte a4);

    private readonly RequestFileDelegate RequestFile;

    private static bool ProcessPenumbraPath(string path, out string outPath)
    {
        outPath = path;
        if (string.IsNullOrEmpty(path)) return false;
        if (!path.StartsWith('|')) return false;

        var split = path.Split("|");
        if (split.Length != 3) return false;

        outPath = split[2];
        return true;
    }

    private bool GetReplacePath(string gamePath, out string localPath)
    {
        localPath = null!;

        // check for matching avfx path
        if(_avfxManager.GetReplacePath(gamePath, out var avfxLocalPath))
        {
            localPath = avfxLocalPath;
            return true;
        }

        // check for matching scd path
        if(_scdManager.GetReplacePath(gamePath, out var scdLocalPath))
        {
            localPath = scdLocalPath;
            return true;
        }

        // return a custom backup path if none present.
        return GetCustomPathBackup(gamePath, out localPath);
    }

    private bool GetCustomPathBackup(string gamePath, out string localPath)
    {
        localPath = null!;
        if (_dataManager.FileExists(gamePath)) return false; // not custom path

        return CustomPathBackups.TryGetValue(gamePath.ToLower(), out localPath!);
    }

    public void ReloadPath(string gamePath, string localPath)
    {
        if (string.IsNullOrEmpty(gamePath)) return;

        var gameResource = GetResource(gamePath, true);
        if (DoDebug(gamePath)) _logger.LogDebug($"[ReloadPath] {gamePath} / {localPath} -> " + gameResource.ToString("X8"), LoggerType.SpatialAudioLogger);

        if (gameResource != IntPtr.Zero)
        {
            RequestFile(GetFileManager2(), gameResource + GameResourceOffset, gameResource, 1);
        }

        if (string.IsNullOrEmpty(localPath)) return;

        var localGameResource = GetResource(gamePath, false); // get local path resource
        if (DoDebug(gamePath)) _logger.LogDebug($"[ReloadPath] {gamePath} / {localPath} -> " + localGameResource.ToString("X8"), LoggerType.SpatialAudioLogger);

        if (localGameResource != IntPtr.Zero)
        {
            RequestFile(GetFileManager2(), localGameResource + GameResourceOffset, localGameResource, 1);
        }
    }

    public static string Reverse(string data) => new(data.ToCharArray().Reverse().ToArray());

    private IntPtr GetResource(string path, bool original, bool decRef = true)
    {
        var extension = Reverse(path.Split('.')[1]);
        var typeBytes = Encoding.ASCII.GetBytes(extension);
        var bType = stackalloc byte[typeBytes.Length + 1];
        Marshal.Copy(typeBytes, 0, new IntPtr(bType), typeBytes.Length);
        var pResourceType = (ResourceType*)bType;

        // Category
        var split = path.Split('/');
        var categoryString = split[0];
        var categoryBytes = categoryString switch
        {
            "bgcommon" => BitConverter.GetBytes(1u),
            "cur" => ResourceUtils.GetDatCategory(3u, split[1]),
            "chara" => BitConverter.GetBytes(4u),
            "shader" => BitConverter.GetBytes(5u),
            "ui" => BitConverter.GetBytes(6u),
            "sound" => BitConverter.GetBytes(7u),
            "vfx" => BitConverter.GetBytes(8u),
            "bg" => ResourceUtils.GetBgCategory(split[1], split[2]),
            "music" => ResourceUtils.GetDatCategory(12u, split[1]),
            _ => BitConverter.GetBytes(0u)
        };
        var bCategory = stackalloc byte[categoryBytes.Length + 1];
        Marshal.Copy(categoryBytes, 0, new IntPtr(bCategory), categoryBytes.Length);
        var pCategoryId = (uint*)bCategory;

        ByteString.FromString(path, out var resolvedPath);
        var hash = resolvedPath.GetHashCode();

        var hashBytes = BitConverter.GetBytes(hash);
        var bHash = stackalloc byte[hashBytes.Length + 1];
        Marshal.Copy(hashBytes, 0, new IntPtr(bHash), hashBytes.Length);
        var pResourceHash = (int*)bHash;

        var resource = original ? new IntPtr(GetResourceSyncHook.Original(GetFileManager(), pCategoryId, pResourceType, pResourceHash, resolvedPath.Path, null)) :
            new IntPtr(GetResourceSyncDetour(GetFileManager(), pCategoryId, pResourceType, pResourceHash, resolvedPath.Path, null));
        if (decRef) DecRef(resource);

        return resource;
    }

    // not sure why we would want to trigger this condition yet.
    private static bool DoDebug(string path) => false; //  Plugin.State == WorkspaceState.None && Plugin.Managers.Where( x => x != null && x.DoDebug( path ) ).Any();
}
