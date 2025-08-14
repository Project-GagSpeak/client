using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Resource;
using GagSpeak.GameInternals.Structs;
using Penumbra.Api.Enums;
using Penumbra.GameData;
using Penumbra.String;
using Penumbra.String.Classes;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
#nullable enable

namespace GagSpeak.GameInternals.Detours;

// Most of this is taken from penumbra as vfxeditor has been falling behind on updates.
// we technically dont need this, unless we want to manipulate resources while they are in use.
// However, while the only nessisary calls are for actor create and removal, we are using custom paths as avfx and scd files.
// Because of this, we must also handle resources, as they are loaded by the game when spawning actors.
public unsafe partial class ResourceDetours
{
    // We need to use the ReadFile function to load local, uncompressed files instead of loading them from the SqPacks.
    private delegate byte ReadFile(IntPtr fileHandler, SeFileDescriptor* fileDesc, int priority, bool isSync);
    [Signature(Sigs.ReadFile)]
    private readonly ReadFile ReadFileFunc = null!;


    private delegate byte ReadSqPack(IntPtr fileHandler, SeFileDescriptor* pFileDesc, int priority, bool isSync);
    [Signature(Sigs.ReadSqPack, DetourName = nameof(ReadSqPackDetour))]
    private readonly Hook<ReadSqPack> ReadSqPackHook = null!;

    // Fired every time any resoruce is loaded.
    private delegate ResourceHandle* GetResourceSync(ResourceManager* resourceManager, ResourceCategory* pCategoryId, ResourceType* pResourceType, 
        int* pResourceHash, byte* pPath, GetResourceParameters* pGetResParams, nint unk7, uint unk8);
    [Signature(Sigs.GetResourceSync, DetourName = nameof(GetResourceSyncDetour))]
    private readonly Hook<GetResourceSync> GetResourceSyncHook = null!;

    // Fired every time any resource is loaded asynchronously.
    private delegate ResourceHandle* GetResourceAsync(ResourceManager* resourceManager, ResourceCategory* pCategoryId, ResourceType* pResourceType, 
        int* pResourceHash, byte* pPath, GetResourceParameters* pGetResParams, byte isUnknown, nint unk8, uint unk9);
    [Signature(Sigs.GetResourceAsync, DetourName = nameof(GetResourceAsyncDetour))]
    private readonly Hook<GetResourceAsync> GetResourceAsyncHook = null!;

    // for reading sqpack file detours, to ensure our custom paths are correctly handled.
    private byte ReadSqPackDetour(IntPtr fileHandler, SeFileDescriptor* pFileDesc, int priority, bool isSync)
    {
        // ret original if resourcehandle is null.
        if (pFileDesc->ResourceHandle is null)
            return ReadSqPackHook.Original(fileHandler, pFileDesc, priority, isSync);

        // return original if the path has no original gamepath.
        if (pFileDesc->ResourceHandle->GamePath(out var baseGamePath))
            return ReadSqPackHook.Original(fileHandler, pFileDesc, priority, isSync);

        var originalPath = baseGamePath.ToString();

        // Return original if not a custom gs resoruce path, as we dont care about it.
        if (!TryGetGsResourcePath(originalPath, out var gsPath))
            return ReadSqPackHook.Original(fileHandler, pFileDesc, priority, isSync);

        // otherwise log the replaced path.
        _logger.LogDebug($"ReadSqPack -> GAGSPEAK_PATH: {gsPath}");
        // update the file descriptor path to the new path.
        pFileDesc->FileMode = FileMode.LoadUnpackedResource;
        ByteString.FromString(gsPath, out var gamePath);

        // mandatory to be utf16 for a successful read.
        var utfPath = Encoding.Unicode.GetBytes(gsPath);
        Marshal.Copy(utfPath, 0, new IntPtr(&pFileDesc->Utf16FileName), utfPath.Length);
        var fileDesc = stackalloc byte[0x20 + utfPath.Length + 0x16];
        // copy the file descriptor to the stack.
        Marshal.Copy(utfPath, 0, new IntPtr(fileDesc + 0x21), utfPath.Length);
        // update the file descriptor path to the new path.
        pFileDesc->FileDescriptor = fileDesc;

        return ReadFileFunc(fileHandler, pFileDesc, priority, isSync);
    }
    private ResourceHandle* GetResourceSyncDetour(ResourceManager* resourceManager, ResourceCategory* categoryId, ResourceType* resourceType,
        int* resourceHash, byte* path, GetResourceParameters* pGetResParams, nint unk8, uint unk9)
        => GetResourceHandler(true, resourceManager, categoryId, resourceType, resourceHash, path, pGetResParams, 0, unk8, unk9);

