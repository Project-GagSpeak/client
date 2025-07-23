using CkCommons;
using GagspeakAPI.Attributes;
using GagspeakAPI.Dto.VibeRoom;

namespace GagSpeak.State.Models;
public readonly record struct FullPatternData
{
    public DeviceStream[] DeviceData { get; init; } = Array.Empty<DeviceStream>();
    public ToyBrandName PrimaryDeviceUsed { get; init; } = ToyBrandName.Unknown;
    public ToyBrandName SecondaryDeviceUsed { get; init; } = ToyBrandName.Unknown;
    public ToyMotor MotorsUsed { get; init; } = ToyMotor.Unknown;
    public FullPatternData(DeviceStream[] deviceData)
    {
        DeviceData = deviceData;
        var devices = DeviceData.Select(x => x.Toy).Distinct().ToArray();
        var motors = DeviceData.SelectMany(d => d.MotorData).Select(m => m.Motor).Aggregate(ToyMotor.Unknown, (acc, val) => acc | val);
        PrimaryDeviceUsed = devices.Length > 0 ? devices[0] : ToyBrandName.Unknown;
        SecondaryDeviceUsed = devices.Length > 1 ? devices[1] : ToyBrandName.Unknown;
        MotorsUsed = motors;
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
