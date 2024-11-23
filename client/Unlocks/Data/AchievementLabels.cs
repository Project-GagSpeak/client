using GagspeakAPI.Data.IPC;
using System.Windows.Forms;

namespace GagSpeak.Achievements;

public struct UnlockReward
{
    /// <summary> The Component Associated with the unlock. </summary>
    public ProfileComponent Component { get; set; } = ProfileComponent.Plate;

    /// <summary> If the Unlock is a Background, Border, or Overlay. </summary>
    public StyleKind Type { get; set; } = StyleKind.Background;

    /// <summary> The Value within that defined enum that is unlocked by this. </summary>
    public int Value { get; set; } = 0;

    public UnlockReward(ProfileComponent component, StyleKind type, int value)
    {
        Component = component;
        Type = type;
        Value = value;
    }
}

public struct AchievementInfo
{
    /// <summary> Unique Achievement ID </summary>
    public int Id { get; init; }
    /// <summary> Achievement Title </summary>
    public string Title { get; init; }
    /// <summary> Achievement Description </summary>
    public string Description { get; init; }

    /// <summary> The Reward for unlocking this achievement. </summary>
    public UnlockReward UnlockReward { get; init; }

    public AchievementInfo(int id, string title, string description, UnlockReward reward = new UnlockReward())
    {
        Id = id;
        Title = title;
        Description = description;
        UnlockReward = reward;
    }
}

public static class Achievements
{
    // Orders
    public static readonly AchievementInfo JustAVolunteer = new AchievementInfo(1, "Just a Volunteer", "Finish 1 Order");
    public static readonly AchievementInfo AsYouCommand = new AchievementInfo(2, "As You Command", "Finish 10 Orders");
    public static readonly AchievementInfo AnythingForMyOwner = new AchievementInfo(3, "Perfect Programming", "Finish 100 Orders");
    public static readonly AchievementInfo GoodDrone = new AchievementInfo(4, "The Perfect Drone", "Finish 1000 Orders");
    public static readonly AchievementInfo BadSlut = new AchievementInfo(5, "Bad Slut", "Fail 1 Order");
    public static readonly AchievementInfo NeedsTraining = new AchievementInfo(6, "In Need of Training", "Fail 10 Orders");
    public static readonly AchievementInfo UsefulInOtherWays = new AchievementInfo(7, "Useful In Other Ways", "Fail 100 Orders");
    public static readonly AchievementInfo NewSlaveOwner = new AchievementInfo(8, "New Slave Owner", "Create 1 Order");
    public static readonly AchievementInfo TaskManager = new AchievementInfo(9, "Task Manager", "Create 10 Orders");
    public static readonly AchievementInfo MaidMaster = new AchievementInfo(10, "Maid Master", "Create 100 Orders");
    public static readonly AchievementInfo QueenOfDrones = new AchievementInfo(11, "Queen of Drones", "Create 1000 Orders");

    // Gags
    public static readonly AchievementInfo SelfApplied = new AchievementInfo(12, "The Right Fit", "Apply a Gag to Yourself");
    public static readonly AchievementInfo SilenceSlut = new AchievementInfo(13, "A Perfect Fit", "Apply a Gag to another Kinkster");
    public static readonly AchievementInfo WatchYourTongue = new AchievementInfo(14, "Watch Your Tongue", "Apply gags to other kinksters 10 times.");
    public static readonly AchievementInfo TongueTamer = new AchievementInfo(15, "Tongue Tamer", "Apply gags to other kinksters 100 times.");
    public static readonly AchievementInfo KinkyLibrarian = new AchievementInfo(142, "Kinky Librarian", "Apply gags to other kinksters 500 times.");
    public static readonly AchievementInfo OrchestratorOfSilence = new AchievementInfo(16, "Orchestrator Of Silence", "Apply gags to other kinksters 1000 times.");

    public static readonly AchievementInfo SilencedSlut = new AchievementInfo(143, "Silenced Slut", "Get gagged by another Kinkster");
    public static readonly AchievementInfo InDeepSilence = new AchievementInfo(144, "In Deep Silence", "Get gagged by another Kinkster 10 times.");
    public static readonly AchievementInfo SilentObsessions = new AchievementInfo(145, "Silent Obsessions", "Get gagged by another Kinkster 100 times.");
    public static readonly AchievementInfo GoldenSilence = new AchievementInfo(146, "Of Golden Silence", "Get gagged by another Kinkster 500 times.");
    public static readonly AchievementInfo AKinkForDrool = new AchievementInfo(147, "A Kink for Drool", "Get gagged by another Kinkster 1000 times.");
    public static readonly AchievementInfo ThePerfectGagSlut = new AchievementInfo(148, "The Perfect GagSlut", "Get gagged by another Kinkster 5000 times.");

    public static readonly AchievementInfo ShushtainableResource = new AchievementInfo(17, "Sustainable Resource", "Have all three Gag Slots occupied at once.");

