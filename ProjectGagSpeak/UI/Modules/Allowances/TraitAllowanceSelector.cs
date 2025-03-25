using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components;
using GagSpeak.UI.Handlers;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;
using System.Collections.Immutable;
namespace GagSpeak.UI.Wardrobe;

public class TraitAllowanceSelector : DisposableMediatorSubscriberBase
{
    private readonly GagspeakConfigService _config;
    private readonly IdDisplayHandler _nameHandle;
    private readonly TraitsManager _manager;
    private readonly PairManager _pairs;
    private readonly FavoritesManager _favorites;
    private readonly CosmeticService _cosmetics;

    public TraitAllowanceSelector(ILogger<TraitAllowanceSelector> logger,
        GagspeakMediator mediator, GagspeakConfigService config,
        IdDisplayHandler handler, TraitsManager manager, PairManager pairs,
        FavoritesManager favorites, CosmeticService cosmetics) : base(logger, mediator)
    {
        _config = config;
        _nameHandle = handler;
        _manager = manager;
        _pairs = pairs;
        _favorites = favorites;
        _cosmetics = cosmetics;

        Mediator.Subscribe<RefreshUiMessage>(this, _ => UpdatePairList());
    }

    private ImmutableList<Pair>  _immutablePairs = ImmutableList<Pair>.Empty;
    // Internal Storage.
    private string              _searchValue = string.Empty;
    private float               _availableWidth = 0f;
    private HashSet<Pair>       _selectedPairs = new HashSet<Pair>();
    private Pair?               _lastPair = null;

    public ImmutableList<Pair>  FilteredPairs => _immutablePairs;
    public HashSet<Pair>        SelectedPairs => _selectedPairs;

    public void DrawSearch()
    {
        if (DrawerHelpers.FancySearchFilter("Pair Search", ImGui.GetContentRegionAvail().X, "Search for Pairs", ref _searchValue, 128, ImGui.GetFrameHeight(), FavoritesFilter))
        {
            UpdatePairList();
        }
    }

    // Filter Variables. Defines if our latest filter toggle was to select or deselect all.
    private bool _curFavState = false;

    private void FavoritesFilter()
    {
        if (CkGui.IconButton(FAI.Star, inPopup: true))
        {
            var favoritePairs = _immutablePairs.Where(p => _favorites._favoriteKinksters.Contains(p.UserData.UID));
            Logger.LogDebug("FavoritePairs: {0}", favoritePairs.Count());
            if (_curFavState)
            {
                Logger.LogDebug("Removing FavoritePairs: " + string.Join(", ", favoritePairs.Select(p => p.UserData.UID)));
                _selectedPairs.ExceptWith(favoritePairs);
            }
            else
            {
                Logger.LogDebug("Adding FavoritePairs: " + string.Join(", ", favoritePairs.Select(p => p.UserData.UID)));
                _selectedPairs.UnionWith(favoritePairs);
            }
            _curFavState = !_curFavState;
        }
        CkGui.AttachToolTip(_curFavState ? "Deselect all favorited pairs." : "Select all favorited pairs.");
    }
    
