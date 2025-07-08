using CheapLoc;

namespace GagSpeak.Localization
{
    internal static class GSLoc
    {
        public static Intro Intro { get; set; } = new();
        public static Tutorials Tutorials { get; set; } = new();
        public static CoreUi CoreUi { get; set; } = new();
        public static Settings Settings { get; set; } = new();
        public static Gags Gags { get; set; } = new();
        public static Wardrobe Wardrobe { get; set; } = new();
        public static Puppet Puppet { get; set; } = new();
        public static Toybox Toybox { get; set; } = new();

        public static void ReInitialize()
        {
            Intro = new Intro();
            Tutorials = new Tutorials();
            CoreUi = new CoreUi();
            Settings = new Settings();
            Gags = new Gags();
            Wardrobe = new Wardrobe();
            Puppet = new Puppet();
            Toybox = new Toybox();
        }
    }

    #region Intro
    public class Intro
    {
        public ToS ToS { get; set; } = new();
        public Register Register { get; set; } = new();
    }

    // Get to Last.
    public class ToS
    {
        public readonly string Title = Loc.Localize("ToS_Title", "ACTUAL LABEL HERE");
    }

    // Get to Last.
    public class Register
    {
        public readonly string Title = Loc.Localize("Register_Title", "ACTUAL LABEL HERE");
    }
    #endregion Intro

    #region Tutorials
    public class Tutorials
    {
        public HelpMainUi HelpMainUi { get; set; } = new();
        public HelpRemote HelpRemote { get; set; } = new();
        public HelpGags HelpGags { get; set; } = new();
        public HelpGagStorage HelpGagStorage { get; set; } = new();
        public HelpRestraints HelpRestraints { get; set; } = new();
        public HelpCursedLoot HelpCursedLoot { get; set; } = new();
        public HelpToybox HelpToybox { get; set; } = new();
        public HelpPatterns HelpPatterns { get; set; } = new();
        public HelpTriggers HelpTriggers { get; set; } = new();
        public HelpAlarms HelpAlarms { get; set; } = new();
        public HelpAchievements HelpAchievements { get; set; } = new();
    }

    public class HelpMainUi
    {
        public readonly string Step1Title = Loc.Localize("HelpMainUi_Step1Title", "The Connection State");
        public readonly string Step1Desc = Loc.Localize("HelpMainUi_Step1Desc", "Displays the current connection status to GagSpeak Servers.");
        public readonly string Step1DescExtended = Loc.Localize("HelpMainUi_Step1DescExtended", "Button displays as green when connected, and red when disconnected. " +
            "Hovering over the icon will show you the current status.");

        public readonly string Step2Title = Loc.Localize("HelpMainUi_Step2Title", "The Homepage");
        public readonly string Step2Desc = Loc.Localize("HelpMainUi_Step2Desc", "All of GagSpeaks Modules are accessed from this menu.");

        public readonly string Step3Title = Loc.Localize("HelpMainUi_Step3Title", "To the Whitelist Panel");
        public readonly string Step3Desc = Loc.Localize("HelpMainUi_Step3Desc", "To view your whitelist, press this icon from the tab bar (this guide will do it for you)");
        public readonly string Step3DescExtended = Loc.Localize("HelpMainUi_Step3DescExtended", "This is where you go to view and interact with your added Kinksters.");

        public readonly string Step4Title = Loc.Localize("HelpMainUi_Step4Title", "The Whitelist");
        public readonly string Step4Desc = Loc.Localize("HelpMainUi_Step4Desc", "In here, you can add and remove Kinksters, or Grant/Revoke individual access for " +
            "how much anyone can do to you here.");
        public readonly string Step4DescExtended = Loc.Localize("HelpMainUi_Step4DescExtended", "MIDDLE-Clicking on a Kinksters Name lets you open their KinkPlate." + Environment.NewLine +
            "RMB lets you set the nickname of a Kinkster. " + Environment.NewLine +
            "The Magnifying Glass is where you can inspect the permissions this Kinkster has given you access to. " + Environment.NewLine +
            "The Gear Icon is where you can set the permissions for this Kinkster." + Environment.NewLine +
            "The Triple Dots is where you can interact with the Kinkster in various ways.");

        public readonly string Step5Title = Loc.Localize("HelpMainUi_Step5Title", "Adding Kinksters");
        public readonly string Step5Desc = Loc.Localize("HelpMainUi_Step5Desc", "This is what you press to add another Kinkster to your Whitelist.");
        public readonly string Step5DescExtended = Loc.Localize("HelpMainUi_Step5DescExtended", "To add another Kinkster, you will need to obtain their UID, we will show you how to do this soon.");

        public readonly string Step6Title = Loc.Localize("HelpMainUi_Step6Title", "To the Account Panel");
        public readonly string Step6Desc = Loc.Localize("HelpMainUi_Step6Desc", "Select the Account Panel to continue the tutorial.");
        public readonly string Step6DescExtended = Loc.Localize("HelpMainUi_Step6DescExtended", "This page will contain all information related to settings, profile setup, configs, and support links!");

        public readonly string Step7Title = Loc.Localize("HelpMainUi_Step7Title", "Your Profile Picture");
        public readonly string Step7Desc = Loc.Localize("HelpMainUi_Step7Desc", "This is your Account's Profile Image, It is displayed in KinkPlates which others can see.");

        public readonly string Step8Title = Loc.Localize("HelpMainUi_Step8Title", "Your Identification");
        public readonly string Step8Desc = Loc.Localize("HelpMainUi_Step8Desc", "This is your unique UID. To pair with other Kinksters, they will need this.");
        public readonly string Step8DescExtended = Loc.Localize("HelpMainUi_Step8DescExtended", "This UID Defines your account, I would not recommend putting it out publically in global chats or kinkplate descriptions.");

        public readonly string Step9Title = Loc.Localize("HelpMainUi_Step9Title", "Setting your Safeword");
        public readonly string Step9Desc = Loc.Localize("HelpMainUi_Step9Desc", "Triggered with [/safeword YOURSAFEWORD]. You can set it by hitting the pencil edit icon.");
        public readonly string Step9DescExtended = Loc.Localize("HelpMainUi_Step9DescExtended", "Safewords have a 5 minute cooldown when used, and will disable all active restrictions on you.");

        public readonly string Step10Title = Loc.Localize("HelpMainUi_Step10Title", "The Importance of Safewords");
        public readonly string Step10Desc = Loc.Localize("HelpMainUi_Step10Desc", "Safewords are a vital part of any Kinkster's experience, and BDSM as a whole. If anyone ever safewords. You should respect it with utmost importance.");

        public readonly string Step11Title = Loc.Localize("HelpMainUi_Step11Title", "Editing your Profile");
        public readonly string Step11Desc = Loc.Localize("HelpMainUi_Step11Desc", "You can customize the display of your Kinkplate, description, and Avatar here.");

        public readonly string Step12Title = Loc.Localize("HelpMainUi_Step12Title", "Accessing Settings");
        public readonly string Step12Desc = Loc.Localize("HelpMainUi_Step12Desc", "This is where you access your GagSpeak Global Settings, Hardcore Parameters, Plugin Preferences, and Account Management. (Take a look at it after the tutorial!)");

        public readonly string Step13Title = Loc.Localize("HelpMainUi_Step13Title", "Supporting GagSpeak");
        public readonly string Step13Desc = Loc.Localize("HelpMainUi_Step13Desc", "If you ever fancy tossing a tip or becoming a supporter as a thanks for all the hard work, or just to help support me, it would be much apperciated." +
            "\n\nBut please don't feel guilty if you don't. Only support me if you want to! I will always love and cherish you all regardless ♥");

        public readonly string Step14Title = Loc.Localize("HelpMainUi_Step14Title", "To the Global Chat Panel");
        public readonly string Step14Desc = Loc.Localize("HelpMainUi_Step14Desc", "Select the Global Chat Panel to continue the tutorial, A place to talk to anyone else on GagSpeak from Anywhere!");

        public readonly string Step15Title = Loc.Localize("HelpMainUi_Step15Title", "The Global Chat");
        public readonly string Step15Desc = Loc.Localize("HelpMainUi_Step15Desc", "You're Name in here will display as [Kinkster-###] Where the ### is the last 3 characters of your UID." + Environment.NewLine +
            "This is done intentionally with safety in mind, to keep you anonymous for other Kinksters you have not yet added!");
        public readonly string Step15DescExtended = Loc.Localize("HelpMainUi_Step15DescExtended", "You can hover over any name to perform actions such as opening their Light KinkPlate™ or Muting them." + Environment.NewLine +
            "The last 3 ### are intentionally visible to allow Kinksters to distinguish who they are talking with but better communication. Use this to your " +
            "advantage to discuss meetup spots to add eachother properly, without exposing yourself!");

        public readonly string Step16Title = Loc.Localize("HelpMainUi_Step16Title", "To the Pattern Hub");
        public readonly string Step16Desc = Loc.Localize("HelpMainUi_Step16Desc", "Go ahead and navigate to the Pattern Hub, GagSpeaks own 'Kinky Social Media' platform!");

        public readonly string Step17Title = Loc.Localize("HelpMainUi_Step17Title", "The Pattern Hub");
        public readonly string Step17Desc = Loc.Localize("HelpMainUi_Step17Desc", "A Central Hub to browse various Patterns other Kinksters have uploaded. View Pattern Downloads, Likes, and Tags!");

        public readonly string Step18Title = Loc.Localize("HelpMainUi_Step18Title", "Searching the Pattern Hub");
        public readonly string Step18Desc = Loc.Localize("HelpMainUi_Step18Desc", "If you fancy looking for particular kinds of Patterns, you can search up keywords here.");

        public readonly string Step19Title = Loc.Localize("HelpMainUi_Step19Title", "Updating Patterns");
        public readonly string Step19Desc = Loc.Localize("HelpMainUi_Step19Desc", "After changing your search, the order, or the filter, this must be pressed to update the results.");

