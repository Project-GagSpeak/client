using CkCommons.DrawSystem;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;

namespace GagSpeak.DrawSystem;

public class SorterHelpers
{
    private readonly MainConfig _config;
    private readonly KinksterManager _sundesmos;
    private readonly PairService _pairService;

    public SorterHelpers(MainConfig config, KinksterManager sundesmos, PairService pairService)
    {
        _config = config;
        _sundesmos = sundesmos;
        _pairService = pairService;


        ByRenderPair = new KinksterRendered(l => _pairService.GetDisplayName(l.Data.User));
        ByOnline = new KinksterOnline(l => _pairService.IsOnline(l.Data.User));
        ByNamePair = new KinksterName(l => _pairService.GetDisplayName(l.Data.User));

        //ByNameRadar = new RadarName(l => _pairService.GetDisplayName(l.Data.User));
        //ByRenderRadar = new RadarRendered(l => _pairService.IsRendered(l.Data.User));
    }

    // Instanced Sorters
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByFavorite = new StaticSorters.Favorite();
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByTemporary = new StaticSorters.Temporary();
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByDateAdded = new StaticSorters.DateAdded();

    // Static Sorters
    public readonly ISortMethod<DynamicLeaf<Kinkster>> ByRenderPair;
    public readonly ISortMethod<DynamicLeaf<Kinkster>> ByOnline;
    public readonly ISortMethod<DynamicLeaf<Kinkster>> ByNamePair;
    //public readonly ISortMethod<DynamicLeaf<IRadarSyncMember>> ByRenderRadar;
    //public readonly ISortMethod<DynamicLeaf<IRadarSyncMember>> ByNameRadar;

    public IReadOnlyList<ISortMethod<DynamicLeaf<Kinkster>>> SavedWhitelistSortOrder(string name)
        => GetCurrentConfigList(name).Select(ToSortMethod).ToList();

    //public IReadOnlyList<ISortMethod<DynamicLeaf<IRadarSyncMember>>> GetRadarSortPreset()
    //    => [ByRenderRadar, ByNameRadar];

    public IReadOnlyList<ISortMethod<DynamicLeaf<Kinkster>>> GetAllDirectPairSteps()
        => [ByRenderPair, ByOnline, ByFavorite, ByNamePair, ByTemporary, ByDateAdded];

    public IReadOnlyList<ISortMethod<DynamicLeaf<Kinkster>>> GetWhitelistFilterable(string name)
        => name switch
        {
            Constants.FolderTagVisible => [ByFavorite, ByNamePair, ByTemporary, ByDateAdded],
            Constants.FolderTagOnline => [ByRenderPair, ByFavorite, ByNamePair, ByTemporary, ByDateAdded],
            Constants.FolderTagOffline => [ByFavorite,  ByNamePair, ByTemporary, ByDateAdded],
            Constants.FolderTagAll => [ByRenderPair, ByOnline, ByFavorite, ByNamePair, ByTemporary, ByDateAdded],
            _ => [ByFavorite, ByNamePair],
        };

    // Instanced static defaults for filters.
    public static IReadOnlyList<FolderSortFilter> DefaultSortOrderAll = [FolderSortFilter.Favorite, FolderSortFilter.Temporary, FolderSortFilter.Rendered, FolderSortFilter.Online, FolderSortFilter.Alphabetical, FolderSortFilter.DateAdded];
    public static IReadOnlyList<FolderSortFilter> DefaultSortOrderVisible = [FolderSortFilter.Favorite, FolderSortFilter.Temporary, FolderSortFilter.Alphabetical, FolderSortFilter.DateAdded];
    public static IReadOnlyList<FolderSortFilter> DefaultSortOrderOnline = [FolderSortFilter.Favorite, FolderSortFilter.Temporary, FolderSortFilter.Rendered, FolderSortFilter.Alphabetical, FolderSortFilter.DateAdded];
    public static IReadOnlyList<FolderSortFilter> DefaultSortOrderOffline = [FolderSortFilter.Favorite, FolderSortFilter.Alphabetical, FolderSortFilter.Temporary, FolderSortFilter.DateAdded];

    public List<FolderSortFilter> GetCurrentConfigList(string folderName) => folderName switch
    {
        Constants.FolderTagAll => _config.Data.WhitelistSortOrderAll,
        Constants.FolderTagVisible => _config.Data.WhitelistSortOrderVisible,
        Constants.FolderTagOnline => _config.Data.WhitelistSortOrderOnline,
        _ => _config.Data.WhitelistSortOrderOffline,
    };

