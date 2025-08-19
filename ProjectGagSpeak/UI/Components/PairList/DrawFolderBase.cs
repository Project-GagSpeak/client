using CkCommons.Gui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Kinksters;
using GagSpeak.Services.Configs;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using System.Collections.Immutable;
using CkCommons.Raii;

namespace GagSpeak.Gui.Components;

/// <summary> The base for the draw folder, a dropdown section in the list of paired users </summary>
public abstract class DrawFolderBase : IDrawFolder
{
    public IImmutableList<DrawUserPair> DrawPairs { get; init; }
    protected readonly string _id;
    protected readonly IImmutableList<Kinkster> _allPairs;
    protected readonly ServerConfigManager _serverConfigs;

    private bool _wasHovered = false;

    public int OnlinePairs => DrawPairs.Count(u => u.Pair.IsOnline);
    public int TotalPairs => _allPairs.Count;
    public string ID => _id;

    protected DrawFolderBase(string id, IImmutableList<DrawUserPair> drawPairs,
        IImmutableList<Kinkster> allPairs, ServerConfigManager serverConfigs)
    {
        _id = id;
        DrawPairs = drawPairs;
        _allPairs = allPairs;
        _serverConfigs = serverConfigs;

    }

    protected abstract bool RenderIfEmpty { get; }

    public void Draw()
    {
        if (!RenderIfEmpty && !DrawPairs.Any())
            return;

        using var id = ImRaii.PushId("folder_" + _id);
        var size = new Vector2(CkGui.GetWindowContentRegionWidth() - ImGui.GetCursorPosX(), ImGui.GetFrameHeight());
        using (CkRaii.Child("folder__" + _id, size, _wasHovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : 0, 0f))
        {
            var icon = _serverConfigs.NickStorage.OpenPairListFolders.Contains(_id) ? FAI.CaretDown : FAI.CaretRight;

            CkGui.FramedIconText(icon);

            ImGui.SameLine();
            var leftSideEnd = DrawIcon();

            ImGui.SameLine();
            var rightSideStart = ImGui.GetWindowContentRegionMin().X + CkGui.GetWindowContentRegionWidth() - ImGui.GetStyle().ItemSpacing.X;

            // draw name
            ImGui.SameLine(leftSideEnd);
            DrawName(rightSideStart - leftSideEnd);
        }
        _wasHovered = ImGui.IsItemHovered();
        if (ImGui.IsItemClicked())
        {
            _serverConfigs.NickStorage.OpenPairListFolders.SymmetricExceptWith(new[] { _id });
            _serverConfigs.SaveNicknames();
        }

        ImGui.Separator();

        // if opened draw content
        if (_serverConfigs.NickStorage.OpenPairListFolders.Contains(_id))
        {
            using var indent = ImRaii.PushIndent(CkGui.IconSize(FAI.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);
            foreach (var item in DrawPairs)
                item.DrawPairedClient();

            ImGui.Separator();
        }
    }

    protected abstract float DrawIcon();

    protected abstract void DrawName(float width);
}
