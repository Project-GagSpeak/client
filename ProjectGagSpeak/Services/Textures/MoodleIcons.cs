using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Buffers.Binary;

namespace GagSpeak.Services.Textures;

/// <summary>
///     Only keep this until we find a better way to store and display moodle icon information.
/// </summary>
public class MoodleIcons
{
    private readonly ILogger<MoodleIcons> _logger;
    public MoodleIcons(ILogger<MoodleIcons> logger)
    {
        _logger = logger;
    }

    public IDalamudTextureWrap? GetGameIconOrDefault(uint iconId)
        => Svc.Texture.GetFromGameIcon(iconId).GetWrapOrDefault();

    public IDalamudTextureWrap GetGameIconOrEmpty(uint iconId)
        => Svc.Texture.GetFromGameIcon(iconId).GetWrapOrEmpty();

    public IDalamudTextureWrap? GetGameIconOrDefault(int iconId, int stacks)
        => Svc.Texture.GetFromGameIcon(new GameIconLookup((uint)(iconId + stacks - 1))).GetWrapOrDefault();


    /// <summary>
    ///     Draws the Moodle icon. This only draw a single image so you can use IsItemHovered() outside.
    /// </summary>
    public void DrawMoodleIcon(int iconId, int stacks, Vector2 size)
    {
        if (Svc.Texture.GetFromGameIcon(new GameIconLookup((uint)(iconId + stacks - 1))).GetWrapOrDefault() is { } wrap)
            ImGui.Image(wrap.ImGuiHandle, size);
        else
            ImGui.Dummy(size);
    }

