using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;

namespace OtterGui.Classes;

public static class MessageServiceExtensions
{
    public static void NotificationMessage(this MessageService service, string content, NotificationType type = NotificationType.None,
        bool doPrint = true)
        => service.AddMessage(new Notification(content, type), doPrint, true, true, false);

    public static void NotificationMessage(this MessageService service, Exception ex, string content1,
        NotificationType type = NotificationType.None, bool doPrint = true)
        => service.AddMessage(new Notification(ex, content1, content1.TrimEnd('.'), type), doPrint, true, true, false);

    public static void NotificationMessage(this MessageService service, Exception ex, string content1, string content2,
        NotificationType type = NotificationType.None, bool doPrint = true)
        => service.AddMessage(new Notification(ex, content1, content2, type), doPrint, true, true, false);
}

public static class SeStringBuilderExtensions
{
    public const ushort Green  = 504;
    public const ushort Yellow = 31;
    public const ushort Red    = 534;
    public const ushort Blue   = 517;
    public const ushort White  = 1;
    public const ushort Purple = 541;

    public static SeStringBuilder AddText(this SeStringBuilder sb, string text, int color, bool brackets = false)
        => sb.AddUiForeground((ushort)color).AddText(brackets ? $"[{text}]" : text).AddUiForegroundOff();

    public static SeStringBuilder AddGreen(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Green, brackets);

    public static SeStringBuilder AddYellow(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Yellow, brackets);

    public static SeStringBuilder AddRed(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Red, brackets);

    public static SeStringBuilder AddBlue(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Blue, brackets);

    public static SeStringBuilder AddWhite(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, White, brackets);

    public static SeStringBuilder AddPurple(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Purple, brackets);

    public static SeStringBuilder AddCommand(this SeStringBuilder sb, string command, string description)
        => sb.AddText("    》 ")
            .AddBlue(command)
            .AddText($" - {description}");

    public static SeStringBuilder AddInitialPurple(this SeStringBuilder sb, string word, bool withComma = true)
        => sb.AddPurple($"[{word[0]}]")
            .AddText(withComma ? $"{word[1..]}, " : word[1..]);
}