        public readonly string Step20Title = Loc.Localize("HelpMainUi_Step20Title", "Filter Type");
        public readonly string Step20Desc = Loc.Localize("HelpMainUi_Step20Desc", "Change if you want your search to relate to results in order from most downloads, published date, author, tags, or duration lengths!");

        public readonly string Step21Title = Loc.Localize("HelpMainUi_Step21Title", "Result Order");
        public readonly string Step21Desc = Loc.Localize("HelpMainUi_Step21Desc", "Change results to display in ascending order or desending order.");
        public readonly string Step21DescExtended = Loc.Localize("HelpMainUi_Step21DescExtended", "This affect is directly linked to the Filter Type.");
    }

    public class HelpRemote
    {
        public readonly string Step1Title = Loc.Localize("HelpRemote_Step1Title", "The Power Button");
        public readonly string Step1Desc = Loc.Localize("HelpRemote_Step1Desc", "When active, interactions from this remote are sent to connected devices.");

        public readonly string Step2Title = Loc.Localize("HelpRemote_Step2Title", "The Float Button");
        public readonly string Step2Desc = Loc.Localize("HelpRemote_Step2Desc", "While active, the pink dot will not drop to the floor when released, and stay where you left it.");
        public readonly string Step2DescExtended = Loc.Localize("HelpRemote_Step2DescExtended", "Togglable via clicking it, or with MIDDLE-CLICK, while in the remote window.");

        public readonly string Step3Title = Loc.Localize("HelpRemote_Step3Title", "The Loop Button");
        public readonly string Step3Desc = Loc.Localize("HelpRemote_Step3Desc", "Begins recording from the moment you click and drag the pink dot, to the moment you release it, then repeats that data. ");
        public readonly string Step3DescExtended = Loc.Localize("HelpRemote_Step3DescExtended", "Togglable vis button interaction, or using RIGHT-CLICK, while in the remote window.");

        public readonly string Step4Title = Loc.Localize("HelpRemote_Step4Title", "The Timer");
        public readonly string Step4Desc = Loc.Localize("HelpRemote_Step4Desc", "Displays how long your remote has been running for.");

        public readonly string Step5Title = Loc.Localize("HelpRemote_Step5Title", "The Controllable Circle");
        public readonly string Step5Desc = Loc.Localize("HelpRemote_Step5Desc", "Mouse-Interactable. Can be moved around while remote is active. Height represents Intensity Level.");

        public readonly string Step6Title = Loc.Localize("HelpRemote_Step6Title", "The Output Display");
        public readonly string Step6Desc = Loc.Localize("HelpRemote_Step6Desc", "A display of the recorded vibrations from the remote for visual feedback!.");

        public readonly string Step7Title = Loc.Localize("HelpRemote_Step7Title", "The Device List");
        public readonly string Step7Desc = Loc.Localize("HelpRemote_Step7Desc", "Shows you the affected Connected Devices that this remote is controlling.");
        public readonly string Step7DescExtended = Loc.Localize("HelpRemote_Step7DescExtended", "During Open Betas development this feature will see further functionality, but for now it does not function.");
    }

    public class HelpGags
    {
        public readonly string Step1Title = Loc.Localize("HelpGags_Step1Title", "What do Layers do?");
        public readonly string Step1Desc = Loc.Localize("HelpGags_Step1Desc", "Layers define the priorities of applied gags. If Conflicts immerge, higher layers take priority.");
        public readonly string Step1DescExtended = Loc.Localize("HelpGags_Step1DescExtended", "For Example: Glamours on the same slot will take the priority of the gag on the higher layer.");

        public readonly string Step2Title = Loc.Localize("HelpGags_Step2Title", "Equipping a Gag");
        public readonly string Step2Desc = Loc.Localize("HelpGags_Step2Desc", "The Gag Displayed here reflects the currently equipped gag for the corrisponding layer.\nEquip one to continue the Tutorial.");

        public readonly string Step3Title = Loc.Localize("HelpGags_Step3Title", "Selecting a Padlock");
        public readonly string Step3Desc = Loc.Localize("HelpGags_Step3Desc", "You can select the lock to apply to your gag here.\nSelect any Padlock to continue.");

        public readonly string Step4Title = Loc.Localize("HelpGags_Step4Title", "Brief Info on Padlocks");
        public readonly string Step4Desc = Loc.Localize("HelpGags_Step4Desc", "Each padlock you select has its own properties:" + Environment.NewLine +
            "Metal Locks ⇒ Can be locked/unlocked by anyone." + Environment.NewLine +
            "Password Locks ⇒ Requires password to unlock" + Environment.NewLine +
            "Timer Locks ⇒ Unlock after a certain time." + Environment.NewLine +
            "Owner Locks ⇒ Can be only interacted with by Kinksters with OwnerLock perms." + Environment.NewLine +
            "Devotional Locks ⇒ Can be only interacted with by the Locker. (DevotionalLock access required)");

        public readonly string Step5Title = Loc.Localize("HelpGags_Step5Title", "Locking the Selected Padlock");
        public readonly string Step5Desc = Loc.Localize("HelpGags_Step5Desc", "Once you have chosen a padlock and filled out nessisary fields, this will complete the locking process.");
        public readonly string Step5DescExtended = Loc.Localize("HelpGags_Step5DescExtended", "While a Gag is Locked, you cannot change the gag type or lock type until unlocked.");

        public readonly string Step6Title = Loc.Localize("HelpGags_Step6Title", "Unlocking the Selected Padlock");
        public readonly string Step6Desc = Loc.Localize("HelpGags_Step6Desc", "To unlock a locked Padlock, you must correctly guess its password, if it was set with one.");

        public readonly string Step7Title = Loc.Localize("HelpGags_Step7Title", "Removing a Gag");
        public readonly string Step7Desc = Loc.Localize("HelpGags_Step7Desc", "To Remove a Gag, Simply Right click the Gag Selection List");
    }

    public class HelpGagStorage
    {
        public readonly string Step1Title = Loc.Localize("HelpGagStorage_Step1Title", "Selecting a Gag");
        public readonly string Step1Desc = Loc.Localize("HelpGagStorage_Step1Desc", "This is where you can select a Gag to customize.");

        public readonly string Step2Title = Loc.Localize("HelpGagStorage_Step2Title", "Gag Glamours");
        public readonly string Step2Desc = Loc.Localize("HelpGagStorage_Step2Desc", "This is where you can change the look of a Gag.");

        public readonly string Step3Title = Loc.Localize("HelpGagStorage_Step3Title", "Adjustments");
        public readonly string Step3Desc = Loc.Localize("HelpGagStorage_Step3Desc", "This is where you can set MetaData toggles and the Enabled State, along with C+ Profiles.");

        public readonly string Step4Title = Loc.Localize("HelpGagStorage_Step4Title", "Moodles");
        public readonly string Step4Desc = Loc.Localize("HelpGagStorage_Step4Desc", "If you have any moodles available, you can apply them to the Gag here.");

        public readonly string Step5Title = Loc.Localize("HelpGagStorage_Step5Title", "Audio");
        public readonly string Step5Desc = Loc.Localize("HelpGagStorage_Step5Desc", "This is where you can link certain types of sounds from audio selections to your Gag.");

        public readonly string Step6Title = Loc.Localize("HelpGagStorage_Step6Title", "Saving Customizations");
        public readonly string Step6Desc = Loc.Localize("HelpGagStorage_Step6Desc", "To apply any new changes you made to a gags customizations, you must press the Save Button.");
    }

    public class HelpRestraints
    {
        public readonly string Step1Title = Loc.Localize("HelpRestraints_Step1Title", "Adding a New Restraint Set");
        public readonly string Step1Desc = Loc.Localize("HelpRestraints_Step1Desc", "Select this button to begin creating a new Restraint Set!\n(The Tutorial will do this for you)");

        public readonly string Step2Title = Loc.Localize("HelpRestraints_Step2Title", "The Info Tab");
        public readonly string Step2Desc = Loc.Localize("HelpRestraints_Step2Desc", "Space to insert the Name of the Restraint and a short description for it!");

        public readonly string Step3Title = Loc.Localize("HelpRestraints_Step3Title", "To the Appearance Tab");
        public readonly string Step3Desc = Loc.Localize("HelpRestraints_Step3Desc", "Navigate to the Appearance Tab for setting Glamour & Customizations");

        public readonly string Step4Title = Loc.Localize("HelpRestraints_Step4Title", "Setting Gear Items");
        public readonly string Step4Desc = Loc.Localize("HelpRestraints_Step4Desc", "This space is there you can setup what Glamourer Appearance you want to have applied.");

        public readonly string Step5Title = Loc.Localize("HelpRestraints_Step5Title", "Restraint MetaData");
        public readonly string Step5Desc = Loc.Localize("HelpRestraints_Step5Desc", "Determines if your Hat or Visor will be enabled.");

        public readonly string Step6Title = Loc.Localize("HelpRestraints_Step6Title", "Importing Current Gear");
        public readonly string Step6Desc = Loc.Localize("HelpRestraints_Step6Desc", "Takes your current Appearance from Glamourer, and applies it here.");

        public readonly string Step7Title = Loc.Localize("HelpRestraints_Step7Title", "Importing Customizations");
        public readonly string Step7Desc = Loc.Localize("HelpRestraints_Step7Desc", "Takes your characters current customization appearance, and store it as part of the set.");

        public readonly string Step8Title = Loc.Localize("HelpRestraints_Step8Title", "Customizations: Applying");
        public readonly string Step8Desc = Loc.Localize("HelpRestraints_Step8Desc", "If selected, the customization state you imported will apply with the set.");

        public readonly string Step9Title = Loc.Localize("HelpRestraints_Step9Title", "Customizations: Clearing");
        public readonly string Step9Desc = Loc.Localize("HelpRestraints_Step9Desc", "If Selected, the stored customization data for the set will be cleared.");

