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
        VersionEntry(1, 0, 0, 0)
            .RegisterMain("Initial Public Open-Beta Release of Project GagSpeak")
            .RegisterFeature("All Features from Closed Beta Migrated to Open-Beta")
            .RegisterFeature("Expect UI To get overall polish, and other documented or closed off featured to be completed during open Devlopment.");
    }
}
