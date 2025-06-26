using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;
using System.Runtime.CompilerServices;

namespace GagSpeak.CkCommons.Classes;
public class OptionalBoolIconCheckbox(FontAwesomeIcon icon, uint crossColor = 0xFF0000FF, uint checkColor = 0xFF00FF00, uint dotColor = 0xFFD0D0D0) : OptionalBoolCheckbox
{
    /// <inheritdoc/>
    protected override void RenderSymbol(OptionalBool value, Vector2 position, float size)
    {
        using var font = ImRaii.PushFont(UiBuilder.IconFont);

        var iconSize = ImUtf8.CalcTextSize(icon.ToIconString());
        var iconPosition = position + (new Vector2(size) - iconSize) * 0.5f;

        switch (value.Value)
        {
            case true:
                ImGui.GetWindowDrawList().AddText(iconPosition, ImGui.GetColorU32(checkColor), icon.ToIconString());
                break;
            case false:
                ImGui.GetWindowDrawList().AddText(iconPosition, ImGui.GetColorU32(crossColor), icon.ToIconString());
                break;
            case null:
                ImGui.GetWindowDrawList().AddText(iconPosition, ImGui.GetColorU32(dotColor), icon.ToIconString());
                break;
        }
    }

    /// <summary> Draw the tri-state checkbox. </summary>
    /// <param name="label"> The label for the checkbox as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The input/output value. </param>
    /// <returns> True when <paramref name="value"/> changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DrawIconCheckbox(ReadOnlySpan<char> label, OptionalBool current, out OptionalBool newValue, bool disabled = false)
    {
        // Initialize newValue to the current state initially
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
}
