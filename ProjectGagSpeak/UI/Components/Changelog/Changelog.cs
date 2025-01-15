using FFXIVClientStructs.FFXIV.Client.Game.Control;

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
        VersionEntry(1, 2, 1, 6)
            .RegisterMain("Fixed all the lingering issues since 1.2.1.0 in regards to appearance updates.")
            .RegisterMain("IF YOU HAD A PADLOCK ON PRIOR TO 1.2.1.0, YOU WILL NEED TO SAFEWORD OUT OF IT.");
        VersionEntry(1, 2, 1, 0)
            .RegisterMain("Please be patient with me and features, i will be pivoting my attention to helping add additions to glamourer soon.")
            .RegisterMain("Fixed backend logic for client padlocks to retain same structure as padlocks, which should correct most sync issues.")
            .RegisterQol("Timer locks should now function properly.")
            .RegisterFeature("Backend structure for Orders Module is now implemented.")
            .RegisterFeature("Backend alignment with appearance updates is now more streamlined.")
            .RegisterFeature("Active Restraint Sets are now stored server-side, along with gags.")
            .RegisterFeature("Emote Trigger section has been added, but is not functional. Triggers rework update next.™")
            .RegisterQol("Command help for /safeword and /safewordhardcore parameters have been added.")
            .RegisterQol("GagSpeak will now wait for you to be fully present after login before reactivating hardcore states.")
            .RegisterQol("Some achievements are not functional once more / work properly.")
            .RegisterQol("You can now toggle client-side settings while offline.")
            .RegisterQol("Puppeteer alias permissions are now implemented and functional.")
            .RegisterBugfix("Prevented crash that occured while disabling the plugin in the menu after logging out.")
            .RegisterBugfix("Fixed issue where the forced stay filters cleared on each plugin load.");
        VersionEntry(1, 2, 0, 0)
            .RegisterMain("HIGHLY RECOMMEND YOU SKIM THROUGH THIS")
            .RegisterMain("Custom Pair Action Combos are now Live! See the finer details about what you apply to your pairs!")
            .RegisterMain("A new Timer Padlock has been added, no more hope of guessing the password for you!")
            .RegisterFeature("GagType dropdowns lets you see their enabled Gag Glamours, along with the slot and item.")
            .RegisterFeature("Restraint dropdowns lets you preview the outfit and view any Hardcore traits applied when you equip it.")
            .RegisterFeature("Pattern dropdowns lets you see pattern descriptions, if they loop, and how long they run for!")
            .RegisterFeature("Alarm dropdowns let you see when they go off (scoped to your time zone), and what pattern it sets off.")
            .RegisterFeature("Trigger dropdowns let you see trigger priority, description, its kind, and execution action type.")
            .RegisterQol("Padlocks have now had their permission hierarchy restructured. There is now unique permissions for allowing " +
            "permanent, owner, and devotional type locks, giving you full control over what locks can be applied, and for how long.")
            .RegisterQol("GagStorage now properly waits for you to hit save before updating the data, and will not revert if you click off.")
            .RegisterQol("LightStorage now also contains a few extra lightweight details about the data on pairs you store.")
            .RegisterQol("SafewordUsed is no longer in permissions to avoid confusion.")
            .RegisterQol("Aggressive detection for C+ is now in place, if issues persist, it is likely a customize+ issue.")
            .RegisterQol("ForcedStay text node filters now use proper syntax for serialization (backend stuff)")
            .RegisterQol("Preliminary support for Puppeteer AllowAliasTriggers permission level has been added, logic WIP.")
            .RegisterQol("Pairs you have not allowed to apply your own moodles to you, will not be able to see your moodles in the wardrobe.")
            .RegisterQol("Kinkster-### is not Kinkster-####, this can be opt-out in the config settings under preferences.")
            .RegisterBugfix("Fixed an issue that caused ")
            .RegisterBugfix("Achievement 'Silence of Shame' properly tracks now.");
        VersionEntry(1, 1, 3, 1)
            .RegisterFeature("This whole time ive been handling movement controls with Forced follow wrong. It will now read your movement mode when it starts and cache it. " +
            "Afterwards, it will update your movement only while cached, and restore it to the cached mode afterwards. Prior to this the framework updates were changing your " +
            "movement prior to it being cached so it was always being set to legacy and never reverting. This should no longer occur.")
            .RegisterBugfix("Fixed issue where time required achievements were counting down instead of up.")
            .RegisterBugfix("fixed issue that caused automatically stopped forced follows to not halt achievement timers.")
            .RegisterBugfix("Fixed the issue where the 6 hour interval token refreshes could return invalid auth tokens while zoning. (Extremely rare edge case).")
            .RegisterBugfix("Added a 2 second wait time after blindfold equips to account for mares 1st person update delays until they fix it.");
        VersionEntry(1, 1, 3, 0)
            .RegisterFeature("Added FAQ section in the account tab of the UI")
            .RegisterQol("The DTR bar options function with the action notifiers correctly.")
            .RegisterQol("The option to hide the main UI actually does something now.")
            .RegisterQol("The privacy filter DTR only displays player characters now and not NPC's")
            .RegisterBugfix("Fixed issue where PVP kills required a vibe AND restraint set.")
            .RegisterBugfix("Hopefully fixed most hardcore achievements.");
        VersionEntry(1, 1, 2, 0)
            .RegisterMain("The Achievement System Issue/Bug Polish Update.")
            .RegisterFeature("Achievement Data will no longer update if you are in ANY state where your data is not either fully loaded, or any issues occurred during loading state.")
            .RegisterFeature("Duration achievements now have a leeway gap to make sure if you lock something and the achievement doesn't register it until a second later, " +
            "you wont run into issues with losing progress.")
            .RegisterFeature("Duration Achievement Progress on self is now checked upon each connection, and automatically rewarded if you met the milestone.")
            .RegisterQol("The progress bars now update in a more dynamic manner than by a percentage based increase based on the end milestone unit.")
            .RegisterQol("To ensure that Achievement Titles fit in kinkplates, the title 'I Can't Believe You've Done This' is now 'You've Done It Now'.")
            .RegisterBugfix("Fixed issue where the pvp kill achievements did not require you to be restrained before. But now they do.")
            .RegisterBugfix("Fixed the 'Escaping Isn't So Easy' achievement.")
            .RegisterBugfix("Hopefully fixed the forced follow movement change legacy issue for most.")
            .RegisterBugfix("Prevent Gags with empty slots from being considered a valid item to store.");
        VersionEntry(1, 1, 1, 2)
            .RegisterFeature("Quick HotFix to ensure that duration based achievements give a leeway gap for the achievement to trigger a success.")
            .RegisterFeature("Working on deducing a permanent fix to the achievement bug is still in progress but wanted to get this out ASAP.");
        VersionEntry(1, 1, 1, 1)
            .RegisterFeature("When using /safeword or /safewordhardcore, you can provide a UID at the end of the command to isolate the revert function to a single pair.")
            .RegisterFeature("You can now adjust the opacity of the blindfold overlay.")
            .RegisterQol("You can now adjust the opacity of the blindfold overlay.")
            .RegisterQol("You can now click through the blindfold window.")
            .RegisterQol("Privacy DTR bar no longer shows characters that are not controllable players (no more examine actor ghosts).")
            .RegisterQol("Kinkplates now abide by dalamuds global scaling tool.")
            .RegisterQol("Kinkplates have had their width expanded to account for longer titles, and the height expanded to account for the overflow line in the description.")
            .RegisterQol("Kinkplate profile pictures are now slightly larger in light kinkplates.")
            .RegisterQol("Sends event messages on PiShock instructions send to you from others.")
            .RegisterQol("HOPEFULLY fix the blindfold delay?")
            .RegisterBugfix("Fixed Devotional locks breaking blindfolds.")
            .RegisterBugfix("Fixed movement always resetting to standard upon disabling the plugin if you had legacy prior.")
            .RegisterBugfix("made minor bug fixes to resolve over 30+ reported other minor bugs.");
        VersionEntry(1, 1, 1, 0)
            .RegisterMain("Authentication issues should be almost if not completely resolved now... (I pray and hope so) Safe to say more may spawn but should be easier to patch.")
            .RegisterFeature("Prevent duplicate keys from characters.")
            .RegisterFeature("Prevent people from trying to connect as a primary user with an alt account.")
            .RegisterFeature("Cursed Loot should now function in Deep Dungeons.")
            .RegisterBugfix("Fixed crash that occursed when you reconnected with an expected restraint set that no longer exists for you.")
            .RegisterBugfix("Fixed issue where the 'Your Rubber Slut' Achievement required 4 days instead of 14 days.")
            .RegisterBugfix("Fixed an issue where the pair requests only displayed 1 outgoing max.")
            .RegisterBugfix("Fixed pair request messages not displaying.")
            .RegisterBugfix("Fixed where migrations for restraint sets were not allowing 'Transfer All' unless you had a cursed item created for some reason.");
        VersionEntry(1, 1, 0, 3)
            .RegisterMain("Update for new Moodles tuple status. Until Moodles is 1.0.0.39 expect the ShareHub and other Moodles features to be non-functional.");
        VersionEntry(1, 1, 0, 2)
            .RegisterBugfix("We're just not going to talk about how stupid of a fix this was.");
        VersionEntry(1, 1, 0, 0)
            .RegisterMain("Moodles ShareHub is now Live!")
            .RegisterMain("'My Publications' tab is now in the Home Page. Manage published uploads of Patterns & Moodles in one place!")
            .RegisterFeature("Browse, Like, Un-Like, Copy, and Try-On browsed Moodles in the Moodle ShareHub!")
            .RegisterFeature("Use the 'Try On' feature to apply moodles to your character before copying them to your status list to see how you like them!")
            .RegisterFeature("ACHIEVEMENT SAVE DATA IS NOW OUTPUT TO YOUR LOGS EVERY TIME YOU CONNECT TO THE SERVER. IF IT SOMEHOW RESETS BEFORE I CAN FIX " +
            "THE ISSUES, SEND ME YOUR LOGS IMMIDIATELY SO I CAN RESTORE THEM.")
            .RegisterQol("A new interface for uploading and publishing Moodles now exists.")
            .RegisterQol("All publication features from the pattern edit window and save window have been removed, and moved to the Publications UI.")
            .RegisterQol("You can now like patterns and moodles once again.")
            .RegisterBugfix("Fixed the rare issue in where loading the game after repedative crashing failed to load " +
            "required configs from services. (or at least a band-aid fix)");
        VersionEntry(1, 0, 4, 2)
            .RegisterFeature("Support for new Moodles Preset and Stack On Reapply mare issue fixed.");
        VersionEntry(1, 0, 4, 1)
            .RegisterBugfix("Fixed the issue where sending requests with no pairs added displayed nothing, softlocking new users.");
        VersionEntry(1, 0, 4, 0)
            .RegisterMain("Replaced adding Kinksters with a Kinkster Request System.")
            .RegisterMain("Send or Cancel Kinkster Requests, and Accept / Decline incoming ones.")
            .RegisterMain("Kinkster Requests can be sent directly from Global Chat.")
            .RegisterFeature("A Primer for the Moodles Share Hub has been added.")
            .RegisterQol("Your 3 Letter tag is now displayed beside paired users in Global Chat for better clarity.")
            .RegisterQol("The ammount of cached messages in global chat was reduced from 1000 to 350 for increased performance.")
            .RegisterQol("Instead of using mouse keybinds to interact with users in global chat, there is a Popup menu instead." +
            "This should help prevent accidentally performing a kinkster pair request to the wrong person if chat moves too fast.");

        VersionEntry(1, 0, 3, 1)
            .RegisterFeature("Added Global Alias Triggers.")
            .RegisterQol("Overall grammer and spelling improvements.")
            .RegisterQol("Added the ability to clone hardcore permissions set for one Kinkster to other pairs in different collection types.")
            .RegisterBugfix("Fixed issue where you couldnt remove alias triggers.")
            .RegisterBugfix("Fixed issue where you couldnt fire commands that were not aliases at all lol.");
        VersionEntry(1, 0, 3, 0)
            .RegisterMain("Puppeteer Has gotten a massive DLC expansion pack kink deluxe.")
            .RegisterMain("Puppeteer alias outputs can now contain all trigger actions.")
            .RegisterFeature("Chat Triggers were removed.")
            .RegisterFeature("Trigger configs were reset due to the above change.")
            .RegisterQol("Alias Triggers should be automatically migrated for you.")
            .RegisterBugfix("Lots of bugs are still known in my #gagspeak-issues channel, i will try and sort through them overtime. Please be paitent.");
        VersionEntry(1, 0, 2, 1)
            .RegisterMain("Appended back the Puppeteer Trigger Info section of the Puppeteer Module. (Alias Lists still in progress)")
            .RegisterQol("Update pair with your UID button has been moved to the puppeteer UI and no longer in the actions list.")
            .RegisterBugfix("None yet, i want to finish this up first...");
        VersionEntry(1, 0, 2, 0)
            .RegisterMain("Puppeteer Module temporarily under Maitenance after recovering from the backend changes.")
            .RegisterFeature("You will now be able to see when you have successfully updated a pair with your name for puppeteer.")
            .RegisterFeature("Alias List entries now have a better UI and can be labeled.")
            .RegisterBugfix("Fixed the backend explosion that ruined everything.")
            .RegisterBugfix("Fixed some other bugs.");
        VersionEntry(1, 0, 1, 0)
            .RegisterQol("Glamourer Automatic Recalculations now only occur when you are Gagged, Restrained, or have Cursed Loot active, instead of happening all the time. This was an oversight, I apologize.")
            .RegisterQol("New Account creation now WAITS until you have a valid registered authentication before appending your primary account.")
            .RegisterQol("If you attempt to make your primary account during server downtime, GagSpeak will reset the authentication and allow you to use the button again.")
            .RegisterBugfix("Tells now properly garble when enabled.")
            .RegisterBugfix("The issue where new Cursed Items could not be made after removing a cursed item has been fixed.")
            .RegisterBugfix("Rolling the same value in a deathroll as the cap is no longer seen as an invalid roll.")
            .RegisterBugfix("When in forced stay, you will no longer attempt to interact with the chamber enterance node in estates where one doesn't exist.")
            .RegisterBugfix("Fixed issue where removing patterns/restraints with the right click popup menu crashed the game.")
            .RegisterBugfix("No longer checks say chat back on in the garbler preferences.")
            .RegisterBugfix("Fixed Duration achievements displaying progress in the UI incorrectly.")
            .RegisterBugfix("A Note on anyone reporting Mare related Crashes: This occurs due to (what I highly presume to be the case) Mare loading Mods with invalid Vertexs (invalid mods) multiple times in quick succession." +
            "You likely already notice these crashes occur when people with broken mods load in for the first time, because on login's and first loads they load twice. The same occurs when mare connects and " +
            "tries to load someones glamour and their restraint set. GagSpeak does not interact with Mare, they have no association with a reason for crash than any other plugin loading a set at the same time would have.");
        VersionEntry(1, 0, 0, 6)
            .RegisterBugfix("Fixed Global Chat not checking if Live Chat Garbler was active or not before translating a message.")
            .RegisterBugfix("Fixed Cursed Loot not working when in solo instances. (no it doesnt work in Deep dungeons, stop asking.)");

        VersionEntry(1, 0, 0, 0)
            .RegisterMain("Initial Public Open-Beta Release of Project GagSpeak")
            .RegisterFeature("All Features from Closed Beta Migrated to Open-Beta")
            .RegisterFeature("Expect UI To get overall polish, and other documented or closed off featured to be completed during open Devlopment.");
    }
}