        public readonly string Step10Title = Loc.Localize("HelpRestraints_Step10Title", "To the Mods Tab");
        public readonly string Step10Desc = Loc.Localize("HelpRestraints_Step10Desc", "In this tab you can add mods that can be temporarily set while the restraint is active.");

        public readonly string Step11Title = Loc.Localize("HelpRestraints_Step11Title", "Selecting a Mod");
        public readonly string Step11Desc = Loc.Localize("HelpRestraints_Step11Desc", "You can select a mod from your penumbra mods here.");

        public readonly string Step12Title = Loc.Localize("HelpRestraints_Step12Title", "Adding a Mod");
        public readonly string Step12Desc = Loc.Localize("HelpRestraints_Step12Desc", "Once you found one you want to add, you can press this button to append it.");

        public readonly string Step13Title = Loc.Localize("HelpRestraints_Step13Title", "Setting Mod Options.");
        public readonly string Step13Desc = Loc.Localize("HelpRestraints_Step13Desc", "You are able to decide if the mod is toggled back off, or if it should perform a redraw.");
        public readonly string Step13DescExtended = Loc.Localize("HelpRestraints_Step13DescExtended", "Asking a restraint set to perform a redraw for a mod will allow any added " +
            "animation mods to apply their modded animation on the first try, without needing to play it twice for mare to reconize it.");

        public readonly string Step14Title = Loc.Localize("HelpRestraints_Step14Title", "The Moodles Tab");
        public readonly string Step14Desc = Loc.Localize("HelpRestraints_Step14Desc", "In this section you can define which Moodles are applied with this Set.");

        public readonly string Step15Title = Loc.Localize("HelpRestraints_Step15Title", "Moodles: Statuses");
        public readonly string Step15Desc = Loc.Localize("HelpRestraints_Step15Desc", "You can append individual Moodle Statuses here.");

        public readonly string Step16Title = Loc.Localize("HelpRestraints_Step16Title", "Moodles: Presets");
        public readonly string Step16Desc = Loc.Localize("HelpRestraints_Step16Desc", "You can append a Moodle Preset here as well, which stores a collection of statuses.");

        public readonly string Step17Title = Loc.Localize("HelpRestraints_Step17Title", "Currently Stored Moodles");
        public readonly string Step17Desc = Loc.Localize("HelpRestraints_Step17Desc", "Displays the current finalized selection of appended presets and statuses.");

        public readonly string Step18Title = Loc.Localize("HelpRestraints_Step18Title", "The Sounds Tab");
        public readonly string Step18Desc = Loc.Localize("HelpRestraints_Step18Desc", "You are able to link certain types of sounds from audio selections to your set.");

        public readonly string Step19Title = Loc.Localize("HelpRestraints_Step19Title", "Restraint Audio");
        public readonly string Step19Desc = Loc.Localize("HelpRestraints_Step19Desc", "WIP");

        public readonly string Step20Title = Loc.Localize("HelpRestraints_Step20Title", "The Hardcore Traits Tab");
        public readonly string Step20Desc = Loc.Localize("HelpRestraints_Step20Desc", "You can set which Hardcore Traits are applied when restrained by certain Kinksters here.");

        public readonly string Step21Title = Loc.Localize("HelpRestraints_Step21Title", "Selecting a Kinkster");
        public readonly string Step21Desc = Loc.Localize("HelpRestraints_Step21Desc", "First, you should select a Kinkster that you wish to set Hardcore Traits for.");

        public readonly string Step22Title = Loc.Localize("HelpRestraints_Step22Title", "Setting Hardcore Traits");
        public readonly string Step22Desc = Loc.Localize("HelpRestraints_Step22Desc", "Now you can pick which traits you want to check off that relate to your set.");
        public readonly string Step22DescExtended = Loc.Localize("HelpRestraints_Step22DescExtended", "This traits will only take affect if applied by the Kinkster you set them for.");

        public readonly string Step23Title = Loc.Localize("HelpRestraints_Step23Title", "Saving the New Set");
        public readonly string Step23Desc = Loc.Localize("HelpRestraints_Step23Desc", "Pressing this will save any changes you have made to an edited set. Or finish creation of a new one.");

        public readonly string Step24Title = Loc.Localize("HelpRestraints_Step24Title", "The Restraint Set List");
        public readonly string Step24Desc = Loc.Localize("HelpRestraints_Step24Desc", "Any created sets are listed here.");

        public readonly string Step25Title = Loc.Localize("HelpRestraints_Step25Title", "Toggling Restraint Sets");
        public readonly string Step25Desc = Loc.Localize("HelpRestraints_Step25Desc", "Pressing this button will toggle a restraint set.");

        public readonly string Step26Title = Loc.Localize("HelpRestraints_Step26Title", "Locking a Restraint Set");
        public readonly string Step26Desc = Loc.Localize("HelpRestraints_Step26Desc", "Once a set is active, this Padlock dropdown will appear. You are able to self-lock your set here.");
    }

    public class HelpCursedLoot
    {
        public readonly string Step1Title = Loc.Localize("HelpCursedLoot_Step1Title", "Creating Cursed Items");
        public readonly string Step1Desc = Loc.Localize("HelpCursedLoot_Step1Desc", "Drops down the expanded cursed item creator window.");

        public readonly string Step2Title = Loc.Localize("HelpCursedLoot_Step2Title", "The Cursed Item Name");
        public readonly string Step2Desc = Loc.Localize("HelpCursedLoot_Step2Desc", "Defines a name for the Cursed Item. This is purely for organizations and display sake.");

        public readonly string Step3Title = Loc.Localize("HelpCursedLoot_Step3Title", "Defining the type");
        public readonly string Step3Desc = Loc.Localize("HelpCursedLoot_Step3Desc", "Allows you to define the Cursed Item as a Gag, or a Equipment Piece.");
        public readonly string Step3DescExtended = Loc.Localize("HelpCursedLoot_Step3DescExtended", "Gags do not have precedence, and cannot be applied once all 3 gag layers are full.");

        public readonly string Step4Title = Loc.Localize("HelpCursedLoot_Step4Title", "Adding the New Cursed Item");
        public readonly string Step4Desc = Loc.Localize("HelpCursedLoot_Step4Desc", "Once you are finished creating the Cursed Item, pressing this button will add it to your list.");
        public readonly string Step4DescExtended = Loc.Localize("HelpCursedLoot_Step4DescExtended", "PRECAUTION: Once you add an item to your list, you cannot change it between Gag & Equip Types!");

        public readonly string Step5Title = Loc.Localize("HelpCursedLoot_Step5Title", "The Cursed Item List");
        public readonly string Step5Desc = Loc.Localize("HelpCursedLoot_Step5Desc", "The arrangement of Cursed Items you have created.");

        public readonly string Step6Title = Loc.Localize("HelpCursedLoot_Step6Title", "The Enabled Pool");
        public readonly string Step6Desc = Loc.Localize("HelpCursedLoot_Step6Desc", "The list of items that will be randomly selected from whenever you find a Mimic Chest");

        public readonly string Step7Title = Loc.Localize("HelpCursedLoot_Step7Title", "Adding Items to the Pool");
        public readonly string Step7Desc = Loc.Localize("HelpCursedLoot_Step7Desc", "This will move an cursed item into the Enabled Pool.\nGive it a shot!");

        public readonly string Step8Title = Loc.Localize("HelpCursedLoot_Step8Title", "Removing Items from the Pool");
        public readonly string Step8Desc = Loc.Localize("HelpCursedLoot_Step8Desc", "This will remove the item from the enabled pool.");

        public readonly string Step9Title = Loc.Localize("HelpCursedLoot_Step9Title", "The Lower Lock Timer Limit");
        public readonly string Step9Desc = Loc.Localize("HelpCursedLoot_Step9Desc", "Whatever you set in here will be the LOWER LIMIT of how long a Cursed Item will end up locked for.");
        public readonly string Step9DescExtended = Loc.Localize("HelpCursedLoot_Step9DescExtended", "BE AWARE: MIMIC PADLOCKS CANNOT BE UNLOCKED. YOU MUST WAIT FOR THEM TO EXPIRE. SET TIMER LIMIT ACCORDINGLY.");

        public readonly string Step10Title = Loc.Localize("HelpCursedLoot_Step10Title", "The Upper Lock Timer Limit");
        public readonly string Step10Desc = Loc.Localize("HelpCursedLoot_Step10Desc", "Whatever you set in here will be the UPPER LIMIT of how long a Cursed Item will end up locked for.");
        public readonly string Step10DescExtended = Loc.Localize("HelpCursedLoot_Step10DescExtended", "BE AWARE: MIMIC PADLOCKS CANNOT BE UNLOCKED. YOU MUST WAIT FOR THEM TO EXPIRE. SET TIMER LIMIT ACCORDINGLY.");

        public readonly string Step11Title = Loc.Localize("HelpCursedLoot_Step11Title", "The CursedItem Discovery Percent");
        public readonly string Step11Desc = Loc.Localize("HelpCursedLoot_Step11Desc", "Whatever you set here will be the %% Chance that a chest you loot will be Cursed Loot.");
    }

    public class HelpToybox
    {
        public readonly string Step1Title = Loc.Localize("HelpToybox_Step1Title", "Intiface Connection Status");
        public readonly string Step1Desc = Loc.Localize("HelpToybox_Step1Desc", "The current connection status to the Intiface Central websocket server.");
        public readonly string Step1DescExtended = Loc.Localize("HelpToybox_Step1DescExtended", "This requires you to have intiface central open. " +
            "If it is not open it will not function. See next step for info.");

