using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Raii;
using GagSpeak.Services.Textures;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Buffers.Binary;
using System.Text.RegularExpressions;

namespace GagSpeak.CkCommons;

/// <summary>
///     Represents a rich text string that can be colored, have images, or be bolded.
///     Payloads can add and take away from effects, and must be maintained in a style formatter.
/// </summary>
public class RichTextString
{
    // The payloads to display at render time.
    public List<RichPayload> Payloads { get; } = new();
    private readonly Stack<uint> _strokeColorStack = new();

    // Useful to know when we should recalculate.
    public ImFontPtr LastFont { get; private set; }
    public float LastWrapWidth { get; private set; }
    public uint CurrentStrokeColor => _strokeColorStack.Count > 0 ? _strokeColorStack.Peek() : 0xFF000000;

    private bool IsValid = false;
    private string RawText => string.Concat(Payloads.OfType<TextPayload>().Select(p => p.Text));

    /// <summary>
    ///     You must call <see cref="UpdateCaches(ImFontPtr, float)"/> after construction to ensure the caches are valid.
    /// </summary>
    public RichTextString(string rawString)
    {
        BuildPayloads(rawString);
        // An update cache will trigger after this due to the lastFont and width not being up to date.
    }


    // must be manually invoked after construction.
    public unsafe void UpdateCaches(ImFontPtr font, float wrapWidth)
    {
        // update the font and wrap width to the new value.
        LastFont = font;
        LastWrapWidth = wrapWidth;
        // Update the individual caches to respect the new font and wrap width.
        var currentLineWidth = 0f;
        foreach (var payload in Payloads)
        {
            Svc.Logger.Debug($"[RichText] Compiling [{payload.GetType().Name}] Payload, beginning at {currentLineWidth} width.");
            payload.UpdateCache(font, wrapWidth, ref currentLineWidth);
            Svc.Logger.Debug($"[RichText] Complied [{payload.GetType().Name}] Payload ending at {currentLineWidth} width.");
        }
    }

    /// <summary>
    ///     Renders the combined richText for display. It is up to you to make sure the caches are valid.
    /// </summary>
    public void Render(ImFontPtr font, float wrapWidth)
    {
        // if there is a missmatch with the font pointer and wrapwidth, recalculate.
        if (!MatchesCachedState(font, wrapWidth))
        {
            Svc.Logger.Information($"[RichText] Recalculating caches for font {font.GetDebugName()} and wrap width {wrapWidth}.");
            UpdateCaches(font, wrapWidth);
        }

        // If not valid, just display the textwrap unformatted.
        if (!IsValid)
        {
            ImGui.TextWrapped(RawText);
            return;
        }

        // Display the rich text.
        foreach (var payload in Payloads)
            payload.Draw();

        // remove all pushed colors.
        _colorStack.Dispose();
    }

    public unsafe bool MatchesCachedState(ImFontPtr font, float wrapWidth)
        => LastFont.NativePtr == font.NativePtr && LastWrapWidth == wrapWidth;

    public void BuildPayloads(string rawText)
    {
        var result = Regex.Split(rawText, @"(\[color=[0-9a-z#]+\])|(\[\/color\])|(\[stroke=[0-9a-z#]+\])|(\[\/stroke\])|(\[img=[^\]]+\])|(\[emote=[^\]]+\])|(\[para\])|(\[line\])", RegexOptions.IgnoreCase);
        int[] valid = [0, 0]; // [color, stroke]
        var sw = new Stopwatch();
        sw.Start();
        try
        {
            foreach (var part in result)
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                // off switches.
                switch (part)
                {
                    case "[line]":
                        Payloads.Add(new SeperatorPayload());
                        continue;
                    case "[para]":
                        Payloads.Add(new NewLinePayload());
                        continue;
                    case "[/color]":
                        Payloads.Add(ColorPayload.Off);
                        valid[0]--;
                        continue;
                    case "[/stroke]":
                        Payloads.Add(StrokePayload.Off);
                        valid[1]--;
                        continue;
                }

                // On Switches
                if (part.StartsWith("[color=", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseColor(part[7..^1], out var color))
                        throw new Exception($"[RichText] Invalid [color] tag value: {part}");
                    
                    Payloads.Add(new ColorPayload(color));
                    valid[0]++;                    
                    continue;
                }

                if (part.StartsWith("[stroke=", StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryParseColor(part[8..^1], out var stroke))
                        throw new Exception($"[RichText] Invalid [stroke] tag value: {part}");
                    
                    Payloads.Add(new StrokePayload(stroke));
                    valid[1]++;
                    continue;
                }

                if (part.StartsWith("[img=", StringComparison.OrdinalIgnoreCase))
                {
                    var path = part[5..^1]; // strip [img= and ]
                    Payloads.Add(new ImagePayload(path));
                    continue;
                }

                if (part.StartsWith("[emote=", StringComparison.OrdinalIgnoreCase))
                {
                    var name = part[7..^1];
                    if (!Enum.TryParse<CoreEmoteTexture>(name, true, out var emote))
                        throw new Exception($"[RichText] Invalid [emote] tag value: {name}");

                    Payloads.Add(new ImagePayload(() => CosmeticService.CoreEmoteTextures.GetValueOrDefault(emote)));
                    continue;
                }

                // Otherwise just normal text payload.
                Payloads.Add(new TextPayload(part));
            }
            // all were valid.
            IsValid = true;
        }
        catch (Exception ex)
        {
            Svc.Logger.Error($"Error while parsing rich text string: {rawText}\n{ex}");
            Payloads.Clear();
            Payloads.Add(new TextPayload($"BAD_SYNTAX"));
            IsValid = false;
        }
        finally
        {
            sw.Stop();
            Svc.Logger.Information($"[RichText] Parsed {Payloads.Count} payloads in {sw.ElapsedMilliseconds}ms. Colors: {valid[0]}, Strokes: {valid[1]}");
        }
    }

    private bool TryParseColor(string value, out uint color)
    {
        color = 0;
        // attempt to get the row id.
        if (ushort.TryParse(value, out var rowId))
        {
            // if it was vaid, get the UIColor row.
            if (Svc.Data.GetExcelSheet<UIColor>().GetRowOrDefault(rowId) is { } row && rowId != 0)
            {
                // the color will be the reverse endianness of the Dark value.
                color = BinaryPrimitives.ReverseEndianness(row.Dark);
                return true;
            }
        }
        // otherwise, it might be a named color, so try that.
        else if (Enum.TryParse<XlDataUiColor>(value, true, out var namedColor))
        {
            // if valid, grab the rowId of that result.
            rowId = (ushort)namedColor;
            if (Svc.Data.GetExcelSheet<UIColor>().GetRowOrDefault(rowId) is { } row && rowId != 0)
            {
                color = BinaryPrimitives.ReverseEndianness(row.Dark);
                return true;
            }
        }
        return false;
    }
}
