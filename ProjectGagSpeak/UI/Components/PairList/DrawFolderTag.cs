using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Configs;
using ImGuiNET;
using System.Collections.Immutable;

namespace GagSpeak.UI.Components.UserPairList;

/// <summary> The inherited class of the draw folder which determines what folders should draw what components. </summary>
public class DrawFolderTag : DrawFolderBase
{

    public DrawFolderTag(string id, IImmutableList<DrawUserPair> drawPairs, 
        IImmutableList<Pair> allPairs, ServerConfigurationManager configs,
        UiSharedService uiShared) : base(id, drawPairs, allPairs, configs, uiShared)
    { }

    protected override bool RenderIfEmpty => _id switch
    {
        Globals.CustomOnlineTag => false,
        Globals.CustomOfflineTag => false,
        Globals.CustomVisibleTag => false,
        Globals.CustomAllTag => true,
        _ => true,
    };

    private bool RenderCount => _id switch
    {
        Globals.CustomOnlineTag => false,
        Globals.CustomOfflineTag => false,
        Globals.CustomVisibleTag => false,
        Globals.CustomAllTag => false,
        _ => true
    };

    protected override float DrawIcon()
    {
        var icon = _id switch
        {
            Globals.CustomOnlineTag => FontAwesomeIcon.Link,
            Globals.CustomOfflineTag => FontAwesomeIcon.Unlink,
            Globals.CustomVisibleTag => FontAwesomeIcon.Eye,
            Globals.CustomAllTag => FontAwesomeIcon.User,
            _ => FontAwesomeIcon.Folder
        };

        ImGui.AlignTextToFramePadding();
        _uiShared.IconText(icon);

        if (RenderCount)
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = ImGui.GetStyle().ItemSpacing.X / 2f }))
            {
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();

                ImGui.TextUnformatted("[" + OnlinePairs.ToString() + "]");
            }
            UiSharedService.AttachToolTip(OnlinePairs + " online" + Environment.NewLine + TotalPairs + " total");
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
            Globals.CustomOnlineTag => "GagSpeak Online Users",
            Globals.CustomOfflineTag => "GagSpeak Offline Users",
            Globals.CustomVisibleTag => "Visible",
            Globals.CustomAllTag => "Users",
            _ => _id
        };
        ImGui.TextUnformatted(name);
    }
}
