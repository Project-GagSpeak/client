using Dalamud.Game.Text.SeStringHandling;
using System.Text.RegularExpressions;

namespace GagSpeak.CkCommons.Helpers;

public static partial class RegexEx
{
    public static Match TryMatchTriggerWord(string message, string triggerWord)
    {
        var triggerRegex = $@"(?<=^|\s){triggerWord}(?=[^a-z])";
        return Regex.Match(message, triggerRegex);
    }

    public static string EnsureUniqueName<T>(string baseName, IEnumerable<T> collection, Func<T, string> nameSelector)
    {
        // Regex to match the base name and the (X) suffix if it exists
        var suffixPattern = @"^(.*?)(?: \((\d+)\))?$";
        var match = Regex.Match(baseName, suffixPattern);

        var namePart = match.Groups[1].Value; // The base part of the name
        var currentNumber = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;

        // Increment current number for the new copy
        currentNumber = Math.Max(1, currentNumber);

        var newName = baseName;

        // Ensure the name is unique by appending (X) and incrementing if necessary
        while (collection.Any(item => nameSelector(item) == newName))
            newName = $"{namePart} ({currentNumber++})";

        return newName;
    }

    public static string StripColorTags(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Define a regex pattern to match any [color=...] and [/color] tags
        string pattern = @"\[\/?(color|glow)(=[^\]]*)?\]";

        // Use Regex.Replace to remove the tags
        string result = Regex.Replace(input, pattern, string.Empty);

        return result;
    }

    /// <summary> encapsulates the puppeteer command within '(' and ')' </summary>
    public static SeString GetSubstringWithinParentheses(this SeString str, char startBracket = '(', char EndBracket = ')')
    {
        int startIndex = str.TextValue.IndexOf(startBracket);
        int endIndex = str.TextValue.IndexOf(EndBracket);

        // If both brackets are found and the end bracket is after the start bracket
        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
            return str.TextValue.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

        return str;
    }

    /// <summary> Converts square brackets to angle brackets </summary>
    public static SeString ConvertSquareToAngleBrackets(this SeString str)
    {
        try
        {
            return str.TextValue.Replace("[", "<").Replace("]", ">");
        }
        catch (Exception)
        {
            return str;
        }
    }

    [GeneratedRegex(@"(\[color=[0-9a-zA-Z]+\])|(\[\/color\])|(\[glow=[0-9a-zA-Z]+\])|(\[\/glow\])|(\[i\])|(\[\/i\])", RegexOptions.IgnoreCase, "en-US")]
    public static partial Regex SplitRegex();
}

