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
        public HelpMainUi MainUi { get; set; } = new();
        public HelpRemote Remote { get; set; } = new();
        public HelpRestraints Restraints { get; set; } = new();
        public HelpRestrictions Restrictions { get; set; } = new();
        public HelpGags Gags { get; set; } = new();
        public HelpCursedLoot CursedLoot { get; set; } = new();
        public HelpPuppeteer Puppeteer { get; set; } = new();
        public HelpToys Toys { get; set; } = new();
        public HelpVibeLobby VibeLobby { get; set; } = new();
        public HelpPatterns Patterns { get; set; } = new();
        public HelpAlarms Alarms { get; set; } = new();
        public HelpTriggers Triggers { get; set; } = new();
        public HelpAchievements Achievements { get; set; } = new();
    }

    public class HelpMainUi
    {
        public readonly string Step1Title = Loc.Localize("HelpMainUi_Step1Title", "Startup Tutorial");
        public readonly string Step1Desc = Loc.Localize("HelpMainUi_Step1Desc", "Welcome to GagSpeak! This many Kinksters are currently online!");
        public readonly string Step1DescExtended = Loc.Localize("HelpMainUi_Step1DescExtended", "To view other tutorials like this, click (?) icons on window headers!");

        public readonly string Step2Title = Loc.Localize("HelpMainUi_Step2Title", "Connection State");
        public readonly string Step2Desc = Loc.Localize("HelpMainUi_Step2Desc", "Your current connection status.");
        public readonly string Step2DescExtended = Loc.Localize("HelpMainUi_Step2DescExtended", "You can hover over this button for more details.");

        public readonly string Step3Title = Loc.Localize("HelpMainUi_Step3Title", "Adding Kinksters");
        public readonly string Step3Desc = Loc.Localize("HelpMainUi_Step3Desc", "Send out Kinkster requests here.");
        public readonly string Step3DescExtended = Loc.Localize("HelpMainUi_Step3DescExtended", "Sent requests expire automatically within " +
            "3 days if not responded to, and can also be canceled at anytime.");

        public readonly string Step4Title = Loc.Localize("HelpMainUi_Step4Title", "Attaching Messages");
        public readonly string Step4Desc = Loc.Localize("HelpMainUi_Step4Desc", "Messages can be attached to sent Kinkster Requests, if desired.");
        public readonly string Step4DescExtended = Loc.Localize("HelpMainUi_Step4DescExtended", "These can provide context for who's sending the request, helping inform the recipient who you are!");

        public readonly string Step5Title = Loc.Localize("HelpMainUi_Step5Title", "Requests");
        public readonly string Step5Desc = Loc.Localize("HelpMainUi_Step5Desc", "You can see the requests sent to and from other Kinksters here. The Reply icon can be used to interact with individual requests quickly.");
        public readonly string Step5DescExtended = Loc.Localize("HelpMainUi_Step5DescExtended", $" ");

        public readonly string Step6Title = Loc.Localize("HelpMainUi_Step6Title", "Whitelist");
        public readonly string Step6Desc = Loc.Localize("HelpMainUi_Step6Desc", "Where your added Kinksters appear. Click the side arrow to open the interactions menu.");
        public readonly string Step6DescExtended = Loc.Localize("HelpMainUi_Step6DescExtended",
            "[LEFT CLICK] Toggle between Nickname and UID." +
            "[MIDDLE CLICK] Open the KinkPlate for the Kinkster." +
            "[SHIFT + RIGHT CLICK] Edit the nickname for this Kinkster.");

        public readonly string Step7Title = Loc.Localize("HelpMainUi_Step7Title", "Homepage");
        public readonly string Step7Desc = Loc.Localize("HelpMainUi_Step7Desc", "Access GagSpeak's modules, or manage your profile here.");
        public readonly string Step7DescExtended = Loc.Localize("HelpMainUi_Step7DescExtended", "This page contains important information about you, and access to profile setup, configs, and support links!");

        public readonly string Step8Title = Loc.Localize("HelpMainUi_Step8Title", "Client UID");
        public readonly string Step8Desc = Loc.Localize("HelpMainUi_Step8Desc", "Your UID for pairing.");
        public readonly string Step8DescExtended = Loc.Localize("HelpMainUi_Step8DescExtended", "This defines your account, you shouldn't display this in global chats or KinkPlates.");

        public readonly string Step9Title = Loc.Localize("HelpMainUi_Step9Title", "Safewords");
        public readonly string Step9Desc = Loc.Localize("HelpMainUi_Step9Desc", "Triggered with [/safeword YOURSAFEWORD], or [/safeword YOURSAFEWORD SPECIFICUID]. This removes everything from you!");
        public readonly string Step9DescExtended = Loc.Localize("HelpMainUi_Step9DescExtended", "Using your safeword will override everything, so please use it responsibly. " +
            "If you get stuck and can't use your chat, you can use the hardcore safeword with CTRL+ALT+Backspace, which will disable all hardcore settings across all pairs.");

        public readonly string Step10Title = Loc.Localize("HelpMainUi_Step10Title", "Setting Safeword");
        public readonly string Step10Desc = Loc.Localize("HelpMainUi_Step10Desc", "Click this text to edit your safeword.");
        public readonly string Step10DescExtended = Loc.Localize("HelpMainUi_Step10DescExtended", "Safewords have a 5 minute cooldown when used, and will remove all active bindings.");

        public readonly string Step11Title = Loc.Localize("HelpMainUi_Step11Title", "KinkPlate™ Editing");
        public readonly string Step11Desc = Loc.Localize("HelpMainUi_Step11Desc", "Make Customizations to your KinkPlate™ here.");
        public readonly string Step11DescExtended = Loc.Localize("HelpMainUi_Step11DescExtended", "You can customize the display of your KinkPlate™, description, and Avatar here.");

        public readonly string Step12Title = Loc.Localize("HelpMainUi_Step12Title", "KinkPlate™ Publicity");
        public readonly string Step12Desc = Loc.Localize("HelpMainUi_Step12Desc", "If a KinkPlate™ is public, it can be viewed in Global Chat.");
        public readonly string Step12DescExtended = Loc.Localize("HelpMainUi_Step12DescExtended", "Private KinkPlates™ can only be viewed by yourself and your Kinkster pairs.");

        public readonly string Step13Title = Loc.Localize("HelpMainUi_Step13Title", "KinkPlate™ Titles");
        public readonly string Step13Desc = Loc.Localize("HelpMainUi_Step13Desc", "Earned through Achievements, and displayed on your KinkPlate™.");
        public readonly string Step13DescExtended = Loc.Localize("HelpMainUi_Step13DescExtended", "Over 200 different titles exist, and are shown in Light and Full KinkPlate™'s");

        public readonly string Step14Title = Loc.Localize("HelpMainUi_Step14Title", "Customizing Profile");
        public readonly string Step14Desc = Loc.Localize("HelpMainUi_Step14Desc", "Unlocked as rewards from Achievements! (WIP) (SEE INFO)");
        public readonly string Step14DescExtended = Loc.Localize("HelpMainUi_Step14DescExtended", "KinkPlate™ customizations are still (WIP) as I have no graphic artist support, " +
            "if you would like to contribute, please let me know! Until then, this is WIP.");

        public readonly string Step15Title = Loc.Localize("HelpMainUi_Step15Title", "Profile Description");
        public readonly string Step15Desc = Loc.Localize("HelpMainUi_Step15Desc", "More space than the search info provides!");
        public readonly string Step15DescExtended = Loc.Localize("HelpMainUi_Step15DescExtended", "Results can vary based on how the description is calculated, " +
            "preview result on light and full KinkPlates!");

        public readonly string Step16Title = Loc.Localize("HelpMainUi_Step16Title", "Previewing Light KinkPlate™");
        public readonly string Step16Desc = Loc.Localize("HelpMainUi_Step16Desc", "Your light KinkPlate™ can be previewed here.");
        public readonly string Step16DescExtended = Loc.Localize("HelpMainUi_Step16DescExtended", "Light KinkPlates only display profile image, supporter tier, titles, and " +
            "descriptions.");

        public readonly string Step17Title = Loc.Localize("HelpMainUi_Step17Title", "Previewing Full KinkPlate™");
        public readonly string Step17Desc = Loc.Localize("HelpMainUi_Step17Desc", "Your full KinkPlate™ can be previewed here.");
        public readonly string Step17DescExtended = Loc.Localize("HelpMainUi_Step17DescExtended", "Full KinkPlates can reflect your current restrictions, hardcore traits, and " +
            "hardcore states!");

        public readonly string Step18Title = Loc.Localize("HelpMainUi_Step18Title", "Editing Profile Image");
        public readonly string Step18Desc = Loc.Localize("HelpMainUi_Step18Desc", "You can edit your profile image here.");
        public readonly string Step18DescExtended = Loc.Localize("HelpMainUi_Step18DescExtended", "The editor lets you pan, resize, rotate, and zoom uploaded files of any size to the fit you like!");

        public readonly string Step19Title = Loc.Localize("HelpMainUi_Step19Title", "Saving Profile Changes");
        public readonly string Step19Desc = Loc.Localize("HelpMainUi_Step19Desc", "Make sure you save changes, or edits will be lost!");
        public readonly string Step19DescExtended = Loc.Localize("HelpMainUi_Step19DescExtended", " ");

        public readonly string Step20Title = Loc.Localize("HelpMainUi_Step20Title", "Pattern Hub");
        public readonly string Step20Desc = Loc.Localize("HelpMainUi_Step20Desc", "Browse and explore patterns uploaded by others.");
        public readonly string Step20DescExtended = Loc.Localize("HelpMainUi_Step20DescExtended", " ");

        public readonly string Step21Title = Loc.Localize("HelpMainUi_Step21Title", "Pattern Search");
        public readonly string Step21Desc = Loc.Localize("HelpMainUi_Step21Desc", "Use tags and filters to narrow your search results.");
        public readonly string Step21DescExtended = Loc.Localize("HelpMainUi_Step21DescExtended", "Up to a maximum of 50 results are polled, so if " +
            "you can't find the result you are looking for, narrow it with filters!");

        public readonly string Step22Title = Loc.Localize("HelpMainUi_Step22Title", "Pattern Results");
        public readonly string Step22Desc = Loc.Localize("HelpMainUi_Step22Desc", "Results let you preview devices & motors used, duration, and authors.");
        public readonly string Step22DescExtended = Loc.Localize("HelpMainUi_Step22DescExtended", " ");

        public readonly string Step23Title = Loc.Localize("HelpMainUi_Step23Title", "Moodle Hub");
        public readonly string Step23Desc = Loc.Localize("HelpMainUi_Step23Desc", "Browse and explore Moodles uploaded by others.");
        public readonly string Step23DescExtended = Loc.Localize("HelpMainUi_Step23DescExtended", " ");

        public readonly string Step24Title = Loc.Localize("HelpMainUi_Step24Title", "Moodle Search");
        public readonly string Step24Desc = Loc.Localize("HelpMainUi_Step24Desc", "Use tags and filters to narrow your search results.");
        public readonly string Step24DescExtended = Loc.Localize("HelpMainUi_Step24DescExtended", "Up to a maximum of 75 results are polled, so if " +
            "you can't find the result you are looking for, narrow it with filters!");

        public readonly string Step25Title = Loc.Localize("HelpMainUi_Step25Title", "Moodle Results");
        public readonly string Step25Desc = Loc.Localize("HelpMainUi_Step25Desc", "You can preview a Moodle by hovering over it's icon.");
        public readonly string Step25DescExtended = Loc.Localize("HelpMainUi_Step25DescExtended", "You can also try on, like, or grab a copy of the Moodle for yourself.");

        public readonly string Step26Title = Loc.Localize("HelpMainUi_Step26Title", "Global Chat");
        public readonly string Step26Desc = Loc.Localize("HelpMainUi_Step26Desc", "Chat Anonymously with other Kinksters from anywhere in the world with Global Chat!");
        public readonly string Step26DescExtended = Loc.Localize("HelpMainUi_Step26DescExtended", "ChatLogs are restored on reconnection, and reset at midnight every day relative to your local time zone.");

        public readonly string Step27Title = Loc.Localize("HelpMainUi_Step27Title", "Using Global Chat");
        public readonly string Step27Desc = Loc.Localize("HelpMainUi_Step27Desc", "To talk in Global Chat, you must verify your account first! This is to prevent anonymous harassment.");
        public readonly string Step27DescExtended = Loc.Localize("HelpMainUi_Step27DescExtended", "In order to verify your account, you will need to join the discord server, where further instructions can be found.");

        public readonly string Step28Title = Loc.Localize("HelpMainUi_Step28Title", "Chat Emotes");
        public readonly string Step28Desc = Loc.Localize("HelpMainUi_Step28Desc", "You can add expressive emotes to messages!");
        public readonly string Step28DescExtended = Loc.Localize("HelpMainUi_Step28DescExtended", "Emotes can also be manually added to chat messages by typing out emotes like discord emotes. :catsnuggle:");

        public readonly string Step29Title = Loc.Localize("HelpMainUi_Step29Title", "Chat Scroll");
        public readonly string Step29Desc = Loc.Localize("HelpMainUi_Step29Desc", "Sets if the window will always auto-scroll to the last sent message.");
        public readonly string Step29DescExtended = Loc.Localize("HelpMainUi_Step29DescExtended", "Turning Auto-Scroll off lets you scroll up freely.");

        public readonly string Step30Title = Loc.Localize("HelpMainUi_Step30Title", "Chat Message Examine");
        public readonly string Step30Desc = Loc.Localize("HelpMainUi_Step30Desc", "Hover messages to see when they were sent, the Kinkster's Light KinkPlate™, or send them a request!");
        public readonly string Step30DescExtended = Loc.Localize("HelpMainUi_Step30DescExtended", "Additionally, you are able to choose to add a kinkster to " +
            "your silence list, hiding messages from them until the next plugin restart.");

        public readonly string Step31Title = Loc.Localize("HelpMainUi_Step31Title", "Self Plug");
        public readonly string Step31Desc = Loc.Localize("HelpMainUi_Step31Desc", "If you ever fancy tossing a tip or becoming a supporter as a thanks for all the hard work, or just to help support me, it would be much appreciated." +
            "\n\nBut please don't feel guilty if you don't. Only support me if you want to! I will always love and cherish you all regardless ♥");
        public readonly string Step31DescExtended = Loc.Localize("HelpMainUi_Step31DescExtended", " ");
    }

    public class HelpRemote
    {
        //public readonly string Step1Title = Loc.Localize("HelpRemote_Step1Title", "The Power Button");
        //public readonly string Step1Desc = Loc.Localize("HelpRemote_Step1Desc", "When active, interactions from this remote are sent to connected devices.");

        //public readonly string Step2Title = Loc.Localize("HelpRemote_Step2Title", "The Float Button");
        //public readonly string Step2Desc = Loc.Localize("HelpRemote_Step2Desc", "While active, the pink dot will not drop to the floor when released, and stay where you left it.");
        //public readonly string Step2DescExtended = Loc.Localize("HelpRemote_Step2DescExtended", "Togglable via clicking it, or with MIDDLE-CLICK, while in the remote window.");

        //public readonly string Step3Title = Loc.Localize("HelpRemote_Step3Title", "The Loop Button");
        //public readonly string Step3Desc = Loc.Localize("HelpRemote_Step3Desc", "Begins recording from the moment you click and drag the pink dot, to the moment you release it, then repeats that data. ");
        //public readonly string Step3DescExtended = Loc.Localize("HelpRemote_Step3DescExtended", "Togglable vis button interaction, or using RIGHT-CLICK, while in the remote window.");

        //public readonly string Step4Title = Loc.Localize("HelpRemote_Step4Title", "The Timer");
        //public readonly string Step4Desc = Loc.Localize("HelpRemote_Step4Desc", "Displays how long your remote has been running for.");

        //public readonly string Step5Title = Loc.Localize("HelpRemote_Step5Title", "The Controllable Circle");
        //public readonly string Step5Desc = Loc.Localize("HelpRemote_Step5Desc", "Mouse-Interactable. Can be moved around while remote is active. Height represents Intensity Level.");

        //public readonly string Step6Title = Loc.Localize("HelpRemote_Step6Title", "The Output Display");
        //public readonly string Step6Desc = Loc.Localize("HelpRemote_Step6Desc", "A display of the recorded vibrations from the remote for visual feedback!.");

        //public readonly string Step7Title = Loc.Localize("HelpRemote_Step7Title", "The Device List");
        //public readonly string Step7Desc = Loc.Localize("HelpRemote_Step7Desc", "Shows you the affected Connected Devices that this remote is controlling.");
        //public readonly string Step7DescExtended = Loc.Localize("HelpRemote_Step7DescExtended", "During Open Betas development this feature will see further functionality, but for now it does not function.");
    }

    public class HelpRestraints
    {
        
        public readonly string Step1Title = Loc.Localize("HelpRestraints_Step1Title", "Searching");
        public readonly string Step1Desc = Loc.Localize("HelpRestraints_Step1Desc", "You can search for a specific restraint set by typing its name here.");
        public readonly string Step1DescExtended = Loc.Localize("HelpRestraints_Step1DescExtended", " ");

        public readonly string Step2Title = Loc.Localize("HelpRestraints_Step2Title", "Creating a Folder");
        public readonly string Step2Desc = Loc.Localize("HelpRestraints_Step2Desc", "Create folders to organize your restraints.");
        public readonly string Step2DescExtended = Loc.Localize("HelpRestraints_Step2DescExtended", "We'll create a new \"Tutorial Folder\" when you click next. You can rearrange things with drag and drop.");

        public readonly string Step3Title = Loc.Localize("HelpRestraints_Step3Title", "Creating a New Set");
        public readonly string Step3Desc = Loc.Localize("HelpRestraints_Step3Desc", "Make a new restraint set here.");
        public readonly string Step3DescExtended = Loc.Localize("HelpRestraints_Step3DescExtended", "Click next and we'll make a new \"Tutorial Restraint\" for you.");

        public readonly string Step4Title = Loc.Localize("HelpRestraints_Step4Title", "The Restraints List");
        public readonly string Step4Desc = Loc.Localize("HelpRestraints_Step4Desc", "This is the list of all restraint sets you have created.");
        public readonly string Step4DescExtended = Loc.Localize("HelpRestraints_Step4DescExtended", "You can click the star next to an item to mark it as a favorite, or delete a set by holding shift while clicking the trash icon.");

        public readonly string Step5Title = Loc.Localize("HelpRestraints_Step5Title", "The Selected Restraint");
        public readonly string Step5Desc = Loc.Localize("HelpRestraints_Step5Desc", "You can see the details of any selected restraint.");
        public readonly string Step5DescExtended = Loc.Localize("HelpRestraints_Step5DescExtended", "The different properties of your restraint will show up here, as well as any attached hardcore traits and moodles. You can mouse over lit up icons to see each property.");

        public readonly string Step6Title = Loc.Localize("HelpRestraints_Step6Title", "Editing a Restraint");
        public readonly string Step6Desc = Loc.Localize("HelpRestraints_Step6Desc", "Edit a restraint set by double-clicking it's name here");
        public readonly string Step6DescExtended = Loc.Localize("HelpRestraints_Step6DescExtended", "You can't edit something you're wearing, by the way.");

        public readonly string Step7Title = Loc.Localize("HelpRestraints_Step7Title", "Renaming a Restraint");
        public readonly string Step7Desc = Loc.Localize("HelpRestraints_Step7Desc", "If you change your mind, you can always rename a restraint.");
        public readonly string Step7DescExtended = Loc.Localize("HelpRestraints_Step7DescExtended", "You can also rename a restraint set by right-clicking it in the main list.");

        public readonly string Step8Title = Loc.Localize("HelpRestraints_Step8Title", "Meta Properties");
        public readonly string Step8Desc = Loc.Localize("HelpRestraints_Step8Desc", "Restraint sets can also edit meta properties about you.");
        public readonly string Step8DescExtended = Loc.Localize("HelpRestraints_Step8DescExtended", "A property set to forced show will always override one set to forced hide. Redrawing your character is necessary if animations are being changed.");

        public readonly string Step9Title = Loc.Localize("HelpRestraints_Step9Title", "Hardcore Traits");
        public readonly string Step9Desc = Loc.Localize("HelpRestraints_Step9Desc", "Each of the hardcore traits can be toggled on or off.\n[NOTE] With the Trait Allowances currently disabled, these will always be applied with any Restraints. Be careful!");
        public readonly string Step9DescExtended = Loc.Localize("HelpRestraints_Step9DescExtended", "These will always be applied if you put on a restraint yourself. " +
            "Be careful which ones you set, as they each disable various aspects of gameplay!");

        public readonly string Step10Title = Loc.Localize("HelpRestraints_Step10Title", "Arousal");
        public readonly string Step10Desc = Loc.Localize("HelpRestraints_Step10Desc", "You can decide how arousing being in this set is.");
        public readonly string Step10DescExtended = Loc.Localize("HelpRestraints_Step10DescExtended", "Arousal builds up slowly over time, and can do things like change your chat messages, blur your screen, increase global cooldowns, and more.");

        public readonly string Step11Title = Loc.Localize("HelpRestraints_Step11Title", "Setting the Equipment");
        public readonly string Step11Desc = Loc.Localize("HelpRestraints_Step11Desc", "This is the base of what will be applied with this restraint.");
        public readonly string Step11DescExtended = Loc.Localize("HelpRestraints_Step11DescExtended", " ");

        public readonly string Step12Title = Loc.Localize("HelpRestraints_Step12Title", "Importing");
        public readonly string Step12Desc = Loc.Localize("HelpRestraints_Step12Desc", "You can import your current gear and accessories seperately.");
        public readonly string Step12DescExtended = Loc.Localize("HelpRestraints_Step12DescExtended", " ");

        public readonly string Step13Title = Loc.Localize("HelpRestraints_Step13Title", "Slot Types");
        public readonly string Step13Desc = Loc.Localize("HelpRestraints_Step13Desc", "Slots can be either a gear item, or a restriction. Click to swap between the two");
        public readonly string Step13DescExtended = Loc.Localize("HelpRestraints_Step13DescExtended", " ");
        
        public readonly string Step14Title = Loc.Localize("HelpRestraints_Step14Title", "Overlay");
        public readonly string Step14Desc = Loc.Localize("HelpRestraints_Step14Desc", "A row that's set to overlay (the eye with a slash through) will not apply if it is set to \"Nothing\".");
        public readonly string Step14DescExtended = Loc.Localize("HelpRestraints_Step14DescExtended", "A row with an open eye will always apply.");

        public readonly string Step15Title = Loc.Localize("HelpRestraints_Step15Title", "Layers");
        public readonly string Step15Desc = Loc.Localize("HelpRestraints_Step15Desc", "These are additional things you can apply on top of your base restraint.");
        public readonly string Step15DescExtended = Loc.Localize("HelpRestraints_Step15DescExtended", "There can be up to 5 layers on any set.");

        public readonly string Step16Title = Loc.Localize("HelpRestraints_Step16Title", "Adding a Layer");
        public readonly string Step16Desc = Loc.Localize("HelpRestraints_Step16Desc", "You can add a layer by clicking this button.");
        public readonly string Step16DescExtended = Loc.Localize("HelpRestraints_Step16DescExtended", " ");

        public readonly string Step17Title = Loc.Localize("HelpRestraints_Step17Title", "Layer Types");
        public readonly string Step17Desc = Loc.Localize("HelpRestraints_Step17Desc", "A layer can either be a restriction, or a mod preset.");
        public readonly string Step17DescExtended = Loc.Localize("HelpRestraints_Step17DescExtended", " ");

        public readonly string Step18Title = Loc.Localize("HelpRestraints_Step18Title", "Mod Presets and Moodles");
        public readonly string Step18Desc = Loc.Localize("HelpRestraints_Step18Desc", "Set up which mods to load, and moodles to apply for the base set.");
        public readonly string Step18DescExtended = Loc.Localize("HelpRestraints_Step18DescExtended", " ");

        public readonly string Step19Title = Loc.Localize("HelpRestraints_Step19Title", "Adding a Mod Preset");
        public readonly string Step19Desc = Loc.Localize("HelpRestraints_Step19Desc", "Use the dropdowns to pick which mod and preset to load.");
        public readonly string Step19DescExtended = Loc.Localize("HelpRestraints_Step19DescExtended", " ");

        public readonly string Step20Title = Loc.Localize("HelpRestraints_Step20Title", "Adding a Moodle");
        public readonly string Step20Desc = Loc.Localize("HelpRestraints_Step20Desc", "You can add a moodle by clicking this button.");
        public readonly string Step20DescExtended = Loc.Localize("HelpRestraints_Step20DescExtended", " ");

        public readonly string Step21Title = Loc.Localize("HelpRestraints_Step21Title", "Moodle Presets");
        public readonly string Step21Desc = Loc.Localize("HelpRestraints_Step21Desc", "Switch between individual moodles or moodle presets with this button.");
        public readonly string Step21DescExtended = Loc.Localize("HelpRestraints_Step21DescExtended", "Hold shift when clicking to switch.");

        public readonly string Step22Title = Loc.Localize("HelpRestraints_Step22Title", "Moodle Preview");
        public readonly string Step22Desc = Loc.Localize("HelpRestraints_Step22Desc", "Hover over any of the moodles here to see what they will look like.");
        public readonly string Step22DescExtended = Loc.Localize("HelpRestraints_Step22DescExtended", " ");
        
        public readonly string Step23Title = Loc.Localize("HelpRestraints_Step23Title", "Cancelling Changes");
        public readonly string Step23Desc = Loc.Localize("HelpRestraints_Step23Desc", "Made a mistake or just don't want to save a change, use the back button.");
        public readonly string Step23DescExtended = Loc.Localize("HelpRestraints_Step23DescExtended", " ");

        public readonly string Step24Title = Loc.Localize("HelpRestraints_Step24Title", "Saving Changes");
        public readonly string Step24Desc = Loc.Localize("HelpRestraints_Step24Desc", "If you are happy with your design, you can save it by clicking this button.");
        public readonly string Step24DescExtended = Loc.Localize("HelpRestraints_Step24DescExtended", " ");

        public readonly string Step25Title = Loc.Localize("HelpRestraints_Step25Title", "Setting a Thumbnail");
        public readonly string Step25Desc = Loc.Localize("HelpRestraints_Step25Desc", "Double-click to open the thumbnail image selector.");
        public readonly string Step25DescExtended = Loc.Localize("HelpRestraints_Step25DescExtended", " ");

        public readonly string Step26Title = Loc.Localize("HelpRestraints_Step26Title", "Refresh the Folder");
        public readonly string Step26Desc = Loc.Localize("HelpRestraints_Step26Desc", "If you added an image to the folder, you can refresh to get it to show up here.");
        public readonly string Step26DescExtended = Loc.Localize("HelpRestraints_Step26DescExtended", " ");

        public readonly string Step27Title = Loc.Localize("HelpRestraints_Step27Title", "Changing the Scale");
        public readonly string Step27Desc = Loc.Localize("HelpRestraints_Step27Desc", "You can always get a closer look at your images if you need to.");
        public readonly string Step27DescExtended = Loc.Localize("HelpRestraints_Step27DescExtended", " ");

        public readonly string Step28Title = Loc.Localize("HelpRestraints_Step28Title", "Import a File");
        public readonly string Step28Desc = Loc.Localize("HelpRestraints_Step28Desc", "If you already have an image you like, you can import from a file anywhere on your computer.");
        public readonly string Step28DescExtended = Loc.Localize("HelpRestraints_Step28DescExtended", " ");

        public readonly string Step29Title = Loc.Localize("HelpRestraints_Step29Title", "Import From the Clipboard");
        public readonly string Step29Desc = Loc.Localize("HelpRestraints_Step29Desc", "You can also grab an image from your clipboard.");
        public readonly string Step29DescExtended = Loc.Localize("HelpRestraints_Step29DescExtended", "Make a cute snip of yourself in a restraint and use this to import it from your screen snipping tool of choice.");

        public readonly string Step30Title = Loc.Localize("HelpRestraints_Step30Title", "The Active Restraint");
        public readonly string Step30Desc = Loc.Localize("HelpRestraints_Step30Desc", "You can see the active restraint right here.");
        public readonly string Step30DescExtended = Loc.Localize("HelpRestraints_Step30DescExtended", " ");

        public readonly string Step31Title = Loc.Localize("HelpRestraints_Step31Title", "Applying a Restraint");
        public readonly string Step31Desc = Loc.Localize("HelpRestraints_Step31Desc", "Select a restraint from the dropdown to apply it to yourself.");
        public readonly string Step31DescExtended = Loc.Localize("HelpRestraints_Step31DescExtended", "Favorite restraints will show up at the top." +
            "Note: Any hardcore traits enabled on a selected restraint always apply when you put it on yourself.");

        public readonly string Step32Title = Loc.Localize("HelpRestraints_Step32Title", "Locking a Restraint");
        public readonly string Step32Desc = Loc.Localize("HelpRestraints_Step32Desc", "There are several locks to choose from.");
        public readonly string Step32DescExtended = Loc.Localize("HelpRestraints_Step32DescExtended", "A lock can only be applied or removed by someone else if you've granted them the correct permissions.");

        public readonly string Step33Title = Loc.Localize("HelpRestraints_Step33Title", "The Layers");
        public readonly string Step33Desc = Loc.Localize("HelpRestraints_Step33Desc", "You can edit the applied layers here.");
        public readonly string Step33DescExtended = Loc.Localize("HelpRestraints_Step33DescExtended", "Make sure to click \"Update Layers\" button when you're done with changes. You cannot edit the active layers when the set has been locked by someone else.");

        public readonly string Step34Title = Loc.Localize("HelpRestraints_Step34Title", "Unlocking a Restraint");
        public readonly string Step34Desc = Loc.Localize("HelpRestraints_Step34Desc", "You can't take a restraint off unless it's unlocked.");
        public readonly string Step34DescExtended = Loc.Localize("HelpRestraints_Step34DescExtended", "(We definitely snuck a Metal Padlock on you while you were reading earlier steps, click next and it will be unlocked for you.)"); //ehehehe i am amused

        public readonly string Step35Title = Loc.Localize("HelpRestraints_Step35Title", "Removing a Restraint");
        public readonly string Step35Desc = Loc.Localize("HelpRestraints_Step35Desc", "Right click to remove a restraint once it's unlocked. Click next and right click this spot to remove this restraint.");
        public readonly string Step35DescExtended = Loc.Localize("HelpRestraints_Step35DescExtended", " ");
    }

    public class HelpRestrictions
    {
        // a lot of this tutorial is going to be similar to the restraints tutorial.
        public readonly string Step1Title = Loc.Localize("HelpRestrictions_Step1Title", "Searching your Restrictions");
        public readonly string Step1Desc = Loc.Localize("HelpRestrictions_Step1Desc", "You can search for any restriction here.");
        public readonly string Step1DescExtended = Loc.Localize("HelpRestrictions_Step1DescExtended", " ");

        public readonly string Step2Title = Loc.Localize("HelpRestrictions_Step2Title", "Creating a Folder");
        public readonly string Step2Desc = Loc.Localize("HelpRestrictions_Step2Desc", "You can use folders to organize your restrictions. Move items around with drag and drop.");
        public readonly string Step2DescExtended = Loc.Localize("HelpRestrictions_Step2DescExtended", "Your pairs don't see these folders, they are just for you.");

        public readonly string Step3Title = Loc.Localize("HelpRestrictions_Step3Title", "Creating a Restriction");
        public readonly string Step3Desc = Loc.Localize("HelpRestrictions_Step3Desc", "The + button creates a new Restriction");
        public readonly string Step3DescExtended = Loc.Localize("HelpRestrictions_Step3DescExtended", " ");

        public readonly string Step4Title = Loc.Localize("HelpRestrictions_Step4Title", "Restriction Types");
        public readonly string Step4Desc = Loc.Localize("HelpRestrictions_Step4Desc", "There are a few different types of restrictions. Click the i icon to see details about each type");
        public readonly string Step4DescExtended = Loc.Localize("HelpRestrictions_Step4DescExtended", "A normal restriction is for simple items with no special properties.\n" +
            "Blindfold provides the ability to darken your screen with an overlay image.\n" +
            "Hypnotic items let you set up custom spiral image overlays with optional text and effects.");

        public readonly string Step5Title = Loc.Localize("HelpRestrictions_Step5Title", "The Restrictions List");
        public readonly string Step5Desc = Loc.Localize("HelpRestrictions_Step5Desc", "Here's where all your restrictions live. Click on one to select it.");
        public readonly string Step5DescExtended = Loc.Localize("HelpRestrictions_Step5DescExtended", "You can click the star next to an item to mark it as a favorite, or delete an item by holding shift while clicking the trash icon.");

        public readonly string Step6Title = Loc.Localize("HelpRestrictions_Step6Title", "The Selected Restriction");
        public readonly string Step6Desc = Loc.Localize("HelpRestrictions_Step6Desc", "The currently selected restriction is displayed here.");
        public readonly string Step6DescExtended = Loc.Localize("HelpRestrictions_Step6DescExtended", " ");

        public readonly string Step7Title = Loc.Localize("HelpRestrictions_Step7Title", "The Restriction Editor");
        public readonly string Step7Desc = Loc.Localize("HelpRestrictions_Step7Desc", "You can edit the selected restriction by double-clicking the name.");
        public readonly string Step7DescExtended = Loc.Localize("HelpRestrictions_Step7DescExtended", "You cannot edit a restriction if it's active.");

        public readonly string Step8Title = Loc.Localize("HelpRestrictions_Step8Title", "Renaming a Restriction");
        public readonly string Step8Desc = Loc.Localize("HelpRestrictions_Step8Desc", "You can always rename a restriction.");
        public readonly string Step8DescExtended = Loc.Localize("HelpRestrictions_Step8DescExtended", "You can also rename a restriction by right-clicking it in the main list.");

        public readonly string Step9Title = Loc.Localize("HelpRestrictions_Step9Title", "Meta Properties");
        public readonly string Step9Desc = Loc.Localize("HelpRestrictions_Step9Desc", "You can have a restriction force certain attrbutes. A green check means always forced on, a red X forces it off, whereas a grey dot means it will not be changed.");
        public readonly string Step9DescExtended = Loc.Localize("HelpRestrictions_Step9DescExtended", "If your restriction mod also has animations, make sure redraw is checked.");

        public readonly string Step10Title = Loc.Localize("HelpRestrictions_Step10Title", "Setting the Associated Glamour");
        public readonly string Step10Desc = Loc.Localize("HelpRestrictions_Step10Desc", "The item that gets put on with this restriction.");
        public readonly string Step10DescExtended = Loc.Localize("HelpRestrictions_Step10DescExtended", "Advanced dyes aren't supported. There are guides for how to make items dyeable all over.");

        public readonly string Step11Title = Loc.Localize("HelpRestrictions_Step11Title", "Hypnotic Restriction");
        public readonly string Step11Desc = Loc.Localize("HelpRestrictions_Step11Desc", "These special restriction types allow you to create overlays with custom images, text, and effects.");
        public readonly string Step11DescExtended = Loc.Localize("HelpRestrictions_Step11DescExtended", "You will be able to customize the image overlaid on screen, any text that's displayed, and a bunch of other effects in later steps.");

        public readonly string Step12Title = Loc.Localize("HelpRestrictions_Step12Title", "First Person");
        public readonly string Step12Desc = Loc.Localize("HelpRestrictions_Step12Desc", "You can set up a blindfold or hypnotic restriction to lock your camera to first person.");
        public readonly string Step12DescExtended = Loc.Localize("HelpRestrictions_Step12DescExtended", "It must have an associated image for this to function, even if it's just a fully transparent one.");

        public readonly string Step13Title = Loc.Localize("HelpRestrictions_Step13Title", "Selecting an Image");
        public readonly string Step13Desc = Loc.Localize("HelpRestrictions_Step13Desc", "Clicking the top bar will open the thumbnail viewer to pick an image for your hypnotic effect.");
        public readonly string Step13DescExtended = Loc.Localize("HelpRestrictions_Step13DescExtended", "There's a spiral included by default, but you can always add your own images.");

        public readonly string Step14Title = Loc.Localize("HelpRestrictions_Step14Title", "Refresh the Folder");
        public readonly string Step14Desc = Loc.Localize("HelpRestrictions_Step14Desc", "If you added an image to the folder, you can refresh to get it to show up here.");
        public readonly string Step14DescExtended = Loc.Localize("HelpRestrictions_Step14DescExtended", " ");

        public readonly string Step15Title = Loc.Localize("HelpRestrictions_Step15Title", "Changing the Scale");
        public readonly string Step15Desc = Loc.Localize("HelpRestrictions_Step15Desc", "You can always get a closer look at your images if you need to.");
        public readonly string Step15DescExtended = Loc.Localize("HelpRestrictions_Step15DescExtended", " ");

        public readonly string Step16Title = Loc.Localize("HelpRestrictions_Step16Title", "Import a File");
        public readonly string Step16Desc = Loc.Localize("HelpRestrictions_Step16Desc", "If you already have an image you like, you can import from a file anywhere on your computer.");
        public readonly string Step16DescExtended = Loc.Localize("HelpRestrictions_Step16DescExtended", " ");

        public readonly string Step17Title = Loc.Localize("HelpRestrictions_Step17Title", "Import From the Clipboard");
        public readonly string Step17Desc = Loc.Localize("HelpRestrictions_Step17Desc", "You can also grab an image from your clipboard.");
        public readonly string Step17DescExtended = Loc.Localize("HelpRestrictions_Step17DescExtended", " ");

        public readonly string Step18Title = Loc.Localize("HelpRestrictions_Step18Title", "The Effect Editor");
        public readonly string Step18Desc = Loc.Localize("HelpRestrictions_Step18Desc", "This button opens the effect editor for hypnotic restrictios.");
        public readonly string Step18DescExtended = Loc.Localize("HelpRestrictions_Step18DescExtended", " ");

        public readonly string Step19Title = Loc.Localize("HelpRestrictions_Step19Title", "Effect Options");
        public readonly string Step19Desc = Loc.Localize("HelpRestrictions_Step19Desc", "You can set several different options for your hypnotic effect.");
        public readonly string Step19DescExtended = Loc.Localize("HelpRestrictions_Step19DescExtended", "Each of the options has a tooltip explaining what it does. Hover over for more info!");

        public readonly string Step20Title = Loc.Localize("HelpRestrictions_Step20Title", "The Word List");
        public readonly string Step20Desc = Loc.Localize("HelpRestrictions_Step20Desc", "This is a list of all the words that can be shown when the effect is active.");
        public readonly string Step20DescExtended = Loc.Localize("HelpRestrictions_Step20DescExtended", "Click the + to add a new word. You can add multiple words, " +
            "reorder them by holding shift while left- or right-clicking. Delete any word by holding control and left-clicking." +
            "You can see these key binds again at any time by hovering over the little ? icon in the corner.");

        public readonly string Step21Title = Loc.Localize("HelpRestrictions_Step21Title", "Coloring your Effects");
        public readonly string Step21Desc = Loc.Localize("HelpRestrictions_Step21Desc", "The three color pickers can be used to edit the text, outline (stroke), and main spiral colors separately.");
        public readonly string Step21DescExtended = Loc.Localize("HelpRestrictions_Step21DescExtended", " ");

        public readonly string Step22Title = Loc.Localize("HelpRestrictions_Step22Title", "Hardcore Traits");
        public readonly string Step22Desc = Loc.Localize("HelpRestrictions_Step22Desc", "You can set which Hardcore Traits are applied when you use this restriction.");
        public readonly string Step22DescExtended = Loc.Localize("HelpRestrictions_Step22DescExtended", "These traits will always apply to restrictions you've put on yourself, so be careful!");

        public readonly string Step23Title = Loc.Localize("HelpRestrictions_Step23Title", "Arousal");
        public readonly string Step23Desc = Loc.Localize("HelpRestrictions_Step23Desc", "You can decide how arousing being in this restriction is.");
        public readonly string Step23DescExtended = Loc.Localize("HelpRestrictions_Step23DescExtended", "Arousal builds up slowly over time, and can do things like change your chat messages, blur your screen, increase global cooldowns, and more.");

        public readonly string Step24Title = Loc.Localize("HelpRestrictions_Step24Title", "Moodles");
        public readonly string Step24Desc = Loc.Localize("HelpRestrictions_Step24Desc", "Manage any moodles for this restriction here.");
        public readonly string Step24DescExtended = Loc.Localize("HelpRestrictions_Step24DescExtended", "Moodles are custom buffs and debuffs!");

        public readonly string Step25Title = Loc.Localize("HelpRestrictions_Step25Title", "Using Moodle Presets");
        public readonly string Step25Desc = Loc.Localize("HelpRestrictions_Step25Desc", "Switch between individual moodles or moodle presets with this button.");
        public readonly string Step25DescExtended = Loc.Localize("HelpRestrictions_Step25DescExtended", "Hold shift and click to switch.");

        public readonly string Step26Title = Loc.Localize("HelpRestrictions_Step26Title", "Moodle Preview");
        public readonly string Step26Desc = Loc.Localize("HelpRestrictions_Step26Desc", "Hover over any of the moodles here to see what they will look like.");
        public readonly string Step26DescExtended = Loc.Localize("HelpRestrictions_Step26DescExtended", " ");

        public readonly string Step27Title = Loc.Localize("HelpRestrictions_Step27Title", "Attached Mods");
        public readonly string Step27Desc = Loc.Localize("HelpRestrictions_Step27Desc", "Which mod this restriction will load when you apply it");
        public readonly string Step27DescExtended = Loc.Localize("HelpRestrictions_Step27DescExtended", " ");

        public readonly string Step28Title = Loc.Localize("HelpRestrictions_Step28Title", "Adding a Mod");
        public readonly string Step28Desc = Loc.Localize("HelpRestrictions_Step28Desc", "You can add a mod by selecting it from the dropdown.");
        public readonly string Step28DescExtended = Loc.Localize("HelpRestrictions_Step28DescExtended", " ");

        public readonly string Step29Title = Loc.Localize("HelpRestrictions_Step29Title", "Mod Presets");
        public readonly string Step29Desc = Loc.Localize("HelpRestrictions_Step29Desc", "Select the mod preset to load from this dropdown.");
        public readonly string Step29DescExtended = Loc.Localize("HelpRestrictions_Step29DescExtended", "You can create and edit these in the Mod Presets window.");

        public readonly string Step30Title = Loc.Localize("HelpRestrictions_Step30Title", "Preset Preview");
        public readonly string Step30Desc = Loc.Localize("HelpRestrictions_Step30Desc", "Hover over the preset name to see a small popup preview of the settings that will be loaded.");
        public readonly string Step30DescExtended = Loc.Localize("HelpRestrictions_Step30DescExtended", " ");

        public readonly string Step31Title = Loc.Localize("HelpRestrictions_Step31Title", "Canceling changes");
        public readonly string Step31Desc = Loc.Localize("HelpRestrictions_Step31Desc", "If you made a mistake or just don't want to save a change, use the back button.");
        public readonly string Step31DescExtended = Loc.Localize("HelpRestrictions_Step31DescExtended", " ");

        public readonly string Step32Title = Loc.Localize("HelpRestrictions_Step32Title", "Saving Changes");
        public readonly string Step32Desc = Loc.Localize("HelpRestrictions_Step32Desc", "If you are happy with your design, you can save it by clicking this button.");
        public readonly string Step32DescExtended = Loc.Localize("HelpRestrictions_Step32DescExtended", " ");

        public readonly string Step33Title = Loc.Localize("HelpRestrictions_Step33Title", "Setting a Thumbnail");
        public readonly string Step33Desc = Loc.Localize("HelpRestrictions_Step33Desc", "Double-click to open the thumbnail image selector. The Restraints page tutorial has more info on how this works.");
        public readonly string Step33DescExtended = Loc.Localize("HelpRestrictions_Step33DescExtended", " ");

        public readonly string Step34Title = Loc.Localize("HelpRestrictions_Step34Title", "Your Active Restrictions");
        public readonly string Step34Desc = Loc.Localize("HelpRestrictions_Step34Desc", "You can see the active restrictions right here.");
        public readonly string Step34DescExtended = Loc.Localize("HelpRestrictions_Step34DescExtended", "You can have up to 5 restrictions active at any time." +
            "These are applied in order from top to bottom, and are shown on top of restraints.");

        public readonly string Step35Title = Loc.Localize("HelpRestrictions_Step35Title", "Applying a Restriction");
        public readonly string Step35Desc = Loc.Localize("HelpRestrictions_Step35Desc", "Select a restriction from the dropdown to apply it to yourself.");
        public readonly string Step35DescExtended = Loc.Localize("HelpRestrictions_Step35DescExtended", "Favorite restrictions will show up at the top. " +
            "Note: Any hardcore traits enabled on a selected restriction always apply when you put it on yourself.");

        public readonly string Step36Title = Loc.Localize("HelpRestrictions_Step36Title", "No Free Slots!");
        public readonly string Step36Desc = Loc.Localize("HelpRestrictions_Step36Desc", "You don't have any open slots available. A couple of steps are going to be skipped");
        public readonly string Step36DescExtended = Loc.Localize("HelpRestrictions_Step36DescExtended", "If you want to see how applying, locking and unlocking restrictions works, restart this tutorial with an empty slot!");

        public readonly string Step37Title = Loc.Localize("HelpRestrictions_Step37Title", "Locking a Restriction");
        public readonly string Step37Desc = Loc.Localize("HelpRestrictions_Step37Desc", "There are several locks to choose from.");
        public readonly string Step37DescExtended = Loc.Localize("HelpRestrictions_Step37DescExtended", "A lock can only be applied or removed by someone else if you've granted " +
            "them the correct permissions.");

        public readonly string Step38Title = Loc.Localize("HelpRestrictions_Step38Title", "Unlocking a Restriction");
        public readonly string Step38Desc = Loc.Localize("HelpRestrictions_Step38Desc", "You can't take a restriction off unless it's unlocked.");
        public readonly string Step38DescExtended = Loc.Localize("HelpRestrictions_Step38DescExtended", "(We definitely snuck a Metal Padlock on you while you were reading " +
            "earlier steps, click next and it will be unlocked for you.)");

        public readonly string Step39Title = Loc.Localize("HelpRestrictions_Step39Title", "Removing a Restriction");
        public readonly string Step39Desc = Loc.Localize("HelpRestrictions_Step39Desc", "Right click the thumbnail to remove a restriction once it's unlocked.\nJust in case we " +
            "didn't put anything on you, we aren't going to remove anything for you. Once you click next, this tutorial is finished, and you can remove items normally.");
        public readonly string Step39DescExtended = Loc.Localize("HelpRestrictions_Step39DescExtended", " ");

    }

    public class HelpGags
    {
        public readonly string Step1Title = Loc.Localize("HelpGags_Step1Title", "Searching Your Gags");
        public readonly string Step1Desc = Loc.Localize("HelpGags_Step1Desc", "You can search for specific gags here.");
        public readonly string Step1DescExtended = Loc.Localize("HelpGags_Step1DescExtended", " ");

        public readonly string Step2Title = Loc.Localize("HelpGags_Step2Title", "Creating a Folder");
        public readonly string Step2Desc = Loc.Localize("HelpGags_Step2Desc", "Clicking this will let you create a new folder.");
        public readonly string Step2DescExtended = Loc.Localize("HelpGags_Step2DescExtended", "You can drag and drop gags into folders to organize them.");

        public readonly string Step3Title = Loc.Localize("HelpGags_Step3Title", "The Gag List");
        public readonly string Step3Desc = Loc.Localize("HelpGags_Step3Desc", "This is the list of all of the gags!");
        public readonly string Step3DescExtended = Loc.Localize("HelpGags_Step3DescExtended", "You can click the star next to an item to mark it as a favorite, or delete a set with by holding shift while clicking the trash icon.");

        public readonly string Step4Title = Loc.Localize("HelpGags_Step4Title", "The Selected Gag");
        public readonly string Step4Desc = Loc.Localize("HelpGags_Step4Desc", "This is where you can view info about your currently selected gag.");
        public readonly string Step4DescExtended = Loc.Localize("HelpGags_Step4DescExtended", " ");

        public readonly string Step5Title = Loc.Localize("HelpGags_Step5Title", "Visual State");
        public readonly string Step5Desc = Loc.Localize("HelpGags_Step5Desc", "A gag set to have visuals enabled will apply the configured customizations, you can click to toggle this.");
        public readonly string Step5DescExtended = Loc.Localize("HelpGags_Step5DescExtended", "The chat garbler and other traits of the gag will still work if this is off.");

        public readonly string Step6Title = Loc.Localize("HelpGags_Step6Title", "The Gag Editor");
        public readonly string Step6Desc = Loc.Localize("HelpGags_Step6Desc", "You can edit the selected gag by double-clicking its name here.");
        public readonly string Step6DescExtended = Loc.Localize("HelpGags_Step6DescExtended", " ");

        public readonly string Step7Title = Loc.Localize("HelpGags_Step7Title", "Metadata");
        public readonly string Step7Desc = Loc.Localize("HelpGags_Step7Desc", "You can set each gag to show or hide your helmet and visor, or force a redraw.");
        public readonly string Step7DescExtended = Loc.Localize("HelpGags_Step7DescExtended", "The redraw option is useful if you have a mod that changes animations.");

        public readonly string Step8Title = Loc.Localize("HelpGags_Step8Title", "Setting the Associated Glamour");
        public readonly string Step8Desc = Loc.Localize("HelpGags_Step8Desc", "You can set what gets applied to you for this gag.");
        public readonly string Step8DescExtended = Loc.Localize("HelpGags_Step8DescExtended", " ");

        public readonly string Step9Title = Loc.Localize("HelpGags_Step9Title", "Hardcore Traits");
        public readonly string Step9Desc = Loc.Localize("HelpGags_Step9Desc", "You can set which Hardcore Traits are applied when you use this gag.");
        public readonly string Step9DescExtended = Loc.Localize("HelpGags_Step9DescExtended", "These traits will always apply to gags you've put on yourself, so be careful!");

        public readonly string Step10Title = Loc.Localize("HelpGags_Step10Title", "Arousal");
        public readonly string Step10Desc = Loc.Localize("HelpGags_Step10Desc", "You can decide how arousing being in this gag is.");
        public readonly string Step10DescExtended = Loc.Localize("HelpGags_Step10DescExtended", "Arousal builds up slowly over time, and can do things like change your chat messages, blur your screen, increase global cooldowns, and more.");

        public readonly string Step11Title = Loc.Localize("HelpGags_Step11Title", "Customize+ Preset");
        public readonly string Step11Desc = Loc.Localize("HelpGags_Step11Desc", "You can set a Customize+ Preset to apply with this gag.");
        public readonly string Step11DescExtended = Loc.Localize("HelpGags_Step11DescExtended", " ");

        public readonly string Step12Title = Loc.Localize("HelpGags_Step12Title", "Moodles");
        public readonly string Step12Desc = Loc.Localize("HelpGags_Step12Desc", "Manage any moodles for this gag here.");
        public readonly string Step12DescExtended = Loc.Localize("HelpGags_Step12DescExtended", "Moodles are custom buffs and debuffs!");

        public readonly string Step13Title = Loc.Localize("HelpGags_Step13Title", "Using Moodles Presets");
        public readonly string Step13Desc = Loc.Localize("HelpGags_Step13Desc", "Switch between individual moodles or moodle presets with this button.");
        public readonly string Step13DescExtended = Loc.Localize("HelpGags_Step13DescExtended", "Hold shift and click to switch.");

        public readonly string Step14Title = Loc.Localize("HelpGags_Step14Title", "Moodle Preview");
        public readonly string Step14Desc = Loc.Localize("HelpGags_Step14Desc", "Hover over any of the moodles here to see what they will look like.");
        public readonly string Step14DescExtended = Loc.Localize("HelpGags_Step14DescExtended", " ");

        public readonly string Step15Title = Loc.Localize("HelpGags_Step15Title", "Attached Mod");
        public readonly string Step15Desc = Loc.Localize("HelpGags_Step15Desc", "You can set up a mod to load alongside your gag.");
        public readonly string Step15DescExtended = Loc.Localize("HelpGags_Step15DescExtended", " ");

        public readonly string Step16Title = Loc.Localize("HelpGags_Step16Title", "Selecting a Mod");
        public readonly string Step16Desc = Loc.Localize("HelpGags_Step16Desc", "Pick which mod from this dropdown.");
        public readonly string Step16DescExtended = Loc.Localize("HelpGags_Step16DescExtended", " ");

        public readonly string Step17Title = Loc.Localize("HelpGags_Step17Title", "Mod Presets");
        public readonly string Step17Desc = Loc.Localize("HelpGags_Step17Desc", "Select the mod preset to load from this dropdown.");
        public readonly string Step17DescExtended = Loc.Localize("HelpGags_Step17DescExtended", "\"Current\" will save the configuration you have right now. " +
            "You can create new presets and edit them in the Mod Presets window.");

        public readonly string Step18Title = Loc.Localize("HelpGags_Step18Title", "Preset Preview");
        public readonly string Step18Desc = Loc.Localize("HelpGags_Step18Desc", "Underneath the selection, you can preview the settings associated with the selected mod.");
        public readonly string Step18DescExtended = Loc.Localize("HelpGags_Step18DescExtended", " ");

        public readonly string Step19Title = Loc.Localize("HelpGags_Step19Title", "Canceling changes");
        public readonly string Step19Desc = Loc.Localize("HelpGags_Step19Desc", "If you made a mistake or just don't want to save a change, use the back button.");
        public readonly string Step19DescExtended = Loc.Localize("HelpGags_Step19DescExtended", " ");

        public readonly string Step20Title = Loc.Localize("HelpGags_Step20Title", "Saving Changes");
        public readonly string Step20Desc = Loc.Localize("HelpGags_Step20Desc", "If you are happy with your design, you can save it by clicking this button.");
        public readonly string Step20DescExtended = Loc.Localize("HelpGags_Step20DescExtended", " ");

        public readonly string Step21Title = Loc.Localize("HelpGags_Step21Title", "Active Gags List");
        public readonly string Step21Desc = Loc.Localize("HelpGags_Step21Desc", "This is the list of gags active on you right now.");
        public readonly string Step21DescExtended = Loc.Localize("HelpGags_Step21DescExtended", "You can have up to 3 gags active at any time." +
            "Any with conflicting glamour slots will only show the lowest.");

        public readonly string Step22Title = Loc.Localize("HelpGags_Step22Title", "Selecting a Gag");
        public readonly string Step22Desc = Loc.Localize("HelpGags_Step22Desc", "You can select a gag from the dropdown to apply it to yourself.");
        public readonly string Step22DescExtended = Loc.Localize("HelpGags_Step22DescExtended", "Favorite gags will show up at the top. Note: Any hardcore traits enabled on a selected gag always apply when you put it on yourself.");

        public readonly string Step23Title = Loc.Localize("HelpGags_Step23Title", "Locking a Gag");
        public readonly string Step23Desc = Loc.Localize("HelpGags_Step23Desc", "There are several locks to choose from.");
        public readonly string Step23DescExtended = Loc.Localize("HelpGags_Step23DescExtended", "A lock can only be applied or removed by someone else if you've granted them the correct permissions.");

        public readonly string Step24Title = Loc.Localize("HelpGags_Step24Title", "Unlocking a Gag");
        public readonly string Step24Desc = Loc.Localize("HelpGags_Step24Desc", "You can't take a gag off unless it's unlocked.");
        public readonly string Step24DescExtended = Loc.Localize("HelpGags_Step24DescExtended", "We snuck a Metal padlock onto this gag so you can see what it looks like. Click next to unlock the gag.");

        public readonly string Step25Title = Loc.Localize("HelpGags_Step25Title", "Removing a Gag");
        public readonly string Step25Desc = Loc.Localize("HelpGags_Step25Desc", "Right click the thumbnail to remove a gag once it's unlocked.");
        public readonly string Step25DescExtended = Loc.Localize("HelpGags_Step25DescExtended", " ");
    }

    public class HelpCursedLoot
    {
        //public readonly string Step1Title = Loc.Localize("HelpCursedLoot_Step1Title", "Creating Cursed Items");
        //public readonly string Step1Desc = Loc.Localize("HelpCursedLoot_Step1Desc", "Drops down the expanded cursed item creator window.");

        //public readonly string Step2Title = Loc.Localize("HelpCursedLoot_Step2Title", "The Cursed Item Name");
        //public readonly string Step2Desc = Loc.Localize("HelpCursedLoot_Step2Desc", "Defines a name for the Cursed Item. This is purely for organizations and display sake.");

        //public readonly string Step3Title = Loc.Localize("HelpCursedLoot_Step3Title", "Defining the type");
        //public readonly string Step3Desc = Loc.Localize("HelpCursedLoot_Step3Desc", "Allows you to define the Cursed Item as a Gag, or a Equipment Piece.");
        //public readonly string Step3DescExtended = Loc.Localize("HelpCursedLoot_Step3DescExtended", "Gags do not have precedence, and cannot be applied once all 3 gag layers are full.");

        //public readonly string Step4Title = Loc.Localize("HelpCursedLoot_Step4Title", "Adding the New Cursed Item");
        //public readonly string Step4Desc = Loc.Localize("HelpCursedLoot_Step4Desc", "Once you are finished creating the Cursed Item, pressing this button will add it to your list.");
        //public readonly string Step4DescExtended = Loc.Localize("HelpCursedLoot_Step4DescExtended", "PRECAUTION: Once you add an item to your list, you cannot change it between Gag & Equip Types!");

        //public readonly string Step5Title = Loc.Localize("HelpCursedLoot_Step5Title", "The Cursed Item List");
        //public readonly string Step5Desc = Loc.Localize("HelpCursedLoot_Step5Desc", "The arrangement of Cursed Items you have created.");

        //public readonly string Step6Title = Loc.Localize("HelpCursedLoot_Step6Title", "The Enabled Pool");
        //public readonly string Step6Desc = Loc.Localize("HelpCursedLoot_Step6Desc", "The list of items that will be randomly selected from whenever you find a Mimic Chest");

        //public readonly string Step7Title = Loc.Localize("HelpCursedLoot_Step7Title", "Adding Items to the Pool");
        //public readonly string Step7Desc = Loc.Localize("HelpCursedLoot_Step7Desc", "This will move an cursed item into the Enabled Pool.\nGive it a shot!");

        //public readonly string Step8Title = Loc.Localize("HelpCursedLoot_Step8Title", "Removing Items from the Pool");
        //public readonly string Step8Desc = Loc.Localize("HelpCursedLoot_Step8Desc", "This will remove the item from the enabled pool.");

        //public readonly string Step9Title = Loc.Localize("HelpCursedLoot_Step9Title", "The Lower Lock Timer Limit");
        //public readonly string Step9Desc = Loc.Localize("HelpCursedLoot_Step9Desc", "Whatever you set in here will be the LOWER LIMIT of how long a Cursed Item will end up locked for.");
        //public readonly string Step9DescExtended = Loc.Localize("HelpCursedLoot_Step9DescExtended", "BE AWARE: MIMIC PADLOCKS CANNOT BE UNLOCKED. YOU MUST WAIT FOR THEM TO EXPIRE. SET TIMER LIMIT ACCORDINGLY.");

        //public readonly string Step10Title = Loc.Localize("HelpCursedLoot_Step10Title", "The Upper Lock Timer Limit");
        //public readonly string Step10Desc = Loc.Localize("HelpCursedLoot_Step10Desc", "Whatever you set in here will be the UPPER LIMIT of how long a Cursed Item will end up locked for.");
        //public readonly string Step10DescExtended = Loc.Localize("HelpCursedLoot_Step10DescExtended", "BE AWARE: MIMIC PADLOCKS CANNOT BE UNLOCKED. YOU MUST WAIT FOR THEM TO EXPIRE. SET TIMER LIMIT ACCORDINGLY.");

        //public readonly string Step11Title = Loc.Localize("HelpCursedLoot_Step11Title", "The CursedItem Discovery Percent");
        //public readonly string Step11Desc = Loc.Localize("HelpCursedLoot_Step11Desc", "Whatever you set here will be the %% Chance that a chest you loot will be Cursed Loot.");
    }
    
    public class HelpPuppeteer
    {
        public readonly string Step1Title = Loc.Localize("HelpPuppeter_Step1Title", "Puppeteer");
        public readonly string Step1Desc = Loc.Localize("HelpPuppeter_Step1Desc", "Puppeteer lets other Kinksters control you by running commands and aliases");
        public readonly string Step1DescExtended = Loc.Localize("HelpPuppeter_Step1DescExtended", "With everything in GagSpeak, you have full control over what you allow others to do. Please play safe!");

        public readonly string Step2Title = Loc.Localize("HelpPuppeter_Step2Title", "Aliases");
        public readonly string Step2Desc = Loc.Localize("HelpPuppeter_Step2Desc", "The Aliases tab is where you can configure special responses to a specific trigger.");
        public readonly string Step2DescExtended = Loc.Localize("HelpPuppeter_Step2DescExtended", "An Alias is like a special command where you can set what activates it and what it does when it is activated. They can do many things, from running commands, to playing patterns on connected toys, to putting on restraints, to applying moodles, and more!");

        public readonly string Step3Title = Loc.Localize("HelpPuppeter_Step3Title", "Alias List");
        public readonly string Step3Desc = Loc.Localize("HelpPuppeter_Step3Desc", "Here's the list of all your configured aliases");
        public readonly string Step3DescExtended = Loc.Localize("HelpPuppeter_Step3DescExtended", "You can right click an alias for quick-access to options, including deleting them.");

        public readonly string Step4Title = Loc.Localize("HelpPuppeter_Step4Title", "The Top Bar");
        public readonly string Step4Desc = Loc.Localize("HelpPuppeter_Step4Desc", "You can search for aliases, or create a new alias or folder here.");
        public readonly string Step4DescExtended = Loc.Localize("HelpPuppeter_Step4DescExtended", "The buttons on the side of the bar can be used to create a new alias or folder.");

        public readonly string Step5Title = Loc.Localize("HelpPuppeter_Step5Title", "Selected Alias");
        public readonly string Step5Desc = Loc.Localize("HelpPuppeter_Step5Desc", "You can see your currently selected alias on the right panel.");
        public readonly string Step5DescExtended = Loc.Localize("HelpPuppeter_Step5DescExtended", " ");

        public readonly string Step6Title = Loc.Localize("HelpPuppeter_Step6Title", "Editing an Alias");
        public readonly string Step6Desc = Loc.Localize("HelpPuppeter_Step6Desc", "Clicking this button will let you edit an alias.");
        public readonly string Step6DescExtended = Loc.Localize("HelpPuppeter_Step6DescExtended", " ");

        public readonly string Step7Title = Loc.Localize("HelpPuppeter_Step7Title", "Alias Name");
        public readonly string Step7Desc = Loc.Localize("HelpPuppeter_Step7Desc", "The name you want to give the alias. This doesn't affect how it works, but helps you keep things organized.");
        public readonly string Step7DescExtended = Loc.Localize("HelpPuppeter_Step7DescExtended", " ");

        public readonly string Step8Title = Loc.Localize("HelpPuppeter_Step8Title", "Alias Actions");
        public readonly string Step8Desc = Loc.Localize("HelpPuppeter_Step8Desc", "Here's where you can set up the various Alias actions.");
        public readonly string Step8DescExtended = Loc.Localize("HelpPuppeter_Step8DescExtended", "You can only use each action type once, but you do not have to use all of them for every alias.");

        public readonly string Step9Title = Loc.Localize("HelpPuppeter_Step9Title", "Alias Permissions");
        public readonly string Step9Desc = Loc.Localize("HelpPuppeter_Step9Desc", "You can configure which kinksters are allowed to activate this alias here.");
        public readonly string Step9DescExtended = Loc.Localize("HelpPuppeter_Step9DescExtended", "Leaving this empty will make the alias global. Global aliases can be used by ANYONE, so long as they are enabled.");

        public readonly string Step10Title = Loc.Localize("HelpPuppeter_Step10Title", "Saving Aliases");
        public readonly string Step10Desc = Loc.Localize("HelpPuppeter_Step10Desc", "Click this button to save your changes and exit the editor");
        public readonly string Step10DescExtended = Loc.Localize("HelpPuppeter_Step10DescExtended", "The back button next to it will exit the editor without saving.");

        public readonly string Step11Title = Loc.Localize("HelpPuppeter_Step11Title", "Puppeteers Tab");
        public readonly string Step11Desc = Loc.Localize("HelpPuppeter_Step11Desc", "This tab is where you can configure other puppeteer permissions for all of your Kinkster pairs.");
        public readonly string Step11DescExtended = Loc.Localize("HelpPuppeter_Step11DescExtended", " ");

        public readonly string Step12Title = Loc.Localize("HelpPuppeter_Step12Title", "Puppeteers Kinkster List");
        public readonly string Step12Desc = Loc.Localize("HelpPuppeter_Step12Desc", "This is a list of all of the Kinksters you're paired with. The folders split them up into who is and isn't allowed to control you.");
        public readonly string Step12DescExtended = Loc.Localize("HelpPuppeter_Step12DescExtended", " ");

        public readonly string Step13Title = Loc.Localize("HelpPuppeter_Step13Title", "Puppeteers Kinkster Settings");
        public readonly string Step13Desc = Loc.Localize("HelpPuppeter_Step13Desc", "The right pane is where you can see and configure settings for a specific Kinkster.");
        public readonly string Step13DescExtended = Loc.Localize("HelpPuppeter_Step13DescExtended", " ");

        public readonly string Step14Title = Loc.Localize("HelpPuppeter_Step14Title", "Puppeteers Name");
        public readonly string Step14Desc = Loc.Localize("HelpPuppeter_Step14Desc", "In order for a kinkster to be able to control you, you need their in-game name.");
        public readonly string Step14DescExtended = Loc.Localize("HelpPuppeter_Step14DescExtended", "The kinkster will have to sync their name with you on the Marionettes tab.");

        public readonly string Step15Title = Loc.Localize("HelpPuppeter_Step15Title", "Puppeteers Triggers");
        public readonly string Step15Desc = Loc.Localize("HelpPuppeter_Step15Desc", "Puppeteer works by listening for specific words or phrases in chat. Each word you set here is the start of the trigger.");
        public readonly string Step15DescExtended = Loc.Localize("HelpPuppeter_Step15DescExtended", "Think of it like how slash commands work, except the \"/\" is the words you add here.");

        public readonly string Step16Title = Loc.Localize("HelpPuppeter_Step16Title", "Puppeteers Allowed Orders");
        public readonly string Step16Desc = Loc.Localize("HelpPuppeter_Step16Desc", "You can control which sets of commands others can make you use.");
        public readonly string Step16DescExtended = Loc.Localize("HelpPuppeter_Step16DescExtended", "Be careful with who you give full access to, as they can run any commands on your client.");

        public readonly string Step17Title = Loc.Localize("HelpPuppeter_Step17Title", "Puppeteers Advanced");
        public readonly string Step17Desc = Loc.Localize("HelpPuppeter_Step17Desc", "Custom Scope Brackets are the characters that Puppeteer uses to mark the start and end of multi-word commands. They are not needed for single-word commands and emotes.");
        public readonly string Step17DescExtended = Loc.Localize("HelpPuppeter_Step17DescExtended",
            "To use a command like \"/gaction \"Jump\",\" or an alias with multiple words like \"please sit down,\" it needs to be surrounded by these symbols for it to be correctly handled.\n" +
            "ex: 'yappy (gaction \"Jump\")' They go around the command, not the trigger word.");

        public readonly string Step18Title = Loc.Localize("HelpPuppeter_Step18Title", "Marionettes");
        public readonly string Step18Desc = Loc.Localize("HelpPuppeter_Step18Desc", "You can use this page to see how you can control other Kinksters.");
        public readonly string Step18DescExtended = Loc.Localize("HelpPuppeter_Step18DescExtended", "Only kinksters who have given you control permission will be available.");

        public readonly string Step19Title = Loc.Localize("HelpPuppeter_Step19Title", "Marionettes Kinksters");
        public readonly string Step19Desc = Loc.Localize("HelpPuppeter_Step19Desc", "Select a Kinster to view here");
        public readonly string Step19DescExtended = Loc.Localize("HelpPuppeter_Step19DescExtended", " ");

        public readonly string Step20Title = Loc.Localize("HelpPuppeter_Step20Title", "Marionettes Aliases");
        public readonly string Step20Desc = Loc.Localize("HelpPuppeter_Step20Desc", "The list of aliases for this Kinkster.");
        public readonly string Step20DescExtended = Loc.Localize("HelpPuppeter_Step20DescExtended", "Enabled and disabled show up in seperate folders.");

        public readonly string Step21Title = Loc.Localize("HelpPuppeter_Step21Title", "Marionettes Permissions");
        public readonly string Step21Desc = Loc.Localize("HelpPuppeter_Step21Desc", "This is where you can view the permission you've been given for a selected Kinkster.");
        public readonly string Step21DescExtended = Loc.Localize("HelpPuppeter_Step21DescExtended", " ");

        public readonly string Step22Title = Loc.Localize("HelpPuppeter_Step22Title", "Marionettes Name Sharing");
        public readonly string Step22Desc = Loc.Localize("HelpPuppeter_Step22Desc", "In order for a Kinkster to respond to your commands, they will need your ingame name. Click the Sync button to securely send your name to them.");
        public readonly string Step22DescExtended = Loc.Localize("HelpPuppeter_Step22DescExtended", " ");

        public readonly string Step23Title = Loc.Localize("HelpPuppeter_Step23Title", "Marionettes Triggers");
        public readonly string Step23Desc = Loc.Localize("HelpPuppeter_Step23Desc", "The list of trigger phrases for this Kinkster.");
        public readonly string Step23DescExtended = Loc.Localize("HelpPuppeter_Step23DescExtended", " ");

        public readonly string Step24Title = Loc.Localize("HelpPuppeter_Step24Title", "Marionettes Selected Alias");
        public readonly string Step24Desc = Loc.Localize("HelpPuppeter_Step24Desc", "Selecting an alias on the left side will show you what what it's set up to do here.");
        public readonly string Step24DescExtended = Loc.Localize("HelpPuppeter_Step24DescExtended", "You can see all of the actions that will happen when you use this alias.");

    }


    public class HelpToys
    {
        //public readonly string Step1Title = Loc.Localize("HelpToybox_Step1Title", "Intiface Connection Status");
        //public readonly string Step1Desc = Loc.Localize("HelpToybox_Step1Desc", "The current connection status to the Intiface Central websocket server.");
        //public readonly string Step1DescExtended = Loc.Localize("HelpToybox_Step1DescExtended", "This requires you to have intiface central open. " +
        //    "If it is not open it will not function. See next step for info.");

        //public readonly string Step2Title = Loc.Localize("HelpToybox_Step2Title", "Open Intiface");
        //public readonly string Step2Desc = Loc.Localize("HelpToybox_Step2Desc", "This button will do one of 3 things:" + Environment.NewLine +
        //    "1. Bring Intiface Central infront of other active windows if already opened or minimized." + Environment.NewLine +
        //    "2. Open the Intiface Central Program if on your computer but not yet open." + Environment.NewLine +
        //    "3. Directs you to the download link if you do not have it installed on your computer.");

        //public readonly string Step3Title = Loc.Localize("HelpToybox_Step3Title", "Selecting Vibrator Kind");
        //public readonly string Step3Desc = Loc.Localize("HelpToybox_Step3Desc", "Chose Between Simulated (No IRL Toy Required), and Actual (Your IRL Toys).");

        //public readonly string Step4Title = Loc.Localize("HelpToybox_Step4Title", "Simulated Audio Selection");
        //public readonly string Step4Desc = Loc.Localize("HelpToybox_Step4Desc", "With a Simulated Vibrator, you can select which audio you want played to you. A quiet or normal version");

        //public readonly string Step5Title = Loc.Localize("HelpToybox_Step5Title", "Playback Audio Devices");
        //public readonly string Step5Desc = Loc.Localize("HelpToybox_Step5Desc", "You can also select which audio device the sound is played back to.");

        //public readonly string Step6Title = Loc.Localize("HelpToybox_Step6Title", "The Device Scanner");
        //public readonly string Step6Desc = Loc.Localize("HelpToybox_Step6Desc", "For actual devices, you can use the device scanner to start / stop scanning for any new IRL devices to connect with.");
        //public readonly string Step6DescExtended = Loc.Localize("HelpToybox_Step6DescExtended", "Any found device should be listed below this scanner when located.");
    }

    public class HelpVibeLobby
    {

    }

    public class HelpPatterns
    {
        //public readonly string Step1Title = Loc.Localize("HelpPatterns_Step1Title", "Creating New Patterns");
        //public readonly string Step1Desc = Loc.Localize("HelpPatterns_Step1Desc", "The Button to click in order to record a new pattern.");

        //public readonly string Step2Title = Loc.Localize("HelpPatterns_Step2Title", "The Recorded Duration");
        //public readonly string Step2Desc = Loc.Localize("HelpPatterns_Step2Desc", "Shows how long your Pattern has been recording for.");

        //public readonly string Step3Title = Loc.Localize("HelpRemote_Step3Title", "The Float Button");
        //public readonly string Step3Desc = Loc.Localize("HelpRemote_Step3Desc", "While active, the pink dot will not drop to the floor when released, and stay where you left it.");
        //public readonly string Step3DescExtended = Loc.Localize("HelpRemote_Step3DescExtended", "Togglable via clicking it, or with MIDDLE-CLICK, while in the remote window.");

        //public readonly string Step4Title = Loc.Localize("HelpRemote_Step4Title", "The Loop Button");
        //public readonly string Step4Desc = Loc.Localize("HelpRemote_Step4Desc", "Begins recording from the moment you click and drag the pink dot, to the moment you release it, then repeats that data. ");
        //public readonly string Step4DescExtended = Loc.Localize("HelpRemote_Step4DescExtended", "Togglable vis button interaction, or using RIGHT-CLICK, while in the remote window.");

        //public readonly string Step5Title = Loc.Localize("HelpRemote_Step5Title", "The Controllable Circle");
        //public readonly string Step5Desc = Loc.Localize("HelpRemote_Step5Desc", "Click and drag the pink dot to move it around. The Higher the dot is, the higher the intensity of the vibrator's motors.");

        //public readonly string Step6Title = Loc.Localize("HelpRemote_Step6Title", "Starting your Recording!");
        //public readonly string Step6Desc = Loc.Localize("HelpRemote_Step6Desc", "Begins storing any vibrator data recorded from dragging the circle around.");

        //public readonly string Step7Title = Loc.Localize("HelpRemote_Step7Title", "Stopping your Recording");
        //public readonly string Step7Desc = Loc.Localize("HelpRemote_Step7Desc", "When you are finished recording, press this again, and a save pattern prompt will appear.");

        //public readonly string Step8Title = Loc.Localize("HelpRemote_Step8Title", "Saving Pattern Name");
        //public readonly string Step8Desc = Loc.Localize("HelpRemote_Step8Desc", "Define the name of the pattern you have created.");

        //public readonly string Step9Title = Loc.Localize("HelpRemote_Step9Title", "Saving Pattern Description");
        //public readonly string Step9Desc = Loc.Localize("HelpRemote_Step9Desc", "Set the description of your pattern here.");

        //public readonly string Step10Title = Loc.Localize("HelpRemote_Step10Title", "Saving Pattern Loop Status");
        //public readonly string Step10Desc = Loc.Localize("HelpRemote_Step10Desc", "Define if your created pattern should loop once it reaches the end.");

        //public readonly string Step11Title = Loc.Localize("HelpRemote_Step11Title", "Optionally Discarding Pattern.");
        //public readonly string Step11Desc = Loc.Localize("HelpRemote_Step11Desc", "If you dont like the pattern you made, you can discard it here.");

        //public readonly string Step12Title = Loc.Localize("HelpRemote_Step12Title", "Adding the New Pattern");
        //public readonly string Step12Desc = Loc.Localize("HelpRemote_Step12Desc", "To Finialize the Pattern Creation, Save & Add the pattern here.");

        //public readonly string Step13Title = Loc.Localize("HelpRemote_Step13Title", "Modifying Patterns");
        //public readonly string Step13Desc = Loc.Localize("HelpRemote_Step13Desc", "Selecting a pattern from the pattern list will allow you to edit it.");

        //public readonly string Step14Title = Loc.Localize("HelpRemote_Step14Title", "Editing Display Info");
        //public readonly string Step14Desc = Loc.Localize("HelpRemote_Step14Desc", "In the editor there is basic display info and adjustments. Display info shows the basic labels of the pattern");

        //public readonly string Step15Title = Loc.Localize("HelpRemote_Step15Title", "Changing Pattern Loop State");
        //public readonly string Step15Desc = Loc.Localize("HelpRemote_Step15Desc", "If you want to change wether the pattern loops or not, you can do so here.");

        //public readonly string Step16Title = Loc.Localize("HelpRemote_Step16Title", "Changing the Start-Point");
        //public readonly string Step16Desc = Loc.Localize("HelpRemote_Step16Desc", "This lets you change the point in the pattern that playback will start at.");

        //public readonly string Step17Title = Loc.Localize("HelpRemote_Step17Title", "Changing the Duration");
        //public readonly string Step17Desc = Loc.Localize("HelpRemote_Step17Desc", "This lets you change how long the pattern playback will go on for from its start point.");

        //public readonly string Step18Title = Loc.Localize("HelpRemote_Step18Title", "Saving Changes");
        //public readonly string Step18Desc = Loc.Localize("HelpRemote_Step18Desc", "Updates any changes you made to your edit.");
    }

    public class HelpAlarms
    {
        //public readonly string Step1Title = Loc.Localize("HelpAlarms_Step1Title", "Creating a New Alarm");
        //public readonly string Step1Desc = Loc.Localize("HelpAlarms_Step1Desc", "To create a new alarm, you must first press this button.");

        //public readonly string Step2Title = Loc.Localize("HelpAlarms_Step2Title", "Setting The Alarm Name");
        //public readonly string Step2Desc = Loc.Localize("HelpAlarms_Step2Desc", "Begin by defining a name for your alarm.");

        //public readonly string Step3Title = Loc.Localize("HelpAlarms_Step3Title", "The localized TimeZone");
        //public readonly string Step3Desc = Loc.Localize("HelpAlarms_Step3Desc", "Your current local time. This means you dont need to worry " +
        //    "about timezones when setting these, just make it your own time.");

        //public readonly string Step4Title = Loc.Localize("HelpAlarms_Step4Title", "Setting the Alarm Time");
        //public readonly string Step4Desc = Loc.Localize("HelpAlarms_Step4Desc", "You can set your time by using the mouse scrollwheel over the hour and minute numbers.");

        //public readonly string Step5Title = Loc.Localize("HelpAlarms_Step5Title", "The Pattern to Play");
        //public readonly string Step5Desc = Loc.Localize("HelpAlarms_Step5Desc", "Select which stored pattern you wish for the alarm to play when it goes off.");

        //public readonly string Step6Title = Loc.Localize("HelpAlarms_Step6Title", "Alarm Pattern Start-Point");
        //public readonly string Step6Desc = Loc.Localize("HelpAlarms_Step6Desc", "Identify at which point in the pattern the alarm should start to play at.");

        //public readonly string Step7Title = Loc.Localize("HelpAlarms_Step7Title", "Alarm Pattern Duration");
        //public readonly string Step7Desc = Loc.Localize("HelpAlarms_Step7Desc", "Identify for how long the patterns alarm should play for before stopping.");

        //public readonly string Step8Title = Loc.Localize("HelpAlarms_Step8Title", "Alarm Frequency");
        //public readonly string Step8Desc = Loc.Localize("HelpAlarms_Step8Desc", "Set the days of the week this alarm go off");

        //public readonly string Step9Title = Loc.Localize("HelpAlarms_Step9Title", "Saving the Alarm");
        //public readonly string Step9Desc = Loc.Localize("HelpAlarms_Step9Desc", "Save/Apply changes and append the new alarm.");

        //public readonly string Step10Title = Loc.Localize("HelpAlarms_Step10Title", "The Alarm List");
        //public readonly string Step10Desc = Loc.Localize("HelpAlarms_Step10Desc", "This is where all created alarms are stored.");

        //public readonly string Step11Title = Loc.Localize("HelpAlarms_Step11Title", "Toggling Alarms");
        //public readonly string Step11Desc = Loc.Localize("HelpAlarms_Step11Desc", "The button you need to press to toggle the alarm state.");
    }

    public class HelpTriggers
    {
        //public readonly string Step1Title = Loc.Localize("HelpTriggers_Step1Title", "Creating a New Trigger");
        //public readonly string Step1Desc = Loc.Localize("HelpTriggers_Step1Desc", "This is the button you use to create a new trigger.");

        //public readonly string Step2Title = Loc.Localize("HelpTriggers_Step2Title", "Trigger Shared Info");
        //public readonly string Step2Desc = Loc.Localize("HelpTriggers_Step2Desc", "Every Trigger Type has a name, priority, and description field to set.");

        //public readonly string Step3Title = Loc.Localize("HelpTriggers_Step3Title", "Trigger Actions");
        //public readonly string Step3Desc = Loc.Localize("HelpTriggers_Step3Desc", "The Trigger Action Kind you select, is the resulting action that is executed once the trigger's condition is met.");

        //public readonly string Step4Title = Loc.Localize("HelpTriggers_Step4Title", "Trigger Action Kinds");
        //public readonly string Step4Desc = Loc.Localize("HelpTriggers_Step4Desc", "Selecting an option from here will set the trigger action kind. Note that base on the kind you select, there are different sub-options to choose.");

        //public readonly string Step5Title = Loc.Localize("HelpTriggers_Step5Title", "Selecting Trigger Types.");
        //public readonly string Step5Desc = Loc.Localize("HelpTriggers_Step5Desc", "You can create many different kinds of triggers using this dropdown, let's overview some of them.");

        //public readonly string Step6Title = Loc.Localize("HelpTriggers_Step6Title", "Chat Triggers");
        //public readonly string Step6Desc = Loc.Localize("HelpTriggers_Step6Desc", "Chat triggers will scan for a particular message within chat, in a spesified set of channels." + Environment.NewLine +
        //    "If desired, you can filter it to be from a certain person.");

        //public readonly string Step7Title = Loc.Localize("HelpTriggers_Step7Title", "Action Triggers");
        //public readonly string Step7Desc = Loc.Localize("HelpTriggers_Step7Desc", "Will execute the trigger when a certain spell or action is used. Can configure variety of settings. (See more)");
        //public readonly string Step7DescExtended = Loc.Localize("HelpTriggers_Step7DescExtended", "Can configure further settings such as damage threshold ammounts, action types beyond damage/heals, and source/target directionals.");

        //public readonly string Step8Title = Loc.Localize("HelpTriggers_Step8Title", "Health % Triggers");
        //public readonly string Step8Desc = Loc.Localize("HelpTriggers_Step8Desc", "Fires a trigger when you or another player passes above or below a certain health value. Can be a raw value or percentage.");

        //public readonly string Step9Title = Loc.Localize("HelpTriggers_Step9Title", "Restraint Triggers");
        //public readonly string Step9Desc = Loc.Localize("HelpTriggers_Step9Desc", "Fires a trigger whenever a particular restraint set becomes either enabled or locked.");

        //public readonly string Step10Title = Loc.Localize("HelpTriggers_Step10Title", "Gag Triggers");
        //public readonly string Step10Desc = Loc.Localize("HelpTriggers_Step10Desc", "Fires a trigger whenever a particular Gag is applied or locked.");

        //public readonly string Step11Title = Loc.Localize("HelpTriggers_Step11Title", "Social Triggers");
        //public readonly string Step11Desc = Loc.Localize("HelpTriggers_Step11Desc", "Fires a trigger whenever you fail a social game.");
        //public readonly string Step11DescExtended = Loc.Localize("HelpTriggers_Step11DescExtended", "Currently only supports DeathRolls");

        //public readonly string Step12Title = Loc.Localize("HelpTriggers_Step12Title", "Emote Triggers");
        //public readonly string Step12Desc = Loc.Localize("HelpTriggers_Step12Desc", "Fires whenever an emote is executed.");

        //public readonly string Step13Title = Loc.Localize("HelpTriggers_Step13Title", "Saving your Trigger");
        //public readonly string Step13Desc = Loc.Localize("HelpTriggers_Step13Desc", "When you are satisfied with your trigger settings, click to create the trigger.");

        //public readonly string Step14Title = Loc.Localize("HelpTriggers_Step14Title", "The Trigger List");
        //public readonly string Step14Desc = Loc.Localize("HelpTriggers_Step14Desc", "The space where your created triggers will be listed.");

        //public readonly string Step15Title = Loc.Localize("HelpTriggers_Step15Title", "Toggling Triggers");
        //public readonly string Step15Desc = Loc.Localize("HelpTriggers_Step15Desc", "Clicking this button switches the triggers between off and on.");
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

        public DDSPrefs DDSPrefs { get; set; } = new();
        public MainOptions MainOptions { get; set; } = new();
        public Preferences Preferences { get; set; } = new();
        public Accounts Accounts { get; set; } = new();
    }

    public class DDSPrefs
    {
        public readonly string FavoritesFirstLabel = Loc.Localize("Preferences_FavoritesFirstLabel", "By Favorites First");
        public readonly string FavoritesFirstTT = Loc.Localize("Preferences_FavoritesFirstTT", "Sort Favorite-First Render for main folders.");

        public readonly string ShowVisibleSeparateLabel = Loc.Localize("Preferences_ShowVisibleSeparateLabel", "Visible Folder");
        public readonly string ShowVisibleSeparateTT = Loc.Localize("Preferences_ShowVisibleSeparateTT", "Lists rendered online kinksters in a separate folder.");

        public readonly string ShowOfflineSeparateLabel = Loc.Localize("Preferences_ShowOfflineSeparateLabel", "Offline Folder");
        public readonly string ShowOfflineSeparateTT = Loc.Localize("Preferences_ShowOfflineSeparateTT", "Lists offline kinksters in a separate group.");

        public readonly string PreferNicknamesLabel = Loc.Localize("Preferences_PreferNicknamesLabel", "Prefer Nicknames");
        public readonly string PreferNicknamesTT = Loc.Localize("Preferences_PreferNicknamesTT", "Still use a kinksters nickname, even while visible.");

        public readonly string FocusTargetLabel = Loc.Localize("Preferences_FocusTargetLabel", "Prefer FocusTarget");
        public readonly string FocusTargetTT = Loc.Localize("Preferences_FocusTargetTT", "Uses the FocusTarget instead of the Target for identifying kinksters." +
            "--SEP--Used when clicking the eye icon in the whitelist.");
    }

    public class MainOptions
    {
        public readonly string HeaderGags = Loc.Localize("MainOptions_HeaderGags", "Gags");
        public readonly string HeaderWardrobe = Loc.Localize("MainOptions_HeaderWardrobe", "Wardrobe");
        public readonly string HeaderPuppet = Loc.Localize("MainOptions_HeaderPuppet", "Puppeteer");
        public readonly string HeaderToybox = Loc.Localize("MainOptions_HeaderToybox", "Toybox");
        public readonly string HeaderAudio = Loc.Localize("MainOptions_HeaderAudio", "Spatial Audio");

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

        public readonly string SpatialAudioActive = Loc.Localize("MainOptions_SpatialAudioActive", "Spatial Audio Features");
        public readonly string SpatialAudioActiveTT = Loc.Localize("MainOptions_SpatialAudioActiveTT", "Emits vibrator audio to nearby paired players when a sex toy is active." +
            "--SEP--Also allows you to hear nearby paired players' sex toy vibrations when they are active.");

        public readonly string VibeLobbyNickname = Loc.Localize("MainOptions_VibeLobbyNickname", "Displayed Name in VibeRooms™");
        public readonly string VibeLobbyNicknameTT = Loc.Localize("MainOptions_VibeLobbyNicknameTT", "Your name in a VibeRoom chat or participant list.--SEP--If left blank, defaults to 'Anon. Kinkster'" +
            "--SEP--This trigger phrase will work when said by ANYONE.");

        public readonly string IntifaceAutoConnect = Loc.Localize("MainOptions_IntifaceAutoConnect", "Auto-Connect to Intiface");
        public readonly string IntifaceAutoConnectTT = Loc.Localize("MainOptions_IntifaceAutoConnectTT", "Automatically connect to the Intiface Desktop App when GagSpeak starts.");

        public readonly string IntifaceAddressTT = Loc.Localize("MainOptions_IntifaceAddressTT", "Set a custom Intiface server address." +
            "--SEP--Leave blank to use the default Intiface server address.");

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
        public readonly string PiShockMaxShockIntensity = Loc.Localize("MainOptions_PiShockMaxShockIntensity", "Max Shock: ");
        public readonly string PiShockMaxShockDuration = Loc.Localize("MainOptions_PiShockMaxShockDuration", "Max Shock Time: ");
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

        public readonly string PrefThreeCharaAnonName = Loc.Localize("Preferences_ThreeCharaAnonName", "Display [Kinkster-###] over [Kinkster-####] in Global Chat");
        public readonly string PrefThreeCharaAnonNameTT = Loc.Localize("Preferences_ThreeCharaAnonNameTT", "Displays the first three characters of a player's name instead of 4." +
            "--SEP--Primary intended for legacy users attached to their 3 character names.");

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

        public readonly string DeleteButtonLabel = Loc.Localize("Accounts_DeleteButtonLabel", "Delete Profile");
        public readonly string DeleteButtonDisabledTT = Loc.Localize("Accounts_DeleteButtonDisabledTT", "Cannot delete this account as it is not yet registered.");
        public readonly string DeleteButtonTT = Loc.Localize("Accounts_DeleteButtonTT", "Deletes this profile from your account!" +
            "--NL----COL--WARNING:--COL--Once an profile is deleted, the secret key is invalidated." +
            "--SEP--Must hold --COL--CTRL+SHIFT--COL--To delete this profile.");
        public readonly string RemoveMainProfileTT = Loc.Localize("Accounts_RemoveMainProfileTT", "Deletes your main profile from your account!" +
            "--SEP----COL--THIS WILL REMOVE YOUR ENTIRE ACCOUNT AND ALL OTHER PROFILES.--COL--" +
            "--SEP--Must hold --COL--CTRL+SHIFT--COL--To delete this profile." +
            "--NL--(A confirmation dialog will open upon clicking this button)");

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
    }

    public class Restrictions
    {
        // Add more here if people actually care for it.
    }

    public class Restraints
    {
        // Add more here if people actually care for it.
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
