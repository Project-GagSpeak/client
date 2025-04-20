using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Helpers;

public static partial class CkComponents
{
    public static float FCRounding => ImGui.GetStyle().FrameRounding * 1.25f;
    public static float FCRoundingLarge => ImGui.GetStyle().FrameRounding * 1.75f;



    public static ImRaii.IEndObject FramedChild(string strId, uint bgCol)
        => new UnconditionalFramedChild(bgCol, FCRounding, ImGui.BeginChild(strId));

    public static ImRaii.IEndObject FramedChild(string strId, float rounding, uint bgCol)
        => new UnconditionalFramedChild(bgCol, rounding, ImGui.BeginChild(strId));

    public static ImRaii.IEndObject FramedChild(string strId, uint bgCol, Vector2 size)
        => new UnconditionalFramedChild(bgCol, FCRounding, ImGui.BeginChild(strId, size));

    public static ImRaii.IEndObject FramedChild(string strId, float rounding, uint bgCol, Vector2 size)
        => new UnconditionalFramedChild(bgCol, rounding, ImGui.BeginChild(strId, size));

    public static ImRaii.IEndObject FramedChild(string strId, uint bgCol, Vector2 size, ImGuiWindowFlags flags)
        => new UnconditionalFramedChild(bgCol, FCRounding, ImGui.BeginChild(strId, size, false, flags));

    public static ImRaii.IEndObject FramedChild(string strId, float rounding, uint bgCol, Vector2 size, ImGuiWindowFlags flags)
        => new UnconditionalFramedChild(bgCol, rounding, ImGui.BeginChild(strId, size, false, flags));

    private struct UnconditionalFramedChild : ImRaii.IEndObject
    {
        private Action EndAction { get; }
        public bool Success { get; }
        public bool Disposed { get; private set; }

        public UnconditionalFramedChild(uint bgColor, float rounding, bool success)
        {
            this.EndAction = () =>
            {
                ImGui.EndChild();
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
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
