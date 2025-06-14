using Lumina.Excel.Sheets;
using System.Collections.Immutable;

namespace GagSpeak.Utils;

/// <summary>
/// An internal struct that extracts only the information we care about
/// from an EmoteRow for efficient loading and caching.
/// </summary>
public readonly record struct ParsedEmoteRow : IEquatable<ParsedEmoteRow>
{
    public readonly uint RowId;
    public readonly ushort IconId;
    public readonly string Name;
    public readonly byte EmoteConditionMode;

    public readonly ImmutableArray<string> EmoteCommands;

    // maybe remove idk.
    public IEnumerable<string> CommandsSafe 
        => EmoteCommands.IsDefault ? Enumerable.Empty<string>() : EmoteCommands;

    public ParsedEmoteRow()
    {
        RowId = 0;
        IconId = 450;
        Name = string.Empty;
        EmoteConditionMode = 0;
        EmoteCommands = ImmutableArray<string>.Empty;
    }

    public ParsedEmoteRow(Emote emote)
    {
        RowId = emote.RowId;
        IconId = (ushort)(emote.Icon == 64350 ? 405 : emote.Icon);
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
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToImmutableArray()
            : ImmutableArray<string>.Empty;
    }

    public string InfoString
        => $"Emote: {Name} ({RowId})(Icon {IconId})-(Cond.{EmoteConditionMode}) " +
           $"--> Cmds: {string.Join(", ", EmoteCommands.IsDefault ? Enumerable.Empty<string>() : EmoteCommands)}\n";

    /// <inheritdoc/>
    public override string ToString()
        => Name;

    /// <inheritdoc/>
    public bool Equals(ParsedEmoteRow other)
        => RowId == other.RowId;

    /// <inheritdoc/>
    public override int GetHashCode()
        => RowId.GetHashCode();
}
