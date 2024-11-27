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