    public static readonly AchievementInfo OfVoicelessPleas = new AchievementInfo(18, "Of Voiceless Pleas", "Send a Garbled Message to /say. (Please be smart about this)"); // /say
    public static readonly AchievementInfo DefianceInSilence = new AchievementInfo(149, "Defiance In Silence", "Send 500 Garbled Message to /say. (Please be smart about this)"); // /say
    public static readonly AchievementInfo MuffledResilience = new AchievementInfo(150, "Muffled Resilience", "Send 1000 Garbled Message to /say. (Please be smart about this)"); // /say
    public static readonly AchievementInfo TrainedInSubSpeech = new AchievementInfo(151, "Trained In Sub Speech", "Send 2500 Garbled Message to /say. (Please be smart about this)"); // /say
    public static readonly AchievementInfo PublicSpeaker = new AchievementInfo(19, "Public Speaker", "Say anything longer than 5 words with LiveChatGarbler on in /yell (Please be smart about this)"); // /yell
    public static readonly AchievementInfo FromCriesOfHumility = new AchievementInfo(20, "From Cries of Humility", "Say anything longer than 5 words with LiveChatGarbler on in /shout (Please be smart about this)"); // /shout

    public static readonly AchievementInfo WhispersToWhimpers = new AchievementInfo(185, "Whispers to Whimpers", "Wear a gag for 5 Minutes");
    public static readonly AchievementInfo OfMuffledMoans = new AchievementInfo(186, "Of Muffled Moans", "Wear a gag for 10 Minutes");
    public static readonly AchievementInfo SilentStruggler = new AchievementInfo(187, "Silent Struggler", "Wear a gag for 30 Minutes");
    public static readonly AchievementInfo QuietedCaptive = new AchievementInfo(188, "Quieted Captive", "Wear a gag for 1 Hour");
    public static readonly AchievementInfo MessyDrooler = new AchievementInfo(189, "Messy Drooler", "Wear a gag for 6 Hours");
    public static readonly AchievementInfo DroolingDiva = new AchievementInfo(190, "Drooling Diva", "Wear a gag for 12 Hours");
    public static readonly AchievementInfo EmbraceOfSilence= new AchievementInfo(191, "The Embrace of Silence", "Wear a gag for 1 Day");
    public static readonly AchievementInfo SubjugationToSilence = new AchievementInfo(192, "Subjugation to Silence", "Wear a gag for 4 Days");
    public static readonly AchievementInfo SpeechSilverSilenceGolden = new AchievementInfo(21, "Speech is Silver, Silence is Golden", "Wear a gag for 1 week");
    public static readonly AchievementInfo TheKinkyLegend = new AchievementInfo(22, "The Kinky Legend", "Wear a gag continuously for 2 weeks");

    public static readonly AchievementInfo SilentButDeadly = new AchievementInfo(23, "Silent but Deadly", "Complete 10 roulettes with a gag equipped");

    public static readonly AchievementInfo ATrueGagSlut = new AchievementInfo(24, "A True Gag Slut", "Be gagged by 10 different people in less than 1 hour");

    public static readonly AchievementInfo GagReflex = new AchievementInfo(25, "Gag Reflex", "Be blown away in a gold saucer GATE with any gag equipped.");

    public static readonly AchievementInfo QuietNowDear = new AchievementInfo(26, "Quiet Now, Dear", "Use /shush while targeting a gagged player");
    public static readonly AchievementInfo SilenceOfShame = new AchievementInfo(152, "The Silence of Shame", "Be /shush'ed while gagged");

    public static readonly AchievementInfo YourFavoriteNurse = new AchievementInfo(27, "Your Favorite Nurse", "Apply a restraint set or Gag to a GagSpeak pair while you have a Mask Gag Equipped 20 times");

    public static readonly AchievementInfo SayMmmph = new AchievementInfo(28, "Say Mmmph!", "Take a screenshot in /gpose while gagged");

    // Wardrobe
    public static readonly AchievementInfo FirstTiemers = new AchievementInfo(29, "First Tiemers", "Have a Restraint Set applied for the first time (or apply one to someone else)");
    public static readonly AchievementInfo Cuffed19 = new AchievementInfo(30, "Cuffed-19", "Get your hands restrained 19 times.");
    public static readonly AchievementInfo TheRescuer = new AchievementInfo(31, "The Rescuer", "Unlock 100 Restraints from someone other than yourself.");
    public static readonly AchievementInfo SelfBondageEnthusiast = new AchievementInfo(32, "Self-Bondage Enthusiast", "Apply a restraint to yourself 100 times.");
    public static readonly AchievementInfo DiDEnthusiast = new AchievementInfo(33, "Making a Damsel out of You.", "Apply a restraint set to someone else 100 times.");
    public static readonly AchievementInfo CrowdPleaser = new AchievementInfo(34, "Crowd Pleaser", "Be restrained with 15 or more people around you.");
    public static readonly AchievementInfo Humiliation = new AchievementInfo(35, "Lesson in Humiliation", "Be restrained with 5 or more GagSpeak Pairs nearby.");
    public static readonly AchievementInfo BondageBunny = new AchievementInfo(36, "Bondage Bunny", "Be restrained by 5 different people in less than 2 hours.");

    public static readonly AchievementInfo ToDyeFor = new AchievementInfo(37, "To Dye For", "Dye a Restraint Set 5 times");
    public static readonly AchievementInfo DyeAnotherDay = new AchievementInfo(38, "Dye Another Day", "Dye a Restraint Set 10 times");
    public static readonly AchievementInfo DyeHard = new AchievementInfo(39, "Dye Hard", "Dye a Restraint Set 15 times");

