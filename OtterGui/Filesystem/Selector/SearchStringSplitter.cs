namespace OtterGui.Filesystem.Selector;

public interface ISplitterEntry<TEnum, in TEntry>
{
    string Needle { get; init; }
    TEnum  Type   { get; init; }

    public bool Contains(TEntry other);
}

public abstract class SearchStringSplitter<TEnum, TObj, TEntry>
    where TEnum : unmanaged, Enum where TEntry : ISplitterEntry<TEnum, TEntry>, new()
{
    protected abstract bool ConvertToken(char token, out TEnum val);
    protected abstract bool AllowsNone(TEnum val);
    protected abstract bool Matches(TEntry entry, TObj haystack);
    protected abstract bool MatchesNone(TEnum type, bool negated, TObj haystack);

    protected virtual void PostProcessing()
    {
        // Remove all optional tags that are negated.
        foreach (var item in Negated)
            General.RemoveAll(i => i.Equals(item));

        // Remove all optional tags that are restricted by None.
        foreach (var none in None.Where(none => !none.Item2))
            General.RemoveAll(i => i.Type.Equals(none.Item1));

        foreach (var item in Forced)
        {
            // Remove all optional tags that are forced anyway.
            General.RemoveAll(i => i.Equals(item));

            // If any negated tag is contained in a forced match, we can not have matches.
            if (Negated.Any(i => item.Contains(i)))
            {
                State = FilterState.NoMatches;
                return;
            }

            // If any tag is forced that also has to be not set, we can not have matches.
            if (None.Any(i => !i.Item2 && i.Item1.Equals(item.Type)))
            {
                State = FilterState.NoMatches;
                return;
            }
        }

        // If only a single general tag remains, it is forced.
        if (General.Count == 1)
        {
            Forced.Add(General[0]);
            General.Clear();
        }

        // Check if we have any filters.
        State = General.Count is 0 && Forced.Count is 0 && Negated.Count is 0 && None.Count is 0
            ? FilterState.NoFilters
            : FilterState.Normal;
    }

    protected enum FilterState
    {
        NoFilters = 0,
        Normal    = 1,
        NoMatches = 2,
    }


    protected          FilterState         State   = FilterState.NoFilters;
    protected readonly List<TEntry>        General = [];
    protected readonly List<TEntry>        Forced  = [];
    protected readonly List<TEntry>        Negated = [];
    protected readonly List<(TEnum, bool)> None    = [];

    public bool IsVisible(TObj haystack)
    {
        switch (State)
        {
            case FilterState.NoFilters: return true;
            case FilterState.NoMatches: return false;
        }

        // All None filters have to match.
        if (None.Any(p => !MatchesNone(p.Item1, p.Item2, haystack)))
            return false;

        // All forced entries have to exist.
        if (Forced.Any(forcedEntry => !Matches(forcedEntry, haystack)))
            return false;

        // No negated entry may exist.
        if (Negated.Any(negatedEntry => Matches(negatedEntry, haystack)))
            return false;

        // At least one of the general entries has to exist.
        return General.Count == 0 || General.Any(entry => Matches(entry, haystack));
    }

    public void Parse(ReadOnlySpan<char> input)
    {
        General.Clear();
        Forced.Clear();
        Negated.Clear();
        None.Clear();
        var list          = Forced;
        var currentOffset = 0;
        var start         = 0;
        var count         = -1;
        var typeToken     = default(TEnum);
        while (currentOffset < input.Length)
        {
            switch (input[currentOffset])
            {
                case ' ' or '\t' or '\n':
                    ++currentOffset;
                    break;
                case '?' when ReferenceEquals(list, Forced)
                 && typeToken.Equals(default(TEnum))
                 && currentOffset + 1 < input.Length
                 && !char.IsWhiteSpace(input[currentOffset + 1]):
                    list = General;
                    ++currentOffset;
                    break;
                case '-' when ReferenceEquals(list, Forced)
                 && typeToken.Equals(default(TEnum))
                 && currentOffset + 1 < input.Length
                 && !char.IsWhiteSpace(input[currentOffset + 1]):
                    list = Negated;
                    ++currentOffset;
                    break;
                case '"' when currentOffset + 1 < input.Length:
                    count = input[(currentOffset + 1)..].IndexOf('"');
                    if (count == -1)
                    {
                        start = currentOffset;
                        count = input[start..].IndexOfAny(' ', '\t', '\n');
                        if (count == -1)
                            count = input.Length - start;
                    }
                    else
                    {
                        start         =  currentOffset + 1;
                        currentOffset += 2;
                    }

                    break;
                default:
                    if (typeToken.Equals(default(TEnum))
                     && currentOffset + 1 < input.Length
                     && input[currentOffset + 1] is ':'
                     && ConvertToken(input[currentOffset], out var tok))
                    {
                        typeToken     =  tok;
                        currentOffset += 2;
                    }
                    else
                    {
                        start = currentOffset;
                        count = input[start..].IndexOfAny(' ', '\t', '\n');
                        if (count == -1)
                            count = input.Length - start;
                    }

                    break;
            }

            if (count <= 0)
                continue;

            var spanText = input.Slice(start, count);
            {
                if (AllowsNone(typeToken) && spanText.Equals("none", StringComparison.OrdinalIgnoreCase))
                    None.Add((typeToken, ReferenceEquals(list, Negated)));
                else
                    list.Add(new TEntry
                    {
                        Needle = spanText.ToString().ToLowerInvariant(),
                        Type   = typeToken,
                    });
            }

            currentOffset += count;
            start         =  0;
            count         =  -1;
            list          =  Forced;
            typeToken     =  default;
        }

        PostProcessing();
    }
}
