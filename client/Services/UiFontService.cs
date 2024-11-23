using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;

namespace GagSpeak.Services;

/// <summary> Manages GagSpeaks custom fonts during plugin lifetime. </summary>
public sealed class UiFontService : IDisposable
{
    private readonly IDalamudPluginInterface _pi;
    public IFontHandle GameFont { get; private set; }
    public IFontHandle IconFont { get; private set; }
    public IFontHandle UidFont { get; private set; }
    // the below 3 are the same font at different sizes because idk how to register seperate sizes.
    public IFontHandle GagspeakFont { get; private set; }
    public IFontHandle GagspeakLabelFont { get; private set; }
    public IFontHandle GagspeakTitleFont { get; private set; }
    public UiFontService(IDalamudPluginInterface pi)
    {
        _pi = pi;

        // the special gagspeak font that i cant ever get to load for some wierd ass reason.
        var gagspeakFontFile = Path.Combine(_pi.AssemblyLocation.DirectoryName!, "Assets", "DoulosSIL-Regular.ttf");
        if (File.Exists(gagspeakFontFile))
        {
            // get the glyph ranges
            var glyphRanges = GetGlyphRanges();

            // create the font handle
            GagspeakFont = pi.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(
                tk => tk.AddFontFromFile(gagspeakFontFile, new SafeFontConfig { SizePx = 22, GlyphRanges = glyphRanges })));

            GagspeakLabelFont = _pi.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(
                tk => tk.AddFontFromFile(gagspeakFontFile, new SafeFontConfig { SizePx = 36, GlyphRanges = glyphRanges })));

            GagspeakTitleFont = _pi.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(
                tk => tk.AddFontFromFile(gagspeakFontFile, new SafeFontConfig { SizePx = 48, GlyphRanges = glyphRanges })));
        }

        // the font atlas for our UID display (make it the font from gagspeak probably unless this fits more)
        UidFont = _pi.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
        {
            e.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new()
            {
                SizePx = 35
            }));
        });

        // the font atlas for our game font
        GameFont = _pi.UiBuilder.FontAtlas.NewGameFontHandle(new(GameFontFamilyAndSize.Axis12));
        // the font atlas for our icon font
        IconFont = _pi.UiBuilder.IconFontFixedWidthHandle;
    }

    public void Dispose()
    {
        GagspeakFont?.Dispose();
        GagspeakLabelFont?.Dispose();
        GagspeakTitleFont?.Dispose();
        UidFont?.Dispose();
        GameFont?.Dispose();
        IconFont?.Dispose();
    }

    private ushort[] GetGlyphRanges() // Used for the GagSpeak custom Font Service to be injected properly.
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