    public static readonly AchievementInfo RiggersFirstSession = new AchievementInfo(40, "Riggers First Session", "Lock someone in a Restraint Set for 30 minutes");
    public static readonly AchievementInfo MyLittlePlaything = new AchievementInfo(41, "My Little Plaything", "Lock someone in a Restraint Set for 1 hour");
    public static readonly AchievementInfo SuitsYouBitch = new AchievementInfo(42, "Suits you, Bitch!", "Lock someone in a Restraint Set for 6 hours");
    public static readonly AchievementInfo TiesThatBind = new AchievementInfo(43, "The Ties That Bind", "Lock someone in a Restraint Set for 1 day");
    public static readonly AchievementInfo SlaveTrainer = new AchievementInfo(44, "Slave Trainer", "Lock someone in a Restraint Set for 1 week");
    public static readonly AchievementInfo CeremonyOfEternalBondage = new AchievementInfo(45, "Ceremony Of Eternal Bondage", "Lock someone in a Restraint Set for 1 month");

    public static readonly AchievementInfo FirstTimeBondage = new AchievementInfo(46, "First Time Bondage", "Endure being locked in a Restraint Set for 30 minutes");
    public static readonly AchievementInfo AmateurBondage = new AchievementInfo(47, "Amateur Bondage", "Endure being locked in a Restraint Set for 1 hour");
    public static readonly AchievementInfo ComfortRestraint = new AchievementInfo(48, "In Comforting Restraints", "Endure being locked in a Restraint Set for 6 hours");
    public static readonly AchievementInfo YourBondageMaid = new AchievementInfo(49, "Bondage Maid", "Endure being locked in a Restraint Set for 1 day");
    public static readonly AchievementInfo YourRubberMaid = new AchievementInfo(153, "Rubber Maid", "Endure being locked in a Restraint Set for 4 days");
    public static readonly AchievementInfo TrainedBondageSlave = new AchievementInfo(50, "Trained Bondage Slave", "Endure being locked in a Restraint Set for 1 week");
    public static readonly AchievementInfo YourRubberSlut = new AchievementInfo(154, "Your Rubber Slut", "Endure being locked in a Restraint Set for 2 weeks");
    public static readonly AchievementInfo ATrueBondageSlave = new AchievementInfo(51, "A True Bondage Slave", "Endure being locked in a Restraint Set for 1 month");

    public static readonly AchievementInfo KinkyExplorer = new AchievementInfo(52, "Kinky Explorer", "Run a Dungeon with Cursed Bondage Loot enabled.");
    public static readonly AchievementInfo TemptingFatesTreasure = new AchievementInfo(53, "Tempting Fate's Treasure", "Be Caught in Cursed Bondage Loot for the first time.");
    public static readonly AchievementInfo BadEndSeeker = new AchievementInfo(54, "Bad End Seeker", "Get trapped in Cursed Bondage Loot 25 times. (Chance must be 25% or lower)");
    public static readonly AchievementInfo EverCursed = new AchievementInfo(55, "Ever Cursed", "Get trapped in Cursed Bondage Loot 100 times. (Chance must be 25% or lower)");
    public static readonly AchievementInfo HealSlut = new AchievementInfo(56, "Heal Slut", "Complete a duty as a healer while wearing a gag, restraint, or using a vibe.");

    public static readonly AchievementInfo BondagePalace = new AchievementInfo(57, "Bondage Palace", "Reach Floor 50 or 100 of Palace of the Dead while bound.");
    public static readonly AchievementInfo HornyOnHigh = new AchievementInfo(58, "Horny on High", "Reach Floor 30 of Heaven-on-High while bound.");
    public static readonly AchievementInfo EurekaWhorethos = new AchievementInfo(59, "Eureka Whore-thos", "Reach Floor 30 of Eureka Orthos while bound.");
    
    public static readonly AchievementInfo MyKinkRunsDeep = new AchievementInfo(60, "My Kinks Run Deep", "Complete a deep dungeon with hardcore stimulation or hardcore restraints.");
    public static readonly AchievementInfo MyKinksRunDeeper = new AchievementInfo(61, "My Kinks Run Deeper", "Solo a deep dungeon with hardcore stimulation or hardcore restraints.");

    public static readonly AchievementInfo TrialOfFocus = new AchievementInfo(62, "Trial Of Focus", "Complete a trial within 10 levels of max level with stimulation (HardcoreLabels Focus).");
    public static readonly AchievementInfo TrialOfDexterity = new AchievementInfo(63, "Trial of Dexterity", "Complete a trial within 10 levels of max level with arms/legs restrained.");
    public static readonly AchievementInfo TrialOfTheBlind = new AchievementInfo(64, "Trial Of The Blind", "Complete a trial within 10 levels of max level while blindfolded.");

    public static readonly AchievementInfo RunningGag = new AchievementInfo(65, "Running Gag", "Incorrectly guess a gag's lock password while unrestrained and running.");

    public static readonly AchievementInfo AuctionedOff = new AchievementInfo(66, "Auctioned Off", "Have a restraint set enabled by one GagSpeak user be removed by a different GagSpeak user.");

    public static readonly AchievementInfo SoldSlave = new AchievementInfo(67, "Sold Slave", "Have a password-locked restraint set locked by one GagSpeak user be unlocked by another.");

    public static readonly AchievementInfo Bondodge = new AchievementInfo(68, "Bondodge", "Within 2 seconds of having a restraint set applied to you, remove it from yourself.");

