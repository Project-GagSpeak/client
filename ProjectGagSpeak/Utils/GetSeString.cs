using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;

namespace GagSpeak.Utils;

/// <summary> Various approaches for creating SeString objects or obtaining them. Be it safe, or unsafe. </summary>
public static class GS_GetSeString
{
    internal static unsafe SeString GetSeString(byte* textPtr)
        => GetSeString((IntPtr)textPtr);

    internal static SeString GetSeString(IntPtr textPtr)
    {
        if (textPtr == IntPtr.Zero)
        {
            // _logger.LogError("GetSeString: textPtr is null");
            return null!;
        }

        try
        {
            return MemoryHelper.ReadSeStringNullTerminated(textPtr);
        }
        catch (Exception)
        {
            // _logger.LogError($"Error in GetSeString: {ex.Message}");
            return null!;
        }
    }

    internal static unsafe string GetSeStringText(byte* textPtr)
        => GetSeStringText(GetSeString(textPtr));

    internal static string GetSeStringText(IntPtr textPtr)
        => GetSeStringText(GetSeString(textPtr));

    internal static string GetSeStringText(SeString seString)
    {
        var pieces = seString.Payloads.OfType<TextPayload>().Select(t => t.Text);
        var text = string.Join(string.Empty, pieces).Replace('\n', ' ').Trim();
        return text;
    }
}
