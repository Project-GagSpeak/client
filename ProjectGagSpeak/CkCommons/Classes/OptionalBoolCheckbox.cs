using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text.Widget;
using OtterGuiInternal.Utility;
using System.Runtime.CompilerServices;

namespace GagSpeak.CkCommons.Classes;

public class OptionalBoolCheckbox(uint crossColor = 0xFF0000FF, uint checkColor = 0xFF00FF00, uint dotColor = 0xFFD0D0D0)
    : MultiStateCheckbox<OptionalBool>
{


    /// <inheritdoc/>
    protected override void RenderSymbol(OptionalBool value, Vector2 position, float size)
    {
        switch (value.Value)
        {
            case true:
                SymbolHelpers.RenderCheckmark(ImGui.GetWindowDrawList(), position, ImGui.GetColorU32(checkColor), size);
                break;
            case false:
                SymbolHelpers.RenderCross(ImGui.GetWindowDrawList(), position, ImGui.GetColorU32(crossColor), size);
                break;
            case null:
                SymbolHelpers.RenderDot(ImGui.GetWindowDrawList(), position, ImGui.GetColorU32(dotColor), size);
                break;
        }
    }

    /// <summary> Draw the tri-state checkbox. </summary>
    /// <param name="label"> The label for the checkbox as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The input/output value. </param>
    /// <returns> True when <paramref name="value"/> changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Draw(ReadOnlySpan<char> label, OptionalBool current, out OptionalBool newValue, bool disabled = false)
    {
        newValue = current;

        using (ImRaii.Disabled(disabled))
        {
            if (Draw(label, ref newValue))
                return true;
        }
        CkGui.AttachToolTip("This attribute will " + (newValue.Value switch
        {
            true => "be enabled.",
            false => "be disabled.",
            null => "be left as is.",
        }));
        return false;
    }

    /// <inheritdoc/>
    protected override OptionalBool NextValue(OptionalBool value)
    {
        // Cycle through the states: null -> true -> false -> null
        return value.Value switch
        {
            null => OptionalBool.True,
            true => OptionalBool.False,
            false => OptionalBool.Null,
        };
    }

    /// <inheritdoc/>
    protected override OptionalBool PreviousValue(OptionalBool value)
    {
        // Cycle through the states in reverse: null -> false -> true -> null
        return value.Value switch
        {
            null => OptionalBool.False,
            true => OptionalBool.Null,
            false => OptionalBool.True,
        };
    }
}
