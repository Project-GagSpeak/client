using ImGuiNET;
using Lumina.Misc;

namespace GagSpeak.Utils;

/// <summary> A class for all of the UI helpers, including basic functions for drawing repetative yet unique design elements </summary>
public static class GenericHelpers
{
    public static IEnumerable<Padlocks> NoOwnerPadlockList = Enum.GetValues<Padlocks>()
        .Cast<Padlocks>()
        .Where(p => p is not Padlocks.OwnerPadlock && p is not Padlocks.OwnerTimerPadlock
            && p is not Padlocks.DevotionalPadlock && p is not Padlocks.DevotionalTimerPadlock
            && p is not Padlocks.MimicPadlock)
        .ToArray();

    public static IEnumerable<Padlocks> NoMimicPadlockList = Enum.GetValues<Padlocks>()
        .Cast<Padlocks>()
        .Where(p => p is not Padlocks.MimicPadlock)
        .ToArray();

    /// <summary> A generic function to iterate through a collection and perform an action on each item </summary>
    public static void Each<T>(this IEnumerable<T> collection, Action<T> function)
    {
        foreach (var x in collection)
        {
            function(x);
        }
    }

    public static bool EqualsAny<T>(this T obj, params T[] values)
    {
        return values.Any(x => x!.Equals(obj));
    }

    // execute agressive inlining functions safely
    public static void Safe(Action action, bool suppressErrors = false)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            // log errors if not surpressed
            if (!suppressErrors)
            {
                throw new Exception($"{e.Message}\n{e.StackTrace ?? ""}");
            }
        }
    }

    public static readonly HashSet<string> TimerPadlocks = new HashSet<string>
    {
        Padlocks.FiveMinutesPadlock.ToName(),
        Padlocks.TimerPasswordPadlock.ToName(),
        Padlocks.OwnerTimerPadlock.ToName(),
        Padlocks.DevotionalTimerPadlock.ToName(),
        Padlocks.MimicPadlock.ToName()
    };

    public static void OpenCombo(string comboLabel)
    {
        var windowId = ImGui.GetID(comboLabel);
        var popupId = ~Crc32.Get("##ComboPopup", windowId);
        ImGui.OpenPopup(popupId); // was originally popup ID
    }
}
