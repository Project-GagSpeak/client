namespace GagSpeak.Gui.Components;

public sealed class StylizedTabbarBuilder<ITab> where ITab : struct, Enum
{
    private readonly List<StylizedTabbar<ITab>.StylizedTab> _tabs = [];

    public StylizedTabbarBuilder<ITab> AddTab(ITab tab, string label, FAI? icon = null, uint? col = null, string? tooltip = null)
    {
        _tabs.Add(new(tab, label, icon, col, tooltip));
        return this;
    }

    public StylizedTabbar<ITab> Build()
    {
        ValidateTabs();

        var tabbar = new StylizedTabbar<ITab>();
        foreach (var tab in _tabs)
            tabbar.AddTab(tab);

        return tabbar;
    }

    private void ValidateTabs()
    {
        var enumValues = Enum.GetValues<ITab>().ToHashSet();
        var defined = _tabs.Select(t => t.Tab).ToHashSet();

        if (!enumValues.SetEquals(defined))
            throw new InvalidOperationException($"Tab mismatch. Missing: {string.Join(", ", enumValues.Except(defined))}");
    }
}
