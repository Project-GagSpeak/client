using Dalamud.Interface.Colors;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace GagSpeak.CkCommons.Helpers;

public struct ParsedSeStringText
{
    public string               Text        { get; set; } = string.Empty;
    public uint                 Foreground  { get; set; } = 0;
    public uint                 Glow        { get; set; } = 0;
    public bool                 Italic      { get; set; } = false;
    public ParsedSeStringFlags  Flags       { get; set; } = ParsedSeStringFlags.None;

    public ParsedSeStringText() { }
    public ParsedSeStringText(string text) => Text = text;

    public static readonly ParsedSeStringText Empty = new();

    public void AddColor(uint color)
    {
        Foreground = color;
        Flags |= ParsedSeStringFlags.HasForeground;
    }

    public void AddGlow(uint color)
    {
        Glow = color;
        Flags |= ParsedSeStringFlags.HasGlow;
    }

    public void AddItalic()
    {
        Italic = true;
        Flags |= ParsedSeStringFlags.HasItalic;
    }

    public override string ToString()
    {
        return $"Text: {Text}, Foreground: 0x{Foreground:X8}, Glow: 0x{Glow:X8}, Italic: {Italic}, Flags: {Flags}";
    }
}
