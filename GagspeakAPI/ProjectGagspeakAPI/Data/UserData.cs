using GagspeakAPI.Enums;
using MessagePack;

namespace GagspeakAPI.Data;

/// <summary>
/// Data object that represents the core information about a user that we ususually want to obtain.
/// <para>
/// If possible, consider adding the CKVanityTier to the user object as we will want to fetch that info from most players.
/// </para>
/// </summary>
/// <param name="UID">the user identification</param>
/// <param name="Alias">the alias UID of the user if one is set.</param>
[MessagePackObject(keyAsPropertyName: true)]
public record UserData(string UID, string? Alias = null, CkSupporterTier? SupporterTier = CkSupporterTier.NoRole, DateTime? createdOn = null)
{
    [IgnoreMember]
    public string AliasOrUID => string.IsNullOrWhiteSpace(Alias) ? UID : Alias;
}