    // Puppeteer
    public static readonly AchievementInfo AnObedientPet = new AchievementInfo(69, "An Obedient Pet", "Be ordered to sit by another pair through Puppeteer.");

    public static readonly AchievementInfo ControlMyBody = new AchievementInfo(70, "Control My Body", "Enable Allow Motions for another pair.");

    public static readonly AchievementInfo CompleteDevotion = new AchievementInfo(71, "Of Complete Devotion", "Enable All Commands for another pair.");

    public static readonly AchievementInfo MasterOfPuppets = new AchievementInfo(72, "The Master of Puppets", "Puppeteer someone 10 times in an hour.");

    public static readonly AchievementInfo KissMyHeels = new AchievementInfo(73, "Kiss my Heels", "Order someone to /grovel 50 times using Puppeteer.");

    public static readonly AchievementInfo HouseServant = new AchievementInfo(163, "House Servant", "Be ordered to /sweep 10 times using Puppeteer.");

    public static readonly AchievementInfo Ashamed = new AchievementInfo(74, "Ashamed", "Be ordered to /sulk 5 times through Puppeteer.");

    public static readonly AchievementInfo AMaestroOfMyProperty = new AchievementInfo(75, "A Maestro of my Property", "Order someone to execute any emote with 'dance' in it 10 times.");

    public static readonly AchievementInfo OrchestratorsApprentice = new AchievementInfo(164, "Orchestrator's Apprentice", "Puppeteer someone 10 times.");
    public static readonly AchievementInfo NoStringsAttached = new AchievementInfo(165, "No Strings Attached", "Puppeteer someone 25 times.");
    public static readonly AchievementInfo PuppetMaster = new AchievementInfo(166, "Puppet Master", "Puppeteer someone 50 times.");
    public static readonly AchievementInfo MasterOfManipulation = new AchievementInfo(167, "Master of Manipulation", "Puppeteer someone 100 times.");
    public static readonly AchievementInfo TheGrandConductor = new AchievementInfo(168, "The Grand Conductor", "Puppeteer someone 250 times.");
    public static readonly AchievementInfo MaestroOfStrings = new AchievementInfo(169, "Maestro of Strings", "Puppeteer someone 500 times.");
    public static readonly AchievementInfo OfGrandiousSymphony = new AchievementInfo(170, "Of Grandious Symphony", "Puppeteer someone 1000 times.");
    public static readonly AchievementInfo SovereignMaestro = new AchievementInfo(171, "Sovereign Maestro", "Puppeteer someone 2500 times.");
    public static readonly AchievementInfo OrchestratorOfMinds = new AchievementInfo(172, "Orchestrator of Minds", "Puppeteer someone 5000 times.");

    public static readonly AchievementInfo WillingPuppet = new AchievementInfo(173, "Willing Puppet", "Be Puppeteered 10 times.");
    public static readonly AchievementInfo AtYourCommand = new AchievementInfo(174, "At Your Command", "Be Puppeteered 25 times.");
    public static readonly AchievementInfo YourMarionette = new AchievementInfo(175, "Your Marionette", "Be Puppeteered 50 times.");
    public static readonly AchievementInfo TheInstrument = new AchievementInfo(176, "The Instrument", "Be Puppeteered 100 times.");
    public static readonly AchievementInfo AMannequinsMadness = new AchievementInfo(177, "A Mannequin's Madness", "Be Puppeteered 250 times.");
    public static readonly AchievementInfo DevotedDoll = new AchievementInfo(178, "Devoted Doll", "Be Puppeteered 500 times.");
    public static readonly AchievementInfo EnthralledDoll = new AchievementInfo(179, "Enthralled Doll", "Be Puppeteered 1000 times.");
    public static readonly AchievementInfo ObedientDoll = new AchievementInfo(180, "Obedient Doll", "Be Puppeteered 1750 times.");
    public static readonly AchievementInfo ServiceDoll = new AchievementInfo(181, "Service Doll", "Be Puppeteered 2500 times.");
    public static readonly AchievementInfo MastersPlaything = new AchievementInfo(182, "Master's Plaything", "Be Puppeteered 5000 times.");
    public static readonly AchievementInfo MistressesPlaything = new AchievementInfo(183, "Mistress's Plaything", "Be Puppeteered 5000 times.");
    public static readonly AchievementInfo ThePerfectDoll = new AchievementInfo(184, "The Perfect Doll", "Be Puppeteered 10000 times.");

    // Toybox
    public static readonly AchievementInfo MyPleasantriesForAll = new AchievementInfo(76, "My Pleasantries for All", "Create and publish a pattern for the first time.");
    public static readonly AchievementInfo DeviousComposer = new AchievementInfo(77, "Devious Composer", "Publish 10 patterns you have made.");

    public static readonly AchievementInfo TasteOfTemptation = new AchievementInfo(201, "Taste of Temptation", "Download your first Pattern from the Pattern Hub.");
    public static readonly AchievementInfo SeekerOfSensations = new AchievementInfo(202, "Seeker of Sensations", "Download 10 Patterns from the Pattern Hub.");
    public static readonly AchievementInfo CravingPleasure = new AchievementInfo(78, "A Craving Pleasure", "Download 30 Patterns from the Pattern Hub.");

