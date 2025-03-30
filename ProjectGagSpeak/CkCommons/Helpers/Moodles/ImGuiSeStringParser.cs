using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.UI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Buffers.Binary;

namespace GagSpeak.CkCommons.Helpers;

public static class ImGuiSeStringParser
{
    private static string           _lastString;
    private static ParsedSeString   _lastParsedString;

    public static void DisplayMoodleString(string moodleEncodedString, IDataManager data)
    {
        // Grab from the cache instead of re-parsing if the string is the same. This avoids overhead from drawframes.
        if (string.Equals(moodleEncodedString, _lastString, StringComparison.OrdinalIgnoreCase))
        {
            _lastParsedString.RenderText(ImGui.GetContentRegionAvail().X);
            return;
        }

        // Otherwise, parse it then render it.
        var parsed = ParseMoodleSeStringInternal(moodleEncodedString, data, out var error);
        if (!parsed.RawString.IsNullOrWhitespace())
        {
            // Cache it if valid.
            _lastString = moodleEncodedString;
            _lastParsedString = parsed;
            parsed.RenderText(ImGui.GetContentRegionAvail().X);
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, error);
        }
    }

    /// <summary> Parses a Moodle Formatted string into a ParsedSeString. The parse from moodles goes in an order such as:
    /// <para> [glow = pink][color= red]Love Heart[/ color][/ glow]Applies reduced [color = yellow]Focus[/ color] by 10 %. </para>
    /// We need to follow a small set of rules during the parse.
    /// <list type="number">
    /// <item> If a modifier is found, append it to the current flags. </item>
    /// <item> Once normal text is found, append it to the item. </item>
    /// <item> after text is found, process the removal of added modifiers. </item>
    /// <item> Once all are removed after a text is added, append the chunk. </item>
    /// <item> Must Remember to reject on duplicates and on additional text inputs where mismatch occurs. </item>
    /// </list>
    /// </summary>
    /// <param name="text"> the moodle-encoded-string. </param>
    /// <param name="error"> The error string output if failure occurs. </param>
    /// <returns> the parsed moodle string ready to be rendered for display. </returns>
    private static ParsedSeString ParseMoodleSeStringInternal(string text, IDataManager data, out string error)
    {
        // assume no error.
        error = string.Empty;
        var result = RegexEx.SplitRegex().Split(text);
        var parsedTextBuilder = new List<ParsedSeStringText>();

        try
        {
            // store the valid count for each attribute.
            ParsedSeStringText currentChunkText = ParsedSeStringText.Empty;
            ParsedSeStringFlags currentChunkFlags = ParsedSeStringFlags.None;

            // for each found result within the split.
            foreach (var str in result)
            {
                // if it contained nothing, skip to the next.
                if (str == string.Empty)
                    continue;

                // if it contains the unique color tag identifier...
                if (str.StartsWith("[color=", StringComparison.OrdinalIgnoreCase))
                {
                    // If we are trying to execute this while there is stored text, process the previous item chunk first.
                    if (!currentChunkText.Text.IsNullOrEmpty())
                        AppendChunk(str);

                    // If we already have a modifier and it's a different one, throw an error.
                    if ((currentChunkFlags & ParsedSeStringFlags.HasForeground) != 0)
                        throw new Exception("Error: Cannot add modifier while previous modifiers are active.");

                    // Given "[color=***************[/color]" we want to parse out **** region.
                    // This will work if we provide it with a raw number, like 351, but if we give it "ParsedPink" it will fail.
                    var success = ushort.TryParse(str[7..^1], out var r);

                    // If it did fail, attempt to see if we named it any of the colors that are reconized by dalamud using the same method.
                    if (!success)
                        r = (ushort)Enum.GetValues<XlDataUiColor>().FirstOrDefault(x => x.ToString().Equals(str[7..^1], StringComparison.OrdinalIgnoreCase));

                    // If the end result was WhitNormal, or the resulting value r is not present in the datasheet, throw a color error.
                    if (data.GetExcelSheet<UIColor>().GetRowOrDefault(r) is { } validUICol && r != 0)
                    {
                        // convert the forground to imgui u32 format.
                        var col = BinaryPrimitives.ReverseEndianness(validUICol.Dark);
                        // Add the color modifier
                        currentChunkFlags |= ParsedSeStringFlags.HasForeground;
                        currentChunkText.AddColor(col);
                    }
                    else throw new Exception("Error: Color is out of range.");
                }
                // If the string contains the unique color tag closing identifier, turn off the foreground.
                else if (str.Equals("[/color]", StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure color was previously applied
                    if ((currentChunkFlags & ParsedSeStringFlags.HasForeground) == 0)
                        throw new Exception("Error: Mismatched [color] closing tag. [String Attempted: " + str + "] [Current Flags: " + currentChunkFlags + "]");

                    currentChunkFlags &= ~ParsedSeStringFlags.HasForeground;
                }

                // if the string contains the unique glow tag identifier...
                else if (str.StartsWith("[glow=", StringComparison.OrdinalIgnoreCase))
                {
                    // If we are trying to execute this while there is stored text, process the previous item chunk first.
                    if (!currentChunkText.Text.IsNullOrEmpty())
                        AppendChunk(str);

                    if ((currentChunkFlags & ParsedSeStringFlags.HasGlow) != 0)
                        throw new Exception("Error: Cannot add modifier while previous modifiers are active. [String Attempted: " + str + "] [Current Flags: " + currentChunkFlags + "]");

                    // Given "[glow=***************[/glow]" we want to parse out **** region.
                    // This will work if we provide it with a raw number, like 351, but if we give it "ParsedPink" it will fail.
                    var success = ushort.TryParse(str[6..^1], out var r);
                    // If it did fail, attempt to see if we named it any of the colors that are reconized by dalamud using the same method.
                    if (!success)
                        r = (ushort)Enum.GetValues<XlDataUiColor>().FirstOrDefault(x => x.ToString().Equals(str[6..^1], StringComparison.OrdinalIgnoreCase));

                    // If the end result was WhitNormal, or the resulting value r is not present in the datasheet, throw a color error.
                    if (r == 0 || data.GetExcelSheet<UIColor>().GetRowOrDefault(r) is not { } validUIGlow)
                        throw new Exception("Error: Glow is out of range.");

                    // Add the glow modifier

                    // convert the forground to imgui u32 format.
                    var glow = BinaryPrimitives.ReverseEndianness(validUIGlow.Light);
                    currentChunkFlags |= ParsedSeStringFlags.HasGlow;
                    currentChunkText.AddGlow(glow);
                }
                // If the string contains the unique glow tag closing identifier, turn off the glow.
                else if (str.Equals("[/glow]", StringComparison.OrdinalIgnoreCase))
                {
                    if ((currentChunkFlags & ParsedSeStringFlags.HasGlow) == 0)
                        throw new Exception("Error: Mismatched [glow] closing tag.");

                    currentChunkFlags &= ~ParsedSeStringFlags.HasGlow;
                }

                // if the string contains the unique italics tag identifier...
                else if (str.Equals("[i]", StringComparison.OrdinalIgnoreCase))
                {
                    // If we are trying to execute this while there is stored text, process the previous item chunk first.
                    if (!currentChunkText.Text.IsNullOrEmpty())
                        AppendChunk(str);

                    if ((currentChunkFlags & ParsedSeStringFlags.HasItalic) != 0)
                        throw new Exception("Error: Cannot add modifier while previous modifiers are active. [String Attempted: " + str + "] [Current Flags: " + currentChunkFlags + "]");

                    currentChunkFlags |= ParsedSeStringFlags.HasItalic;
                    currentChunkText.AddItalic();
                }
                // If the string contains the unique italics tag closing identifier, turn off the italics.
                else if (str.Equals("[/i]", StringComparison.OrdinalIgnoreCase))
                {
                    if ((currentChunkFlags & ParsedSeStringFlags.HasItalic) == 0)
                        throw new Exception("Error: Mismatched [i] closing tag.");

                    currentChunkFlags &= ~ParsedSeStringFlags.HasItalic;
                }
                // otherwise simply append the text.
                else
                {
                    // The next request is to append text. However, if text is already present, lets first append that before we add.
                    if (!currentChunkText.Text.IsNullOrEmpty())
                        AppendChunk(str);

                    currentChunkText.Text += str;
                }
            }

            // If there's any remaining chunk after parsing all items, append it.
            if (currentChunkFlags is ParsedSeStringFlags.None && !string.IsNullOrEmpty(currentChunkText.Text))
            {
                // _logger.LogDebug("Adding New Chunk Flat: " + currentChunkText.ToString());
                parsedTextBuilder.Add(currentChunkText);
            }

            // Construct the final ParsedSeString and return it
            return new ParsedSeString(text, parsedTextBuilder.ToArray());

            void AppendChunk(string str)
            {
                if (currentChunkFlags is not 0)
                {
                    throw new Exception("Error: Cannot add text while previous modifiers are active which have not yet been closed.\n" +
                        "[String Attempted: " + str + "] [Current Flags: " + currentChunkFlags + "] [Stored Text: " + currentChunkText.Text + "]");
                }

                // The current flags are 0, so we can properly append and reset status.
                // _logger.LogDebug("Adding New Chunk: " + currentChunkText.ToString());
                parsedTextBuilder.Add(currentChunkText);
                currentChunkText = ParsedSeStringText.Empty;
                currentChunkFlags = ParsedSeStringFlags.None;
            }

        }
        catch (Exception e)
        {
            var parsedState = string.Join("\n", parsedTextBuilder.Select(t => t.Text));
            error = $"Please check syntax:\n{e.Message}\n\nCurrent Parsed State:\n{parsedState}";
            return ParsedSeString.Empty;
        }
    }
}