        public readonly string Step2Title = Loc.Localize("HelpToybox_Step2Title", "Open Intiface");
        public readonly string Step2Desc = Loc.Localize("HelpToybox_Step2Desc", "This button will do one of 3 things:" + Environment.NewLine +
            "1. Bring Intiface Central infront of other active windows if already opened or minimized." + Environment.NewLine +
            "2. Open the Intiface Central Program if on your computer but not yet open." + Environment.NewLine +
            "3. Directs you to the download link if you do not have it installed on your computer.");

        public readonly string Step3Title = Loc.Localize("HelpToybox_Step3Title", "Selecting Vibrator Kind");
        public readonly string Step3Desc = Loc.Localize("HelpToybox_Step3Desc", "Chose Between Simulated (No IRL Toy Required), and Actual (Your IRL Toys).");

        public readonly string Step4Title = Loc.Localize("HelpToybox_Step4Title", "Simulated Audio Selection");
        public readonly string Step4Desc = Loc.Localize("HelpToybox_Step4Desc", "With a Simulated Vibrator, you can select which audio you want played to you. A quiet or normal version");

        public readonly string Step5Title = Loc.Localize("HelpToybox_Step5Title", "Playback Audio Devices");
        public readonly string Step5Desc = Loc.Localize("HelpToybox_Step5Desc", "You can also select which audio device the sound is played back to.");

        public readonly string Step6Title = Loc.Localize("HelpToybox_Step6Title", "The Device Scanner");
        public readonly string Step6Desc = Loc.Localize("HelpToybox_Step6Desc", "For actual devices, you can use the device scanner to start / stop scanning for any new IRL devices to connect with.");
        public readonly string Step6DescExtended = Loc.Localize("HelpToybox_Step6DescExtended", "Any found device should be listed below this scanner when located.");
    }

    public class HelpPatterns
    {
        public readonly string Step1Title = Loc.Localize("HelpPatterns_Step1Title", "Creating New Patterns");
        public readonly string Step1Desc = Loc.Localize("HelpPatterns_Step1Desc", "The Button to click in order to record a new pattern.");

        public readonly string Step2Title = Loc.Localize("HelpPatterns_Step2Title", "The Recorded Duration");
        public readonly string Step2Desc = Loc.Localize("HelpPatterns_Step2Desc", "Shows how long your Pattern has been recording for.");

        public readonly string Step3Title = Loc.Localize("HelpRemote_Step3Title", "The Float Button");
        public readonly string Step3Desc = Loc.Localize("HelpRemote_Step3Desc", "While active, the pink dot will not drop to the floor when released, and stay where you left it.");
        public readonly string Step3DescExtended = Loc.Localize("HelpRemote_Step3DescExtended", "Togglable via clicking it, or with MIDDLE-CLICK, while in the remote window.");

        public readonly string Step4Title = Loc.Localize("HelpRemote_Step4Title", "The Loop Button");
        public readonly string Step4Desc = Loc.Localize("HelpRemote_Step4Desc", "Begins recording from the moment you click and drag the pink dot, to the moment you release it, then repeats that data. ");
        public readonly string Step4DescExtended = Loc.Localize("HelpRemote_Step4DescExtended", "Togglable vis button interaction, or using RIGHT-CLICK, while in the remote window.");

        public readonly string Step5Title = Loc.Localize("HelpRemote_Step5Title", "The Controllable Circle");
        public readonly string Step5Desc = Loc.Localize("HelpRemote_Step5Desc", "Click and drag the pink dot to move it around. The Higher the dot is, the higher the intensity of the vibrator's motors.");

        public readonly string Step6Title = Loc.Localize("HelpRemote_Step6Title", "Starting your Recording!");
        public readonly string Step6Desc = Loc.Localize("HelpRemote_Step6Desc", "Begins storing any vibrator data recorded from dragging the circle around.");
        
        public readonly string Step7Title = Loc.Localize("HelpRemote_Step7Title", "Stopping your Recording");
        public readonly string Step7Desc = Loc.Localize("HelpRemote_Step7Desc", "When you are finished recording, press this again, and a save pattern prompt will appear.");

        public readonly string Step8Title = Loc.Localize("HelpRemote_Step8Title", "Saving Pattern Name");
        public readonly string Step8Desc = Loc.Localize("HelpRemote_Step8Desc", "Define the name of the pattern you have created.");

        public readonly string Step9Title = Loc.Localize("HelpRemote_Step9Title", "Saving Pattern Description");
        public readonly string Step9Desc = Loc.Localize("HelpRemote_Step9Desc", "Set the description of your pattern here.");

        public readonly string Step10Title = Loc.Localize("HelpRemote_Step10Title", "Saving Pattern Loop Status");
        public readonly string Step10Desc = Loc.Localize("HelpRemote_Step10Desc", "Define if your created pattern should loop once it reaches the end.");

        public readonly string Step11Title = Loc.Localize("HelpRemote_Step11Title", "Optionally Discarding Pattern.");
        public readonly string Step11Desc = Loc.Localize("HelpRemote_Step11Desc", "If you dont like the pattern you made, you can discard it here.");

        public readonly string Step12Title = Loc.Localize("HelpRemote_Step12Title", "Adding the New Pattern");
        public readonly string Step12Desc = Loc.Localize("HelpRemote_Step12Desc", "To Finialize the Pattern Creation, Save & Add the pattern here.");

        public readonly string Step13Title = Loc.Localize("HelpRemote_Step13Title", "Modifying Patterns");
        public readonly string Step13Desc = Loc.Localize("HelpRemote_Step13Desc", "Selecting a pattern from the pattern list will allow you to edit it.");

        public readonly string Step14Title = Loc.Localize("HelpRemote_Step14Title", "Editing Display Info");
        public readonly string Step14Desc = Loc.Localize("HelpRemote_Step14Desc", "In the editor there is basic display info and adjustments. Display info shows the basic labels of the pattern");

        public readonly string Step15Title = Loc.Localize("HelpRemote_Step15Title", "Changing Pattern Loop State");
        public readonly string Step15Desc = Loc.Localize("HelpRemote_Step15Desc", "If you want to change wether the pattern loops or not, you can do so here.");

        public readonly string Step16Title = Loc.Localize("HelpRemote_Step16Title", "Changing the Start-Point");
        public readonly string Step16Desc = Loc.Localize("HelpRemote_Step16Desc", "This lets you change the point in the pattern that playback will start at.");

        public readonly string Step17Title = Loc.Localize("HelpRemote_Step17Title", "Changing the Duration");
        public readonly string Step17Desc = Loc.Localize("HelpRemote_Step17Desc", "This lets you change how long the pattern playback will go on for from its start point.");

        public readonly string Step18Title = Loc.Localize("HelpRemote_Step18Title", "Saving Changes");
        public readonly string Step18Desc = Loc.Localize("HelpRemote_Step18Desc", "Updates any changes you made to your edit.");
    }

    public class HelpAlarms
    {
        public readonly string Step1Title = Loc.Localize("HelpAlarms_Step1Title", "Creating a New Alarm");
        public readonly string Step1Desc = Loc.Localize("HelpAlarms_Step1Desc", "To create a new alarm, you must first press this button.");

        public readonly string Step2Title = Loc.Localize("HelpAlarms_Step2Title", "Setting The Alarm Name");
        public readonly string Step2Desc = Loc.Localize("HelpAlarms_Step2Desc", "Begin by defining a name for your alarm.");

        public readonly string Step3Title = Loc.Localize("HelpAlarms_Step3Title", "The localized TimeZone");
        public readonly string Step3Desc = Loc.Localize("HelpAlarms_Step3Desc", "Your current local time. This means you dont need to worry " +
            "about timezones when setting these, just make it your own time.");

        public readonly string Step4Title = Loc.Localize("HelpAlarms_Step4Title", "Setting the Alarm Time");
        public readonly string Step4Desc = Loc.Localize("HelpAlarms_Step4Desc", "You can set your time by using the mouse scrollwheel over the hour and minute numbers.");

        public readonly string Step5Title = Loc.Localize("HelpAlarms_Step5Title", "The Pattern to Play");
        public readonly string Step5Desc = Loc.Localize("HelpAlarms_Step5Desc", "Select which stored pattern you wish for the alarm to play when it goes off.");

        public readonly string Step6Title = Loc.Localize("HelpAlarms_Step6Title", "Alarm Pattern Start-Point");
        public readonly string Step6Desc = Loc.Localize("HelpAlarms_Step6Desc", "Identify at which point in the pattern the alarm should start to play at.");

        public readonly string Step7Title = Loc.Localize("HelpAlarms_Step7Title", "Alarm Pattern Duration");
        public readonly string Step7Desc = Loc.Localize("HelpAlarms_Step7Desc", "Identify for how long the patterns alarm should play for before stopping.");

        public readonly string Step8Title = Loc.Localize("HelpAlarms_Step8Title", "Alarm Frequency");
        public readonly string Step8Desc = Loc.Localize("HelpAlarms_Step8Desc", "Set the days of the week this alarm go off");

        public readonly string Step9Title = Loc.Localize("HelpAlarms_Step9Title", "Saving the Alarm");
        public readonly string Step9Desc = Loc.Localize("HelpAlarms_Step9Desc", "Save/Apply changes and append the new alarm.");

        public readonly string Step10Title = Loc.Localize("HelpAlarms_Step10Title", "The Alarm List");
        public readonly string Step10Desc = Loc.Localize("HelpAlarms_Step10Desc", "This is where all created alarms are stored.");

        public readonly string Step11Title = Loc.Localize("HelpAlarms_Step11Title", "Toggling Alarms");
        public readonly string Step11Desc = Loc.Localize("HelpAlarms_Step11Desc", "The button you need to press to toggle the alarm state.");
    }

    public class HelpTriggers
    {
        public readonly string Step1Title = Loc.Localize("HelpTriggers_Step1Title", "Creating a New Trigger");
        public readonly string Step1Desc = Loc.Localize("HelpTriggers_Step1Desc", "This is the button you use to create a new trigger.");

