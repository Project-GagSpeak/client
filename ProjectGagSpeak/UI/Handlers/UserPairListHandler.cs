using CkCommons.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using ImGuiNET;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.Gui.Handlers;

/// <summary>
/// Handler for drawing the list of user pairs in various ways.
/// Providing a handler for this allows us to draw the list in multiple formats and ways.
/// </summary>
public class UserPairListHandler
{
    private readonly ILogger<UserPairListHandler> _logger;
    private readonly GagspeakMediator _mediator;
    private List<IDrawFolder> _drawFolders;
    private List<DrawUserPair> _allUserPairDrawsDistinct; // distinct userpairs to draw
    private readonly KinksterManager _pairManager;
    private readonly DrawEntityFactory _drawEntityFactory;
    private readonly DrawRequests _drawRequests;
    private readonly MainConfig _configService;

    private Kinkster? _selectedPair = null;
    private string _filter = string.Empty;

    public UserPairListHandler(ILogger<UserPairListHandler> logger, GagspeakMediator mediator, 
        KinksterManager pairs, DrawEntityFactory drawEntityFactory, DrawRequests drawRequests,
        MainConfig configService)
    {
        _logger = logger;
        _mediator = mediator;
        _pairManager = pairs;
        _drawEntityFactory = drawEntityFactory;
        _drawRequests = drawRequests;
        _configService = configService;


        _drawRequests.UpdateKinksterRequests();
        UpdateDrawFoldersAndUserPairDraws();
    }

    /// <summary> List of all draw folders to display in the UI </summary>
    public List<DrawUserPair> AllPairDrawsDistinct => _allUserPairDrawsDistinct;

    public Kinkster? SelectedPair
    {
        get => _selectedPair;
        private set
        {
            if (_selectedPair != value)
            {
                _selectedPair = value;
                _logger.LogTrace("Selected Pair: " + value?.UserData.UID);
                _mediator.Publish(new UserPairSelected(value));
            }
        }
    }

    public string Filter
    {
        get => _filter;
        private set
        {
            if (!string.Equals(_filter, value, StringComparison.OrdinalIgnoreCase))
            {
                _mediator.Publish(new RefreshUiMessage());
            }

            _filter = value;
        }
    }

    /// <summary>
    /// Draws the list of pairs belonging to the client user.
    /// Groups the pairs by their tags (folders)
    /// </summary>
    public void DrawPairs()
    {
        using var child = ImRaii.Child("list", ImGui.GetContentRegionAvail(), border: false, WFlags.NoScrollbar);

        // Draw out the requests first.
        _drawRequests.Draw();

        // display a message is no pairs are present.
        if (AllPairDrawsDistinct.Count <= 0)
        {
            CkGui.ColorTextCentered("You Have No Pairs Added!", ImGuiColors.DalamudYellow);
        }
        else
        {
            foreach (var item in _drawFolders)
            {
                // draw the content
                if (item is DrawFolderBase folderBase && folderBase.ID == Constants.CustomAllTag && _configService.Current.ShowOfflineUsersSeparately) 
                    continue;
                // draw folder if not all tag.
                item.Draw();
            }
        }
    }

    // Probably rework this later idk.
    /// <summary> Draws all bi-directionally paired users (online or offline) without any tag header. </summary>
    public void DrawPairListSelectable(bool showOffline, byte id)
    {
        var tagToUse = Constants.CustomAllTag;

        var allTagFolder = _drawFolders
            .FirstOrDefault(folder => folder is DrawFolderBase && ((DrawFolderBase)folder).ID == tagToUse);

        if (allTagFolder is null)
            return;

        var folderDrawPairs = showOffline ? ((DrawFolderBase)allTagFolder).DrawPairs.ToList() : ((DrawFolderBase)allTagFolder).DrawPairs.Where(x => x.Pair.IsOnline).ToList();

        using var indent = ImRaii.PushIndent(CkGui.IconSize(FAI.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);

        if (!folderDrawPairs.Any())
        {
            ImGui.TextUnformatted("No Draw Pairs to Draw");
        }

        for (var i = 0; i < folderDrawPairs.Count(); i++)
        {
            var item = folderDrawPairs[i];

            var isSelected = SelectedPair is not null && SelectedPair.UserData.UID == item.Pair.UserData.UID;

            using (var color = ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(isSelected ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg), isSelected))
            {
                if (item.DrawPairedClient(id, true, true, false, false, false, true, false))
                {
                    SelectedPair = item.Pair;
                }
            }
        }
    }

    /// <summary> Draws the search filter for our user pair list (whitelist) </summary>
    public void DrawSearchFilter(bool showClearText)
    {
        var width = ImGui.GetContentRegionAvail().X;
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

        var buttonOneSize = showClearText
            ? CkGui.IconTextButtonSize(FAI.Ban, "Clear") + spacing
            : CkGui.IconButtonSize(FAI.Ban).X + spacing;

        var searchWidth = width - buttonOneSize;

        ImGui.SetNextItemWidth(searchWidth);
        var filter = Filter;
        if (ImGui.InputTextWithHint("##filter", "Filter for UID/notes", ref filter, 255))
            Filter = filter;

        // perform the firstbutton if we should.
        ImUtf8.SameLineInner();
        if (showClearText)
        {
            if (CkGui.IconTextButton(FAI.Ban, "Clear", disabled: string.IsNullOrEmpty(Filter)))
                Filter = string.Empty;
        }
        else
        {
            if (CkGui.IconButton(FAI.Ban, disabled: string.IsNullOrEmpty(Filter)))
                Filter = string.Empty;
        }
        CkGui.AttachToolTip("Clears the filter");
    }

