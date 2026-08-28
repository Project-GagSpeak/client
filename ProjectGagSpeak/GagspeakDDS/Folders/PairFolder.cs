using CkCommons.DrawSystem;
using Dalamud.Bindings.ImGui;
using GagSpeak.DrawSystem;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;

namespace GagSpeak.DrawSystem;

public sealed class PairFolder : DynamicFolder<Kinkster>, ISortableFolder<Kinkster>
{
    private readonly SorterHelpers _sortHelper;
    private readonly MainConfig _config;

    private readonly Func<IReadOnlyList<Kinkster>> _generator;
    private readonly Func<List<FolderSortFilter>> _getSorts;
    private readonly IReadOnlyList<FolderSortFilter> _validSorts;
    public PairFolder(SorterHelpers sortHelpers, MainConfig config, DynamicFolderGroup<Kinkster> parent,
        uint id, FAI icon, string name, uint iconColor, Func<IReadOnlyList<Kinkster>> generator,
        Func<List<FolderSortFilter>> sorters)
        : base(parent, id, icon, name)
    {
        _sortHelper = sortHelpers;
        _config = config;
        // Can set stylizations here.
        NameColor = uint.MaxValue;
        IconColor = iconColor;
        BgColor = uint.MinValue;
        BorderColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        GradientColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
        
        _generator = generator;
        _getSorts = sorters;
        _validSorts = _sortHelper.GetValidWhitelistSorts(Name);
        ApplyLatestSorter();
    }

    public int Rendered => Children.Count(s => s.Data.IsRendered);
    public int Online => Children.Count(s => s.Data.IsOnline);
    protected override IReadOnlyList<Kinkster> GetAllItems() => _generator();
    protected override DynamicLeaf<Kinkster> ToLeaf(Kinkster item) => new(this, item.User.UID, item);

    // Maybe replace with something better later. Would be nice to not depend on multiple generators but idk.
    public string BracketText => Name switch
    {
        Consts.DDS_All => $"[{TotalChildren}]",
        Consts.DDS_Rendered => $"[{Rendered}]",
        Consts.DDS_Online => $"[{Online}]",
        Consts.DDS_Offline => $"[{TotalChildren}]",
        _ => string.Empty,
    };

    public string BracketTooltip => Name switch
    {
        Consts.DDS_All => $"{TotalChildren} total",
        Consts.DDS_Rendered => $"{Rendered} visible",
        Consts.DDS_Online => $"{Online} online",
        Consts.DDS_Offline => $"{TotalChildren} offline",
        _ => string.Empty,
    };

    #region ISortableFolder
    public IReadOnlyDynamicSorter<DynamicLeaf<Kinkster>> DynamicSorter => Sorter;
    public IReadOnlyList<ISortMethod<DynamicLeaf<Kinkster>>> UnusedSteps { get; private set; } = [];
    public void ApplyLatestSorter()
    {
        var all = _sortHelper.GetWhitelistFilterable(Name);
        // Strip invalid steps
        var configRef = _getSorts();
        configRef.RemoveAll(o => !_validSorts.Contains(o));
        // Collect the desired filters.
        var desired = configRef.Select(_sortHelper.ToSortMethod).ToList();
        // Update the Folders sorter to the new steps.
        Sorter.SetSteps(desired);
        UnusedSteps = [.. all.Except(desired)];
        _config.Save();
    }

    public bool AddFilter(ISortMethod<DynamicLeaf<Kinkster>> filter)
    {
        _getSorts().Add(_sortHelper.ToSortFilter(filter));
        ApplyLatestSorter();
        return true;
    }

    public bool RemoveFilter(int index)
    {
        var curSorts = _getSorts();
        if (index < 0 || index >= curSorts.Count)
            return false;

        curSorts.RemoveAt(index);
        ApplyLatestSorter();
        return true;
    }

    public bool MoveFilters(int[] fromIndices, int targetIdx)
    {
        var sortOrder = _getSorts();
        // Sort in descending order for efficient removal
        Array.Sort(fromIndices);
        Array.Reverse(fromIndices);

        // Collect items to move
        var toMove = new List<FolderSortFilter>(fromIndices.Length);
        foreach (var item in fromIndices)
            toMove.Add(sortOrder[item]);

        // Remove from the list in descending order
        foreach (var idx in fromIndices)
            sortOrder.RemoveAt(idx);

        sortOrder.InsertRange(Math.Min(targetIdx, sortOrder.Count), toMove);
        ApplyLatestSorter();
        return true;
    }

    public bool ClearFilters()
    {
        var sortOrder = _getSorts();
        sortOrder.Clear();
        sortOrder.Add(FolderSortFilter.Alphabetical);
        ApplyLatestSorter();
        return true;
    }
    #endregion
}
