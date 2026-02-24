namespace GagSpeak.MufflerCore;

/// <summary>
///     Phonetically accurate, realistic garbling data.
/// </summary>
public class GarbleData
{
    public string Name { get; set; }
    public Dictionary<string, PhonemeProperties> Phonemes { get; set; }

    public GarbleData(string name, Dictionary<string, PhonemeProperties> phonemes)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Phonemes = phonemes ?? throw new ArgumentNullException(nameof(phonemes));
    }
}

public class PhonemeProperties
{
    [JsonProperty("MUFFLE")]
    public int Muffle { get; set; }

    [JsonProperty("SOUND")]
    public string Sound { get; set; }
}

/// <summary>
///     Fallback, classic garbler data used by most common garble applications.
/// </summary>
public static class FallbackGarbleData
{
    public static readonly IReadOnlyDictionary<GagMuffleType, List<string>> GagDataMap = new Dictionary<GagMuffleType, List<string>>
    {
        { GagMuffleType.MouthOpen, ["a", "a", "a", "e", "e", "e", "ae", "h", "h", "hh", ""] },
        { GagMuffleType.MouthClosed, ["h", "h", "h", "m", "m", "m", "mh", "n", "n", "ng", "mgh", "", ""] },
        { GagMuffleType.MouthFull, ["m", "m", "m", "m", "m", "h", "h", "mh", "", ""] },
        { GagMuffleType.NoSound, [""] }
    };

    public static GagMuffleType ToPrioritizedType(this GagMuffleType flags)
    {
        ulong value = (ulong)flags;
        if (value is 0)
            return GagMuffleType.None;

        // Returns the enum value corresponding to the highest set bit
        int highestBitIndex = BitOperations.Log2(value);
        return (GagMuffleType)(1UL << highestBitIndex);
    }
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