        public readonly string Step2Title = Loc.Localize("HelpTriggers_Step2Title", "Trigger Shared Info");
        public readonly string Step2Desc = Loc.Localize("HelpTriggers_Step2Desc", "Every Trigger Type has a name, priority, and description field to set.");

        public readonly string Step3Title = Loc.Localize("HelpTriggers_Step3Title", "Trigger Actions");
        public readonly string Step3Desc = Loc.Localize("HelpTriggers_Step3Desc", "The Trigger Action Kind you select, is the resulting action that is executed once the trigger's condition is met.");

        public readonly string Step4Title = Loc.Localize("HelpTriggers_Step4Title", "Trigger Action Kinds");
        public readonly string Step4Desc = Loc.Localize("HelpTriggers_Step4Desc", "Selecting an option from here will set the trigger action kind. Note that base on the kind you select, there are different sub-options to choose.");

        public readonly string Step5Title = Loc.Localize("HelpTriggers_Step5Title", "Selecting Trigger Types.");
        public readonly string Step5Desc = Loc.Localize("HelpTriggers_Step5Desc", "You can create many different kinds of triggers using this dropdown, let's overview some of them.");

        public readonly string Step6Title = Loc.Localize("HelpTriggers_Step6Title", "Chat Triggers");
        public readonly string Step6Desc = Loc.Localize("HelpTriggers_Step6Desc", "Chat triggers will scan for a particular message within chat, in a spesified set of channels." + Environment.NewLine +
            "If desired, you can filter it to be from a certain person.");

        public readonly string Step7Title = Loc.Localize("HelpTriggers_Step7Title", "Action Triggers");
        public readonly string Step7Desc = Loc.Localize("HelpTriggers_Step7Desc", "Will execute the trigger when a certain spell or action is used. Can configure variety of settings. (See more)");
        public readonly string Step7DescExtended = Loc.Localize("HelpTriggers_Step7DescExtended", "Can configure further settings such as damage threshold ammounts, action types beyond damage/heals, and source/target directionals.");

        public readonly string Step8Title = Loc.Localize("HelpTriggers_Step8Title", "Health % Triggers");
        public readonly string Step8Desc = Loc.Localize("HelpTriggers_Step8Desc", "Fires a trigger when you or another player passes above or below a certain health value. Can be a raw value or percentage.");

        public readonly string Step9Title = Loc.Localize("HelpTriggers_Step9Title", "Restraint Triggers");
        public readonly string Step9Desc = Loc.Localize("HelpTriggers_Step9Desc", "Fires a trigger whenever a particular restraint set becomes either enabled or locked.");

        public readonly string Step10Title = Loc.Localize("HelpTriggers_Step10Title", "Gag Triggers");
        public readonly string Step10Desc = Loc.Localize("HelpTriggers_Step10Desc", "Fires a trigger whenever a particular Gag is applied or locked.");

        public readonly string Step11Title = Loc.Localize("HelpTriggers_Step11Title", "Social Triggers");
        public readonly string Step11Desc = Loc.Localize("HelpTriggers_Step11Desc", "Fires a trigger whenever you fail a social game.");
        public readonly string Step11DescExtended = Loc.Localize("HelpTriggers_Step11DescExtended", "Currently only supports DeathRolls");

        public readonly string Step12Title = Loc.Localize("HelpTriggers_Step12Title", "Emote Triggers");
        public readonly string Step12Desc = Loc.Localize("HelpTriggers_Step12Desc", "Fires whenever an emote is executed.");

        public readonly string Step13Title = Loc.Localize("HelpTriggers_Step13Title", "Saving your Trigger");
        public readonly string Step13Desc = Loc.Localize("HelpTriggers_Step13Desc", "When you are satisfied with your trigger settings, click to create the trigger.");

        public readonly string Step14Title = Loc.Localize("HelpTriggers_Step14Title", "The Trigger List");
        public readonly string Step14Desc = Loc.Localize("HelpTriggers_Step14Desc", "The space where your created triggers will be listed.");

        public readonly string Step15Title = Loc.Localize("HelpTriggers_Step15Title", "Toggling Triggers");
        public readonly string Step15Desc = Loc.Localize("HelpTriggers_Step15Desc", "Clicking this button switches the triggers between off and on.");
    }

    public class HelpAchievements
    {
        public readonly string Step1Title = Loc.Localize("HelpAchievements_Step1Title", "Your Overall Progress");
        public readonly string Step1Desc = Loc.Localize("HelpAchievements_Step1Desc", "Shows the overall number of achievements you have completed.");

        public readonly string Step2Title = Loc.Localize("HelpAchievements_Step2Title", "Resetting Achievements");
        public readonly string Step2Desc = Loc.Localize("HelpAchievements_Step2Desc", "Resets all achievement progress.");

        public readonly string Step3Title = Loc.Localize("HelpAchievements_Step3Title", "Achievement Module Sections");
        public readonly string Step3Desc = Loc.Localize("HelpAchievements_Step3Desc", "Achievements are split into components for your convience and for organization. All components are listed here.");

        public readonly string Step4Title = Loc.Localize("HelpAchievements_Step4Title", "Achievement Titles");
        public readonly string Step4Desc = Loc.Localize("HelpAchievements_Step4Desc", "Every Achievement has a Title. Once you earn this achievement, " +
            "you are able to set a Title from your unlocked Achievements to your Kinkplate.");

        public readonly string Step5Title = Loc.Localize("HelpAchievements_Step5Title", "Achievement Progress Meter");
        public readonly string Step5Desc = Loc.Localize("HelpAchievements_Step5Desc", "The current progress you have made towards completing an achievement.");
        public readonly string Step5DescExtended = Loc.Localize("HelpAchievements_Step5DescExtended", "Achievements are catagorized into many types: " + Environment.NewLine +
            "- Condition Based (Require Fulfilling a Condition)" + Environment.NewLine +
            "- Progress Based (Require a certain amount of progress to be made)" + Environment.NewLine +
            "- Time Based (Require a certain amount of time to pass)" + Environment.NewLine +
            "- Threshold Based (Require a certain limit to be surpassed at any moment in time." + Environment.NewLine +
            "- Duration Based (Require a certain amount of time to be spent in a certain state.)");

        public readonly string Step6Title = Loc.Localize("HelpAchievements_Step6Title", "Achievement Rewards");
        public readonly string Step6Desc = Loc.Localize("HelpAchievements_Step6Desc", "Achievement Rewards come in the form of profile cosmetic customizations, in addition to your title, and can be previewed here.");
        public readonly string Step6DescExtended = Loc.Localize("HelpAchievements_Step6DescExtended", "Cosmetic rewards can be used to decorate your profile in the profile editor.");
    }

    #endregion Tutorials

    #region CoreUi
    public class CoreUi
    {
        public Tabs Tabs { get; set; } = new();
        public Homepage Homepage { get; set; } = new();
        public Whitelist Whitelist { get; set; } = new();
        public Discover Discover { get; set; } = new();
        public Account Account { get; set; } = new();
        public Warnings Warnings { get; set; } = new();
    }

    public class Tabs
    {
        public readonly string MenuTabHomepage = Loc.Localize("Tabs_MenuTabHomepage", "Home");
        public readonly string MenuTabWhitelist = Loc.Localize("Tabs_MenuTabWhitelist", "Pair Whitelist");
        public readonly string MenuTabDiscover = Loc.Localize("Tabs_MenuTabDiscover", "Pattern Hub");
        public readonly string MenuTabGlobalChat = Loc.Localize("Tabs_MenuTabGlobalChat", "Global Cross-Region Chat");
        public readonly string MenuTabAccount = Loc.Localize("Tabs_MenuTabAccount", "Account Settings");

        public readonly string ToyboxOverview = Loc.Localize("Tabs_ToyboxOverview", "Overview");
        public readonly string ToyboxVibeServer = Loc.Localize("Tabs_ToyboxVibeServer", "Vibe Server");
        public readonly string Patterns = Loc.Localize("Tabs_Patterns", "Patterns");
        public readonly string ToyboxTriggers = Loc.Localize("Tabs_ToyboxTriggers", "Triggers");
        public readonly string ToyboxAlarms = Loc.Localize("Tabs_ToyboxAlarms", "Alarms");

        public readonly string AchievementsComponentGeneral = Loc.Localize("Tabs_AchievementsComponentGeneral", "General");
        public readonly string AchievementsComponentOrders = Loc.Localize("Tabs_AchievementsComponentOrders", "Orders");
        public readonly string AchievementsComponentGags = Loc.Localize("Tabs_AchievementsComponentGags", "Gags");
        public readonly string AchievementsComponentWardrobe = Loc.Localize("Tabs_AchievementsComponentWardrobe", "Wardrobe");
        public readonly string AchievementsComponentPuppeteer = Loc.Localize("Tabs_AchievementsComponentPuppeteer", "Puppeteer");
        public readonly string AchievementsComponentToybox = Loc.Localize("Tabs_AchievementsComponentToybox", "Toybox");
        public readonly string AchievementsComponentsHardcore = Loc.Localize("Tabs_AchievementsComponentsHardcore", "Hardcore");
        public readonly string AchievementsComponentRemotes = Loc.Localize("Tabs_AchievementsComponentRemotes", "Sex Toy Remote");
        public readonly string AchievementsComponentSecrets = Loc.Localize("Tabs_AchievementsComponentSecrets", "Hidden");
    }

    public class Homepage
    {
        // Add more here if people actually care for it.
    }

    public class Whitelist
    {
        // Add more here if people actually care for it.
    }

    public class Discover
    {
        // Add more here if people actually care for it.
    }

    public class Account
    {
        // Add more here if people actually care for it.
    }

    public class Warnings
    {
        // Add more here if people actually care for it.
    }
    #endregion CoreUi

