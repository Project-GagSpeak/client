using Dalamud.Hooking;
using GagSpeak.UpdateMonitoring.SpatialAudio.Managers;
using GagSpeak.UpdateMonitoring.SpatialAudio.Structs;
using Penumbra.String;
using Penumbra.String.Classes;
using System.Runtime.InteropServices;
using FileMode = GagSpeak.UpdateMonitoring.SpatialAudio.Structs.FileMode;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Loaders;

public unsafe partial class ResourceLoader
{
    // ===== FILES =========

    public delegate byte ReadFilePrototype(IntPtr fileHandler, SeFileDescriptor* fileDesc, int priority, bool isSync);

    public delegate byte ReadSqpackPrototype(IntPtr fileHandler, SeFileDescriptor* fileDesc, int priority, bool isSync);

    public delegate void* GetResourceSyncPrototype(IntPtr resourceManager, uint* categoryId, ResourceType* resourceType,
        int* resourceHash, byte* path, GetResourceParameters* resParams);

    public delegate void* GetResourceAsyncPrototype(IntPtr resourceManager, uint* categoryId, ResourceType* resourceType,
        int* resourceHash, byte* path, GetResourceParameters* resParams, bool isUnknown);

    // ====== FILES HOOKS ========

    public Hook<GetResourceSyncPrototype> GetResourceSyncHook { get; private set; }

    public Hook<GetResourceAsyncPrototype> GetResourceAsyncHook { get; private set; }

    public Hook<ReadSqpackPrototype> ReadSqpackHook { get; private set; }

    public ReadFilePrototype ReadFile { get; private set; }

    private void* GetResourceSyncDetour(IntPtr resourceManager, uint* categoryId, ResourceType* resourceType, int* resourceHash, 
        byte* path, GetResourceParameters* resParams)
        => GetResourceHandler(true, resourceManager, categoryId, resourceType, resourceHash, path, resParams, false);

    private void* GetResourceAsyncDetour(IntPtr resourceManager, uint* categoryId, ResourceType* resourceType, 
        int* resourceHash, byte* path, GetResourceParameters* resParams, bool isUnknown) 
        => GetResourceHandler(false, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);

    private void* CallOriginalHandler(bool isSync, IntPtr resourceManager, uint* categoryId, ResourceType* resourceType, 
        int* resourceHash, byte* path, GetResourceParameters* resParams, bool isUnknown) 
        => isSync
            ? GetResourceSyncHook.Original(resourceManager, categoryId, resourceType, resourceHash, path, resParams)
            : GetResourceAsyncHook.Original(resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);

    private void* GetResourceHandler(bool isSync, IntPtr resourceManager, uint* categoryId, 
        ResourceType* resourceType, int* resourceHash, byte* path, GetResourceParameters* resParams, bool isUnknown)
    {
        if (!Utf8GamePath.FromPointer(path, MetaDataComputation.All, out var gamePath))
        {
            return CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);
        }

        var gamePathString = gamePath.ToString();

        _logger.LogDebug(gamePathString, LoggerType.SpatialAudioLogger);

        var replacedPath = GetReplacePath(gamePathString, out var localPath) ? localPath : null;

        if (replacedPath == null || replacedPath.Length >= 260)
        {
            var unreplaced = CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);
            if (DoDebug(gamePathString)) _logger.LogDebug($"ORIGINAL: {gamePathString} -> " + new IntPtr(unreplaced).ToString("X8"), LoggerType.SpatialAudioLogger);
            return unreplaced;
        }

        var resolvedPath = new FullPath(replacedPath);
        // do not handle event for custom mdls, this will never occur.
        // PathResolved?.Invoke(*resourceType, resolvedPath);

        *resourceHash = ResourceUtils.ComputeHash(resolvedPath.InternalName, resParams);
        path = resolvedPath.InternalName.Path;

        var replaced = CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, resParams, isUnknown);
        _logger.LogDebug($"REPLACED: {gamePathString} -> {replacedPath} -> " + new IntPtr(replaced).ToString("X8"), LoggerType.SpatialAudioLogger);
        return replaced;
    }

    private byte ReadSqpackDetour(IntPtr fileHandler, SeFileDescriptor* fileDesc, int priority, bool isSync)
    {
        if (fileDesc->ResourceHandle == null) return ReadSqpackHook.Original(fileHandler, fileDesc, priority, isSync);

        if (!fileDesc->ResourceHandle->GamePath(out var originalGamePath))
        {
            return ReadSqpackHook.Original(fileHandler, fileDesc, priority, isSync);
        }

        var originalPath = originalGamePath.ToString();
        // dont need this, but do need the output.
        var isPenumbra = ProcessPenumbraPath(originalPath, out var gameFsPath);

        _logger.LogDebug(gameFsPath, LoggerType.SpatialAudioLogger);

        var isRooted = Path.IsPathRooted(gameFsPath);

        // looking for refreshed paths, could also be like |default_1|path.avfx
        if (gameFsPath != null && !isRooted)
        {
            var replacementPath = GetReplacePath(gameFsPath, out var localPath) ? localPath : null;
            if (replacementPath != null && Path.IsPathRooted(replacementPath) && replacementPath.Length < 260)
            {
                gameFsPath = replacementPath;
                isRooted = true;
                isPenumbra = false;
            }
        }

        // call the original if it's a penumbra path that doesn't need replacement as well
        if (gameFsPath == null || gameFsPath.Length >= 260 || !isRooted || isPenumbra)
        {
            _logger.LogDebug($"ORIGINAL: {originalPath}", LoggerType.SpatialAudioLogger);
            return ReadSqpackHook.Original(fileHandler, fileDesc, priority, isSync);
        }

        _logger.LogDebug($"REPLACED: {gameFsPath}", LoggerType.SpatialAudioLogger);

        fileDesc->FileMode = FileMode.LoadUnpackedResource;

        ByteString.FromString(gameFsPath, out var gamePath);

        // note: must be utf16
        var utfPath = Encoding.Unicode.GetBytes(gameFsPath);
        Marshal.Copy(utfPath, 0, new IntPtr(&fileDesc->Utf16FileName), utfPath.Length);
        var fd = stackalloc byte[0x20 + utfPath.Length + 0x16];
        Marshal.Copy(utfPath, 0, new IntPtr(fd + 0x21), utfPath.Length);
        fileDesc->FileDescriptor = fd;

        return ReadFile(fileHandler, fileDesc, priority, isSync);
    }
}
