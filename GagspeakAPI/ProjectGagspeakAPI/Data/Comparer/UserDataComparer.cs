using GagspeakAPI.Data;

namespace GagspeakAPI.Data.Comparer;

public class UserDataComparer : IEqualityComparer<UserData>
{
    private static UserDataComparer _instance = new();

    private UserDataComparer()
    { }

    public static UserDataComparer Instance => _instance;

    public bool Equals(UserData? x, UserData? y)
    {
        if (x == null || y == null) return false;
        return x.UID.Equals(y.UID, StringComparison.Ordinal);
    }

    public int GetHashCode(UserData obj)
    {
        return obj.UID.GetHashCode();
    }
}

public class PrivateRoomUserComparer : IEqualityComparer<PrivateRoomUser>
{
    private static PrivateRoomUserComparer _instance = new();

    private PrivateRoomUserComparer()
    { }

    public static PrivateRoomUserComparer Instance => _instance;

    public bool Equals(PrivateRoomUser? x, PrivateRoomUser? y)
    {
        if (x == null || y == null) return false;
        return x.UserUID.Equals(y.UserUID, StringComparison.Ordinal);
    }

    public int GetHashCode(PrivateRoomUser obj)
    {
        return obj.UserUID.GetHashCode();
    }
}
