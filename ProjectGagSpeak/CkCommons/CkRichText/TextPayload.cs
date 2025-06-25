using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Raii;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons;

public class TextPayload : RichPayload
{
    private (string LineText, float LineWidth)[] _splitCache = Array.Empty<(string, float)>();
    public string Text { get; }
    public TextPayload(string text)
    {
        Text = text;
    }

    public float GetLastLineWidth() => _splitCache.Length > 0 ? _splitCache[^1].LineWidth : 0f;

    public override void Draw(CkRaii.RichColor c)
    {
        if (IsInline)
            ImGui.SameLine(0, 0);

        // print text normally if there are no splits in the cached text.
        if (_splitCache.Length == 0)
        {
            if (true)
                CkGui.OutlinedFont(Text, 0xFFFFFFFF, 0xFFFF0000, 1);
            else
                ImGui.TextUnformatted(Text);
        }
        else
        {
            if (false)
            {
                foreach (var (line, _) in _splitCache)
                    CkGui.OutlinedFont(line, 0xFFFFFFFF, 0xFFFF0000, 1);
            }
            else
            {
                foreach (var (line, _) in _splitCache)
                    ImGui.TextUnformatted(line);
            }
        }
    }

    public override void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth)
    {
        if(curLineWidth != 0f)
            IsInline = true;

        var words = Text.Split(' ');
        var lines = new List<(string line, float width)>();

        var charIndex = 0;
        var remainingWidth = wrapWidth - curLineWidth;
        var lineStart = 0;

        // for each word.
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            // Get the word width.
            var wordWidth = 0f;
            foreach (var c in word)
                wordWidth += font.GetCharAdvance(c);

            if(i > 0)
                wordWidth += font.GetCharAdvance(' ');

            // If the word doesn't fit, we need to split the line.
            if (wordWidth > remainingWidth)
            {
                if (i == 0)
                {
                    // First word too wide to fit on current line, mark as not inline
                    IsInline = false;
                }
                else
                {
                    // Add current line, exclude trailing space
                    var length = charIndex - lineStart - 1;
                    if (length > 0)
                    {
                        var lineText = Text.Substring(lineStart, length);
                        lines.Add((lineText, curLineWidth));
                    }
                }

                lineStart = charIndex;
                remainingWidth = wrapWidth;
                curLineWidth = 0; // reset to far left.
            }

            // Subtract the word's width from the remaining width (which reset to wrapwidth if split)
            remainingWidth -= wordWidth;
            curLineWidth += wordWidth;

            // add it as a split index.
            charIndex += word.Length;
            // add space char if not the last word
            if (i < words.Length - 1)
                charIndex += 1;
        }
        // Add the last line
        if (lineStart < Text.Length)
        {
            var finalLineText = Text.Substring(lineStart, charIndex - lineStart);
            lines.Add((finalLineText, curLineWidth));
        }

        _splitCache = lines.ToArray();
        curLineWidth = lines.Count > 0 ? lines[^1].width : 0f;

        // Fix: If there was no split, but the line doesn't fit, IsInline must be false
        if (lines.Count == 1 && lines[0].width > wrapWidth)
            IsInline = false;
    }
}

// use the below algoithm if preferring zero-allocation during cache updates. (difference is neglegable though.)
//// Use readonlySpan for zero allocation.
//ReadOnlySpan<char> span = Text.AsSpan();

//// Render each line based on the cached split indices.
//for (int i = 0; i < _newLineSplits.Length; i++)
//{
//    int start = _newLineSplits[i];
//    int end = (i + 1 < _newLineSplits.Length) ? _newLineSplits[i + 1] : Text.Length;
//    // Render the line if it has content.
//    if (end - start > 0)
//        ImUtf8.Text(span.Slice(start, end - start));
//}

// Below is a previous algorithm for index-based caching to avoid substrings.
// However, im pretty sure that doing it above allows for faster rendertime efficiency.

//// Split text into words and cache their widths
//var words = Text.Split(' ');
//// Temporary list for split indices (start of each new line)
//var splits = new List<int> { 0 };

//// Begin processing.
//int charIndex = 0;
//float remainingWidth = wrapWidth - startWidth;

//// for each split word
//for (int i = 0; i < words.Length; i++)
//{
//    var word = words[i];
//    float wordWidth = 0f;
//    foreach (var c in word)
//        wordWidth += font.GetCharAdvance(c);

//    float spaceWidth = (i > 0) ? font.GetCharAdvance(' ') : 0f;
//    float totalWordWidth = wordWidth + spaceWidth;

//    // If the word doesn't fit, start a new line at the space before this word
//    if (i > 0 && totalWordWidth > remainingWidth)
//    {
//        // The split is at the space before this word
//        splits.Add(charIndex - 1); // -1 to point to the space
//        // Reset for new line
//        remainingWidth = wrapWidth;
//    }

//    // Otherwise it fits.
//    remainingWidth -= totalWordWidth;
//    // Move charIndex forward (add space if not last word)
//    charIndex += word.Length;
//    if (i < words.Length - 1)
//        charIndex += 1; // for the space
//}
//// compile the final split
//_newLineSplits = splits.ToArray();

