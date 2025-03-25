using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI;

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
            AttachToolTip(text);
    }

    public static void HelpText(string helpText, bool inner = false)
    {
        if (inner) { ImUtf8.SameLineInner(); }
        else { ImGui.SameLine(); }
        var hovering = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));
        IconText(FAI.QuestionCircle, hovering ? ImGui.GetColorU32(ImGuiColors.TankBlue) : ImGui.GetColorU32(ImGuiCol.TextDisabled));
        AttachToolTip(helpText);
    }

    /// <summary> An Unformatted Text version of ImGui.TextColored accepting UINT </summary>
    public static void ColorText(string text, uint color)
    {
        using var raiicolor = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }


    /// <summary> An Unformatted Text version of ImGui.TextColored </summary>
    public static void ColorText(string text, Vector4 color)
    {
        using var raiicolor = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    /// <summary> Displays colored text based on the boolean value of true or false. </summary>
    public static void ColorTextBool(string text, bool value)
    {
        var color = value ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed;
        ColorText(text, color);
    }

    /// <summary> Displays colored text based on the boolean value of true or false. </summary>
    /// <remarks> Can provide custom colors if desired. </remarks>
    public static void ColorTextBool(string text, bool value, Vector4 colorTrue = default, Vector4 colorFalse = default)
    {
        var color = value
            ? (colorTrue == default) ? ImGuiColors.HealerGreen : colorTrue
            : (colorFalse == default) ? ImGuiColors.DalamudRed : colorFalse;

        ColorText(text, color);
    }

    public static void ColorTextCentered(string text, Vector4 color)
    {
        var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ColorText(text, color);
    }

    /// <summary> What it says on the tin. </summary>
    public static void ColorTextWrapped(string text, Vector4 color)
    {
        using var raiicolor = ImRaii.PushColor(ImGuiCol.Text, color);
        TextWrapped(text);
    }

    /// <summary> Helper function to draw the outlined font in ImGui. </summary>
    public static void OutlinedFont(string text, Vector4 fontColor, Vector4 outlineColor, int thickness)
    {
        var original = ImGui.GetCursorPos();

        using (ImRaii.PushColor(ImGuiCol.Text, outlineColor))
        {
            ImGui.SetCursorPos(original with { Y = original.Y - thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X - thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { Y = original.Y + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X - thickness, Y = original.Y - thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X + thickness, Y = original.Y + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X - thickness, Y = original.Y + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X + thickness, Y = original.Y - thickness });
            ImGui.TextUnformatted(text);
        }

        using (ImRaii.PushColor(ImGuiCol.Text, fontColor))
        {
            ImGui.SetCursorPos(original);
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original);
            ImGui.TextUnformatted(text);
        }
    }

    public static void OutlinedFont(string text, uint fontColor, uint outlineColor, int thickness)
    {
        var original = ImGui.GetCursorPos();

        using (ImRaii.PushColor(ImGuiCol.Text, outlineColor))
        {
            ImGui.SetCursorPos(original with { Y = original.Y - thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X - thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { Y = original.Y + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X - thickness, Y = original.Y - thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X + thickness, Y = original.Y + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X - thickness, Y = original.Y + thickness });
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original with { X = original.X + thickness, Y = original.Y - thickness });
            ImGui.TextUnformatted(text);
        }

        using (ImRaii.PushColor(ImGuiCol.Text, fontColor))
        {
            ImGui.SetCursorPos(original);
            ImGui.TextUnformatted(text);
            ImGui.SetCursorPos(original);
            ImGui.TextUnformatted(text);
        }
    }


    public static void OutlinedFont(ImDrawListPtr drawList, string text, Vector2 textPos, uint fontColor, uint outlineColor, int thickness)
    {
        drawList.AddText(textPos with { Y = textPos.Y - thickness },
            outlineColor, text);
        drawList.AddText(textPos with { X = textPos.X - thickness },
            outlineColor, text);
        drawList.AddText(textPos with { Y = textPos.Y + thickness },
            outlineColor, text);
        drawList.AddText(textPos with { X = textPos.X + thickness },
            outlineColor, text);
        drawList.AddText(new Vector2(textPos.X - thickness, textPos.Y - thickness),
            outlineColor, text);
        drawList.AddText(new Vector2(textPos.X + thickness, textPos.Y + thickness),
            outlineColor, text);
        drawList.AddText(new Vector2(textPos.X - thickness, textPos.Y + thickness),
            outlineColor, text);
        drawList.AddText(new Vector2(textPos.X + thickness, textPos.Y - thickness),
            outlineColor, text);

        drawList.AddText(textPos, fontColor, text);
        drawList.AddText(textPos, fontColor, text);
    }

    public static Vector2 CalcFontTextSize(string text, IFontHandle fontHandle = null!)
    {
        if (fontHandle is null)
            return ImGui.CalcTextSize(text);

        using (fontHandle.Push())
            return ImGui.CalcTextSize(text);
    }

    public static void CopyableDisplayText(string text, string tooltip = "Click to copy")
    {
        // then when the item is clicked, copy it to clipboard so we can share with others
        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(text);
        }
        AttachToolTip(tooltip);
    }

    public static void TextWrapped(string text)
    {
        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    public static void GagspeakText(string text, Vector4? color = null)
        => FontText(text, UiFontService.GagspeakFont, color);

    public static void GagspeakBigText(string text, Vector4? color = null)
        => FontText(text, UiFontService.GagspeakLabelFont, color);

    public static void GagspeakTitleText(string text, Vector4? color = null)
        => FontText(text, UiFontService.GagspeakTitleFont, color);

    public static void BigText(string text, Vector4? color = null)
        => FontText(text, UiFontService.UidFont, color);

    /// <summary> Draws iconText centered within ImGui.GetFrameHeight() square. </summary>
    public static void FramedIconText(FAI icon, Vector4 color)
        => FramedIconText(icon, CkGui.Color(color));

    /// <summary> Draws iconText centered within ImGui.GetFrameHeight() square. </summary>
    public static void FramedIconText(FAI icon, uint? color = null)
    {
        using var font = UiFontService.IconFont.Push();
        // Get the text size.
        var text = icon.ToIconString();
        var vector = ImGui.CalcTextSize(text);
        // Get current pos.
        var pos = ImGui.GetCursorScreenPos();
        // Get the frame height.
        var frameHeight = ImGui.GetFrameHeight();
        var region = new Vector2(frameHeight);
        // move the pos so that it is centered within the region
        var centerPos = new Vector2(pos.X + ImGui.GetStyle().FramePadding.X, pos.Y + (frameHeight / 2f - (vector.Y / 2f)));
        // Draw a dummy to fill the frame region.
        ImGui.Dummy(region);
        // Then draw the text in the center.
        ImGui.GetWindowDrawList().AddText(centerPos, color ?? ImGui.GetColorU32(ImGuiCol.Text), text);
    }


    public static void IconText(FAI icon, uint color)
    {
        FontText(icon.ToIconString(), UiFontService.IconFont, color);
    }

    public static void IconText(FAI icon, Vector4? color = null)
    {
        IconText(icon, color == null ? ImGui.GetColorU32(ImGuiCol.Text) : ImGui.GetColorU32(color.Value));
    }

    private static void FontText(string text, IFontHandle font, Vector4? color = null)
    {
        FontText(text, font, color == null ? ImGui.GetColorU32(ImGuiCol.Text) : ImGui.GetColorU32(color.Value));
    }

    private static void FontText(string text, IFontHandle font, uint color)
    {
        using var pushedFont = font.Push();
        using var pushedColor = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    // Helper function to draw an input text for a set width, with an icon drawn right aligned.
    public static void InputTextRightIcon(string label, float width, string hint, ref string input, uint length, FAI icon)
    {
        // Draw input text with hint below.
        ImGui.SetNextItemWidth(width);
        ImGui.InputTextWithHint(label, hint, ref input, length);
        // Get the itemrect
        var itemRect = ImGui.GetItemRectSize();
        // run a sameline from the position to set the cursorPosX to the end for us to draw the right aligned icon.
        ImGui.SameLine(ImGui.GetCursorPosX() + itemRect.X - ImGui.GetTextLineHeight());
        IconText(icon, ImGui.GetColorU32(ImGuiCol.Text));
    }
}