    public static readonly AchievementInfo GoodVibes = new AchievementInfo(79, "Good Vibes", "Like a pattern from the Pattern Hub.");
    public static readonly AchievementInfo DelightfulPleasures = new AchievementInfo(203, "Delightful Pleasures", "Like 10 Patterns from the Pattern Hub.");
    public static readonly AchievementInfo PatternLover = new AchievementInfo(204, "Pattern Lover", "Like 25 Patterns from the Pattern Hub.");
    public static readonly AchievementInfo SensualConnoisseur = new AchievementInfo(205, "Sensual Connoisseur", "Like 50 Patterns from the Pattern Hub.");
    public static readonly AchievementInfo PassionateAdmirer = new AchievementInfo(206, "Passionate Admirer", "Like 100 Patterns from the Pattern Hub.");

    public static readonly AchievementInfo ALittleTease = new AchievementInfo(193, "A Little Tease", "Play a pattern for 20 seconds.");
    public static readonly AchievementInfo ShortButSweet = new AchievementInfo(194, "Short But Sweet", "Play a pattern for 1 minutes.");
    public static readonly AchievementInfo TemptingRythms = new AchievementInfo(195, "Tempting Rythms", "Play a pattern for 2 minutes.");
    public static readonly AchievementInfo MyBuildingDesire = new AchievementInfo(196, "My Building Desire", "Play a pattern for 5 minutes.");
    public static readonly AchievementInfo WithWavesOfSensation = new AchievementInfo(197, "Waves Of Sensation", "Play a pattern for 10 minutes.");
    public static readonly AchievementInfo WithHeightenedSensations = new AchievementInfo(198, "With Heightened Sensations", "Play a pattern for 15 minutes.");
    public static readonly AchievementInfo MusicalMoaner = new AchievementInfo(199, "Musical Moaner", "Play a pattern for 20 minutes.");
    public static readonly AchievementInfo StimulatingExperiences = new AchievementInfo(200, "Stimulating Experiences", "Play a pattern for 30 minutes.");
    public static readonly AchievementInfo EnduranceKing = new AchievementInfo(80, "Endurance King", "Play a pattern for an hour (59m) without pause.");
    public static readonly AchievementInfo EnduranceQueen = new AchievementInfo(155, "Endurance Queen", "Play a pattern for an hour (59m) without pause.");

    public static readonly AchievementInfo CollectorOfSinfulTreasures = new AchievementInfo(81, "Collector of Sinful Treasures", "Connect a real device (Intiface / PiShock Device) to GagSpeak.");

    public static readonly AchievementInfo VulnerableVibrations = new AchievementInfo(207, "Vulnerable Vibrations", "stagger while a pattern is actively playing.");

    public static readonly AchievementInfo MotivationForRestoration = new AchievementInfo(82, "Motivation for Restoration", "Play a pattern for over 30 minutes in Diadem.");

    public static readonly AchievementInfo KinkyGambler = new AchievementInfo(83, "Kinky Gambler", "Complete a DeathRoll (win or loss) while having a DeathRoll trigger on.");

    public static readonly AchievementInfo SubtleReminders = new AchievementInfo(84, "Subtle Reminders", "Have 10 Triggers go off.");
    public static readonly AchievementInfo LostInTheMoment = new AchievementInfo(85, "Lost in the Moment", "Have 100 Triggers go off.");
    public static readonly AchievementInfo TriggerHappy = new AchievementInfo(86, "Trigger Happy", "Have 1000 Triggers go off.");

    public static readonly AchievementInfo HornyMornings = new AchievementInfo(87, "Horny Mornings", "Have an alarm go off.");

    // Hardcore
    public static readonly AchievementInfo AllTheCollarsOfTheRainbow = new AchievementInfo(89, "All Collars of the Rainbow", "Force 20 pairs to follow you.");

    public static readonly AchievementInfo UCanTieThis = new AchievementInfo(90, "U Can't Tie This", "Be forced to follow someone, throughout a duty.");

    public static readonly AchievementInfo ForcedFollow = new AchievementInfo(91, "Come Follow Along Now", "Force someone to follow you for 1 minute.");
    public static readonly AchievementInfo ForcedWalkies = new AchievementInfo(92, "The Leash of Your Worries", "Force someone to follow you for 5 minutes.");

    public static readonly AchievementInfo TimeForWalkies = new AchievementInfo(93, "Time for Walkies", "Be forced to follow someone for 1 minute.");
    public static readonly AchievementInfo GettingStepsIn = new AchievementInfo(94, "Getting My Steps In", "Be forced to follow someone for 5 minutes.");
    public static readonly AchievementInfo WalkiesLover = new AchievementInfo(95, "Walkies Lover", "Be forced to follow someone for 10 minutes.");

    public static readonly AchievementInfo LivingFurniture = new AchievementInfo(96, "Living Art", "Be forced to sit for 1 hour or more.");

    public static readonly AchievementInfo WalkOfShame = new AchievementInfo(97, "Walk of Shame", "Be bound, blindfolded, and leashed in a major city.");

    public static readonly AchievementInfo BlindLeadingTheBlind = new AchievementInfo(98, "When Blind lead the Blind", "Be blindfolded while having someone follow you blindfolded.");

    public static readonly AchievementInfo WhatAView = new AchievementInfo(99, "Wow, What A View!", "Use the /lookout emote while wearing a blindfold.");

