using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons;

public class TextPayload : RichPayload
{
    private (string LineText, float LineWidth)[] _splitCache = Array.Empty<(string, float)>();
    private string _text     = string.Empty;
    private bool   _isInline = false;
    public TextPayload(string text)
    {
        _text = text;
    }

    public string RawText => _text;

    public void Draw(uint? strokeColor)
    {
        if (_isInline)
            ImGui.SameLine(0, 0);

        // print text normally if there are no splits in the cached text.
        if (_splitCache.Length == 0)
        {
            if (strokeColor is not null)
                TextOutlined(_text, strokeColor.Value);
            else
                ImGui.TextUnformatted(_text);
        }
        else
        {
            if (strokeColor is not null)
            {
                foreach (var (line, _) in _splitCache)
                    TextOutlined(line, strokeColor.Value);
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
        if (curLineWidth != 0f)
            _isInline = true;

        var words = _text.Split(' ');
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

            if (i > 0)
                wordWidth += font.GetCharAdvance(' ');

            // If the word doesn't fit, we need to split the line.
            if (wordWidth > remainingWidth)
            {
                if (i == 0)
                {
                    // First word too wide to fit on current line, mark as not inline
                    _isInline = false;
                }
                else
                {
                    // Add current line, exclude trailing space
                    var length = charIndex - lineStart - 1;
                    if (length > 0)
                    {
                        var lineText = _text.Substring(lineStart, length);
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
        if (lineStart < _text.Length)
        {
            var finalLineText = _text.Substring(lineStart, charIndex - lineStart);
            lines.Add((finalLineText, curLineWidth));
        }

        _splitCache = lines.ToArray();
        curLineWidth = lines.Count > 0 ? lines[^1].width : 0f;
    }

    private static void TextOutlined(string text, uint strokeColor)
    {
        var original = ImGui.GetCursorPos();
        using (ImRaii.PushColor(ImGuiCol.Text, strokeColor))
        {
            ImGui.SetCursorPos(original with { Y = original.Y-- });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X-- });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { Y = original.Y++ });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X++ });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X--, Y = original.Y-- });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X++, Y = original.Y++ });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X--, Y = original.Y++ });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X++, Y = original.Y-- });
            ImGui.TextUnformatted(text);
        }

        ImGui.SetCursorPos(original);
        ImGui.TextUnformatted(text);
    }
}
