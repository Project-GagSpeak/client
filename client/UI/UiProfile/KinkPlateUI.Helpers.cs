using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using ImGuiNET;
using System.Numerics;

namespace GagSpeak.UI.Profile;

/// <summary>
/// Helper Functions for Drawing out the components.
/// </summary>
public partial class KinkPlateUI : WindowMediatorSubscriberBase
{
    public static void AddImageRounded(ImDrawListPtr drawList, IDalamudTextureWrap? wrap, Vector2 topLeftPos, Vector2 size, float rounding, bool tt = false, string ttText = "")
    {
        try
        {
            if (wrap is { } validWrap)
            {
                ImGui.GetWindowDrawList().AddImageRounded(
                    validWrap.ImGuiHandle,
                    topLeftPos,
                    topLeftPos + size,
                    Vector2.Zero,
                    Vector2.One,
                    ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)),
                    rounding);
                if(tt)
                {
                    AddRelativeTooltip(topLeftPos, size, ttText);
                }
            }
        }
        catch (Exception ex) { StaticLogger.Logger.LogError($"Error: {ex}"); }
    }

    public static void AddImage(ImDrawListPtr drawList, IDalamudTextureWrap? wrap, Vector2 pos, Vector2 size, Vector4? tint = null, bool tt = false, string ttText = "")
    {
        try
        {
            if (wrap is { } validWrap)
            {
                // handle tint.
                var actualTint = tint ?? new Vector4(1f, 1f, 1f, 1f);
                // handle image.
                drawList.AddImage(validWrap.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One, ImGui.GetColorU32(actualTint));
                if(tt)
                {
                    AddRelativeTooltip(pos, size, ttText);
                }
            }
        }
        catch (Exception ex) { StaticLogger.Logger.LogError($"Error: {ex}"); }
    }

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
                UiSharedService.ColorText(currentLine, color);
                ImGui.SetCursorScreenPos(new Vector2(startX, ImGui.GetCursorScreenPos().Y + 5f));
                currentLine = split[1];
                currentLines += 1.5;
                continue;
            }

            if (testLineWidth > lineWidth)
            {
                // Current word exceeds line width; finalize the current line
                currentLine += "\n";
                UiSharedService.ColorText(currentLine, color);
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
        UiSharedService.ColorTextWrapped(newDescText.TrimEnd(), color);
    }

    public static void AddRelativeTooltip(Vector2 pos, Vector2 size, string text)
    {
        // add a scaled dummy over this area.
        ImGui.SetCursorScreenPos(pos);
        ImGuiHelpers.ScaledDummy(size);
        UiSharedService.AttachToolTip(text);
    }
}