    public void DrawMoodleStatusTooltip(MoodlesStatusInfo item, IEnumerable<MoodlesStatusInfo> otherStatuses)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(350f, 0f), new Vector2(350f, float.MaxValue));

            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 8f);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 4f);
            using var popupBorder = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f);
            using var frameColor = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

            ImGui.BeginTooltip();

            // push the title, converting all color tags into the actual label.
            DisplayMoodleTitle(item.Title);

            if (!item.Description.IsNullOrWhitespace())
            {
                ImGui.Separator();
                DisplayMoodleDescription(item.Description, 4);
            }

            ImGui.Separator();
            CkGui.ColorText("Stacks:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Stacks.ToString());
            if (item.StackOnReapply)
            {
                ImGui.SameLine();
                CkGui.ColorText(" (inc by " + item.StacksIncOnReapply + ")", ImGuiColors.ParsedGold);
            }

            CkGui.ColorText("Duration:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text($"{item.Days}d {item.Hours}h {item.Minutes}m {item.Seconds}");

            CkGui.ColorText("Category:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Type.ToString());

            CkGui.ColorText("Dispellable:", ImGuiColors.ParsedGold);
            ImGui.SameLine();
            ImGui.Text(item.Dispelable ? "Yes" : "No");

            if (item.StatusOnDispell != Guid.Empty)
            {
                CkGui.ColorText("StatusOnDispell:", ImGuiColors.ParsedGold);
                ImGui.SameLine();
                var status = otherStatuses.FirstOrDefault(x => x.GUID == item.StatusOnDispell).Title ?? "Unknown";
                ImGui.Text(status);
            }

            ImGui.EndTooltip();
        }
    }

    private string _lastTitle;
    private ParsedSeString _lastParsedTitle;
    private string _lastDescription;
    private ParsedSeString _lastParsedDescription;

    public void DisplayMoodleTitle(string title, float width = 0)
    {
        // render the text of the last parsed title if the strings match already.
        var drawWidth = width > 0 ? width : ImGui.GetContentRegionAvail().X;
        if (string.Equals(title, _lastTitle, StringComparison.OrdinalIgnoreCase))
        {
            //_logger.LogDebug("Displaying Title: " + _lastParsedTitle.ToString());
            _lastParsedTitle.RenderText(drawWidth);
            return;
        }

        // Otherwise, parse it then render it.
        var parsed = ParseMoodleSeStringInternal(title, out var error);
        if (!parsed.RawString.IsNullOrWhitespace())
        {
            // Cache it if valid.
            _lastTitle = title;
            _lastParsedTitle = parsed;
            //_logger.LogDebug("Parsed Title: " + parsed.RawString);
            parsed.RenderText(drawWidth);
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, error);
        }
    }

    public void DisplayMoodleDescription(string description, int lineCount)
    {
        // render the text of the last parsed description if the strings match already.
        if (string.Equals(description, _lastDescription, StringComparison.OrdinalIgnoreCase))
        {
            //_logger.LogDebug("Displaying Description: " + _lastParsedDescription.ToString());
            _lastParsedDescription.RenderText(ImGui.GetContentRegionAvail().X, lineCount);
            return;
        }

        // Otherwise, parse it then render it.
        var parsed = ParseMoodleSeStringInternal(description, out var error);
        if (!parsed.RawString.IsNullOrWhitespace())
        {
            // Cache it if valid.
            _lastDescription = description;
            _lastParsedDescription = parsed;
            parsed.RenderText(ImGui.GetContentRegionAvail().X, lineCount);
        }
        else
        {
            _logger.LogWarning("Error: " + error);
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
    private ParsedSeString ParseMoodleSeStringInternal(string text, out string error)
    {
        // assume no error.
        error = string.Empty;
        var result = RegexEx.SplitRegex().Split(text);
        var parsedTextBuilder = new List<ParsedSeStringText>();

        try
        {
            // print the split regex, with a \n between each split.
            //_logger.LogInformation("Split Regex: " + string.Join("\n", result));


            // store the valid count for each attribute.
            var currentChunkText = ParsedSeStringText.Empty;
            var currentChunkFlags = ParsedSeStringFlags.None;

            // for each found result within the split.
            foreach (var str in result)
            {
                // if it contained nothing, skip to the next.
                if (str == string.Empty)
                    continue;

                //_logger.LogDebug("Processing: " + str);

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
                    if (Svc.Data.GetExcelSheet<UIColor>().GetRowOrDefault(r) is { } validUICol && r != 0)
                    {
                        // convert the forground to imgui u32 format.
                        var col = BinaryPrimitives.ReverseEndianness(validUICol.Dark);
                        // Add the color modifier
                        //_logger.LogDebug($"Adding Color 0x{col:X8}");
                        currentChunkFlags |= ParsedSeStringFlags.HasForeground;
                        currentChunkText.Foreground = col;
                        currentChunkText.Flags |= ParsedSeStringFlags.HasForeground;
                    }
                    else throw new Exception("Error: Color is out of range.");
                }
                // If the string contains the unique color tag closing identifier, turn off the foreground.
                else if (str.Equals("[/color]", StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure color was previously applied
                    if ((currentChunkFlags & ParsedSeStringFlags.HasForeground) == 0)
                        throw new Exception("Error: Mismatched [color] closing tag. [String Attempted: " + str + "] [Current Flags: " + currentChunkFlags + "]");

                    //_logger.LogDebug("Removing Color");
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
                    if (r == 0 || Svc.Data.GetExcelSheet<UIColor>().GetRowOrDefault(r) is not { } validUIGlow)
                        throw new Exception("Error: Glow is out of range.");

                    // Add the glow modifier

                    // convert the forground to imgui u32 format.
                    var glow = BinaryPrimitives.ReverseEndianness(validUIGlow.Light);
                    //_logger.LogDebug($"Adding Glow 0x{glow:X8}");
                    currentChunkFlags |= ParsedSeStringFlags.HasGlow;
                    currentChunkText.Glow = glow;
                    currentChunkText.Flags |= ParsedSeStringFlags.HasGlow;
                }
                // If the string contains the unique glow tag closing identifier, turn off the glow.
                else if (str.Equals("[/glow]", StringComparison.OrdinalIgnoreCase))
                {
                    if ((currentChunkFlags & ParsedSeStringFlags.HasGlow) == 0)
                        throw new Exception("Error: Mismatched [glow] closing tag.");

                    //_logger.LogDebug("Removing Glow");
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

                    //_logger.LogDebug("Adding Italics");
                    currentChunkFlags |= ParsedSeStringFlags.HasItalic;
                    currentChunkText.Italic = true;
                    currentChunkText.Flags |= ParsedSeStringFlags.HasItalic;
                }
                // If the string contains the unique italics tag closing identifier, turn off the italics.
                else if (str.Equals("[/i]", StringComparison.OrdinalIgnoreCase))
                {
                    if ((currentChunkFlags & ParsedSeStringFlags.HasItalic) == 0)
                        throw new Exception("Error: Mismatched [i] closing tag.");

                    //_logger.LogDebug("Removing Italics");
                    currentChunkFlags &= ~ParsedSeStringFlags.HasItalic;
                }
                // otherwise simply append the text.
                else
                {
                    // The next request is to append text. However, if text is already present, lets first append that before we add.
                    if(!currentChunkText.Text.IsNullOrEmpty())
                        AppendChunk(str);

                    //_logger.LogDebug("Adding Text");
                    currentChunkText.Text += str;
                }
            }

            // If there's any remaining chunk after parsing all items, append it.
            if (currentChunkFlags is ParsedSeStringFlags.None && !string.IsNullOrEmpty(currentChunkText.Text))
            {
                //_logger.LogDebug("Adding New Chunk Flat: " + currentChunkText.ToString());
                parsedTextBuilder.Add(currentChunkText);
            }

            // create the final item to return.
            var parsedText = new ParsedSeString(text, parsedTextBuilder.ToArray());
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
                //_logger.LogDebug("Adding New Chunk: " + currentChunkText.ToString());
                parsedTextBuilder.Add(currentChunkText);
                currentChunkText = ParsedSeStringText.Empty;
                currentChunkFlags = ParsedSeStringFlags.None;
            }

        }
        catch (Exception e)
        {
            // Convert parsedTextBuilder to a string for easier debugging.
            var parsedState = string.Join("\n", parsedTextBuilder.Select(t => t.Text));

            // Update the error message to include the current state of parsedTextBuilder
            error = $"Please check syntax:\n{e.Message}\n\nCurrent Parsed State:\n{parsedState}";

            return ParsedSeString.Empty;
        }
    }
}
