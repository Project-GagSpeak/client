using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using CkCommons.DrawSystem;

namespace GagSpeak.Gui;

public partial class DebugStorageUI
{
    private void DrawDDSDebug<T>(string label, DynamicDrawSystem<T> dds) where T : class
    {
        using var _ = ImRaii.TreeNode($"{label} (DDS)");
        if (!_)
            return;
        // Have details about the draw system and its loaded information.
        DrawFolderMap(dds);

        // Now draw out the root data.
        DrawFolderGroup(dds, dds.Root);

        CkGui.SeparatorSpaced(GsCol.VibrantPink.Uint());
    }

    private void DrawFolderMap<T>(DynamicDrawSystem<T> dds) where T : class
    {
        using var _ = ImRaii.TreeNode("Folder Map");
        if (!_)
            return;

        using (var t = ImRaii.Table("FolderMap-table", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t)
                return;

            ImGui.TableSetupColumn("String Name");
            ImGui.TableSetupColumn("Folder Name");
            ImGui.TableSetupColumn("Folder Path");
            ImGui.TableHeadersRow();

            foreach (var (strName, folder) in dds.FolderMap)
            {
                ImGui.TableNextColumn();
                ImGui.Text(strName);
                ImGui.TableNextColumn();
                ImGui.Text(folder.Name);
                ImGui.TableNextColumn();
                CkGui.ColorText(folder.FullPath, ImGuiColors.TankBlue);
                ImGui.TableNextRow();
            }
        }

    }
    // Pretty sure type casting in a draw function is not the best idea, but its just for debugging.
    private void DrawFolderGroup<T>(DynamicDrawSystem<T> dds, IDynamicFolderGroup<T> group) where T : class
    {
        using var _ = ImRaii.TreeNode($"{group.Name}##{group.ID}");
        if (!_)
            return;
        // Sort order
        ImGui.Text("Sort Order:");
        CkGui.ColorTextInline(string.Join(", ", group.Sorter.Select(s => s.Name)), ImGuiColors.DalamudViolet);
        // Initial details about the folder
        DrawFolderInfo(group);
        // Children
        foreach (var child in group.Children)
        {
            if (child is DynamicFolderGroup<T> childGroup)
                DrawFolderGroup(dds, childGroup);
            else if (child is DynamicFolder<T> childFolder)
                DrawFolder(dds, childFolder);
            else
                CkGui.ColorText("INVALID CHILD TYPE", ImGuiColors.DalamudRed);
        }
        // Divider
        CkGui.SeparatorSpaced(GsCol.VibrantPink.Uint());
    }

    // Need to draw distinct folders for each kind of folder? (Do default for now)
    private void DrawFolder<T>(DynamicDrawSystem<T> dds, IDynamicFolder<T> folder) where T : class
    {
        using var _ = ImRaii.TreeNode($"{folder.Name}##{folder.ID}");
        if (!_)
            return;
        // Sort order
        ImGui.Text("Sort Order:");
        CkGui.ColorTextInline(string.Join(", ", folder.Sorter.Select(s => s.Name)), ImGuiColors.DalamudViolet);
        // Initial details about the folder.
        DrawFolderInfo(folder);
        // Children
        using (var t = ImRaii.Table($"FolderLeaves-{folder.ID}", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t)
                return;

            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("FullPath");
            ImGui.TableHeadersRow();

            foreach (var leaf in folder.Children)
            {
                ImGui.TableNextColumn();
                ImGui.Text(leaf.Name);
                ImGui.TableNextColumn();
                CkGui.ColorText(leaf.FullPath, ImGuiColors.DalamudViolet);
                ImGui.TableNextRow();
            }
        }
        CkGui.SeparatorSpaced(GsCol.VibrantPink.Uint());
    }

    private void DrawFolderInfo<T>(IDynamicCollection<T> folder) where T : class
    {
        // Initial details about the folder.
        using (var t = ImRaii.Table($"GroupData-{folder.ID}", 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t)
                return;

            ImGui.TableSetupColumn("ID");
            ImGui.TableSetupColumn("Parent");
            ImGui.TableSetupColumn("Icon");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Items");
            ImGui.TableSetupColumn("FullPath");
            ImGui.TableSetupColumn("Flags");
            ImGui.TableHeadersRow();

            ImGui.TableNextColumn();
            ImGui.Text(folder.ID.ToString());
            ImGui.TableNextColumn();
            CkGui.ColorText(folder.IsRoot ? "UNK" : folder.Parent.Name, ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            CkGui.IconText(folder.Icon, folder.IconColor);
            ImGui.TableNextColumn();
            CkGui.ColorText(folder.Name, folder.NameColor);
            ImGui.TableNextColumn();
            ImGui.Text(folder.TotalChildren.ToString());
            ImGui.TableNextColumn();
            CkGui.ColorText(folder.FullPath, ImGuiColors.TankBlue);
            ImGui.TableNextColumn();
            ImGui.Text(folder.Flags.ToString());
        }
    }
}
