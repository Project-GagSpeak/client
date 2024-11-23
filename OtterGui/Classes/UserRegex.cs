using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Classes;

/// <summary> A wrapper to combine a textual user input and a compiled regex. </summary>
public struct UserRegex
{
    /// <summary> Empty filter. </summary>
    public static readonly UserRegex Empty = new();

    /// <summary> Default options optimized for efficiency. Compiled is always set. </summary>
    public const RegexOptions DefaultOptions =
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.NonBacktracking;

    /// <summary> The user-supplied text. </summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary> The compiled regex, if text is valid regex. </summary>
    public Regex? Regex { get; private set; }

    /// <summary> Empty filter. </summary>
    public UserRegex()
    {}

    /// <summary> Create a filter from text. </summary>
    /// <param name="text"> The input text. </param>
    /// <param name="options"> The regex-compile options. Compiled is always set. </param>
    public UserRegex(string text, RegexOptions options = DefaultOptions)
        => UpdateText(text, options);

    /// <summary> Create a filter from an existing regex. </summary>
    public UserRegex(Regex? regex)
        => UpdateText(regex);

    /// <summary> Update the filter via text. </summary>
    public void UpdateText(string text, RegexOptions options = DefaultOptions)
    {
        Text  = text;
        Regex = BuildRegex(text, options);
    }

    /// <summary> Update the filter via regex. </summary>
    public void UpdateText(Regex? regex)
    {
        Text  = regex?.ToString() ?? string.Empty;
        Regex = regex;
    }

    /// <summary> If the filter matches the given text. Empty filters match everything. </summary>
    public readonly bool Match(string text)
        => Text.Length == 0 || (Regex?.IsMatch(text) ?? text.Contains(Text, StringComparison.OrdinalIgnoreCase));

    private static Regex? BuildRegex(string text, RegexOptions options)
    {
        if (text.Length == 0)
            return null;

        try
        {
            return new Regex(text, options | RegexOptions.Compiled);
        }
        catch
        {
            return null;
        }
    }

    /// <summary> Draw an input field for a user regex. </summary>
    /// <param name="label"> The ImGui label used. </param>
    /// <param name="regex"> The UserRegex on which to operate. </param>
    /// <param name="hint"> Optional Hint text for an empty input field. </param>
    /// <param name="tooltip"> Optional action that draws a tooltip. </param>
    /// <param name="width"> Optional width for the input field. </param>
    /// <param name="failureColor"> Optional failure border color around the input for invalid regex strings. </param>
    /// <param name="options"> The regex options to compile the regex with. </param>
    /// <returns> Whether <paramref name="regex"/> was changed. </returns>
    public static bool DrawRegexInput(string label, ref UserRegex regex, string hint = "", Action? tooltip = null, float width = 0, uint failureColor = 0,
        RegexOptions options = DefaultOptions)
    {
        bool change;
        var  text       = regex.Text;
        var  drawBorder = failureColor != 0 && text.Length > 0 && regex.Regex == null;
        using (var style = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, ImGuiHelpers.GlobalScale, drawBorder))
        {
            using var color  = ImRaii.PushColor(ImGuiCol.Border, failureColor, drawBorder);

            if (width != 0)
                ImGui.SetNextItemWidth(width);

            change = ImGui.InputTextWithHint(label, hint, ref text, 256) && text != regex.Text;
        }

        if (tooltip != null && ImGui.IsItemHovered())
            tooltip();

        if (change)
            regex.UpdateText(text, options);

        return change;
    }
}