    public IReadOnlyList<FolderSortFilter> GetValidWhitelistSorts(string folderName) => folderName switch
    {
        Constants.FolderTagAll => DefaultSortOrderAll,
        Constants.FolderTagVisible => DefaultSortOrderVisible,
        Constants.FolderTagOnline => DefaultSortOrderOnline,
        Constants.FolderTagOffline => DefaultSortOrderOffline,
        _ => DefaultSortOrderOffline
    };


    public ISortMethod<DynamicLeaf<Kinkster>> ToSortMethod(FolderSortFilter filter)
        => filter switch
        {
            FolderSortFilter.Rendered => ByRenderPair,
            FolderSortFilter.Online => ByOnline,
            FolderSortFilter.Favorite => ByFavorite,
            FolderSortFilter.Alphabetical => ByNamePair,
            FolderSortFilter.Temporary => ByTemporary,
            FolderSortFilter.DateAdded => ByDateAdded,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };

    public FolderSortFilter ToSortFilter(ISortMethod<DynamicLeaf<Kinkster>> sortMethod)
        => sortMethod switch
        {
            KinksterRendered => FolderSortFilter.Rendered,
            KinksterOnline => FolderSortFilter.Online,
            StaticSorters.Favorite => FolderSortFilter.Favorite,
            KinksterName => FolderSortFilter.Alphabetical,
            StaticSorters.Temporary => FolderSortFilter.Temporary,
            StaticSorters.DateAdded => FolderSortFilter.DateAdded,
            _ => throw new ArgumentOutOfRangeException(nameof(sortMethod), sortMethod, null)
        };


    readonly struct KinksterRendered(Func<DynamicLeaf<Kinkster>, IComparable?> resFunc) : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Rendered";
        public FAI Icon => FAI.Eye; // Maybe change.
        public string Tooltip => "Sort by rendered status.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => resFunc;
    }

    readonly struct KinksterOnline(Func<DynamicLeaf<Kinkster>, IComparable?> resFunc) : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Online";
        public FAI Icon => FAI.Wifi; // Maybe change.
        public string Tooltip => "Sort by online status.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => resFunc;
    }


    readonly struct KinksterName(Func<DynamicLeaf<Kinkster>, IComparable?> nameResFunc) : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Name";
        public FAI Icon => FAI.SortAlphaDown; // Maybe change.
        public string Tooltip => "Sort by name.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => nameResFunc;
    }

    //readonly struct RadarName(Func<DynamicLeaf<IRadarSyncMember>, IComparable?> resFunc) : ISortMethod<DynamicLeaf<IRadarSyncMember>>
    //{
    //    public string Name => "Name";
    //    public FAI Icon => FAI.SortAlphaDown; // Maybe change.
    //    public string Tooltip => "Sort by name.";
    //    public Func<DynamicLeaf<IRadarSyncMember>, IComparable?> KeySelector => resFunc;
    //}

    //readonly struct RadarRendered : ISortMethod<DynamicLeaf<IRadarSyncMember>>
    //{
    //    private readonly Func<DynamicLeaf<IRadarSyncMember>, IComparable?> _resFunc;

    //    public RadarRendered(Func<DynamicLeaf<IRadarSyncMember>, IComparable?> resFunc)
    //        => _resFunc = resFunc;

    //    public string Name => "Rendered";
    //    public FAI Icon => FAI.Eye;
    //    public string Tooltip => "Sort by rendered status.";

    //    public Func<DynamicLeaf<IRadarSyncMember>, IComparable?> KeySelector => _resFunc;
    //}
}

public static class StaticSorters
{
    public readonly struct DateAdded : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Date Added";
        public FAI Icon => FAI.Calendar; // Maybe change.
        public string Tooltip => "Sort by date added.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.CreatedAt;
    }

    public readonly struct Temporary : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Temporary";
        public FAI Icon => FAI.Clock; // Maybe change.
        public string Tooltip => "Sort temporary pairs.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.IsTemporary ? 0 : 1;
    }

    public readonly struct Favorite : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Favorite";
        public FAI Icon => FAI.Star; // Maybe change.
        public string Tooltip => "Sort by favorite status.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.IsFavorite ? 0 : 1;
    }

    public readonly struct ByRequestTime : ISortMethod<DynamicLeaf<RequestEntry>>
    {
        public string Name => "Request Time";
        public FAI Icon => FAI.Stopwatch;
        public string Tooltip => "Sort by request time.";
        public Func<DynamicLeaf<RequestEntry>, IComparable?> KeySelector => l => l.Data.ExpireTime;
    }
}
