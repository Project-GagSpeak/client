using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Helpers;

public static partial class CkComponents
{
    public static ImRaii.IEndObject FramedChild(string strId, uint bgCol)
        => new UnconditionalFramedChild(bgCol, ImGui.BeginChild(strId));

    public static ImRaii.IEndObject FramedChild(string strId, uint bgCol, Vector2 size)
        => new UnconditionalFramedChild(bgCol, ImGui.BeginChild(strId, size));

    public static ImRaii.IEndObject FramedChild(string strId, uint bgCol, Vector2 size, ImGuiWindowFlags flags)
        => new UnconditionalFramedChild(bgCol, ImGui.BeginChild(strId, size, false, flags));

    private struct UnconditionalFramedChild : ImRaii.IEndObject
    {
        private Action EndAction { get; }
        public bool Success { get; }
        public bool Disposed { get; private set; }

        public UnconditionalFramedChild(uint bgColor, bool success)
        {
            this.EndAction = () =>
            {
                ImGui.EndChild();
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                var rounding = ImGui.GetStyle().FrameRounding * 1.25f;
                ImGui.GetWindowDrawList().AddRectFilled(min, max, bgColor, rounding, ImDrawFlags.RoundCornersAll);
                ImGui.GetWindowDrawList().AddRect(min, max, bgColor, rounding, ImDrawFlags.None, 2);

            };
            this.Success = success;
            this.Disposed = false;
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
