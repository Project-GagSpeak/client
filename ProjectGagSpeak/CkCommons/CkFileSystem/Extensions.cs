using System.Text.RegularExpressions;

namespace GagSpeak.CkCommons.FileSystem;

public static partial class Extensions
{
    /// <summary> Move an item in a list from index 1 to index 2. The indices are clamped to the valid range.
    /// <remarks> Other list entries are shifted accordingly. </remarks>
    public static bool Move<T>(this IList<T> list, int idx1, int idx2)
    {
        idx1 = Math.Clamp(idx1, 0, list.Count - 1);
        idx2 = Math.Clamp(idx2, 0, list.Count - 1);
        if (idx1 == idx2)
            return false;

        var tmp = list[idx1];
        // move element down and shift other elements up
        if (idx1 < idx2)
            for (var i = idx1; i < idx2; i++)
                list[i] = list[i + 1];
        // move element up and shift other elements down
        else
            for (var i = idx1; i > idx2; i--)
                list[i] = list[i - 1];

        list[idx2] = tmp;
        return true;
    }

    /// <summary> Move an item in a list from index 1 to index 2. The indices are clamped to the valid range and returned.
    /// <remarks> Other list entries are shifted accordingly. </remarks>
    public static bool Move<T>(this IList<T> list, ref int idx1, ref int idx2)
    {
        idx1 = Math.Clamp(idx1, 0, list.Count - 1);
        idx2 = Math.Clamp(idx2, 0, list.Count - 1);
        if (idx1 == idx2)
            return false;

        var tmp = list[idx1];
        // move element down and shift other elements up
        if (idx1 < idx2)
            for (var i = idx1; i < idx2; i++)
                list[i] = list[i + 1];
        // move element up and shift other elements down
        else
            for (var i = idx1; i > idx2; i--)
                list[i] = list[i - 1];

        list[idx2] = tmp;
        return true;
    }

    /// <summary> A filesystem name may not contain forward-slashes, as they are used to split paths. </summary>
    /// <param name="name"> The original name given for the file's item. </param>
    /// <returns> The name to be displayed in the file list. </returns>
    /// <remarks> The empty string as name signifies the root, so it can also not be used. </remarks>
    public static string FixName(this string name)
    {
        var fix = name.Replace('/', '\\').Trim();
        return fix.Length == 0 ? "<None>" : fix;
    }

    /// <summary> Split a path string into directories. </summary>
    /// <remarks> Empty entries will be skipped. </remarks>
    public static string[] SplitDirectories(this string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    /// <summary> Return whether a character is invalid in a windows path. </summary>
    public static bool IsInvalidInPath(this char c)
        => Invalid.Contains(c);

    /// <summary> Return whether a character is not valid ASCII. </summary>
    public static bool IsInvalidAscii(this char c)
        => c >= 128;

    /// <summary> Return a string with all symbols invalid in a windows path removed. </summary>
    /// <param name="s"> The string containing invalid symbols. </param>
    /// <returns> The string without any invalid symbols. </returns>
    public static string RemoveInvalidPathSymbols(this string s)
        => string.Concat(s.Split(Path.GetInvalidFileNameChars()));

    /// <summary> Check if a string is a duplicated string with appended number. </summary>
    /// <returns> True if it is, in which case the baseName without " (number)" and the number are returned. </returns>
    /// <remarks> If it is not, baseName is empty and number is 0. </remarks>
    public static bool IsDuplicateName(this string name, out string baseName, out int number)
    {
        var match = DuplicatePathRegex().Match(name);
        if (match.Success)
        {
            baseName = match.Groups["BaseName"].Value;
            number   = int.Parse(match.Groups["Amount"].Value);
            return true;
        }

        baseName = string.Empty;
        number   = 0;
        return false;
    }

    /// <summary> Obtain a unique file name with appended numbering if the file or directory name exists already. </summary>
    /// <returns> An empty string if the given string is empty or if the maximum amount of accepted duplicates is reached. </returns>
    public static string ObtainUniqueFile(this string name, int maxDuplicates = int.MaxValue)
        => ObtainUniqueString(name, s => File.Exists(s) || Directory.Exists(s), maxDuplicates);

    // Obtain a unique string with appended numbering if the name is not unique as determined by the predicate.
    // Returns an empty string if the given string is empty or if the maximum amount of accepted duplicates is reached.
    public static string ObtainUniqueString(this string name, Predicate<string> isDuplicate, int maxDuplicates = int.MaxValue)
    {
        if (name.Length == 0 || !isDuplicate(name))
            return name;

        if (!name.IsDuplicateName(out var baseName, out _))
            baseName = name;

        var idx     = 2;
        var newName = $"{baseName} ({idx})";
        while (isDuplicate(newName))
        {
            newName = $"{baseName} ({++idx})";
            if (idx == maxDuplicates)
                return string.Empty;
        }

        return newName;
    }

    // Increment the duplication part of a given name.
    // If the name does not end in a duplication part, appends (2).
    public static string IncrementDuplicate(this string name)
        => name.IsDuplicateName(out var baseName, out var idx) ? $"{baseName} ({idx + 1})" : $"{name} (2)";

    // Data.
    private static readonly HashSet<char> Invalid            = new(Path.GetInvalidFileNameChars());

    [GeneratedRegex(@"(?<BaseName>.*) \((?<Amount>([2-9]|\d\d+))\)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex DuplicatePathRegex();
}
