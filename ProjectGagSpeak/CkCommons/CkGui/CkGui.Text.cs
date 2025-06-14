using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using System.Runtime.CompilerServices;

namespace GagSpeak.CkCommons.Gui;

// Partial Class for Text Display Helpers.
public partial class CkGui
{
    /// <seealso cref="ImUtf8.TextRightAligned(ReadOnlySpan{byte}, float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RightAligned(string text, float offset = 0)
    {
        offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted(text);
    }

    public static void RightAlignedColor(string text, uint color, float offset = 0)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        RightAligned(text, offset);
    }

    public static void RightAlignedColor(string text, Vector4 color, float offset = 0)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        RightAligned(text, offset);
    }

    /// <seealso cref="ImUtf8.TextRightAligned(ReadOnlySpan{byte}, float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RightFrameAligned(string text, float offset = 0)
    {
        offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImUtf8.TextFrameAligned(text);
    }

    public static void RightFrameAlignedColor(string text, uint color, float offset = 0)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        RightFrameAligned(text, offset);
    }

    public static void RightFrameAlignedColor(string text, Vector4 color, float offset = 0)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        RightFrameAligned(text, offset);
    }

    /// <summary> An Unformatted Text version of ImGui.TextColored accepting UINT </summary>
    public static void TextInline(string text, bool inner = true)
    {
        if (inner) ImUtf8.SameLineInner();
        else ImGui.SameLine();

        ImGui.TextUnformatted(text);
    }


    public static void TextFrameAlignedInline(string text, bool inner = true)
    {
        if (inner) 
            ImUtf8.SameLineInner();
        else 
            ImGui.SameLine();

        ImUtf8.TextFrameAligned(text);
    }

    /// <summary> An Unformatted Text version of ImGui.TextColored accepting UINT </summary>
    public static void ColorText(string text, uint color)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    /// <summary> An Unformatted Text version of ImGui.TextColored accepting UINT </summary>
    public static void ColorTextInline(string text, uint color, bool inner = true)
    {
        if (inner) ImUtf8.SameLineInner();
        else ImGui.SameLine();

        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    /// <summary> An Frame-Aligned Text version of ImGui.TextColored accepting UINT </summary>
    public static void ColorTextFrameAligned(string text, uint color)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImUtf8.TextFrameAligned(text);
    }

    /// <summary> An Frame-Aligned Text version of ImGui.TextColored accepting UINT </summary>
    public static void ColorTextFrameAlignedInline(string text, uint color, bool inner = true)
    {
        if (inner) ImUtf8.SameLineInner();
        else ImGui.SameLine();

        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImUtf8.TextFrameAligned(text);
    }

    /// <summary> An Unformatted Text version of ImGui.TextColored </summary>
    public static void ColorText(string text, Vector4 color)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    /// <summary> An Unformatted Text version of ImGui.TextColored </summary>
    public static void ColorTextInline(string text, Vector4 color, bool inner = true)
    {
        if (inner) ImUtf8.SameLineInner();
        else ImGui.SameLine();

        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    /// <summary> An Frame-Aligned Text version of ImGui.TextColored accepting UINT </summary>
    public static void ColorTextFrameAligned(string text, Vector4 color)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImUtf8.TextFrameAligned(text);
    }

    /// <summary> An Frame-Aligned Text version of ImGui.TextColored accepting UINT </summary>
    public static void ColorTextFrameAlignedInline(string text, Vector4 color, bool inner = true)
    {
        if (inner) ImUtf8.SameLineInner();
        else ImGui.SameLine();

        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImUtf8.TextFrameAligned(text);
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

    public static void CenterTextAligned(string text)
    {
        var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImUtf8.TextFrameAligned(text);
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
        var region = new Vector2(ImGui.GetFrameHeight());

        using var font = UiFontService.IconFont.Push();
        // Get the text size.
        var text = icon.ToIconString();
        var iconSize = ImGui.CalcTextSize(text);
        var currentPos = ImGui.GetCursorScreenPos();
        var iconPosition = currentPos + (region - iconSize) * 0.5f;
        // Draw a dummy to fill the frame region.
        ImGui.Dummy(region);
        ImGui.GetWindowDrawList().AddText(iconPosition, color ?? ImGui.GetColorU32(ImGuiCol.Text), text);
    }

    public static void IconText(FAI icon, uint color)
    {
        FontText(icon.ToIconString(), UiFontService.IconFont, color);
    }

    public static void IconText(FAI icon, Vector4? color = null)
    {
        IconText(icon, color == null ? ImGui.GetColorU32(ImGuiCol.Text) : ImGui.GetColorU32(color.Value));
    }

    public static void FontText(string text, IFontHandle font, Vector4? color = null)
    {
        FontText(text, font, color == null ? ImGui.GetColorU32(ImGuiCol.Text) : ImGui.GetColorU32(color.Value));
    }

    public static void FontText(string text, IFontHandle font, uint color)
    {
        using var pushedFont = font.Push();
        using var pushedColor = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    public static void FontTextCentered(string text, IFontHandle font, Vector4? color = null)
    {
        FontTextCentered(text, font, color == null ? ImGui.GetColorU32(ImGuiCol.Text) : ImGui.GetColorU32(color.Value));
    }

    public static void FontTextCentered(string text, IFontHandle font, uint color)
    {
        using var pushedFont = font.Push();
        using var pushedColor = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGuiUtil.Center(text);
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
