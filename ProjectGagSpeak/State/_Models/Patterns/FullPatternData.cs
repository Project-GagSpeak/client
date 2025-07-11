using CkCommons;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;
public readonly record struct FullPatternData
{ 
    public PatternDeviceData[] DeviceData { get; init; } = Array.Empty<PatternDeviceData>();

    public FullPatternData(PatternDeviceData[] deviceData)
    {
        DeviceData = deviceData;
    }

    public static FullPatternData Empty => new FullPatternData(Array.Empty<PatternDeviceData>());

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

public readonly record struct PatternDeviceData(CoreIntifaceElement DeviceBrand, PatternMotorData[] MotorDots);

public readonly record struct PatternMotorData(CoreIntifaceElement Type, uint Index, double[] Data);
