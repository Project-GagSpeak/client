using GagSpeak.UI;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Helpers;

public readonly struct ParsedSeString(string rawString, ParsedSeStringText[] parsedTextChunks)
{
    public string               RawString           => rawString;
    public ParsedSeStringText[] ParsedTextChunks    => parsedTextChunks;

    public static ParsedSeString Empty               => new(string.Empty, Array.Empty<ParsedSeStringText>());

    public override string ToString()
        => "Size: " + parsedTextChunks.Length + ", Parsed Chunks:\n" + string.Join("\n", parsedTextChunks);

    /// <summary> Helper function to render a parsed SeString to ImGui Display.
    /// <para> Supports textwrapping, but you need to draw it within a child window or other framed content. </para>
    /// <remarks> the width is defined by the display width, and begins at the ScreenCursorPos the function is called at. </remarks>
    public void RenderText(float displayWidth, int maxLines = 1)
    {
        // Track the current line, line text, and remaining width left for the current line.
        var currentLine = 0;
        var cursorPos = ImGui.GetCursorScreenPos();
        var maxPosX = cursorPos.X + displayWidth;
        var minPosX = cursorPos.X;

        foreach (var text in parsedTextChunks)
        {
            var remainingText = text.Text;
            ImGui.SetCursorScreenPos(cursorPos);
            while (!string.IsNullOrEmpty(remainingText))
            {
                var newlineIndex = remainingText.IndexOf('\n');
                if (newlineIndex >= 0)
                {
                    // Handle the newline by breaking the text and updating cursor position
                    var lineBeforeNewline = remainingText.Substring(0, newlineIndex);
                    RenderLine(lineBeforeNewline, text, true, ref cursorPos);

                    // Update the remaining text after the newline
                    remainingText = remainingText.Substring(newlineIndex + 1);

                    // Move the cursor to the next line
                    cursorPos = cursorPos with { X = minPosX };
                    ImGui.SetCursorScreenPos(cursorPos);
                    continue; // Skip the rest of the loop and process the next portion of text
                }

                var measuredWidth = ImGui.CalcTextSize(remainingText).X;

                // Compare based on cursor position + width instead of just width
                if (cursorPos.X + measuredWidth > maxPosX)
                {
                    // pass the remaining text into an array of words.
                    var words = remainingText.Split(' ');
                    var lineText = string.Empty;
                    int splitIndex = 0;

                    foreach (var word in words)
                    {
                        var newLineText = string.IsNullOrEmpty(lineText) ? word : lineText + " " + word;
                        var lineWidth = ImGui.CalcTextSize(newLineText).X;

                        // If adding this word exceeds the maximum width, stop adding more words
                        if (cursorPos.X + lineWidth > maxPosX)
                            break;

                        // Otherwise, add the word to the line text and increment the splitIndex
                        lineText = newLineText;
                        splitIndex++;
                    }

                    RenderLine(lineText, text, true, ref cursorPos);
                    remainingText = string.Join(" ", words.Skip(splitIndex)).TrimStart();

                    // Move to next line
                    currentLine++;

                    // Handle condition where we exceed max count.
                    if (currentLine >= maxLines)
                    {
                        RenderLine("...", text, true, ref cursorPos);
                        return;
                    }
                }
                else
                {
                    RenderLine(remainingText, text, false, ref cursorPos);
                    remainingText = string.Empty;
                }
            }
            cursorPos = ImGui.GetCursorScreenPos();
        }
        ImGui.SetCursorScreenPos(cursorPos + new Vector2(0, ImGui.GetTextLineHeightWithSpacing()));
        return;

        // internal helper function to render a single line of text.
        void RenderLine(string lineText, ParsedSeStringText text, bool renderingFullLine, ref Vector2 cursorPos)
        {
            // Render the current line based on flags
            switch (text.Flags)
            {
                case ParsedSeStringFlags.All: // we don't support Italics yet so treat these the same.
                case ParsedSeStringFlags.ColoredGlow:
                    CkGui.DrawOutlinedFont(lineText, text.Foreground, text.Glow, 1);
                    break;

                case ParsedSeStringFlags.ItalicGlow:
                    CkGui.DrawOutlinedFont(lineText, 0xFFFFFFFF, text.Glow, 1);
                    break;

                case ParsedSeStringFlags.ColoredItalic:
                case ParsedSeStringFlags.HasForeground:
                    CkGui.ColorText(lineText, text.Foreground);
                    break;

                case ParsedSeStringFlags.HasGlow:
                    CkGui.DrawOutlinedFont(lineText, 0xFFFFFFFF, text.Glow, 1);
                    break;

                case ParsedSeStringFlags.HasItalic:
                    ImGui.TextUnformatted(lineText);
                    break;

                default:
                    ImGui.TextUnformatted(lineText);
                    break;
            }
            // Reset position for continuous rendering
            if (!renderingFullLine)
                ImGui.SameLine(0,1);
            cursorPos = ImGui.GetCursorScreenPos();
        }
    }
}