    #region Settings
    public class Settings
    {
        public readonly string OptionalPlugins = Loc.Localize("Settings_OptionalPlugins", "Plugins:");
        public readonly string PluginValid = Loc.Localize("Settings_PluginValid", "Plugin enabled and up to date.");
        public readonly string PluginInvalid = Loc.Localize("Settings_PluginInvalid", "Plugin is not up to date or GagSpeak has an outdated API.");
        public readonly string AccountClaimText = Loc.Localize("Settings_AccountClaimText", "Register account:");

        public readonly string TabsGlobal = Loc.Localize("Settings_TabsGlobal", "General");
        public readonly string TabsHardcore = Loc.Localize("Settings_TabsHardcore", "Hardcore");
        public readonly string TabsPreferences = Loc.Localize("Settings_TabsPreferences", "Chat & UI");
        public readonly string TabsAccounts = Loc.Localize("Settings_TabsAccounts", "Account Management");

        public MainOptions MainOptions { get; set; } = new();
        public Hardcore Hardcore { get; set; } = new();
        public ForcedStay ForcedStay { get; set; } = new();
        public Preferences Preferences { get; set; } = new();
        public Accounts Accounts { get; set; } = new();
    }

    public class MainOptions
    {
        public readonly string HeaderGags = Loc.Localize("MainOptions_HeaderGags", "Gags");
        public readonly string HeaderWardrobe = Loc.Localize("MainOptions_HeaderWardrobe", "Wardrobe");
        public readonly string HeaderPuppet = Loc.Localize("MainOptions_HeaderPuppet", "Puppeteer");
        public readonly string HeaderToybox = Loc.Localize("MainOptions_HeaderToybox", "Toybox");

        public readonly string LiveChatGarbler = Loc.Localize("MainOptions_LiveChatGarbler", "Live Chat Garbler");
        public readonly string LiveChatGarblerTT = Loc.Localize("MainOptions_LiveChatGarblerTT", "Generates garbled text using GagSpeak's server-side chat garbler." +
                "--SEP--Note: Garbled text is visible to other players.");

        public readonly string GaggedNameplates = Loc.Localize("MainOptions_GaggedNameplates", "Gagged Nameplates");
        public readonly string GaggedNameplatesTT = Loc.Localize("MainOptions_GaggedNameplatesTT", "Displays custom icons indicating your gagstate on your nameplate while gagged!");

        public readonly string GagGlamours = Loc.Localize("MainOptions_GagGlamours", "Gag Glamours");
        public readonly string GagGlamoursTT = Loc.Localize("MainOptions_GagGlamoursTT", "Allows Glamourer to apply gag glamour items from your Gag Storage.");

        public readonly string GagPadlockTimer = Loc.Localize("MainOptions_GagPadlockTimer", "Expired Timer Gag Removal");
        public readonly string GagPadlockTimerTT = Loc.Localize("MainOptions_GagPadlockTimerTT", "Automatically removes locked gags when the timer expires.");

        public readonly string WardrobeActive = Loc.Localize("MainOptions_WardrobeActive", "Wardrobe Features");
        public readonly string WardrobeActiveTT = Loc.Localize("MainOptions_WardrobeActiveTT", "Enables Wardrobe functionality.");

        public readonly string RestrictionGlamours = Loc.Localize("MainOptions_RestrictionGlamours", "Restriction Glamours");
        public readonly string RestrictionGlamoursTT = Loc.Localize("MainOptions_RestrictionGlamoursTT", "Allows Glamourer to apply restraint glamour items from your Restraint Storage." +
            "--SEP--Restraint glamours can be created in the Wardrobe Interface.");

        public readonly string RestraintSetGlamour = Loc.Localize("MainOptions_RestraintSetGlamour", "Restraint Glamours");
        public readonly string RestraintSetGlamourTT = Loc.Localize("MainOptions_RestraintSetGlamourTT", "Allows Glamourer to apply restraints from your Restraint Sets." +
            "--SEP--Restraint sets can be created in the Wardrobe Interface.");

        public readonly string RestraintPadlockTimer = Loc.Localize("MainOptions_RestraintPadlockTimer", "Expired Timer Restraint Removal");
        public readonly string RestraintPadlockTimerTT = Loc.Localize("MainOptions_RestraintPadlockTimerTT", "Automatically removes locked restraints when the timer expires.");

        public readonly string CursedLootActive = Loc.Localize("MainOptions_CursedLootActive", "Cursed Loot");
        public readonly string CursedLootActiveTT = Loc.Localize("MainOptions_CursedLootActiveTT", "Enables Cursed Loot functionality." +
            "--SEP--When opening dungeon coffers, there is a chance that a gag or restraint will be applied and locked." +
            "--SEP--Cursed Loot timers and chance can be set under Wardrobe > Cursed Loot and CANNOT be unlocked. ");


        public readonly string MimicsApplyTraits = Loc.Localize("MainOptions_MimicsApplyTraits", "Cursed Loot can Apply Traits");
        public readonly string MimicsApplyTraitsTT = Loc.Localize("MainOptions_MimicsApplyTraitsTT", "Allows applied cursed items to set their hardcore traits." +
            "--SEP--WARNING: This includes traits such as immobilize, weighted, and other action limiting factors!");

        public readonly string MoodlesActive = Loc.Localize("MainOptions_MoodlesActive", "Moodles Integration");
        public readonly string MoodlesActiveTT = Loc.Localize("MainOptions_MoodlesActiveTT", "Enables Moodles integration and functionality.");

        public readonly string PuppeteerActive = Loc.Localize("MainOptions_PuppeteerActive", "Puppeteer Features");
        public readonly string PuppeteerActiveTT = Loc.Localize("MainOptions_PuppeteerActiveTT", "Enables Puppeteer functionality.");

        public readonly string GlobalTriggerPhrase = Loc.Localize("MainOptions_GlobalTriggerPhrase", "Global Trigger Phrase");
        public readonly string GlobalTriggerPhraseTT = Loc.Localize("MainOptions_GlobalTriggerPhraseTT", "Sets a global trigger phrase for Puppeteer." +
            "--SEP--This trigger phrase will work when said by ANYONE.");

        public readonly string GlobalSit = Loc.Localize("MainOptions_GlobalSit", "Globally Allow Sit Requests");
        public readonly string GlobalSitTT = Loc.Localize("MainOptions_GlobalSitTT", "Allows anyone to request a sit from you." +
            "--SEP--This permission limits commands to /groundsit, /sit and /changepose (/cpose). ");


        public readonly string GlobalMotion = Loc.Localize("MainOptions_GlobalMotion", "Globally Allow Motion Requests");
        public readonly string GlobalMotionTT = Loc.Localize("MainOptions_GlobalKneelTT", "Allow anyone to request a motion action from you." +
            "--SEP--A motion request includes any emotes or expressions that can be found in Emotes." +
            "--SEP--This permission limits commands to emotes and expressions.");
        public readonly string GlobalAlias = Loc.Localize("MainOptions_GlobalAlias", "Globally Allow Alias Requests");
        public readonly string GlobalAliasTT = Loc.Localize("MainOptions_GlobalAliasTT", "Allows anyone to request that you use a global alias action you have configured." +
            "--SEP--This permission includes any game and plugin commands that originate from an alias you have configured." +
            "--SEP--WARNING: Use this responsibly and with caution as it will allow ANYONE to execute commands you have in your alias's.");
        public readonly string GlobalAll = Loc.Localize("MainOptions_GlobalAll", "Globally Allow All Requests");
        public readonly string GlobalAllTT = Loc.Localize("MainOptions_GlobalAllTT", "Allows anyone to request any action from you." +
            "--SEP--This permission includes any game and plugin commands, emotes and expressions." +
            "--SEP--WARNING: Use this responsibly and with caution as it will allow ANYONE to interact with your character and game client (e.g /logout).");

        public readonly string ToyboxActive = Loc.Localize("MainOptions_ToyboxActive", "Toybox Features");
        public readonly string ToyboxActiveTT = Loc.Localize("MainOptions_ToyboxActiveTT", "Enables Toybox functionality.");

        public readonly string IntifaceAutoConnect = Loc.Localize("MainOptions_IntifaceAutoConnect", "Auto-Connect to Intiface");
        public readonly string IntifaceAutoConnectTT = Loc.Localize("MainOptions_IntifaceAutoConnectTT", "Automatically connect to the Intiface Desktop App when GagSpeak starts.");

        public readonly string IntifaceAddressTT = Loc.Localize("MainOptions_IntifaceAddressTT", "Set a custom Intiface server address." +
            "--SEP--Leave blank to use the default Intiface server address.");

        public readonly string VibeServerAutoConnect = Loc.Localize("MainOptions_VibeServerAutoConnect", "Auto-Connect to Vibe Server");
        public readonly string VibeServerAutoConnectTT = Loc.Localize("MainOptions_VibeServerAutoConnectTT", "Automatically connect to the Vibe Server when GagSpeak starts.");

        public readonly string SpatialAudioActive = Loc.Localize("MainOptions_SpatialAudioActive", "Spatial Audio Features");
        public readonly string SpatialAudioActiveTT = Loc.Localize("MainOptions_SpatialAudioActiveTT", "Emits vibrator audio to nearby paired players when a sex toy is active." +
            "--SEP--Also allows you to hear nearby paired players' sex toy vibrations when they are active.");

        public readonly string PiShockKeyTT = Loc.Localize("MainOptions_PiShockKeyTT", "Required PiShock API key for any PiShock related items to function.");
        public readonly string PiShockUsernameTT = Loc.Localize("MainOptions_PiShockUsernameTT", "Username associated with the PiShock API key.");
        public readonly string PiShockShareCodeRefreshTT = Loc.Localize("MainOptions_PiShockShareCodeRefreshTT", "Forces Global PiShock Share Code to obtain updated data from the API and push it to other online pairs.");
        public readonly string PiShockShareCode = Loc.Localize("MainOptions_PiShockShareCode", "Global PiShock Share Code");
        public readonly string PiShockShareCodeTT = Loc.Localize("MainOptions_PiShockShareCodeTT", "Global PiShock Share Code used for your connected ShockCollar." +
            "--SEP--NOTE: Only paired players with access to your Hardcore mode will have access.");

