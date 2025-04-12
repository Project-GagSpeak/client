using ImGuiNET;
using System.Runtime.CompilerServices;

namespace GagSpeak.UI;
#nullable disable

// DragDrop helpers, pulled from ECommons.ImGuiMethods.ImGuiDragDrop for reference, credit to them for original code. This is an adaptation.
public partial class CkGui
{
    /// <summary> A helper function to attach a tooltip to a section in the UI currently hovered. </summary>
    public static unsafe void SetDragDropPayload<T>(string type, T data, ImGuiCond condition = 0) where T : unmanaged
    {
        var ptr = Unsafe.AsPointer(ref data);
        ImGui.SetDragDropPayload(type, new IntPtr(ptr), (uint)Unsafe.SizeOf<T>(), condition);
    }

    public static unsafe bool AcceptDragDropPayload<T>(string type, out T payload, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None) where T : unmanaged
    {
        ImGuiPayload* payloadPtr = ImGui.AcceptDragDropPayload(type, flags);
        payload = (payloadPtr != null) ? Unsafe.Read<T>(payloadPtr->Data) : default;
        return payloadPtr != null;
    }

    public static unsafe void SetDragDropPayload(string type, string data, ImGuiCond condition = 0)
    {
        fixed (char* chars = data)
        {
            var byteCount = Encoding.Default.GetByteCount(data);
            var bytes = stackalloc byte[byteCount];
            Encoding.Default.GetBytes(chars, data.Length, bytes, byteCount);

            ImGui.SetDragDropPayload(type, new IntPtr(bytes), (uint)byteCount, condition);
        }
    }

    public static unsafe bool AcceptDragDropPayload(string type, out string payload, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
    {
        ImGuiPayload* payloadPtr = ImGui.AcceptDragDropPayload(type, flags);
        payload = (payloadPtr != null) ? Encoding.Default.GetString((byte*)payloadPtr->Data, payloadPtr->DataSize) : null;
        return payloadPtr != null;
    }
}
