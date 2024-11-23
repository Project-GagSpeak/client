using Dalamud.Interface.ImGuiNotification;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Raii;

namespace OtterGui.Custom;

public static class CustomGui
{
    public const uint DiscordColor     = 0xFFDA8972;
    public const uint ReniColorButton  = 0xFFCC648D;
    public const uint ReniColorHovered = 0xFFB070B0;
    public const uint ReniColorActive  = 0xFF9070E0;

    /// <summary> Draw a button to open the official discord server. </summary>
    public static void DrawDiscordButton(MessageService message, float width)
    {
        const string address = @"https://discord.gg/kVva7DHV4r";
        using var    color   = ImRaii.PushColor(ImGuiCol.Button, DiscordColor);

        DrawLinkButton(message, "Join Discord for Support", address, width, $"Open {address}");
    }

    /// <summary> Draw the button that opens the ReniGuide. </summary>
    public static void DrawGuideButton(MessageService message, float width)
    {
        const string address = @"https://reniguide.info/";
        using var color = ImRaii.PushColor(ImGuiCol.Button, ReniColorButton)
            .Push(ImGuiCol.ButtonHovered, ReniColorHovered)
            .Push(ImGuiCol.ButtonActive,  ReniColorActive);

        DrawLinkButton(message, "Beginner's Guides", address, width,
            $"Open {address}\nImage and text based guides for most functionality of Penumbra made by Serenity.\n"
          + "Not directly affiliated and potentially, but not usually out of date.");
    }

    /// <summary> Draw a button that opens an address in the browser. </summary>
    public static void DrawLinkButton(MessageService message, string text, string address, float width, string? tooltip = null)
    {
        if (ImGui.Button(text, new Vector2(width, 0)))
            try
            {
                var process = new ProcessStartInfo(address)
                {
                    UseShellExecute = true,
                };
                Process.Start(process);
            }
            catch
            {
                message.NotificationMessage($"Could not open {text} at {address} in external browser", NotificationType.Error);
            }

        if (tooltip != null)
            ImGuiUtil.HoverTooltip(tooltip);
    }
}