    public static readonly AchievementInfo WhoNeedsToSee = new AchievementInfo(100, "Who Needs to See?", "Be blindfolded in hardcore mode for 3 hours.");
    public static readonly AchievementInfo OfDomesticDiscipline = new AchievementInfo(101, "Of Domestic Discipline", "Be forced to stay in someone's house for 30 minutes.");
    public static readonly AchievementInfo HomeboundSubmission = new AchievementInfo(102, "Homebound Submission", "Be forced to stay in someone's house for 1 hour.");
    public static readonly AchievementInfo PerfectHousePet = new AchievementInfo(103, "Perfect House Pet", "Be forced to stay in someone's house for 1 day.");

    public static readonly AchievementInfo IndulgingSparks = new AchievementInfo(104, "From Indulging Sparks", "Give out 10 shocks.");
    public static readonly AchievementInfo ShockingTemptations = new AchievementInfo(105, "Shocking Temptations", "Give out 100 shocks.");
    public static readonly AchievementInfo TheCrazeOfShockies = new AchievementInfo(106, "The Shockies Craze", "Give out 1000 shocks.");
    public static readonly AchievementInfo WickedThunder = new AchievementInfo(107, "Of Wicked Thunder", "Give out 10,000 shocks.");
    public static readonly AchievementInfo ElectropeHasNoLimits = new AchievementInfo(108, "Electrope Has No Limits", "Give out 25,000 shocks.");

    public static readonly AchievementInfo ElectrifyingPleasure = new AchievementInfo(109, "Electrifying Pleasure", "Get shocked 10 times.");
    public static readonly AchievementInfo ShockingExperience = new AchievementInfo(110, "Shocking Experience", "Get shocked 100 times.");
    public static readonly AchievementInfo WiredForObedience = new AchievementInfo(111, "Wired for Obedience", "Get shocked 1000 times.");
    public static readonly AchievementInfo ShockAddiction = new AchievementInfo(112, "Shock Addiction", "Get shocked 10,000 times.");
    public static readonly AchievementInfo SlaveToTheShock = new AchievementInfo(113, "A Slave to the Shock", "Get shocked 25,000 times.");
    public static readonly AchievementInfo ShockSlut = new AchievementInfo(114, "Shock Slut", "Get shocked 50,000 times.");

    // Remotes
    public static readonly AchievementInfo JustVibing = new AchievementInfo(115, "Just Vibing", "Use the Remote Control feature for the first time.");
    public static readonly AchievementInfo DontKillMyVibe = new AchievementInfo(116, "Don't Kill My Vibe", "Dial the remotes intensity from 100% to 0% in under a second.");
    public static readonly AchievementInfo VibingWithFriends = new AchievementInfo(117, "Vibing With Friends", "Host a Vibe Server Vibe Room.");

    // Generic
    public static readonly AchievementInfo TutorialComplete = new AchievementInfo(118, "Kinky Beginnings", "Complete the Introduction and Register a successful account!");

    public static readonly AchievementInfo KinkyNovice = new AchievementInfo(119, "Kinky Novice", "Add your first Kinkster in GagSpeak!");
    public static readonly AchievementInfo TheCollector = new AchievementInfo(120, "The Collector", "Add 20 Pairs.");

    public static readonly AchievementInfo BoundaryRespecter = new AchievementInfo(121, "Boundary Respecter", "Apply a preset for a pair, defining the boundaries of your contact.");
    
    public static readonly AchievementInfo HelloKinkyWorld = new AchievementInfo(122, "Hello Kinky World!", "Use the gagspeak global chat for the first time.");
    
    public static readonly AchievementInfo KnowsMyLimits = new AchievementInfo(123, "Knows My Limits", "Use your Safeword for the first time.");
    
    public static readonly AchievementInfo WarriorOfLewd = new AchievementInfo(124, "Warrior Of Lewd", "View a FULL Cutscene while Bound and Gagged.");
    
    public static readonly AchievementInfo EscapingIsNotEasy = new AchievementInfo(125, "Escaping Isn't So Easy..", "Change your equipment/change job while locked in a restraint set.");
    
    public static readonly AchievementInfo ICantBelieveYouveDoneThis = new AchievementInfo(126, "I Can't Believe You've Done This", "Get /slapped while bound.");
    
    public static readonly AchievementInfo WithAKissGoodbye = new AchievementInfo(208, "With A Kiss Goodbye", "/dote a Kinkster that is immobilized.");
    public static readonly AchievementInfo ProlificPetter = new AchievementInfo(209, "Prolific Petter", "/pet an immobilized Kinkster.");


    public static readonly AchievementInfo EscapedPatient = new AchievementInfo(156, "Escaped Patient", "Kill 10 enemies in PvP Frontlines while restrained or vibed.");
    public static readonly AchievementInfo BoundToKill = new AchievementInfo(157, "Bound to Kill", "Kill 25 enemies in PvP Frontlines while restrained or vibed.");
    public static readonly AchievementInfo TheShackledSlayer = new AchievementInfo(158, "The Shackled Slayer", "Kill 50 enemies in PvP Frontlines while restrained or vibed.");
    public static readonly AchievementInfo DangerousConvict = new AchievementInfo(159, "Dangerous Convict", "Kill 100 enemies in PvP Frontlines while restrained or vibed.");
    public static readonly AchievementInfo OfUnyieldingForce = new AchievementInfo(160, "Of Unyielding Force", "Kill 200 enemies in PvP Frontlines while restrained or vibed.");
    public static readonly AchievementInfo StimulationOverdrive = new AchievementInfo(161, "Stimulation Overdrive", "Kill 300 enemies in PvP Frontlines while restrained or vibed.");
    public static readonly AchievementInfo BoundYetUnbroken = new AchievementInfo(162, "Bound yet Unbroken", "Kill 400 enemies in PvP Frontlines while restrained or vibed.");
    public static readonly AchievementInfo ChainsCantHoldMe = new AchievementInfo(88, "Chains Can't Hold Me", "Kill 500 enemies in PvP Frontlines while restrained or vibed.");

