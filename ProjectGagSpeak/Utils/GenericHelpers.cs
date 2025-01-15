using ImGuiNET;
using Lumina.Misc;

namespace GagSpeak.Utils;

/// <summary> A class for all of the UI helpers, including basic functions for drawing repetative yet unique design elements </summary>
public static class GenericHelpers
{
    // work on integrating these into a utilities pack.
    public static IEnumerable<ActionExecutionType> ActionTypesRestraint => Enum.GetValues<ActionExecutionType>()
        .Where(x => x is not ActionExecutionType.Restraint && x is not ActionExecutionType.TextOutput);
    public static IEnumerable<ActionExecutionType> ActionTypesOnGag => Enum.GetValues<ActionExecutionType>()
        .Where(x => x is not ActionExecutionType.Gag && x is not ActionExecutionType.TextOutput);
    public static IEnumerable<ActionExecutionType> ActionTypesTrigger => Enum.GetValues<ActionExecutionType>()
        .Where(x => x is not ActionExecutionType.TextOutput);

    public static IEnumerable<NewState> RestrictedTriggerStates => Enum.GetValues<NewState>()
        .Where(x => x is not NewState.Disabled && x is not NewState.Unlocked);


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

    public static void OpenCombo(string comboLabel)
    {
        var windowId = ImGui.GetID(comboLabel);
        var popupId = ~Crc32.Get("##ComboPopup", windowId);
        ImGui.OpenPopup(popupId); // was originally popup ID
    }
}
