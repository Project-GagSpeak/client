using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using GagSpeak.CkCommons.FileSystem;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;

namespace GagSpeak.CkCommons.Gui.Utility;

// Covers FANCY SELECTABLE STYLING.
public static partial class CkGuiUtils
{
    // Create a folder item selectable with custom rounding and styling.
    public static bool DrawTestSelectable(string label, bool selected)
    {
        // Identify our starting position.
        var cursorPos = ImGui.GetCursorPos();
        var res = false;
        // construct a group that everything is incapsulated within.
        using (ImRaii.Group())
        {            
            // Set the cursor back to the start.
            ImGui.SetCursorPos(cursorPos);

            // Draw the content.
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            ImGui.Text("Hello");
        }

        // Draw a border frame. (if desired) around the item.
        ImGui.GetWindowDrawList().AddRect(
            ImGui.GetItemRectMin() + ImGui.GetStyle().ItemInnerSpacing,
            ImGui.GetItemRectMax() - ImGui.GetStyle().ItemInnerSpacing,
            CkGui.Color(ImGuiColors.ParsedPink), 5);
        return res;
    }

    public static void DrawFolderSelectable<T>(CkFileSystem<T>.Folder folder, uint lineCol, bool selected) where T : class
    {
        // must be a valid drag drop source, so use invisible button.
        ImGui.InvisibleButton(folder.Identifier.ToString(), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeightWithSpacing()));
        var rectMin = ImGui.GetItemRectMin();       
        var rectMax = ImGui.GetItemRectMax();
        var bgColor = ImGui.IsItemHovered() ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkGui.Color(new Vector4(0.3f, 0.3f, 0.3f, 0.5f));
        ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, bgColor, 5);
        ImGui.GetWindowDrawList().AddRect(rectMin, rectMax, lineCol, 5);

        // Then the actual items.
        using (ImRaii.Group())
        {
            ImGui.SetCursorScreenPos(rectMin with { X = rectMin.X + ImGuiHelpers.GlobalScale * 2 });
            ImGui.AlignTextToFramePadding();
            CkGui.IconButton(folder.State ? FAI.FolderOpen : FAI.FolderClosed, inPopup: true);
            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(folder.Name);
        }
    }

}
