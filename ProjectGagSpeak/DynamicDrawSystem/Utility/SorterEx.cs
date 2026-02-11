using CkCommons.DrawSystem;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagspeakAPI.Data;

namespace GagSpeak.DrawSystem;

public static class SorterEx
{
    // Used here as Kinkster is shared commonly across multiple draw systems.
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByRendered = new Rendered();
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByOnline = new Online();
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByFavorite = new Favorite();
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByPairName = new PairName();
    public static readonly ISortMethod<DynamicLeaf<Kinkster>> ByDateAdded = new DateAdded();

    public static readonly IReadOnlyList<ISortMethod<DynamicLeaf<Kinkster>>> AllGroupSteps
        = [ByRendered, ByOnline, ByFavorite, ByPairName, ByDateAdded];

    // Converters
    public static ISortMethod<DynamicLeaf<Kinkster>> ToSortMethod(this FolderSortFilter filter)
        => filter switch
        {
            FolderSortFilter.Rendered => ByRendered,
            FolderSortFilter.Online => ByOnline,
            FolderSortFilter.Favorite => ByFavorite,
            FolderSortFilter.Alphabetical => ByPairName,
            FolderSortFilter.DateAdded => ByDateAdded,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };

    public static FolderSortFilter ToFolderSortFilter(this ISortMethod<DynamicLeaf<Kinkster>> sortMethod)
        => sortMethod switch
        {
            Rendered => FolderSortFilter.Rendered,
            Online => FolderSortFilter.Online,
            Favorite => FolderSortFilter.Favorite,
            PairName => FolderSortFilter.Alphabetical,
            DateAdded => FolderSortFilter.DateAdded,
            _ => throw new ArgumentOutOfRangeException(nameof(sortMethod), sortMethod, null)
        };

    /// <summary>
    ///     Preset for the AllFolder, to sort by name -> visible -> online -> favorite.
    /// </summary>
    public static readonly IReadOnlyList<ISortMethod<DynamicLeaf<Kinkster>>> AllFolderSorter
        = [ ByRendered, ByOnline, ByFavorite, ByPairName ];

    // Sort Helpers

    public struct Rendered : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Rendered";
        public FAI Icon => FAI.Eye; // Maybe change.
        public string Tooltip => "Sort by rendered status.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.IsRendered ? 0 : 1;
    }

    public struct Online : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Online";
        public FAI Icon => FAI.Wifi; // Maybe change.
        public string Tooltip => "Sort by online status.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.IsOnline ? 0 : 1;
    }

    public struct Favorite : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Favorite";
        public FAI Icon => FAI.Star; // Maybe change.
        public string Tooltip => "Sort by favorite status.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.IsFavorite ? 0 : 1;
    }
    public struct DateAdded : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Date Added";
        public FAI Icon => FAI.Calendar; // Maybe change.
        public string Tooltip => "Sort by date added.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.UserPair.CreatedAt;
    }

    public struct PairName : ISortMethod<DynamicLeaf<Kinkster>>
    {
        public string Name => "Name";
        public FAI Icon => FAI.SortAlphaDown; // Maybe change.
        public string Tooltip => "Sort by name.";
        public Func<DynamicLeaf<Kinkster>, IComparable?> KeySelector => l => l.Data.AlphabeticalSortKey();
    }

    public struct ByRequestTime : ISortMethod<DynamicLeaf<RequestEntry>>
    {
        public string Name => "Request Time";
        public FAI Icon => FAI.Stopwatch;
        public string Tooltip => "Sort by request time.";
        public Func<DynamicLeaf<RequestEntry>, IComparable?> KeySelector => l => l.Data.ExpireTime;
    }

    public struct ByAliasName : ISortMethod<DynamicLeaf<AliasTrigger>>
    {
        public string Name => "Name";
        public FAI Icon => FAI.SortAlphaDown; // Maybe change.
        public string Tooltip => "Sort by name.";
        public Func<DynamicLeaf<AliasTrigger>, IComparable?> KeySelector => l => l.Data.Label;
    }
}

