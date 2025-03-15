using GagSpeak.UI;

namespace GagSpeak.CkCommons;

public enum CkColor
{
    // Colors used in remotes (from old version, subject to change)
    VibrantPink,
    VibrantPinkHovered,
    VibrantPinkPressed,

    CkMistressColor,
    CkMistressText,
    
    LushPinkLine,
    LushPinkButton,
    
    RemotePlaybackBg,
    RemoteInterfaceBg,
    RemoteInterfaceBgAlt,

    ButtonDrag,

    SideButton,
    SideButtonBG,

    // UI Element Components
    FancyHeader,
    FancyHeaderContrast,
    ElementHeader,
    ElementSplit,
    ElementBG,

    // Favoriting
    FavoriteStarOn,
    FavoriteStarHovered,
    FavoriteStarOff,

    // File System
    FolderExpanded,
    FolderCollapsed,
    FolderLine,

    // TriStateBoxes
    TriStateCheck,
    TriStateCross,
    TriStateNeutral,

    // IconCheckboxes
    IconCheckOn,
    IconCheckOff,
}

public static class CkColors
{
    public static Vector4 Vec4(this CkColor color)
        => color switch
        {
            CkColor.VibrantPink         => new Vector4(0.977f, 0.380f, 0.640f, 0.914f),
            CkColor.VibrantPinkHovered  => new Vector4(0.986f, 0.464f, 0.691f, 0.955f),
            CkColor.VibrantPinkPressed  => new Vector4(0.846f, 0.276f, 0.523f, 0.769f),

            CkColor.CkMistressColor     => new Vector4(0.886f, 0.407f, 0.658f, 1f),
            CkColor.CkMistressText      => new Vector4(1     , 0.711f, 0.843f, 1f),

            CkColor.LushPinkLine        => new Vector4(0.806f, 0.102f, 0.407f, 1    ),
            CkColor.LushPinkButton      => new Vector4(1     , 0.051f, 0.462f, 1    ),

            CkColor.RemotePlaybackBg    => new Vector4(0.042f, 0.042f, 0.042f, 0.930f),
            CkColor.RemoteInterfaceBg   => new Vector4(0.110f, 0.110f, 0.110f, 0.930f),
            CkColor.RemoteInterfaceBgAlt=> new Vector4(0.100f, 0.100f, 0.100f, 0.930f),

            CkColor.ButtonDrag          => new Vector4(0.097f, 0.097f, 0.097f, 0.930f),

            CkColor.SideButton          => new Vector4(0.451f, 0.451f, 0.451f, 1),
            CkColor.SideButtonBG        => new Vector4(0.451f, 0.451f, 0.451f, 0.25f),

            // UI Editors.
            CkColor.FancyHeader         => new Vector4(0.579f, 0.170f, 0.359f, 0.828f),
            CkColor.FancyHeaderContrast => new Vector4(0.100f, 0.022f, 0.022f, 0.299f),
            CkColor.ElementHeader       => new Vector4(1     , 0.181f, 0.715f, 0.825f),
            CkColor.ElementSplit        => new Vector4(0.180f, 0.180f, 0.180f, 1),
            CkColor.ElementBG           => new Vector4(1     , 0.742f, 0.910f, 0.416f),
            
            CkColor.FolderExpanded      => new Vector4(0.753f, 0.941f, 1.000f, 1.000f),
            CkColor.FolderCollapsed     => new Vector4(0.753f, 0.941f, 1.000f, 1.000f),
            CkColor.FolderLine          => new Vector4(0.753f, 0.941f, 1.000f, 1.000f),
            
            CkColor.FavoriteStarOn      => new Vector4(0.816f, 0.816f, 0.251f, 1.000f),
            CkColor.FavoriteStarHovered => new Vector4(0.816f, 0.251f, 0.816f, 1.000f),
            CkColor.FavoriteStarOff     => new Vector4(0.502f, 0.502f, 0.502f, 0.125f),
            
            CkColor.TriStateCheck       => new Vector4(0.000f, 0.816f, 0.000f, 1.000f),
            CkColor.TriStateCross       => new Vector4(0.816f, 0.000f, 0.000f, 1.000f),
            CkColor.TriStateNeutral     => new Vector4(0.816f, 0.816f, 0.816f, 1.000f),

            _ => Vector4.Zero,
        };

    public static uint Uint(this CkColor color)
        => color switch
        {
            CkColor.VibrantPink             => 0xE9A360F9,
            CkColor.VibrantPinkHovered      => 0xF3B076FB,
            CkColor.VibrantPinkPressed      => 0xC48546D7,

            CkColor.LushPinkLine            => 0xFF671ACD,
            CkColor.LushPinkButton          => 0xFF750DFF,

            CkColor.RemotePlaybackBg        => 0xED0A0A0A,
            CkColor.RemoteInterfaceBg       => 0xED1C1C1C,
            CkColor.RemoteInterfaceBgAlt    => 0xED191919,

            CkColor.ButtonDrag              => 0xED181818,
            CkColor.SideButton              => 0xFF737373,
            CkColor.SideButtonBG            => 0x3F737373,

            // Main UI
            CkColor.FancyHeader             => Vec4ToUint(CkColor.FancyHeader),
            CkColor.FancyHeaderContrast     => Vec4ToUint(CkColor.FancyHeaderContrast),
            CkColor.ElementHeader           => 0xD2B62EFF,
            CkColor.ElementSplit            => 0xFF2D2D2D,
            CkColor.ElementBG               => 0x6AE8BDFF,

            CkColor.FolderExpanded          => 0xFFFFF0C0,
            CkColor.FolderCollapsed         => 0xFFFFF0C0,
            CkColor.FolderLine              => 0xFF40D0D0,

            CkColor.TriStateCheck           => 0xFFD040D0,
            CkColor.TriStateCross           => 0x20808080,
            CkColor.TriStateNeutral         => 0xFF00D000,

            CkColor.IconCheckOn             => 0xFF0000D0,
            CkColor.IconCheckOff            => 0xFFD0D0D0,
            _                               => 0x00000000,
        };

    // Helper functions for when we add new colors
    public static uint Vec4ToUint(CkColor color)
    {
        var col = Vec4(color);
            uint ret = (byte)(col.W * 255);
            ret <<= 8;
            ret += (byte)(col.Z * 255);
            ret <<= 8;
            ret += (byte)(col.Y * 255);
            ret <<= 8;
            ret += (byte)(col.X * 255);
            return ret;
    }

    // Helper functions for when we add new colors
    public static Vector4 UintToVector4(uint color)
    {
        var w = ((color >> 24) & 0xFF) / 255f; // Alpha
        var z = ((color >> 16) & 0xFF) / 255f; // Blue
        var y = ((color >> 8) & 0xFF) / 255f; // Green
        var x = (color & 0xFF) / 255f; // Red

        return new Vector4(x, y, z, w);
    }

    // Down the line if we want we can add custom color themes from a palatte here.
}
