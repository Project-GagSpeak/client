using GagspeakAPI.Enums;
using MessagePack;

namespace GagspeakAPI.Data.Character;

/// <summary>
/// CharacterData class stores all of the user's settings, permissions, and apperance data
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record CharaActiveStateData
{
    public Guid ActiveSetId { get; set; } = Guid.Empty; // the ID of the user's active outfit
	public string ActiveSetEnabler { get; set; } = ""; // person who Enabled the set.
    public string Padlock { get; set; } = Padlocks.None.ToName(); // Type of padlock used to lock the set.
    public string Password { get; set; } = ""; // password bound to the set's lock type.
    public DateTimeOffset Timer { get; set; } = DateTimeOffset.UtcNow; // timer placed on the set's lock
    public string Assigner { get; set; } = ""; // UID that locked the set.
}
