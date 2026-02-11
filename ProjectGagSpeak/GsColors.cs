using CkCommons;
using Dalamud.Bindings.ImGui;
using GagSpeak.PlayerClient;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace GagSpeak;

public struct GagSpeakTheme
{
    public string Name { get; init; }
    public string Author { get; init; }
    public string Description { get; init; }
    public Dictionary<GsCol, uint> Colors { get; init; }
    public Dictionary<CkCol, uint> CkColors { get; init; }
    public Dictionary<ImGuiCol, uint> ImGuiColors { get; init; }
    // Maybe some way to indicate which should remain untouched or something idk.
    public ImGuiStyle StyleRef { get; init; }
}

public enum GsCol
{
    // Rename these later when we see them easier in an editor
    VibrantPink,
    VibrantPinkHovered,
    VibrantPinkPressed,

    ShopKeeperColor,
    ShopKeeperText,

    LushPinkLine,
    LushPinkLineDisabled,
    LushPinkButton,
    LushPinkButtonDisabled,

    RemoteBg,
    RemoteBgDark,
    RemoteLines,

    ButtonDrag,

    SideButton,
    SideButtonBG,
}

/// <summary>
///     Highly optimized Color storage container with room for theme application. <br />
///     GSColors include colors used distinctly by GS for its coloring.
///     Can also contain colors intended to override the CkStyle colors.
/// </summary>
public static class GsColors
{
    public static readonly int          Count   = Enum.GetValues<GsCol>().Length;
    private static readonly Vector4[]   _vec4   = new Vector4[Count];
    private static readonly uint[]      _u32    = new uint[Count];

    // Static constructor runs once, ensures _vec4 and _u32 are populated immediately
    static GsColors()
    {
        foreach (var kvp in Defaults)
        {
            int index = (int)kvp.Key;
            _vec4[index] = kvp.Value;
            _u32[index] = kvp.Value.ToUint();
        }
    }

    public static Dictionary<GsCol, Vector4> AsVec4Dictionary()
        => Enumerable.Range(0, Count).ToDictionary(i => (GsCol)i, i => _vec4[i]);

    public static Dictionary<GsCol, uint> AsUintDictionary()
        => Enumerable.Range(0, Count).ToDictionary(i => (GsCol)i, i => _u32[i]);