    public void UpdateKinksterRequests() => _drawRequests.UpdateKinksterRequests();

    /// <summary> 
    /// Updates our draw folders and user pair draws.
    /// Called upon construction and UI Refresh event.
    /// </summary>
    public void UpdateDrawFoldersAndUserPairDraws()
    {
        _drawFolders = GetDrawFolders().ToList();
        _allUserPairDrawsDistinct = _drawFolders
            .SelectMany(folder => folder.DrawPairs) // throughout all the folders
            .DistinctBy(pair => pair.Pair)          // without duplicates
            .ToList();
    }

    /// <summary> Fetches the folders to draw in the user pair list (whitelist) </summary>
    /// <returns> List of IDrawFolders to display in the UI </returns>
    public IEnumerable<IDrawFolder> GetDrawFolders()
    {
        List<IDrawFolder> drawFolders = [];

        // the list of all direct pairs.
        var allPairs = _pairManager.DirectPairs;

        // the filters list of pairs will be the pairs that match the filter.
        var filteredPairs = allPairs
            .Where(p =>
            {
                if (Filter.IsNullOrEmpty()) return true;
                // return a user if the filter matches their alias or ID, playerChara name, or the nickname we set.
                return p.UserData.AliasOrUID.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
                       (p.GetNickname()?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (p.PlayerName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) ?? false);
            });

        // the alphabetical sort function of the pairs.
        string? AlphabeticalSort(Kinkster u)
            => !string.IsNullOrEmpty(u.PlayerName)
                    ? (_configService.Current.PreferNicknamesOverNames ? u.GetNickname() ?? u.UserData.AliasOrUID : u.PlayerName)
                    : u.GetNickname() ?? u.UserData.AliasOrUID;

        // filter based on who is online (or paused but that shouldnt exist yet unless i decide to add it later here)
        bool FilterOnlineOrPausedSelf(Kinkster u)
            => u.IsOnline || !u.IsOnline && !_configService.Current.ShowOfflineUsersSeparately || u.UserPair.OwnPerms.IsPaused;

        bool FilterPairedOrPausedSelf(Kinkster u)
             => u.IsOnline || !u.IsOnline || u.UserPair.OwnPerms.IsPaused;


        // collect the sorted list
        List<Kinkster> BasicSortedList(IEnumerable<Kinkster> u)
            => u.OrderByDescending(u => u.IsVisible)
                .ThenByDescending(u => u.IsOnline)
                .ThenBy(AlphabeticalSort, StringComparer.OrdinalIgnoreCase)
                .ToList();

        ImmutableList<Kinkster> ImmutablePairList(IEnumerable<Kinkster> u) => u.ToImmutableList();

        // if we should filter visible users
        bool FilterVisibleUsers(Kinkster u) => u.IsVisible;

        bool FilterOnlineUsers(Kinkster u) => u.IsOnline;

        bool FilterOfflineUsers(Kinkster u) => !u.IsOnline && !u.UserPair.OwnPerms.IsPaused;


        // if we wish to display our visible users separately, then do so.
        if (_configService.Current.ShowVisibleUsersSeparately)
        {
            // display all visible pairs, without filter
            var allVisiblePairs = ImmutablePairList(allPairs
                .Where(FilterVisibleUsers));
            // display the filtered visible pairs based on the filter we applied
            var filteredVisiblePairs = BasicSortedList(filteredPairs
                .Where(FilterVisibleUsers));

            // add the draw folders based on the 
            drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Constants.CustomVisibleTag, filteredVisiblePairs, allVisiblePairs));
        }

        var allOnlinePairs = ImmutablePairList(allPairs.Where(FilterOnlineOrPausedSelf));
        var onlineFilteredPairs = BasicSortedList(filteredPairs.Where(FilterOnlineUsers));

        var bidirectionalTaggedPairs = BasicSortedList(filteredPairs
            .Where(u => FilterOnlineUsers(u) && FilterPairedOrPausedSelf(u)));

        // _logger.LogTrace("Adding Pair Section List Tag: " + Constants.CustomAllTag, LoggerType.UI);
        drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Constants.CustomAllTag, bidirectionalTaggedPairs, allOnlinePairs));


        // if we want to show offline users seperately,
        if (_configService.Current.ShowOfflineUsersSeparately)
        {
            // create the draw folders for the online untagged pairs
            // _logger.LogTrace("Adding Pair Section List Tag: " + Constants.CustomOnlineTag, LoggerType.UI);
            drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Constants.CustomOnlineTag, onlineFilteredPairs, allOnlinePairs));

            // then do so.
            var allOfflinePairs = ImmutablePairList(allPairs
                .Where(FilterOfflineUsers));
            var filteredOfflinePairs = BasicSortedList(filteredPairs
                .Where(FilterOfflineUsers));

            // add the folder.
            // _logger.LogTrace("Adding Pair Section List Tag: " + Constants.CustomOfflineTag, LoggerType.UI);
            drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Constants.CustomOfflineTag, filteredOfflinePairs,
                allOfflinePairs));

        }

        return drawFolders;
    }
}
