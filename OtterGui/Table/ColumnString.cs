using Dalamud.Interface.Utility;
using ImGuiNET;

namespace OtterGui.Table;

public class ColumnString<TItem> : Column<TItem>
{
    public ColumnString()
        => Flags &= ~ImGuiTableColumnFlags.NoResize;

    public    string FilterValue = string.Empty;
    protected Regex? FilterRegex;

    public virtual string ToName(TItem item)
        => item!.ToString() ?? string.Empty;

    public override int Compare(TItem lhs, TItem rhs)
        => string.Compare(ToName(lhs), ToName(rhs), StringComparison.InvariantCulture);

    public override bool DrawFilter()
    {
        using var style = Raii.ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);

        ImGui.SetNextItemWidth(-Table.ArrowWidth * ImGuiHelpers.GlobalScale);
        var tmp = FilterValue;
        if (!ImGui.InputTextWithHint(FilterLabel, Label, ref tmp, 256) || tmp == FilterValue)
            return false;

        FilterValue = tmp;
        try
        {
            FilterRegex = new Regex(FilterValue, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch
        {
            FilterRegex = null;
        }

        return true;
    }

    public override bool FilterFunc(TItem item)
    {
        if (FilterValue.Length == 0)
            return true;

        var name = ToName(item);
        return FilterRegex?.IsMatch(name) ?? name.Contains(FilterValue, StringComparison.OrdinalIgnoreCase);
    }

    public override void DrawColumn(TItem item, int _)
    {
        ImGui.TextUnformatted(ToName(item));
    }
}