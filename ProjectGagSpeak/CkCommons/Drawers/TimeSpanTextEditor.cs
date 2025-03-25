using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Classes;

namespace GagSpeak.CkCommons.Drawers;

/// <summary> A stateful text editor for timeSpan that uses GsString formatting. </summary>
/// <remarks> Uses functions for getters and setters. </remarks>
public class TimeSpanTextEditor
{
    private string _timeSpanString;
    private TimeSpan _timeSpan;
    private readonly Func<TimeSpan> _getter;
    private readonly Action<TimeSpan> _setter;

    public TimeSpanTextEditor(Func<TimeSpan> getter, Action<TimeSpan> setter)
    {
        _getter = getter;
        _setter = setter;
        _timeSpan = getter(); // Get the initial value
        _timeSpanString = _timeSpan.ToGsRemainingTime();
    }

    public void DrawInputTimer(string label, float width, string hint)
    {
        // Refresh the cached value if it has changed externally
        var currentValue = _getter();
        if (currentValue != _timeSpan)
        {
            _timeSpan = currentValue;
            _timeSpanString = _timeSpan.ToGsRemainingTime();
        }

        ImGui.SetNextItemWidth(width);
        ImGui.InputTextWithHint(label, hint, ref _timeSpanString, 16);
        // Apply updates only when editing finishes
        if (ImGui.IsItemDeactivatedAfterEdit() && GsPadlockEx.TryParseTimeSpan(_timeSpanString, out var newSpan))
        {
            if (newSpan != _timeSpan) // Prevent unnecessary updates
            {
                _timeSpan = newSpan;
                _setter(newSpan); // Apply new value via provided function
            }
        }
    }
}
