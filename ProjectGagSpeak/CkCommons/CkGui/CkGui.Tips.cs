using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GagSpeak.Gui;

// Partial Class for Text Display Helpers.
public static partial class CkGui
{
    public const string TipSep = "--SEP--";
    public const string TipNL = "--NL--";
    public const string TipCol = "--COL--";
    private static readonly Regex TooltipTokenRegex = new($"({TipSep}|{TipNL}|{TipCol})", RegexOptions.Compiled | RegexOptions.CultureInvariant);
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
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f)
            .Push(ImGuiStyleVar.WindowRounding, 4f)
            .Push(ImGuiStyleVar.PopupBorderSize, borderSize);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);

        // Split the text by regex.
        var tokens = TooltipTokenRegex.Split(text);

        // if there were no tokens, just print the text unformatted
        if (tokens.Length <= 1)
        {
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            return;
        }

        // Otherwise, parse it!
        var useColor = false;
        bool firstLineSegment = true;

        foreach (var token in tokens)
        {
            switch(token)
            {
                case TipSep:
                    ImGui.Separator();
                    break;

                case TipNL:
                    ImGui.NewLine();
                    break;

                case TipCol:
                    useColor = !useColor;
                    break;

                default:
                    if (string.IsNullOrEmpty(token))
                        continue; // Skip empty tokens

                    if (!firstLineSegment)
                        ImGui.SameLine(0, 0);

                    if (useColor && color.HasValue)
                        ColorText(token, color.Value);
                    else
                        ImGui.TextUnformatted(token);

                    firstLineSegment = false;
                    break;
            }
        }

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

    public static void HelpText(string helpText, Vector4 tooltipCol, bool inner = false, uint? offColor = null)
    {
        if (inner)
            ImUtf8.SameLineInner();
        else
            ImGui.SameLine();

        var hovering = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));
        FramedIconText(FAI.QuestionCircle, hovering ? ImGui.GetColorU32(ImGuiColors.TankBlue) : offColor ?? ImGui.GetColorU32(ImGuiCol.TextDisabled));
        AttachToolTip(helpText, color: tooltipCol);
    }

    public static void HelpText(string helpText, uint tooltipCol, bool inner = false, uint? offColor = null)
    {
        if (inner)
            ImUtf8.SameLineInner();
        else
            ImGui.SameLine();

        var hovering = ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetTextLineHeight()));
        FramedIconText(FAI.QuestionCircle, hovering ? ImGui.GetColorU32(ImGuiColors.TankBlue) : offColor ?? ImGui.GetColorU32(ImGuiCol.TextDisabled));
        AttachToolTip(helpText, color: ColorHelpers.RgbaUintToVector4(tooltipCol));
    }
}