        public readonly string PiShockVibeTime = Loc.Localize("MainOptions_PiShockVibeTime", "Global Max Vibration Time");
        public readonly string PiShockVibeTimeTT = Loc.Localize("MainOptions_PiShockVibeTimeTT", "The maximum time in seconds that your shock collar can vibrate for.");
        public readonly string PiShockPermsLabel = Loc.Localize("MainOptions_PiShockPermsLabel", "Global Shock Collar Permissions (Parsed From Share Code)");
        public readonly string PiShockAllowShocks = Loc.Localize("MainOptions_PiShockAllowShocks", "Allow Shocks");
        public readonly string PiShockAllowVibes = Loc.Localize("MainOptions_PiShockAllowVibes", "Allow Vibrations");
        public readonly string PiShockAllowBeeps = Loc.Localize("MainOptions_PiShockAllowBeeps", "Allow Beeps");
        public readonly string PiShockMaxShockIntensity = Loc.Localize("MainOptions_PiShockMaxShockIntensity", "Global Max Shock Intensity: ");
        public readonly string PiShockMaxShockDuration = Loc.Localize("MainOptions_PiShockMaxShockDuration", "Global Max Shock Duration: ");
    }

    public class Hardcore
    {
        public readonly string TabBlindfold = Loc.Localize("Hardcore_TabBlindfold", "Blindfold Item");
        public readonly string TabForcedStayFilters = Loc.Localize("Hardcore_TabForcedStayFilters", "Forced Stay Filters");

        public readonly string BlindfoldSlot = Loc.Localize("Hardcore_BlindfoldSlot", "Slot");
        public readonly string BlindfoldItem = Loc.Localize("Hardcore_BlindfoldItem", "Item");
        public readonly string BlindfoldDye = Loc.Localize("Hardcore_BlindfoldDye", "Dyes");
        public readonly string BlindfoldFirstPerson = Loc.Localize("Hardcore_BlindfoldFirstPerson", "Force First-Person Perspective");
        public readonly string BlindfoldFirstPersonTT = Loc.Localize("Hardcore_BlindfoldFirstPersonTT", "Forces you into first-person perspective when blindfolded.");

        public readonly string BlindfoldType = Loc.Localize("Hardcore_BlindfoldType", "Lace Style");
        public readonly string BlindfoldTypeTT = Loc.Localize("Hardcore_BlindfoldTypeTT", "Select the type of blindfold you wish to use.");

        public readonly string OverlayMaxOpacity = Loc.Localize("Hardcore_OverlayMaxOpacity", "Opacity");
        public readonly string OverlayMaxOpacityTT = Loc.Localize("Hardcore_OverlayMaxOpacityTT", "Set the opacity of the blindfold to account for variance in monitor displays.");

        public readonly string AddNodeLastSeenTT = Loc.Localize("Hardcore_AddNodeLastSeenTT", "Add last interacted node to list." +
            "--SEP--Note: Auto-selecting yes is not an allowed option");
        public readonly string AddNodeNewTT = Loc.Localize("Hardcore_AddNodeNew", "Add a new TextNode to the ForcedStay Prompt List.");
        public readonly string AddNodeNewChamberTT = Loc.Localize("Hardcore_AddNodeNewChamber", "Add a new ChamberNode to the ForcedStay Prompt List.");
        public readonly string ChamberAutoMoveTT = Loc.Localize("Hardcore_ChamberAutoMoveTT", "Automatically move to the chamber when entering an estate.");
    }

    public class ForcedStay
    {
        public readonly string LeaveAPTFriendly = Loc.Localize("ForcedStay_LeaveAPTFriendly", "[ForcedStay] Prevent Apartment Leaving");
        public readonly string LeaveAPTName = Loc.Localize("ForcedStay_LeaveAPTName", "Exit");
        public readonly string LeaveAPTOption = Loc.Localize("ForcedStay_LeaveAPTOption", "Cancel");

        public readonly string LeaveChamberFriendly = Loc.Localize("ForcedStay_LeaveChamberFriendly", "[ForcedStay] Prevent Chamber Leaving");
        public readonly string LeaveChamberName = Loc.Localize("ForcedStay_LeaveChamberName", "Exit");
        public readonly string LeaveChamberLabel = Loc.Localize("ForcedStay_LeaveChamberLabel", "What would you like to do?");
        public readonly string LeaveChamberOption = Loc.Localize("ForcedStay_LeaveChamberOption", "Nothing.");

        public readonly string LeaveEstateFriendly = Loc.Localize("ForcedStay_LeaveEstateFriendly", "[ForcedStay] Prevent Estate Leaving");
        public readonly string LeaveEstateName = Loc.Localize("ForcedStay_LeaveEstateName", "Exit");
        public readonly string LeaveEstateLabel = Loc.Localize("ForcedStay_LeaveEstateLabel", "Leave the estate hall?");
        public readonly string LeaveEstateOption = Loc.Localize("ForcedStay_LeaveEstateOption", "No");

        public readonly string EnterEstateFriendly = Loc.Localize("ForcedStay_EnterEstateFriendly", "[ForcedStay] Auto-Enter Estate (Prevents Logout Escape)");
        public readonly string EnterEstateName = Loc.Localize("ForcedStay_EnterEstateName", "Entrance");
        public readonly string EnterEstateLabel = Loc.Localize("ForcedStay_EnterEstateLabel", "Enter the estate hall?");
        public readonly string EnterEstateOption = Loc.Localize("ForcedStay_EnterEstateOption", "Yes");

        public readonly string EnterAPTOneFriendly = Loc.Localize("ForcedStay_EnterAPTOneFriendly", "[ForcedStay] Open Apartment Menu (1/3)");
        public readonly string EnterAPTOneName = Loc.Localize("ForcedStay_EnterAPTOneName", "Apartment Building Entrance");
        public readonly string EnterAPTOneOption = Loc.Localize("ForcedStay_EnterAPTOneOption", "Go to specified apartment");

        public readonly string EnterAPTTwoFriendly = Loc.Localize("ForcedStay_EnterAPTTwoFriendly", "[ForcedStay] Select Apartment Room (2/3)");
        public readonly string EnterAPTTwoName = Loc.Localize("ForcedStay_EnterAPTTwoName", "Apartment Building Entrance");

        public readonly string EnterAPTThreeFriendly = Loc.Localize("ForcedStay_EnterAPTThreeFriendly", "[ForcedStay] Enter Selected Apartment (3/3)");
        public readonly string EnterAPTThreeName = Loc.Localize("ForcedStay_EnterAPTThreeName", "Apartment Building Entrance");
        public readonly string EnterAPTThreeLabel = Loc.Localize("ForcedStay_EnterAPTThreeLabel", @"/^Enter .+?'s room\\?$/");
        public readonly string EnterAPTThreeOption = Loc.Localize("ForcedStay_EnterAPTThreeOption", "Yes");

        public readonly string EnterFCOneFriendly = Loc.Localize("ForcedStay_EnterFCOneFriendly", "[ForcedStay] Open FC Chambers Menu (1/3)");
        public readonly string EnterFCOneName = Loc.Localize("ForcedStay_EnterFCOneName", "Entrance to Additional Chambers");
        public readonly string EnterFCOneOption = Loc.Localize("ForcedStay_EnterFCOneOption", "Move to specified private chambers");

        public readonly string EnterFCTwoFriendly = Loc.Localize("ForcedStay_EnterFCTwoFriendly", "[ForcedStay] Select FC Chamber Room (2/3)");
        public readonly string EnterFCTwoName = Loc.Localize("ForcedStay_EnterFCTwoName", "Entrance to Additional Chambers");

        public readonly string EnterFCThreeFriendly = Loc.Localize("ForcedStay_EnterFCThreeFriendly", "[ForcedStay] Enter Selected Chamber (3/3)");
        public readonly string EnterFCThreeName = Loc.Localize("ForcedStay_EnterFCThreeName", "Apartment Building Entrance");
        public readonly string EnterFCThreeLabel = Loc.Localize("ForcedStay_EnterFCThreeLabel", @"/^Enter .+?'s room\\?$/");
        public readonly string EnterFCThreeOption = Loc.Localize("ForcedStay_EnterFCThreeOption", "Yes");
    }

    public class Preferences
    {
        public readonly string LangDialectLabel = Loc.Localize("Preferences_LangLabel", "Language & Region:");
        public readonly string LangTT = Loc.Localize("Preferences_LangTT", "Select language for GagSpeak Live Chat Garbler.");
        public readonly string DialectTT = Loc.Localize("Preferences_DialectTT", "Select region for GagSpeak Live Chat Garbler.");
        public readonly string HeaderPuppet = Loc.Localize("Preferences_HeaderPuppet", "Puppeteer Channels");

        // UI Preferences Section
        public readonly string HeaderUiPrefs = Loc.Localize("Preferences_HeaderUiPrefs", "User Interface");

        public readonly string ShowMainUiOnStartLabel = Loc.Localize("Preferences_ShowMainUiOnStartLabel", "Open the Main Window UI upon plugin startup.");
        public readonly string ShowMainUiOnStartTT = Loc.Localize("Preferences_ShowMainUiOnStartTT", "Determines if the Main UI will open upon plugin startup or not.");

        public readonly string EnableDtrLabel = Loc.Localize("Preferences_EnableDtrEntryLabel", "Display status and visible pair count in Server Info Bar");
        public readonly string EnableDtrTT = Loc.Localize("Preferences_EnableDtrEntryTT", "Adds GagSpeak connection status and visible pair count to the Server Info Bar.");

