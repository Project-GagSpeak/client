namespace GagSpeak.PlayerClient;

/// <summary> 
///   Manages blocked and muted individuals. Muted are scoped to plugin lifetime, 
///   blocked are based on GagSpeak AccountID across all relevent UIDS. <para/>
///   May also store sanction bans here if desired down the line.
/// </summary>
public sealed class BlockService
{
    private readonly MainConfig _config;

    private readonly HashSet<string> _silencedUids = new(StringComparer.Ordinal);

    public BlockService(MainConfig config)
    {
        _config = config;
    }

    public bool IsMuted(string uid)
        => _silencedUids.Contains(uid);

    public void MuteUser(string uid)
        => _silencedUids.Add(uid);

    public void UnmuteUser(string uid)
        => _silencedUids.Remove(uid);
}
