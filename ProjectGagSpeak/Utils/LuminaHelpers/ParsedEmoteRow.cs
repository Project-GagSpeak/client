using Lumina.Excel.Sheets;
using System.Collections.Immutable;

namespace GagSpeak.UpdateMonitoring;

/// <summary>
/// An internal struct that extracts only the information we care about
/// from an EmoteRow for efficient loading and caching.
/// </summary>
public readonly struct ParsedEmoteRow
{
    public readonly int RowId;
    public readonly ushort IconId;
    public readonly string Name;
    public readonly byte EmoteConditionMode;

    public readonly IEnumerable<string> EmoteCommands;

    public ParsedEmoteRow(Emote emote)
    {
        RowId = (int)emote.RowId;
        IconId = emote.Icon;
        Name = emote.Name.ToString();
        EmoteConditionMode = emote.EmoteMode.Value.ConditionMode;

        // Deal with Lumina ValueNullable voodoo.
        var commands = emote.TextCommand.ValueNullable;
        EmoteCommands = commands.HasValue
            ? new[]
            {
                    commands.Value.Command.ToString().TrimStart('/'),
                    commands.Value.ShortCommand.ToString().TrimStart('/'),
                    commands.Value.Alias.ToString().TrimStart('/'),
                    commands.Value.ShortAlias.ToString().TrimStart('/')
            }
            : ImmutableArray<string>.Empty;
    }

    public string GetEmoteName() => Name;
    public uint GetIconId() => IconId;
}