    // Secrets
    public static readonly AchievementInfo HiddenInPlainSight = new AchievementInfo(127, "Hidden in Plain Sight", "???");

    public static readonly AchievementInfo Experimentalist = new AchievementInfo(128, "The Experimentalist", "???");

    public static readonly AchievementInfo HelplessDamsel = new AchievementInfo(129, "Completely Helpless", "???");

    public static readonly AchievementInfo GaggedPleasure = new AchievementInfo(130, "Hrmph~!", "???");

    public static readonly AchievementInfo BondageClub = new AchievementInfo(131, "Bondage Club", "???");

    public static readonly AchievementInfo BadEndHostage = new AchievementInfo(132, "A Bad End Hostage", "???");

    public static readonly AchievementInfo TourDeBound = new AchievementInfo(133, "Tour De Bound", "???");

    public static readonly AchievementInfo MuffledProtagonist = new AchievementInfo(134, "The Muffled Protagonist", "???");

    public static readonly AchievementInfo BoundgeeJumping = new AchievementInfo(135, "Boundgee Jumping", "???");

    public static readonly AchievementInfo KinkyTeacher = new AchievementInfo(136, "Perverted Teacher", "???");
    public static readonly AchievementInfo KinkyProfessor = new AchievementInfo(137, "Kinky Professor", "???");
    public static readonly AchievementInfo KinkyMentor = new AchievementInfo(138, "Kinky Mentor", "???");

    public static readonly AchievementInfo ExtremeBondageEnjoyer = new AchievementInfo(139, "Extreme Bondage Enjoyer", "???");

    public static readonly AchievementInfo WildRide = new AchievementInfo(140, "Of Wild Rides", "???");

    public static readonly AchievementInfo SlavePresentation = new AchievementInfo(141, "A Presentable Slave", "???");


