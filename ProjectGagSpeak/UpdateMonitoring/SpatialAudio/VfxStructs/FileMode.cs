namespace GagSpeak.UpdateMonitoring.SpatialAudio.Structs;
public enum FileMode : uint
{
    LoadUnpackedResource = 0,
    LoadFileResource = 1, // Shit in My Games uses this
    LoadIndexResource = 0xA, // load index/index2
    LoadSqPackResource = 0xB
}
