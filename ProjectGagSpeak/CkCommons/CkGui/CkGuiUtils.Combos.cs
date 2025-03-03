using ImGuiNET;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.Gui.Utility;
public static partial class CkGuiUtils
{
    /// <summary> Yanked variant of GenericEnumCombo from ImGui, with a custom display text when not found.
    /// <para> Can specify enum values to skip at start or end and gives all those enum values as options. </para>
    /// </summary>
    /// <returns> True if the value was changed. </returns>
    /// <remarks> Uses the supplied toString function if any, otherwise ToString. </remarks>
    public static bool EnumCombo<T>(string label, float width, T current, out T newValue,
        Func<T, string>? toString = null, string defaultText = "Select Item..", int skip = 0) where T : struct, Enum
        => EnumCombo(label, width, current, out newValue, Enum.GetValues<T>().Skip(skip), toString, defaultText);

    /// <summary> Yanked variant of GenericEnumCombo from ImGui, with a custom display text when not found. </summary>
    /// <returns> True if the value was changed. </returns>
    /// <remarks> Uses the supplied toString function if any, otherwise ToString. </remarks>
    public static bool EnumCombo<T>(string label, float width, T current, out T newValue,
        IEnumerable<T> options, Func<T, string>? toString = null, string defaultText = "Select Item..") where T : struct, Enum
    {
        ImGui.SetNextItemWidth(width);
        var previewText = options.Contains(current) ? (toString?.Invoke(current) ?? current.ToString()) : defaultText;
        using var combo = ImRaii.Combo(label, previewText);
        if (combo)
            foreach (var data in options)
            {
                var name = toString?.Invoke(data) ?? data.ToString();
                if (name.Length == 0 || !ImGui.Selectable(name, data.Equals(current)) || data.Equals(current))
                    continue;

                newValue = data;
                return true;
            }

        newValue = current;
        return false;
    }

}
