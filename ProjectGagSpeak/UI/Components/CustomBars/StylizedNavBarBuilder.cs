namespace GagSpeak.Gui.Components;

public sealed class StylizedNavBarBuilder<TId> where TId : struct, Enum
{
    private readonly List<NavBarGroup<TId>> _groups = [];
    private GroupBuilder? _current;

    public GroupBuilder CreateGroup(string header)
    {
        // finalize previous group if still open
        _current?.Commit();
        _current = new GroupBuilder(this, header);
        return _current;
    }

    public StylizedNavBar<TId> Build()
    {
        _current?.Commit();
        return new StylizedNavBar<TId>(_groups);
    }

    public IReadOnlyList<NavBarGroup<TId>> ToList()
        => _groups;

    private void AddGroup(NavBarGroup<TId> group)
        => _groups.Add(group);

    public sealed class GroupBuilder
    {
        private readonly StylizedNavBarBuilder<TId> _parent;
        private readonly string _header;
        private readonly List<NavBarItem<TId>> _items = [];

        private bool _committed;

        internal GroupBuilder(StylizedNavBarBuilder<TId> parent, string header)
        {
            _parent = parent;
            _header = header;
        }

        public GroupBuilder Add(TId id, string label, FAI? icon = null)
        {
            _items.Add(new NavBarItem<TId>(id, label, icon));
            return this;
        }

        public StylizedNavBarBuilder<TId> EndGroup()
        {
            Commit();
            return _parent;
        }

        internal void Commit()
        {
            if (_committed)
                return;

            _committed = true;

            _parent.AddGroup(new NavBarGroup<TId>(_header, _items.ToArray()));
        }
    }
}
