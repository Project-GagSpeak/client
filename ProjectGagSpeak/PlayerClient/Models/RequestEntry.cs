using GagSpeak.WebAPI;
using GagspeakAPI.Network;

namespace GagSpeak.PlayerClient;

/// <summary>
///   Information on a current request that you have pending for another user.
/// </summary>
public class RequestEntry(KinksterRequest request) : IEquatable<RequestEntry>, IEquatable<KinksterRequest>
{
    internal KinksterRequest Data => request;
    public bool FromClient => request.User.UID == MainHub.OwnUserData.UID;

    // For anonymous display.
    public string SenderAnonName => request.User.AnonName;
    public string SenderTag => request.User.AnonTag;
    public string RecipientAnonName => request.Target.AnonName;
    public string RecipientTag => request.Target.AnonTag;

    // For comparison and unique identification.
    public string SenderUID => request.User.UID;
    public string RecipientUID => request.Target.UID;

    // Information about said request.
    public bool IsTemporaryRequest => request.IsTemp;
    public string Message => request.Message;

    public bool HasMessage => request.Message.Length > 0;
    public bool HasExpired => request.IsExpired();
    public TimeSpan TimeToRespond => request.TimeLeft();
    public DateTime SentTime => request.CreatedAt;
    public DateTime ExpireTime => request.CreatedAt + TimeSpan.FromDays(3);

    public string GetRemainingTimeString()
    {
        var timeLeft = TimeToRespond;
        return timeLeft.Days > 0 ? $"{timeLeft.Days}d {timeLeft.Hours}h" : $"{timeLeft.Hours}h {timeLeft.Minutes}m";
    }

    // Equality members.
    public bool Equals(RequestEntry? other)
        => other is not null &&
           SenderUID == other.SenderUID &&
           RecipientUID == other.RecipientUID;

    public bool Equals(KinksterRequest? other)
        => other is not null &&
           SenderUID == other.User.UID &&
           RecipientUID == other.Target.UID;

    public override bool Equals(object? obj)
        => obj switch
        {
            RequestEntry re => Equals(re),
            KinksterRequest req => Equals(req),
            _ => false
        };

    public override int GetHashCode()
        => HashCode.Combine(SenderUID, RecipientUID);

    public static bool operator ==(RequestEntry? left, RequestEntry? right)
        => Equals(left, right);

    public static bool operator !=(RequestEntry? left, RequestEntry? right)
        => !Equals(left, right);
}
