using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components.UserPairList;
using ImGuiNET;
using OtterGui.Text;
using System.Collections.Immutable;

namespace GagSpeak.UI.Handlers;

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
    private readonly PairManager _pairManager;
    private readonly DrawEntityFactory _drawEntityFactory;
    private readonly DrawRequests _drawRequests;
    private readonly GagspeakConfigService _configService;
    private readonly UiSharedService _uiShared;
    private Pair? _selectedPair = null;
    private string _filter = string.Empty;

    public UserPairListHandler(ILogger<UserPairListHandler> logger, GagspeakMediator mediator, 
        PairManager pairs, DrawEntityFactory drawEntityFactory, DrawRequests drawRequests,
        GagspeakConfigService configService, UiSharedService uiShared)
    {
        _logger = logger;
        _mediator = mediator;
        _pairManager = pairs;
        _drawEntityFactory = drawEntityFactory;
        _drawRequests = drawRequests;
        _configService = configService;
        _uiShared = uiShared;

        _drawRequests.UpdateKinksterRequests();
        UpdateDrawFoldersAndUserPairDraws();
    }

    /// <summary> List of all draw folders to display in the UI </summary>
    public List<DrawUserPair> AllPairDrawsDistinct => _allUserPairDrawsDistinct;

    public Pair? SelectedPair
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
        using var child = ImRaii.Child("list", ImGui.GetContentRegionAvail(), border: false, ImGuiWindowFlags.NoScrollbar);

        // Draw out the requests first.
        _drawRequests.Draw();

        // display a message is no pairs are present.
        if (AllPairDrawsDistinct.Count <= 0)
        {
            UiSharedService.ColorTextCentered("You Have No Pairs Added!", ImGuiColors.DalamudYellow);
        }
        else
        {
            foreach (var item in _drawFolders)
            {
                // draw the content
                if (item is DrawFolderBase folderBase && folderBase.ID == Globals.CustomAllTag && _configService.Config.ShowOfflineUsersSeparately) 
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
        var tagToUse = Globals.CustomAllTag;

        var allTagFolder = _drawFolders
            .FirstOrDefault(folder => folder is DrawFolderBase && ((DrawFolderBase)folder).ID == tagToUse);

        if (allTagFolder is null)
            return;

        var folderDrawPairs = showOffline ? ((DrawFolderBase)allTagFolder).DrawPairs.ToList() : ((DrawFolderBase)allTagFolder).DrawPairs.Where(x => x.Pair.IsOnline).ToList();

        using var indent = ImRaii.PushIndent(_uiShared.GetIconData(FontAwesomeIcon.EllipsisV).X + ImGui.GetStyle().ItemSpacing.X, false);

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
            ? _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Ban, "Clear") + spacing
            : _uiShared.GetIconButtonSize(FontAwesomeIcon.Ban).X + spacing;

        var searchWidth = width - buttonOneSize;

        ImGui.SetNextItemWidth(searchWidth);
        var filter = Filter;
        if (ImGui.InputTextWithHint("##filter", "Filter for UID/notes", ref filter, 255))
            Filter = filter;

        // perform the firstbutton if we should.
        ImUtf8.SameLineInner();
        if (showClearText)
        {
            if (_uiShared.IconTextButton(FontAwesomeIcon.Ban, "Clear", disabled: string.IsNullOrEmpty(Filter)))
                Filter = string.Empty;
        }
        else
        {
            if (_uiShared.IconButton(FontAwesomeIcon.Ban, disabled: string.IsNullOrEmpty(Filter)))
                Filter = string.Empty;
        }
        UiSharedService.AttachToolTip("Clears the filter");
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
        string? AlphabeticalSort(Pair u)
            => !string.IsNullOrEmpty(u.PlayerName)
                    ? (_configService.Config.PreferNicknamesOverNames ? u.GetNickname() ?? u.UserData.AliasOrUID : u.PlayerName)
                    : u.GetNickname() ?? u.UserData.AliasOrUID;

        // filter based on who is online (or paused but that shouldnt exist yet unless i decide to add it later here)
        bool FilterOnlineOrPausedSelf(Pair u)
            => u.IsOnline || !u.IsOnline && !_configService.Config.ShowOfflineUsersSeparately || u.UserPair.OwnPairPerms.IsPaused;

        bool FilterPairedOrPausedSelf(Pair u)
             => u.IsOnline || !u.IsOnline || u.UserPair.OwnPairPerms.IsPaused;


        // collect the sorted list
        List<Pair> BasicSortedList(IEnumerable<Pair> u)
            => u.OrderByDescending(u => u.IsVisible)
                .ThenByDescending(u => u.IsOnline)
                .ThenBy(AlphabeticalSort, StringComparer.OrdinalIgnoreCase)
                .ToList();

        ImmutableList<Pair> ImmutablePairList(IEnumerable<Pair> u) => u.ToImmutableList();

        // if we should filter visible users
        bool FilterVisibleUsers(Pair u) => u.IsVisible;

        bool FilterOnlineUsers(Pair u) => u.IsOnline;

        bool FilterOfflineUsers(Pair u) => !u.IsOnline && !u.UserPair.OwnPairPerms.IsPaused;


        // if we wish to display our visible users separately, then do so.
        if (_configService.Config.ShowVisibleUsersSeparately)
        {
            // display all visible pairs, without filter
            var allVisiblePairs = ImmutablePairList(allPairs
                .Where(FilterVisibleUsers));
            // display the filtered visible pairs based on the filter we applied
            var filteredVisiblePairs = BasicSortedList(filteredPairs
                .Where(FilterVisibleUsers));

            // add the draw folders based on the 
            drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Globals.CustomVisibleTag, filteredVisiblePairs, allVisiblePairs));
        }

        var allOnlinePairs = ImmutablePairList(allPairs.Where(FilterOnlineOrPausedSelf));
        var onlineFilteredPairs = BasicSortedList(filteredPairs.Where(FilterOnlineUsers));

        var bidirectionalTaggedPairs = BasicSortedList(filteredPairs
            .Where(u => FilterOnlineUsers(u) && FilterPairedOrPausedSelf(u)));

        _logger.LogDebug("Adding Pair Section List Tag: " + Globals.CustomAllTag, LoggerType.UserPairDrawer);
        drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Globals.CustomAllTag, bidirectionalTaggedPairs, allOnlinePairs));


        // if we want to show offline users seperately,
        if (_configService.Config.ShowOfflineUsersSeparately)
        {
            // create the draw folders for the online untagged pairs
            _logger.LogDebug("Adding Pair Section List Tag: " + Globals.CustomOnlineTag, LoggerType.UserPairDrawer);
            drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Globals.CustomOnlineTag, onlineFilteredPairs, allOnlinePairs));

            // then do so.
            var allOfflinePairs = ImmutablePairList(allPairs
                .Where(FilterOfflineUsers));
            var filteredOfflinePairs = BasicSortedList(filteredPairs
                .Where(FilterOfflineUsers));

            // add the folder.
            _logger.LogDebug("Adding Pair Section List Tag: " + Globals.CustomOfflineTag, LoggerType.UserPairDrawer);
            drawFolders.Add(_drawEntityFactory.CreateDrawTagFolder(Globals.CustomOfflineTag, filteredOfflinePairs,
                allOfflinePairs));

        }

        return drawFolders;
    }
}