    // Full mapping to quickly go from Uint to AchievementInfo
    public static readonly Dictionary<int, AchievementInfo> AchievementMap = new Dictionary<int, AchievementInfo>
    {
        { 1, JustAVolunteer },
        { 2, AsYouCommand },
        { 3, AnythingForMyOwner },
        { 4, GoodDrone },
        { 5, BadSlut },
        { 6, NeedsTraining },
        { 7, UsefulInOtherWays },
        { 8, NewSlaveOwner },
        { 9, TaskManager },
        { 10, MaidMaster },
        { 11, QueenOfDrones },
        { 12, SelfApplied },
        { 13, SilenceSlut },
        { 14, WatchYourTongue },
        { 15, TongueTamer },
        { 16, OrchestratorOfSilence },
        { 17, ShushtainableResource },
        { 18, OfVoicelessPleas },
        { 19, PublicSpeaker },
        { 20, FromCriesOfHumility },
        { 21, SpeechSilverSilenceGolden },
        { 22, TheKinkyLegend },
        { 23, SilentButDeadly },
        { 24, ATrueGagSlut },
        { 25, GagReflex },
        { 26, QuietNowDear },
        { 27, YourFavoriteNurse },
        { 28, SayMmmph },
        { 29, FirstTiemers },
        { 30, Cuffed19 },
        { 31, TheRescuer },
        { 32, SelfBondageEnthusiast },
        { 33, DiDEnthusiast },
        { 34, CrowdPleaser },
        { 35, Humiliation },
        { 36, BondageBunny },
        { 37, ToDyeFor },
        { 38, DyeAnotherDay },
        { 39, DyeHard },
        { 40, RiggersFirstSession },
        { 41, MyLittlePlaything },
        { 42, SuitsYouBitch },
        { 43, TiesThatBind },
        { 44, SlaveTrainer },
        { 45, CeremonyOfEternalBondage },
        { 46, FirstTimeBondage },
        { 47, AmateurBondage },
        { 48, ComfortRestraint },
        { 49, YourBondageMaid },
        { 50, TrainedBondageSlave },
        { 51, ATrueBondageSlave },
        { 52, KinkyExplorer },
        { 53, TemptingFatesTreasure },
        { 54, BadEndSeeker },
        { 55, EverCursed },
        { 56, HealSlut },
        { 57, BondagePalace },
        { 58, HornyOnHigh },
        { 59, EurekaWhorethos },
        { 60, MyKinkRunsDeep },
        { 61, MyKinksRunDeeper },
        { 62, TrialOfFocus },
        { 63, TrialOfDexterity },
        { 64, TrialOfTheBlind },
        { 65, RunningGag },
        { 66, AuctionedOff },
        { 67, SoldSlave },
        { 68, Bondodge },
        { 69, AnObedientPet },
        { 70, ControlMyBody },
        { 71, CompleteDevotion },
        { 72, MasterOfPuppets },
        { 73, KissMyHeels },
        { 74, Ashamed },
        { 75, AMaestroOfMyProperty },
        { 76, MyPleasantriesForAll },
        { 77, DeviousComposer },
        { 78, CravingPleasure },
        { 79, PatternLover },
        { 80, EnduranceKing },
        { 81, CollectorOfSinfulTreasures },
        { 82, MotivationForRestoration },
        { 83, KinkyGambler },
        { 84, SubtleReminders },
        { 85, LostInTheMoment },
        { 86, TriggerHappy },
        { 87, HornyMornings },
        { 88, ChainsCantHoldMe },
        { 89, AllTheCollarsOfTheRainbow },
        { 90, UCanTieThis },
        { 91, ForcedFollow },
        { 92, ForcedWalkies },
        { 93, TimeForWalkies },
        { 94, GettingStepsIn },
        { 95, WalkiesLover },
        { 96, LivingFurniture },
        { 97, WalkOfShame },
        { 98, BlindLeadingTheBlind },
        { 99, WhatAView },
        { 100, WhoNeedsToSee },
        { 101, OfDomesticDiscipline },
        { 102, HomeboundSubmission },
        { 103, PerfectHousePet },
        { 104, IndulgingSparks },
        { 105, ShockingTemptations },
        { 106, TheCrazeOfShockies },
        { 107, WickedThunder },
        { 108, ElectropeHasNoLimits },
        { 109, ElectrifyingPleasure },
        { 110, ShockingExperience },
        { 111, WiredForObedience },
        { 112, ShockAddiction },
        { 113, SlaveToTheShock },
        { 114, ShockSlut },
        { 115, JustVibing },
        { 116, DontKillMyVibe },
        { 117, VibingWithFriends },
        { 118, TutorialComplete },
        { 119, KinkyNovice },
        { 120, TheCollector },
        { 121, BoundaryRespecter },
        { 122, HelloKinkyWorld },
        { 123, KnowsMyLimits },
        { 124, WarriorOfLewd },
        { 125, EscapingIsNotEasy },
        { 126, ICantBelieveYouveDoneThis },
        { 127, HiddenInPlainSight },
        { 128, Experimentalist },
        { 129, HelplessDamsel },
        { 130, GaggedPleasure },
        { 131, BondageClub },
        { 132, BadEndHostage },
        { 133, TourDeBound },
        { 134, MuffledProtagonist },
        { 135, BoundgeeJumping },
        { 136, KinkyTeacher },
        { 137, KinkyProfessor },
        { 138, KinkyMentor },
        { 139, ExtremeBondageEnjoyer },
        { 140, WildRide },
        { 141, SlavePresentation },
        { 142, KinkyLibrarian },
        { 143, SilencedSlut },
        { 144, InDeepSilence },
        { 145, SilentObsessions },
        { 146, GoldenSilence },
        { 147, AKinkForDrool },
        { 148, ThePerfectGagSlut },
        { 149, DefianceInSilence },
        { 150, MuffledResilience },
        { 151, TrainedInSubSpeech },
        { 152, SilenceOfShame },
        { 153, YourRubberMaid },
        { 154, YourRubberSlut },
        { 155, EnduranceQueen },
        { 156, EscapedPatient },
        { 157, BoundToKill },
        { 158, TheShackledSlayer },
        { 159, DangerousConvict },
        { 160, OfUnyieldingForce },
        { 161, StimulationOverdrive },
        { 162, BoundYetUnbroken },
        { 163, HouseServant },
        { 164, OrchestratorsApprentice },
        { 165, NoStringsAttached },
        { 166, PuppetMaster },
        { 167, MasterOfManipulation },
        { 168, TheGrandConductor },
        { 169, MaestroOfStrings },
        { 170, OfGrandiousSymphony },
        { 171, SovereignMaestro },
        { 172, OrchestratorOfMinds },
        { 173, WillingPuppet },
        { 174, AtYourCommand },
        { 175, YourMarionette },
        { 176, TheInstrument },
        { 177, AMannequinsMadness },
        { 178, DevotedDoll },
        { 179, EnthralledDoll },
        { 180, ObedientDoll },
        { 181, ServiceDoll },
        { 182, MastersPlaything },
        { 183, MistressesPlaything },
        { 184, ThePerfectDoll },
        { 185, WhispersToWhimpers },
        { 186, OfMuffledMoans },
        { 187, SilentStruggler },
        { 188, QuietedCaptive },
        { 189, MessyDrooler },
        { 190, DroolingDiva },
        { 191, EmbraceOfSilence },
        { 192, SubjugationToSilence },
        { 193, ALittleTease },
        { 194, ShortButSweet },
        { 195, TemptingRythms },
        { 196, MyBuildingDesire },
        { 197, WithWavesOfSensation },
        { 198, WithHeightenedSensations },
        { 199, MusicalMoaner },
        { 200, StimulatingExperiences },
        { 201, TasteOfTemptation },
        { 202, SeekerOfSensations },
        { 203, DelightfulPleasures },
        { 204, PassionateAdmirer },
        { 205, SensualConnoisseur },
        { 206, PassionateAdmirer },
        { 207, VulnerableVibrations },
        { 208, WithAKissGoodbye },
        { 209, ProlificPetter }
    };
}