    public void DrawResultList()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2f));
        using var font = ImRaii.PushFont(UiBuilder.DefaultFont);
        using var buttonKiller = ImRaii.PushColor(ImGuiCol.Button, 0xFF000000)
            .Push(ImGuiCol.ButtonHovered, 0xFF000000).Push(ImGuiCol.ButtonActive, 0xFF000000);

        _availableWidth = ImGui.GetContentRegionAvail().X;
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetFrameHeight());
        var remainder = ImGuiClip.FilteredClippedDraw(_immutablePairs, skips, CheckFilter, DrawSelectable);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetFrameHeight());
    }

    public void UpdatePairList()
    {
        // Get direct pairs, then filter them.
        var filteredPairs = _pairs.DirectPairs
            .Where(p =>
            {
                if (_searchValue.IsNullOrEmpty())
                    return true;
                // Match for Alias, Uid, Nick, or PlayerName.
                return p.UserData.AliasOrUID.Contains(_searchValue, StringComparison.OrdinalIgnoreCase)
                    || (p.GetNickname()?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (p.PlayerName?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false);
            });

        // Take the remaining filtered list, and sort it.
        _immutablePairs = filteredPairs
            .OrderByDescending(u => u.IsVisible)
            .ThenByDescending(u => u.IsOnline)
            .ThenBy(pair => !pair.PlayerName.IsNullOrEmpty()
                ? (_config.Config.PreferNicknamesOverNames ? pair.GetNickAliasOrUid() : pair.PlayerName)
                : pair.GetNickAliasOrUid(), StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();

        // clear any pairs in the selected that are no longer present.
        _selectedPairs.RemoveWhere(p => !_immutablePairs.Contains(p));
    }


    private bool CheckFilter(Pair pair)
    {
        return pair.UserData.AliasOrUID.Contains(_searchValue, StringComparison.OrdinalIgnoreCase)
            || (pair.GetNickname()?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false)
            || (pair.PlayerName?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void DrawSelectable(Pair pair)
    {
        var selected = _selectedPairs.Contains(pair);
        var dispText = _nameHandle.GetPlayerText(pair);
        var shiftRegion = Vector2.Zero;
        // Create a child that draws out the pair element, and all its internals.
        using (ImRaii.Child("Selectable"+ pair.UserData.UID, new Vector2(_availableWidth, ImGui.GetFrameHeight())))
        {
            using (ImRaii.Group())
            {
                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
                DrawLeftIcon(dispText.text, pair.IsVisible, pair.IsOnline);

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(dispText.text);
            }
            CkGui.AttachToolTip("Hold CTRL to multi-select.--SEP--Hold SHIFT to group multi-select");

            ImGui.SameLine(0, 0);
            shiftRegion = DrawRight(dispText.text, pair);
        }
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var hovered = ImGui.IsMouseHoveringRect(min, max - shiftRegion);
        var color = selected
            ? CkColor.ElementBG.Uint()
            : hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : ImGui.GetColorU32(ImGuiCol.ChildBg);
        // draw the "hovered" frame color.
        ImGui.GetWindowDrawList().AddRectFilled(min, max, color);

        // handle how we select it.
        if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            if(ImGui.GetIO().KeyCtrl)
            {
                if (selected) _selectedPairs.Remove(pair);
                else _selectedPairs.Add(pair);
                _lastPair = pair;
            }
            else if (ImGui.GetIO().KeyShift && _lastPair is { } lastPair)
            {
                var idx = _immutablePairs.IndexOf(lastPair);
                var newIdx = _immutablePairs.IndexOf(pair);
                // Add all pairs between the last selected and this one.
                var start = Math.Min(idx, newIdx);
                var end = Math.Max(idx, newIdx);
                var pairsToToggle = _immutablePairs.Skip(start).Take(end - start + 1);
                // If our last selection is not in the list, we should remove all entries between.
                var lastContained = _selectedPairs.Contains(lastPair);
                var curContained = _selectedPairs.Contains(pair);

                // If both active, set all to inactive.
                switch((lastContained, curContained))
                {
                    case (true, true):
                        _selectedPairs.ExceptWith(pairsToToggle);
                        break;
                    case (true, false):
                        _selectedPairs.UnionWith(pairsToToggle);
                        _selectedPairs.Add(pair);
                        break;
                    case (false, true):
                        _selectedPairs.ExceptWith(pairsToToggle);
                        break;
                    case (false, false):
                        _selectedPairs.UnionWith(pairsToToggle);
                        _selectedPairs.Add(pair);
                        break;

                }

                _lastPair = pair;
            }
            else if (ImGui.GetIO().KeyAlt)
            {
                // toggle the favorite state of all selected pairs, based on if the current pair is favorited.
                if(_favorites._favoriteKinksters.Contains(pair.UserData.UID))
                    _favorites.RemoveKinksters(_selectedPairs.Select(p => p.UserData.UID));
                else
                    _favorites.AddKinksters(_selectedPairs.Select(p => p.UserData.UID));
            }
            else
            {
                // if we didnt hold control, make what we select the only selection.
                _selectedPairs.Clear();
                _selectedPairs.Add(pair);
                _lastPair = pair;
            }
        }
    }

    private void DrawLeftIcon(string displayName, bool isVisible, bool isOnline)
    {
        using var color = ImRaii.PushColor(ImGuiCol.Text, isOnline ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed);
        var userPairText = string.Empty;

        ImGui.AlignTextToFramePadding();
        if (!isOnline)
        {
            CkGui.IconText(FAI.User);
            CkGui.AttachToolTip(displayName + " is offline.");
        }
        else if (isVisible)
        {
            CkGui.IconText(FAI.Eye);
            CkGui.AttachToolTip(displayName + " is visible.");
        }
        else
        {
            CkGui.IconText(FAI.User);
            CkGui.AttachToolTip(displayName + " is online.");
        }
        ImGui.SameLine();
    }

    private Vector2 DrawRight(string dispName, Pair pair)
    {
        var endX = ImGui.GetContentRegionAvail().X;
        var currentX = endX - ImGui.GetTextLineHeightWithSpacing();
        ImGui.SameLine(0, currentX);
        // Draw the favoriteStar.
        Icons.DrawFavoriteStar(_favorites, pair.UserData.UID);

        // If we should draw the icon, adjust the currentX and draw it.
        if (pair.UserData.Tier is { } tier)
        {
            currentX -= ImGui.GetFrameHeight();
            ImGui.SameLine(0, currentX);
            DrawTierIcon(dispName, pair.UserData, tier);
        }
        return new Vector2(endX - currentX, 0);
    }

    private void DrawTierIcon(string displayName, UserData userData, CkSupporterTier tier)
    {
        if (tier is CkSupporterTier.NoRole)
            return;

        var img = _cosmetics.GetSupporterInfo(userData);
        if (img.SupporterWrap is { } wrap)
        {
            ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
            CkGui.AttachToolTip(img.Tooltip);
        }

    }
}
