using ImGuiNET;
using OtterGuiInternal.Enums;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace OtterGuiInternal.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct ImGuiWindow
{
    // @formatter:off
    [FieldOffset(0x0000)]                              public byte*             Name;
    [FieldOffset(0x0008)]                              public ImGuiId           Id;
    [FieldOffset(0x000C)]                              public ImGuiWindowFlags  Flags;
    [FieldOffset(0x0010)]                              public ImGuiWindowFlags  LastFlags;
    [FieldOffset(0x0048)]                              public Vector2           Pos;
    [FieldOffset(0x0050)]                              public Vector2           Size;
    [FieldOffset(0x0058)]                              public Vector2           SizeFull;
    [FieldOffset(0x0060)]                              public Vector2           ContentSize;
    [FieldOffset(0x0068)]                              public Vector2           ContentSizeIdeal;
    [FieldOffset(0x0070)]                              public Vector2           ContentSizeExplicit;
    [FieldOffset(0x0078)]                              public Vector2           WindowPadding;
    [FieldOffset(0x007C)]                              public float             WindowRounding;
    [FieldOffset(0x0080)]                              public float             WindowBorderSize;
    [FieldOffset(0x0084)]                              public int               NameBufLength;
    [FieldOffset(0x0098)]                              public Vector2           Scroll;
    [FieldOffset(0x00A0)]                              public Vector2           ScrollMax;
    [FieldOffset(0x00A8)]                              public Vector2           ScrollTarget;
    [FieldOffset(0x00B0)]                              public Vector2           ScrollTargetCenterRatio;
    [FieldOffset(0x00B8)]                              public Vector2           ScrollTargetEdgeSnapDist;
    [FieldOffset(0x00C0)]                              public Vector2           ScrollBarSizes;
    [FieldOffset(0x00C8)][MarshalAs(UnmanagedType.U1)] public bool              ScrollBarX;
    [FieldOffset(0x00C9)][MarshalAs(UnmanagedType.U1)] public bool              ScrollBarY;
    [FieldOffset(0x00CA)][MarshalAs(UnmanagedType.U1)] public bool              Active;
    [FieldOffset(0x00CB)][MarshalAs(UnmanagedType.U1)] public bool              WasActive;
    [FieldOffset(0x00CC)][MarshalAs(UnmanagedType.U1)] public bool              WriteAccessed;
    [FieldOffset(0x00CD)][MarshalAs(UnmanagedType.U1)] public bool              Collapsed;
    [FieldOffset(0x00CE)][MarshalAs(UnmanagedType.U1)] public bool              WantCollapseToggle;
    [FieldOffset(0x00CF)][MarshalAs(UnmanagedType.U1)] public bool              SkipItems;
    [FieldOffset(0x00D0)][MarshalAs(UnmanagedType.U1)] public bool              Appearing;
    [FieldOffset(0x00D1)][MarshalAs(UnmanagedType.U1)] public bool              Hidden;
    [FieldOffset(0x00D2)][MarshalAs(UnmanagedType.U1)] public bool              IsFallbackWindow;
    [FieldOffset(0x00D3)][MarshalAs(UnmanagedType.U1)] public bool              IsExplicitChild;
    [FieldOffset(0x00D4)][MarshalAs(UnmanagedType.U1)] public bool              HasCloseButton;
    [FieldOffset(0x0110)]                              public ImVector<ImGuiId> IdStack;
    [FieldOffset(0x0118)]                              public ImGuiWindowTemp   Dc;
    // @formatter:on
}
