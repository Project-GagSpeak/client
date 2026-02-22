namespace GagSpeak.MufflerCore;

public class GagData
{
    public string Name { get; set; }
    public Dictionary<string, PhonemeProperties> Phonemes { get; set; }

    public GagData(string name, Dictionary<string, PhonemeProperties> phonemes)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Phonemes = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
    }
}

public static class StaticGarbleData
{
    public const string MouthOpenKey = "MOUTH_OPEN";
    public const string MouthClosedKey = "MOUTH_CLOSED";
    public const string MouthFullKey = "MOUTH_FULL";
    public const string NoSoundKey = "NO_SOUND";

    public static readonly IReadOnlyDictionary<string, List<string>> GagDataMap = new Dictionary<string, List<string>>
    {
        { MouthOpenKey, ["a", "e", "ae", "h", "hh"] },
        { MouthClosedKey, ["h", "m", "mh", "n", "ng", "mgh"] },
        { MouthFullKey, ["m", "m", "m", "m", "m", "h", "mh"] },
        { NoSoundKey, [""] }
    };
}


public class PhonemeProperties
{
    [JsonProperty("MUFFLE")]
    public int Muffle { get; set; }

    [JsonProperty("SOUND")]
    public string Sound { get; set; }
}

[Flags]
public enum GagMuffleType
{
    None = 0,
    MouthOpen = 1 << 0,
    MouthClosed = 1 << 1,
    MouthFull = 1 << 2,
    NoSound = 1 << 3,
}
