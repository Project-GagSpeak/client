using ImGuiNET;
using OtterGui.Raii;
using System.Linq;

namespace GagSpeak.Gui.Utility;
public static partial class CkGuiUtils
{
    /// <summary> Yanked variant of GenericEnumCombo from ImGui, with a custom display text when not found.
    /// <para> Can specify enum values to skip at start or end and gives all those enum values as options. </para>
    /// </summary>
    /// <returns> True if the value was changed. </returns>
    /// <remarks> Uses the supplied toString function if any, otherwise ToString. </remarks>
    public static bool EnumCombo<T>(string label, float width, T current, out T newValue, Func<T, string>? toString = null, 
        string defaultText = "Select Item..", int skip = 0, CFlags flags = CFlags.NoArrowButton) where T : struct, Enum
        => EnumCombo(label, width, current, out newValue, Enum.GetValues<T>().Skip(skip), toString, defaultText, flags);

    /// <summary> Yanked variant of GenericEnumCombo from ImGui, with a custom display text when not found. </summary>
    /// <returns> True if the value was changed. </returns>
    /// <remarks> Uses the supplied toString function if any, otherwise ToString. </remarks>
    public static bool EnumCombo<T>(string label, float width, T current, out T newValue, IEnumerable<T> options, Func<T, string>? toString = null,
        string defaultText = "Select Item..", CFlags flags = CFlags.None) where T : struct, Enum
    {
        ImGui.SetNextItemWidth(width);
        var previewText = options.Contains(current) ? (toString?.Invoke(current) ?? current.ToString()) : defaultText;
        using (var combo = ImRaii.Combo(label, previewText, flags))
        {
            if (combo)
            {
                foreach (var data in options)
                {
                    var name = toString?.Invoke(data) ?? data.ToString();
                    if (name.Length == 0 || !ImGui.Selectable(name, data.Equals(current)) || data.Equals(current))
                        continue;

                    newValue = data;
                    return true;
                }
            }
        }
        // reset to None if right-clicked.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            newValue = options.FirstOrDefault();
            return true;
        }

        newValue = current;
        return false;
    }

    public static string LayerIdxName(int idx) => idx < 0 ? "Any Layer" : $"Layer {idx + 1}";
    public static bool LayerIdxCombo(string label, float width, int curIdx, out int newIdx, int items, bool showAny = false, CFlags flags = CFlags.None)
    {
        ImGui.SetNextItemWidth(width);
        bool inRange = curIdx >= 0 && curIdx < items;
        var previewText = inRange ? $"Layer {curIdx + 1}" : "Any Layer";
        using (var c = ImRaii.Combo(label, previewText, flags))
        {
            if (c)
            {
                // Selection for "Any Layer".
                if (showAny)
                {
                    if (ImGui.Selectable("Any Layer", curIdx == -1) && curIdx == -1)
                    {
                        newIdx = -1;
                        return true;
                    }
                }
                // Remaining layers.
                for (var i = 0; i < items; i++)
                {
                    var name = $"Layer {i + 1}";
                    if (!ImGui.Selectable(name, i == curIdx) || i == curIdx)
                        continue;

                    newIdx = i;
                    return true;
                }
            }
        }
        // reset to None if right-clicked.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            newIdx = -1;
            return true;
        }

        newIdx = curIdx;
        return false;
    }


    /// <summary> a generic string combo for string usage. </summary>
    /// <returns> True if the value was changed. </returns>
    /// <remarks> Useful for non-enum based dropdowns for simplistic options. </remarks>
    public static bool StringCombo(string label, float width, string current, out string newValue,
        IEnumerable<string> options, string defaultText = "Select Item...")
    {
        ImGui.SetNextItemWidth(width);
        var previewText = options.Contains(current) ? current.ToString() : defaultText;
        using var combo = ImRaii.Combo(label, previewText);
        if (combo)
            foreach (var data in options)
            {
                if (data.Length == 0 || !ImGui.Selectable(data, data.Equals(current)) || data.Equals(current))
                    continue;

                newValue = data;
                return true;
            }

        newValue = current;
        return false;
    }

    public static bool IntCombo(string label, float width, int current, out int newValue, IEnumerable<int> options, 
        Func<int, string>? toString = null, string defaultText = "Select Item...", CFlags flags = CFlags.None)
    {
        ImGui.SetNextItemWidth(width);
        var previewText = options.Contains(current) ? (toString?.Invoke(current) ?? current.ToString()) : defaultText;
        using (var combo = ImRaii.Combo(label, previewText, flags))
        {
            if (combo)
            {
                foreach (var option in options)
                {
                    var display = toString?.Invoke(option) ?? option.ToString();
                    if (display.Length == 0 || !ImGui.Selectable(display, option == current) || option == current)
                        continue;

                    newValue = option;
                    return true;
                }
            }
        }

        // Reset to first option if right-clicked
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            newValue = options.FirstOrDefault();
            return true;
        }

        newValue = current;
        return false;
    }

}
