using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Permissions;

namespace GagspeakAPI.Data.Comparer;

/// <summary>
/// Compares two UserDto objects based on their UID.
/// </summary>
public class UserDtoComparer : IEqualityComparer<UserDto>
{
     private static UserDtoComparer _instance = new();

     private UserDtoComparer() { }

     public static UserDtoComparer Instance => _instance;

     /// <summary>
     /// Method determines if the UserDto objects are equal based on their UID.
     /// </summary>
     /// <param name="x"></param>
     /// <param name="y"></param>
     /// <returns></returns>
     public bool Equals(UserDto? x, UserDto? y)
     {
          if (x == null || y == null) return false;
          return x.User.UID.Equals(y.User.UID, StringComparison.Ordinal);
     }

     /// <summary>
     /// Gets the has code of the UserDto
     /// </summary>
     public int GetHashCode(UserDto obj)
     {
          return obj.User.UID.GetHashCode();
     }
}
