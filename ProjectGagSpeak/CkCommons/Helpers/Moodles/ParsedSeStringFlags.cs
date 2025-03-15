namespace GagSpeak.CkCommons.Helpers;

[Flags]
public enum ParsedSeStringFlags
{
    None          = 0x00,
    HasForeground = 0x01,
    HasGlow       = 0x02,
    HasItalic     = 0x04,

    All = HasForeground | HasGlow | HasItalic,
    ColoredItalic = HasForeground | HasItalic,
    ColoredGlow = HasForeground | HasGlow,
    ItalicGlow = HasItalic | HasGlow,
}
