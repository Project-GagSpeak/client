namespace GagSpeak.PlayerState.Visual;

/// <summary>
///     Flag-Based Enum detailing the various attributes of a Visual Updates.
/// </summary>
/// <remarks> May be removed later after a rework to cache management. </remarks>
[Flags]
public enum VisualUpdateFlags : byte
{
    None =             0x0,  // (0)  (( 1 << 0 ))
    Glamour =          0x1,  // (1)  (( 1 << 0 ))
    Mod =              0x2,  // (2)  (( 1 << 1 ))
    Moodle =           0x4,  // (4)  (( 1 << 2 ))
    Helmet =           0x8,  // (8)  (( 1 << 3 ))
    Visor =            0x10, // (16) (( 1 << 4 ))
    Weapon =           0x20, // (32) (( 1 << 5 ))
    CustomizeProfile = 0x40, // (64) (( 1 << 6 ))

    AllGag = Glamour | Mod | Moodle | Helmet | Visor | Weapon | CustomizeProfile,
    AllRestriction = Glamour | Mod | Moodle,
    AllRestraint = Glamour | Mod | Moodle | Helmet | Visor | Weapon | CustomizeProfile,
}
