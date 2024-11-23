using GagspeakAPI.Data.Permissions;
using MessagePack;

namespace GagspeakAPI.Data.Character;

/// <summary>
/// CharacterData class stores all of the user's settings, permissions, and appearance data
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class CharaCompositeData
{
    // Gag Information, Updates to all ONLINE PLAYERS (even when u are offline)
    public CharaAppearanceData AppearanceData { get; set; } = new();

    // Restraint set information, Updates to all ONLINE PLAYERS (even when u are offline)
    public CharaWardrobeData WardrobeData { get; set; } = new();

    // Stores the list of alias triggers for all pairs, but when sending and receiving
    // only the pair we care about is handled. (makes it so we can push in bulk)
    public Dictionary<string, CharaAliasData> AliasData { get; set; } = new();

    // stores the list of patterns you have in your toybox, and info for them (will also store trigger information) Updates to all ONLINE PLAYERS
    public CharaToyboxData ToyboxData { get; set; } = new();

    // The StorageData for the user.
    public CharaStorageData LightStorageData { get; set; } = new();

}
