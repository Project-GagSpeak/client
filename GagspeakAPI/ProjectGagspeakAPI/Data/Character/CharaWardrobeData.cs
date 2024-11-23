using GagspeakAPI.Enums;
using GagspeakAPI.Data.Interfaces;
using MessagePack;
using GagspeakAPI.Data.Struct;

namespace GagspeakAPI.Data.Character;

/// <summary>
/// CharacterData class stores all of the user's settings, permissions, and appearance data
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class CharaWardrobeData : IPadlockable
{
    public Guid ActiveSetId { get; set; } = Guid.Empty;
	public string ActiveSetEnabledBy { get; set; } = string.Empty;
    public string Padlock { get; set; } = Padlocks.None.ToName(); // Type of padlock used to lock the set.
	public string Password { get; set; } = ""; // password bound to the set's lock type.
	public DateTimeOffset Timer { get; set; } = DateTimeOffset.UtcNow; // timer placed on the set's lock
	public string Assigner { get; set; } = ""; // UID that locked the set.

    public List<Guid> ActiveCursedItems { get; set; } = new List<Guid>(); // List of cursed items that are currently active.

    public override string ToString()
    {
        return $"ActiveId = {ActiveSetId}, EnabledBy = {ActiveSetEnabledBy}, " +
            $"Padlock = {Padlock}, Password = {Password}, Timer = {Timer}, Assigner = {Assigner}";
    }
}
