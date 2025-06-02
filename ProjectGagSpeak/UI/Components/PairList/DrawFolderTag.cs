using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Configs;
using ImGuiNET;
using System.Collections.Immutable;

namespace GagSpeak.CkCommons.Gui.Components;

/// <summary> The inherited class of the draw folder which determines what folders should draw what components. </summary>
public class DrawFolderTag : DrawFolderBase
{
    public DrawFolderTag(
        string id,
        IImmutableList<DrawUserPair> drawPairs, 
        IImmutableList<Pair> allPairs,
        ServerConfigurationManager configs) : base(id, drawPairs, allPairs, configs)
    { }

    protected override bool RenderIfEmpty => _id switch
    {
        Constants.CustomOnlineTag => false,
        Constants.CustomOfflineTag => false,
        Constants.CustomVisibleTag => false,
        Constants.CustomAllTag => true,
        _ => true,
    };

    private bool RenderCount => _id switch
    {
        Constants.CustomOnlineTag => false,
        Constants.CustomOfflineTag => false,
        Constants.CustomVisibleTag => false,
        Constants.CustomAllTag => false,
        _ => true
    };

    protected override float DrawIcon()
    {
        var icon = _id switch
        {
            Constants.CustomOnlineTag => FAI.Link,
            Constants.CustomOfflineTag => FAI.Unlink,
            Constants.CustomVisibleTag => FAI.Eye,
            Constants.CustomAllTag => FAI.User,
            _ => FAI.Folder
        };

        ImGui.AlignTextToFramePadding();
        CkGui.IconText(icon);

        if (RenderCount)
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = ImGui.GetStyle().ItemSpacing.X / 2f }))
            {
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();

                ImGui.TextUnformatted("[" + OnlinePairs.ToString() + "]");
            }
            CkGui.AttachToolTip(OnlinePairs + " online" + Environment.NewLine + TotalPairs + " total");
        }
        ImGui.SameLine();
        return ImGui.GetCursorPosX();
    }

    /// <summary> The label for each dropdown folder in the list. </summary>
    protected override void DrawName(float width)
    {
        ImGui.AlignTextToFramePadding();
        var name = _id switch
        {
            Constants.CustomOnlineTag => "GagSpeak Online Users",
            Constants.CustomOfflineTag => "GagSpeak Offline Users",
            Constants.CustomVisibleTag => "Visible",
            Constants.CustomAllTag => "Users",
            _ => _id
        };
        ImGui.TextUnformatted(name);
    }
}
