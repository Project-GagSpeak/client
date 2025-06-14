using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Clip;
using GagSpeak.CkCommons.Gui.Handlers;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.Kinksters.Pairs;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Textures;
using ImGuiNET;
using System.Collections.Immutable;

namespace GagSpeak.CkCommons.Gui.Wardrobe;

/// <summary> For drawing a list of kinksters, allowing for searching and filtering. </summary>
/// <remarks> You will need to add a mediator subscription to update things yourself! </remarks>
public class KinksterSearchList
{
    protected readonly MainConfig _config;
    protected readonly IdDisplayHandler _nameHandle;
    protected readonly PairManager _pairs;
    protected readonly CosmeticService _cosmetics;

    protected ImmutableList<Pair> _immutablePairs = ImmutableList<Pair>.Empty;
    protected string _searchValue = string.Empty;

    public KinksterSearchList(
        MainConfig config,
        IdDisplayHandler handler, 
        PairManager pairs, 
        CosmeticService cosmetics)
    {
        _config = config;
        _nameHandle = handler;
        _pairs = pairs;
        _cosmetics = cosmetics;
    }

    public virtual void DrawSearch(string id, string hint = "Search for Kinksters", uint len = 128, float buttonWidth = 0f, Action? buttons = null)
    {
        if (FancySearchBar.Draw(id, ImGui.GetContentRegionAvail().X, hint, ref _searchValue, len, buttonWidth, buttons))
            UpdateList();
    }
    
    /// <summary> Draws the resulting list, with a custom drawaction allowed for replacement display. </summary>
    /// <remarks> Keep in mind the drawact height must match the passed in height. </remarks>
    public void DrawResultList(Action<Pair, float>? selectableDraw = null)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(2f));
        using var font = ImRaii.PushFont(UiBuilder.DefaultFont);
        using var buttonKiller = ImRaii.PushColor(ImGuiCol.Button, 0xFF000000)
            .Push(ImGuiCol.ButtonHovered, 0xFF000000).Push(ImGuiCol.ButtonActive, 0xFF000000);

        var drawAct = selectableDraw ?? DrawSelectable;
        var skips = ImGuiClip.GetNecessarySkips(ImGui.GetFrameHeight());
        var remainder = CkGuiClip.FilteredClippedDraw(_immutablePairs, skips, CheckFilter, drawAct);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetFrameHeight());
    }

    public void UpdateList()
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
                ? (_config.Current.PreferNicknamesOverNames ? pair.GetNickAliasOrUid() : pair.PlayerName)
                : pair.GetNickAliasOrUid(), StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();
    }

    private bool CheckFilter(Pair pair)
    {
        return pair.UserData.AliasOrUID.Contains(_searchValue, StringComparison.OrdinalIgnoreCase)
            || (pair.GetNickname()?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false)
            || (pair.PlayerName?.Contains(_searchValue, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary> If you overwrite this, make sure the height is ImGui.GetFrameHeight(). </summary>
    protected virtual void DrawSelectable(Pair pair, float width)
    {
        using var id = ImRaii.PushId(pair.UserData.UID);

        var displayName = pair.GetNickAliasOrUid();
        var isOnline = pair.IsOnline;
        // Draw the tier icon if applicable.
        DrawTierIcon(pair);
        // Draw the name and online status.
        _nameHandle.DrawPairText(pair.UserData.UID, pair, 0f, () => width - ImGui.GetFrameHeight() - 20f, true, true);
    }

    protected void DrawTierIcon(Pair pair)
    {
        if (pair.UserData.Tier is CkSupporterTier.NoRole)
            return;

        var img = _cosmetics.GetSupporterInfo(pair.UserData);
        if (img.SupporterWrap is { } wrap)
        {
            ImGui.Image(wrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
            CkGui.AttachToolTip(img.Tooltip);
        }
    }
}
