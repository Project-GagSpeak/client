namespace OtterGui.Table;

public class ColumnNumber<TItem> : ColumnString<TItem>
{
    public enum ComparisonMethod : byte
    {
        Equal        = 0,
        LessEqual    = 1,
        GreaterEqual = 2,
    };

    protected readonly ComparisonMethod Comparison;
    protected          int?             FilterNumber;

    public virtual int ToValue(TItem item)
        => item!.GetHashCode();

    public override int Compare(TItem lhs, TItem rhs)
        => ToValue(lhs).CompareTo(ToValue(rhs));

    public override string ToName(TItem item)
        => ToValue(item).ToString();

    public override bool DrawFilter()
    {
        if (!base.DrawFilter())
            return false;

        if (int.TryParse(FilterValue, out var number))
        {
            FilterNumber = number;
            FilterRegex  = null;
        }
        else
        {
            FilterNumber = null;
        }

        return true;
    }

    public override bool FilterFunc(TItem item)
    {
        if (!FilterNumber.HasValue)
            return base.FilterFunc(item);

        var value = ToValue(item);
        return Comparison switch
        {
            ComparisonMethod.Equal        => value == FilterNumber.Value,
            ComparisonMethod.LessEqual    => value <= FilterNumber.Value,
            ComparisonMethod.GreaterEqual => value >= FilterNumber.Value,
            _                             => true,
        };
    }

    public ColumnNumber(ComparisonMethod method)
        => Comparison = method;

    public override void DrawColumn(TItem item, int _)
        => ImGuiUtil.RightAlign(ToName(item));
}
