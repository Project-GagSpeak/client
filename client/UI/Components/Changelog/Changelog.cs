namespace GagSpeak.UI.Components;
public class Changelog
{
    public List<VersionEntry> Versions { get; private set; } = new List<VersionEntry>();

    public Changelog()
    {
        // append the version information here.
        AddVersionData();
    }

    public VersionEntry VersionEntry(int versionMajor, int versionMinor, int minorUpdate, int updateImprovements)
    {
        var entry = new VersionEntry(versionMajor, versionMinor, minorUpdate, updateImprovements);
        Versions.Add(entry);
        return entry;
    }

    // Add Version Data here.
    private void AddVersionData()
    {
        VersionEntry(0, 9, 9, 13)
            .RegisterMain("Fixed broken signatures (hopefully all of them now)")
            .RegisterFeature("Fixed some remaining bugs and issues with achievements inproperly tracking.");
        VersionEntry(0, 9, 9, 12)
            .RegisterMain("Addedd all internal tutorial modules to the plugin.")
            .RegisterBugfix("Fixed about 50+ bugs i lost count");
        VersionEntry(0, 9, 9, 11)
            .RegisterMain("Final DevPatch update for any beta testers to report anything that may have broken from the update.");
        VersionEntry(0, 9, 9, 10)
            .RegisterMain("PROBABLY THE FINAL CLOSED BETA UPDATE, JUST FYI")
            .RegisterMain("Profile editor has been polished up. Now supports light and full plate previews.")
            .RegisterMain("Duration achievements are now properly tracked.")
            .RegisterFeature("Achievements and cosmetics are now directly linked after a large backend rework. (400% faster)")
            .RegisterQol("Achievements now only skip over achievements that fail to load, instead of wiping all your achievements.")
            .RegisterQol("Polished up the homepage.")
            .RegisterQol("More debug filters for the logger have been added.")
            .RegisterQol("All config versions have been reset to 0, in preperation for launch day.")
            .RegisterBugfix("Fixes with several achievements made.")
            .RegisterBugfix("Puppeteer, Wardorbe, and Whitelist pair lists no longer have conflicting hovers.");
        VersionEntry(0, 9, 9, 8)
            .RegisterMain("Reporting functoinality polling completed!")
            .RegisterMain("added over 50 more achievements!")
            .RegisterFeature("Profile report functionality has now been finalized in its logic implementation.")
            .RegisterFeature("Profiles properly restore and other Kinksters information updates once resolved.")
            .RegisterQol("Lots of quality life backend changes to help ensure safety with the discord bot.");
        VersionEntry(0, 9, 9, 7)
            .RegisterMain("The Localization Overhaul: All major components of the plugin now can support localization.")
            .RegisterMain("GagSpeak Report System is now fully integrated. You are able to report other pairs KinkPlates if they violate our " +
            "team's policy, or are missused to harass others. (Directly or indirectly)")
            .RegisterFeature("Added Localization Base for Global Settings, Preferences, Account Management, and ForcedStay filters.")
            .RegisterFeature("All Achievement Titles have been adjusted to their finalized polished form.")
            .RegisterQol("The Duration Trackers have now been set to detect if a set is no longer active upon reconnection to the server, " +
            "to avoid things tracking after they shouldnt be.")
            .RegisterQol("Font Space used in some headers under settings has been refined.")
            .RegisterQol("The Question mark Text now highlights while over it.")
            .RegisterQol("KinkPlates now have a Mini Flag icon for reporting so you can access them via light KinkPlates as well!")
            .RegisterBugfix("Fixed a bug where profile images from non-paired user's kinkplates did not properly update.")
            .RegisterFeature("Fixed the Chat Box visibility hardcore toggle so it works again.");
        VersionEntry(0, 9, 9, 6)
            .RegisterMain("Hardcore Mode ForcedEmoteState can now fully block out emote execution from any and all sources.")
            .RegisterMain("Reworked the DeathRoll System to have its own module and logic center, for more clear code.")
            .RegisterMain("The entire Puppeteer backend was restructured for more accurate, and more optimal processing.")
            .RegisterMain("DeathRoll Sessions have now had a logic overhaul for more support in handling a diverse range of situations to cover all possibilities.")
            .RegisterFeature("DeathRolls now also process rolls using regex so all languages can be supported.")
            .RegisterFeature("Global Triggers can now also have multiple phrases split with a '|' delimiter.")
            .RegisterQol("Swiftly go from your forced emote to your forced cycle pose with almost 0 transition delay.")
            .RegisterQol("Your entire puppeteer window, trigger phrases, characters, alias list, can be modified however " +
            "much you like, and update only when you hit the save button now.")
            .RegisterQol("You no longer need to hit enter for changes to take effect in the Puppeteer tab.")
            .RegisterQol("DeathRoll sessions will now automatically dispose of any other sessions you either started or joined when joining another session.")
            .RegisterBugfix("Fixed issue where DeathRoll triggers improperly fired.")
            .RegisterBugfix("fixed issue where Puppeteer triggers were not functioning as they should.")
            .RegisterBugfix("fixed issue where puppeteer alias triggers were not functioning as they should.")
            .RegisterBugfix("fixed bug where Puppeteer MotionAllowance permissions only allowed executing emotes that had " +
            "the same database table name as the actual emote command. Now it processes ALL emote commands.")
            .RegisterBugfix("Fixed 7 different bugs with achievements not displaying or executing properly.");

        VersionEntry(0, 9, 9, 5)
            .RegisterMain("More Plugin Cleanup!")
            .RegisterBugfix("Fixed issue where chat triggers didn't work correctly.")
            .RegisterBugfix("Fixed issue where the trigger editor was displaying incorrect information.")
            .RegisterBugfix("Moodles dropdowns no longer display default values but rather the stored selections.")
            .RegisterBugfix("Restraint Trigger actions now functional.")
            .RegisterBugfix("Gag Trigger Actions now functional.")
            .RegisterBugfix("Fixed issue where almost everything in the Spell/Action trigger was borked and making bad calculations.")
            .RegisterBugfix("Fixed issue where updated Intiface Central applications were no longer recognized.")
            .RegisterBugfix("Fixed issue where some achievements were broken.")
            .RegisterBugfix("Restricted Restraint & Gag Trigger Actions and Detections to only Apply & Lock to avoid feedback looping.")
            .RegisterBugfix("Restricted Restraint triggers from calling other restraint triggers. (Same with gags) for the same reason as above.")
            .RegisterBugfix("Fixed issues where gag triggers were not properly recognized.")
            .RegisterBugfix("Fixed issues where restraint triggers were not correctly detected.")
            .RegisterBugfix("Fixed ")
            .RegisterBugfix("Fixed issues where gags removed upon padlocks without timers being removed.");
        VersionEntry(0, 9, 9, 4)
            .RegisterFeature("You can now cache Glamourer Design customizations in restraint sets if you desire.")
            .RegisterBugfix("Fixed gag and restraint calls sending duplicate callbacks.");
        VersionEntry(0, 9, 9, 3)
            .RegisterMain("You can now properly bind Moodles to your Gags & Restraints, without flooding peoples mare or status manager spam.")
            .RegisterFeature("Attempting to remove moodles bound to your gags and restraints will now instantly reapply them before mare can process an update." +
            "\n To them, your moodle will simply remain there, and not have been removed at all. To you? You can try all you like, but it will be fruitless effort~")
            .RegisterQol("Added an enhanced information window for any duration achievements to see actively tracked items.")
            .RegisterBugfix("Fixed issue where logging in and out caused some padlock fields to disappear.")
            .RegisterBugfix("Fixed issue where achievements did not properly know when to stop tracking items")
            .RegisterBugfix("Fixed issue where switching between gags caused a double update instead of a single one. " +
            "(still needed on sets because penumbra reasons)")
            .RegisterBugfix("Fixed issue where the password field on locked gags did not appear.")
            .RegisterFeature("Fixed issue where moodles would not reapply when you removed an item with a moodle that another item still used the moodle of.");

        VersionEntry(0, 9, 9, 2)
            .RegisterMain("The BugFix Update")
            .RegisterQol("Controller input should now work on cursed loot chest (i hope)")
            .RegisterBugfix("Fixed issue where alarms would not save")
            .RegisterBugfix("Fixed issue where patterns did not properly stop")
            .RegisterBugfix("Fixed issue where patterns were not properly linked to the toybox manager on natural lifecycle stops.")
            .RegisterBugfix("Fixed issue where vibe remote images would not display.")
            .RegisterBugfix("Fixed issue where some event viewer messages did not display correctly.")
            .RegisterBugfix("Fixed issue where getting slapped while bound will also be granted when someone else is " +
            "getting slapped and you are just in range for it to get logged")
            .RegisterBugfix("Fixed bug with Quiet Now, Dear, and What a View achievements to properly track emote users / receivers.")
            .RegisterBugfix("Fixed issue where vibes would still play upon logout.")
            .RegisterBugfix("Fixed issue with safeword not clearing gag information properly.")
            .RegisterBugfix("Fixed issue where using a safeword would not fire the correct triggers.")
            .RegisterBugfix("Attempting to fix some issues with achievement timers.")
            .RegisterBugfix("All Achievement Progress Bar Labels now have appropriate text display.");

        VersionEntry(0, 9, 9, 1)
            .RegisterMain("Achievement Title setting has been implemented now and should properly save.")
            .RegisterFeature("Dropdowns for all modifiable components of the kink plate's are not injected into the editor")
            .RegisterQol("The Image editor is now pulled off into its own window to help reduce window clutter.")
            .RegisterQol("The overall display of the image editor has been modified for a cleaner workspace window.")
            .RegisterBugfix("Fixed issue in where profiles sometimes would not update.");
        VersionEntry(0, 9, 9, 0)
            .RegisterMain("Achievements have been restructured for customization support being added soon.")
            .RegisterMain("Account Management / Alt Transfer now properly transfers all data.")
            .RegisterFeature("Light & Full KinkPlate variants now display peoples current achievement counts and titles.")
            .RegisterFeature("Achievement count now updates with actual achievement count.")
            .RegisterBugfix("Fixed issues where settings, gags, restraints, and other things were applied when swapping from a character with a gagspeak account, to one without.");
            
        VersionEntry(0, 9, 8, 6)
            .RegisterMain("Polished up the actions menu.")
            .RegisterFeature("At the top of the actions menu, an error message will now display when a change fails to apply if possible.")
            .RegisterQol("All actions now display Nick, Alias, or UID, in that order")
            .RegisterQol("Tooltips are now decorated the same for default themed tooltip actions.")
            .RegisterQol("Colored tooltip text is now built into the tooltip system.")
            .RegisterBugfix("Fixed issue where timed locks allowed empty times, or times longer than the max allowed times.")
            .RegisterBugfix("Fixed devotional timer locks not displaying the timer field.");
        VersionEntry(0, 9, 8, 5)
            .RegisterQol("Spacing with double ENTER's now pinches the gap on Full KinkPlates to allow more space for people who space out descriptions on their Light KinkPlates.")
            .RegisterQol("Now must use SHIFT+MIDDLE-CLICK to silence a user in global chat.")
            .RegisterBugfix("Fixed issue where you were able to mute yourself (you goober)");
        VersionEntry(0, 9, 8, 4)
            .RegisterMain("You can now preview your own Light KinkPlate in the profile editor.")
            .RegisterFeature("If someone is spamming global chat and being a bother to you, simply Middle-Click the name to silence them from your chat. This applies until plugin reload.")
            .RegisterQol("The preview chat text should now be properly aligned to the bottom (yes, finally)");
        VersionEntry(0, 9, 8, 3)
            .RegisterMain("Light KinkPlates are here!")
            .RegisterFeature("Light KinkPlates™ display the minimum information about yourself on profile popout or mini profile view.")
            .RegisterFeature("To see another Kinksters Light KinkPlate™, right click their name in Global Chat or in a Vibe Server Private Room!")
            .RegisterQol("README HERE:\nIn the profile editor, you can set if your profile is public. Public profiles mean their Light KinkPlate™ displays their description and profile image to non-pairs.");
        VersionEntry(0, 9, 8, 2)
            .RegisterMain("Added Popout 'Light KinkPlates' for quick access to profiles.")
            .RegisterFeature("You can now see the last 3 alphanumerics of Anon. Kinksters UID's so you have a way to communicate with people you may be trying to establish contact with over private rooms or global chat.")
            .RegisterQol("Descriptions are now properly text wrapped.");
        VersionEntry(0, 9, 8, 1)
            .RegisterMain("Profiles now display everything they should properly! YIPPEE!")
            .RegisterFeature("You can now save and update your descriptions.")
            .RegisterBugfix("No longer fails to load some textures.")
            .RegisterQol("Better aligned the portrait so the border doesnt crop off the ends anymore.");
        VersionEntry(0, 9, 8, 0)
            .RegisterMain("The Entire Interaction Backend System has been restructured.")
            .RegisterMain("Please expect any potential bugs with interacting with pairs and report them to me.")
            .RegisterFeature("You can now store a lightweight storage of pairs data for your actions to reference and know what to apply.")
            .RegisterFeature("You should now be able to set descriptions in your profiles.")
            .RegisterQol("All achievement data can now be easily obtained via this new method, and will be integrated shortly.");

        VersionEntry(0, 9, 7, 7)
            .RegisterMain("KinkPlate Designs are now finished being implemented, functionality will be added next.")
            .RegisterFeature("KinkPlates will now appear by middle clicking another pairs name in the whitelist tab.");
        VersionEntry(0, 9, 7, 5)
            .RegisterMain("Backend has now been restructured to handle KinkPlates™.")
            .RegisterFeature("Kink Plate foundation for profile pop-outs is slowly being put together now")
            .RegisterQol("All Core GagSpeak Images are now internally cached for single loads during plugin lifetime, to optimize plate load times and other loads.")
            .RegisterBugfix("Should no longer crash when attempting to use emotes while forced to emote.")
            .RegisterBugfix("KNOWN BUG: Hardcore traits are not being reverted under certain conditions.")
            .RegisterBugfix("KNOWN BUG: Account Management is still a big wonky on what gets transferred or not, this will be looked into after profiles.");

        VersionEntry(0, 9, 7, 4)
            .RegisterBugfix("Using Emotes in ForcedEmote no longer crashes game.");
        VersionEntry(0, 9, 7, 3)
            .RegisterFeature("KinkPlate™ Draft outline startings...")
            .RegisterBugfix("Fixed issue where calculations depended on settings instead of the application of said data.")
            .RegisterQol("You can now view the time a message in global chat was sent by hovering over the name that sent it.");
        VersionEntry(0, 9, 7, 1)
            .RegisterMain("Global chat preview no longer overlaps with the text chat, but will snap to the bottom of the chat, meaning if you scroll up it will go away.")
            .RegisterMain("This is the compromise i had to make in order for it to work.")
            .RegisterMain("Live Chat Garbler Algorithm has been rewritten, and should now allow auto translate messages to be present in messages, " +
            "while removing them and still garbling the text")
            .RegisterBugfix("Fixed an issue in where sending auto translate messages by themselves caused a crash.")
            .RegisterBugfix("IF YOU CRASH WITH THE NEW CHAT GARBLER PLEASE REPORT IT.");
        VersionEntry(0, 9, 7, 0)
            .RegisterMain("Global Chat has been given a light polish.")
            .RegisterMain("Fixed Users Sending Duplicate calls of their Data Updates, " +
            "causing some race conditions to ruin their actively stored states.")
            .RegisterFeature("Devotional Locks now actually work (for real this time)")
            .RegisterFeature("Updates now apply one after the other, in synchronization for a more stable experience.")
            .RegisterFeature("You can now see how many new chat messages are in global chat by the Golden number next to the Global Chat Icon.")
            .RegisterQol("The last 300 messages of global chat now persist between Plugin Loads, so long as you toggle it on the same day.")
            .RegisterBugfix("Fixed the issue in where desyncs occurred (solved from the above features)")
            .RegisterBugfix("Fixed some issues with hardcore emote forcing.");
        VersionEntry(0, 9, 6, 13)
            .RegisterMain("Adding a chat message preview to the global chat.")
            .RegisterFeature("Everyone can now see if the Main GagSpeak dev is in global chat.")
            .RegisterFeature("You keep your chat input focus after sending a message in global chat.")
            .RegisterFeature("Updated Customize+ IPC so it works again")
            .RegisterBugfix("Fixed some other boxs relating to keybind issues.")
            .RegisterBugfix("At the moment some people are sending duplicate calls and im not sure why. Hopefully this is fixed soon.");
        VersionEntry(0, 9, 6, 12)
            .RegisterBugfix("Fixed hopefully all remaining bugs with the forced emote system.");
        VersionEntry(0, 9, 6, 9)
            .RegisterMain("Nice Version Number.")
            .RegisterMain("Hopefully fixes most of the emotelock commands.")
            .RegisterFeature("Please note that it's still going through some of its kinks and its difficult to account for every edge case. Im looking into a better way to catch emote execution but for now what it offers is its best.")
            .RegisterFeature("PLEASE Keep an eye out on your logs when you are using this, if you get flooded with emote messages being forced /safewordhardcore");
        VersionEntry(0, 9, 6, 7)
            .RegisterMain("Reworked Hardcore ForcedSit and ForcedGroundsit to ForcedEmote.")
            .RegisterFeature("You can now force Pairs to maintain ANY looped emote state.")
            .RegisterFeature("You can now force pairs into a spesific Cycle Pose State.")
            .RegisterQol("The Hardcore Action buttons for these have been refined.")
            .RegisterMain("CAUTION: THIS IS A TEST BUILD, THESE NEW FEATURES COULD WORK OR GO INCREDIBLY WRONG.");
        VersionEntry(0, 9, 6, 6)
            .RegisterFeature("Removed the import button as it's no longer necessary with the Pattern Hub")
            .RegisterBugfix("Fixed Distances being incorrectly measured during Hardcore ForcedStay");
        VersionEntry(0, 9, 6, 4)
            .RegisterMain("Finally got around to making a better ApiController than Mare's Controller Format.")
            .RegisterMain("Client-to-Server communication is now more streamlined and efficient.")
            .RegisterMain("Communication Performance will see a 150-300% boost in speed.")
            .RegisterMain("IF YOU ENCOUNTER ANY CONNECTION BUGS PLEASE REPORT THEM.")
            .RegisterFeature("The Add User button now is no longer interactable while disconnected.")
            .RegisterQol("Instead of Disconnecting/Reconnecting 2-4 times every time you try to reconnect, disconnect, or connect, it will now only do what it needs to, once.")
            .RegisterQol("Achievement SaveData is updated on each disconnect, instead of each plugin disposal.")
            .RegisterQol("Due to only needing to perform connection action once, connecting/disconnecting will see a 1.5-3x performance boost.")
            .RegisterQol("(Backend Note: The Toybox & Main Hubs are now split into their own managed hubs, with shared Tokens, for better performance)")
            .RegisterBugfix("The Horizontal line below the connection status no longer clips into the connection status when disconnected.");
        VersionEntry(0, 9, 6, 0)
            .RegisterMain("Full backend rework on Toybox Patterns, Alarms, Triggers for more streamlined functionality and future expandability.")
            .RegisterFeature("Toybox Actions should now be functional properly this time outside of the remote interfaces.")
            .RegisterQol("View Access has been removed from patterns triggers and alarms as you can control this with pair permissions already.")
            .RegisterBugfix("Fixed issue in where using Devotional Toggles in hardcore mode made it impossible to unlock someone in a locked state.");            
        VersionEntry(0, 9, 5, 11)
            .RegisterMain("Mostly all achievements outside of trigger and alarm toggles should be functional now!.");
        VersionEntry(0,9,5,5)
            .RegisterBugfix("Fixed Hardcore Forced Follow UI Bug and ForcedSit bug (Hopefully)");
        VersionEntry(0, 9, 5, 1)
            .RegisterMain("Half of the achievements Functionality has been added.")
            .RegisterMain("CAUTION: This is a TEST BUILD update, meaning a lot more debug messages will occur than usual.")
            .RegisterFeature("Wardrobe / Restraint Data has been changed to a new format, as such any data prior to this update for active sets are wiped.")
            .RegisterFeature("Achievements have a reset button in the UI but i would not recommend clicking it unless you know how to properly resync your data.")
            .RegisterBugfix("More like a known issue than a fixed one, but im aware that my image cache fails to load 1-2% of the time. idk a fix yet.");
        VersionEntry(0, 9, 5, 0)
            .RegisterMain("GagSpeak Interaction Event History is now implemented.")
            .RegisterFeature("New DTR Bar has been added to display total new interaction messages from other pairs.")
            .RegisterFeature("The Bell Icon in the main window now updates from Bell-Slash to Bell whenever you have new notifications.")
            .RegisterQol("Clicking the DTR Bar will open a mini-window to view interactions others have done to you.")
            .RegisterFeature("The Event Viewer UI has been removed. Replaced with the Interactions Viewer.")
            .RegisterBugfix("Various minor bugfixes have been made to the plugin.");

        VersionEntry(0, 9, 4, 7)
            .RegisterMain("ForcedSit/ForcedGroundsit is finally functional.")
            .RegisterFeature("ForcedGroundsit will not detect your current cpose and cycle you to be on your knees.")
            .RegisterFeature("The above feature also works when forced to sit in the open, making you perform a groundsit instead.")
            .RegisterQol("When ForcedSit is active, /cpose is not finally blocked.")
            .RegisterQol("When ordered to sit or groundsit, while already sitting, GagSpeak will no longer make you stand up.");
        VersionEntry(0, 9, 4, 6)
            .RegisterMain("Added further backend support functionality for Hardcore Features.")
            .RegisterFeature("You can now use Hardcore Safeword with CTRL+ALT+BACKSPACE")
            .RegisterFeature("You should now be able to interact with others using the hardcore actions.")
            .RegisterFeature("SafewordHardcore should now disable all active hardcore actions.")
            .RegisterQol("GagSpeak should no longer conflict with Cammy/FreeCam.")
            .RegisterQol("Sit & Groundsit should now function properly.");
        VersionEntry(0, 9, 4, 5)
            .RegisterMain("Patched Hardcore Settings not properly displaying to the gear and actions menus");
        VersionEntry(0, 9, 4, 4)
            .RegisterMain("Full Hardcore Permission Backend Rework.")
            .RegisterFeature("Additonal heardcore features have been added.")
            .RegisterMain("DO NOT PLAY WITH NEW HARDCORE INTERACTIONS YET AS THEY REMAIN UNTESTED WITH NEW FUNCTIONALITY")
            .RegisterMain("If any of the existing hardcore actions end up breaking let me know please.")
            .RegisterFeature("Localization for ForcedStay is in the works.");
        VersionEntry(0, 9, 4, 0)
            .RegisterMain("Hardcore Functionality is in near full functionality. (but may contain bugs)")
            .RegisterFeature("Forced follow now functions properly.")
            .RegisterFeature("Forced Sit not functions properly")
            .RegisterFeature("Forced Stay now functions properly.")
            .RegisterFeature("Forced Blindfold now functions properly, though glamour is still delayed??")
            .RegisterQol("ForcedToStay now has auto selection for apartment and FC Private Chambers.")
            .RegisterQol("You are able to modify the selection of which chamber or apartment is entered while forced to stay.")
            .RegisterBugfix("Fixed Bug where using Safeword would not restore you out of ANY Hardcore attributes.")
            .RegisterBugfix("Fixed bug where player would get locked in place when asked to sit.")
            .RegisterBugfix("Fixed bug in where player could be blindfolded perminantly.")
            .RegisterBugfix("Fixed bug in where you could break out of follow with LMB+RMB")
            .RegisterBugfix("Fixed issue where Immobilize did not work due to LMB+RMB breaking it.");
        VersionEntry(0, 9, 3, 9)
            .RegisterFeature("Preliminary Hardcore Functionality preparation has been added.")
            .RegisterMain("DO NOT TEST HARDCORE FEATURES RIGHT NOW, THEY ARE NOT READY.");
        VersionEntry(0, 9, 3, 8)
            .RegisterMain("OneTimeAccount Generation should work properly again now.")
            .RegisterQol("Warns you if your live chat garbler is active when a mimic gags you in chat and notifications, for safety!");
        VersionEntry(0, 9, 3, 6)
            .RegisterMain("Properly Syncronize Achievement Data on Connection, Reconnection, Pause, Logout, Game Close, and Unhandled Exceptions.");
        VersionEntry(0, 9, 3, 5)
            .RegisterBugfix("Fixed the damn timer padlock bug. I was missing one conditional *cries*");
        VersionEntry(0, 9, 3, 4)
            .RegisterFeature("You can now shift + middle click to set items from penumbra to expanded cursed item windows.");
        VersionEntry(0, 9, 3, 3)
            .RegisterMain("Gag Padlock Images now properly display")
            .RegisterFeature("All Moodles assignments and recalculations have been reworked to support layered bondage.")
            .RegisterFeature("Moodles have been heavily reworked for maximum precision.")
            .RegisterBugfix("Fixed issues where moodles would not properly be removed when they should have been.");
        VersionEntry(0, 9, 3, 2)
            .RegisterBugfix("Cursed Items now toggle the attached mod if they should.");
        VersionEntry(0, 9, 3, 1)
            .RegisterFeature("Added in the permissions for devotional lock allowances")
            .RegisterQol("Added images for devotionalLocks and Mimic Locks");
        VersionEntry(0, 9, 3, 0)
            .RegisterMain("Cursed Loot Gags are now functional.")
            .RegisterMain("Majority of padlock fixes have been made.")
            .RegisterFeature("DEVOTIONAL PADLOCKS are now a new type of padlock.")
            .RegisterFeature("Mimic Padlocks are now a kind of padlock that can be applied by Mimics")
            .RegisterQol("Your Gag's Lock in the ActiveGagsPanel is now colored base on the padlock kind.")
            .RegisterBugfix("Your game no longer will crash when opening a coffer with no cursed items in your pool.")
            .RegisterBugfix("Padlocks such as Owner Padlocks can now properly be removed.")
            .RegisterBugfix("Padlocks applied by other players can now be removed from active gags panel if they normally should be.");
        VersionEntry(0, 9, 2, 0)
            .RegisterMain("Cursed loot functionality has been readded to the plugin.")
            .RegisterMain("The Appearance Updater has recieved an overhaul to work better.")
            .RegisterFeature("Cursed Loot UI Has been completely revamped for individual items.")
            .RegisterFeature("Cursed Loot can be either gags or equipment items. (Gags Non-Functional ATM)")
            .RegisterFeature("Appearance Updates have been overhauled and are now 15x faster than before.")
            .RegisterFeature("Application inconsistancies have been fixed.")
            .RegisterQol("Updates happen now as: RESTRAINT -> GAG -> CURSED ITEM -> BLINDFOLD")
            .RegisterBugfix("Fixed cases where some calls updated things out of order.")
            .RegisterBugfix("Fixed blindfolds taking forever to apply.")
            .RegisterBugfix("Fixed game crashing on closing game");
        VersionEntry(0, 9, 1, 0)
            .RegisterMain("I HIGHLY RECOMMEND YOU SET YOUR CURSED LOOT CHANCE TO 0%")
            .RegisterFeature("New overhaul rework for cursed loot being implemented. Still IN PROGRESS.")
            .RegisterFeature("You should be able to set up cursed items now in the cursed loot tab. Report any bugs you find.");
        VersionEntry(0, 9, 0, 2)
            .RegisterMain("Added the rest of the event triggers into the achievement monitors.")
            .RegisterFeature("Achievements now accurately display progression like they should.")
            .RegisterBugfix("Fixed bugs where satisfying certain conditions caused crashes, these should now be handled within the event manager to prevent crashes.");
        VersionEntry(0, 9, 0, 1)
            .RegisterMain("WARNING: 0.9.0.0 CURRENTLY INTRODUCED SOME DX11 CRASHES FROM PERFORMING CERTAIN ACTIONS.")
            .RegisterFeature("I've done my best to catch all conditions that trigger dx11 crashes and contained them.")
            .RegisterFeature("I've plugged in more event handles for triggers on achievement conditions.")
            .RegisterFeature("I've applied trigger conditions for 20/37 total triggers, will do more when i wake up tomorrow.")
            .RegisterBugfix("There are a lots, i know. Im panicking because of the deadline for open beta. Be gentle. Thanks.");
        VersionEntry(0, 9, 0, 0)
            .RegisterMain("Achievements are now implemented into GagSpeak.")
            .RegisterFeature("The Achievements Menu has been added to the homepage.")
            .RegisterFeature("Achievements sync with the server every 30m, and retain data through server interrupts and restarts.")
            .RegisterQol("Note: Most Achievements don't work yet, i need to heavily test out the actions with other beta testers to get it right");
        VersionEntry(0, 8, 8, 2)
            .RegisterBugfix("Fixed crash when getting cursed loot due to a bad index match.");
        VersionEntry(0,8,8,0)
            .RegisterMain("Cursed Bondage Loot (Cursed Dungeon Loot) Is now a component of the Wardrobe Module.")
            .RegisterFeature("Assign a pool of restraint sets that Cursed Loot can use when functioning.")
            .RegisterFeature("Define the lower and upper lock period limits for customization.")
            .RegisterFeature("Bind defined gags to each active cursed set in the pool.")
            .RegisterFeature("Adjust the randomization / frequency Cursed Loot appears.")
            .RegisterFeature("Relish in gambling your freedom away with the new Cursed Loot system.~ ♥")
            .RegisterQol("Improved some backend logic for increased framework performance times.");
        VersionEntry(0, 8, 7, 6)
            .RegisterMain("Warning this version is very cursed. Mainly meant for debugging while i sleep.")
            .RegisterFeature("Some debugging variables spawn in the homepage to help with some achievement scouting.");
        VersionEntry(0, 8, 7, 5)
            .RegisterMain("Hardcore Blindfolds now function properly.")
            .RegisterFeature("WARNING: DO NOT TRY FORCED TO STAY OR FORCED TO SIT JUST QUITE YET THEY ARE UNTESTED.");
        VersionEntry(0, 8, 7, 4)
            .RegisterMain("Adding a testing interaction of a hardcore chat restrictor to the homepage.");
        VersionEntry(0, 8, 7, 3)
            .RegisterMain("This bug was such a big deal its now being posted as a Main Entry." + Environment.NewLine
            + "FINALLY fixed the bug where GagSpeak would rapidly update glamour endlessly, due to completing an update on the same tick framework tick")
            .RegisterFeature("Added framework foundation for cosmetic achievements.")
            .RegisterBugfix("Fixed Gags not swapping properly anymore with the new server restructure.")
            .RegisterBugfix("Fixed Gag Swaps not properly unequipping gags on a gag swap.");
        VersionEntry(0, 8, 7, 2)
            .RegisterFeature("Added a quick Open Actions Button for visible pair context menus.");
        VersionEntry(0, 8, 7, 1)
            .RegisterBugfix("Fixed Moodles dropdowns globally increasing the effected size of dropdowns.");
        VersionEntry(0, 8, 7, 0)
            .RegisterMain("Added a more Advanced Logger System that adds logger filter categories beyond provided ones.")
            .RegisterFeature("Switch on or off the individual logger categories in the settings window for helpful debugging.")
            .RegisterQol("Added an all on / all off button to auto assign all recommended logger filters or turn them all off.")
            .RegisterQol("Reorganized the asset folders into their own directories for less messy output navigation.");
        VersionEntry(0, 8, 6, 2)
            .RegisterMain("Logic for handling puppeteer trigger phrases should be mostly corrected now.")
            .RegisterQol("The main puppeteer permission options have been added as buttons in the trigger phrase box.");
        VersionEntry(0, 8, 6, 0)
            .RegisterMain("Gag and Restraint Set Actions now have correct logic. Major Debugging Part 1")
            .RegisterFeature("The Puppeteer UI has been reworked to a cleaner design layout.")
            .RegisterQol("You can now interact with the user list items without needing to hover the player name text itself.")
            .RegisterQol("Puppeteer userPair selectors now display which pair is selected.");
        VersionEntry(0, 8, 5, 2)
            .RegisterMain("Clicking on the Privacy DTR bar opens a mini-window to click on player names to view locations.");
        VersionEntry(0, 8, 5, 1)
            .RegisterMain("Client now properly communicates with server and discord bot for messages and reconnections.");
        VersionEntry(0, 8, 4, 0)
            .RegisterMain("Reworked entire backend for how Client Player Data is stored and synced.")
            .RegisterFeature("You can now 'REAPPLY' to game/Automation, retaining your customizations.")
            .RegisterQol("You can clone restraint sets now.");
        VersionEntry(0, 8, 3, 1)
            .RegisterMain("You can now Clone & Remove restraint sets from the selector menu.")
            .RegisterFeature("Added Button to import your actors current equipment into restraint Appearance.");
        VersionEntry(0, 8, 3, 0)
            .RegisterMain("Customize+ Gag Integration is now fully functional.")
            .RegisterMain("C+ Gags can have set priorities to prioritize one preset over another.")
            .RegisterFeature("A testing simulation of StruggleSim is added, but currently does nothing.")
            .RegisterQol("Presets automatically reapply when you try to disable them.");
        VersionEntry(0, 8, 2, 0)
            .RegisterMain("Action/Spell Triggers can now be used as a Trigger Type.\n(I stress tested it in pvp and alliance raids without crashing, so should be fine)")
            .RegisterMain("Health% Triggers can now be used as a Trigger Type.")
            .RegisterMain("Restraint Monitored Triggers can now be used as a Trigger Type. (Fire Triggers when certain sets are set to certain states [Applied, Locked, ext.])")
            .RegisterMain("GagState Monitored Triggers can now be used as a Trigger Type. (Fire Triggers when certain gags are set to certain states (Lock&Unlock do not work at the moment)")
            .RegisterMain("Added a new Trigger Type: Social Action. (Currently only supports DeathRollLoss)")
            .RegisterMain("Added new Shock Collars Action TriggerActionType. It's limitations reflect your global PiShock ShareCode.")
            .RegisterMain("Added new Restraint Set Application TriggerActionType.")
            .RegisterMain("Added new Gag Application TriggerActionType.")
            .RegisterMain("Added new Moodle Status Application TriggerActionType.")
            .RegisterMain("Added new Moodle Preset Application TriggerActionType.")
            .RegisterFeature("The Commands '/dr' (DeathRoll) and '/dr r' (DeathRoll Reply) are now implemented to help with DeathRoll games.")
            .RegisterQol("The Edit Trigger Header now displayed 'Edit Trigger' instead of the trigger name to avoid text going off the screen.")
            .RegisterQol("Additional Logging options have been added to the debug tab, to hide battery update check and alarm check logs.")
            .RegisterBugfix("(Partially Fixed) issue in where Dropdowns that displayed selected options, were not actually selected. (Still WIP on this)");
        VersionEntry(0, 8, 1, 3)
            .RegisterBugfix("Finally fixed the bug where GagSpeak crashed your game when accessing the Moodles page.")
            .RegisterBugfix("This bug occurred for players who had 0 presets made, and it tried to draw a blank combo box incorrectly.");
        VersionEntry(0, 8, 0, 0)
            .RegisterMain("PiShock Beta Integration is implemented.")
            .RegisterFeature("PiShock Options in the settings window have been appended.")
            .RegisterFeature("PiShock Global & Per-Pair options have been included in the permissions popout and GagSpeak Settings.")
            .RegisterFeature("DO NOT TEST THESE IF YOU ARE NOT WORKING DIRECTLY WITH MY ON DEVELOPMENT. STILL TESTING FEATURES.");
        VersionEntry(0, 7, 5, 1)
            .RegisterMain("Added Toybox Device Debugger")
            .RegisterFeature("Additional Buttons in toybox settings to auto connect to intiface & the vibe server upon server connection have been added.");
        VersionEntry(0, 7, 5, 0)
            .RegisterMain("Full backend restructure to permission Actions. May be buggy, please be patient.")
            .RegisterMain("Added Toybox interactions (ALL UNTESTED ATM)")
            .RegisterBugfix("Fixed /safeword sending your plugin state into limbo.");
        VersionEntry(0, 7, 4, 1)
            .RegisterMain("Force Headgear and Force Visor on gags now FINALLY WORK.");
        VersionEntry(0, 7, 4, 0)
            .RegisterMain("Updated Buttplug Package to v4.0")
            .RegisterFeature("Added a new preset dropdown to bulk set permissions for pairs.")
            .RegisterFeature("DO NOT TEST THIS FEATURE YET, IT IS UNTESTED");
        VersionEntry(0, 7, 3, 3)
            .RegisterBugfix("I Forgot to update the api lol. Please see version 0.7.3.2 for update details.");
        VersionEntry(0, 7, 3, 2)
            .RegisterMain("Added further Preparations for trigger logic implementation on Sunday.")
            .RegisterQol("Optimized the ActionEffectMonitor to only perform logic on action types we care about, and discard the rest instantly. Leading to performance boost.");
        VersionEntry(0, 7, 3, 1)
            .RegisterFeature("Please refer to 0.7.3.0 for full major changes.")
            .RegisterQol("Author fields of downloaded patterns can no longer be edited.")
            .RegisterQol("Downloaded patterns publish buttons are disabled.")
            .RegisterBugfix("Fixed download button issue.")
            .RegisterBugfix("Fixed other minor bugs.");

        VersionEntry(0, 7, 3, 0)
            .RegisterMain("PatternHub Tab is now functional!")
            .RegisterMain("Toybox Patterns Tab now allows you to publish and unpublish patterns to the PatternHub.")
            .RegisterMain("PatternHub Likes, Downloads, and display boxes have been introduced.")
            .RegisterFeature("Upload up to a maximum of 10 patterns a week. (up to 20 for supporters)")
            .RegisterFeature("Like other users patterns and download them to your pattern library.")
            .RegisterFeature("Search for patterns based on various filters, and sort styles.")
            .RegisterQol("Patterns in the toybox tab now display a globe if they are published. (This can be a bit buggy if you remove them from your config, so until i add a new thing for that hang in there and deal with it i guess.")
            .RegisterQol("Popup buttons now have actual transparency, instead of faked transparency.")
            .RegisterBugfix("Fixed incorrect alignment in some parts throughout the plugin.");

        VersionEntry(0, 7, 2, 4)
            .RegisterQol("Lock and Unlock and Apply action dropdowns now properly disable and enable when they should. Alignment also fixed.")
            .RegisterQol("Lock Restraint Set action button now displays a persons locked restraint set while they are locked.")
            .RegisterBugfix("Fixed Owner Padlocks incorrectly checking against submitted passwords over player name matching.");

        VersionEntry(0, 7, 2, 3)
            .RegisterMain("Im a silly goober and tried to open the changelog before it was registered.");

        VersionEntry(0, 7, 2, 2)
            .RegisterMain("Added Changelog Window")
            .RegisterFeature("New PatternHub tab is available in the main window. (Currently has no functionality)")
            .RegisterQol("Helmet and Visor toggles are implemented, but will not work until Buttplug.IO updated to 3.0.2 (somepoint this weekend)")
            .RegisterBugfix("Fixed an issue where slots outside the helmet slot would not unequip the glamourerd item when unequipped via the active slot panel.");
    }
}
