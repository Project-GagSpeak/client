using ImGuiNET;
using Lumina.Misc;

namespace GagSpeak.Utils;

/// <summary> A class for all of the UI helpers, including basic functions for drawing repetative yet unique design elements </summary>
public static class GenericHelpers
{
    // work on integrating these into a utilities pack.
    public static IEnumerable<InvokableActionType> ActionTypesRestraint => Enum.GetValues<InvokableActionType>()
        .Where(x => x is not InvokableActionType.Restraint && x is not InvokableActionType.TextOutput);
    public static IEnumerable<InvokableActionType> ActionTypesOnGag => Enum.GetValues<InvokableActionType>()
        .Where(x => x is not InvokableActionType.Gag && x is not InvokableActionType.TextOutput);
    public static IEnumerable<InvokableActionType> ActionTypesTrigger => Enum.GetValues<InvokableActionType>()
        .Where(x => x is not InvokableActionType.TextOutput);

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
