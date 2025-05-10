using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Text;
using System.Runtime.CompilerServices;

namespace GagSpeak.CkCommons.Gui;

// Partial Class for Text Display Helpers.
public partial class CkGui
{
    /// <summary> A helper function to attach a tooltip to a section in the UI currently hovered. </summary>
    public static void AttachToolTip(string text, float borderSize = 1f, Vector4? color = null, bool displayAnyways = false)
    {
        if (text.IsNullOrWhitespace()) return;

        // if the item is currently hovered, with the ImGuiHoveredFlags set to allow when disabled
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) || displayAnyways)
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
    }

    /// <summary> Add a relative tooltip for any area within a given size from the defined position. </summary>
    public static void AddRelativeTooltip(Vector2 pos, Vector2 size, string text)
    {
        // add a scaled dummy over this area.
        if(ImGui.IsMouseHoveringRect(pos, pos + size))
            AttachToolTip(text, displayAnyways: true);
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
