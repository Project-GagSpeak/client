using ImGuiNET;
using System.Runtime.InteropServices;

namespace GagSpeak.Gui.Utility;
public static partial class CkGuiUtils
{
    private static int FindWrapPosition(string text, float wrapWidth)
    {
        float currentWidth = 0;
        var lastSpacePos = -1;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            currentWidth += ImGui.CalcTextSize(c.ToString()).X;
            if (char.IsWhiteSpace(c))
            {
                lastSpacePos = i;
            }
            if (currentWidth > wrapWidth)
            {
                return lastSpacePos >= 0 ? lastSpacePos : i;
            }
        }
        return -1;
    }

    private static string FormatTextForDisplay(string text, float wrapWidth)
    {
        // Normalize newlines for processing
        text = text.Replace("\r\n", "\n");
        var lines = text.Split('\n').ToList();

        // Traverse each line to check if it exceeds the wrap width
        for (var i = 0; i < lines.Count; i++)
        {
            var lineWidth = ImGui.CalcTextSize(lines[i]).X;

            while (lineWidth > wrapWidth)
            {
                // Find where to break the line
                var wrapPos = FindWrapPosition(lines[i], wrapWidth);
                if (wrapPos >= 0)
                {
                    // Insert a newline at the wrap position
                    var part1 = lines[i].Substring(0, wrapPos);
                    var part2 = lines[i].Substring(wrapPos).TrimStart();
                    lines[i] = part1;
                    lines.Insert(i + 1, part2);
                    lineWidth = ImGui.CalcTextSize(part2).X;
                }
                else
                {
                    break;
                }
            }
        }

        // Join lines with \n for internal representation
        return string.Join("\n", lines);
    }

    private static unsafe int TextEditCallback(ImGuiInputTextCallbackData* data, float wrapWidth)
    {
        var text = Marshal.PtrToStringAnsi((IntPtr)data->Buf, data->BufTextLen);

        // Normalize newlines for processing
        text = text.Replace("\r\n", "\n");
        var lines = text.Split('\n').ToList();

        var textModified = false;

        // Traverse each line to check if it exceeds the wrap width
        for (var i = 0; i < lines.Count; i++)
        {
            var lineWidth = ImGui.CalcTextSize(lines[i]).X;

            // Skip wrapping if this line ends with \r (i.e., it's a true newline)
            if (lines[i].EndsWith("\r"))
            {
                continue;
            }

            while (lineWidth > wrapWidth)
            {
                // Find where to break the line
                var wrapPos = FindWrapPosition(lines[i], wrapWidth);
                if (wrapPos >= 0)
                {
                    // Insert a newline at the wrap position
                    var part1 = lines[i].Substring(0, wrapPos);
                    var part2 = lines[i].Substring(wrapPos).TrimStart();
                    lines[i] = part1;
                    lines.Insert(i + 1, part2);
                    textModified = true;
                    lineWidth = ImGui.CalcTextSize(part2).X;
                }
                else
                {
                    break;
                }
            }
        }

        // Merge lines back to the buffer
        if (textModified)
        {
            var newText = string.Join("\n", lines); // Use \n for internal representation

            var newTextBytes = Encoding.UTF8.GetBytes(newText.PadRight(data->BufSize, '\0'));
            Marshal.Copy(newTextBytes, 0, (IntPtr)data->Buf, newTextBytes.Length);
            data->BufTextLen = newText.Length;
            data->BufDirty = 1;
            data->CursorPos = Math.Min(data->CursorPos, data->BufTextLen);
        }

        return 0;
    }

    public unsafe static bool InputTextWrapMultiline(string id, ref string text, uint maxLength = 500, int lineHeight = 2, float? width = null)
    {
        var wrapWidth = width ?? ImGui.GetContentRegionAvail().X; // Determine wrap width

        // Format text for display
        text = FormatTextForDisplay(text, wrapWidth);

        var result = ImGui.InputTextMultiline(id, ref text, maxLength,
             new(width ?? ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * lineHeight), // Expand height calculation
             ImGuiInputTextFlags.CallbackEdit | ImGuiInputTextFlags.NoHorizontalScroll, // Flag settings
             (data) => { return TextEditCallback(data, wrapWidth); });

        // Restore \r\n for display consistency
        text = text.Replace("\n", "");

        return result;
    }

}
