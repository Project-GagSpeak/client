using GagSpeak.Services.Mediator;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Profile;

/// <summary>
/// Helper Functions for Drawing out the components.
/// </summary>
public partial class KinkPlateUI : WindowMediatorSubscriberBase
{
    public static void DrawLimitedDescription(string desc, Vector4 color, Vector2 size)
    {
        // Calculate the line height and determine the max lines based on available height
        float lineHeight = ImGui.CalcTextSize("A").Y;
        int maxLines = (int)(size.Y / lineHeight);
        var startX = ImGui.GetCursorScreenPos().X;
        double currentLines = 1;
        float lineWidth = size.X; // Max width for each line
        string[] words = desc.Split(' '); // Split text by words
        string newDescText = "";
        string currentLine = "";

        foreach (var word in words)
        {
            // Try adding the current word to the line
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            float testLineWidth = ImGui.CalcTextSize(testLine).X;

            // if a word contains newlines, count how many, append, and add to current lines.
            if (word.Contains("\n\n"))
            {
                // get the text both before and after it.
                var split = word.Split("\n\n");
                currentLine += split[0];
                CkGui.ColorText(currentLine, color);
                ImGui.SetCursorScreenPos(new Vector2(startX, ImGui.GetCursorScreenPos().Y + 5f));
                currentLine = split[1];
                currentLines += 1.5;
                continue;
            }

            if (testLineWidth > lineWidth)
            {
                // Current word exceeds line width; finalize the current line
                currentLine += "\n";
                CkGui.ColorText(currentLine, color);
                ImGui.SetCursorScreenPos(new Vector2(startX, ImGui.GetCursorScreenPos().Y));
                currentLine = word;
                currentLines++;

                // Check if maxLines is reached and break if so
                if (currentLines >= maxLines)
                    break;
            }
            else
            {
                // Word fits in the current line; accumulate it
                currentLine = testLine;
            }
        }

        // Add any remaining text if we haven’t hit max lines
        if (currentLines < maxLines && !string.IsNullOrEmpty(currentLine))
        {
            newDescText += currentLine;
            currentLines++; // Increment the line count for the final line
        }
        CkGui.ColorTextWrapped(newDescText.TrimEnd(), color);
    }
}
