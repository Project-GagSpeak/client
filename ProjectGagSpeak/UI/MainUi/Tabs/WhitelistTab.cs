using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Handlers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using OtterGui.Text;
using System.Collections.Immutable;
using TerraFX.Interop.Windows;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.LayoutManager;

namespace GagSpeak.Gui.MainWindow;

/// <summary> 
/// Sub-class of the main UI window. Handles drawing the whitelist/contacts tab of the main UI.
/// </summary>
public class WhitelistTab : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly KinksterManager _kinksters;
    private readonly DrawEntityFactory _factory;

    private List<IRequestsFolder> _requestFolders;
    private List<IDrawFolder> _drawFolders;
    private string _filter = string.Empty;
    public WhitelistTab(ILogger<WhitelistTab> logger, GagspeakMediator mediator,
        MainConfig config, KinksterManager kinksters, DrawEntityFactory factory)
        : base(logger, mediator)
    {
        _config = config;
        _kinksters = kinksters;
        _factory = factory;

        Mediator.Subscribe<RefreshUiRequestsMessage>(this, _ => _requestFolders = GetRequestFolders());
        Mediator.Subscribe<RefreshUiKinkstersMessage>(this, _ => _drawFolders = GetDrawFolders());

        _requestFolders = GetRequestFolders();
        _drawFolders = GetDrawFolders();
    }

    public void DrawWhitelistSection()
    {
        DrawSearchFilter();
        ImGui.Separator();

        using var _ = CkRaii.Child("content", ImGui.GetContentRegionAvail(), wFlags: WFlags.NoScrollbar);
        
        foreach (var item in _requestFolders)
            item.Draw();
        
        foreach (var item in _drawFolders)
            item.Draw();
    }

    private void DrawSearchFilter()
    {
        var width = ImGui.GetContentRegionAvail().X;
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var buttonSize = CkGui.IconTextButtonSize(FAI.Ban, "Clear") + spacing;
        var searchWidth = width - buttonSize;

        ImGui.SetNextItemWidth(searchWidth);
        if (ImGui.InputTextWithHint("##filter", "_filter for UID/notes", ref _filter, 255))
            _drawFolders = GetDrawFolders();

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Ban, "Clear", disabled: _filter.Length is 0))
        {
            _filter = string.Empty;
            _drawFolders = GetDrawFolders();
        }
        CkGui.AttachToolTip("Clears the filter");
    }

    public List<IRequestsFolder> GetRequestFolders()
    {
        // Create a list of request folders to display in the UI.
        List<IRequestsFolder> requestFolders = [];
        requestFolders.Add(_factory.CreatePairRequestFolder("Kinkster Requests"));
        requestFolders.Add(_factory.CreateCollarRequestFolder("Collar Requests"));
        return requestFolders;
    }

    /// <summary> Fetches the folders to draw in the user pair list (whitelist) </summary>
    /// <returns> List of IDrawFolders to display in the UI </returns>
    public List<IDrawFolder> GetDrawFolders()
    {
        List<IDrawFolder> drawFolders = [];
        // the list of all direct pairs.
        var allPairs = _kinksters.DirectPairs;
        // the filters list of pairs will be the pairs that match the filter.
        var filteredPairs = allPairs
            .Where(p =>
            {
                if (_filter.IsNullOrEmpty())
                    return true;
                // return a user if the filter matches their alias or ID, playerChara name, or the nickname we set.
                return p.UserData.AliasOrUID.Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                       (p.GetNickname()?.Contains(_filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (p.PlayerName?.Contains(_filter, StringComparison.OrdinalIgnoreCase) ?? false);
            });

        // the alphabetical sort function of the pairs.
        string? AlphabeticalSort(Kinkster u)
            => !string.IsNullOrEmpty(u.PlayerName)
                    ? (_config.Current.PreferNicknamesOverNames ? u.GetNickname() ?? u.UserData.AliasOrUID : u.PlayerName)
                    : u.GetNickname() ?? u.UserData.AliasOrUID;
        // filter based on who is online (or paused but that shouldnt exist yet unless i decide to add it later here)
        bool FilterOnlineOrPausedSelf(Kinkster u)
            => u.IsOnline || !u.IsOnline && !_config.Current.ShowOfflineUsersSeparately || u.UserPair.OwnPerms.IsPaused;
        // filter based on who is online or paused, but also allow paused users to be shown if they are self.
        bool FilterPairedOrPausedSelf(Kinkster u)
             => u.IsOnline || !u.IsOnline || u.UserPair.OwnPerms.IsPaused;
        bool FilterOfflineUsers(Kinkster u) 
            => !u.IsOnline && !u.UserPair.OwnPerms.IsPaused;
        // collect the sorted list
        List<Kinkster> BasicSortedList(IEnumerable<Kinkster> u)
            => u.OrderByDescending(u => u.IsVisible)
                .ThenByDescending(u => u.IsOnline)
                .ThenBy(AlphabeticalSort, StringComparer.OrdinalIgnoreCase)
                .ToList();
        // converter to immutable list
        ImmutableList<Kinkster> ImmutablePairList(IEnumerable<Kinkster> u) => u.ToImmutableList();

        // if we wish to display our visible users separately, then do so.
        if (_config.Current.ShowVisibleUsersSeparately)
        {
            var allVisiblePairs = ImmutablePairList(allPairs.Where(u => u.IsVisible));
            var filteredVisiblePairs = BasicSortedList(filteredPairs.Where(u => u.IsVisible));
            drawFolders.Add(_factory.CreateDrawTagFolder(Constants.CustomVisibleTag, filteredVisiblePairs, allVisiblePairs));
        }

        var allOnlinePairs = ImmutablePairList(allPairs.Where(FilterOnlineOrPausedSelf));
        var onlineFilteredPairs = BasicSortedList(filteredPairs.Where(u => u.IsOnline && FilterPairedOrPausedSelf(u)));
        drawFolders.Add(_factory.CreateDrawTagFolder(_config.Current.ShowOfflineUsersSeparately ? Constants.CustomOnlineTag : Constants.CustomAllTag, onlineFilteredPairs, allOnlinePairs));

        // if we want to show offline users separately,
        if (_config.Current.ShowOfflineUsersSeparately)
        {
            var allOfflinePairs = ImmutablePairList(allPairs.Where(FilterOfflineUsers));
            var filteredOfflinePairs = BasicSortedList(filteredPairs.Where(FilterOfflineUsers));
            drawFolders.Add(_factory.CreateDrawTagFolder(Constants.CustomOfflineTag, filteredOfflinePairs, allOfflinePairs));
        }

        return drawFolders;
    }
}
