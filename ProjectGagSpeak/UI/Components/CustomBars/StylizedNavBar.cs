using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace GagSpeak.Gui.Components;

public sealed record NavBarItem<TId>(TId Id, string Label, FAI? Icon = null) where TId : struct, Enum;
public sealed record NavBarGroup<TId>(string Header, IReadOnlyList<NavBarItem<TId>> Items) where TId : struct, Enum;

public class StylizedNavBar<TNavTab> where TNavTab : struct, Enum
{
    protected readonly List<NavBarGroup<TNavTab>> _groups = [];
    private TNavTab _selectedTab;

    public StylizedNavBar()
    {
        _selectedTab = default;
    }

    public StylizedNavBar(IReadOnlyList<NavBarGroup<TNavTab>> groups)
    {
        _groups = groups.ToList();
        _selectedTab = default;
    }

    public virtual TNavTab TabSelection
    {
        get => _selectedTab;
        set
        {
            var prev = _selectedTab;
            _selectedTab = value;
            OnTabChanged(prev, value);
        }
    }

    public void Draw()
        => Draw(null, null);

    public void Draw(float width)
        => Draw(width, null);

    public void Draw(float? width, float? rounding)
        => DrawInternal(width ?? ImGui.GetContentRegionAvail().X, rounding ?? ImGui.GetStyle().FrameRounding);

    protected virtual void DrawInternal(float width, float rounding)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Button, 0)
            .Push(ImGuiCol.ButtonHovered, GsCol.HoverOverlay.Uint())
            .Push(ImGuiCol.ButtonActive, GsCol.ActiveOverlay.Uint());
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, rounding)
            .Push(ImGuiStyleVar.ButtonTextAlign, new Vector2(0f, 0.5f));

        foreach (var group in _groups)
        {
            DrawGroupHeader(group.Header);

            foreach (var item in group.Items)
            {
                bool isSelected = EqualityComparer<TNavTab>.Default.Equals(item.Id, _selectedTab);
                if (DrawNavElement(item, width, isSelected))
                    TabSelection = item.Id;
            }

            DrawGroupSeparator(group, width);
        }
    }

    protected virtual bool IsDisabled(TNavTab id)
        => false;

    protected virtual void DrawGroupHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return;

        CkGui.InlineSpacingInner();
        ImGui.TextDisabled(header);
    }

    protected virtual bool DrawNavElement(NavBarItem<TNavTab> item, float width, bool isSelected)
    {
        var disabled = IsDisabled(item.Id);
        if (item.Icon is not null)
        {
            return CkGui.IconTextButton(item.Icon.Value, item.Label, width, true, disabled, $"nav_{item.Id}");
        }
        else
        {
            using var dis = ImRaii.Disabled(disabled);
            using var id = ImRaii.PushId(Convert.ToInt32(item.Id));
            return ImGui.Button(item.Label, new(width, 0));
        }
    }

    protected virtual void DrawGroupSeparator(NavBarGroup<TNavTab> group, float width)
    {
        ImGui.Spacing();
        ImGui.Separator();
    }

    protected virtual void OnTabChanged(TNavTab oldTab, TNavTab newTab)
    {
        // Nothing at the moment.
    }
}
