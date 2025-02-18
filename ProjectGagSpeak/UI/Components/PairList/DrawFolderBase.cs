using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI.Handlers;
using System.Collections.Immutable;
using GagSpeak.UI;
using OtterGui.Text;
using GagSpeak.PlayerData.Storage;

namespace GagSpeak.UI.Components.UserPairList;

/// <summary>
/// The base for the draw folder, which is a dropdown section in the list of paired users, and handles the basic draw functionality
/// </summary>
public abstract class DrawFolderBase : IDrawFolder
{
    public IImmutableList<DrawUserPair> DrawPairs { get; init; }
    protected readonly string _id;
    protected readonly IImmutableList<Pair> _allPairs;
    protected readonly NicknamesConfigService _openFolders;
    protected readonly UiSharedService _uiShared;
    private float _menuWidth = -1;
    private bool _wasHovered = false;

    public int OnlinePairs => DrawPairs.Count(u => u.Pair.IsOnline);
    public int TotalPairs => _allPairs.Count;
    public string ID => _id;

    protected DrawFolderBase(string id, IImmutableList<DrawUserPair> drawPairs,
        IImmutableList<Pair> allPairs, NicknamesConfigService openFolders, UiSharedService uiShared)
    {
        _id = id;
        DrawPairs = drawPairs;
        _allPairs = allPairs;
        _openFolders = openFolders;
        _uiShared = uiShared;
    }

    protected abstract bool RenderIfEmpty { get; }
    protected abstract bool RenderMenu { get; }

    public void Draw()
    {
        if (!RenderIfEmpty && !DrawPairs.Any())
            return;

        using var id = ImRaii.PushId("folder_" + _id);
        var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), _wasHovered);
        using (ImRaii.Child("folder__" + _id, new Vector2(
            UiSharedService.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight())))
        {
            // draw opener
            var icon = _openFolders.Storage.OpenPairListFolders.Contains(_id) ? FontAwesomeIcon.CaretDown : FontAwesomeIcon.CaretRight;

            ImUtf8.SameLineInner();
            ImGui.AlignTextToFramePadding();
            _uiShared.IconText(icon);

            ImGui.SameLine();
            var leftSideEnd = DrawIcon();

            ImGui.SameLine();
            var rightSideStart = ImGui.GetWindowContentRegionMin().X + UiSharedService.GetWindowContentRegionWidth() - ImGui.GetStyle().ItemSpacing.X;

            // draw name
            ImGui.SameLine(leftSideEnd);
            DrawName(rightSideStart - leftSideEnd);
        }
        _wasHovered = ImGui.IsItemHovered();
        if (ImGui.IsItemClicked())
        {
            _openFolders.Storage.OpenPairListFolders.SymmetricExceptWith(new[] { _id });
            _openFolders.Save();
        }

        color.Dispose();

        ImGui.Separator();

        // if opened draw content
        if (_openFolders.Storage.OpenPairListFolders.Contains(_id))
        {
            using var indent = ImRaii.PushIndent(_uiShared.GetIconData(FontAwesomeIcon.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);
            if (DrawPairs.Any())
            {
                foreach (var item in DrawPairs)
                    item.DrawPairedClient(0);
            }
            else
            {
                ImGui.TextUnformatted("No Draw Pairs to Draw");
            }

            ImGui.Separator();
        }
    }

    protected abstract float DrawIcon();

    protected abstract void DrawMenu(float menuWidth);

    protected abstract void DrawName(float width);
}