    public static void SetColors(MainConfig config)
    {
        foreach (var kvp in config.GsColors)
            Set(kvp.Key, kvp.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(GsCol var, Vector4 col)
    {
        _vec4[(int)var] = col;
        _u32[(int)var] = col.ToUint();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(GsCol var, uint col)
    {
        _u32[(int)var] = col;
        _vec4[(int)var] = col.ToVec4();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RevertCol(GsCol col)
    {
        var defaultCol = Defaults[col];
        _vec4[(int)col] = defaultCol;
        _u32[(int)col] = defaultCol.ToUint();
    }

    public static void RevertAll()
    {
        foreach (var kvp in Defaults)
        {
            int index = (int)kvp.Key;
            _vec4[index] = kvp.Value;
            _u32[index] = kvp.Value.ToUint();
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Uint(this GsCol col) => _u32[(int)col];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Vec4(this GsCol col) => _vec4[(int)col];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector4 Vec4Ref(this GsCol col) => ref _vec4[(int)col];

    public static uint ToUint(this Vector4 color)
    {
        var r = (byte)(color.X * 255);
        var g = (byte)(color.Y * 255);
        var b = (byte)(color.Z * 255);
        var a = (byte)(color.W * 255);

        return (uint)((a << 24) | (b << 16) | (g << 8) | r);
    }

    public static Vector4 ToVec4(this uint color)
    {
        var r = (color & 0x000000FF) / 255f;
        var g = ((color & 0x0000FF00) >> 8) / 255f;
        var b = ((color & 0x00FF0000) >> 16) / 255f;
        var a = ((color & 0xFF000000) >> 24) / 255f;
        return new Vector4(r, g, b, a);
    }

    /// <summary>
    ///     Converts the colors to the config dictionary format.
    /// </summary>
    public static Dictionary<GsCol, uint> ToConfigDict()
    {
        var dict = new Dictionary<GsCol, uint>(Count);
        for (int i = 0; i < Count; i++)
            dict[(GsCol)i] = _u32[i];
        return dict;
    }

    // Default color mapping from CkCol (example, fill in your actual colors)
    public static readonly IReadOnlyDictionary<GsCol, Vector4> Defaults = new Dictionary<GsCol, Vector4>
    {
        { GsCol.VibrantPink,             new Vector4(0.977f, 0.380f, 0.640f, 0.914f) },
        { GsCol.VibrantPinkHovered,      new Vector4(0.986f, 0.464f, 0.691f, 0.955f) },
        { GsCol.VibrantPinkPressed,      new Vector4(0.846f, 0.276f, 0.523f, 0.769f) },

        { GsCol.ShopKeeperColor,         new Vector4(0.886f, 0.407f, 0.658f, 1.000f) },
        { GsCol.ShopKeeperText,          new Vector4(1.000f, 0.711f, 0.843f, 1.000f) },

        { GsCol.LushPinkLine,            new Vector4(0.806f, 0.102f, 0.407f, 1.000f) },
        { GsCol.LushPinkLineDisabled,    new Vector4(0.806f, 0.102f, 0.407f, 0.500f) },
        { GsCol.LushPinkButton,          new Vector4(1.000f, 0.051f, 0.462f, 1.000f) },
        { GsCol.LushPinkButtonDisabled,  new Vector4(1.000f, 0.051f, 0.462f, 0.500f) },

        { GsCol.RemoteBg,                new Vector4(0.122f, 0.122f, 0.161f, 1.000f) },
        { GsCol.RemoteBgDark,            new Vector4(0.090f, 0.090f, 0.122f, 1.000f) },
        { GsCol.RemoteLines,             new Vector4(0.404f, 0.404f, 0.404f, 1.000f) },

        { GsCol.ButtonDrag,              new Vector4(0.097f, 0.097f, 0.097f, 0.930f) },

        { GsCol.SideButton,              new Vector4(0.451f, 0.451f, 0.451f, 1.000f) },
        { GsCol.SideButtonBG,            new Vector4(0.451f, 0.451f, 0.451f, 0.250f) },
    };

    public static string ToName(this GsCol idx) => idx switch
    {
        GsCol.VibrantPink               => "Vibrant Pink",
        GsCol.VibrantPinkHovered        => "Vibrant Pink (Hovered)",
        GsCol.VibrantPinkPressed        => "Vibrant Pink (Pressed)",

        GsCol.ShopKeeperColor           => "Shopkeeper Color",
        GsCol.ShopKeeperText            => "Shopkeeper Text",

        GsCol.LushPinkLine              => "Lush Pink Line",
        GsCol.LushPinkLineDisabled      => "Lush Pink Line (Disabled)",
        GsCol.LushPinkButton            => "Lush Pink Button",
        GsCol.LushPinkButtonDisabled    => "Lush Pink Button (Disabled)",

        GsCol.RemoteBg                  => "Remote Background",
        GsCol.RemoteBgDark              => "Remote Background (Dark)",
        GsCol.RemoteLines               => "Remote Lines",

        GsCol.ButtonDrag                => "Button Drag",

        GsCol.SideButton                => "Side Button",
        GsCol.SideButtonBG              => "Side Button Background",

        _ => idx.ToString()
    };

    public static void Vec4ToClipboard(Dictionary<GsCol, Vector4> cols)
    {
        if (cols is null || cols.Count is 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"public static readonly Dictionary<GsCol, Vector4> TEMPLATE = new Dictionary<GsCol, Vector4>");
        sb.AppendLine("{");

        var maxEnumLen = cols.Keys.Max(k => k.ToString().Length);
        foreach (var kvp in cols.OrderBy(k => (int)k.Key))
        {
            var name = kvp.Key.ToString().PadRight(maxEnumLen);
            var v = kvp.Value;
            sb.AppendLine($"    {{ GsCol.{name}, new Vector4({v.X:0.###}f, {v.Y:0.###}f, {v.Z:0.###}f, {v.W:0.###}f) }},");
        }
        sb.AppendLine("};");

        Clipboard.SetText(sb.ToString());
    }

    public static void UintToClipboard(Dictionary<GsCol, uint> cols)
    {
        if (cols is null || cols.Count is 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"public static readonly IReadOnlyDictionary<GsCol, uint> TEMPLATE = new Dictionary<GsCol, uint>");
        sb.AppendLine("{");

        var maxEnumLen = cols.Keys.Max(k => k.ToString().Length);
        foreach (var kvp in cols.OrderBy(k => (int)k.Key))
            sb.AppendLine($"    {{ GsCol.{kvp.Key.ToString().PadRight(maxEnumLen)}, 0x{kvp.Value:X8} }},");
        sb.AppendLine("};");

        Clipboard.SetText(sb.ToString());
    }


}
