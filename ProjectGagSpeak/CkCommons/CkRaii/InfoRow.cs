using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Gui;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="InfoRow(FAI, string, string, string, string?)" />"
    public static ImRaii.IEndObject InfoRow(FAI icon, string iconTT)
        => InfoRow(icon, string.Empty, iconTT, string.Empty, string.Empty);

    /// <inheritdoc cref="InfoRow(FAI, string, string, string, string?)" />"
    public static ImRaii.IEndObject InfoRow(string tooltips, FAI icon)
        => InfoRow(icon, string.Empty, tooltips, tooltips, string.Empty);

    /// <inheritdoc cref="InfoRow(FAI, string, string, string, string?)" />"
    public static ImRaii.IEndObject InfoRow(FAI icon, string prefix, string tooltips)
        => InfoRow(icon, prefix, tooltips, tooltips, string.Empty);

    /// <summary> Draws out a InfoRow Group displaying an Icon, prefix text, center object, and suffix text. </summary>
    /// <param name="icon"> The icon to display. </param>
    /// <param name="prefix"> The text to show prior to the draw action. </param>
    /// <param name="suffix"> The text to show after the draw action. </param>
    /// <param name="iconTT"> The tooltip to display when hovering over the icon. </param>
    /// <param name="groupTT"> The tooltip to display when hovering over the text or draw action area. </param>
    /// <remarks> Your draw logic within should not exceed ImGui.GetFrameHeight() </remarks>
    public static ImRaii.IEndObject InfoRow(FAI icon, string prefix, string iconTT, string groupTT, string? suffix = null)
    {
        // Begin the outer group.
        ImGui.BeginGroup();

        // The Icon section of the row.
        CkGui.FramedIconText(icon);
        CkGui.AttachToolTip(iconTT);

        // Ensure action is drawn beside.
        ImUtf8.SameLineInner();

        // The action & text section.
        using var id = ImRaii.PushId(icon + prefix);
        ImGui.BeginGroup();

        // The prefix text.
        if (!prefix.IsNullOrWhitespace())
        {
            ImUtf8.TextFrameAligned(prefix);
            ImUtf8.SameLineInner();
        }
        // After internal segment is drawn out.
        return new EndUnconditionally(() =>
        {
            // Suffix Inline.
            if(!suffix.IsNullOrWhitespace())
                CkGui.TextFrameAlignedInline(suffix);

            // End the inner group.
            ImGui.EndGroup();
            CkGui.AttachToolTipRect(ImGui.GetItemRectMin(),ImGui.GetItemRectMax(), groupTT);

            // End the outer group.
            ImGui.EndGroup();
        }, true);
    }
}
