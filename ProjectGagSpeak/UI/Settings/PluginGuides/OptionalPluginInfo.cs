namespace GagSpeak.Gui;
public sealed class OptionalPluginInfo
{
    public required string IconUrl;
    public required string Name;
    public required string Author;
    public required string Punchline;
    public required string RepoUrl;

    public string? DiscordUrl { get; init; }
    public string? DiscordTooltip { get; init; }

    public string? GithubUrl { get; init; }

    public List<string> BulletInfo { get; init; } = [];
}
