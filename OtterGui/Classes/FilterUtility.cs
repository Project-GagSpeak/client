using ImGuiNET;

namespace OtterGui.Classes;

public abstract class FilterUtility<T>
{
    protected LowerString Filter          = LowerString.Empty;
    protected long        NumericalFilter = 0;
    private   string      _input          = string.Empty;
    protected int         FilterMode      = -1;

    public bool IsEmpty
        => FilterMode == -1;

    protected abstract string                                                     Tooltip { get; }
    protected abstract (LowerString Filter, long NumericalFilter, int FilterMode) FilterChange(string input);

    public abstract bool ApplyFilter(in T value);

    public bool Draw(float width)
    {
        ImGui.SetNextItemWidth(width);
        var change = ImGui.InputTextWithHint("##filterInput", "Filter...", ref _input, 64);
        ImGuiUtil.HoverTooltip(Tooltip);
        if (!change)
            return false;

        (Filter, NumericalFilter, FilterMode) = FilterChange(_input);
        return true;
    }
}
