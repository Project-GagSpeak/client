using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using ImGuiNET;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

/// <summary> Manages GagSpeaks custom fonts during plugin lifetimtk. </summary>
public sealed class UiFontService : IHostedService
{
    public static IFontHandle IconFont => Svc.PluginInterface.UiBuilder.IconFontFixedWidthHandle;
    public static IFontHandle UidFont { get; private set; }
    public static IFontHandle Default150Percent { get; private set; }
    public unsafe static ImFontPtr Default150PercentPtr { get; private set; }
    public static IFontHandle FullScreenFont { get; private set; }
    public unsafe static ImFontPtr FullScreenFontPtr { get; private set; }

    // the below 3 are the same font at different sizes because idk how to register seperate sizes.
    // You can implement font pointers if you want here, but due to the glyph allocations, it will take 12s to load.
    public static IFontHandle GagspeakFont { get; private set; }
    public static IFontHandle GagspeakLabelFont { get; private set; }
    public static IFontHandle GagspeakTitleFont { get; private set; }

    public UiFontService()
    {
        
    }

    private async Task InitializeAllFonts()
    {
        // Initialize the nessisary fonts.
        await InitNessisaryFonts().ConfigureAwait(false);
        // Build the fonts.
        await Svc.PluginInterface.UiBuilder.FontAtlas.BuildFontsAsync().ConfigureAwait(false);

        // Load the large font.
        InitLargeFonts();

        Svc.Logger.Information("UiFontService: Fonts initialized successfully.");
    }

    private async Task InitNessisaryFonts()
    {
        UidFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new() { SizePx = 35 }));
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

        // Wait for them to be valid.
        await UidFont.WaitAsync().ConfigureAwait(false);
        await GagspeakFont.WaitAsync().ConfigureAwait(false);
        await GagspeakLabelFont.WaitAsync().ConfigureAwait(false);
        await GagspeakTitleFont.WaitAsync().ConfigureAwait(false);
        Svc.Logger.Information("UiFontService: Initialized Nessisary fonts.");
    }

    private void InitLargeFonts()
    {
        Default150Percent = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(prebuild =>
            {
                Default150PercentPtr = prebuild.AddDalamudDefaultFont(UiBuilder.DefaultFontSizePx * 1.5f);
            });
        });

        FullScreenFont = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(prebuild =>
            {
                FullScreenFontPtr = prebuild.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new() { SizePx = 300 });
            });
        });
        Svc.Logger.Information("UiFontService: Initialized supported fonts.");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Svc.Logger.Information("UiFontService Started.");
        // Fire-And-Forget, we must not block the startup process.
        _ = InitializeAllFonts();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Svc.Logger.Information("UiFontService Stopped.");
        GagspeakFont?.Dispose();
        GagspeakLabelFont?.Dispose();
        GagspeakTitleFont?.Dispose();
        UidFont?.Dispose();
        FullScreenFont?.Dispose();
        return Task.CompletedTask;
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
