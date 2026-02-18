using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;

namespace GagSpeak.Services;

/// <summary>
///     Manages GagSpeaks custom fonts during plugin lifetime. <para />
///     Should probably look at chat2 to see how to handle 
///     pointers for fonts and various font scales better at some point.
/// </summary>
public static class Fonts
{
    public const int FULLSCREEN_FONT_SIZE = 300;
    public static IFontHandle FullscreenFont { get; private set; }
    public static ImFontPtr FullscreenFontPtr { get; private set; }

    // Gs-Related Fonts.
    public static IFontHandle GagspeakFont { get; private set; }
    public static IFontHandle GagspeakLabelFont { get; private set; }
    public static IFontHandle GagspeakTitleFont { get; private set; }

    // Normal Fonts
    public static IFontHandle IconFont => Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle;
    public static IFontHandle UidFont { get; private set; }
    public static IFontHandle Default150Percent { get; private set; }
    public static ImFontPtr Default150PercentPtr { get; private set; }

    /// <summary>
    ///     Helper task to initialize GagSpeak's fonts.
    /// </summary>
    public static async Task InitializeFonts()
    {
        // Attempt the large font first.
        FullscreenFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(prebuild =>
            {
                FullscreenFontPtr = prebuild.AddDalamudAssetFont(Dalamud.DalamudAsset.InconsolataRegular, new() { SizePx = 300 });
            });
        });

        // grab the file locat ion of the GagSpeak Font.
        var gsFontFileLoc = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Assets", "DoulosSIL-Regular.ttf");
        if (File.Exists(gsFontFileLoc))
        {
            // get the glyph ranges
            var glyphRanges = GetGlyphRanges();

            // Assign the IFontHandltk.
            GagspeakFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
            {
                tk.OnPreBuild(e => e.AddFontFromFile(gsFontFileLoc, new SafeFontConfig { SizePx = 22, GlyphRanges = glyphRanges }));
            });

            GagspeakLabelFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
            {
                tk.OnPreBuild(e => e.AddFontFromFile(gsFontFileLoc, new SafeFontConfig { SizePx = 36, GlyphRanges = glyphRanges }));
            });

            GagspeakTitleFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
            {
                tk.OnPreBuild(e => e.AddFontFromFile(gsFontFileLoc, new SafeFontConfig { SizePx = 48, GlyphRanges = glyphRanges }));
            });
        }

        // Now for UID and 150% default.
        UidFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new() { SizePx = 35 }));
        });

        Default150Percent = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(prebuild =>
            {
                Default150PercentPtr = prebuild.AddDalamudDefaultFont(UiBuilder.DefaultFontSizePx * 1.5f);
            });
        });

        // Wait for them to be valid.
        await UidFont.WaitAsync().ConfigureAwait(false);
        await Default150Percent.WaitAsync().ConfigureAwait(false);
        await GagspeakFont.WaitAsync().ConfigureAwait(false);
        await GagspeakLabelFont.WaitAsync().ConfigureAwait(false);
        await GagspeakTitleFont.WaitAsync().ConfigureAwait(false);
        await FullscreenFont.WaitAsync().ConfigureAwait(false);
        await Svc.PluginInterface.UiBuilder.FontAtlas.BuildFontsAsync().ConfigureAwait(false);
        Svc.Logger.Information("Fonts: Initialized Nessisary fonts.");
    }

    public static void Dispose()
    {
        Svc.Logger.Information("Disposing Fonts.");
        GagspeakFont?.Dispose();
        GagspeakLabelFont?.Dispose();
        GagspeakTitleFont?.Dispose();
        UidFont?.Dispose();
        Default150Percent?.Dispose();
        FullscreenFont?.Dispose();
    }

    private static ushort[] GetGlyphRanges() // Used for the GagSpeak custom Font Service to be injected properly.
    {
        return new ushort[] {
            0x0020, 0x007E,  // Basic Latin
            0x00A0, 0x00FF,  // Latin-1 Supplement
            0x0100, 0x017F,  // Latin Extended-A
            0x0180, 0x024F,  // Latin Extended-B
            0x0250, 0x02AF,  // IPA Extensions
            0x02B0, 0x02FF,  // Spacing Modifier Letters
            0x0300, 0x036F,  // Combining Diacritical Marks
            0x0370, 0x03FF,  // Greek and Coptic
            0x0400, 0x04FF,  // Cyrillic
            0x0500, 0x052F,  // Cyrillic Supplement
            0x1AB0, 0x1AFF,  // Combining Diacritical Marks Extended
            0x1D00, 0x1D7F,  // Phonetic Extensions
            0x1D80, 0x1DBF,  // Phonetic Extensions Supplement
            0x1DC0, 0x1DFF,  // Combining Diacritical Marks Supplement
            0x1E00, 0x1EFF,  // Latin Extended Additional
            0x2000, 0x206F,  // General Punctuation
            0x2070, 0x209F,  // Superscripts and Subscripts
            0x20A0, 0x20CF,  // Currency Symbols
            0x20D0, 0x20FF,  // Combining Diacritical Marks for Symbols
            0x2100, 0x214F,  // Letterlike Symbols
            0x2150, 0x218F,  // Number Forms
            0x2190, 0x21FF,  // Arrows
            0x2200, 0x22FF,  // Mathematical Operators
            0x2300, 0x23FF,  // Miscellaneous Technical
            0x2400, 0x243F,  // Control Pictures
            0x2440, 0x245F,  // Optical Character Recognition
            0x2460, 0x24FF,  // Enclosed Alphanumerics
            0x2500, 0x257F,  // Box Drawing
            0x2580, 0x259F,  // Block Elements
            0x25A0, 0x25FF,  // Geometric Shapes
            0x2600, 0x26FF,  // Miscellaneous Symbols
            0x2700, 0x27BF,  // Dingbats
            0x27C0, 0x27EF,  // Miscellaneous Mathematical Symbols-A
            0x27F0, 0x27FF,  // Supplemental Arrows-A
            0
        };
    }
}
