using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="Group(uint, float, float)"/>
    public static ImRaii.IEndObject Group()
        => Group(0, 0);

    /// <inheritdoc cref="Group(uint, float, float)"/>
    public static ImRaii.IEndObject Group(uint bgCol)
        => Group(bgCol, 0);

    /// <summary> An extended utility version of ImRaii.Group that allows for background color support </summary>
    /// <param name="bgCol"> The color drawn out behind the group. </param>
    /// <param name="rounding"> The rounding applied to the drawn BG. </param>
    public static ImRaii.IEndObject Group(uint bgCol, float rounding, float frameThickness = 0)
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
                wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding);

            // Draw frame (if included)
            if (frameThickness is not 0)
                wdl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, ImDrawFlags.None, frameThickness);

            // Merge back channels.
            wdl.ChannelsMerge();
        }, true);
    }


    /// <inheritdoc cref="FakeChild(float, uint, float, float)"/>"
    public static ImRaii.IEndObject FakeChild(float widthSpan)
        => FakeChild(widthSpan, 0, 0);


    /// <inheritdoc cref="FakeChild(float, uint, float, float)"/>"
    public static ImRaii.IEndObject FakeChild(float widthSpan, uint bgCol)
        => FakeChild(widthSpan, bgCol, 0);


    /// <summary> Simulates a fake child through the windowDrawList and ImGui.Group()
    /// <para><b>DON'T USE ImGui.GetWindowContentRegionAvail() in this!!!</b></para>
    /// </summary>
    /// <remarks> Allows for a child a flexible height, or area where a height is not yet known. </remarks>
    /// <param name="widthSpan"> The width of the fake child. </param>
    /// <param name="bgCol"> The color drawn out behind the group. </param>
    /// <param name="rounding"> The rounding applied to the drawn BG. </param>
    /// <param name="frameThickness"> The thickness of the frame, if one should be applied. </param>
    public static ImRaii.IEndObject FakeChild(float widthSpan, uint bgCol, float rounding, float frameThickness = 0)
    {
        var wdl = ImGui.GetWindowDrawList();
        wdl.ChannelsSplit(2);
        wdl.ChannelsSetCurrent(1);

        // Offset by windowPadding
        ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetStyle().WindowPadding);
        var innerWidth = widthSpan - ImGui.GetStyle().WindowPadding.X * 2;

        // Draw the group in the foreground.
        ImGui.BeginGroup();

        // After group is drawn
        return new EndObjectContainer(() =>
        {
            ImGui.EndGroup();

            // Switch to background channel.
            wdl.ChannelsSetCurrent(0);

            // Add the BG, if we should.
            if (bgCol is not 0)
                wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, ImDrawFlags.RoundCornersAll);

            // Add the Frame, if we should.
            if (frameThickness is not 0)
                wdl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, ImDrawFlags.None, frameThickness);

            // Merge back together the channels.
            wdl.ChannelsMerge();
        }, true, new Vector2(innerWidth, 0));
    }
}
