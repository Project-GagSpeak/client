using ImGuiNET;
using OtterGui.OtterGuiInternal.Enums;
using OtterGuiInternal.Enums;
using OtterGuiInternal.Structs;

namespace OtterGuiInternal;

public static unsafe partial class ImGuiNativeInterop
{
    private const string CLibraryName = "cimgui";


    [LibraryImport(CLibraryName, EntryPoint = "igGetCurrentWindow")]
    public static partial ImGuiWindow* GetCurrentWindow();


    [LibraryImport(CLibraryName, EntryPoint = "igItemAdd")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ItemAdd(ImRect bb, ImGuiId id, ImRect* navBb, ItemFlags flags);


    [LibraryImport(CLibraryName, EntryPoint = "igButtonBehavior")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool ButtonBehavior(ImRect bb, ImGuiId id,
        [MarshalAs(UnmanagedType.U1)] out bool hovered,
        [MarshalAs(UnmanagedType.U1)] out bool held, ImGuiButtonFlags flags);


    [LibraryImport(CLibraryName, EntryPoint = "igItemSize_Rect")]
    public static partial void ItemSizeRect(ImRect bb, float textBaseLineY);


    [LibraryImport(CLibraryName, EntryPoint = "igItemSize_Vec2")]
    public static partial void ItemSizeVec(ImVec2 bb, float textBaseLineY);


    [LibraryImport(CLibraryName, EntryPoint = "igRenderNavHighlight")]
    public static partial void RenderNavHighlight(ImRect bb, ImGuiId id, NavHighlightFlags flags);


    [LibraryImport(CLibraryName, EntryPoint = "igRenderFrame")]
    public static partial void RenderFrame(ImVec2 min, ImVec2 max, uint fillColor, [MarshalAs(UnmanagedType.U1)] bool border, float rounding);


    [LibraryImport(CLibraryName, EntryPoint = "igCalcItemSize")]
    public static partial void CalcItemSize(out ImVec2 result, ImVec2 min, float defaultWidth, float defaultHeight);


    [LibraryImport(CLibraryName, EntryPoint = "igRenderTextClippedEx")]
    public static partial void RenderTextClippedEx(ImDrawList* drawList, ImVec2 posMin, ImVec2 posMax, byte* text, byte* textDisplayEnd,
        ImVec2* textSizeIfKnown, ImVec2 align, ImRect* clipRect);


    [LibraryImport(CLibraryName, EntryPoint = "igSetItemKeyOwner")]
    public static partial void SetItemKeyOwner(ImGuiKey key, ImGuiInputFlags flags);

    [LibraryImport(CLibraryName, EntryPoint = "igSetItemUsingMouseWheel")]
    public static partial void SetItemUsingMouseWheel();
}