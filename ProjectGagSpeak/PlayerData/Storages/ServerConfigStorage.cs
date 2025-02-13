namespace GagSpeak.PlayerData.Storage;

[Serializable]
public class ServerNicknamesStorage
{
    public Dictionary<string, string> UidServerComments { get; set; } = new(StringComparer.Ordinal);
}

[Serializable]
public class ServerStorage
{
    public List<Authentication> Authentications { get; set; } = []; // the authentications we have for this client
    public bool FullPause { get; set; } = false;                    // if client is disconnected from the server (not integrated yet)
    public bool ToyboxFullPause { get; set; } = false;               // if client is disconnected from the toybox server (not integrated yet)
    public string ServerName { get; set; } = string.Empty;          // name of the server client is connected to
    public string ServiceUri { get; set; } = string.Empty;           // address of the server the client is connected to
}

[Serializable]
public class ServerTagStorage
{
    public HashSet<string> OpenPairTags { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> ServerAvailablePairTags { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<string>> UidServerPairedUserTags { get; set; } = new(StringComparer.Ordinal);
}

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

[Serializable]
public class SecretKey
{
    public string Label { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool HasHadSuccessfulConnection { get; set; } = false;
    public string LinkedProfileUID { get; set; } = string.Empty;
}
