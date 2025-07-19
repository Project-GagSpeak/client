using CkCommons;
using GagspeakAPI.Dto.VibeRoom;

namespace GagSpeak.State.Models;
public readonly record struct FullPatternData
{
    public DeviceStream[] DeviceData { get; init; } = Array.Empty<DeviceStream>();

    public FullPatternData(DeviceStream[] deviceData)
    {
        DeviceData = deviceData;
    }

    public static FullPatternData Empty => new FullPatternData(Array.Empty<DeviceStream>());

    public string ToCompressedBase64()
    {
        var json = JsonConvert.SerializeObject(this);
        var compressed = json.Compress(6);
        return Convert.ToBase64String(compressed);
    }

    public static FullPatternData FromCompressedBase64(string compressedString)
    {
        var bytes = Convert.FromBase64String(compressedString);
        bytes.DecompressToString(out var decompressed);
        return JsonConvert.DeserializeObject<FullPatternData>(decompressed);
    }
}
