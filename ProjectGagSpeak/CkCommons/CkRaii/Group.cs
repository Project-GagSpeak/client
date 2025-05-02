using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="Group(uint, float, float, ImDrawFlags)"/>
    public static ImRaii.IEndObject Group()
        => Group(0, 0, 0, ImDrawFlags.None);

    /// <inheritdoc cref="Group(uint, float, float, ImDrawFlags)"/>
    public static ImRaii.IEndObject Group(uint bgCol, float rounding, ImDrawFlags flags = ImDrawFlags.None)
        => Group(bgCol, rounding, 0, flags);

    /// <summary> An extended utility version of ImRaii.Group that allows for background color support </summary>
    /// <param name="bgCol"> The color drawn out behind the group. </param>
    /// <param name="rounding"> The rounding applied to the drawn BG. </param>
    /// <remarks> DO NOT NEST THESE WITHIN OTHER GROUPS. If you want to simply group things, use ImRaii.Group() </remarks>
    public static ImRaii.IEndObject Group(uint bgCol, float rounding, float frameThickness = 0, ImDrawFlags flags = ImDrawFlags.None)
    {
        var wdl = ImGui.GetWindowDrawList();
        wdl.ChannelsSplit(2);
        // Foreground.
        wdl.ChannelsSetCurrent(1);
        // Draw group.
        ImGui.BeginGroup();

        // After group is drawn
        return new EndUnconditionally(() =>
        {
            ImGui.EndGroup();
            // Switch to background channel.
            wdl.ChannelsSetCurrent(0);

            // Draw background (if included)
            if (bgCol is not 0) 
                wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, flags);

            // Draw frame (if included)
            if (frameThickness is not 0)
                wdl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, flags, frameThickness);

            // Merge back channels.
            wdl.ChannelsMerge();
        }, true);
    }
}
