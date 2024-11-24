namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
[Serializable]
public record Authentication
{
    public ulong CharacterPlayerContentId { get; set; } = 0;
    public string CharacterName { get; set; } = string.Empty;
    public uint WorldId { get; set; } = 0;
    public bool IsPrimary { get; set; } = false;
    public SecretKey SecretKey { get; set; } = new();
}
