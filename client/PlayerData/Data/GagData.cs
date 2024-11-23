namespace GagSpeak.PlayerData.Data;

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


public class PhonemeProperties
{
    [JsonProperty("MUFFLE")]
    public int Muffle { get; set; }

    [JsonProperty("SOUND")]
    public string Sound { get; set; }
}
