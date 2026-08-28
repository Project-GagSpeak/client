using GagSpeak.PlayerClient;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ImSharp;

namespace GagSpeak;

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
public struct ColorMod
{
    public GsCol Var;
    public Vector4 BackupVec4;
    public uint BackupU32;
}

/// <summary>
///   Highly optimized Color storage container with room for theme application. <br />
///   GSColors include colors used distinctly by GS for its coloring.
///   Can also contain colors intended to override the CkStyle colors.
/// </summary>
public static class GsColors
{
    // Placeholder Colors!
    public static readonly Vector4 BgCol = new Vector4(0.055f, 0.063f, 0.078f, 0.75f);
    public static readonly Vector4 ActionBar = new Vector4(0.039f, 0.043f, 0.063f, 0.75f);
    public static readonly Vector4 RibbonTop = new Vector4(0.047f, 0.055f, 0.071f, 0.85f);
    public static readonly Vector4 RibbonBot = new Vector4(0.031f, 0.039f, 0.051f, 0.85f);
    public static readonly Vector4 BorderSoft = new Vector4(0.245f, 0.257f, 0.304f, 1.0f);
    public static readonly Vector4 SurfaceCol = new Vector4(0.055f, 0.063f, 0.078f, 0.75f);


    public static readonly int Count = GsCol.Values.Count;
    private static readonly Vector4[] _vec4 = new Vector4[Count];
    private static readonly uint[] _u32 = new uint[Count];

    private static readonly ColorMod[] _stack = new ColorMod[256];
    private static int _stackTop;

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
    public static int StackSize => _stackTop;

    public static Dictionary<GsCol, Vector4> AsVec4Dictionary()
        => Enumerable.Range(0, Count).ToDictionary(i => (GsCol)i, i => _vec4[i]);

    public static Dictionary<GsCol, uint> AsUintDictionary()
        => Enumerable.Range(0, Count).ToDictionary(i => (GsCol)i, i => _u32[i]);

    public static uint Uint(this GsCol col)
        => _u32[(int)col];

    public static Vector4 Vec4(this GsCol col)
        => _vec4[(int)col];

    public static void SetColors(MainConfig config)
    {
        foreach (var kvp in config.GsColors)
            Set(kvp.Key, kvp.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Set(GsCol var, Vector4 col)
    {
        Debug.Assert(_stackTop == 0, "Do not modify base colors while a stack is active!");
        _vec4[(int)var] = col;
        _u32[(int)var] = col.ToUint();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void Set(GsCol var, uint col)
    {
        Debug.Assert(_stackTop == 0, "Do not modify base colors while a stack is active!");
        _u32[(int)var] = col;
        _vec4[(int)var] = col.ToVec4();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RevertCol(GsCol col)
    {
        Debug.Assert(_stackTop == 0, "Do not revert base colors while a stack is active!");
        var defaultCol = Defaults[col];
        _vec4[(int)col] = defaultCol;
        _u32[(int)col] = defaultCol.ToUint();
    }

    public static void RevertAll()
    {
        Debug.Assert(_stackTop == 0, "Do not revert base colors while a stack is active!");
        foreach (var kvp in Defaults)
        {
            int index = (int)kvp.Key;
            _vec4[index] = kvp.Value;
            _u32[index] = kvp.Value.ToUint();
        }
    }

    // Maybe apply AggressiveOptimization to these if we get better performance with it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushColor(GsCol var, Vector4 color)
    {
        Debug.Assert(_stackTop < _stack.Length, "Stack overflow in PushColor");
        int index = (int)var;
        // Backup both uint and vec4 instantly
        _stack[_stackTop++] = new ColorMod
        {
            Var = var,
            BackupVec4 = _vec4[index],
            BackupU32 = _u32[index]
        };
        // Apply override
        _vec4[index] = color;
        _u32[index] = color.ToUint();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushColor(GsCol var, uint color)
    {
        Debug.Assert(_stackTop < _stack.Length, "Stack overflow in PushColor");
        int index = (int)var;
        _stack[_stackTop++] = new ColorMod
        {
            Var = var,
            BackupVec4 = _vec4[index],
            BackupU32 = _u32[index]
        };
        _u32[index] = color;
        _vec4[index] = color.ToVec4();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PopColor(int count = 1)
    {
        Debug.Assert(_stackTop >= count, "Stack underflow in PopColor");
        while (count-- > 0)
        {
            var mod = _stack[--_stackTop];
            int index = (int)mod.Var;
            // Restore instantly without conversions
            _vec4[index] = mod.BackupVec4;
            _u32[index] = mod.BackupU32;
        }
    }


    #region Disposable Usings
    /// <summary> 
    ///   Starts a chainable, disposable color push.
    /// </summary>
    public static ColorDisposable Push(GsCol var, Vector4 color, bool condition = true)
        => new ColorDisposable().Push(var, color, condition);

    /// <summary>
    ///   Starts a chainable, disposable color push.
    /// </summary>
    public static ColorDisposable Push(GsCol var, uint color, bool condition = true)
        => new ColorDisposable().Push(var, color, condition);

    /// <summary>
    ///   Automatically tracks and pops pushed colors when disposed.
    /// </summary>
    public struct ColorDisposable : IDisposable
    {
        public int PushedCount { get; private set; }

        public ColorDisposable Push(GsCol var, Vector4 color, bool condition = true)
        {
            if (condition)
            {
                PushColor(var, color);
                PushedCount++;
            }
            return this; // Returns itself to allow chaining
        }

        public ColorDisposable Push(GsCol var, uint color, bool condition = true)
        {
            if (condition)
            {
                PushColor(var, color);
                PushedCount++;
            }
            return this;
        }

        public void Dispose()
        {
            if (PushedCount > 0)
            {
                PopColor(PushedCount);
                PushedCount = 0;
            }
        }
    }
    #endregion
    // Check about MethodImpl later for the below methods, they likely need it more.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(this Vector4 color)
    {
        var r = (byte)(color.X * 255);
        var g = (byte)(color.Y * 255);
        var b = (byte)(color.Z * 255);
        var a = (byte)(color.W * 255);
        return (uint)((a << 24) | (b << 16) | (g << 8) | r);
    }

    // Might need to invert?
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(this Vector3 color)
            => byte.CreateSaturating(color.X * 255.0f) | ((uint)byte.CreateSaturating(color.Y * 255.0f) << 8) | ((uint)byte.CreateSaturating(color.Z * 255.0f) << 16) | (0xFFu << 24);

    // Might need to invert?
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVec3(this uint color)
        => unchecked(new((byte)color / 255.0f, (byte)(color >> 8) / 255.0f, (byte)(color >> 16) / 255.0f));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ToVec4(this uint color)
    {
        var r = (color & 0x000000FF) / 255f;
        var g = ((color & 0x0000FF00) >> 8) / 255f;
        var b = ((color & 0x00FF0000) >> 16) / 255f;
        var a = ((color & 0xFF000000) >> 24) / 255f;
        return new Vector4(r, g, b, a);
    }

    /// <summary>
    ///   Converts the colors to the config dictionary format.
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
