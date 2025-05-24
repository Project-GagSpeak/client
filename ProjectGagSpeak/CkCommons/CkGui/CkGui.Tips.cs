using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui;

// Partial Class for Text Display Helpers.
public partial class CkGui
{
    /// <summary> A helper function to attach a tooltip to a section in the UI currently hovered. </summary>
    /// <remarks> If the string is null, empty, or whitespace, will do early return at no performance impact. </remarks>
    public static void AttachToolTip(string? text, float borderSize = 1f, Vector4? color = null)
    {
        if (text.IsNullOrWhitespace())
            return;

        // if the item is currently hovered, with the ImGuiHoveredFlags set to allow when disabled
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ToolTipInternal(text, borderSize, color);
    }

    /// <summary> A helper function to attach a tooltip to a section in the UI currently hovered. </summary>
    /// <remarks> If the string is null, empty, or whitespace, will do early return at no performance impact. </remarks>
    public static void AttachToolTipRect(Vector2 min, Vector2 max, string? text, float borderSize = 1f, Vector4? color = null)
    {
        if (text.IsNullOrWhitespace())
            return;

        // if the item is currently hovered, with the ImGuiHoveredFlags set to allow when disabled
        if (ImGui.IsMouseHoveringRect(min, max))
            ToolTipInternal(text, borderSize, color);
    }

    private static void ToolTipInternal(string text, float borderSize = 1f, Vector4? color = null)
    {
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
        using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, borderSize);
        using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);
        // begin the tooltip interface
        ImGui.BeginTooltip();
        // push the text wrap position to the font size times 35
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        // we will then check to see if the text contains a tooltip
        if (text.Contains(TooltipSeparator, StringComparison.Ordinal) || text.Contains(ColorToggleSeparator, StringComparison.Ordinal))
        {
            // if it does, we will split the text by the tooltip
            var splitText = text.Split(TooltipSeparator, StringSplitOptions.None);
            // for each of the split text, we will display the text unformatted
            for (var i = 0; i < splitText.Length; i++)
            {
                if (splitText[i].Contains(ColorToggleSeparator, StringComparison.Ordinal) && color.HasValue)
                {
                    var colorSplitText = splitText[i].Split(ColorToggleSeparator, StringSplitOptions.None);
                    var useColor = false;

                    for (var j = 0; j < colorSplitText.Length; j++)
                    {
                        if (useColor)
                        {
                            ImGui.SameLine(0, 0); // Prevent new line
                            ImGui.TextColored(color.Value, colorSplitText[j]);
                        }
                        else
                        {
                            if (j > 0) ImGui.SameLine(0, 0); // Prevent new line
                            ImGui.TextUnformatted(colorSplitText[j]);
                        }
                        // Toggle the color for the next segment
                        useColor = !useColor;
                    }
                }
                else
                {
                    ImGui.TextUnformatted(splitText[i]);
                }
                if (i != splitText.Length - 1) ImGui.Separator();
            }
        }
        // otherwise, if it contains no tooltip, then we will display the text unformatted
        else
        {
            ImGui.TextUnformatted(text);
        }
        // finally, pop the text wrap position and end the tooltip
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    public static void HelpText(string helpText, bool inner = false, uint? offColor = null)
    {
        if (inner)
            ImUtf8.SameLineInner();
        else
            ImGui.SameLine();

        var hovering = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));
        FramedIconText(FAI.QuestionCircle, hovering ? ImGui.GetColorU32(ImGuiColors.TankBlue) : offColor ?? ImGui.GetColorU32(ImGuiCol.TextDisabled));
        AttachToolTip(helpText);
    }
}
