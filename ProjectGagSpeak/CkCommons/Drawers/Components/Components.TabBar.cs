using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.Services;
using GagSpeak.UI;
using ImGuiNET;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.CkCommons.Helpers;

// Tab bars currently have no window background, but rather are bordered.
public static partial class CkComponents
{
    public static ImRaii.IEndObject TabBarChild(string id, out ICkTab? selected, params ICkTab[] tabs)
        => new UnconditionalTabBar(id, ImGui.GetContentRegionAvail(), WFlags.None, out selected, tabs);

    public static ImRaii.IEndObject TabBarChild(string id, WFlags childFlags, out ICkTab? selected, params ICkTab[] tabs)
        => new UnconditionalTabBar(id, ImGui.GetContentRegionAvail(), childFlags, out selected, tabs);

    public static ImRaii.IEndObject TabBarChild(string id, Vector2 width, out ICkTab? selected, params ICkTab[] tabs)
        => new UnconditionalTabBar(id, width, WFlags.None, out selected, tabs);

    public static ImRaii.IEndObject TabBarChild(string id, Vector2 width, WFlags childFlags, out ICkTab? selected, params ICkTab[] tabs)
        => new UnconditionalTabBar(id, width, childFlags, out selected, tabs);


    private struct UnconditionalTabBar : ImRaii.IEndObject
    {
        private Action EndAction { get; }
        public bool Success { get; } = true;
        public bool Disposed { get; private set; }

        public UnconditionalTabBar(string id, Vector2 region, WFlags childFlags, out ICkTab? selected, params ICkTab[] tabs)
        {
            ImGui.BeginGroup();
            FancyTabBar.DrawBar(id, region.X, out selected, tabs);
            var innerSize = new Vector2(region.X, Math.Min(region.Y, ImGui.GetContentRegionAvail().Y));
            this.Success = ImGui.BeginChild(id, innerSize, false, childFlags);
            this.Disposed = false;

            EndAction = () =>
            {
                ImGui.EndChild();
                ImGui.EndGroup();
                // border frame it.
                ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(),
                    CkColor.VibrantPink.Uint(), FancyTabBar.Rounding, ImDrawFlags.RoundCornersAll, 1.5f);
            };
        }

        public void Dispose()
        {
            if (this.Disposed)
                return;

            this.EndAction();
            this.Disposed = true;
        }
    }
}
