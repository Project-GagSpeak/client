namespace GagSpeak.Gui.Changelog;

// The Elements that makeup the Changelog.yaml file
public sealed class ChangelogFile
{
    public FAI Icon { get; set; }
    public string Tagline { get; set; }
    public string Subline { get; set; }
    public List<ChangelogVersion> Changelog { get; set; }
}

public sealed class ChangelogVersion
{
    public string Version { get; set; }
    public string Date { get; set; }
    public string Title { get; set; }
    public string HeaderMessage { get; set; }
    // The sections of each changelog version
    public List<VersionSegment> Segments { get; set; }
}

public sealed class VersionSegment
{
    public FAI Icon { get; set; }
    public string Title { get; set; }
    public AccentColor Accent { get; set; }
    
    // Nested groupings for complex segments
    public List<VersionSubsection> Subsections { get; set; } = [];

    // Direct bullets for simple segments
    public List<ChangelogBullet> Bullets { get; set; } = [];
}

public sealed class VersionSubsection
{
    public string Title { get; set; }
    public List<ChangelogBullet> Bullets { get; set; } = [];
}

// The new class to hold bullet data
public sealed class ChangelogBullet
{
    public string Text { get; set; }
    public bool IsImportant { get; set; }
    public string Contributor { get; set; } = string.Empty;
}

public enum AccentColor
{
    None = 0,
    Gold,
    Yellow,
    Orange,
    Teal,
    Blue,
    Purple,
    Red,
    Pink,
    Grey,
    Green,
}
