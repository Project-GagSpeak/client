using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using ImGuiNET;
using System.Collections.Immutable;

namespace GagSpeak.UI.Components.UserPairList;

/// <summary>
/// Class handling the tag (name) that a dropdown folder section has in the list of paired users
/// <para> 
/// Notibly, by being a parent of the draw folder base, it is able to override some functions inside the base,
/// such as draw icon, allowing it to draw customized icon's for the spesific catagoeiss of folder dropdowns
/// </para>
/// </summary>
public class DrawFolderTag : DrawFolderBase
{

    public DrawFolderTag(string id, IImmutableList<DrawUserPair> drawPairs, 
        IImmutableList<Pair> allPairs, UiSharedService uiSharedService)
        : base(id, drawPairs, allPairs, uiSharedService)
    { }

    protected override bool RenderIfEmpty => _id switch
    {
        Globals.CustomOnlineTag => false,
        Globals.CustomOfflineTag => false,
        Globals.CustomVisibleTag => false,
        Globals.CustomAllTag => true,
        _ => true,
    };

    protected override bool RenderMenu => _id switch
    {
        Globals.CustomOnlineTag => false,
        Globals.CustomOfflineTag => false,
        Globals.CustomVisibleTag => false,
        Globals.CustomAllTag => false,
        _ => true,
    };

    private bool RenderPause => _id switch
    {
        Globals.CustomOnlineTag => false,
        Globals.CustomOfflineTag => false,
        Globals.CustomVisibleTag => false,
        Globals.CustomAllTag => false,
        _ => true,
    } && _allPairs.Any();

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
        _uiSharedService.IconText(icon);

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
