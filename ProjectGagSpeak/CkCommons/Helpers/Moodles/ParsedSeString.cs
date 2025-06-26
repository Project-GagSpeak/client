using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui;
using ImGuiNET;

namespace GagSpeak.CkCommons.Helpers;

public class ParsedSeString(string rawString, ParsedSeStringText[] parsedTextChunks)
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
        // Use monofont for performance optimizations.
        using var font = ImRaii.PushFont(UiBuilder.MonoFont);
        // Get the width of a single character in the current font.
        float charWidth = ImGui.CalcTextSize("W").X;

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

                // Calculate how many characters fit in the line based on the available width
                int charsPerLine = (int)((maxPosX - cursorPos.X) / charWidth);

                if (remainingText.Length > charsPerLine)
                {
                    // Substring the text to fit within the width, taking care of the word boundary
                    var lineText = remainingText.Substring(0, charsPerLine);
                    var lastSpaceIndex = lineText.LastIndexOf(' ');

                    // If we have a space, split at the space, otherwise, just truncate at charsPerLine
                    if (lastSpaceIndex >= 0)
                    {
                        lineText = remainingText.Substring(0, lastSpaceIndex);
                        remainingText = remainingText.Substring(lastSpaceIndex + 1);
                    }
                    else
                    {
                        remainingText = remainingText.Substring(charsPerLine);
                    }

                    RenderLine(lineText, text, true, ref cursorPos);
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
                    // If the line fits, just render it
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
                    CkGui.OutlinedFont(lineText, text.Foreground, text.Glow, 1);
                    break;

                case ParsedSeStringFlags.ItalicGlow:
                    CkGui.OutlinedFont(lineText, 0xFFFFFFFF, text.Glow, 1);
                    break;

                case ParsedSeStringFlags.ColoredItalic:
                case ParsedSeStringFlags.HasForeground:
                    CkGui.ColorText(lineText, text.Foreground);
                    break;

                case ParsedSeStringFlags.HasGlow:
                    CkGui.OutlinedFont(lineText, 0xFFFFFFFF, text.Glow, 1);
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