    private ResourceHandle* GetResourceAsyncDetour(ResourceManager* resourceManager, ResourceCategory* categoryId, ResourceType* resourceType,
        int* resourceHash, byte* path, GetResourceParameters* pGetResParams, byte isUnknown, nint unk8, uint unk9)
        => GetResourceHandler(false, resourceManager, categoryId, resourceType, resourceHash, path, pGetResParams, isUnknown, unk8, unk9);

    /// <summary>
    ///     Resources can be obtained synchronously and asynchronously. We need to change behaviour in both cases.
    ///     Both work basically the same, so we can reduce the main work to one function used by both hooks.
    /// </summary>
    private ResourceHandle* GetResourceHandler(bool isSync, ResourceManager* resourceManager, ResourceCategory* categoryId,
        ResourceType* resourceType, int* resourceHash, byte* path, GetResourceParameters* pGetResParams, byte isUnk, nint unk8, uint unk9)
    {
        // if the resource path is not from a pointer, then return the original handler.
        if (!Utf8GamePath.FromPointer(path, MetaDataComputation.CiCrc32, out var gamePath))
        {
            _logger.LogWarning("Could not create GamePath from resource path.");
            return CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, pGetResParams, isUnk, unk8, unk9);
        }

        // if the game path is empty, protect it against crashes from null / empty game paths and return null.
        if (gamePath.IsEmpty)
        {
            _logger.LogWarning($"Returning null, as path was an empty resource path requested with category {*categoryId}, type {*resourceType}, hash {*resourceHash}.");
            return null; // this is safe to do here.
        }

        var originalPath = gamePath;
        // If not a valid Gs Resource path, return the original.
        if (!TryGetGsResourcePath(originalPath.ToString(), out var localPath) || localPath.Length > 260)
            return CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, pGetResParams, isUnk, unk8, unk9);

        _logger.LogDebug($"GAGSPEAK_PATH: {originalPath} -> (local) {localPath}");
        var resolvedPath = new FullPath(localPath);
        // add the crc64 to the path for later reference.
        if (*resourceType == ResourceType.Scd)
            CustomScdCrc.Add(resolvedPath.Crc64);

        // obtain the resource hash for the computated resource path.
        *resourceHash = ComputeHash(resolvedPath.InternalName, pGetResParams);
        path = resolvedPath.InternalName.Path;

        // mark the replaced path we the call to the original handler with the new path and resource hash containing the custom data.
        _logger.LogDebug($"REPLACED: {originalPath} -> (resolved local) {resolvedPath}");
        return CallOriginalHandler(isSync, resourceManager, categoryId, resourceType, resourceHash, path, pGetResParams, isUnk, unk8, unk9);
    }

    // Helper functions.
    private static int ComputeHash(CiByteString path, GetResourceParameters* pGetResParams)
    {
        if (pGetResParams is null || !pGetResParams->IsPartialRead) return path.Crc32;

        return CiByteString.Join((byte)'.', path,
            CiByteString.FromString(pGetResParams->SegmentOffset.ToString("x"), out var s1, MetaDataComputation.None) ? s1 : CiByteString.Empty,
            CiByteString.FromString(pGetResParams->SegmentLength.ToString("x"), out var s2, MetaDataComputation.None) ? s2 : CiByteString.Empty
        ).Crc32;
    }

    /// <summary>
    ///     We don't nessisarily need to validate the path is from the game path, but rather return original
    ///     if the path is not one of our custom paths. This way we do not need to iterate gamedata every process.
    /// </summary>
    private bool TryGetGsResourcePath(string gamePath, [NotNullWhen(true)] out string? gsResourcePath)
    {
        if (_cache.TryGetReplacedPath(gamePath, out gsResourcePath) && gsResourcePath.Length <= 260)
            return true;
        // otherwise it failed.
        gsResourcePath = null;
        return false;
    }

    private bool GameFilePathExists(string gamePath)
    {
        try
        {
            return Svc.Data.FileExists(gamePath);
        }
        catch (Bagagwa ex)
        {
            _logger.LogInformation($"GameFilePathExists failed for path: {gamePath}. Exception: {ex.Message}");
            return false; // not valid gamepath.
        }
    }

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

    private ResourceHandle* CallOriginalHandler(bool isSync, ResourceManager* resourceManager, ResourceCategory* categoryId, ResourceType* resourceType, 
        int* resourceHash, byte* path, GetResourceParameters* pGetResParams, byte isUnk, nint unk8, uint unk9)
    {
        return isSync
            ? GetResourceSyncHook.Original(resourceManager, categoryId, resourceType, resourceHash, path, pGetResParams, unk8, unk9)
            : GetResourceAsyncHook.Original(resourceManager, categoryId, resourceType, resourceHash, path, pGetResParams, isUnk, unk8, unk9);
    }
}