        public readonly string PrivacyRadarLabel = Loc.Localize("Preferences_PrivacyRadarLabel", "Privacy Radar DTR Entry");
        public readonly string PrivacyRadarTT = Loc.Localize("Preferences_PrivacyRadarTT", "Displays any non-GagSpeak paired player within render range for privacy.");

        public readonly string ActionsNotifLabel = Loc.Localize("Preferences_ActionsNotifLabel", "Actions Notifier DTR Entry");
        public readonly string ActionsNotifTT = Loc.Localize("Preferences_ActionsNotifTT", "Displays a bell icon when a paired player uses an action on you.");

        public readonly string VibeStatusLabel = Loc.Localize("Preferences_VibeStatusLabel", "Vibe Status DTR Entry");
        public readonly string VibeStatusTT = Loc.Localize("Preferences_VibeStatusTT", "Displays a vibe icon when you have an actively vibrating sex toy.");

        public readonly string ShowVisibleSeparateLabel = Loc.Localize("Preferences_ShowVisibleSeparateLabel", "Show separate Visible group");
        public readonly string ShowVisibleSeparateTT = Loc.Localize("Preferences_ShowVisibleSeparateTT", "Lists paired players within render range in a separate group.");

        public readonly string ShowOfflineSeparateLabel = Loc.Localize("Preferences_ShowOfflineSeparateLabel", "Show separate Offline group");
        public readonly string ShowOfflineSeparateTT = Loc.Localize("Preferences_ShowOfflineSeparateTT", "Lists offline paired players in a separate group.");

        public readonly string PrefThreeCharaAnonName = Loc.Localize("Preferences_ThreeCharaAnonName", "Display [Kinkster-###] over [Kinkster-####] in Global Chat");
        public readonly string PrefThreeCharaAnonNameTT = Loc.Localize("Preferences_ThreeCharaAnonNameTT", "Displays the first three characters of a player's name instead of 4." +
            "--SEP--Primary intended for legacy users attached to their 3 character names.");

        public readonly string PreferNicknamesLabel = Loc.Localize("Preferences_PreferNicknamesLabel", "Prefer nicknames for visible pairs");
        public readonly string PreferNicknamesTT = Loc.Localize("Preferences_PreferNicknamesTT", "Displays nicknames instead of character names for paired players within render range.");

        public readonly string ShowProfilesLabel = Loc.Localize("Preferences_ShowProfilesLabel", "Show GagSpeak profiles on hover");
        public readonly string ShowProfilesTT = Loc.Localize("Preferences_ShowProfilesTT", "Displays the configured user profile after hovering over the player.");

        public readonly string ProfileDelayLabel = Loc.Localize("Preferences_ProfileDelayLabel", "Hover Delay");
        public readonly string ProfileDelayTT = Loc.Localize("Preferences_ProfileDelayTT", "Sets the delay before a profile is displayed on hover.");

        public readonly string ContextMenusLabel = Loc.Localize("Preferences_ShowContextMenusLabel", "Enable right-click context menu for visible pairs");
        public readonly string ContextMenusTT = Loc.Localize("Preferences_ShowContextMenusTT", "Displays a context menu when right-clicking on a targeted pair." +
            "--SEP--The context menu provides quick access to pair actions or to view a KinkPlate.");

        // Notifications Section
        public readonly string HeaderNotifications = Loc.Localize("Preferences_HeaderNotifications", "Notifications");
        public readonly string ZoneChangeWarnLabel = Loc.Localize("Preferences_ZoneChangeWarnLabel", "Live Chat Garbler Warning (On Zone Change)");
        public readonly string ZoneChangeWarnTT = Loc.Localize("Preferences_ZoneChangeWarnTT", "Displays a chat and toast notification when changing zones while gagged with Live Chat Garbler enabled." +
            "--SEP--Useful in preventing accidentally speaking in undesired chat channels with garbled text.");

        public readonly string ConnectedNotifLabel = Loc.Localize("Preferences_ConnectedNotifLabel", "Enable Connection Notifications");
        public readonly string ConnectedNotifTT = Loc.Localize("Preferences_ConnectedNotifTT", "Displays a notification when server connection status changes." +
            "--SEP--Notifies you when: connected, disconnected, reconnecting or connection lost.");

        public readonly string OnlineNotifLabel = Loc.Localize("Preferences_OnlineNotifLabel", "Enable Online Pair Notifications");
        public readonly string OnlineNotifTT = Loc.Localize("Preferences_OnlineNotifTT", "Displays a notification when a pair comes online.");

        public readonly string LimitForNicksLabel = Loc.Localize("Preferences_LimitForNicksLabel", "Limit Online Pair Notifications to Nicknamed Pairs");
        public readonly string LimitForNicksTT = Loc.Localize("Preferences_LimitForNicksTT", "Limits notifications to pairs with an assigned nickname.");
    }

    public class Accounts
    {
        public readonly string PrimaryLabel = Loc.Localize("Accounts_PrimaryLabel", "Primary Account");
        public readonly string SecondaryLabel = Loc.Localize("Accounts_SecondaryLabel", "Secondary Accounts");
        public readonly string NoSecondaries = Loc.Localize("Accounts_NoSecondaries", "No secondary accounts to display." +
            "\nA secondary account key can be obtained by registering with the GagSpeak bot in the CK Discord server. An account is bound to a single character.");

        public readonly string CharaNameLabel = Loc.Localize("Accounts_CharaNameLabel", "Account Character's Name");
        public readonly string CharaWorldLabel = Loc.Localize("Accounts_CharaWorldLabel", "Account Character's World");
        public readonly string CharaKeyLabel = Loc.Localize("Accounts_CharaKeyLabel", "Account Secret Key");

        public readonly string DeleteButtonLabel = Loc.Localize("Accounts_DeleteButtonLabel", "Delete Account");
        public readonly string DeleteButtonDisabledTT = Loc.Localize("Accounts_DeleteButtonDisabledTT", "Cannot delete this account as it is not yet registered.");
        public readonly string DeleteButtonTT = Loc.Localize("Accounts_DeleteButtonTT", "Permanently deleting this account from GagSpeak servers." +
            "--SEP--WARNING: Once an account is deleted, the associated secret key will become unusable." +
            "--SEP--If you wish to create a new account for the currently logged in character, you will need to obtain a new secret key." +
            "--SEP--(A confirmation dialog will open upon clicking this button)");
        public readonly string DeleteButtonPrimaryTT = Loc.Localize("Accounts_DeleteButtonPrimaryTT", "--SEP----COL--DELETING THIS ACCOUNT WILL ALSO DELETE ALL SECONDARY ACCOUNTS");

        public readonly string FingerprintPrimary = Loc.Localize("Accounts_FingerprintPrimary", "Primary GagSpeak Account");
        public readonly string FingerprintSecondary = Loc.Localize("Accounts_FingerprintSecondary", "Secondary GagSpeak Account");

        public readonly string SuccessfulConnection = Loc.Localize("Accounts_SuccessfulConnection", "Successfully connected to the GagSpeak servers with a registered secret key." +
            "--SEP--This secret key is bound to this character and cannot be removed unless the account is deleted.");
        public readonly string NoSuccessfulConnection = Loc.Localize("Accounts_NoSuccessfulConnection", "Failed to connect to the GagSpeak servers with a registered secret key.");
        public readonly string EditKeyAllowed = Loc.Localize("Accounts_EditKeyAllowed", "Toggle display of secret key field");
        public readonly string EditKeyNotAllowed = Loc.Localize("Accounts_EditKeyNotAllowed", "Cannot change a secret key that has been verified. This character is now bound to this account.");
        public readonly string CopyKeyToClipboard = Loc.Localize("Accounts_CopyKeyToClipboard", "Click to copy secret key to clipboard");

        public readonly string RemoveAccountPrimaryWarning = Loc.Localize("Accounts_RemoveAccountPrimaryWarning", "By deleting your primary account, all secondary accounts will also be deleted.");
        public readonly string RemoveAccountWarning = Loc.Localize("Accounts_RemoveAccountWarning", "Your UID will be removed from all pairing lists.\nYou will be unable to use this secret key.");
        public readonly string RemoveAccountConfirm = Loc.Localize("Accounts_RemoveAccountConfirm", "Are you sure you want to delete this account?");
    }
    #endregion Settings

    #region Gags
    public class Gags
    {
        public ActiveGags ActiveGags { get; set; } = new();
        public GagStorage GagStorage { get; set; } = new();
    }

    public class ActiveGags
    {
        // Add more here if people actually care for it.
    }

    public class GagStorage
    {
        // Add more here if people actually care for it.
    }
    #endregion Gags

    #region Wardrobe
    public class Wardrobe
    {
        public Restrictions Restrictions { get; set; } = new();
        public Restraints Restraints { get; set; } = new();
        public CursedLoot CursedLoot { get; set; } = new();
    }

    public class Restrictions
    {
        // Add more here if people actually care for it.
    }

    public class Restraints
    {
        // Add more here if people actually care for it.
    }

    public class CursedLoot
    {
        // Add more here if people actually care for it.
        public readonly string TreasureName = Loc.Localize("CursedLoot_TreasureName", "Treasure Coffer");

    }

    #endregion Wardrobe

    #region Puppet
    public class Puppet
    {
        // just put everything in here probably, its small enough.
    }
    #endregion Puppet

    #region Toybox
    public class Toybox
    {
        public Overview Overview { get; set; } = new();
        public VibeServer VibeServer { get; set; } = new();
        public Patterns Patterns { get; set; } = new();
        public Triggers Triggers { get; set; } = new();
        public Alarms Alarms { get; set; } = new();
    }

    public class Overview
    {
        // Add more here if people actually care for it.
    }

    public class VibeServer
    {
        // Add more here if people actually care for it.
    }

    public class Patterns
    {
        // Add more here if people actually care for it.
    }

    public class Triggers
    {
        // Add more here if people actually care for it.
    }

    public class Alarms
    {
        // Add more here if people actually care for it.
    }
    #endregion Toybox
}
