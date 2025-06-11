using GagspeakAPI.Attributes;
using System.Collections.Immutable;

namespace GagSpeak.Utils;

// define a dictionary list for each class, and pull from that respective one from upper level action data class
public static class RestrictedActions
{
    public static readonly ImmutableDictionary<uint, Traits> Adventurer = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,    Traits.Weighty) // Sprint
        .Add(7,    Traits.None)    // Teleport
        .Add(8,    Traits.None);   // Return

    public static readonly ImmutableDictionary<uint, Traits> Gladiator = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,    Traits.Weighty)                          // Sprint
        .Add(7,    Traits.None)                             // Teleport
        .Add(8,    Traits.None)                             // Return
        .Add(28,   Traits.Immobile)                         // Toggle Iron Will
        .Add(23,   Traits.Immobile | Traits.BoundArms)      // Circle of Scorn
        .Add(7535, Traits.Immobile)                         // Reprisal
        .Add(7548, Traits.Immobile)                         // Arms Length
        .Add(7533, Traits.Blindfolded)                      // Provoke
        .Add(7537, Traits.Blindfolded)                      // Shirk
        .Add(7538, Traits.Immobile | Traits.BoundLegs)      // Interject
        .Add(7540, Traits.Immobile | Traits.BoundLegs)      // Low Blow
        .Add(7531, Traits.None)                             // Rampart
        .Add(17,   Traits.None)                             // Sentinel
        .Add(9,    Traits.Immobile | Traits.BoundArms)      // Fast Blade
        .Add(15,   Traits.Immobile | Traits.BoundArms)      // Riot Blade
        .Add(21,   Traits.Immobile | Traits.BoundArms)      // Rage of Halone
        .Add(24,   Traits.Immobile | Traits.BoundArms |
                   Traits.Blindfolded | Traits.BoundLegs)   // Shield Lob
        .Add(7381, Traits.Immobile | Traits.BoundArms)      // Total Eclipse
        .Add(16,   Traits.Immobile | Traits.BoundLegs)      // Shield Bash
        .Add(20,   Traits.Immobile | Traits.BoundLegs);     // Fight or Flight

    public static readonly ImmutableDictionary<uint, Traits> Pugilist = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,    Traits.Weighty)                          // Sprint
        .Add(7,    Traits.None)                             // Teleport
        .Add(8,    Traits.None)                             // Return
        .Add(7541, Traits.None)                             // Second Wind
        .Add(7863, Traits.Immobile | Traits.BoundLegs)      // Leg Sweep
        .Add(7542, Traits.BoundArms)                        // Bloodpath
        .Add(7549, Traits.Immobile | Traits.BoundArms)      // Feint
        .Add(7548, Traits.Immobile)                         // Arm's Length
        .Add(7546, Traits.Immobile)                         // True North
        .Add(53,   Traits.Immobile | Traits.BoundArms)      // Bootshine
        .Add(54,   Traits.Immobile | Traits.BoundArms)      // True Strike
        .Add(56,   Traits.Immobile | Traits.BoundArms)      // Snap Punch
        .Add(62,   Traits.Immobile | Traits.BoundArms)      // Arm of the Destroyer
        .Add(61,   Traits.Immobile | Traits.BoundArms)      // Twin Snakes
        .Add(66,   Traits.Immobile | Traits.BoundArms |     // Demolish
                   Traits.BoundLegs)
        .Add(74,   Traits.Immobile | Traits.BoundLegs)      // Dragon Kick
        .Add(36940,Traits.Immobile | Traits.BoundArms)      // Steeled Meditation
        .Add(25761,Traits.Immobile | Traits.BoundArms)      // Steel Peak
        .Add(65,   Traits.Immobile | Traits.BoundArms);     // Mantra

    public static readonly ImmutableDictionary<uint, Traits> Marauder = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,    Traits.Weighty)                          // Sprint
        .Add(7,    Traits.None)                             // Teleport
        .Add(8,    Traits.None)                             // Return
        .Add(7531, Traits.None)                             // Rampart
        .Add(7540, Traits.Immobile | Traits.BoundLegs)      // Low Blow
        .Add(7533, Traits.Blindfolded)                      // Provoke
        .Add(7538, Traits.Immobile | Traits.BoundLegs)      // Interject
        .Add(7535, Traits.Immobile)                         // Reprisal
        .Add(7548, Traits.Immobile)                         // Arm's Length
        .Add(7537, Traits.Blindfolded)                      // Shirk
        .Add(31,   Traits.Immobile | Traits.BoundArms)      // Heavy Swing
        .Add(37,   Traits.Immobile | Traits.BoundArms)      // Maim
        .Add(42,   Traits.Immobile | Traits.BoundArms)      // Storm's Path
        .Add(45,   Traits.Immobile | Traits.BoundArms |     // Storm's Eye
                   Traits.BoundLegs)
        .Add(41,   Traits.Immobile | Traits.BoundArms)      // Overpower
        .Add(46,   Traits.Immobile | Traits.BoundArms |
                   Traits.BoundLegs | Traits.Blindfolded)   // Tomahawk
        .Add(38,   Traits.Immobile)                         // Berserk
        .Add(48,   Traits.None)                             // Defience
        .Add(40,   Traits.None)                             // Thrill of Battle
        .Add(44,   Traits.None)                             // Vengeance
        .Add(43,   Traits.Blindfolded | Traits.Immobile |   // Holmgang
                   Traits.BoundArms);

    public static readonly ImmutableDictionary<uint, Traits> Lancer = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,    Traits.Weighty)                          // Sprint
        .Add(7,    Traits.None)                             // Teleport
        .Add(8,    Traits.None)                             // Return
        .Add(7541, Traits.None)                             // Second Wind
        .Add(7863, Traits.Immobile | Traits.BoundLegs)      // Leg Sweep
        .Add(7542, Traits.BoundArms)                        // Bloodpath
        .Add(7549, Traits.Immobile | Traits.BoundArms)      // Feint
        .Add(7548, Traits.Immobile)                         // Arm's Length
        .Add(7546, Traits.Immobile)                         // True North
        .Add(75,   Traits.Immobile | Traits.BoundArms)      // True Thrust
        .Add(78,   Traits.Immobile)                         // Vorpal Thrust
        .Add(84,   Traits.Immobile | Traits.BoundArms)      // Full Thrust
        .Add(87,   Traits.Immobile | Traits.BoundArms |
                   Traits.BoundLegs)                        // Disembowel
        .Add(88,   Traits.Immobile | Traits.BoundArms)      // Chaos Thrust
        .Add(90,   Traits.Immobile | Traits.BoundArms |
                   Traits.Blindfolded)                      // Piercing Talon
        .Add(83,   Traits.None)                             // Life Surge
        .Add(85,   Traits.None);                            // Lance Change

    public static readonly ImmutableDictionary<uint, Traits> Archer = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7554,  Traits.None)                                                // Leg Graze
        .Add(7541,  Traits.None)                                                // Second Wind
        .Add(7553,  Traits.None)                                                // Foot Graze
        .Add(7557,  Traits.Weighty | Traits.BoundArms)                          // Peloton
        .Add(7551,  Traits.Immobile | Traits.Blindfolded)                       // Head Graze
        .Add(7548,  Traits.Immobile)                                            // Arm's Length
        .Add(97,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Heavy Shot
        .Add(98,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Straight Shot
        .Add(100,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Venomous Bite
        .Add(106,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Quick Nock
        .Add(36974, Traits.Immobile | Traits.BoundLegs | Traits.BoundArms |     // Wide Volley
                    Traits.Blindfolded)
        .Add(113,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Windbite
        .Add(101,   Traits.None)                                                // Raging Strikes
        .Add(110,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Bloodletter
        .Add(112,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Repelling Shot
        .Add(107,   Traits.BoundArms);                                          // Barrage

    public static readonly ImmutableDictionary<uint, Traits> Conjurer = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(16560, Traits.Immobile | Traits.BoundArms)                         // Repose
        .Add(7568,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Esuna
        .Add(7562,  Traits.Immobile | Traits.BoundArms)                         // Lucid Dreaming
        .Add(7561,  Traits.Immobile | Traits.BoundArms)                         // Swiftcase
        .Add(7559,  Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7571,  Traits.Immobile | Traits.BoundArms)                         // Rescue
        .Add(119,   Traits.Immobile | Traits.BoundArms)                         // Stone
        .Add(127,   Traits.Immobile | Traits.BoundArms)                         // Stone II
        .Add(120,   Traits.Immobile | Traits.BoundArms)                         // Cure
        .Add(124,   Traits.Immobile | Traits.BoundArms)                         // Medica
        .Add(125,   Traits.Immobile | Traits.BoundArms)                         // Raise
        .Add(135,   Traits.Immobile | Traits.BoundArms)                         // Cure II
        .Add(121,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Aero
        .Add(132,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Aero II
        .Add(133,   Traits.Immobile | Traits.BoundArms);                        // Medica II

    public static readonly ImmutableDictionary<uint, Traits> Thaumaturge = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7560,  Traits.Immobile | Traits.BoundArms)                         // Addle
        .Add(25880, Traits.Immobile | Traits.BoundArms)                         // Sleep
        .Add(7562,  Traits.Immobile | Traits.BoundArms)                         // Lucid Dreaming
        .Add(7561,  Traits.Immobile | Traits.BoundArms)                         // Swiftcase
        .Add(7559,  Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(142,   Traits.Immobile | Traits.BoundArms)                         // Blizzard
        .Add(141,   Traits.Immobile | Traits.BoundArms)                         // Fire
        .Add(144,   Traits.Immobile | Traits.BoundArms)                         // Thunder
        .Add(25793, Traits.Immobile | Traits.BoundArms)                         // Blizzard II
        .Add(156,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded |   // Scathe
                    Traits.BoundLegs)
        .Add(147,   Traits.Immobile | Traits.BoundArms)                         // Fire II
        .Add(7447,  Traits.Immobile | Traits.BoundArms)                         // Thunder II
        .Add(152,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Fire III
        .Add(149,   Traits.None)                                                // Transpose
        .Add(157,   Traits.Immobile | Traits.BoundArms)                         // Manaward
        .Add(155,   Traits.Immobile | Traits.Blindfolded);                      // Aetherial Manipulation

    public static readonly ImmutableDictionary<uint, Traits> Paladin = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(28,    Traits.Immobile)                                            // Toggle Iron Will
        .Add(23,    Traits.Immobile | Traits.BoundArms)                         // Circle of Scorn
        .Add(7535,  Traits.Immobile)                                            // Reprisal
        .Add(7548,  Traits.Immobile)                                            // Arms Length
        .Add(7533,  Traits.Blindfolded)                                         // Provoke
        .Add(7537,  Traits.Blindfolded)                                         // Shirk
        .Add(7538,  Traits.Immobile | Traits.BoundLegs)                         // Interject
        .Add(7540,  Traits.Immobile | Traits.BoundLegs)                         // Low Blow
        .Add(7531,  Traits.None)                                                // Rampart
        .Add(17,    Traits.None)                                                // Sentinel
        .Add(9,     Traits.Immobile | Traits.BoundArms)                         // Fast Blade
        .Add(15,    Traits.Immobile | Traits.BoundArms)                         // Riot Blade
        .Add(21,    Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Rage of Halone
        .Add(3539,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Royal Authority
        .Add(24,    Traits.Immobile | Traits.BoundArms | Traits.BoundLegs |
                    Traits.Blindfolded)                                         // Shield Lob
        .Add(7381,  Traits.Immobile | Traits.BoundLegs)                         // Total Eclipse
        .Add(16,    Traits.Immobile | Traits.BoundLegs)                         // Shield Bash
        .Add(20,    Traits.None)                                                // Fight or Flight
        .Add(3538,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Goring Blade
        .Add(16461, Traits.Immobile | Traits.BoundLegs)                         // Intervene
        .Add(16460, Traits.Immobile | Traits.BoundArms)                         // Atonement
        .Add(7384,  Traits.Immobile | Traits.BoundArms | Traits.Gagged)         // Holy Spirit
        .Add(29,    Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Spirits Within (Expiacition)
        .Add(7383,  Traits.Immobile | Traits.BoundArms | Traits.Gagged)         // Requiescat
        .Add(16458, Traits.Immobile | Traits.BoundArms | Traits.Gagged)         // Holy Circle
        .Add(3541,  Traits.Immobile | Traits.BoundArms | Traits.Gagged)         // Clemency
        .Add(16459, Traits.Immobile | Traits.BoundArms | Traits.Gagged)         // Confiteor
        .Add(3542,  Traits.Immobile | Traits.BoundArms)                         // Sheltron
        .Add(22,    Traits.None)                                                // Bulwark
        .Add(3540,  Traits.Immobile | Traits.BoundArms)                         // Divine Veil
        .Add(27,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Cover
        .Add(16457, Traits.Immobile | Traits.BoundArms)                         // Prominence
        .Add(30,    Traits.None)                                                // Hallowed Ground
        .Add(7385,  Traits.Immobile | Traits.BoundArms)                         // Passage of Arms
        .Add(7382,  Traits.Immobile | Traits.BoundLegs | Traits.Blindfolded);   // Intervention

    public static readonly ImmutableDictionary<uint, Traits> Monk = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7542,  Traits.BoundArms)                                           // Bloodbath
        .Add(7541,  Traits.None)                                                // Second Wind
        .Add(7549,  Traits.Immobile | Traits.BoundArms)                         // Feint
        .Add(7863,  Traits.Immobile | Traits.BoundLegs)                         // Stun
        .Add(7546,  Traits.Immobile)                                            // True North
        .Add(7548,  Traits.Immobile)                                            // Arm's Length
        .Add(53,    Traits.Immobile | Traits.BoundArms)                         // Bootshine
        .Add(54,    Traits.Immobile | Traits.BoundArms)                         // True Strike
        .Add(56,    Traits.Immobile | Traits.BoundArms)                         // Snap Punch
        .Add(16473, Traits.Immobile | Traits.BoundArms)                         // Four-point Fury
        .Add(74,    Traits.Immobile | Traits.BoundLegs)                         // Dragon Kick
        .Add(62,    Traits.Immobile | Traits.BoundLegs)                         // Arm of the Destroyer (shadow)
        .Add(66,    Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Demolish
        .Add(70,    Traits.Immobile | Traits.BoundArms)                         // Rockbreaker
        .Add(25763, Traits.Immobile | Traits.BoundArms)                         // Howling Fist
        .Add(61,    Traits.Immobile | Traits.BoundArms)                         // Twin Snakes (legs fine)
        .Add(16476, Traits.Immobile | Traits.BoundLegs)                         // Six-sided Star
        .Add(25762, Traits.Immobile | Traits.BoundLegs | Traits.Blindfolded)    // Thunderclap
        .Add(69,    Traits.None)                                                // Perfect Balance
        .Add(4262,  Traits.Immobile | Traits.BoundLegs)                         // Form Shift
        .Add(25764, Traits.Immobile)                                            // Masterful Blitz
        .Add(7394,  Traits.Immobile)                                            // Riddle of Earth
        .Add(7395,  Traits.Immobile)                                            // Riddle of Fire
        .Add(25766, Traits.Immobile | Traits.BoundLegs)                         // Riddle of Wind
        .Add(3546,  Traits.Immobile | Traits.BoundArms)                         // Meditation
        .Add(7396,  Traits.Immobile | Traits.BoundArms)                         // Brotherhood
        .Add(65,    Traits.Immobile | Traits.BoundArms)                         // Mantra
        .Add(16475, Traits.Immobile);                                           // Anatman

    public static readonly ImmutableDictionary<uint, Traits> Warrior = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7535,  Traits.Immobile)                                            // Reprisal
        .Add(7548,  Traits.Immobile)                                            // Arms Length
        .Add(7533,  Traits.Blindfolded)                                         // Provoke
        .Add(7537,  Traits.Blindfolded)                                         // Shirk
        .Add(7538,  Traits.Immobile | Traits.BoundLegs)                         // Interject
        .Add(7540,  Traits.Immobile | Traits.BoundLegs)                         // Low Blow
        .Add(7531,  Traits.None)                                                // Rampart
        .Add(25753, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs |     // Primal Rend
                    Traits.Blindfolded)
        .Add(25752, Traits.Immobile | Traits.BoundArms)                         // Orogeny
        .Add(16464, Traits.Blindfolded)                                         // Nascent Flash
        .Add(7388,  Traits.None)                                                // Shake it Off
        .Add(7387,  Traits.Immobile | Traits.BoundArms)                         // Upheaval
        .Add(7386,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms |     // Onslaught
                    Traits.Blindfolded)
        .Add(3552,  Traits.None)                                                // Equilibrium
        .Add(3551,  Traits.None)                                                // Raw Intuition
        .Add(52,    Traits.Immobile | Traits.BoundArms)                         // Infuriate
        .Add(45,    Traits.Immobile | Traits.BoundArms |                        // Storm's Eye
                    Traits.BoundLegs)
        .Add(51,    Traits.Immobile | Traits.BoundArms)                         // Steel Cyclone
        .Add(43,    Traits.Blindfolded | Traits.Immobile |                      // Holmgang
                    Traits.BoundArms)
        .Add(16462, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Mythril Tempest
        .Add(44,    Traits.None)                                                // Vengeance
        .Add(49,    Traits.Immobile | Traits.BoundArms)                         // Inner Beast
        .Add(31,    Traits.Immobile | Traits.BoundArms)                         // Heavy Swing
        .Add(37,    Traits.Immobile | Traits.BoundArms)                         // Maim
        .Add(38,    Traits.Immobile)                                            // Berserk
        .Add(48,    Traits.None)                                                // Defiance
        .Add(41,    Traits.Immobile | Traits.BoundArms)                         // Overpower
        .Add(46,    Traits.Immobile | Traits.BoundArms | Traits.BoundLegs |     // Tomahawk
                    Traits.Blindfolded)
        .Add(42,    Traits.Immobile | Traits.BoundArms)                         // Storm's Path
        .Add(40,    Traits.None);                                               // Thrill of Battle

    public static readonly ImmutableDictionary<uint, Traits> Dragoon = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7542,  Traits.BoundArms)                                           // Bloodbath
        .Add(7541,  Traits.None)                                                // Second Wind
        .Add(7549,  Traits.Immobile | Traits.BoundArms)                         // Feint
        .Add(7863,  Traits.Immobile | Traits.BoundLegs)                         // Stun
        .Add(7546,  Traits.Immobile)                                            // True North
        .Add(7548,  Traits.Immobile)                                            // Arm's Length
        .Add(75,    Traits.Immobile | Traits.BoundArms)                         // True Thrust
        .Add(78,    Traits.Immobile)                                            // Vorpal Thrust
        .Add(84,    Traits.Immobile | Traits.BoundArms)                         // Heavens' Thrust
        .Add(87,    Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Disembowel
        .Add(88,    Traits.Immobile | Traits.BoundArms)                         // Chaos Thrust
        .Add(3554,  Traits.Immobile | Traits.BoundArms)                         // Fang and Claw
        .Add(3556,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Wheeling Thrust
        .Add(92,    Traits.Immobile | Traits.BoundArms)                         // Jump
        .Add(7399,  Traits.Immobile | Traits.Gagged | Traits.Blindfolded)       // Mirage Dive
        .Add(95,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Spineshatter Dive
        .Add(96,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Dragonfire Dive
        .Add(3555,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Geirskogul
        .Add(16480, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs |     // Stardiver
                    Traits.Blindfolded)
        .Add(25773, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs |     // Wyrmwind Thrust
                    Traits.Blindfolded)
        .Add(90,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Piercing Talon
        .Add(94,    Traits.Immobile | Traits.Blindfolded)                       // Elusive Jump
        .Add(3557,  Traits.Gagged)                                              // Battle Litany
        .Add(7398,  Traits.BoundArms | Traits.Blindfolded)                      // Dragon Sight
        .Add(86,    Traits.Immobile | Traits.BoundArms)                         // Doom Spike
        .Add(7397,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Sonic Thrust
        .Add(16477, Traits.Immobile | Traits.BoundArms)                         // Coerthan Torment
        .Add(85,    Traits.None)                                                // Lance Charge
        .Add(82,    Traits.None);                                               // Life Surge

    public static readonly ImmutableDictionary<uint, Traits> Bard = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7548,  Traits.Immobile)                                            // Arms Length
        .Add(7551,  Traits.Immobile | Traits.Blindfolded)                       // Head Graze
        .Add(7557,  Traits.Weighty)                                             // Peloton
        .Add(7541,  Traits.None)                                                // Second Wind
        .Add(97,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Heavy Shot
        .Add(98,    Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Straight Shot
        .Add(101,   Traits.None)                                                // Raging Strikes
        .Add(100,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Venomous Bite
        .Add(110,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Bloodletter
        .Add(112,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Repelling Shot
        .Add(106,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Quick Nock
        .Add(113,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Windbite
        .Add(114,   Traits.Gagged)                                              // Mage's Ballad
        .Add(3561,  Traits.Blindfolded)                                         // The Warden's Paean
        .Add(107,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Barrage
        .Add(116,   Traits.Gagged)                                              // Army's Paeon
        .Add(117,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Rain of Death
        .Add(118,   Traits.Gagged)                                              // Battle Voice
        .Add(3559,  Traits.Gagged | Traits.BoundArms | Traits.Immobile)         // The Warden's Minuet
        .Add(3558,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Empyreal Arrow
        .Add(3560,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Iron Jaws
        .Add(3562,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Sidewinder
        .Add(7405,  Traits.None)                                                // Troubadour
        .Add(7408,  Traits.Gagged)                                              // Nature's Minne
        .Add(16494, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Shadowbite
        .Add(16496, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Apex Arrow
        .Add(25785, Traits.Gagged);                                             // Radiant Finale

    public static readonly ImmutableDictionary<uint, Traits> WhiteMage = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(135,   Traits.Immobile | Traits.BoundArms)                         // Cure II
        .Add(3570,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Tetragrammaton
        .Add(16531, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Afflatus Solace
        .Add(137,   Traits.Immobile | Traits.BoundArms)                         // Regen
        .Add(7432,  Traits.Immobile | Traits.BoundLegs | Traits.Blindfolded)    // Divine Benison
        .Add(16535, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Afflatus Misery
        .Add(119,   Traits.Immobile | Traits.BoundArms)                         // Stone / Glare
        .Add(121,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Aero / Dia
        .Add(7562,  Traits.Immobile | Traits.BoundArms)                         // Lucid Dreaming
        .Add(133,   Traits.Immobile | Traits.BoundArms)                         // Medica II
        .Add(16534, Traits.None)                                                // Afflatus Rapture
        .Add(124,   Traits.Immobile | Traits.BoundArms)                         // Medica
        .Add(131,   Traits.Immobile | Traits.BoundArms)                         // Cure III
        .Add(25861, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Aqua Veil
        .Add(3571,  Traits.Immobile | Traits.BoundArms)                         // Assize
        .Add(140,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Benediction
        .Add(7568,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Esuna
        .Add(7561,  Traits.Immobile | Traits.BoundArms)                         // Swiftcast
        .Add(125,   Traits.Immobile | Traits.BoundArms)                         // Raise
        .Add(7559,  Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7571,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Rescue
        .Add(16560, Traits.Immobile | Traits.BoundArms)                         // Repose
        .Add(136,   Traits.Immobile | Traits.BoundArms)                         // Presence of Mind
        .Add(7430,  Traits.Immobile | Traits.BoundArms)                         // Thin Air
        .Add(7433,  Traits.Immobile | Traits.BoundArms)                         // Plenary Indulgence
        .Add(16536, Traits.Immobile | Traits.BoundArms)                         // Temperance
        .Add(3569,  Traits.Immobile | Traits.BoundArms)                         // Asylum
        .Add(139,   Traits.Immobile | Traits.BoundArms)                         // Holy
        .Add(25862, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Liturgy of the Bell
        .Add(120,   Traits.Immobile | Traits.BoundArms);                        // Cure

    public static readonly ImmutableDictionary<uint, Traits> BlackMage = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                                       // Sprint
        .Add(7,     Traits.None)                                                          // Teleport
        .Add(8,     Traits.None)                                                          // Return
        .Add(7562,  Traits.Immobile | Traits.BoundArms)                                   // Lucid Dreaming
        .Add(25880, Traits.Immobile | Traits.BoundArms)                                   // Sleep
        .Add(7559,  Traits.Immobile | Traits.BoundArms)                                   // Surecast
        .Add(7561,  Traits.Immobile | Traits.BoundArms)                                   // Swiftcast
        .Add(7560,  Traits.Immobile | Traits.BoundArms)                                   // Addle
        .Add(3577,  Traits.Immobile | Traits.BoundArms)                                   // Fire IV
        .Add(141,   Traits.Immobile | Traits.BoundArms)                                   // Fire (Paradox)
        .Add(152,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)                // Fire III (too much leg lean)
        .Add(16505, Traits.Immobile | Traits.BoundArms)                                   // Despair
        .Add(16507, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Xenoglossy (too much lean)
        .Add(7419,  Traits.Immobile | Traits.Blindfolded)                                 // Between the Lines
        .Add(16506, Traits.Immobile | Traits.BoundLegs)                                   // Umbral Soul
        .Add(154,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)                // Blizzard III (too much lean)
        .Add(3576,  Traits.Immobile | Traits.BoundArms)                                   // Blizzard IV
        .Add(144,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)                // Thunder (too much leg)
        .Add(158,   Traits.Immobile | Traits.BoundArms)                                   // Manafont
        .Add(25793, Traits.Immobile | Traits.BoundArms)                                   // Blizzard II
        .Add(159,   Traits.Immobile | Traits.BoundArms)                                   // Freeze
        .Add(147,   Traits.Immobile | Traits.BoundArms)                                   // Fire II
        .Add(162,   Traits.Immobile | Traits.BoundArms)                                   // Flare
        .Add(149,   Traits.None)                                                          // Transpose
        .Add(3574,  Traits.Immobile | Traits.BoundArms)                                   // Sharpcast
        .Add(3573,  Traits.Immobile | Traits.BoundArms)                                   // Ley Lines
        .Add(7447,  Traits.Immobile | Traits.BoundArms)                                   // Thunder II
        .Add(25796, Traits.Immobile | Traits.BoundArms)                                   // Amplifier
        .Add(7422,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Foul
        .Add(156,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Scathe
        .Add(155,   Traits.Immobile | Traits.Blindfolded)                                 // Aetherial Manipulation
        .Add(7421,  Traits.None)                                                          // Triplecast
        .Add(157,   Traits.Immobile | Traits.BoundArms);                                  // Manaward

    public static readonly ImmutableDictionary<uint, Traits> Arcanist = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                               // Sprint
        .Add(7,     Traits.None)                                                  // Teleport
        .Add(8,     Traits.None)                                                  // Return
        .Add(7560,  Traits.Immobile | Traits.BoundArms)                           // Addle
        .Add(25880, Traits.Immobile | Traits.BoundArms)                           // Sleep
        .Add(7562,  Traits.Immobile | Traits.BoundArms)                           // Lucid Dreaming
        .Add(7561,  Traits.Immobile | Traits.BoundArms)                           // Swiftcast
        .Add(7559,  Traits.Immobile | Traits.BoundArms)                           // Surecast
        .Add(25798, Traits.Immobile | Traits.BoundArms)                           // Summon Carbuncle
        .Add(16230, Traits.Gagged | Traits.Blindfolded)                           // Physick
        .Add(25800, Traits.Gagged | Traits.Blindfolded)                           // Aethercharge
        .Add(25802, Traits.Gagged)                                                // Summon Ruby
        .Add(25883, Traits.Blindfolded)                                           // Gemshine
        .Add(173,   Traits.Blindfolded)                                           // Resurrection
        .Add(25803, Traits.Gagged)                                                // Summon Topaz
        .Add(25804, Traits.Gagged)                                                // Summon Emerald
        .Add(16511, Traits.Immobile | Traits.BoundArms)                           // Outburst
        .Add(258884,Traits.Blindfolded)                                           // Precious Brilliance
        .Add(172,   Traits.Immobile | Traits.BoundArms)                           // Ruin II
        .Add(163,   Traits.Immobile | Traits.BoundArms)                           // Ruin
        .Add(25799, Traits.None)                                                  // Radiant Aegis
        .Add(181,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Fester
        .Add(16508, Traits.Blindfolded);                                          // Energy Drain

    public static readonly ImmutableDictionary<uint, Traits> Summoner = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7562,  Traits.Immobile | Traits.BoundArms)                         // Lucid Dreaming
        .Add(25880, Traits.Immobile | Traits.BoundArms)                         // Sleep
        .Add(7559,  Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7561,  Traits.Immobile | Traits.BoundArms)                         // Swiftcast
        .Add(7560,  Traits.Immobile | Traits.BoundArms)                         // Addle
        .Add(25798, Traits.Immobile | Traits.BoundArms)                         // Summon Carbuncle
        .Add(25801, Traits.Gagged)                                              // Searing Light
        .Add(25799, Traits.None)                                                // Radiant Aegis
        .Add(173,   Traits.Blindfolded)                                         // Resurrection
        .Add(16511, Traits.Immobile | Traits.BoundArms)                         // Outburst
        .Add(3578,  Traits.Immobile | Traits.Blindfolded)                       // Painflare
        .Add(16230, Traits.Gagged | Traits.Blindfolded)                         // Physick
        .Add(25822, Traits.Gagged | Traits.Blindfolded)                         // Astral Flow
        .Add(25884, Traits.Blindfolded)                                         // Precious Brilliance
        .Add(25883, Traits.Blindfolded)                                         // Gemshine
        .Add(7426,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Ruin IV
        .Add(16508, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Energy Drain
        .Add(16510, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Energy Siphon
        .Add(163,   Traits.Immobile | Traits.BoundArms)                         // Ruin III
        .Add(25804, Traits.Gagged)                                              // Summon Emerald
        .Add(25803, Traits.Gagged)                                              // Summon Topaz
        .Add(25802, Traits.Gagged)                                              // Summon Ruby
        .Add(181,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Fester
        .Add(25800, Traits.Gagged | Traits.Blindfolded)                         // Aethercharge
        .Add(7429,  Traits.Blindfolded);                                        // Enkindle Bahamut

    public static readonly ImmutableDictionary<uint, Traits> Scholar = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7568,  Traits.Immobile | Traits.BoundArms)                         // Esuna
        .Add(7561,  Traits.Immobile | Traits.BoundArms)                         // Swiftcast
        .Add(125,   Traits.Immobile | Traits.BoundArms)                         // Raise
        .Add(7559,  Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7571,  Traits.Immobile | Traits.BoundArms)                         // Rescue
        .Add(16560, Traits.Immobile | Traits.BoundArms)                         // Repose
        .Add(166,   Traits.Immobile | Traits.Gagged)                            // Aetherflow
        .Add(167,   Traits.Immobile | Traits.BoundArms)                         // Energy Drain
        .Add(169,   Traits.Gagged)                                              // Lustrate
        .Add(16539, Traits.Immobile | Traits.BoundArms)                         // Art of War
        .Add(188,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded |   // Sacred Soil
                    Traits.Gagged)
        .Add(3583,  Traits.Immobile | Traits.BoundArms)                         // Indomitability
        .Add(3585,  Traits.Immobile | Traits.BoundArms)                         // Deployment Tactics
        .Add(3586,  Traits.Immobile | Traits.BoundArms)                         // Emergency Tactics
        .Add(7434,  Traits.Blindfolded)                                         // Excogitation
        .Add(7436,  Traits.Gagged)                                              // Chain Stratagem
        .Add(7437,  Traits.Gagged)                                              // Aetherpact
        .Add(16542, Traits.Immobile | Traits.BoundArms)                         // Recitation
        .Add(17869, Traits.Immobile | Traits.BoundArms)                         // Ruin I
        .Add(17864, Traits.Blindfolded)                                         // Bio I
        .Add(190,   Traits.Gagged | Traits.Blindfolded)                         // Psysick
        .Add(17215, Traits.Gagged)                                              // Summon Eos
        .Add(16537, Traits.Gagged)                                              // Whispering Dawn
        .Add(185,   Traits.Blindfolded)                                         // Adloquium
        .Add(186,   Traits.None)                                                // Succor
        .Add(17870, Traits.Blindfolded)                                         // Ruin II
        .Add(16538, Traits.Gagged)                                              // Fey Illumination
        .Add(16543, Traits.Gagged)                                              // Fey Blessing
        .Add(16545, Traits.Gagged)                                              // Summon Seraph
        .Add(25867, Traits.None)                                                // Protection
        .Add(25868, Traits.None);                                               // Expedient

    public static readonly ImmutableDictionary<uint, Traits> Rogue = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,    Traits.Weighty)                                          // Sprint
        .Add(7,    Traits.None)                                             // Teleport
        .Add(8,    Traits.None)                                             // Return
        .Add(7541, Traits.None)                                             // Second Wind
        .Add(7863, Traits.Immobile | Traits.BoundLegs)                      // Leg Sweep
        .Add(7542, Traits.BoundArms)                                        // Bloodbath
        .Add(7549, Traits.Immobile | Traits.BoundArms)                      // Feint
        .Add(7548, Traits.Immobile)                                         // Arm's Length
        .Add(7546, Traits.Immobile)                                         // True North
        .Add(2240, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)   // Spinning Edge
        .Add(2242, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)   // Gust Slash
        .Add(2255, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)   // Aeolian Edge
        .Add(2247, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded) // Throwing Dagger
        .Add(2254, Traits.Immobile | Traits.BoundArms)                      // Death Blossom
        .Add(2241, Traits.None)                                             // Shade Shift
        .Add(2245, Traits.None)                                             // Hide
        .Add(2248, Traits.Immobile | Traits.BoundArms)                      // Mug
        .Add(2258, Traits.Immobile);                                        // Trick Attack

    public static readonly ImmutableDictionary<uint, Traits> Ninja = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                             // Sprint
        .Add(7,     Traits.None)                                                // Teleport
        .Add(8,     Traits.None)                                                // Return
        .Add(7542,  Traits.None)                                                // Bloodbath
        .Add(7541,  Traits.None)                                                // Second Wind
        .Add(7549,  Traits.Immobile | Traits.BoundArms)                         // Feint
        .Add(7863,  Traits.Immobile | Traits.BoundLegs)                         // Leg Sweep
        .Add(7546,  Traits.Immobile)                                            // True North
        .Add(7548,  Traits.Immobile)                                            // Arms Length
        .Add(2240,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Spinning Edge
        .Add(2242,  Traits.Immobile | Traits.BoundLegs)                         // Gust Slash
        .Add(2247,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Throwing Dagger
        .Add(2248,  Traits.Immobile | Traits.BoundArms)                         // Mug
        .Add(2258,  Traits.Immobile)                                            // Trick Attack
        .Add(2255,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Aeolian Edge
        .Add(2259,  Traits.Immobile | Traits.BoundArms)                         // Ten
        .Add(2261,  Traits.Immobile | Traits.BoundArms)                         // Chi
        .Add(2263,  Traits.Immobile | Traits.BoundArms)                         // Jin
        .Add(2260,  Traits.Immobile | Traits.BoundArms)                         // Ninjutsu
        .Add(2254,  Traits.Immobile | Traits.BoundArms)                         // Death Blossom
        .Add(2246,  Traits.Immobile | Traits.BoundArms)                         // Assassinate
        .Add(2262,  Traits.Immobile)                                            // Shukuchi
        .Add(2264,  Traits.None)                                                // Kassatsu
        .Add(3563,  Traits.Immobile | Traits.BoundArms)                         // Armor Crush
        .Add(25876, Traits.Immobile | Traits.BoundArms)                         // Huraijin
        .Add(7401,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Hellfrog Medium
        .Add(7403,  Traits.BoundArms)                                           // Ten Chi Jin
        .Add(16489, Traits.None)                                                // Meisui
        .Add(16493, Traits.Immobile | Traits.BoundArms)                         // Bunshin
        .Add(25777, Traits.Immobile | Traits.BoundLegs | Traits.Blindfolded)    // Forked Raiju
        .Add(25778, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded);   // Fleeting Raiju

    public static readonly ImmutableDictionary<uint, Traits> Machinist = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,     Traits.Weighty)                                               // Sprint
        .Add(7,     Traits.None)                                                  // Teleport
        .Add(8,     Traits.None)                                                  // Return
        .Add(7548,  Traits.Immobile)                                              // Arm's Length
        .Add(7551,  Traits.Immobile | Traits.Blindfolded)                         // Head Graze
        .Add(7557,  Traits.Weighty)                                               // Peloton
        .Add(7541,  Traits.None)                                                  // Second Wind
        .Add(2887,  Traits.Immobile | Traits.Gagged)                              // Dismantle
        .Add(16889, Traits.None)                                                  // Tactician
        .Add(7415,  Traits.Immobile | Traits.Gagged)                              // Rook Overdrive
        .Add(2870,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)        // Spread Shot
        .Add(16499, Traits.Immobile | Traits.BoundArms)                           // Bio Blaster
        .Add(2874,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Gauss Round
        .Add(2890,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Ricochet
        .Add(2876,  Traits.None)                                                  // Reassemble
        .Add(7414,  Traits.Blindfolded)                                           // Barrel Stabilizer
        .Add(2864,  Traits.Immobile | Traits.BoundArms | Traits.Gagged)           // Rook Auto-turret
        .Add(17209, Traits.None)                                                  // Hypercharge
        .Add(2878,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Wildfire
        .Add(7418,  Traits.Immobile | Traits.BoundArms)                           // Flamethrower
        .Add(2866,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded)  // Split Shot
        .Add(2868,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded)  // Slug Shot
        .Add(2873,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Clean Shot
        .Add(16498, Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Drill
        .Add(2872,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Air Anchor
        .Add(25788, Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded)  // Chainsaw
        .Add(7410,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)      // Heat Blast
        .Add(16497, Traits.Immobile | Traits.BoundArms);                          // Auto Crossbow

    public static readonly ImmutableDictionary<uint, Traits> DarkKnight = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7535,   Traits.Immobile)                                            // Reprisal
        .Add(7548,   Traits.Immobile)                                            // Arm's Length
        .Add(7533,   Traits.Blindfolded)                                         // Provoke
        .Add(7537,   Traits.Blindfolded)                                         // Shirk
        .Add(7538,   Traits.Immobile | Traits.BoundLegs)                         // Interject
        .Add(7540,   Traits.Immobile | Traits.BoundLegs)                         // Low Blow
        .Add(7531,   Traits.None)                                                // Rampart
        .Add(3617,   Traits.Immobile | Traits.BoundArms)                         // Hard Slash
        .Add(3623,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Syphon Strike
        .Add(3632,   Traits.Immobile | Traits.BoundArms)                         // Souleater
        .Add(16466,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Flood of Darkness
        .Add(7392,   Traits.Immobile | Traits.BoundArms)                         // Bloodspiller
        .Add(3625,   Traits.None)                                                // Blood Weapon
        .Add(25757,  Traits.Immobile | Traits.BoundArms)                         // Shadowbringer
        .Add(3624,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Unmend
        .Add(7390,   Traits.None)                                                // Delirium
        .Add(3640,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Plunge
        .Add(3641,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Abyssal Drain
        .Add(3643,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Carve and Spit
        .Add(16467,  Traits.Immobile | Traits.BoundArms)                         // Edge of Darkness
        .Add(7391,   Traits.Immobile | Traits.BoundArms)                         // Quietus
        .Add(16472,  Traits.Gagged)                                              // Living Shadow
        .Add(3639,   Traits.Blindfolded)                                         // Salted Earth
        .Add(7393,   Traits.None)                                                // The Blackest Night
        .Add(3629,   Traits.None)                                                // Grit
        .Add(3636,   Traits.None)                                                // Nebula
        .Add(16471,  Traits.BoundArms)                                           // Dark Missionary
        .Add(3634,   Traits.BoundArms)                                           // Dark Mind
        .Add(25754,  Traits.Blindfolded)                                         // Oblation
        .Add(3621,   Traits.Immobile | Traits.BoundArms)                         // Unleash
        .Add(16468,  Traits.Immobile | Traits.BoundArms)                         // Stalwart Soul
        .Add(3638,   Traits.None);                                               // Living Dead

    public static readonly ImmutableDictionary<uint, Traits> Astrologian = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7568,   Traits.Immobile | Traits.BoundArms)                         // Esuna
        .Add(7561,   Traits.Immobile | Traits.BoundArms)                         // Swiftcast
        .Add(7559,   Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7571,   Traits.Immobile | Traits.BoundArms)                         // Rescue
        .Add(16560,  Traits.Immobile | Traits.BoundArms)                         // Repose
        .Add(7439,   Traits.Blindfolded)                                         // Earthly Star
        .Add(16556,  Traits.Blindfolded | Traits.BoundArms | Traits.Immobile)    // Celestial Intersection
        .Add(16557,  Traits.Immobile)                                            // Horoscope
        .Add(16559,  Traits.None)                                                // Neutral Sect
        .Add(25873,  Traits.Blindfolded)                                         // Exaltation
        .Add(25874,  Traits.Blindfolded | Traits.Gagged)                         // Macrocosmos
        .Add(3610,   Traits.None)                                                // Benific II
        .Add(3590,   Traits.BoundArms | Traits.Immobile)                         // Draw
        .Add(9629,   Traits.BoundArms | Traits.Immobile)                         // Undraw
        .Add(17055,  Traits.Blindfolded)                                         // Play
        .Add(3595,   Traits.Blindfolded)                                         // Aspected Benific
        .Add(3593,   Traits.BoundArms | Traits.Immobile)                         // Redraw
        .Add(3601,   Traits.None)                                                // Aspected Helios
        .Add(3615,   Traits.Immobile | Traits.BoundArms)                         // Gravity
        .Add(3612,   Traits.Blindfolded)                                         // Synastry
        .Add(16552,  Traits.None)                                                // Divination
        .Add(25870,  Traits.Blindfolded)                                         // Astrodye
        .Add(3613,   Traits.Immobile | Traits.BoundArms)                         // Collective Unconscious
        .Add(16553,  Traits.Gagged | Traits.BoundLegs)                           // Celestial Opposition
        .Add(7443,   Traits.Blindfolded)                                         // Minor Arcana
        .Add(3596,   Traits.Immobile | Traits.BoundArms)                         // Malefic
        .Add(3594,   Traits.None)                                                // Benefic
        .Add(3599,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Combust
        .Add(3606,   Traits.Blindfolded)                                         // Lightspeed
        .Add(3600,   Traits.Immobile | Traits.BoundArms)                         // Helios
        .Add(3603,   Traits.None)                                                // Ascend
        .Add(3614,   Traits.BoundArms | Traits.Immobile | Traits.Blindfolded);   // Essential Dignity

    public static readonly ImmutableDictionary<uint, Traits> Samurai = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7542,   Traits.BoundArms)                                           // Bloodbath
        .Add(7541,   Traits.None)                                                // Second Wind
        .Add(7549,   Traits.Immobile | Traits.BoundArms)                         // Feint
        .Add(7863,   Traits.Immobile | Traits.BoundLegs)                         // Leg Sweep
        .Add(7546,   Traits.Immobile)                                            // True North
        .Add(7548,   Traits.Immobile)                                            // Arms Length
        .Add(7477,   Traits.Immobile | Traits.BoundArms)                         // Hakaze
        .Add(7478,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Jinpu
        .Add(7486,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Enpi
        .Add(7479,   Traits.Immobile | Traits.BoundArms)                         // Shifu
        .Add(7483,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Fuga
        .Add(7481,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Gekko
        .Add(7867,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Ianijutsu
        .Add(7484,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Mangetsu
        .Add(7482,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Kasha
        .Add(7485,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Oka
        .Add(7480,   Traits.Immobile | Traits.BoundArms)                         // Yukikaze
        .Add(7490,   Traits.Immobile | Traits.BoundArms)                         // Hissatsu Shinten
        .Add(7492,   Traits.Immobile | Traits.BoundLegs | Traits.Blindfolded)    // Hissatsu Gyoten
        .Add(7493,   Traits.Immobile | Traits.BoundLegs)                         // Hissatsu: Yaten
        .Add(7497,   Traits.None)                                                // Meditate
        .Add(7491,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Hissatsu Kyuten
        .Add(7495,   Traits.None)                                                // Hagakure
        .Add(7496,   Traits.Immobile | Traits.BoundArms)                         // Hissatsu Guren
        .Add(25779,  Traits.Immobile | Traits.BoundArms)                         // Shoha II
        .Add(25781,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Ogi Namikiri
        .Add(7498,   Traits.Blindfolded);                                        // Third Eye

    public static readonly ImmutableDictionary<uint, Traits> RedMage = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7562,   Traits.Immobile | Traits.BoundArms)                         // Lucid Dreaming
        .Add(25880,  Traits.Immobile | Traits.BoundArms)                         // Sleep
        .Add(7559,   Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7561,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Swiftcast
        .Add(7560,   Traits.Immobile | Traits.BoundArms)                         // Addle
        .Add(25857,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Magick Barrier
        .Add(7519,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Contre Sixte
        .Add(7517,   Traits.Immobile | Traits.BoundArms)                         // Fleche
        .Add(7520,   Traits.Gagged)                                              // Embolden
        .Add(7521,   Traits.Immobile | Traits.BoundArms | Traits.Gagged)         // Manification
        .Add(7506,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Corps-a-corps
        .Add(7515,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Displacement
        .Add(16527,  Traits.Immobile | Traits.BoundLegs)                         // Engagement
        .Add(7523,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Verraise
        .Add(7509,   Traits.Immobile | Traits.BoundArms)                         // Scatter
        .Add(16524,  Traits.Immobile | Traits.BoundArms)                         // Verthunder II
        .Add(16525,  Traits.Immobile | Traits.BoundArms)                         // Veraero II
        .Add(7511,   Traits.Immobile | Traits.BoundArms)                         // Verstone
        .Add(7510,   Traits.Immobile | Traits.BoundArms)                         // Verfire
        .Add(7514,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Vercure
        .Add(7513,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Moulinet
        .Add(7504,   Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Riposte
        .Add(7512,   Traits.Immobile | Traits.BoundArms)                         // Zwerchhau
        .Add(7516,   Traits.Immobile | Traits.BoundArms)                         // Redoublement
        .Add(7503,   Traits.Immobile | Traits.BoundArms)                         // Jolt
        .Add(7507,   Traits.Immobile | Traits.BoundArms)                         // Veraero
        .Add(7505,   Traits.Immobile | Traits.BoundArms)                         // Verthunder
        .Add(7528,   Traits.None)                                                // Accelerate
        .Add(16529,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs);     // Reprise

    public static readonly ImmutableDictionary<uint, Traits> Gunbreaker = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7535,   Traits.Immobile)                                            // Reprisal
        .Add(7548,   Traits.Immobile)                                            // Arms Length
        .Add(7533,   Traits.Blindfolded)                                         // Provoke
        .Add(7537,   Traits.Blindfolded)                                         // Shirk
        .Add(7538,   Traits.Immobile | Traits.BoundLegs)                         // Interject
        .Add(7540,   Traits.Immobile | Traits.BoundLegs)                         // Low Blow
        .Add(7531,   Traits.None)                                                // Rampart
        .Add(16137,  Traits.Immobile | Traits.BoundArms)                         // Keen Edge
        .Add(16139,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Brutal Shell
        .Add(16145,  Traits.Immobile | Traits.BoundArms)                         // Solid Barrel
        .Add(16146,  Traits.Immobile | Traits.BoundArms)                         // Gnashing Fang
        .Add(16162,  Traits.Immobile | Traits.BoundArms)                         // Burst Strike
        .Add(16138,  Traits.None)                                                // No Mercy
        .Add(16154,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Rough Divide
        .Add(16143,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Lightning Shot
        .Add(16153,  Traits.Immobile | Traits.BoundArms)                         // Sonic Break
        .Add(16144,  Traits.Immobile | Traits.BoundArms)                         // Danger Zone
        .Add(16159,  Traits.Immobile | Traits.BoundArms)                         // Bow Shock
        .Add(16155,  Traits.Immobile | Traits.BoundArms)                         // Continuation
        .Add(25760,  Traits.Immobile | Traits.BoundArms)                         // Double Down
        .Add(16164,  Traits.Immobile | Traits.BoundArms)                         // Bloodfest
        .Add(16151,  Traits.None)                                                // Aurora
        .Add(16161,  Traits.Blindfolded)                                         // Heart of Corundum
        .Add(16142,  Traits.None)                                                // Royal Guard
        .Add(16152,  Traits.None)                                                // Superbolide
        .Add(16148,  Traits.BoundArms | Traits.Immobile)                         // Nebula
        .Add(16140,  Traits.None)                                                // Camouflage
        .Add(16160,  Traits.None)                                                // Heart of Light
        .Add(16141,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Demon Slice
        .Add(16149,  Traits.Immobile | Traits.BoundArms)                         // Demon Slaughter
        .Add(16163,  Traits.Immobile | Traits.BoundArms);                        // Fated Circle

    public static readonly ImmutableDictionary<uint, Traits> Dancer = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7548,   Traits.Immobile)                                            // Arms Length
        .Add(7551,   Traits.Immobile | Traits.Blindfolded)                       // Head Graze
        .Add(7557,   Traits.Weighty)                                             // Peloton
        .Add(7541,   Traits.None)                                                // Second Wind
        .Add(16005,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Saber Dance
        .Add(16013,  Traits.None)                                                // Flourish
        .Add(16015,  Traits.Immobile | Traits.BoundLegs)                         // Curing Waltz
        .Add(16011,  Traits.Gagged)                                              // Devilment
        .Add(16012,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Shield Samba
        .Add(15998,  Traits.BoundLegs)                                           // Technical Step
        .Add(15997,  Traits.BoundLegs)                                           // Standard Step
        .Add(16009,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Fan Dance III
        .Add(16007,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Fan Dance
        .Add(15991,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Reverse Cascade
        .Add(15992,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Fountainfall
        .Add(15990,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Fountain
        .Add(15989,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Cascade
        .Add(15993,  Traits.Immobile | Traits.BoundArms)                         // Windmill
        .Add(15994,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Bladeshower
        .Add(15995,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Rising Windmill
        .Add(15996,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Bloodshower
        .Add(16008,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Fan Dance II
        .Add(25791,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs | Traits.Blindfolded) // Fan Dance IV
        .Add(25792,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Starfall Dance
        .Add(16014,  Traits.Immobile | Traits.BoundLegs)                         // Improvisation
        .Add(16010,  Traits.BoundLegs)                                           // En Avant
        .Add(16016,  Traits.None);                                               // Closed Position

    public static readonly ImmutableDictionary<uint, Traits> Reaper = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7542,   Traits.BoundArms)                                           // Bloodbath
        .Add(7541,   Traits.None)                                                // Second Wind
        .Add(7549,   Traits.Immobile | Traits.BoundArms)                         // Feint
        .Add(7863,   Traits.Immobile | Traits.BoundLegs)                         // Leg Sweep
        .Add(7546,   Traits.Immobile)                                            // True North
        .Add(7548,   Traits.Immobile)                                            // Arms Length
        .Add(24373,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Slice
        .Add(24374,  Traits.Immobile | Traits.BoundArms)                         // Waxing Slice
        .Add(24375,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Infernal Slice
        .Add(24383,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Gallows
        .Add(24382,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Gibbet
        .Add(24378,  Traits.Immobile | Traits.BoundArms)                         // Shadow Of Death
        .Add(24386,  Traits.Immobile | Traits.BoundArms)                         // Harpe
        .Add(24404,  Traits.Immobile | Traits.BoundArms)                         // Arcane Crest
        .Add(24394,  Traits.Immobile | Traits.BoundArms)                         // Enshroud
        .Add(34689,  Traits.Immobile)                                            // Blood Stalk
        .Add(24381,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Soul Scythe
        .Add(24393,  Traits.Immobile)                                            // Gluttony
        .Add(24398,  Traits.Immobile | Traits.BoundArms)                         // Communio
        .Add(24385,  Traits.Immobile | Traits.BoundArms)                         // Plentiful Harvest
        .Add(24402,  Traits.Immobile | Traits.BoundArms)                         // Hell's Egress
        .Add(24401,  Traits.Immobile | Traits.BoundArms)                         // Hell's Ingress
        .Add(24405,  Traits.Immobile | Traits.BoundArms)                         // Arcane Circle
        .Add(24380,  Traits.Immobile | Traits.BoundArms)                         // Soul Slice
        .Add(24392,  Traits.Immobile | Traits.BoundArms)                         // Grim Swathe
        .Add(24384,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Guillotine
        .Add(24379,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Whorl of Death
        .Add(24376,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Spinning Scythe
        .Add(24377,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs);     // Nightmare Scythe

    public static readonly ImmutableDictionary<uint, Traits> Sage = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7568,   Traits.Immobile | Traits.BoundArms)                         // Esuna
        .Add(7561,   Traits.Immobile | Traits.BoundArms)                         // Swiftcast
        .Add(7559,   Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7571,   Traits.Immobile | Traits.BoundArms)                         // Rescue
        .Add(16560,  Traits.Immobile | Traits.BoundArms)                         // Repose
        .Add(24318,  Traits.Immobile | Traits.BoundArms)                         // Pneuma
        .Add(24317,  Traits.Blindfolded)                                         // Krasis
        .Add(24283,  Traits.Immobile | Traits.BoundArms)                         // Dosis
        .Add(24284,  Traits.None)                                                // Diagnosis
        .Add(24285,  Traits.Blindfolded | Traits.Immobile | Traits.BoundArms)    // Kardia
        .Add(24286,  Traits.None)                                                // Prognosis
        .Add(24287,  Traits.None)                                                // Egeiro
        .Add(24288,  Traits.Blindfolded)                                         // Physis
        .Add(24289,  Traits.Blindfolded | Traits.Immobile | Traits.BoundLegs)    // Phlegma
        .Add(24290,  Traits.Blindfolded)                                         // Eukrasia
        .Add(24294,  Traits.Blindfolded)                                         // Soteria
        .Add(24295,  Traits.Blindfolded)                                         // Icarus
        .Add(24296,  Traits.Blindfolded | Traits.BoundLegs)                      // Druocole
        .Add(24297,  Traits.None)                                                // Dyskrasia
        .Add(24298,  Traits.BoundLegs)                                           // Kerachole
        .Add(24299,  Traits.None)                                                // Ixochole
        .Add(24300,  Traits.Blindfolded)                                         // Zoe
        .Add(24301,  Traits.Gagged)                                              // Pepsis
        .Add(24303,  Traits.Blindfolded)                                         // Taurochole
        .Add(24304,  Traits.Blindfolded | Traits.Immobile | Traits.BoundArms)    // Toxikon
        .Add(24305,  Traits.Blindfolded)                                         // Haima
        .Add(24309,  Traits.None)                                                // Rhizomata
        .Add(24310,  Traits.None)                                                // Holos
        .Add(24311,  Traits.None);                                               // Panhaima

    public static readonly ImmutableDictionary<uint, Traits> Viper = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7542,   Traits.BoundArms)                                           // Bloodbath
        .Add(7541,   Traits.None)                                                // Second Wind
        .Add(7549,   Traits.BoundArms)                                           // Feint
        .Add(7863,   Traits.BoundLegs)                                           // Leg Sweep
        .Add(7546,   Traits.Immobile)                                            // True North
        .Add(7548,   Traits.Immobile)                                            // Arms Length
        .Add(34606,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms)      // Steel Fangs
        .Add(34607,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms)      // Reaving Fangs
        .Add(34620,  Traits.Immobile | Traits.BoundArms)                         // Vicewinder
        .Add(35920,  Traits.Immobile | Traits.BoundArms)                         // Serpent's Tail
        .Add(35922,  Traits.Immobile | Traits.BoundArms)                         // Twinblood
        .Add(35921,  Traits.Immobile | Traits.BoundArms)                         // Twinfang
        .Add(34623,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms)      // Vicepit
        .Add(35614,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms)      // Steel Maw
        .Add(34615,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms)      // Reaving Maw
        .Add(34633,  Traits.Immobile | Traits.Blindfolded | Traits.BoundArms)    // Uncoiled Fury
        .Add(34647,  Traits.BoundArms)                                           // Serpent's Ire
        .Add(34622,  Traits.Immobile | Traits.BoundArms)                         // Swiftskin's Coil
        .Add(34621,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms)      // Hunter's Coil
        .Add(34626,  Traits.BoundArms | Traits.Gagged)                           // Reawaken
        .Add(34624,  Traits.Immobile | Traits.BoundArms)                         // Hunter's Den
        .Add(34625,  Traits.BoundArms | Traits.Blindfolded)                      // Swiftskin's Den
        .Add(34646,  Traits.Immobile | Traits.Blindfolded)                       // Slither
        .Add(34632,  Traits.BoundArms | Traits.Blindfolded);                     // Writing Snap

    public static readonly ImmutableDictionary<uint, Traits> Pictomancer = ImmutableDictionary<uint, Traits>.Empty
        .Add(4,      Traits.Weighty)                                             // Sprint
        .Add(7,      Traits.None)                                                // Teleport
        .Add(8,      Traits.None)                                                // Return
        .Add(7562,   Traits.Immobile | Traits.BoundArms)                         // Lucid Dreaming
        .Add(25880,  Traits.Immobile | Traits.BoundArms)                         // Sleep
        .Add(7559,   Traits.Immobile | Traits.BoundArms)                         // Surecast
        .Add(7561,   Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Swiftcast
        .Add(7560,   Traits.Immobile | Traits.BoundArms)                         // Addle
        .Add(34650,  Traits.Immobile | Traits.BoundArms)                         // Fire in Red
        .Add(35347,  Traits.Immobile | Traits.BoundArms)                         // Living Muse
        .Add(34653,  Traits.Immobile | Traits.BoundArms)                         // Blizzard in Cyan
        .Add(34662,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Holy in White
        .Add(34663,  Traits.Immobile | Traits.BoundArms | Traits.Blindfolded)    // Comet in Black
        .Add(34678,  Traits.BoundArms | Traits.Blindfolded)                      // Hammer Stamp
        .Add(34688,  Traits.Immobile | Traits.BoundLegs | Traits.BoundArms)      // Rainbow Drip
        .Add(34659,  Traits.Immobile | Traits.BoundArms)                         // Blizzard II in Cyan
        .Add(34656,  Traits.Immobile | Traits.BoundArms)                         // Fire II in Red
        .Add(34681,  Traits.BoundLegs | Traits.BoundArms | Traits.Gagged)        // Star Prism
        .Add(34676,  Traits.BoundArms | Traits.Blindfolded)                      // Mog of the Ages
        .Add(34683,  Traits.Immobile | Traits.BoundLegs)                         // Subtractive Palette
        .Add(35348,  Traits.BoundArms)                                           // Steel Muse
        .Add(35349,  Traits.Immobile | Traits.BoundArms | Traits.BoundLegs)      // Scenic Muse
        .Add(34685,  Traits.BoundArms)                                           // Tempura Coat
        .Add(34684,  Traits.Immobile | Traits.BoundLegs)                         // Smudge
        .Add(34689,  Traits.BoundArms)                                           // Pom Motif
        .Add(34690,  Traits.BoundArms)                                           // Hammer Motif
        .Add(34691,  Traits.BoundArms);                                          // Starry Sky Motif

    public static readonly ImmutableDictionary<uint, Traits> BlueMage = Adventurer;

    public static readonly ImmutableDictionary<uint, Traits> Carpenter = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Blacksmith = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Armorer = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Goldsmith = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Leatherworker = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Weaver = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Alchemist = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Culinarian = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Miner = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Botanist = Adventurer;
    public static readonly ImmutableDictionary<uint, Traits> Fisher = Adventurer;
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
