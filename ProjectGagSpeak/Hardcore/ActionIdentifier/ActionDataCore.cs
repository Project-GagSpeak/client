using System.Collections.Generic;

namespace GagSpeak.Hardcore;

// define a dictionary list for each class, and pull from that respective one from upper level action data class
public static class ActionDataCore
{
    // collection for Adventurer
    public static Dictionary<uint, AcReqProps[]> Adventurer = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Gladiator
    public static Dictionary<uint, AcReqProps[]> Gladiator = new Dictionary<uint, AcReqProps[]>()
    {
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},                         // Sprint
        { 7,    new AcReqProps[]{AcReqProps.None}},                             // Teleport
        { 8,    new AcReqProps[]{AcReqProps.None}},                             // Return
        { 28,   new AcReqProps[]{AcReqProps.Movement} },                        // Toggle Iron Will
        { 23,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // Circle of Scorn
        { 7535, new AcReqProps[]{AcReqProps.Movement}},                         // Reprisal
        { 7548, new AcReqProps[]{AcReqProps.Movement}},                         // Arms Length
        { 7533, new AcReqProps[]{AcReqProps.Sight}},                            // Provoke
        { 7537, new AcReqProps[]{AcReqProps.Sight}},                            // shirk
        { 7538, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}}, // interject
        { 7540, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}}, // low blow
        { 7531, new AcReqProps[]{AcReqProps.None}},                             // Rampart
        { 17,   new AcReqProps[]{AcReqProps.None}},                             // Sentinel
        { 9,    new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // Fast Blade
        { 15,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // Riot Blade
        { 21,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // Rage of Halone
        { 24,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement,
                                 AcReqProps.Sight, AcReqProps.LegMovement}},    // Shield Lob
        { 7381, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // Total Eclipse
        { 16,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}}, // Shield Bash
        { 20,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}}, // Fight or Flight
    };

    // Collection for Pugilist
    public static Dictionary<uint, AcReqProps[]> Pugilist = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Leg Sweep
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Bloodpath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Feint
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Arm's Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Bootshine
        { 53, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // True Stike
        { 54, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Snap Punch
        { 56, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Arm of the Destroyer
        { 62, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Twin Snakes
        { 61, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Demolish
        { 66, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Dragon Kick
        { 74, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Steeled Maditation
        { 36940, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Steel Peak
        { 25761, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Manta
        { 65, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
    };

    // Collection for Marauder
    public static Dictionary<uint, AcReqProps[]> Marauder = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Rampart
        { 7531, new AcReqProps[]{AcReqProps.None}},
        // Low Blow
        { 7540, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Provoke
        { 7533, new AcReqProps[]{AcReqProps.Sight}},
        // Interject
        { 7538, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Reprisal
        { 7535, new AcReqProps[]{AcReqProps.Movement}},
        // Arm's Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Shirk
        { 7537, new AcReqProps[]{AcReqProps.Sight}},
        // Heavy Swing
        { 31, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Maim
        { 37, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Storm's Path
        { 42, new AcReqProps[]{ AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Storm's Eye
        { 45, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Overpower
        { 41, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Tomahawk
        { 46, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Berserk
        { 38, new AcReqProps[]{AcReqProps.Movement}},
        // Defience
        { 48, new AcReqProps[]{AcReqProps.None}},
        // Thrill of Battle
        { 40, new AcReqProps[]{AcReqProps.None}},
        // Vengeance
        { 44, new AcReqProps[]{AcReqProps.None}},
        // Holmgang
        { 43, new AcReqProps[]{AcReqProps.Sight, AcReqProps.Movement, AcReqProps.ArmMovement}},
    };

    // Collection for Lancer
    public static Dictionary<uint, AcReqProps[]> Lancer = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Leg Sweep
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Bloodpath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Feint
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Arm's Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // True Thrust
        { 75, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Vorpal Thrust
        { 78, new AcReqProps[]{AcReqProps.Movement}},
        // Full Thrust
        { 84, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Disembowel
        { 87, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Chaos Thrust
        { 88, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Piercing Talon
        { 90, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Life Surge
        { 83, new AcReqProps[]{AcReqProps.None}},
        // Lance Change
        { 85, new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Archer
    public static Dictionary<uint, AcReqProps[]> Archer = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Leg Graze
        { 7554, new AcReqProps[]{AcReqProps.None}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Foot Graze
        { 7553, new AcReqProps[]{AcReqProps.None}},
        // Peloton
        { 7557, new AcReqProps[]{AcReqProps.Weighted}},
        // Head Graze
        { 7551, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
        // Arm's Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Heavy Shot
        { 97, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Straight Shot
        { 98, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Venomous Bite
        { 100, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Quick Nock
        { 106, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Wide Volley
        { 36974, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement, AcReqProps.Sight}}, // Back flips on spot and shoots from above...
        // Windbite
        { 113, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Raging Strikes
        { 101, new AcReqProps[]{AcReqProps.None}},
        // Bloodletter
        { 110, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Repelling Shot
        { 112, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Berrage
        { 107, new AcReqProps[]{AcReqProps.ArmMovement}}, // There is arm movement required?
    };

    // Collection for Conjurer
    public static Dictionary<uint, AcReqProps[]> Conjurer = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Repose
        { 16560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Esuna
        { 7568, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Swiftcase
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Rescue
        { 7571, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Stone
        { 119, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Stone II
        { 127, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Cure
        { 120, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Medica
        { 124, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Raise
        { 125, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Cure II
        { 135, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Aero
        { 121, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Aero II
        { 132, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Medica II
        { 133, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
    };

    // Collection for Thaumaturge
    public static Dictionary<uint, AcReqProps[]> Thaumaturge = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Addle
        { 7560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Sleep
        { 25880, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Swiftcase
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Blizzard
        { 142, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Fire
        { 141, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Thunder
        { 144, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Blizzard II
        { 25793, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Scathe
        { 156, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Fire II
        { 147, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Thunder II
        { 7447, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Fire III
        { 152, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Transpose
        { 149, new AcReqProps[]{AcReqProps.None}},
        // Manaward
        { 157, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Aetherial Manipulation
        { 155, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
    };

    // Collection for Carpenter
    public static Dictionary<uint, AcReqProps[]> Carpenter = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Blacksmith
    public static Dictionary<uint, AcReqProps[]> Blacksmith = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Armorer
    public static Dictionary<uint, AcReqProps[]> Armorer = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Goldsmith
    public static Dictionary<uint, AcReqProps[]> Goldsmith = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Leatherworker
    public static Dictionary<uint, AcReqProps[]> Leatherworker = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Weaver
    public static Dictionary<uint, AcReqProps[]> Weaver = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Alchemist
    public static Dictionary<uint, AcReqProps[]> Alchemist = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Culinarian
    public static Dictionary<uint, AcReqProps[]> Culinarian = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Miner
    public static Dictionary<uint, AcReqProps[]> Miner = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Botanist
    public static Dictionary<uint, AcReqProps[]> Botanist = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Fisher
    public static Dictionary<uint, AcReqProps[]> Fisher = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Paladin
    public static Dictionary<uint, AcReqProps[]> Paladin = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Toggle Iron Will
        { 28,   new AcReqProps[]{AcReqProps.Movement}},
        // Circle of Scorn
        { 23,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Reprisal
        { 7535, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Provoke
        { 7533, new AcReqProps[]{AcReqProps.Sight}},
        // shirk
        { 7537, new AcReqProps[]{AcReqProps.Sight}},
        // interject
        { 7538, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // low blow
        { 7540, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Rampart
        { 7531, new AcReqProps[]{AcReqProps.None}},
        // Sentinel
        { 17,   new AcReqProps[]{AcReqProps.None}},
        // Fast Blade
        { 9,    new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Riot Blade
        { 15,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Rage of Halone (royal athority)
        { 21,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Royal Authority
        { 3539,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Shield Lob
        { 24,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Total Eclipse
        { 7381, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Shield Bash
        { 16,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Fight or Flight
        { 20,   new AcReqProps[]{AcReqProps.None}},                             
        // Goring Blade
        { 3538, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Intervene
        { 16461,new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Atonement
        { 16460,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Holy Spirit
        { 7384, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Speech}},
        // Spirits Within (Expiacition)
        { 29,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Requiescat
        { 7383, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Speech}},
        // Holy Circle
        { 16458,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Speech}},
        // Clemency
        { 3541, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Speech}},
        // Confiteor
        { 16459,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Speech}},
        // Sheltron
        { 3542, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Bulkwork
        { 22,   new AcReqProps[]{AcReqProps.None}},
        // Divine Veil
        { 3540, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Cover
        { 27,  new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Prominance
        { 16457,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Hallowed Ground
        { 30, new AcReqProps[]{AcReqProps.None}},
        // Passage of Arms
        { 7385,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Intervention
        { 7382,new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.Sight}},
    };

    // Collection for Monk
    public static Dictionary<uint, AcReqProps[]> Monk = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Bloodbath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Feint 
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Stun
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // bootshine
        { 53,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // legs fine
        // true strike
        { 54,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // legs fine
        // snap punch
        { 56,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Four-point Fury
        { 16473,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // surprisingly legs fine
        // Dragon Kick
        { 74,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Arm of the Destroyer (shadow)
        { 62,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Demoilish
        { 66,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Rockbreaker
        { 70,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Howling Fist
        { 25763,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Twin Snakes
        { 61,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // legs fine
        // Six-sided Star
        { 16476,new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Thunderclap
        { 25762,new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Perfect Balance
        { 69,   new AcReqProps[]{AcReqProps.None}},
        // Form Shift
        { 4262, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Masterful Blitz
        { 25764,new AcReqProps[]{AcReqProps.Movement}},
        // Riddle of earth
        { 7394, new AcReqProps[]{AcReqProps.Movement}},
        // Riddle of Fire
        { 7395, new AcReqProps[]{AcReqProps.Movement}},
        // Riddle of Wind
        { 25766,new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // meditation
        { 3546, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Brotherhood
        { 7396, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Mantra
        { 65,   new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Anatman
        { 16475,new AcReqProps[]{AcReqProps.Movement}},
    };

    // Collection for Warrior
    public static Dictionary<uint, AcReqProps[]> Warrior = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Reprisal
        { 7535, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Provoke
        { 7533, new AcReqProps[]{AcReqProps.Sight}},
        // shirk
        { 7537, new AcReqProps[]{AcReqProps.Sight}},
        // interject
        { 7538, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // low blow
        { 7540, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Rampart
        { 7531, new AcReqProps[]{AcReqProps.None}},
        // primal rend
        { 25753, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // orogeny
        { 25752, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // nascent flash
        { 16464, new AcReqProps[]{AcReqProps.Sight}},
        // shake it off
        { 7388, new AcReqProps[]{AcReqProps.None}},
        // upheavel
        { 7387, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // onslaught
        { 7386, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // equilibrium
        { 3552, new AcReqProps[]{AcReqProps.None}},
        // raw intuition
        { 3551, new AcReqProps[]{AcReqProps.None}},
        // infuriate
        { 52, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // storms eye
        { 45, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // steel cyclone
        { 51, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // holmgang
        { 43, new AcReqProps[]{AcReqProps.Sight, AcReqProps.Movement, AcReqProps.ArmMovement}},
        // mythril tempest
        { 16462, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // vengeance
        { 44, new AcReqProps[]{AcReqProps.None}},
        // inner beast
        { 49, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // heavy swing
        { 31, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // maim
        { 37, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // beserk
        { 38, new AcReqProps[]{AcReqProps.Movement}},
        // defiance
        { 48, new AcReqProps[]{AcReqProps.None}},
        // overpower
        { 41, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // tomahawk
        { 46, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // storms path
        { 42, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // thrill of battle
        { 40, new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Dragoon
    public static Dictionary<uint, AcReqProps[]> Dragoon = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Bloodbath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Feint 
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Stun
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        
        // True Thrust
        { 75,  new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Vorpal Thrust
        { 78,  new AcReqProps[]{AcReqProps.Movement}},
        // Heavens' Thrust
        { 84,  new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Disembowel
        { 87,  new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Chaos Thrust
        { 88,  new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},        
        // Fang and Claw
        { 3554,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Wheeling Thrust
        { 3556,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Jump
        { 92, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Mirage Dive
        { 7399,new AcReqProps[]{AcReqProps.Movement, AcReqProps.Speech, AcReqProps.Sight}},
        // Spineshatter Dive
        { 95, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Dragonfire Dive
        { 96, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Geirskogul
        { 3555,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Stardiver
        { 16480,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Wyrmwind Thrust
        { 25773,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Piercing Talon
        { 90, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Elusive Jump
        { 94, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
        // Battle Litany
        { 3557,new AcReqProps[]{AcReqProps.Speech}},
        // Dragon Sight
        { 7398,new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Doom Spike
        { 86, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Sonic Thrust
        { 7397, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Coerthan Torment
        { 16477,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Lance Charge
        { 85,new AcReqProps[]{AcReqProps.None}},
        // Life Surge
        { 82,new AcReqProps[]{AcReqProps.None}},    
    };

    // Collection for Bard
    public static Dictionary<uint, AcReqProps[]> Bard = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Head Graze
        { 7551, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
        // Peleton
        { 7557, new AcReqProps[]{AcReqProps.Weighted}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // heavy shot
        { 97, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // straight shot
        { 98, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // raging strikes
        { 101,new AcReqProps[]{AcReqProps.None}},
        // venomous bite
        { 100,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // bloodletter
        { 110,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // repelling shot
        { 112,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // quick nock
        { 106,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // windbite
        { 113, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // mages ballad
        { 114,new AcReqProps[]{AcReqProps.Speech}},
        // the warden's paean
        { 3561,new AcReqProps[]{AcReqProps.Sight}},
        // barrage
        { 107, new AcReqProps[]{AcReqProps.Sight, AcReqProps.Movement, AcReqProps.ArmMovement}},
        // army's paeon
        { 116,new AcReqProps[]{AcReqProps.Speech}},
        // rain of death
        { 117, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // battle voice
        { 118, new AcReqProps[]{AcReqProps.Speech}},
        // the warderer's minuet
        { 3559,new AcReqProps[]{AcReqProps.Speech, AcReqProps.ArmMovement, AcReqProps.Movement}},
        // empyreal arrow
        { 3558,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // iron jaws
        { 3560,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // sidewinder
        { 3562,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // troubadour
        { 7405,new AcReqProps[]{AcReqProps.None}},
        // Nature's Minne
        { 7408,new AcReqProps[]{AcReqProps.Speech}},
        // shadowbite
        { 16494, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // apex arrow
        { 16496, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // radient finale
        { 25785, new AcReqProps[]{AcReqProps.Speech}},
    };

    // Collection for WhiteMage
    public static Dictionary<uint, AcReqProps[]> WhiteMage = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Cure II
        { 135, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Tetrgrammaton
        { 3570,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Afflatus Solace
        { 16531,new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Regen
        { 137, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Divine Benison
        {7432, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Afflatus Misery
        { 16535, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Stone / Glare
        { 119, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Aero / Dia
        { 121, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Medica II
        { 133, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Afflatus Rapture
        { 16534, new AcReqProps[]{AcReqProps.None}},
        // Medica
        { 124, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Cure III
        { 131, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // AquaVeil
        { 25861, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Assize
        { 3571, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Benediction
        { 140, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Esuna
        { 7568, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // SwiftCast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Raise
        { 125, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // rescue
        { 7571, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Repose
        { 16560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Presence of Mind
        { 136, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Thin Air
        { 7430, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Plenary Indulgence
        { 7433, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Temperance
        { 16536, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Asylum
        { 3569, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Holy
        { 139, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Liturgy of the Bell
        { 25862, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Cure
        { 120, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
    };

    // Collection for BlackMage
    public static Dictionary<uint, AcReqProps[]> BlackMage = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Sleep
        { 25880, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Swiftcast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Addle
        { 7560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        
        // Fire IV
        { 3577, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement fine
        // Fire
        { 141, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // is used for paradox, which has ok leg movement
        // Fire III
        { 152, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}}, // too much leg lean
        // Dispair
        { 16505, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement fine
        // Xenoglossy
        { 16507, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}}, // too much lean 
        // Between the Lines
        { 7419, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}}, // movement here is fine
        // Umbral soul
        { 16506, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Blizzard III
        { 154, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}}, // too much lean
        // Blizzard IV
        { 3576, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement here is fine
        // Thunder
        { 144, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}}, // too much leg movement
        // Manafont
        { 158, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement fine
        // Blizzard II
        { 25793, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement fine
        // Freeze
        { 159, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement fine
        // Fire II
        { 147, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement fine
        // Flare
        { 162, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movement fine
        // Transpose
        { 149, new AcReqProps[]{AcReqProps.None}}, // none needed
        // Sharpcast
        { 3574, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, 
        // Leylines
        { 3573, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Thunder II
        { 7447, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Amplifier
        { 25796, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Foul
        { 7422, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Scathe
        { 156, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Aetherial Manipulation
        { 155, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
        // Triplecast
        { 7421, new AcReqProps[]{AcReqProps.None}},
        // Manaward 
        { 157, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
    };
        
    // Collection for Arcanist
    public static Dictionary<uint, AcReqProps[]> Arcanist = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Addle
        { 7560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Sleep
        { 25880, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Swiftcase
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Summon Carbuncle
        { 25798, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Physic
        { 16230, new AcReqProps[]{AcReqProps.Speech, AcReqProps.Sight}},
        // Aethercharge
        { 25800, new AcReqProps[]{AcReqProps.Speech, AcReqProps.Sight}},
        // Summon Ruby
        { 25802, new AcReqProps[]{AcReqProps.Speech}},
        // Gemshine
        { 25883, new AcReqProps[]{AcReqProps.Sight}},
        // Resurrection
        { 173, new AcReqProps[]{AcReqProps.Sight}},
        // Summon Topaz
        { 25803, new AcReqProps[]{AcReqProps.Speech}},
        // Summon Emerald
        { 25804, new AcReqProps[]{AcReqProps.Speech}},
        // Outburst
        { 16511, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Presious Brilliance
        { 258884, new AcReqProps[]{AcReqProps.Sight}},
        // Ruin II
        { 172, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Ruin
        { 163, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Radiant Aegis
        { 25799, new AcReqProps[]{AcReqProps.None}},
        // Fester
        { 181, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Energy Drain
        { 16508, new AcReqProps[]{AcReqProps.Sight}},

    };

    // Collection for Summoner
    public static Dictionary<uint, AcReqProps[]> Summoner = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Sleep
        { 25880, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Swiftcast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Addle
        { 7560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Summon carbuncle
        { 25798, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Searing Light
        { 25801, new AcReqProps[]{AcReqProps.Speech}},
        // Radiant Aegis
        { 25799, new AcReqProps[]{AcReqProps.None}},
        // Resurection
        { 173, new AcReqProps[]{AcReqProps.Sight}},
        // Outburst
        { 16511, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Painflare
        { 3578, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
        // Physick
        { 16230, new AcReqProps[]{AcReqProps.Speech, AcReqProps.Sight}},
        // Asrtal flow
        { 25822, new AcReqProps[]{AcReqProps.Speech, AcReqProps.Sight}},
        // precious brilliance
        { 25884, new AcReqProps[]{AcReqProps.Sight}},
        // Gemshine
        { 25883, new AcReqProps[]{AcReqProps.Sight}},
        // Ruin IV
        { 7426, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Energy Drain
        { 16508, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Energy Siphon
        { 16510, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Ruin III
        { 163, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Summon Emerald
        { 25804, new AcReqProps[]{AcReqProps.Speech}},
        // Summon Topaz
        { 25803, new AcReqProps[]{AcReqProps.Speech}},
        // Summon Ruby
        { 25802, new AcReqProps[]{AcReqProps.Speech}},
        // Fester
        { 181, new AcReqProps[]{AcReqProps.Sight}},
        // Aethercharge
        { 25800, new AcReqProps[]{AcReqProps.Speech, AcReqProps.Sight}},
        // Enkindle Bahamut
        { 7429, new AcReqProps[]{AcReqProps.Sight}},
    };

    // Collection for Scholar
    public static Dictionary<uint, AcReqProps[]> Scholar = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Esuna
        { 7568, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // SwiftCast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Raise
        { 125, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // rescue
        { 7571, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Repose
        { 16560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // aetherflow
        { 166, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Speech}},
        // energy drain
        { 167, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Lustrate
        { 169, new AcReqProps[]{AcReqProps.Speech}},
        // art of war
        { 16539, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // sacred soil
        { 188, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight, AcReqProps.Speech}},
        // indomitability
        { 3583, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // deployment tactics
        { 3585, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // emergency tactics
        { 3586, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // excogitation
        { 7434, new AcReqProps[]{AcReqProps.Sight}},
        // chain stratagem
        { 7436, new AcReqProps[]{AcReqProps.Speech}},
        // aetherpact
        { 7437, new AcReqProps[]{AcReqProps.Speech}},
        // recitation
        { 16542, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Ruin
        { 17869, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // bio
        { 17864, new AcReqProps[]{AcReqProps.Sight}},
        // psysick
        { 190, new AcReqProps[]{AcReqProps.Speech, AcReqProps.Sight}},
        // summon eos
        { 17215, new AcReqProps[]{AcReqProps.Speech}},
        // whispering dawn
        { 16537, new AcReqProps[]{AcReqProps.Speech}},
        // adloquium
        { 185, new AcReqProps[]{AcReqProps.Sight}},
        // succor
        { 186, new AcReqProps[]{AcReqProps.None}},
        // ruin II
        { 17870, new AcReqProps[]{AcReqProps.Sight}},
        // feyt illumination
        { 16538, new AcReqProps[]{AcReqProps.Speech}},
        // fey blessing
        { 16543, new AcReqProps[]{AcReqProps.Speech}},
        // summon seraph
        { 16545, new AcReqProps[]{AcReqProps.Speech}},
        // proteaction
        { 25867, new AcReqProps[]{AcReqProps.None}},
        // expedient
        { 25868, new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Rogue
    public static Dictionary<uint, AcReqProps[]> Rogue = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Leg Sweep
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Bloodpath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Feint
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Arm's Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Spinning Edge
        { 2240, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Gust Slash
        { 2242, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Aeolian Edge
        { 2255, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Throwing Dagger
        { 2247, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Death Blossom
        { 2254, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Shade Shift
        { 2241, new AcReqProps[]{AcReqProps.None}},
        // Hide
        { 2245, new AcReqProps[]{AcReqProps.None}},
        // Mug
        { 2248, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Trick Attack
        { 2258, new AcReqProps[]{AcReqProps.Movement}},
    };

    // Collection for Ninja
    public static Dictionary<uint, AcReqProps[]> Ninja = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Bloodbath
        { 7542, new AcReqProps[]{AcReqProps.None}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Feint 
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Leg Sweep
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},

        // Spinning Edge
        { 2240, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Gust Slash
        { 2242, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.LegMovement}},
        // Throwing Dagger
        { 2247, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Mug
        { 2248, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Trick Attack
        { 2258, new AcReqProps[]{AcReqProps.Movement}},
        // Aeolian Edge
        { 2255, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement,  AcReqProps.LegMovement}},
        // Ten
        { 2259, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Chi
        { 2261, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Jin
        { 2263, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Ninjutsu
        { 2260, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Death Blossum
        { 2254, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Assassinate
        { 2246, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Shukuchi
        { 2262, new AcReqProps[]{AcReqProps.Movement}},
        // Kassatsu
        { 2264, new AcReqProps[]{AcReqProps.None}},
        // Armor Crush
        { 3563, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}}, // leg movemnt fine
        // Huraijin
        { 25876, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Hellfrog Medium
        { 7401, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Ten Chi Jin
        { 7403, new AcReqProps[]{AcReqProps.ArmMovement}},
        // Meisui
        { 16489, new AcReqProps[]{AcReqProps.None}},
        // Bunshin
        { 16493, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Forked Raiju
        { 25777, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Fleeting Raiju
        { 25778, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
    };

    // Collection for Machinist
    public static Dictionary<uint, AcReqProps[]> Machinist = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Head Graze
        { 7551, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
        // Peleton
        { 7557, new AcReqProps[]{AcReqProps.Weighted}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        
        // Dismantle
        { 2887, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Speech}},
        // Tactitian
        { 16889, new AcReqProps[]{AcReqProps.None}},
        
        // Rook Override
        { 7415, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Speech}},
        // Spread Shot
        { 2870, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Bio Blaster
        { 16499, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Gauss Round
        { 2874, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Ricochet
        { 2890, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Reassemble
        { 2876, new AcReqProps[]{AcReqProps.None}},
        // Barrel Stabilizer
        { 7414, new AcReqProps[]{AcReqProps.Sight}},
        // Rook Auto-turret
        { 2864, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Speech}},
        // Hypercharge
        { 17209, new AcReqProps[]{AcReqProps.None}},
        // Wildfire
        { 2878, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Flamethrower
        { 7418, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        
        // Split Shot
        { 2866, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Slug Shot
        { 2868, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Clean Shot
        { 2873, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Drill
        { 16498, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Air Anchor
        { 2872, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Chainsaw
        { 25788, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Heat Blast
        { 7410, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Auto Crossbow
        { 16497, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
    };

    // Collection for DarkKnight
    public static Dictionary<uint, AcReqProps[]> DarkKnight = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Reprisal
        { 7535, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Provoke
        { 7533, new AcReqProps[]{AcReqProps.Sight}},
        // shirk
        { 7537, new AcReqProps[]{AcReqProps.Sight}},
        // interject
        { 7538, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // low blow
        { 7540, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Rampart
        { 7531, new AcReqProps[]{AcReqProps.None}},
        // Hard Slash
        { 3617, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Syphon Strike
        { 3623, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Souleater
        { 3632, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Flood of darkness
        { 16466, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Bloodspiller
        { 7392, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Blood Weapon
        { 3625, new AcReqProps[]{AcReqProps.None}},
        // Shadowbringer
        { 25757, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Unmend
        { 3624, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Delirium
        { 7390, new AcReqProps[]{AcReqProps.None}},
        // plunge
        { 3640, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Abyssal Drain
        { 3641, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Carve and Spit
        { 3643, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Edge of darkness
        { 16467, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Quietus
        { 7391, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Living Shadow
        { 16472, new AcReqProps[]{AcReqProps.Speech}},
        // Salted Earth
        { 3639, new AcReqProps[]{AcReqProps.Sight}},
        // the blackest Night
        { 7393, new AcReqProps[]{AcReqProps.None}},
        // Grit
        { 3629, new AcReqProps[]{AcReqProps.None}},
        // Nebula
        { 3636, new AcReqProps[]{AcReqProps.None}},
        // Dark missionary
        { 16471, new AcReqProps[]{AcReqProps.ArmMovement}},
        // Dark mind
        { 3634, new AcReqProps[]{AcReqProps.ArmMovement}},
        // Oblation
        { 25754, new AcReqProps[]{AcReqProps.Sight}},
        // Unleash
        { 3621, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // stalwart soul
        { 16468, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // livin dead
        { 3638, new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Astrologian
    public static Dictionary<uint, AcReqProps[]> Astrologian = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Esuna
        { 7568, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // SwiftCast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // rescue
        { 7571, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Repose
        { 16560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // earthly star
        { 7439, new AcReqProps[]{AcReqProps.Sight}},
        // Celestial Intersection
        { 16556, new AcReqProps[]{AcReqProps.Sight, AcReqProps.ArmMovement, AcReqProps.Movement}},
        // Horoscope
        { 16557, new AcReqProps[]{AcReqProps.Movement}},
        // neutral sect
        { 16559, new AcReqProps[]{AcReqProps.None}},
        // Exaltation
        { 25873, new AcReqProps[]{AcReqProps.Sight}},
        // Macrocosmos
        { 25874, new AcReqProps[]{AcReqProps.Sight, AcReqProps.Speech}},
        // Benific II
        { 3610, new AcReqProps[]{AcReqProps.None}},
        // Draw
        { 3590, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Movement}},
        // Undraw
        { 9629, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Movement}},
        // Play
        { 17055, new AcReqProps[]{AcReqProps.Sight}},
        // Aspected Benific
        { 3595, new AcReqProps[]{AcReqProps.Sight}},
        // redraw
        { 3593, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Movement}},
        // aspected Helios
        { 3601, new AcReqProps[]{AcReqProps.None}},
        // Gravity
        { 3615, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Synastry
        { 3612, new AcReqProps[]{AcReqProps.Sight}},
        // Divination
        { 16552, new AcReqProps[]{AcReqProps.None}},
        // Astrodye
        { 25870, new AcReqProps[]{AcReqProps.Sight}},
        // Collective Unconscious
        { 3613, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // celestial opposition
        { 16553, new AcReqProps[]{AcReqProps.Speech, AcReqProps.LegMovement}},
        // Minor arcana
        { 7443, new AcReqProps[]{AcReqProps.Sight}},
        // Malefic
        { 3596, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // benefic
        { 3594, new AcReqProps[]{AcReqProps.None}},
        // combust
        { 3599, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // lightspeed
        { 3606, new AcReqProps[]{AcReqProps.Sight}},
        // helios
        { 3600, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // ascend
        { 3603, new AcReqProps[]{AcReqProps.None}},
        // essential dignity
        { 3614, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Movement, AcReqProps.Sight}},
    };

    // Collection for Samurai
    public static Dictionary<uint, AcReqProps[]> Samurai = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Bloodbath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Feint 
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // leg sweep
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // hakaze
        { 7477, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // jinpu
        { 7478, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // enpi
        { 7486, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // shifu
        { 7479, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // fuga
        { 7483, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // gekko
        { 7481, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // ianijutsu
        { 7867, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // mangetsu
        { 7484, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // kasha
        { 7482, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // oka
        { 7485, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // yukikaze
        { 7480, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // hissatsu shinten
        { 7490, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // hissatsu gyoten
        { 7492, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Hissatsu: yaten
        { 7493, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Meditate
        { 7497, new AcReqProps[]{AcReqProps.None}},
        // hisstatsu kyuten
        { 7491, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Hagakure
        { 7495, new AcReqProps[]{AcReqProps.None}},
        // Hissatsu Guren
        { 7496, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // SHoha II
        { 25779, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Ogi Namikiri
        { 25781, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Third Eye
        { 7498, new AcReqProps[]{AcReqProps.Sight}},
    };

    // Collection for RedMage
    public static Dictionary<uint, AcReqProps[]> RedMage = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Sleep
        { 25880, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Swiftcast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Addle
        { 7560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // magick barrier
        { 25857, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // contre sixte
        { 7519, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // fleche 
        { 7517, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Embolden
        { 7520, new AcReqProps[]{AcReqProps.Speech}},
        // manification
        { 7521, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Speech}},
        // Corps-a-corps
        { 7506, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // displacement
        { 7515, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // engagement
        { 16527, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // verraise
        { 7523, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // scatter
        { 7509, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // verthunder II
        { 16524, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Veraero II
        { 16525, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Verstone
        { 7511, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Verfire 
        { 7510, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // vercure
        { 7514, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // moulinet
        { 7513, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Riposte
        { 7504, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Zwerchhau
        { 7512, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // redoublement
        { 7516, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // jolt
        { 7503, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // veraero
        { 7507, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // verthunder
        { 7505, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // accelerate
        { 7528, new AcReqProps[]{AcReqProps.None}},
        // reprise
        { 16529, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
    };

    // Collection for BlueMage
    public static Dictionary<uint, AcReqProps[]> BlueMage = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Gunbreaker
    public static Dictionary<uint, AcReqProps[]> Gunbreaker = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Reprisal
        { 7535, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Provoke
        { 7533, new AcReqProps[]{AcReqProps.Sight}},
        // shirk
        { 7537, new AcReqProps[]{AcReqProps.Sight}},
        // interject
        { 7538, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // low blow
        { 7540, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Rampart
        { 7531, new AcReqProps[]{AcReqProps.None}},
        // keen edge
        { 16137, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // brutal shell
        { 16139, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // solid barrel
        { 16145, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        //gnashing fang
        { 16146, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, }},
        // burst strike
        { 16162, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // no mercy
        { 16138, new AcReqProps[]{AcReqProps.None}},
        // rough divide
        { 16154, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // lightning shot
        { 16143, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // sonic break
        { 16153, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // danger zone
        { 16144, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // bow shock
        { 16159, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // continuation
        { 16155, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // double down
        { 25760, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // bloodfest
        { 16164, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // aurora
        { 16151, new AcReqProps[]{AcReqProps.None}},
        // heart of corundum
        { 16161, new AcReqProps[]{AcReqProps.Sight}},
        // Royal guard
        { 16142, new AcReqProps[]{AcReqProps.None}},
        // superbolide
        { 16152, new AcReqProps[]{AcReqProps.None}},
        // nebula
        { 16148, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Movement}},
        // camoflage
        { 16140, new AcReqProps[]{AcReqProps.None}},
        // heart of light
        { 16160, new AcReqProps[]{AcReqProps.None}},
        // demon slice
        { 16141, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // demon slaughter
        { 16149, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // fated circle
        { 16163, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
    };
    
    // Collection for Dancer
    public static Dictionary<uint, AcReqProps[]> Dancer = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Head Graze
        { 7551, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight}},
        // Peleton
        { 7557, new AcReqProps[]{AcReqProps.Weighted}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Saber Dance
        { 16005, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Flourish
        { 16013, new AcReqProps[]{AcReqProps.None}},
        // Curing Waltz
        { 16015, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // Devilment
        { 16011, new AcReqProps[]{AcReqProps.Speech}},
        // Shield Samba
        { 16012, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // technical step
        { 15998, new AcReqProps[]{AcReqProps.LegMovement}},
        // standard step
        { 15997, new AcReqProps[]{AcReqProps.LegMovement}},
        // Fan Dance III
        { 16009, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Fan Dance
        { 16007, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Reverse Cascade
        { 15991, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Fountainfall
        { 15992, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Fountain
        { 15990, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Cascade
        { 15989, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Windmill
        { 15993, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Bladeshower
        { 15994, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Rising Windmill
        { 15995, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Bloodshower
        { 15996, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Fan Dance II
        { 16008, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Fan Dance IV
        { 25791, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement, AcReqProps.Sight}},
        // Starfall Dance
        { 25792, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Improvisation
        { 16014, new AcReqProps[]{AcReqProps.LegMovement, AcReqProps.Movement}},
        // En Avant
        { 16010, new AcReqProps[]{AcReqProps.LegMovement}},
        // Closed Position
        { 16016, new AcReqProps[]{AcReqProps.None}},
    };

    // Collection for Reaper
    public static Dictionary<uint, AcReqProps[]> Reaper = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Bloodbath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Feint 
        { 7549, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // leg sweep
        { 7863, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Slice
        { 24373, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Waxing Slice
        { 24374, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Infernal Slice
        { 24375, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Gallows
        { 24383, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Gibbet
        { 24382, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // Shadow Of Death
        { 24378, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Harpe
        { 24386, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Arcane Crest
        { 24404, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Enshroud
        { 24394, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // blood stalk
        { 34689, new AcReqProps[]{AcReqProps.Movement}},
        // soul scythe
        { 24381, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // gluttony
        { 24393, new AcReqProps[]{AcReqProps.Movement}},
        // communio
        { 24398, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // plentiful harvest
        { 24385, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // hell's egress
        { 24402, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // hell's ingress
        { 24401, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // arcane circle
        { 24405, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // soul slice
        { 24380, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // grim swathe
        { 24392, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // guillotine
        { 24384, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // whorl of death
        { 24379, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // spinning scythe
        { 24376, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},
        // nightmare scythe
        { 24377, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement}},    
    };

    // Collection for Sage
    public static Dictionary<uint, AcReqProps[]> Sage = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Esuna
        { 7568, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // SwiftCast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // rescue
        { 7571, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Repose
        { 16560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        
        // Pneuma
        { 24318, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // krasis
        { 24317, new AcReqProps[]{AcReqProps.Sight}},
        // Dosis
        { 24283, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // diagnosis
        { 24284, new AcReqProps[]{AcReqProps.None}},
        // Kardia
        { 24285, new AcReqProps[]{AcReqProps.Sight, AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Prognosis
        { 24286, new AcReqProps[]{AcReqProps.None}},
        // egeiro
        { 24287, new AcReqProps[]{AcReqProps.None}},
        // physis
        { 24288, new AcReqProps[]{AcReqProps.Sight}},
        // Phlegma
        { 24289, new AcReqProps[]{AcReqProps.Sight, AcReqProps.Movement, AcReqProps.LegMovement}},
        // Eukrasia
        { 24290, new AcReqProps[]{AcReqProps.Sight}},
        // soteria
        { 24294, new AcReqProps[]{AcReqProps.Sight}},
        // icarus
        { 24295, new AcReqProps[]{AcReqProps.Sight}},
        // druocole
        { 24296, new AcReqProps[]{AcReqProps.Sight, AcReqProps.LegMovement}},
        // dyskrasia
        { 24297, new AcReqProps[]{AcReqProps.None}},
        // kerachole
        { 24298, new AcReqProps[]{AcReqProps.LegMovement}},
        // Ixochole
        { 24299, new AcReqProps[]{AcReqProps.None}},
        // zoe
        { 24300, new AcReqProps[]{AcReqProps.Sight}},
        // pepsis
        { 24301, new AcReqProps[]{AcReqProps.Speech}},
        // taurochole
        { 24303, new AcReqProps[]{AcReqProps.Sight}},
        // toxikon
        { 24304, new AcReqProps[]{AcReqProps.Sight, AcReqProps.Movement, AcReqProps.ArmMovement}},
        // haima
        { 24305, new AcReqProps[]{AcReqProps.Sight}},
        // rhizomata
        { 24309, new AcReqProps[]{AcReqProps.None}},
        // holos
        { 24310, new AcReqProps[]{AcReqProps.None}},
        // panhaima
        { 24311, new AcReqProps[]{AcReqProps.None}},
    };

    public static Dictionary<uint, AcReqProps[]> Viper = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Bloodbath
        { 7542, new AcReqProps[]{AcReqProps.None, AcReqProps.ArmMovement}},
        // Second Wind
        { 7541, new AcReqProps[]{AcReqProps.None}},
        // Feint 
        { 7549, new AcReqProps[]{AcReqProps.ArmMovement}},
        // leg sweep
        { 7863, new AcReqProps[]{AcReqProps.LegMovement}},
        // True North
        { 7546, new AcReqProps[]{AcReqProps.Movement}},
        // Arms Length
        { 7548, new AcReqProps[]{AcReqProps.Movement}},
        // Steel Fangs
        { 34606, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement} },
        // Reaving Fangs
        { 34607, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement} },
        // Vicewinder 
        { 34620, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Serpent's Tail
        { 35920, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Twinblood is 35922
        { 35922, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Twinfang is 35921
        { 35921, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Vicepit is 34623
        { 34623, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement } },
        // Steel Maw is 35614
        { 35614, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement } },
        // Reaving Maw is 34615
        { 34615, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement } },
        // Uncoiled Fury is 34633
        { 34633, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight, AcReqProps.ArmMovement } },
        // Serpent's Ire is 34647
        { 34647, new AcReqProps[]{AcReqProps.ArmMovement } },
        // Swiftskins's Coil is 34622
        { 34622, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Hunter's Coil is 34621
        { 34621, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement } },
        // Reawaken is 34626
        { 34626, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Speech} },
        // Hunter's Den is 34624
        { 34624, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement} },
        // Swiftskin's Den is 34625
        { 34625, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Sight} },
        // Slither is 34646
        { 34646, new AcReqProps[]{AcReqProps.Movement, AcReqProps.Sight } },
        // Writing Snap is 34632
        { 34632, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Sight } },


    };

    public static Dictionary<uint, AcReqProps[]> Pictomancer = new Dictionary<uint, AcReqProps[]>()
    {
        // Sprint
        { 4,    new AcReqProps[]{AcReqProps.Weighted}},
        // Teleport
        { 7,    new AcReqProps[]{AcReqProps.None}},
        // Return
        { 8,    new AcReqProps[]{AcReqProps.None}},
        // Lucid Dreaming
        { 7562, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Sleep
        { 25880, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Surecast
        { 7559, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Swiftcast
        { 7561, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight}},
        // Addle
        { 7560, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement}},
        // Fire in Red is 34650
        { 34650, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement} },
        // Living Muse is 35347
        { 35347, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement} },
        // Blizzard in Cyan is 34653
        { 34653, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Holy in White is 34662
        { 34662, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight } },
        // Comet in Black is 34663
        { 34663, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.Sight} },
        // Hammer Stamp is 34678
        { 34678, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Sight } },
        // Rainbow Drip is 34688
        { 34688, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement, AcReqProps.ArmMovement } },
        // Blizzard II in Cyan is 34659
        { 34659, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Fire II in Red is 34656
        { 34656, new AcReqProps[]{AcReqProps.Movement, AcReqProps.ArmMovement } },
        // Star Prism is 34681
        { 34681, new AcReqProps[]{AcReqProps.LegMovement, AcReqProps.ArmMovement, AcReqProps.Speech } },
        // Mog of the Ages is 34676
        { 34676, new AcReqProps[]{AcReqProps.ArmMovement, AcReqProps.Sight } },
        // Subtractive Palette is 34683
        { 34683, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement } },
        // Steel Muse is 35348 (striking muse == arms, 
        { 35348, new AcReqProps[]{ AcReqProps.ArmMovement } },
        // Scenic Muse is 35349 (starry muse == legs & arms)
        { 35349, new AcReqProps[]{ AcReqProps.Movement, AcReqProps.ArmMovement, AcReqProps.LegMovement } },
        // Tempura Coat is 34685
        { 34685, new AcReqProps[]{AcReqProps.ArmMovement } },
        // Smudge is 34684
        { 34684, new AcReqProps[]{AcReqProps.Movement, AcReqProps.LegMovement } },
        // Pom Motif is 34689
        { 34689, new AcReqProps[]{AcReqProps.ArmMovement } },
        // Hammer Motif is 34690
        { 34690, new AcReqProps[]{AcReqProps.ArmMovement } },
        // Starry Sky Motif is 34691
        { 34691, new AcReqProps[]{AcReqProps.ArmMovement } }, 

    };
}

/*
// CRAFTING STUFF
// Basic Synthesis
{ 100001, new AcReqProps[]{AcReqProps.}},
// Basic Touch
{ 100002, new AcReqProps[]{AcReqProps.}},
// Master's Mend
{ 100003, new AcReqProps[]{AcReqProps.}},
// Hasty Touch
{ 100355, new AcReqProps[]{AcReqProps.}},
// Rapid Synthesis
{ 100363, new AcReqProps[]{AcReqProps.}},
// Observe
{ 100010, new AcReqProps[]{AcReqProps.}},
// Tricks of the Trade
{ 100371, new AcReqProps[]{AcReqProps.}},
// Waste Not
{ 4631, new AcReqProps[]{AcReqProps.}},
// Veneration
{ 19297, new AcReqProps[]{AcReqProps.}},
// Standard Touch
{ 100004, new AcReqProps[]{AcReqProps.}},
// Great Strides
{ 260, new AcReqProps[]{AcReqProps.}},
// Innovation
{ 19004, new AcReqProps[]{AcReqProps.}},
// Final Apprisal
{ 19012, new AcReqProps[]{AcReqProps.}},
// Waste Not II
{ 4639, new AcReqProps[]{AcReqProps.}},
// Byregot's Blessing
{ 100339, new AcReqProps[]{AcReqProps.}},
// Precise Touch
{ 100128, new AcReqProps[]{AcReqProps.}},
// Muscle Memory
{ 100379, new AcReqProps[]{AcReqProps.}},
// Careful Synthesis
{ 100203, new AcReqProps[]{AcReqProps.}},
// Manipulation
{ 4574, new AcReqProps[]{AcReqProps.}},
// Prudent Touch
{ 100227, new AcReqProps[]{AcReqProps.}},
// Advanced Touch
{ 100411, new AcReqProps[]{AcReqProps.}},
// Reflect
{ 100387, new AcReqProps[]{AcReqProps.}},
// Prepatory Touch
{ 100299, new AcReqProps[]{AcReqProps.}},
// Groundwork
{ 100403, new AcReqProps[]{AcReqProps.}},
// Delicate Synthesis
{ 100323, new AcReqProps[]{AcReqProps.}},
// Intensive Synthesis
{ 100315, new AcReqProps[]{AcReqProps.}},
// Trained Eye
{ 100283, new AcReqProps[]{AcReqProps.}},
// Prudent Synthesis
{ 100427, new AcReqProps[]{AcReqProps.}},
// Trained Finesse
{ 100435, new AcReqProps[]{AcReqProps.}},
// Refined Touch
{ 100443, new AcReqProps[]{AcReqProps.}},
// Immaculate Mend
{ 100467, new AcReqProps[]{AcReqProps.}},
// Trained Perfection
{ 100475, new AcReqProps[]{AcReqProps.}},
// Daring Touch
{ 100451, new AcReqProps[]{AcReqProps.}},

// Careful Observation ---- Specialist
{ 100396, new AcReqProps[]{AcReqProps.}},
// Heart and Soul
{ 100420, new AcReqProps[]{AcReqProps.}},

Gathering role:
// Prospect
{ 227, new AcReqProps[]{AcReqProps.}},
// Triangulate
{ 210, new AcReqProps[]{AcReqProps.}},
// Lay of the Land
{ 228, new AcReqProps[]{AcReqProps.}},
// Arbor Call
{ 211, new AcReqProps[]{AcReqProps.}},
// Lay of the Land II
{ 291, new AcReqProps[]{AcReqProps.}},
// Arbor Call II
{ 290, new AcReqProps[]{AcReqProps.}},
// Truth of Mountains
{ 238, new AcReqProps[]{AcReqProps.}},
// Truth of Forests
{ 221, new AcReqProps[]{AcReqProps.}},
// Fathom
{ 7903, new AcReqProps[]{AcReqProps.}},
// Shark Eye
{ 7904, new AcReqProps[]{AcReqProps.}},
// Shark Eye II
{ 7905, new AcReqProps[]{AcReqProps.}},
// Truth of Oceans
{ 79911, new AcReqProps[]{AcReqProps.}},

// -----Miner:
// Sharp Vision
{ 235, new AcReqProps[]{AcReqProps.}},
// Sharp Vision II
{ 237, new AcReqProps[]{AcReqProps.}},
// Sneak
{ 303, new AcReqProps[]{AcReqProps.}},
// Sharp Vission III
{ 295, new AcReqProps[]{AcReqProps.}},
// Mountaineer's Gift I
{ 21177, new AcReqProps[]{AcReqProps.}},
// The Twelve's Bounty
{ 280, new AcReqProps[]{AcReqProps.}},
// Clear Vision
{ 4072, new AcReqProps[]{AcReqProps.}},
// Bountiful Yield
{ 272, new AcReqProps[]{AcReqProps.}},
// Solid Reason
{ 232, new AcReqProps[]{AcReqProps.}},
// King's Yield
{ 239, new AcReqProps[]{AcReqProps.}},
// King's Yield II
{ 241, new AcReqProps[]{AcReqProps.}},
// Collect
{ 240, new AcReqProps[]{AcReqProps.}},
// Scour
{ 22182, new AcReqProps[]{AcReqProps.}},
// Brazen Prospector
{ 22183, new AcReqProps[]{AcReqProps.}},
// Meticulous Prospector
{ 22184, new AcReqProps[]{AcReqProps.}},
// Scruntiny
{ 22185, new AcReqProps[]{AcReqProps.}},
// Mountaineer's Gift II
{ 25589, new AcReqProps[]{AcReqProps.}},
// Luck of the Mounaineer
{ 4081, new AcReqProps[]{AcReqProps.}},
// Bountiful Yield II
{ 272, new AcReqProps[]{AcReqProps.}},
// The Giving Land
{ 4589, new AcReqProps[]{AcReqProps.}},
// Nald'thal's Tidings
{ 21203, new AcReqProps[]{AcReqProps.}},
// Collector's Focus
{ 21205, new AcReqProps[]{AcReqProps.}},
// Wise to the World
{ 26521, new AcReqProps[]{AcReqProps.}},
// Priming Touch
{ 34871, new AcReqProps[]{AcReqProps.}},

// -----Botanist:
// Field Mastery
{ 218, new AcReqProps[]{AcReqProps.}},
// Field Mastery II
{ 220, new AcReqProps[]{AcReqProps.}},
// SneaK
{ 304, new AcReqProps[]{AcReqProps.}},
// Field Mastery III
{ 294, new AcReqProps[]{AcReqProps.}},
// Pioneer's Gift I
{ 21178, new AcReqProps[]{AcReqProps.}},
// The Twelve's Bounty
{ 282, new AcReqProps[]{AcReqProps.}},
// Flora Mastery
{ 4086, new AcReqProps[]{AcReqProps.}},
// Bountiful Harvest
{ 273, new AcReqProps[]{AcReqProps.}},
// Ageless Words
{ 215, new AcReqProps[]{AcReqProps.}},
// Blessed Harvest
{ 222, new AcReqProps[]{AcReqProps.}},
// Blessed Harvest II
{ 224, new AcReqProps[]{AcReqProps.}},
// Collect
{ 815, new AcReqProps[]{AcReqProps.}},
// Scour
{ 22186, new AcReqProps[]{AcReqProps.}},
// Brazen Woodsman
{ 22187, new AcReqProps[]{AcReqProps.}},
// Meticulous Woodsman
{ 22188, new AcReqProps[]{AcReqProps.}},
// Scrutiny
{ 22189, new AcReqProps[]{AcReqProps.}},
// Pioneer's Gift II
{ 25590, new AcReqProps[]{AcReqProps.}},
// Luck of the Pioneer
{ 4095, new AcReqProps[]{AcReqProps.}},
// Bountiful Harvest II
{ 273, new AcReqProps[]{AcReqProps.}},
// The Giving Land
{ 4590, new AcReqProps[]{AcReqProps.}},
// Nophica's Tidings
{ 21204, new AcReqProps[]{AcReqProps.}},
// Collector's Focus
{ 21206, new AcReqProps[]{AcReqProps.}},
// Wise to the World
{ 26522, new AcReqProps[]{AcReqProps.}},
// Priming Touch
{ 34872, new AcReqProps[]{AcReqProps.}},

// -----Fisher:
// Bait
{ 288, new AcReqProps[]{AcReqProps.}},
// Cast
{ 289, new AcReqProps[]{AcReqProps.}},
// Hook
{ 296, new AcReqProps[]{AcReqProps.}},
// Quit
{ 299, new AcReqProps[]{AcReqProps.}},
// Cast Light
{ 2135, new AcReqProps[]{AcReqProps.}},
// Rest
{ 37047, new AcReqProps[]{AcReqProps.}},
// Chum
{ 4104, new AcReqProps[]{AcReqProps.}},
// Sneak
{ 305, new AcReqProps[]{AcReqProps.}},
// Patience
{ 4102, new AcReqProps[]{AcReqProps.}},
// Powerful Hookset
{ 4103, new AcReqProps[]{AcReqProps.}},
// Precision Hookset
{ 4179, new AcReqProps[]{AcReqProps.}},
// Thaliak's Favor
{ 26804, new AcReqProps[]{AcReqProps.}},
// Release
{ 300, new AcReqProps[]{AcReqProps.}},
// Release List
{ 19264, new AcReqProps[]{AcReqProps.}},
// Mooch
{ 297, new AcReqProps[]{AcReqProps.}},
// Snagging
{ 4100, new AcReqProps[]{AcReqProps.}},
// Makeshift Bait
{ 26805, new AcReqProps[]{AcReqProps.}},
// Collect
{ 4101, new AcReqProps[]{AcReqProps.}},
// Fish Eyes
{ 4105, new AcReqProps[]{AcReqProps.}},
// Patience II
{ 4106, new AcReqProps[]{AcReqProps.}},
// Gig
{ 7632, new AcReqProps[]{AcReqProps.}},
// Mooch II
{ 268, new AcReqProps[]{AcReqProps.}},
// Veteran Trade
{ 7906, new AcReqProps[]{AcReqProps.}},
// Vital Sight
{ 26870, new AcReqProps[]{AcReqProps.}},
// Double Hook
{ 269, new AcReqProps[]{AcReqProps.}},
// Salvage
{ 7910, new AcReqProps[]{AcReqProps.}},
// Nature's Bounty
{ 7909, new AcReqProps[]{AcReqProps.}},
// Surface Slap
{ 4595, new AcReqProps[]{AcReqProps.}},
// Baited Breath
{ 26871, new AcReqProps[]{AcReqProps.}},
// Identical Cast
{ 4596, new AcReqProps[]{AcReqProps.}},
// Prize Catch
{ 26806, new AcReqProps[]{AcReqProps.}},
// Electric Current
{ 26872, new AcReqProps[]{AcReqProps.}},
// Triple Hook
{ 27523, new AcReqProps[]{AcReqProps.}},
// Spareful Hand
{ 37045, new AcReqProps[]{AcReqProps.}},
// Big-game Fishing
{ 37046, new AcReqProps[]{AcReqProps.}},
// Ambitious Lure
{ 37594, new AcReqProps[]{AcReqProps.}},
// Modest Lure
{ 37595, new AcReqProps[]{AcReqProps.}},

*/
